using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

public abstract class IAiderUnityCommand
{

    protected abstract void ExecuteCommand();
    public void Execute()
    {
        try
        {
            ExecuteCommand();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error executing command: {e.Message}");
        }
    }

    public abstract VisualElement BuildDisplay();

    public VisualElement BuildUI()
    {
        var container = new VisualElement();
        container.AddToClassList("command-container");
        string[] SplitCamelCase(string source)
        {
            return Regex.Split(source, @"(?<!^)(?=[A-Z])");
        }
        var className = this.GetType().Name;
        var splitName = SplitCamelCase(className);
        var cssClass = string.Join("-", splitName).ToLower().Replace("-command", "");
        container.AddToClassList("command-container-" + cssClass);

        var titleName = string.Join(" ", splitName);
        var titleLabel = new Label(titleName);
        titleLabel.AddToClassList("command-title");
        container.Add(titleLabel);

        var commandView = BuildDisplay();
        commandView.AddToClassList("command-view");
        container.Add(commandView);

        var executeButton = new Button(() =>
        {
            Execute();
        });
        executeButton.AddToClassList("command-execute-button");

        return container;
    }
}

public class InvalidCommand : IAiderUnityCommand
{
    public string errorMessage;

    public InvalidCommand(string errorMessage)
    {
        this.errorMessage = errorMessage;
    }

    protected override void ExecuteCommand()
    {
        Debug.LogError($"Invalid command: {errorMessage}");
    }

    public override VisualElement BuildDisplay()
    {
        var container = new VisualElement();
        container.Add(new Label(errorMessage));
        return container;
    }
}

// Add Object Command
[Serializable]
public class AddObjectCommand : IAiderUnityCommand
{
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
        this.objectPath = objectPath;
        this.objectType = objectType;
        this.position = new float[] { position.x, position.y, position.z };
        this.rotation = new float[] { rotation.x, rotation.y, rotation.z };
        this.scale = new float[] { scale.x, scale.y, scale.z };
        this.tag = tag;
        this.layer = layer;
    }

    protected override void ExecuteCommand()
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
            Type type = UnityJsonCommandParser.FindType(objectType, false, true);
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

    public override VisualElement BuildDisplay()
    {
        var container = new VisualElement();
        container.Add(new Label($"Adding {objectPath} with type {objectType}"));
        return container;
    }
}

// Execute Code Command
[Serializable]
public class ExecuteCodeCommand : IAiderUnityCommand
{
    public string shortDescription;
    public string code;

    public ExecuteCodeCommand(string code, string shortDescription = null)
    {
        this.shortDescription = shortDescription;
        this.code = code;
    }

    protected override void ExecuteCommand()
    {
        Debug.Log($"Executing ExecuteCodeCommand: {code}");
        try
        {
            var result = CSharpCompiler.ExecuteCommand(code);
            Debug.Log($"Result: {result}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error executing code: {e.Message}");
        }
    }

    public override VisualElement BuildDisplay()
    {
        var container = new VisualElement();
        if (!string.IsNullOrEmpty(shortDescription))
        {
            container.Add(new Label($"{MarkdownParser.ParseString(shortDescription)}"));
        }
        return container;
    }
}


