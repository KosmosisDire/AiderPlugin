using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;


public class Client : Editor
{
    static TcpClient client;
    static NetworkStream stream;

    static bool IsConnected => client != null && stream != null && stream.CanWrite;

    //Attempts to establish a tcp connection to aider bridge
    [MenuItem("Aider/Connect to Bridge")]
    public static async Task<bool> ConnectToBridge()
    {
        try
        {
            AiderRunner.EnsureAiderBridgeRunning();
            client = new();
            do 
            {
                try
                {
                    await client.ConnectAsync("localhost", 65234);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Trying to connect: {e.Message}");
                    await Task.Delay(1000);
                }
            }
            while (!client.Connected);

            stream = client.GetStream();
            Debug.Log("Connected to Aider Bridge.");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to connect to Aider Bridge: " + e.Message);
            client = null;
            stream = null;
            return false;
        }
    }

    public static async Task<bool> Send(AiderRequest request)
    {
        Debug.Log($"Sending request: {request.Content}");
        if (!IsConnected)
        {
            Debug.LogWarning("Not connected to bridge, trying to connect now...");
            await ConnectToBridge();

            if (!IsConnected)
            {
                Debug.LogError("Failed to connect to bridge");
                return false;
            }
        }


        try
        {
            byte[] data = request.Serialize();
            stream.Write(data, 0, data.Length);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("Error sending message: " + e.Message);
            await ConnectToBridge();
            if (IsConnected)
            {
                try
                {
                    byte[] data = request.Serialize();
                    stream.Write(data, 0, data.Length);
                    return true;
                }
                catch (Exception e2)
                {
                    Debug.LogError("Failed to reconnect after send error: " + e2.Message);
                }
            }
            else
            {
                Debug.LogError("Failed to reconnect after send error");
            }

            return false;
        }
    }

    private static async Task ReadExactlyAsync(NetworkStream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        int totalBytesRead = 0;
        while (totalBytesRead < count)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int bytesRead = await stream.ReadAsync(buffer, offset + totalBytesRead, count - totalBytesRead, cancellationToken);
            if (bytesRead == 0)
            {
                throw new EndOfStreamException($"Stream ended while trying to read {count} bytes. Read {totalBytesRead} bytes.");
            }
            totalBytesRead += bytesRead;
        }
    }

    private static async Task<AiderResponse> ReceiveSingleResponseAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            return AiderResponse.Error("Not connected to bridge");
        }

        byte[] headerBytes = new byte[AiderResponseHeader.HeaderSize];
        try
        {
            await ReadExactlyAsync(stream, headerBytes, 0, AiderResponseHeader.HeaderSize, cancellationToken);
        }
        catch (Exception ex)
        {
            return AiderResponse.Error($"Failed to read header: {ex.Message}");
        }

        AiderResponseHeader header;
        header = AiderResponseHeader.Deserialize(headerBytes);
        Debug.Log($"Received header: {header.ContentLength} bytes, last: {header.IsLast}, isDiff: {header.IsDiff}, isError: {header.IsError}, tokensSent: {header.TokensSent}, tokensReceived: {header.TokensReceived}, messageCost: {header.MessageCost}, sessionCost: {header.SessionCost}");

        if (header.ContentLength == 0)
        {
            return new AiderResponse("", header);
        }
        if (header.ContentLength < 0)
        {
                return AiderResponse.Error($"Invalid content length: {header.ContentLength}");
        }
        if (header.ContentLength > 500000) // 0.5 MB
        {
            return AiderResponse.Error($"Tried to read {header.ContentLength} bytes, which is too large.");
        }

        byte[] contentBytes = new byte[header.ContentLength];
        try
        {
            await ReadExactlyAsync(stream, contentBytes, 0, header.ContentLength, cancellationToken);
        }
        catch (Exception ex)
        {
            return AiderResponse.Error($"Failed to read content: {ex.Message}");
        }

        string content = System.Text.Encoding.UTF8.GetString(contentBytes);
        var response = new AiderResponse(content, header);
        Debug.Log($"Received response: {content}");
        return response;
    }

    public static async Task ReceiveAllResponesAsync(Action<AiderResponse> callback,CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            AiderResponse response = await ReceiveSingleResponseAsync(cancellationToken);
            callback?.Invoke(response);

            if (response.Header.IsError || response.Header.IsLast)
            {
                Debug.Log("Received last message or error, stopping receive loop.");
                return;
            }
        }

        if (cancellationToken.IsCancellationRequested)
        {
             Debug.Log("Receive loop cancelled.");
        }
    }

    /// <returns>Get a list of all files currently in the context</returns>
    public static async Task<string[]> GetContextList()
    {
        if (!await Send(new AiderRequest(AiderCommand.Ls, "")))
        {
            return new string[0];
        }

        var resp =  await ReceiveSingleResponseAsync();
        if (resp.Header.IsError)
        {
            return new string[0];
        }

        return resp.Content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Add a file to the context
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns>True if the add succeeded, false if the add failed for any reason.</returns>
    public static async Task<bool> AddFile(string filePath)
    {
        if (!await Send(new AiderRequest(AiderCommand.Add, filePath)))
        {
            return false;
        }

        var resp = await ReceiveSingleResponseAsync();
        return !resp.Header.IsError;
    }

    public static async Task<bool> AddFileFromMemory(string fileName, string content)
    {
        // save file to temp directory
        string tempPath = Path.Combine(Application.temporaryCachePath, fileName);
        File.WriteAllText(tempPath, content);

        // add file to context
        if (!await Send(new AiderRequest(AiderCommand.Add, tempPath)))
        {
            return false;
        }

        var resp = await ReceiveSingleResponseAsync();
        return !resp.Header.IsError;
    }

    /// <summary>
    /// Drop a file from the context
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns>True if the drop succeeded, false if the drop failed for any reason.</returns>
    public static async Task<bool> DropFile(string filePath)
    {
        if (!await Send(new AiderRequest(AiderCommand.Drop, filePath)))
        {
            return false;
        }

        var resp = await ReceiveSingleResponseAsync();
        return !resp.Header.IsError;
    }

    public static async Task<bool> Reset()
    {
        if (!await Send(new AiderRequest(AiderCommand.Reset, "")))
        {
            return false;
        }
        
        var resp = await ReceiveSingleResponseAsync();
        return !resp.Header.IsError;
    }

}
