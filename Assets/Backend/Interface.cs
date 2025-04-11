using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.VisualScripting;
using UnityEngine;

public enum AiderCommand
{
    None = -1,
    Add = 0,
    Architect = 1,
    Ask = 2,
    ChatMode = 3,
    Clear = 4,
    Code = 5,
    Commit = 6,
    Drop = 7,
    Lint = 8,
    Load = 9,
    Ls = 10,
    Map = 11,
    MapRefresh = 12,
    ReadOnly = 13,
    Reset = 14,
    Undo = 15,
    Web = 16
}

public static class AiderCommandHelper
{
    public static readonly IReadOnlyDictionary<AiderCommand, string> CommandDescriptions = 
        new ReadOnlyDictionary<AiderCommand, string>(
            new Dictionary<AiderCommand, string>
            {
                { AiderCommand.Add, "Add files to the chat so aider can edit them or review them in detail" },
                { AiderCommand.Architect, "Enter architect/editor mode using 2 different models. If no prompt provided, switches to architect/editor mode." },
                { AiderCommand.Ask, "Ask questions about the code base without editing any files. If no prompt provided, switches to ask mode." },
                { AiderCommand.ChatMode, "Switch to a new chat mode" },
                { AiderCommand.Clear, "Clear the chat history" },
                { AiderCommand.Code, "Ask for changes to your code. If no prompt provided, switches to code mode." },
                { AiderCommand.Commit, "Commit edits to the repo made outside the chat (commit message optional)" },
                { AiderCommand.Drop, "Remove files from the chat session to free up context space" },
                { AiderCommand.Lint, "Lint and fix in-chat files or all dirty files if none in chat" },
                { AiderCommand.Load, "Load and execute commands from a file" },
                { AiderCommand.Ls, "List all known files and indicate which are included in the chat session" },
                { AiderCommand.Map, "Print out the current repository map" },
                { AiderCommand.MapRefresh, "Force a refresh of the repository map" },
                { AiderCommand.ReadOnly, "Add files to the chat that are for reference only, or turn added files to read-only" },
                { AiderCommand.Reset, "Drop all files and clear the chat history" },
                { AiderCommand.Undo, "Undo the last git commit if it was done by aider" },
                { AiderCommand.Web, "Scrape a webpage, convert to markdown and send in a message" }
            });

    static string[] SplitCamelCase(this string source)
    {
        return Regex.Split(source, @"(?<!^)(?=[A-Z])");
    }

    public static string GetCommandString(this AiderCommand command)
    {
        return string.Join("-", command.ToString().SplitCamelCase()).ToLower();
    }

    public static string GetCommandDescription(this AiderCommand command)
    {
        return CommandDescriptions[command];
    }

    public static AiderCommand ParseCommand(string commandStr)
    {
        var cleanCommand = commandStr.Trim().ToLower();
        if (cleanCommand.StartsWith("/"))
        {
            cleanCommand = cleanCommand.Substring(1);
        }

        if (string.IsNullOrWhiteSpace(cleanCommand))
        {
            return AiderCommand.None;
        }

        cleanCommand = cleanCommand.Split(' ')[0];

        return Enum.TryParse<AiderCommand>(cleanCommand, true, out var command) ? command : AiderCommand.None;
    }
}

public struct AiderRequest
{
    public string Content { get; set; }

    public AiderRequest(AiderCommand command, string content)
    {
        if (command == AiderCommand.None)
        {
            Content = content;
            return;
        }

        Content = $"/{command.GetCommandString()} {content}";
    }

    public AiderRequest(string content)
    {
        Content = content;
    }

    // see interface.py for the deserialization function
    public readonly byte[] Serialize()
    {
        var byteList = new List<byte>();
        byteList.AddRange(BitConverter.GetBytes(Content.Length));
        byteList.AddRange(System.Text.Encoding.UTF8.GetBytes(Content));
        return byteList.ToArray();
    }
}

public struct AiderResponse
{
    public string Content { get; set; }
    public bool Last { get; set; }
    public bool IsError { get; set; }
    public string UsageReport { get; set; }
    private string[] ParsedDataGroups => Regex.Match(UsageReport, @"^.+? +(\d.+?) .+?(\d.+?) .+?(\p{Sc}[\d\.\,]+?) .+?(\p{Sc}[\d\.\,]+?) .+").Groups.Select(g => g.Value).ToArray();
    public string TokenCountSent => ParsedDataGroups.Length > 1 ? ParsedDataGroups[1] : "0";
    public string TokenCountReceived => ParsedDataGroups.Length > 2 ? ParsedDataGroups[2] : "0";
    public string CostMessage => ParsedDataGroups.Length > 3 ? ParsedDataGroups[3] : "0";
    public string CostSession => ParsedDataGroups.Length > 4 ? ParsedDataGroups[4] : "0";

    public AiderResponse(string content, bool last, bool isError, string usageReport)
    {
        Content = content;
        Last = last;
        IsError = isError;
        UsageReport = usageReport;

        if (isError)
        {
            Debug.LogError(content);
        }
    }

    public static AiderResponse Deserialize(byte[] data)
    {
        int pos = 0;
        var contentLength = BitConverter.ToInt32(data, pos); pos += 4;
        var content = System.Text.Encoding.UTF8.GetString(data, pos, contentLength); pos += contentLength;
        var last = BitConverter.ToBoolean(data, pos); pos += 1;
        var error = BitConverter.ToBoolean(data, pos); pos += 1;
        int usageReportLength = BitConverter.ToInt32(data, pos); pos += 4;
        string usageReport = System.Text.Encoding.UTF8.GetString(data, pos, usageReportLength); // pos += usageReportLength;
        return new AiderResponse(content, last, error, usageReport);
    }

    public static AiderResponse Error(string content)
    {
        return new AiderResponse(content, true, true, string.Empty);
    }
}

    