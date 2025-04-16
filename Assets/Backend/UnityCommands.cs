using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
        container.Add(executeButton);

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
public class AddComponentCommand : IAiderUnityCommand
{
    public string objectPath;
    public string componentType;

    public AddComponentCommand(string objectPath, string componentType)
    {
        this.objectPath = objectPath;
        this.componentType = componentType;
    }

    protected override void ExecuteCommand()
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
                Debug.LogWarning($"Component type '{componentType}' not found. Component not added to '{objectPath}'.");
            }
        }
        else
        {
            Debug.LogWarning($"Target object '{objectPath}' not found. Component not added.");
        }
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

    protected override async void ExecuteCommand()
    {
        Debug.Log($"Executing ExecuteCodeCommand: {code}");
        try
        {
            var result = await CSharpCompiler.ExecuteCommand(code);
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

// Set Component Property Command
[Serializable]
public class SetComponentPropertyCommand : IAiderUnityCommand
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

    protected override void ExecuteCommand()
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
                                    Debug.LogError($"Property or field '{propertyPathParts[i]}' not found on {targetObj.GetType().Name}");
                                    return;
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
                                    Debug.Log($"Set property {propertyPath} to asset at {value}");
                                }
                                else
                                {
                                    Debug.LogError($"Failed to load asset at path: {stringValue}");
                                }
                            }
                            else
                            {
                                // Use the original conversion logic
                                object convertedValue = Convert.ChangeType(value, finalPropertyInfo.PropertyType);
                                finalPropertyInfo.SetValue(targetObj, convertedValue);
                                Debug.Log($"Set property {propertyPath} to {value}");
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
                                        Debug.Log($"Set field {propertyPath} to asset at {value}");
                                    }
                                    else
                                    {
                                        Debug.LogError($"Failed to load asset at path: {stringValue}");
                                    }
                                }
                                else
                                {
                                    // Use the original conversion logic
                                    object convertedValue = Convert.ChangeType(value, finalFieldInfo.FieldType);
                                    finalFieldInfo.SetValue(targetObj, convertedValue);
                                    Debug.Log($"Set field {propertyPath} to {value}");
                                }
                            }
                            else
                            {
                                Debug.LogError($"Property or field '{finalProperty}' not found on {targetObj.GetType().Name}");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error setting property: {e.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning($"Component '{componentType}' not found on '{objectPath}'.");
                }
            }
            else
            {
                Debug.LogWarning($"Component type '{componentType}' not found.");
            }
        }
        else
        {
            Debug.LogWarning($"Target object '{objectPath}' not found.");
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
public class DeleteObjectCommand : IAiderUnityCommand
{
    public string objectPath;

    public DeleteObjectCommand(string objectPath)
    {
        this.objectPath = objectPath;
    }

    protected override void ExecuteCommand()
    {
        Debug.Log($"Executing DeleteObjectCommand: {objectPath}");
        GameObject targetObject = GameObject.Find(objectPath);
        if (targetObject != null)
        {
            UnityEngine.Object.DestroyImmediate(targetObject);
            Debug.Log($"Deleted object {objectPath}");
        }
        else
        {
            Debug.LogWarning($"Target object '{objectPath}' not found. Nothing to delete.");
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
public class CreatePrefabCommand : IAiderUnityCommand
{
    public string objectPath;
    public string prefabPath;

    public CreatePrefabCommand(string objectPath, string prefabPath)
    {
        this.objectPath = objectPath;
        this.prefabPath = prefabPath;
    }

    protected override void ExecuteCommand()
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

                // Save as prefab (this part requires Unity Editor)
                UnityEditor.PrefabUtility.SaveAsPrefabAsset(targetObject, prefabPath);
                Debug.Log($"Created prefab at {prefabPath}");
                UnityEditor.AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error creating prefab: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"Target object '{objectPath}' not found. Prefab not created.");
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
public class InstantiatePrefabCommand : IAiderUnityCommand
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

    protected override void ExecuteCommand()
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
                        Debug.LogWarning($"Parent object '{parentPath}' not found. Instance created at root level.");
                    }
                }
                
                Debug.Log($"Instantiated prefab {prefabPath}");
            }
            else
            {
                Debug.LogWarning($"Prefab at path '{prefabPath}' not found.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error instantiating prefab: {e.Message}");
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
public class SetParentCommand : IAiderUnityCommand
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

    protected override void ExecuteCommand()
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
                Debug.Log($"Set {objectPath} parent to null (root)");
                return;
            }
            
            parentObject = GameObject.Find(parentPath);
            if (parentObject != null)
            {
                targetObject.transform.SetParent(parentObject.transform, worldPositionStays);
                Debug.Log($"Set {objectPath} parent to {parentPath}");
            }
            else
            {
                Debug.LogWarning($"Parent object '{parentPath}' not found. Parent not set.");
            }
        }
        else
        {
            Debug.LogWarning($"Target object '{objectPath}' not found. Parent not set.");
        }
    }

    public override VisualElement BuildDisplay()
    {
        var container = new VisualElement();
        container.Add(new Label($"Setting {objectPath} parent to {parentPath}"));
        return container;
    }
}