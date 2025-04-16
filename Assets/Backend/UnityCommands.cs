using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public enum CommandStatus
{
    Success,
    Warning,
    Error
}

public struct CommandFeedback
{
    public string message;
    public CommandStatus status;

    public CommandFeedback(string message = "", CommandStatus status = CommandStatus.Success)
    {
        this.message = "";
        this.status = CommandStatus.Success;
        Log(message, status);
    }


    public void Log(string message, CommandStatus status)
    {
        this.message += "\n" + message;
        if (status == CommandStatus.Error) this.status = CommandStatus.Error;
        else if (status == CommandStatus.Warning && status != CommandStatus.Error) this.status = CommandStatus.Warning;
        else if (status == CommandStatus.Success && status != CommandStatus.Warning) this.status = CommandStatus.Success;

        if (status == CommandStatus.Error)
        {
            Debug.LogError(message);
        }
        else if (status == CommandStatus.Warning)
        {
            Debug.LogWarning(message);
        }
        else
        {
            Debug.Log(message);
        }
    }
}

public abstract class AiderUnityCommandBase
{
    VisualElement container;
    ScrollView outputLog;
    Label outputLabel;

    public bool isFinished = false;
    protected abstract Task<CommandFeedback> ExecuteCommand();
    public async Task Execute()
    {
        // try
        // {
            container.RemoveFromClassList("command-finished");
            container.AddToClassList("command-executing");
            isFinished = false;
            var output = await ExecuteCommand();

            if (output.status == CommandStatus.Error)
            {
                container.AddToClassList("command-error");
                container.RemoveFromClassList("command-warning");
                container.RemoveFromClassList("command-success");
            }
            else if (output.status == CommandStatus.Warning)
            {
                container.AddToClassList("command-warning");
                container.RemoveFromClassList("command-error");
                container.RemoveFromClassList("command-success");
            }
            else if (output.status == CommandStatus.Success)
            {
                container.AddToClassList("command-success");
                container.RemoveFromClassList("command-warning");
                container.RemoveFromClassList("command-error");
            }


            if (outputLabel == null)
                outputLabel = new Label();

            outputLabel.text = output.message?.Trim() ?? "";
            outputLabel.AddToClassList("command-output-label");
            outputLog.Add(outputLabel);

            if (!string.IsNullOrWhiteSpace(outputLabel.text))
            {
                outputLog.AddToClassList("has-output");
            }
            else
            {
                outputLog.RemoveFromClassList("has-output");
            }

            isFinished = true;
            container.RemoveFromClassList("command-executing");
            container.AddToClassList("command-finished");
        // }
        // catch (Exception e)
        // {
        //     Debug.LogError($"Error executing command: {e.Message}");
        // }
    }

    public abstract VisualElement BuildDisplay();

    public VisualElement BuildUI()
    {
        container = new VisualElement();
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

        outputLog = new ScrollView();
        outputLog.AddToClassList("output-log");
        container.Add(outputLog);

        var executeButton = new Button(async () =>
        {
            await Execute();
        });
        executeButton.AddToClassList("command-execute-button");
        container.Add(executeButton);

        return container;
    }
}

public class InvalidCommand : AiderUnityCommandBase
{
    public string errorMessage;

    public InvalidCommand(string errorMessage)
    {
        this.errorMessage = errorMessage;
    }

    protected override async Task<CommandFeedback> ExecuteCommand()
    {
        return new CommandFeedback(errorMessage, CommandStatus.Error);
    }

    public override VisualElement BuildDisplay()
    {
        var container = new VisualElement();
        // container.Add(new Label(errorMessage));
        return container;
    }
}

// Add Object Command
[Serializable]
public class AddComponentCommand : AiderUnityCommandBase
{
    public string objectPath;
    public string componentType;

    public AddComponentCommand(string objectPath, string componentType)
    {
        this.objectPath = objectPath;
        this.componentType = componentType;
    }

    protected override async Task<CommandFeedback> ExecuteCommand()
    {
        Debug.Log($"Executing AddComponentCommand: {objectPath} with component {componentType}");
        GameObject targetObject = GameObject.Find(objectPath);
        if (targetObject != null)
        {
            Type type = UnityJsonCommandParser.FindType(componentType, false, true);
            if (type != null)
            {
                targetObject.AddComponent(type);
                Debug.Log($"Added component {componentType} to {objectPath}");
            }
            else
            {
                return new CommandFeedback($"Component type '{componentType}' not found.", CommandStatus.Error);
            }
        }
        else
        {
            return new CommandFeedback($"Target object '{objectPath}' not found.", CommandStatus.Error);
        }

        return new CommandFeedback($"Added component {componentType} to {objectPath}", CommandStatus.Success);
    }

    public override VisualElement BuildDisplay()
    {
        var container = new VisualElement();
        container.Add(new Label($"Adding {componentType} component to {objectPath}"));
        return container;
    }
}


// Add Object Command
[Serializable]
public class AddObjectCommand : AiderUnityCommandBase
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

    protected override async Task<CommandFeedback> ExecuteCommand()
    {
        var commandFeedback = new CommandFeedback();

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
                commandFeedback.Log($"Parent object '{parentPath}' not found. Setting as root.", CommandStatus.Warning);
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
                    commandFeedback.Log($"Failed to add component of type '{objectType}' to '{objectPath}'.", CommandStatus.Error);
                }
                else
                {
                    commandFeedback.Log($"Added component of type '{objectType}' to '{objectPath}'.", CommandStatus.Success);
                }
            }
            else
            {
                commandFeedback.Log($"Type '{objectType}' not found.", CommandStatus.Error);
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
                commandFeedback.Log($"Layer '{layer}' not found.", CommandStatus.Warning);
            }
        }

        return commandFeedback;
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
public class ExecuteCodeCommand : AiderUnityCommandBase
{
    public string shortDescription;
    public string code;

    public ExecuteCodeCommand(string code, string shortDescription = null)
    {
        this.shortDescription = shortDescription;
        this.code = code;
    }

    protected override async Task<CommandFeedback> ExecuteCommand()
    {
        try
        {
            var result = await CSharpCompiler.ExecuteCommand(code);
            return new CommandFeedback($"{result}", CommandStatus.Success);
        }
        catch (Exception e)
        {
            return new CommandFeedback($"Error executing code: {e.Message}", CommandStatus.Error);
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

// Set Component Property Command
[Serializable]
public class SetComponentPropertyCommand : AiderUnityCommandBase
{
    public string objectPath;
    public string componentType;
    public string propertyPath;
    public object value;

    public SetComponentPropertyCommand(string objectPath, string componentType, string propertyPath, object value)
    {
        this.objectPath = objectPath;
        this.componentType = componentType;
        this.propertyPath = propertyPath;
        this.value = value;
    }

    protected override async Task<CommandFeedback> ExecuteCommand()
    {
        Debug.Log($"Executing SetComponentPropertyCommand: {objectPath}, {componentType}, {propertyPath}");
        GameObject targetObject = GameObject.Find(objectPath);
        if (targetObject != null)
        {
            Type type = UnityJsonCommandParser.FindType(componentType, false, true);
            if (type != null)
            {
                Component component = targetObject.GetComponent(type);
                if (component != null)
                {
                    try
                    {
                        // Split property path for nested properties
                        string[] propertyPathParts = propertyPath.Split('/');
                        object targetObj = component;
                        
                        // Navigate to the final property
                        for (int i = 0; i < propertyPathParts.Length - 1; i++)
                        {
                            var property = targetObj.GetType().GetProperty(propertyPathParts[i]);
                            if (property != null)
                            {
                                targetObj = property.GetValue(targetObj);
                            }
                            else
                            {
                                var field = targetObj.GetType().GetField(propertyPathParts[i]);
                                if (field != null)
                                {
                                    targetObj = field.GetValue(targetObj);
                                }
                                else
                                {
                                    return new CommandFeedback($"Property or field '{propertyPathParts[i]}' not found on {targetObj.GetType().Name}", CommandStatus.Error);
                                }
                            }
                        }

                        // Set the final property value
                        string finalProperty = propertyPathParts[propertyPathParts.Length - 1];
                        var finalPropertyInfo = targetObj.GetType().GetProperty(finalProperty);
                        if (finalPropertyInfo != null)
                        {
                            if (value is string stringValue && IsAssetPath(stringValue))
                            {
                                // Load and assign asset
                                object asset = UnityEditor.AssetDatabase.LoadAssetAtPath(stringValue, finalPropertyInfo.PropertyType);
                                if (asset != null)
                                {
                                    finalPropertyInfo.SetValue(targetObj, asset);
                                    return new CommandFeedback($"Set property {propertyPath} to asset at {value}", CommandStatus.Success);
                                }
                                else
                                {
                                    return new CommandFeedback($"Failed to load asset at path: {stringValue}", CommandStatus.Error);
                                }
                            }
                            else
                            {
                                // Use the original conversion logic
                                object convertedValue = Convert.ChangeType(value, finalPropertyInfo.PropertyType);
                                finalPropertyInfo.SetValue(targetObj, convertedValue);
                                return new CommandFeedback($"Set property {propertyPath} to {value}", CommandStatus.Success);
                            }
                        }
                        else
                        {
                            var finalFieldInfo = targetObj.GetType().GetField(finalProperty);
                            if (finalFieldInfo != null)
                            {
                                if (value is string stringValue && IsAssetPath(stringValue))
                                {
                                    // Load and assign asset
                                    object asset = UnityEditor.AssetDatabase.LoadAssetAtPath(stringValue, finalFieldInfo.FieldType);
                                    if (asset != null)
                                    {
                                        finalFieldInfo.SetValue(targetObj, asset);
                                        return new CommandFeedback($"Set field {propertyPath} to asset at {value}", CommandStatus.Success);
                                    }
                                    else
                                    {
                                        return new CommandFeedback($"Failed to load asset at path: {stringValue}", CommandStatus.Error);
                                    }
                                }
                                else
                                {
                                    // Use the original conversion logic
                                    object convertedValue = Convert.ChangeType(value, finalFieldInfo.FieldType);
                                    finalFieldInfo.SetValue(targetObj, convertedValue);
                                    return new CommandFeedback($"Set field {propertyPath} to {value}", CommandStatus.Success);
                                }
                            }
                            else
                            {
                                return new CommandFeedback($"Property or field '{finalProperty}' not found on {targetObj.GetType().Name}", CommandStatus.Error);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        return new CommandFeedback($"Error setting property '{propertyPath}': {e.Message}", CommandStatus.Error);
                    }
                }
                else
                {
                    return new CommandFeedback($"Component '{componentType}' not found on '{objectPath}'", CommandStatus.Error);
                }
            }
            else
            {
                return new CommandFeedback($"Component type '{componentType}' not found.", CommandStatus.Error);
            }
        }
        else
        {
            return new CommandFeedback($"Target object '{objectPath}' not found.", CommandStatus.Error);
        }
    }

    private bool IsAssetPath(string value)
    {
        // Check if the string looks like an asset path
        if (value.StartsWith("Assets/") || value.StartsWith("Packages/"))
        {
            // Optionally check for valid file extension
            string extension = System.IO.Path.GetExtension(value).ToLower();
            return !string.IsNullOrEmpty(extension);
        }
        return false;
    }

    public override VisualElement BuildDisplay()
    {
        var container = new VisualElement();
        container.Add(new Label($"Setting {componentType}.{propertyPath} to {value} on {objectPath}"));
        return container;
    }
}

// Delete Object Command
[Serializable]
public class DeleteObjectCommand : AiderUnityCommandBase
{
    public string objectPath;

    public DeleteObjectCommand(string objectPath)
    {
        this.objectPath = objectPath;
    }

    protected override async Task<CommandFeedback> ExecuteCommand()
    {
        Debug.Log($"Executing DeleteObjectCommand: {objectPath}");
        GameObject targetObject = GameObject.Find(objectPath);
        if (targetObject != null)
        {
            UnityEngine.Object.DestroyImmediate(targetObject);
            return new CommandFeedback($"Deleted object {objectPath}", CommandStatus.Success);
        }
        else
        {
            return new CommandFeedback($"Target object '{objectPath}' not found.", CommandStatus.Error);
        }
    }

    public override VisualElement BuildDisplay()
    {
        var container = new VisualElement();
        container.Add(new Label($"Deleting object {objectPath}"));
        return container;
    }
}

// Create Prefab Command
[Serializable]
public class CreatePrefabCommand : AiderUnityCommandBase
{
    public string objectPath;
    public string prefabPath;

    public CreatePrefabCommand(string objectPath, string prefabPath)
    {
        this.objectPath = objectPath;
        this.prefabPath = prefabPath;
    }

    protected override async Task<CommandFeedback> ExecuteCommand()
    {
        Debug.Log($"Executing CreatePrefabCommand: {objectPath} to {prefabPath}");
        GameObject targetObject = GameObject.Find(objectPath);
        if (targetObject != null)
        {
            try
            {
                // Ensure directory exists
                string directory = System.IO.Path.GetDirectoryName(prefabPath);
                if (!System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }

                UnityEditor.PrefabUtility.SaveAsPrefabAsset(targetObject, prefabPath);
                UnityEditor.AssetDatabase.Refresh();
                return new CommandFeedback($"Created prefab at {prefabPath}", CommandStatus.Success);
            }
            catch (Exception e)
            {
                return new CommandFeedback($"Error creating prefab: {e.Message}", CommandStatus.Error);
            }
        }
        else
        {
            return new CommandFeedback($"Target object '{objectPath}' not found.", CommandStatus.Error);
        }
    }

    public override VisualElement BuildDisplay()
    {
        var container = new VisualElement();
        container.Add(new Label($"Creating prefab from {objectPath} at {prefabPath}"));
        return container;
    }
}

// Instantiate Prefab Command
[Serializable]
public class InstantiatePrefabCommand : AiderUnityCommandBase
{
    public string prefabPath;
    public float[] position;
    public float[] rotation;
    public float[] scale;
    public string parentPath;

    public InstantiatePrefabCommand(
        string prefabPath, 
        Vector3 position, 
        Vector3 rotation, 
        Vector3 scale,
        string parentPath = null)
    {
        this.prefabPath = prefabPath;
        this.position = new float[] { position.x, position.y, position.z };
        this.rotation = new float[] { rotation.x, rotation.y, rotation.z };
        this.scale = new float[] { scale.x, scale.y, scale.z };
        this.parentPath = parentPath;
    }

    protected override async Task<CommandFeedback> ExecuteCommand()
    {
        Debug.Log($"Executing InstantiatePrefabCommand: {prefabPath}");
        try
        {
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                // Instantiate the prefab
                GameObject instance = UnityEngine.Object.Instantiate(prefab);
                
                // Set transform properties
                instance.transform.position = new Vector3(position[0], position[1], position[2]);
                instance.transform.rotation = Quaternion.Euler(rotation[0], rotation[1], rotation[2]);
                instance.transform.localScale = new Vector3(scale[0], scale[1], scale[2]);
                
                // Set parent if provided
                if (!string.IsNullOrEmpty(parentPath))
                {
                    GameObject parentObject = GameObject.Find(parentPath);
                    if (parentObject != null)
                    {
                        instance.transform.SetParent(parentObject.transform, false);
                    }
                    else
                    {
                        return new CommandFeedback($"Parent object '{parentPath}' not found. Prefab not instantiated under parent.", CommandStatus.Warning);
                    }
                }
                
                return new CommandFeedback($"Instantiated prefab {prefabPath} at {position[0]}, {position[1]}, {position[2]}", CommandStatus.Success);
            }
            else
            {
                return new CommandFeedback($"Prefab '{prefabPath}' not found.", CommandStatus.Error);
            }
        }
        catch (Exception e)
        {
            return new CommandFeedback($"Error instantiating prefab: {e.Message}", CommandStatus.Error);
        }
    }

    public override VisualElement BuildDisplay()
    {
        var container = new VisualElement();
        container.Add(new Label($"Instantiating prefab {prefabPath}"));
        return container;
    }
}

// Set Parent Command
[Serializable]
public class SetParentCommand : AiderUnityCommandBase
{
    public string objectPath;
    public string parentPath;
    public bool worldPositionStays;

    public SetParentCommand(string objectPath, string parentPath, bool worldPositionStays = true)
    {
        this.objectPath = objectPath;
        this.parentPath = parentPath;
        this.worldPositionStays = worldPositionStays;
    }

    protected override async Task<CommandFeedback> ExecuteCommand()
    {
        Debug.Log($"Executing SetParentCommand: {objectPath} to parent {parentPath}");
        GameObject targetObject = GameObject.Find(objectPath);
        if (targetObject != null)
        {
            GameObject parentObject = null;
            
            // Handle null or empty parent path as a request to unparent
            if (string.IsNullOrEmpty(parentPath))
            {
                targetObject.transform.SetParent(null, worldPositionStays);
                return new CommandFeedback($"Unparented {objectPath}", CommandStatus.Success);
            }
            
            parentObject = GameObject.Find(parentPath);
            if (parentObject != null)
            {
                targetObject.transform.SetParent(parentObject.transform, worldPositionStays);
                return new CommandFeedback($"Set parent of {objectPath} to {parentPath}", CommandStatus.Success);
            }
            else
            {
                return new CommandFeedback($"Parent object '{parentPath}' not found. Parent not set.", CommandStatus.Warning);
            }
        }
        else
        {
            return new CommandFeedback($"Target object '{objectPath}' not found.", CommandStatus.Error);
        }
    }

    public override VisualElement BuildDisplay()
    {
        var container = new VisualElement();
        container.Add(new Label($"Setting {objectPath} parent to {parentPath}"));
        return container;
    }
}