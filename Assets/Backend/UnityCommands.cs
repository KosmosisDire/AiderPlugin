using System;
using System.Linq;
using System.Reflection;
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
    public string sourceJson;
    VisualElement container;
    ScrollView outputLog;
    SelectableLabel outputLabel;

    public bool isFinished = false;
    protected abstract Task<CommandFeedback> ExecuteCommand();
    public async Task Execute()
    {
        try
        {
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

            outputLabel ??= new SelectableLabel();
            outputLabel.value = output.message?.Trim() ?? "";
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
        }
        catch (Exception e)
        {
            Debug.LogError($"Error executing command: {e.Message}");
        }
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

        var sourceButton = new Button(() =>
        {
            GUIUtility.systemCopyBuffer = sourceJson;
        });
        sourceButton.AddToClassList("command-source-button");
        container.Add(sourceButton);

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

// Add Component Command
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
        GameObject targetObject = FindObjectUtil.FindObject(objectPath);
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
    public string scenePath;
    public string objectType;
    public Vector3 localPosition;
    public Vector3 localRotation;
    public Vector3 localScale;
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
        this.scenePath = objectPath;
        this.objectType = objectType;
        this.localPosition = position;
        this.localRotation = rotation;
        this.localScale = scale;
        this.tag = tag;
        this.layer = layer;
    }

    public static void ProcessObject(CommandFeedback feedback, GameObject newObject, string scenePath, string objectType, Vector3 localPosition, Vector3 localRotation, Vector3 localScale, string tag = null, string layer = null)
    {
        // Set the object path in the hierarchy
        if (scenePath.LastIndexOf('/') > 0)
        {
            var parentPath = scenePath.Substring(0, scenePath.LastIndexOf('/'));
            var parentObject = FindObjectUtil.FindObject(parentPath);
            if (parentObject != null)
            {
                newObject.transform.SetParent(parentObject.transform, true);
            }
            else
            {
                feedback.Log($"Parent object '{parentPath}' not found. Setting as root.", CommandStatus.Warning);
            }
        }

        // set local coordinates
        newObject.transform.SetLocalPositionAndRotation(localPosition, Quaternion.Euler(localRotation));
        newObject.transform.localScale = localScale;

        newObject.name = scenePath.Substring(scenePath.LastIndexOf('/') + 1);
        newObject.transform.SetAsLastSibling(); // Set the new object as the last sibling in the hierarchy

        // get type from type string
        Type type = UnityJsonCommandParser.FindType(objectType, false, true);
        if (type != typeof(GameObject))
        {
            Debug.Log($"Adding Type: {type}");
            if (type != null)
            {
                // Add the component to the new object
                var component = newObject.AddComponent(type);
                if (component == null)
                {
                    feedback.Log($"Failed to add component of type '{objectType}' to '{scenePath}'.", CommandStatus.Error);
                }
                else
                {
                    feedback.Log($"Added component of type '{objectType}' to '{scenePath}'.", CommandStatus.Success);
                }
            }
            else
            {
                feedback.Log($"Type '{objectType}' not found.", CommandStatus.Error);
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
                feedback.Log($"Layer '{layer}' not found.", CommandStatus.Warning);
            }
        }
    }

    protected override async Task<CommandFeedback> ExecuteCommand()
    {
        var commandFeedback = new CommandFeedback();

        Debug.Log($"Executing AddObjectCommand: {scenePath}");
        GameObject newObject = new("TEMP_OBJECT");

        ProcessObject(commandFeedback, newObject, scenePath, objectType, localPosition, localRotation, localScale, tag, layer);

        return commandFeedback;
    }

    public override VisualElement BuildDisplay()
    {
        var container = new VisualElement();
        container.Add(new Label($"Adding {scenePath} with type {objectType}"));
        return container;
    }
}


// Add Object Command
[Serializable]
public class AddObjectMenuCommand : AiderUnityCommandBase
{
    public string menuPath;
    public string scenePath;
    public Vector3 localPosition;
    public Vector3 localRotation;
    public Vector3 localScale;
    public string tag;
    public string layer;

    public AddObjectMenuCommand(
        string menuPath, 
        string scenePath, 
        Vector3 position, 
        Vector3 rotation, 
        Vector3 scale,
        string tag = null,
        string layer = null)
    {
        this.menuPath = menuPath;
        this.scenePath = scenePath;
        this.localPosition = position;
        this.localRotation = rotation;
        this.localScale = scale;
        this.tag = tag;
        this.layer = layer;
    }

    protected override async Task<CommandFeedback> ExecuteCommand()
    {
        var commandFeedback = new CommandFeedback();

        Debug.Log($"Executing AddObjectCommand: {scenePath}");

        // get a list of all objects in scene before and after the command so we know what object was added
        var allObjectsBefore = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        
        try
        {
            MenuItemsUtility.ExecuteMenuItem(menuPath);
        }
        catch (Exception e)
        {
            commandFeedback.Log($"Error executing menu item '{menuPath}': {e.Message}", CommandStatus.Error);
            return commandFeedback;
        }

        var allObjectsAfter = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        var listDiff = allObjectsAfter.Except(allObjectsBefore).ToList();

        string GetTransformPath(GameObject go)
        {
            var t = go.transform;
            if (t.parent == null) return t.name;
            return GetTransformPath(t.parent.gameObject) + "/" + t.name;
        }

        // find minimum depth object
        int minDepth = int.MaxValue;
        foreach (var obj in listDiff)
        {
            var path = GetTransformPath(obj);
            var depth = path.Split('/').Length;
            if (depth < minDepth)
            {
                minDepth = depth;
            }
        }

        // find all objects with this min depth. This represents all root added objects
        listDiff = listDiff.Where(obj => GetTransformPath(obj).Split('/').Length == minDepth).ToList();

        if (listDiff.Count == 0)
        {
            commandFeedback.Log($"No new objects created after executing menu item '{menuPath}'.", CommandStatus.Error);
            return commandFeedback;
        }

        if (listDiff.Count > 1)
        {
            commandFeedback.Log($"Multiple new objects created after executing menu item '{menuPath}', applying to all.", CommandStatus.Warning);
            return commandFeedback;
        }

        for (int i = 0; i < listDiff.Count; i++)
        {
            var newObject = listDiff[i];
            AddObjectCommand.ProcessObject(commandFeedback, newObject, scenePath, newObject.GetType().Name, localPosition, localRotation, localScale, tag, layer);
        }

        return commandFeedback;
    }

    public override VisualElement BuildDisplay()
    {
        var container = new VisualElement();
        container.Add(new Label($"Adding {scenePath} from menu item {menuPath}"));
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
    public string value;

    public SetComponentPropertyCommand(string objectPath, string componentType, string propertyPath, string value)
    {
        this.objectPath = objectPath;
        this.componentType = componentType;
        this.propertyPath = propertyPath;
        this.value = value;
    }

    private object StringToObject(string value, Type type)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        if (value.StartsWith("{") && value.EndsWith("}"))
        {
            Debug.Log($"Parsing JSON value: {value} to type: {type}");
            return JsonUtility.FromJson(value, type);
        }
        else if (typeof(UnityEngine.Object).IsAssignableFrom(type) && IsAssetPath(value))
        {
            Debug.Log($"Loading asset at path: {value}");
            return UnityEditor.AssetDatabase.LoadAssetAtPath(value, type);
        }
        else if (typeof(UnityEngine.Object).IsAssignableFrom(type) && IsTransformPath(value))
        {
            Debug.Log($"Loading transform at path: {value}");
            var go = FindObjectUtil.FindObject(value);
            if (go == null)
            {
                throw new ArgumentException($"GameObject at path '{value}' not found.");
            }

            if (type == typeof(GameObject))
            {
                return go;
            }
            else
            {
                var component = go.GetComponent(type);
                if (component == null)
                {
                    throw new ArgumentException($"Component of type '{type.Name}' not found on GameObject at path '{value}'.");
                }
                return component;
            }
        }
        else if (type.BaseType == typeof(UnityEngine.Object))
        {
            throw new ArgumentException($"Cannot get object reference for type '{type.Name}' from string '{value}'. Use a valid asset path or transform path.");
        }
        else if (type.IsEnum)
        {
            Debug.Log($"Converting value: {value} to enum type: {type}");
            object enumValue;
            try
            {
                enumValue = Enum.Parse(type, value, true);
            }
            catch (Exception e)
            {
                var names = type.GetEnumNames();
                Debug.LogError($"Enum names: {string.Join(", ", names)}");
                Debug.LogError($"Error parsing enum value: {e.Message}");
                throw new ArgumentException($"Invalid enum value '{value}' for type '{type.Name}'. Valid values are: {string.Join(", ", names)}", e);
            }

            return enumValue;
        }
        else
        {
            Debug.Log($"Converting value: {value} to type: {type}");
            return Convert.ChangeType(value, type);
        }
    }

    protected override async Task<CommandFeedback> ExecuteCommand()
    {
        Debug.Log($"Executing SetComponentPropertyCommand: {objectPath}, {componentType}, {propertyPath}");
        GameObject targetObject = FindObjectUtil.FindObject(objectPath);
        if (targetObject != null)
        {
            Type type = UnityJsonCommandParser.FindType(componentType, false, true);
            if (type != null)
            {
                UnityEngine.Object component = type == typeof(GameObject) ? targetObject : targetObject.GetComponent(type);
                if (component != null)
                {
                    try
                    {
                        var componentType = component.GetType();
                        var property = componentType.GetProperty(propertyPath);
                        var field = componentType.GetField(propertyPath);

                        if (property == null && field == null)
                        {
                            // try ignoring case
                            property = componentType.GetProperty(propertyPath, BindingFlags.IgnoreCase);
                            field = componentType.GetField(propertyPath, BindingFlags.IgnoreCase);
                        }

                        if (propertyPath.StartsWith("m_") && property == null && field == null)
                        {
                            // try to find without the m_ prefix
                            var propertyName = propertyPath[2..];
                            property = componentType.GetProperty(propertyName);
                            field = componentType.GetField(propertyName);

                            if (property == null && field == null)
                            {
                                // try ignoring case
                                property = componentType.GetProperty(propertyName, BindingFlags.IgnoreCase);
                                field = componentType.GetField(propertyName, BindingFlags.IgnoreCase);
                            }
                        }

                        if (property != null)
                        {
                            var newValue = StringToObject(value?.ToString(), property.PropertyType);
                            Debug.Log($"Setting property '{propertyPath}' to value '{newValue}' from '{value}' of type '{property.PropertyType}' on {componentType.Name}");
                            property.SetValue(component, newValue, null); 

                            // check to make sure the value is now correctly set
                            var currentValue = property.GetValue(component, null);
                            if (!Equals(currentValue, newValue))
                            {
                                return new CommandFeedback($"Property '{propertyPath}' not set correctly on {componentType.Name}", CommandStatus.Error);
                            }

                            return new CommandFeedback($"Property '{propertyPath}' set on {componentType.Name}", CommandStatus.Success);
                        }
                        else if (field != null)
                        {
                            var newValue = StringToObject(value?.ToString(), field.FieldType);
                            Debug.Log($"Setting field '{propertyPath}' to value '{newValue}' from '{value}' of type '{field.FieldType}' on {componentType.Name}");
                            field.SetValue(component, newValue);

                            // check to make sure the value is now correctly set
                            var currentValue = field.GetValue(component);
                            if (!Equals(currentValue, newValue))
                            {
                                return new CommandFeedback($"Field '{propertyPath}' not set correctly on {componentType.Name}", CommandStatus.Error);
                            }

                            return new CommandFeedback($"Field '{propertyPath}' set on {componentType.Name}", CommandStatus.Success);
                        }
                        else
                        {
                            return new CommandFeedback($"Property or field '{propertyPath}' not found on {componentType.Name}", CommandStatus.Error);
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

    private bool IsTransformPath(string value)
    {
        return value.Contains("/") && FindObjectUtil.FindObject(value) != null;
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
        GameObject targetObject = FindObjectUtil.FindObject(objectPath);
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
        GameObject targetObject = FindObjectUtil.FindObject(objectPath);
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
                    GameObject parentObject = FindObjectUtil.FindObject(parentPath);
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
        GameObject targetObject = FindObjectUtil.FindObject(objectPath);
        if (targetObject != null)
        {
            GameObject parentObject = null;
            
            // Handle null or empty parent path as a request to unparent
            if (string.IsNullOrEmpty(parentPath))
            {
                targetObject.transform.SetParent(null, worldPositionStays);
                return new CommandFeedback($"Unparented {objectPath}", CommandStatus.Success);
            }
            
            parentObject = FindObjectUtil.FindObject(parentPath);
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