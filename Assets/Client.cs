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

    [MenuItem("Aider/Connect to Bridge")]
    public static void ConnectToBridge()
    //Attempts to establish a tcp connection to aider bridge
    {
        try
        {
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
                MainThread.LogError("Not connected to bridge");
                break;
            }

            byte[] data = new byte[1024];
            cts = new();
            int bytes = await stream.ReadAsync(data, 0, data.Length, cts.Token);

            if (bytes == 0)
            {
                MainThread.LogError("Connection closed");
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

}
