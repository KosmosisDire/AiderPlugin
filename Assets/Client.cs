using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class Client : MonoBehaviour
{
    [MenuItem("Aider/Connect to Bridge")]
    static void ConnectToBridge()
    {
        TcpClient client = new();
        // string portStr = Environment.GetEnvironmentVariable("AIDER_BRIDGE_PORT");
        
        // if (string.IsNullOrWhiteSpace(portStr))
        // {
        //     Debug.LogError("AIDER_BRIDGE_PORT environment variable is not set");
        //     return;
        // }

        // if (!int.TryParse(portStr, out int port))
        // {
        //     Debug.LogError("AIDER_BRIDGE_PORT environment variable is not a valid number");
        //     return;
        // }

        client.Connect("localhost", 65234);

        NetworkStream stream = client.GetStream();
        
        // send "hello world" every 2 seconds async
        SendHelloWorld(stream);
    }

    static async void SendHelloWorld(NetworkStream stream)
    {
        int i = 0;
        while (i < 10)
        {
            byte[] data = System.Text.Encoding.ASCII.GetBytes("Hello World");
            await stream.WriteAsync(data, 0, data.Length);
            await Task.Delay(2000);
            i++;
        }
    }
}
