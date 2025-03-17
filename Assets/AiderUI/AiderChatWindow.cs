using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Diagnostics; //Added for process management
using System.IO; //Added for file path operation
using System.Threading; 


public class AiderChatWindow : EditorWindow
{
    public ScrollView chatContainer;
    public AiderChatList chatList = new("ChatList");

    private Process aiderBridgeProcess; //process to manage aider bridge connection

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
                UnityEngine.Debug.LogError("Aider Bridge Output: " + e.Data);
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

        UnityEngine.Debug.LogError("Aider Bridge started.");

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
            UnityEngine.Debug.LogError("Aider Bridge stopped.");
        }
    }


    [MenuItem("Aider/Chat Window")]
    public static void ShowWindow()
    {
        GetWindow<AiderChatWindow>("Aider");
    }

    private void CreateGUI()
    {
        VisualElement root = rootVisualElement;
        root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/AiderWindow.uss"));
        root.AddToClassList(EditorGUIUtility.isProSkin ? "dark-mode" : "light-mode");
        root.AddToClassList("aider-chat-window");


        chatContainer = new ScrollView();
        chatContainer.AddToClassList("chat-container");
        root.Add(chatContainer);

        for (int i = 0; i < chatList.Count; i++)
        {
            UnityEngine.Debug.Log($"Adding chat message {i}");
            chatList[i].Build(chatContainer);
        }

        var inspectorSkin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);

        // make a footer with a text input and send button with up arrow icon
        VisualElement footer = new();
        footer.AddToClassList("footer");
        root.Add(footer);

        // make chat input
        TextField textField = new()
        {
            multiline = true,
        };
        textField.AddToClassList("chat-input");
        textField.SetPlaceholderText("How can I help you?");
        footer.Add(textField);

        Button button = new Button(() =>
        {
            UnityEngine.Debug.Log($"Sending: {textField.value}");
            var req = new AiderRequest(textField.value);
            textField.value = "";

            Client.Send(req);
            chatList.AddMessage(new AiderChatMessage(chatContainer, req.Content, true, "Command Run: " + req.Command.ToString()));
            chatList.AddMessage(new AiderChatMessage(chatContainer, "", false, "Thinking..."));
            Client.AsyncReceive(HandleResponse);
        })
        {
            style =
            {
                scale = new StyleScale(StyleKeyword.Null),
            }
        };
        button.AddToClassList("send-button");
        footer.Add(button);

        var config = new AiderConfigWindow(root);

        // add floating settings button at top left corner of window
        Button settingsButton = new Button(() =>
        {
           config.Toggle();
        });
        settingsButton.AddToClassList("settings-button");
        root.Add(settingsButton);

    }

    private async void HandleResponse(AiderResponse response)
    {
        // modify current ai message
        var current = chatList.Last();
        if (!current.isUser)
        {
            UnityEngine.Debug.Log($"Add part {response.Part}: {response.Content}");
            current.AppendText(response.Content);

            if (response.Last)
            {
                UnityEngine.Debug.Log("AI response complete");
                AssetDatabase.Refresh();
                chatList.SerializeChat();
                await Task.Delay(1000);
                AssetDatabase.Refresh();
            }
        }
        else
        {
            UnityEngine.Debug.LogError("Expected AI response, but got user response");
        }
    }
}