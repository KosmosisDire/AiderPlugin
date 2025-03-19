using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Serializable = System.SerializableAttribute;

[Serializable]
public class AiderChatList : ScrollView, IEnumerable<AiderChatMessage>
{
    [SerializeField]
    private List<AiderChatMessage> chatList = new();
    [SerializeField]
    public string chatName;

    private string SavePath => $"Assets/AiderUI/{chatName}.json";

    public AiderChatList(string chatName)
    {
        AddToClassList("chat-container");
        chatList = new List<AiderChatMessage>();
        this.chatName = chatName;
        DeserializeChat();
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
            AddMessage(chatTemp.Message, chatTemp.isUser, chatTemp.placeholder);
        }
    }

    public void ScrollToBottom()
    {
        verticalScroller.value = verticalScroller.highValue > 0 ? verticalScroller.highValue : 0;
    }

    public async void AddMessage(string content, bool isUser, string placeholder)
    {
        var msg = new AiderChatMessage(content, isUser, placeholder);
        this.Add(msg);
        chatList.Add(msg);
        SerializeChat();
        await Task.Delay(100);
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