using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public class ChatMessage
{
    public string Message { get; private set; }
    public readonly bool isUser;
    public readonly string placeholder; // show placeholder if the message in null or whitespace
    public TextField label; 
    public VisualElement parent;
    public VisualElement container;
    public Button copyButton;

    async void CopyToClipboard(bool showConfirm = true)
    {
        GUIUtility.systemCopyBuffer = this.Message;
        if (showConfirm)
        {
            copyButton.AddToClassList("confirm");
            await Task.Delay(1000);
            copyButton.RemoveFromClassList("confirm");
        }
    }

    public ChatMessage(VisualElement parent, string message, bool isUser, string placeholder)
    {
        this.parent = parent;
        this.container = new VisualElement();
        container.AddToClassList("message-container");
        container.AddToClassList(isUser ? "is-user" : "is-ai");
        parent.Add(container);

        this.label = new()
        {
            isReadOnly = true
        };
        container.Add(label);

        if (!isUser)
        {
            this.copyButton = new Button(() => CopyToClipboard());
            this.copyButton.AddToClassList("copy-button");
            container.Add(copyButton);
        }

        SetText(message, placeholder);
    }

    public void SetText(string message, string placeholder = "")
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            label.value = placeholder;
            return;
        }

        this.Message = message;
        label.value = this.Message;
    }

    public void AppendText(string message)
    {
        this.Message += message;
        label.value = this.Message;
    }
}
