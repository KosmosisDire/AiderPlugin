using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class AiderChatWindow : EditorWindow
{
    public AiderChatList chatList;
    public AiderContextList contextList;

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

        chatList = new AiderChatList("ChatList");
        root.Add(chatList);

        var inspectorSkin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);

        // make a footer with a text input and send button with up arrow icon
        VisualElement footer = new();
        footer.AddToClassList("footer");
        root.Add(footer);

        VisualElement inputWrapper = new();
        inputWrapper.AddToClassList("input-wrapper");
        footer.Add(inputWrapper);

        // make chat input
        TextField textField = new()
        {
            multiline = true,
        };
        textField.AddToClassList("chat-input");
        textField.SetPlaceholderText("How can I help you?");
        inputWrapper.Add(textField);

        Button button = new Button(() =>
        {
            Debug.Log($"Sending: {textField.value}");
            var req = new AiderRequest(textField.value);
            textField.value = "";

            Client.Send(req);
            chatList.AddMessage(req.Content, true, "Empty Message");
            chatList.AddMessage("", false, "Thinking...");
            Client.AsyncReceive(HandleResponse);
        })
        {
            style =
            {
                scale = new StyleScale(StyleKeyword.Null),
            }
        };

        button.AddToClassList("send-button");
        inputWrapper.Add(button);

        // make context list
        contextList = new AiderContextList();
        contextList.Update(Client.GetContextList());
        footer.Add(contextList);

        var config = new AiderConfigWindow(root);

        // add floating settings button at top left corner of window
        Button settingsButton = new Button(() =>
        {
           config.Toggle();
        });
        settingsButton.AddToClassList("settings-button");
        root.Add(settingsButton);

        root.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
        root.RegisterCallback<DragPerformEvent>(OnDragPerform);
    }
    private void OnDragUpdated(DragUpdatedEvent evt)
    {
        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
    }

    private void OnDragPerform(DragPerformEvent evt)
    {
        DragAndDrop.AcceptDrag();

        foreach (var path in DragAndDrop.paths)
        {
            if (File.Exists(path))
            {
                Client.AddFile(path);
                var context = Client.GetContextList();
                contextList.Update(context);
                Debug.Log($"Added {path} to the context");
            }
        }
    }

    private async void HandleResponseEnd(AiderResponse response, AiderChatMessage messageEl)
    {
        Debug.Log("Response end");
        // reload assets in case a file was changed
        AssetDatabase.Refresh();

        // save chat to a file
        chatList.SerializeChat();

        var context = Client.GetContextList();
        Debug.Log(context);
        contextList.Update(context);

        // after a delay reload again in case writing the file took some time
        await Task.Delay(1000);
        AssetDatabase.Refresh();
    }

    private void HandleResponse(AiderResponse response)
    {
        var current = chatList.Last();
        if (!current.isUser)
        {
            current.AppendText(response.Content);

            if (response.Last)
            {
                HandleResponseEnd(response, current);
            }
        }
        else
        {
            Debug.LogError("Expected AI response, but got user response");
        }
    }
}