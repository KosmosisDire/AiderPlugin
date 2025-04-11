using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Serializable = System.SerializableAttribute;

[Serializable]
public class AiderChatMessage : VisualElement
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
    public Button copyButton;
    public Label usageLabel;

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

    public void SetCostLabel(int tokens, float msgCost = 0)
    {
        var content = tokens + " tokens";
        usageLabel = new Label();
        usageLabel.AddToClassList("tokens-label");
        this.Add(usageLabel);

        if (!isUser)
        {
            content += " • " + msgCost;
            usageLabel.tooltip = "Tokens received and message cost";
        }
        else
        {
            usageLabel.tooltip = "Tokens sent including context";
        }

        usageLabel.text = content;
    }

    public AiderChatMessage(string message, bool isUser, string placeholder)
    {
        AddToClassList("message-container");
        AddToClassList(isUser ? "is-user" : "is-ai");
        this.Message = message;
        this.isUser = isUser;
        this.placeholder = placeholder;

        Reparse();
    }

    public void Reparse()
    {
        this.Clear();
        MarkdownParser.Parse(this, this.Message);

        if (!isUser)
        {
            this.copyButton = new Button(() => CopyToClipboard());
            this.copyButton.AddToClassList("copy-button");
            this.Add(copyButton);
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
}
