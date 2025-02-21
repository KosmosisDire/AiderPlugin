

using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

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


    [MenuItem("Aider/Chat Window")]
    public static void ShowWindow()
    {
        GetWindow<AiderWindow>("Aider");
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
            Debug.Log($"Sending: {textField.value}");
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
            Debug.Log($"Add part {response.Part}: {response.Content}");
            current.message += response.Content;
            UpdateMessage(current, messageContainer);
        }
        else
        {
            Debug.LogError("Expected AI response, but got user response");
        }
    }
}