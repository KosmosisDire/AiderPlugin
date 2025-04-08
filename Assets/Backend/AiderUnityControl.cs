

// stuff allowing Aider to directly control Unity


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public interface IAiderUnityCommand
{
    void Execute();
}

class AiderUnityControl
{
    public static List<IAiderUnityCommand> ParseCommands(string messageContents)
    {
        // find all ```unity code blocks and their contents
        var regex = new Regex(@"```unity\n([\s\S]*?)```", RegexOptions.Multiline);
        var matches = regex.Matches(messageContents);
        if (matches.Count == 0)
        {
            return new();
        }

        List<IAiderUnityCommand> commands = new();
        foreach (Match match in matches)
        {
            var commandBlock = match.Groups[1].Value;
            var commandTypeRegex = new Regex(@"""command"":\s?""(.+?)""", RegexOptions.Multiline);
            var commandTypeMatch = commandTypeRegex.Match(commandBlock);
            if (commandTypeMatch.Success)
            {
                var commandType = commandTypeMatch.Groups[1].Value;
                IAiderUnityCommand command = null;
                Debug.Log($"Command Type: {commandType}");

                switch (commandType)
                {
                    case "addObject":
                        command = JsonUtility.FromJson<AddObjectCommand>(commandBlock) as IAiderUnityCommand;
                        break;
                    case "executeCode":
                        command = JsonUtility.FromJson<ExecuteCodeCommand>(commandBlock) as IAiderUnityCommand;
                        break;
                }

                if (command != null)
                {
                    commands.Add(command);
                }
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

// Add Object Command
[Serializable]
public struct AddObjectCommand : IAiderUnityCommand
{
    public string command;
    public string objectPath;
    public string objectType;
    public float[] position;
    public float[] rotation;
    public float[] scale;
    public string tag;
    public string layer;

    public AddObjectCommand(
        string objectPath, 
        string objectType, 
        Vector3 position, 
        Vector3 rotation, 
        Vector3 scale,
        string tag = null,
        string layer = null)
    {
        this.command = "addObject";
        this.objectPath = objectPath;
        this.objectType = objectType;
        this.position = new float[] { position.x, position.y, position.z };
        this.rotation = new float[] { rotation.x, rotation.y, rotation.z };
        this.scale = new float[] { scale.x, scale.y, scale.z };
        this.tag = tag;
        this.layer = layer;
    }

    public void Execute()
    {
        Debug.Log($"Executing AddObjectCommand: {objectPath}");
        GameObject newObject = new GameObject("TEMP_OBJECT");

        // Set the object path in the hierarchy
        if (objectPath.LastIndexOf('/') > 0)
        {
            var parentPath = objectPath.Substring(0, objectPath.LastIndexOf('/'));
            var parentObject = GameObject.Find(parentPath);
            if (parentObject != null)
            {
                newObject.transform.SetParent(parentObject.transform, true);
            }
            else
            {
                Debug.LogWarning($"Parent object '{parentPath}' not found. Object will be created at root level.");
            }
        }

        // set local coordinates
        newObject.transform.localPosition = new Vector3(position[0], position[1], position[2]);
        newObject.transform.localRotation = Quaternion.Euler(rotation[0], rotation[1], rotation[2]);
        newObject.transform.localScale = new Vector3(scale[0], scale[1], scale[2]);

        newObject.name = objectPath.Substring(objectPath.LastIndexOf('/') + 1);
        newObject.transform.SetAsLastSibling(); // Set the new object as the last sibling in the hierarchy

        // get type from type string
        if (objectType != "GameObject")
        {
            Type type = AiderUnityControl.FindType(objectType, false, true);
            Debug.Log($"Adding Type: {type}");
            if (type != null)
            {
                // Add the component to the new object
                var component = newObject.AddComponent(type);
                if (component == null)
                {
                    Debug.LogWarning($"Failed to add component of type '{objectType}' to '{objectPath}'.");
                }
            }
            else
            {
                Debug.LogWarning($"Type '{objectType}' not found. Component not added to '{objectPath}'.");
            }
        }

        // Set the tag and layer if provided
        if (!string.IsNullOrEmpty(tag))
        {
            newObject.tag = tag;
        }

        if (!string.IsNullOrEmpty(layer))
        {
            int layerIndex = LayerMask.NameToLayer(layer);
            if (layerIndex != -1)
            {
                newObject.layer = layerIndex;
            }
            else
            {
                Debug.LogWarning($"Layer '{layer}' not found. Layer not set for '{objectPath}'.");
            }
        }
    }
}

// Execute Code Command
[Serializable]
public struct ExecuteCodeCommand : IAiderUnityCommand
{
    public string command;
    public string shortDescription;
    public string code;

    public ExecuteCodeCommand(string code, string shortDescription = null)
    {
        this.command = "executeCode";
        this.shortDescription = shortDescription;
        this.code = code;
    }

    public void Execute()
    {
        Debug.Log($"Executing ExecuteCodeCommand: {code}");
        try
        {
            var result = CSEditorHelper.ExecuteCommand(code);
            Debug.Log($"Result: {result}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error executing code: {e.Message}");
        }
    }
}


public static class CSEditorHelper
{
    public static object ExecuteCommand(string code)
    {
        // Create a method that wraps the code
        string wrappedCode = $@"
            using UnityEngine;
            using UnityEditor;
            using System;
            using System.Linq;
            using System.Collections.Generic;

            public class CodeExecutor
            {{
                public static object Execute()
                {{
                    {code}
                    return ""Success"";
                }}
            }}
        ";

        // Use Mono's built-in compiler
        var options = new System.CodeDom.Compiler.CompilerParameters
        {
            GenerateInMemory = true
        };
        
        // Add necessary references
        options.ReferencedAssemblies.Add(typeof(UnityEngine.Object).Assembly.Location);
        options.ReferencedAssemblies.Add(typeof(UnityEditor.Editor).Assembly.Location);
        options.ReferencedAssemblies.Add(typeof(System.Linq.Enumerable).Assembly.Location); // Add System.Core for LINQ
        options.ReferencedAssemblies.Add(typeof(object).Assembly.Location); // Add mscorlib
        options.ReferencedAssemblies.Add(AppDomain.CurrentDomain.GetAssemblies()
            .First(a => a.GetName().Name == "netstandard").Location); // Add netstandard
        
        // Compile and execute
        using (var provider = new Microsoft.CSharp.CSharpCodeProvider())
        {
            var results = provider.CompileAssemblyFromSource(options, wrappedCode);
            if (results.Errors.HasErrors)
            {
                throw new Exception("Compilation failed: " + string.Join(", ", results.Errors.Cast<System.CodeDom.Compiler.CompilerError>().Select(e => e.ErrorText)));
            }

            var assembly = results.CompiledAssembly;
            var type = assembly.GetType("CodeExecutor");
            var method = type.GetMethod("Execute");
            return method.Invoke(null, null);
        }
    }
}
        