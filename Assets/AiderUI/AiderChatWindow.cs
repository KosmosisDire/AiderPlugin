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
    public TextField textField;
    public Button sendButton;
    public Label sessionCostLabel;


    public bool HistoryOpen => chatHistory != null && chatHistory.resolvedStyle.display == DisplayStyle.Flex;

    private async void OnEnable()
    {
        // await Client.ConnectToBridge();
    }

    [MenuItem("Aider/Chat Window")]
    public static void ShowWindow()
    {
        GetWindow<AiderChatWindow>("Aider");
    }


    private async Task CreateGUI()
    {
        var currentChat = EditorPrefs.GetString("Aider-CurrentChat", "");
        if (currentChat != "")
        {
            Debug.Log($"Loading chat {currentChat}");
            chatList = new AiderChatList(currentChat, AiderChatHistory.ChatSavePath);
            
            if (EditorPrefs.GetBool("Aider-ExecuteOnLoad", false))
            {
                var commands = UnityJsonCommandParser.ParseCommands(chatList.chatList.Last().Message);

                // wait for the editor to be idle
                while (EditorApplication.isCompiling || EditorApplication.isUpdating)
                {
                    await Task.Delay(100);
                }
                await Task.Delay(1000);

                foreach (var command in commands)
                {
                    command.Execute();
                }

                EditorPrefs.SetBool("Aider-ExecuteOnLoad", false);
            }
        }

        await Client.ConnectToBridge();

        VisualElement root = rootVisualElement;
        root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/AiderWindow.uss"));
        root.AddToClassList(EditorGUIUtility.isProSkin ? "dark-mode" : "light-mode");
        root.AddToClassList("aider-chat-window");
        if (chatList != null) root.Add(chatList);
        
        if (currentChat == "")
        {
            await NewChat();
        }

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
        textField = new()
        {
            multiline = true,
        };
        textField.AddToClassList("chat-input");
        textField.SetPlaceholderText("How can I help you?");
        inputWrapper.Add(textField);

        sendButton = new Button(async () =>
        {
            await SendCurrentMessage();
        });
        sendButton.style.scale = new StyleScale(StyleKeyword.Null);
        sendButton.AddToClassList("send-button");
        inputWrapper.Add(sendButton);

        // make context list
        contextList = new AiderContextList();
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
        root.RegisterCallback<DragPerformEvent>(async (evt) => await OnDragPerform(evt));
        
        // Add floating add chat button at the top right corner
        newChatButton = new Button(async () => await NewChat());
        newChatButton.tooltip = "New Chat";
        newChatButton.AddToClassList("new-chat-button");
        header.Add(newChatButton);

        ReplaceChat(chatList);
        ShowChat();

        contextList.Update(await Client.GetContextList());

    }

    public async Task SendCurrentMessage()
    {
        DisableSendButton();
        var req = new AiderRequest(textField.value);
        textField.value = "";
        await Client.Send(req);
        chatList.AddMessage(req.Content, true, "Empty Message");
        chatList.AddMessage("", false, "Thinking...");
        _ = Client.ReceiveAllResponesAsync(HandleResponse);
    }

    public void DisableSendButton()
    {
        // sendButton.SetEnabled(false);
    }

    public void EnableSendButton()
    {
        // sendButton.SetEnabled(true);
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

    public async Task NewChat()
    {
        VisualElement root = rootVisualElement;

        // Clears Aider's context
        await Client.Reset();
        contextList?.Update(await Client.GetContextList());

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

    private void OnDragUpdated(DragUpdatedEvent evt)
    {
        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
    }

    private async Task OnDragPerform(DragPerformEvent evt)
    {
        DragAndDrop.AcceptDrag();

        foreach (var path in DragAndDrop.paths)
        {
            if (File.Exists(path))
            {
                await Client.AddFile(path);
                var context = await Client.GetContextList();
                contextList.Update(context);
                Debug.Log($"Added {path} to the context");
            }
        }
    }

    private async Task HandleResponseEnd(AiderResponse response, AiderChatMessage messageEl)
    {
        await Task.Delay(1000);
        
        // save chat to a file
        chatList.SerializeChat();
        EditorPrefs.SetString("Aider-CurrentChat", chatList.chatID);

        var context = await Client.GetContextList();
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

        if (response.HasFileChanges)
        {
            EditorPrefs.SetBool("Aider-ExecuteOnLoad", true);
            AssetDatabase.Refresh();
        }
        else
        {
            foreach (var command in response.Commands)
            {
                command.Execute();
            }
        }
    }

    private void HandleResponse(AiderResponse response)
    {
        var current = chatList.Last();
        if (!current.isUser)
        {
            if (response.Header.IsError)
            {
                current.SetText(response.Content);
                current.AddToClassList("error-message");
                DisableSendButton();
                return;
            }

            if (response.Header.IsDiff) current.AppendText(response.Content);
            else current.SetText(response.Content);

            chatList.ScrollToBottom();

            if (response.Header.IsLast)
            {
                _ = HandleResponseEnd(response, current);
            }
        }
        else
        {
            Debug.LogError("Expected AI response, but got user response");
        }
    }
}