using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;


class UnityJsonCommandParser
{
    public static string FixMultilineStrings(string jsonString)
    {
        if (string.IsNullOrEmpty(jsonString))
            return jsonString;

        var sb = new StringBuilder();
        bool inString = false;
        bool escaped = false;
        
        for (int i = 0; i < jsonString.Length; i++)
        {
            char c = jsonString[i];
            char? nextChar = i < jsonString.Length - 1 ? jsonString[i + 1] : (char?)null;
            
            // Handle string boundaries
            if (c == '"' && !escaped)
            {
                inString = !inString;
                sb.Append(c);
                continue;
            }
            
            // Handle escaping
            if (c == '\\' && !escaped)
            {
                escaped = true;
                sb.Append(c);
                continue;
            }
            
            // Inside a string
            if (inString)
            {
                if (c == '\r' && nextChar == '\n')
                {
                    sb.Append("\\n");
                    i++;
                }
                else if (c == '\n')
                {
                    sb.Append("\\n");
                }
                else if (c == '\r')
                {
                    sb.Append("\\n");
                }
                else if (c == '\t')
                {
                    sb.Append("\\t");
                }
                else
                {
                    sb.Append(c);
                }
                
                escaped = false;
            }
            // Outside a string
            else
            {
                sb.Append(c);
                escaped = false;
            }
        }
        
        return sb.ToString();
    }
    
    public static List<AiderUnityCommandBase> ParseCommands(string messageContents)
    {
        // find all ```unity code blocks and their contents
        var regex = new Regex(@"```unity\n([\s\S]*?)```", RegexOptions.Multiline);
        var matches = regex.Matches(messageContents);
        if (matches.Count == 0)
        {
            return new();
        }

        List<AiderUnityCommandBase> commands = new();
        foreach (Match match in matches)
        {
            try
            {
                var commandBlock = match.Groups[1].Value;
                var commandTypeRegex = new Regex(@"""command"":\s?""(.+?)""", RegexOptions.Multiline);
                var commandTypeMatch = commandTypeRegex.Match(commandBlock);
                if (commandTypeMatch.Success)
                {
                    var commandType = commandTypeMatch.Groups[1].Value;
                    AiderUnityCommandBase command = null;
                    // Debug.Log($"Command Type: {commandType}");

                    commandBlock = FixMultilineStrings(commandBlock);
                    // Debug.Log($"Command Block: {commandBlock}");

                    switch (commandType)
                    {
                        case "addObject":
                            command = JsonUtility.FromJson<AddObjectCommand>(commandBlock);
                            break;
                        case "addObjectMenu":
                            command = JsonUtility.FromJson<AddObjectMenuCommand>(commandBlock);
                            break;
                        case "addComponent":
                            command = JsonUtility.FromJson<AddComponentCommand>(commandBlock);
                            break;
                        case "executeCode":
                            command = JsonUtility.FromJson<ExecuteCodeCommand>(commandBlock);
                            break;
                        case "setComponentProperty":
                            var setPropCommand = JsonUtility.FromJson<SetComponentPropertyCommand>(commandBlock);
                            var valueString = Regex.Match(commandBlock, @"""value""\s*:\s*((?:\{[^{}]*\})|""[^""]*"")").Groups[1].Value.Trim();
                            valueString = valueString.Trim('"');
                            setPropCommand.value = valueString;
                            command = setPropCommand;
                            break;
                        case "deleteObject":
                            command = JsonUtility.FromJson<DeleteObjectCommand>(commandBlock);
                            break;
                        case "createPrefab":
                            command = JsonUtility.FromJson<CreatePrefabCommand>(commandBlock);
                            break;
                        case "instantiatePrefab":
                            command = JsonUtility.FromJson<InstantiatePrefabCommand>(commandBlock);
                            break;
                        case "setParent":
                            command = JsonUtility.FromJson<SetParentCommand>(commandBlock);
                            break;
                    }
 
                    if (command != null)
                    {
                        command.sourceJson = commandBlock;
                        commands.Add(command);
                    }
                }
            }
            catch (Exception e)
            {
                commands.Add(new InvalidCommand($"Error parsing command block: {e.Message}"));
            }
        }

        return commands;
    }

    public static System.Type FindType(string typeName, bool useFullName = false, bool ignoreCase = false)
    {
        if (string.IsNullOrEmpty(typeName)) return null;

        StringComparison e = (ignoreCase) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (useFullName)
        {
            foreach (var assemb in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var t in assemb.GetTypes())
                {
                    if (string.Equals(t.FullName, typeName, e)) return t;
                }
            }
        }
        else
        {
            foreach (var assemb in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var t in assemb.GetTypes())
                {
                    if (string.Equals(t.Name, typeName, e) || string.Equals(t.FullName, typeName, e)) return t;
                }
            }
        }
        return null;
    }
}




