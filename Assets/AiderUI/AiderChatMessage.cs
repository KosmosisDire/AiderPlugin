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
    public Label aiderUsageLabel;
    public Label userUsageLabel;

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

    public void SetMessageLabel(string tokensSent, string tokensRcv, string msgCost)
    {
        if (isUser)
        { var userUsageLabel = new Label($"{tokensSent} tokens");
          userUsageLabel.AddToClassList("user-label"); 
          this.Add(userUsageLabel);
        }
        else
        { var aiderUsageLabel = new Label($"{tokensRcv} tokens • {msgCost}");
          aiderUsageLabel.AddToClassList("aider-label");
          this.Add(aiderUsageLabel);
        }
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
