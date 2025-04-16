using System.Collections.Generic;
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
        set 
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                _message = placeholder;
            }
            else
            {
                _message = value;
            }

            Reparse();
        }
    }
    [SerializeField]
    public bool isUser;
    [SerializeField]
    private int _tokens;
    public int Tokens
    {
        get => _tokens;
        set
        {
            _tokens = value;
            SetCostLabel(_tokens, _cost);
        }
    }

    [SerializeField]
    private float _cost;
    public float Cost
    {
        get => _cost;
        set
        {
            _cost = value;
            SetCostLabel(_tokens, _cost);
        }
    }

    public readonly string placeholder; // show placeholder if the message in null or whitespace
    public TextField label;
    public Button copyButton;
    public Label usageLabel;
    public List<AiderUnityCommandBase> commands;

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

    private void SetCostLabel(int tokens, float msgCost = 0)
    {
        static string FormatTokens(int number)
        {
            if (number < 1000)
            {
                return number.ToString("0");
            }
            else if (number < 1000000)
            {
                return (number / 1000.0).ToString("0.#k");
            }
            else
            {
                return (number / 1000000.0).ToString("0.#m");
            }
        }

        var content = FormatTokens(tokens) + " tokens";
        usageLabel ??= new Label();
        usageLabel.AddToClassList("tokens-label");
        this.Add(usageLabel);

        if (!isUser)
        {
            content += " • " + msgCost.ToString("C2"); // format as currency
            usageLabel.tooltip = "Tokens received and message cost";
        }
        else
        {
            usageLabel.tooltip = "Tokens sent including context";
        }

        usageLabel.text = content;
    }

    public AiderChatMessage(string message, bool isUser, string placeholder, int tokens, float cost)
    {
        AddToClassList("message-container");
        AddToClassList(isUser ? "is-user" : "is-ai");
        this.placeholder = placeholder;
        this.Message = message;
        this.isUser = isUser;
        this.Tokens = tokens;
        this.Cost = cost;

        Reparse();
    }

    public void Reparse()
    {
        this.Clear();
        commands = UnityJsonCommandParser.ParseCommands(this.Message);
        MarkdownParser.Parse(this, this.Message, commands);

        if (!isUser)
        {
            this.copyButton = new Button(() => CopyToClipboard());
            this.copyButton.AddToClassList("copy-button");
            this.Add(copyButton);
        }
    }
}
