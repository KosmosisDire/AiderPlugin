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
    static void ConnectToBridge()
    {
        client = new();
        client.Connect("localhost", 65234);
        stream = client.GetStream();
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

        byte[] data = request.Serialize();
        stream.Write(data, 0, data.Length);
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
