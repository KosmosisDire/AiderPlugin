

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public struct ChatMetadata
{
    public string title;
    public string filename;
    public DateTime lastMessageTime;

    public readonly AiderChatList LoadChat(string fromFolderPath)
    {
        return new AiderChatList(filename, fromFolderPath);
    }
}

public class AiderChatHistory : ScrollView
{
    public List<ChatMetadata> availableChats;
    public static string ChatSavePath => "Assets/AiderUI/chats";

    public void LoadChats()
    {
        availableChats.Clear();

        var chatFiles = System.IO.Directory.GetFiles(ChatSavePath, "*.json");
        foreach (var chatFile in chatFiles)
        {
            var chat = new ChatMetadata();
            var json = System.IO.File.ReadAllText(chatFile);

            var dataTemp = JsonUtility.FromJson<AiderChatList>(json);
            
            chat.title = dataTemp.chatTitle;
            chat.filename = dataTemp.chatID;
            // chat.lastMessageTime = dataTemp.LastMessageTime;
        }
    }

    public void BuildChatList()
    {
        LoadChats();
        Clear();

        foreach (var chat in availableChats)
        {
            var chatCard = new Button(() =>
            {
                var chatList = chat.LoadChat(ChatSavePath);
                parent.Add(chatList);
            });
            chatCard.AddToClassList("chat-history-card");
            chatCard.text = chat.title;
            Add(chatCard);
        }   
    }

    public AiderChatHistory()
    {
        AddToClassList("chat-history");
        availableChats = new List<ChatMetadata>();
        
    }
}