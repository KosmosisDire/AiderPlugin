using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

[SerializableAttribute]
public class AiderChatList : ScrollView, IEnumerable<AiderChatMessage>
{
    [SerializeField] private List<AiderChatMessage> chatList = new();
    [SerializeField] public string chatID;
    [SerializeField] public string chatTitle;
    [SerializeField] private string lastMessageTimeStr;

    public DateTime LastMessageTime
    {
        get => DateTime.TryParse(lastMessageTimeStr, out var time) ? time : DateTime.UnixEpoch;
        set => lastMessageTimeStr = value.ToString("o");
    }

    private string chatSaveFolder;
    public string SavePath => $"{chatSaveFolder}/{chatID}.json";
 
    private VisualElement emptyContainer;

    public AiderChatList(string chatID, string chatSaveFolder)
    {
        AddToClassList("chat-container");
        AddToClassList("empty-container");
        chatList = new List<AiderChatMessage>();
        this.chatID = chatID;
        this.chatSaveFolder = chatSaveFolder;
        DeserializeChat();
        UpdateEmpty();
    }

    public void SerializeChat()
    {
        var json = JsonUtility.ToJson(this);
        System.IO.File.WriteAllText(SavePath, json);
    }

    public void DeserializeChat()
    {
        if (!System.IO.File.Exists(SavePath))
        {
            return;
        }

        var json = System.IO.File.ReadAllText(SavePath);
        var temp = JsonUtility.FromJson<AiderChatList>(json);

        for (int i = 0; i < temp.chatList.Count; i++)
        {
            var chatTemp = temp.chatList[i];
            var msg = new AiderChatMessage(chatTemp.Message, chatTemp.isUser, chatTemp.placeholder);
            this.Add(msg);
            chatList.Add(msg);
        }
    }

    public void ScrollToBottom()
    {
        verticalScroller.value = verticalScroller.highValue > 0 ? verticalScroller.highValue : 0;
    }

    private void UpdateEmpty()
    {
        if (chatList.Count == 0)
        {
            if (emptyContainer == null)
            {
                emptyContainer = new VisualElement();
                emptyContainer.AddToClassList("empty-content");

                var emptyLogo = new VisualElement();
                emptyLogo.AddToClassList("empty-icon");
                emptyContainer.Add(emptyLogo);

                var label = new Label("What changes would you like to make?");
                emptyContainer.Add(label);

                Add(emptyContainer);
            }
        }
        else
        {
            emptyContainer?.RemoveFromHierarchy();
            emptyContainer = null;
            RemoveFromClassList("empty-container");
        }
    }

    public async void AddMessage(string content, bool isUser, string placeholder)
    {
        if (chatList.Count == 0)
        {
            chatTitle = content.Split('\n')[0].Trim();
            chatTitle = chatTitle[..Mathf.Min(20, chatTitle.Length)];
        }

        var msg = new AiderChatMessage(content, isUser, placeholder);
        this.Add(msg);
        chatList.Add(msg);
        UpdateEmpty();
        LastMessageTime = DateTime.Now;

        SerializeChat();

        await Task.Delay(1);
        ScrollToBottom();
    }

    public new AiderChatMessage this[int index]
    {
        get => chatList[index];
    }

    public int Count => chatList.Count;

    public IEnumerator<AiderChatMessage> GetEnumerator()
    {
        return chatList.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}