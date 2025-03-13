using System.Collections.Generic;
using UnityEngine;
using Serializable = System.SerializableAttribute;

[Serializable]
public class AiderChatList : IEnumerable<AiderChatMessage>
{
    [SerializeField]
    private List<AiderChatMessage> chatList = new();
    [SerializeField]
    public string chatName;

    private string SavePath => $"Assets/AiderUI/{chatName}.json";

    public AiderChatList(string chatName)
    {
        chatList = new List<AiderChatMessage>();
        this.chatName = chatName;
        LoadMessages();
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
        JsonUtility.FromJsonOverwrite(json, this);

    }

    public void AddMessage(AiderChatMessage message)
    {
        chatList.Add(message);
        SerializeChat();
    }

    public void LoadMessages()
    {
        DeserializeChat();
    }

    public AiderChatMessage this[int index]
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