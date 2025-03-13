

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


public class AiderChatWindow : EditorWindow
{
    public List<ChatMessage> chatList = new();
    public ScrollView chatContainer;


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
            Debug.Log($"Sending: {textField.value}");
            var req = new AiderRequest(textField.value);
            textField.value = "";

            Client.Send(req);
            chatList.Add( new ChatMessage(chatContainer, req.Content, true, "Command Run: " + req.Command.ToString()));
            chatList.Add(new ChatMessage(chatContainer, "", false, "Thinking..."));
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
            Debug.Log($"Add part {response.Part}: {response.Content}");
            current.AppendText(response.Content);

            if (response.Last)
            {
                Debug.Log("AI response complete");
                AssetDatabase.Refresh();
                await Task.Delay(1000);
                AssetDatabase.Refresh();
            }
        }
        else
        {
            Debug.LogError("Expected AI response, but got user response");
        }
    }
}