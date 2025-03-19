using System;
using System.Collections.Generic;
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
    public static void ConnectToBridge()
    {
        try
        {
            AiderRunner.EnsureAiderBridgeRunning();
            client = new();
            client.Connect("localhost", 65234);
            stream = client.GetStream();
            Debug.Log("Connected to Aider Bridge.");//logs successful connection
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to connect to Aider Bridge: " + e.Message);
            client = null;//clears client reference for failed connection
            stream = null;//clears stream reference
        }
    }

    public static void Send(AiderRequest request)
    {
        if (!IsConnected)
        {
            Debug.LogWarning("Not connected to bridge, trying to connect now...");
            ConnectToBridge();

            if (!IsConnected)
            {
                Debug.LogError("Failed to connect to bridge");
                return;
            }
        }


        try
        {
            byte[] data = request.Serialize();
            stream.Write(data, 0, data.Length);
        }
        catch (Exception e)
        {
            Debug.LogError("Error sending message: " + e.Message);
            ConnectToBridge(); //try to reconnect if send fails
            if (IsConnected)
            {
                byte[] data = request.Serialize();
                stream.Write(data, 0, data.Length);
            }
            else
            {
                Debug.LogError("Failed to reconnect after send error");

            }
        }
    }

    static CancellationTokenSource cts = new();
    public static async void AsyncReceive(Action<AiderResponse> callback)
    {
        cts.Cancel();

        while (true)
        {
            if (!IsConnected)
            {
                Debug.LogError("Not connected to bridge");
                break;
            }

            byte[] data = new byte[1024];
            cts = new();
            int bytes = await stream.ReadAsync(data, 0, data.Length, cts.Token);

            if (bytes == 0)
            {
                Debug.LogError("Connection closed");
                break;
            }

            var response = AiderResponse.Deserialize(data);
            callback(response);

            if (response.Last)
            {
                break;
            }
        }
    }

    public static AiderResponse SyncReceiveOne()
    {
        if (!IsConnected)
        {
            return AiderResponse.Error("Not connected to bridge");
        }

        byte[] data = new byte[1024];
        int bytes = stream.Read(data, 0, data.Length);

        if (bytes == 0)
        {
            return AiderResponse.Error("Connection closed");
        }

        var response = AiderResponse.Deserialize(data);
        return response;
    }

    /// <returns>Get a list of all files currently in the context</returns>
    public static string[] GetContextList()
    {
        Send(new AiderRequest(AiderCommand.Ls, ""));
        var resp =  SyncReceiveOne();
        if (resp.IsError)
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
    public static bool AddFile(string filePath)
    {
        Send(new AiderRequest(AiderCommand.Add, filePath));
        var resp = SyncReceiveOne();
        return !resp.IsError;
    }

    /// <summary>
    /// Drop a file from the context
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns>True if the drop succeeded, false if the drop failed for any reason.</returns>
    public static bool DropFile(string filePath)
    {
        Send(new AiderRequest(AiderCommand.Drop, filePath));
        var resp = SyncReceiveOne();
        return !resp.IsError;
    }

}
