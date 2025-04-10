using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
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

public struct AiderResponseHeader
{
    public static readonly int HeaderSize = 4 + 4 + 1 + 1 + 1; // headerMarker + contentLength + last + isDiff + isError
    public int ContentLength { get; set; }
    public bool IsLast { get; set; }
    public bool IsDiff { get; set; }
    public bool IsError { get; set; }

    public AiderResponseHeader(int contentLength, bool last, bool isDiff = true, bool isError = false)
    {
        ContentLength = contentLength;
        IsLast = last;
        IsDiff = isDiff;
        IsError = isError;
    }

    public static AiderResponseHeader Deserialize(byte[] data)
    {
        int pos = 0;
        var headerMarker = BitConverter.ToInt32(data, pos); pos += 4;
        if (headerMarker != 123456789)
        {
            throw new Exception($"Invalid header marker: {headerMarker}. Expected 123456789.");
        }

        var contentLength = BitConverter.ToInt32(data, pos); pos += 4;
        var last = BitConverter.ToBoolean(data, pos); pos += 1;
        var isDiff = BitConverter.ToBoolean(data, pos); pos += 1;
        var isError = BitConverter.ToBoolean(data, pos); pos += 1;

        return new AiderResponseHeader(contentLength, last, isDiff, isError);
    }
}

public struct AiderResponse
{
    public AiderResponseHeader Header { get; set; }
    public string Content { get; set; }

    public List<IAiderUnityCommand> Commands { get; private set; }

    public bool HasFileChanges => Regex.IsMatch(Content, @"<<<<<<< SEARCH[\n\r]=======[\n\r]([\s\S]+)[\n\r]>>>>>>> REPLACE", RegexOptions.Multiline | RegexOptions.IgnoreCase);

    public AiderResponse(string content, AiderResponseHeader header)
    {
        Content = content;
        Header = header;
        Commands = UnityJsonCommandParser.ParseCommands(content);

        if (header.IsError)
        {
            Debug.LogError(content);
        }
    }

    public static AiderResponse Deserialize(byte[] data, AiderResponseHeader header)
    {
        int pos = 0;
        var contentLength = header.ContentLength;
        var content = System.Text.Encoding.UTF8.GetString(data, pos, contentLength); pos += contentLength;

        return new AiderResponse(content, header);
    }

    public static AiderResponse Error(string content)
    {
        return new AiderResponse(content, new AiderResponseHeader{ IsError = true });
    }
}

    