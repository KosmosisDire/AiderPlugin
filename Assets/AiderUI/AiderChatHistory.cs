

using System;
using System.Collections.Generic;
using System.IO;
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
    private AiderChatWindow chatSession;

    public AiderChatHistory(AiderChatWindow chatSession)
    {
        this.chatSession = chatSession;
        AddToClassList("history-container");
        availableChats = new List<ChatMetadata>();
    }


    public void LoadChats()
    {
        availableChats.Clear();

        if (!System.IO.Directory.Exists(ChatSavePath))
        {
            System.IO.Directory.CreateDirectory(ChatSavePath);
            return;
        }

        var chatFiles = System.IO.Directory.GetFiles(ChatSavePath, "*.json");

        foreach (var chatFile in chatFiles)
        {
            var chat = new ChatMetadata();
            var json = System.IO.File.ReadAllText(chatFile);

            var dataTemp = JsonUtility.FromJson<AiderChatList>(json);
            
            chat.title = dataTemp.chatTitle;
            chat.filename = dataTemp.chatID;
            chat.lastMessageTime = dataTemp.LastMessageTime;

            availableChats.Add(chat);
        }
    }

    private void UpdateEmpty()
    {
        if (availableChats.Count == 0)
        {
            AddToClassList("empty-container");
            var emptyContainer = new VisualElement();
            emptyContainer.AddToClassList("empty-content");
            Add(emptyContainer);

            var emptyLabel = new Label("No Chat History");
            emptyContainer.Add(emptyLabel);

            var emptyIcon = new VisualElement();
            emptyIcon.AddToClassList("empty-icon");
            emptyContainer.Add(emptyIcon);
            

            var newChatButton = new Button(() =>
            {
                chatSession.NewChat();
            })
            {
                text = "New Chat"
            };
            emptyContainer.Add(newChatButton);
        }
        else
        {
            RemoveFromClassList("empty-container");
        }
    }

    public string GetTimeDelta(DateTime lastMessageTime)
    {
        var timeDelta = DateTime.Now - lastMessageTime;
        if (DateTime.Now.Year != lastMessageTime.Year)
        {
            return lastMessageTime.ToString("MMMM dd, yyyy");
        }
        if (timeDelta.TotalDays > 30)
        {
            return lastMessageTime.ToString("MMMM dd");
        }
        if (timeDelta.TotalDays > 1)
        {
            return $"{(int)timeDelta.TotalDays} days ago";
        }
        else if (timeDelta.TotalHours > 1)
        {
            return $"{(int)timeDelta.TotalHours} hours ago";
        }
        else if (timeDelta.TotalMinutes > 1)
        {
            return $"{(int)timeDelta.TotalMinutes} minutes ago";
        }
        else
        {
            return "Just now";
        }
    }

    public void BuildChatList()
    {
        LoadChats();
        Clear();

        UpdateEmpty();

        availableChats.Sort((a, b) => b.lastMessageTime.CompareTo(a.lastMessageTime));

        foreach (var chat in availableChats)
        {
            Debug.Log($"Chat: {chat.title}");
            var chatCard = new VisualElement();
            chatCard.RegisterCallback<MouseUpEvent>((evt) =>
            {
                var chatList = chat.LoadChat(ChatSavePath);
                chatSession.ReplaceChat(chatList);
                chatSession.ShowChat(false);
            });
            chatCard.AddToClassList("history-card");
            chatCard.AddToClassList("hoverable-card");

            var titleContainer = new VisualElement();
            titleContainer.AddToClassList("history-title-container");
            chatCard.Add(titleContainer);
            
            var label = new Label(chat.title);
            label.AddToClassList("history-title");
            titleContainer.Add(label);

            var dateLabel = new Label(GetTimeDelta(chat.lastMessageTime));
            dateLabel.AddToClassList("history-date");
            titleContainer.Add(dateLabel);

            var trashButton = new Button(() =>
            {
                Debug.Log($"Deleting chat: {chat.title}");
                File.Delete(Path.Combine(ChatSavePath, chat.filename + ".json"));
                BuildChatList();
            });
            trashButton.AddToClassList("trash-button");
            // when we hover the trash make sure we ignore hover effect on card (hacky because of unity's event system)
            trashButton.RegisterCallback<MouseEnterEvent>(evt => chatCard.RemoveFromClassList("hoverable-card"));
            trashButton.RegisterCallback<MouseLeaveEvent>(evt => chatCard.AddToClassList("hoverable-card"));
            trashButton.tooltip = "Delete Chat";
            chatCard.Add(trashButton);

            Add(chatCard);
        }   
    }

    
}