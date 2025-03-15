

using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Diagnostics; //added for process management
using System.IO;//added for file path operation


public class ChatEntry
{
    public string message;
    public bool isUser;
    public string placeholder; // show placeholder is the message in null or whitespace
    public Label label; 
}

public class AiderWindow : EditorWindow
{
    public List<ChatEntry> chat = new();
    public ScrollView messageContainer;

    static Color userColor = new Color(0.5f, 0.5f, 1.0f);
    static Color aiColor = new Color(0.8f, 0.5f, 1.0f);


    private Process aiderBridgeProcess; // Added to store process


    [MenuItem("Aider/Chat Window")]
    public static void ShowWindow()
    {
        GetWindow<AiderWindow>("Aider");
    }



    private void OnEnable()
    {
        StartAiderBridge(); // Start the bridge when the window is enabled
    }

    private void OnDisable()
    {
        StopAiderBridge(); // Stop the bridge when the window is disabled
    }

    private void StartAiderBridge()
    {
        if (aiderBridgeProcess != null && !aiderBridgeProcess.HasExited)
        {
            return; // Bridge is already running
        }

        string scriptPath = Path.Combine(Application.dataPath, "Backend/aider-bridge.py"); // Adjust the path as needed
        if (!File.Exists(scriptPath))
        {
            UnityEngine.Debug.LogError("AiderBridge.py not found at: " + scriptPath);
            return;
        }

        aiderBridgeProcess = new Process();
        aiderBridgeProcess.StartInfo.FileName = "python"; // Or "python3"
        aiderBridgeProcess.StartInfo.Arguments = scriptPath;
        aiderBridgeProcess.StartInfo.UseShellExecute = false;
        aiderBridgeProcess.StartInfo.RedirectStandardOutput = true;
        aiderBridgeProcess.StartInfo.RedirectStandardError = true;
        aiderBridgeProcess.StartInfo.CreateNoWindow = true; // Run in background

        aiderBridgeProcess.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                UnityEngine.Debug.Log("Aider Bridge Output: " + e.Data);
            }
        };

        aiderBridgeProcess.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                UnityEngine.Debug.LogError("Aider Bridge Error: " + e.Data);
            }
        };

        aiderBridgeProcess.Start();
        aiderBridgeProcess.BeginOutputReadLine();
        aiderBridgeProcess.BeginErrorReadLine();

        UnityEngine.Debug.Log("Aider Bridge started.");

        // make sure the bridge is running before connecting.
        System.Threading.Thread.Sleep(500); // Adjust as needed.
        Client.ConnectToBridge(); // connect to bridge.
    }

    private void StopAiderBridge()
    {
        if (aiderBridgeProcess != null && !aiderBridgeProcess.HasExited)
        {
            aiderBridgeProcess.Kill();
            aiderBridgeProcess.WaitForExit();
            UnityEngine.Debug.Log("Aider Bridge stopped.");
        }
    }


    void UpdateMessage(ChatEntry chat, VisualElement parent)
    {
        var message = chat.message;
        if (string.IsNullOrWhiteSpace(message))
        {
            message = chat.placeholder;
        }

        var label = chat.label;
        if (label == null)
        {

            var container = new VisualElement()
            {
                style =
                {
                    paddingLeft = 5,
                    paddingRight = 5,
                    paddingTop = 5,
                    paddingBottom = 5,
                    backgroundColor = chat.isUser ? userColor.WithAlpha(0.2f) : aiColor.WithAlpha(0.2f),
                    borderTopLeftRadius = 5,
                    borderTopRightRadius = 5,
                    borderBottomLeftRadius = 5,
                    borderBottomRightRadius = 5,
                    marginBottom = 5,
                    borderLeftColor = chat.isUser ? userColor : aiColor,
                    borderLeftWidth = 1,
                    borderRightColor = chat.isUser ? userColor : aiColor,
                    borderRightWidth = 1,
                    borderTopColor = chat.isUser ? userColor : aiColor,
                    borderTopWidth = 1,
                    borderBottomColor = chat.isUser ? userColor : aiColor,
                    borderBottomWidth = 1,
                }
            };
            parent.Add(container);
            label = new Label(message);
            label.style.whiteSpace = WhiteSpace.PreWrap;
            container.Add(label);

            chat.label = label;
        }
        else
        {
            label.text = message;
        }
    }

    private void CreateGUI()
    {
        VisualElement root = rootVisualElement;

        messageContainer = new ScrollView()
        {
            style =
            {
                flexGrow = 1,
                flexDirection = FlexDirection.Column,
                paddingLeft = 5,
                paddingRight = 5,
                paddingTop = 5,
                paddingBottom = 5,
            }
        };
        root.Add(messageContainer);

        // make chat input
        TextField textField = new()
        {
            multiline = true,
        };
        root.Add(textField);

        Button button = new Button(() =>
        {
            UnityEngine.Debug.Log($"Sending: {textField.value}");
            var req = new AiderRequest(textField.value);
            textField.value = "";

            Client.Send(req);
            chat.Add(new ChatEntry { message = req.Content, isUser = true, placeholder = "Command Run: " + req.Command.ToString() });
            UpdateMessage(chat.Last(), messageContainer);

            chat.Add(new ChatEntry { message = "", isUser = false, placeholder = "Thinking..." });
            Client.AsyncReceive(HandleResponse);
        });
        button.text = "Send";
        root.Add(button);
    }

    private void HandleResponse(AiderResponse response)
    {
        // modify current ai message
        var current = chat.Last();
        if (!current.isUser)
        {
            UnityEngine.Debug.Log($"Add part {response.Part}: {response.Content}");
            current.message += response.Content;
            UpdateMessage(current, messageContainer);
        }
        else
        {
            UnityEngine.Debug.LogError("Expected AI response, but got user response");
        }
    }
}