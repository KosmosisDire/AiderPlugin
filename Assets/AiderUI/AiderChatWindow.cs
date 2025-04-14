using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Diagnostics; //Added for process management
using System.IO; //Added for file path operation
using System.Threading;
using Debug = UnityEngine.Debug;
using System;


public class AiderChatWindow : EditorWindow
{
    public AiderChatList chatList;
    public AiderContextList contextList;
    public AiderChatHistory chatHistory;
    public AiderConfigWindow configWindow;
    public VisualElement footer;
    public VisualElement header;
    public Button settingsButton;
    public Button historyButton;
    public Button newChatButton;
    public Button sendButton;
    public Label sessionCostLabel;

    public bool HistoryOpen => chatHistory != null && chatHistory.resolvedStyle.display == DisplayStyle.Flex;

    private void OnEnable()
    {
        AiderRunner.EnsureAiderBridgeRunning();
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

        NewChat();

        // history 
        chatHistory = new AiderChatHistory(this);
        root.Add(chatHistory);
        chatHistory.BuildChatList();

        var inspectorSkin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);

        // make a footer with a text input and send button with up arrow icon
        footer = new();
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

        textField.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode == KeyCode.Return && !evt.shiftKey)
            {
                SendChatMessage(textField);
            }
        });

        textField.AddToClassList("chat-input");
        textField.SetPlaceholderText("How can I help you?");
        inputWrapper.Add(textField);

        sendButton = new Button(() =>
        {
            SendChatMessage(textField);
        });

        sendButton.style.scale = new StyleScale(StyleKeyword.Null);
        sendButton.AddToClassList("send-button");
        inputWrapper.Add(sendButton);

        // make context list
        contextList = new AiderContextList();
        contextList.Update(Client.GetContextList());
        footer.Add(contextList);

        configWindow = new AiderConfigWindow();
        root.Add(configWindow);
        root.RegisterCallback<ClickEvent>(evt =>
        {
            if (configWindow.IsOpen && evt.target != configWindow && !configWindow.Contains(evt.target as VisualElement))
            {
                HideConfig();
            }
        });

        VisualElement header = new();
        header.AddToClassList("header");
        root.Insert(0, header);

        // add floating settings button at top left corner of window
        settingsButton = new Button(() =>
        {
            if (configWindow.IsOpen)
            {
                HideConfig();
            }
            else
            {
                ShowConfig();
            }
        });
        settingsButton.tooltip = "Settings";
        settingsButton.AddToClassList("settings-button");
        header.Add(settingsButton);
        // add usage report label
        sessionCostLabel = new Label();
        sessionCostLabel.AddToClassList("session-cost-label");
        //sessionCostLabel.tooltip = "Total session cost";
        header.Add(sessionCostLabel);

        // add floating history button at top left corner of window
        historyButton = new Button();
        historyButton.clickable.clicked += () =>
        {
            if (HistoryOpen)
            {
                ShowChat();
            }
            else
            {
                ShowHistory();
            }
        };

        historyButton.tooltip = "History";
        historyButton.AddToClassList("history-button");
        header.Add(historyButton);

        root.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
        root.RegisterCallback<DragPerformEvent>(OnDragPerform);

        // Add floating add chat button at the top right corner
        newChatButton = new Button(NewChat);
        newChatButton.tooltip = "New Chat";
        newChatButton.AddToClassList("new-chat-button");
        header.Add(newChatButton);

        ShowChat();
    }

    public void ReplaceChat(AiderChatList chat)
    {
        VisualElement root = rootVisualElement;
        int index = root.IndexOf(chatList);
        if (index != -1)
        {
            root.Remove(chatList);
            chatList = null;
        }
        else
        {
            index = 0;
        }

        chatList = chat;
        root.Insert(index, chatList);
    }

    public void ShowChat(bool withFooter = true)
    {
        historyButton?.RemoveFromClassList("button-active");
        if (chatList != null) chatList.style.display = DisplayStyle.Flex;
        if (chatHistory != null) chatHistory.style.display = DisplayStyle.None;
        if (withFooter && footer != null) footer.style.display = DisplayStyle.Flex;
    }

    public void ShowHistory()
    {
        historyButton?.AddToClassList("button-active");
        chatHistory.BuildChatList();
        if (chatList != null) chatList.style.display = DisplayStyle.None;
        if (chatHistory != null) chatHistory.style.display = DisplayStyle.Flex;
        if (footer != null) footer.style.display = DisplayStyle.None;
    }

    public void ShowConfig()
    {
        settingsButton?.AddToClassList("button-active");
        configWindow.Show();
    }

    public void HideConfig()
    {
        settingsButton?.RemoveFromClassList("button-active");
        configWindow.Hide();
    }

    public void NewChat()
    {
        VisualElement root = rootVisualElement;

        // Clears Aider's context
        Client.Reset();
        contextList?.Update(Client.GetContextList());

        int index = 0;
        if (chatList != null)
        {
            index = root.IndexOf(chatList);
            if (index != -1) // -1 means not found
            {
                chatList.RemoveFromHierarchy();
                chatList = null;
            }
            else
            {
                index = 0;
            }
        }

        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss");
        chatList = new AiderChatList(timestamp + "-AiderChat", AiderChatHistory.ChatSavePath);
        root.Insert(index, chatList);

        ShowChat();
    }
    private void SendChatMessage(TextField textField)
    {
        UnityEngine.Debug.Log($"Sending: {textField.value}");
        var req = new AiderRequest(textField.value);
        textField.value = "";


        Client.Send(req);
        chatList.AddMessage(req.Content, true, "Empty Message");
        chatList.AddMessage("", false, "Thinking...");
        Client.AsyncReceive(HandleResponse);

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

    private void HandleResponseEnd(AiderResponse response, AiderChatMessage messageEl)
    {
        Debug.Log("Response end");
        // reload assets in case a file was changed
        AssetDatabase.Refresh();

        // save chat to a file
        chatList.SerializeChat();

        var context = Client.GetContextList();
        Debug.Log(context);
        contextList.Update(context);

        if (!string.IsNullOrEmpty(response.UsageReport))
        {

            var userMsg = chatList[chatList.Count - 2];
            // Updates the chat message label for cost/tokens
            messageEl.SetMessageLabel(response.TokenCountReceived, response.CostMessage);
            userMsg.SetMessageLabel(response.TokenCountSent);
            // Updates the total session cost label
            sessionCostLabel.text = $"Session cost: {response.CostSession}";
        }
    }

    private void HandleResponse(AiderResponse response)
    {
        var current = chatList.Last();
        if (!current.isUser)
        {
            current.AppendText(response.Content);
            chatList.ScrollToBottom();

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