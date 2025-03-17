using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Serializable = System.SerializableAttribute;

[Serializable]
public class AiderChatMessage
{
    [SerializeField]
    private string _message;
    public string Message
    {
        get => _message;
        private set => _message = value;
    }
    [SerializeField]
    public bool isUser;

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

    public AiderChatMessage(VisualElement parent, string message, bool isUser, string placeholder)
    {
        this.parent = parent;
        this.Message = message;
        this.isUser = isUser;
        this.placeholder = placeholder;

        Build(parent);
    }

    public void Build(VisualElement parent)
    {
        this.parent = parent;
        this.container = new VisualElement();
        container.AddToClassList("message-container");
        container.AddToClassList(isUser ? "is-user" : "is-ai");
        parent.Add(container);

        Reparse();
    }

    public void Reparse()
    {
        this.container.Clear();
        Debug.Log($"Reparse: {this.Message}");
        MarkdownParser.Parse(container, this.Message);

        if (!isUser)
        {
            this.copyButton = new Button(() => CopyToClipboard());
            this.copyButton.AddToClassList("copy-button");
            container.Add(copyButton);
        }
    }

    public void SetText(string message, string placeholder = "")
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            label.value = MarkdownParser.ParseString(placeholder);
            return;
        }

        this.Message = message;
        Reparse();
    }

    public void AppendText(string message)
    {
        this.Message += message;
        Reparse();
    }

    public void SetParent(VisualElement parent)
    {
        this.parent = parent;
        parent.Add(container);
    }
}
