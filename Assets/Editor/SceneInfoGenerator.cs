using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Reflection;
using Microsoft.CSharp;
using System.CodeDom;
using System.Linq;


public static class SceneInfoGenerator
{
        
    [Serializable]
    public struct ComponentInfo
    {
        public string type;
        public List<string> properties;
    }

    [Serializable]
    public struct SceneObjectInfoDetailed
    {
        public string scenePath;
        public string tag;
        public string layer;
        public bool isActive;
        public List<ComponentInfo> components;
    }

    [Serializable]
    public struct SceneObjectInfoSimple
    {
        public string scenePath;
        public string tag;
        public string layer;
        public bool isActive;
        public List<string> components;
    }

    [Serializable]
    public struct SceneInfo
    {
        public string sceneName;
        public string unityVersion;
        public List<SceneObjectInfoSimple> objects;
    }

    public static string GetObjectPath(UnityEngine.Object obj)
    {
        // check if this gameobject is an asset reference and return the file path instead of a transform path
        var path = AssetDatabase.GetAssetPath(obj.GetInstanceID());

        if (path == "" && obj is GameObject gameObj)
        {
            var transform = gameObj.transform;
            path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
        }
        else if (path == "")
        {
            path = "unknown";
        }

        return path;
    }

    public static string GetObjectString(object obj)
    {
        if (obj == null) return "null";

        var objString = "cannot display as text";

        // check if object directly defines ToString
        if (obj.GetType().GetMethods()
                .Where(m => m.Name == "ToString")
                .Select(m => m.DeclaringType)
                .Any(m => m == obj.GetType()))
        {
            objString = obj.ToString();
        }
        else
        {
            if (obj is UnityEngine.Object unityObj)
            {
                objString = GetObjectPath(unityObj);
            }
        }

        return objString;
    }

    public static string ArrayValue(SerializedProperty property)
    {
        string value = "[";
        for (int i = 0; i < property.arraySize; i++)
        {
            var item = property.GetArrayElementAtIndex(i);
            var obj = item.boxedValue;
            value += GetObjectString(obj);
        }
        value += "]";
        return value;
    }


    public static (string name, string type, string value, bool isPublic) GetPropertyInfo(SerializedProperty property, Component component)
    {
        string name = property.name;
        string type = property.type;
        string value = "null";
        bool isPublic;

        // special case for Unity's private properties
        if (name.StartsWith("m_"))
        {
            name = name[2..]; // Remove the "m_" prefix
            name = char.ToLowerInvariant(name[0]) + name[1..]; // Convert first character to lowercase
        }

        // take property type and value using reflection to make sure the property exists, and we have the right type
        var refProperty = component.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var refField = component.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        
        var hasProperty = refProperty != null;
        var hasField = refField != null;
        
        if ((!hasProperty && !hasField) || 
        (hasProperty && refProperty.GetCustomAttribute<ObsoleteAttribute>() != null) || 
        (hasField && refField.GetCustomAttribute<ObsoleteAttribute>() != null))
        {
            // Skip if property or field is obsolete
            return (null, null, null, false);
        }

        var typeObj = refProperty?.PropertyType ?? refField?.FieldType ?? null;
        type = typeObj?.Name ?? type;
        
        if (hasProperty) value = GetObjectString(refProperty.GetValue(component, null));
        else if (hasField) value = GetObjectString(refField.GetValue(component));

        if (property.isArray)
        {
            value = ArrayValue(property);
        }

        // get public status
        isPublic = (refProperty?.GetMethod?.IsPublic ?? false) || (refField?.IsPublic ?? false);

        // clean up type name
        if (type == "String") type = "string";
        if(typeObj.IsPrimitive)
        {
            // convert primitive type names: ex. Int32 -> int
            using var provider = new CSharpCodeProvider();
            type = provider.GetTypeOutput(new CodeTypeReference(typeObj));
        }

        // clean up value
        if (typeObj == typeof(bool) && value == "True") value = "true";
        if (typeObj == typeof(bool) && value == "False") value = "false";

        return (name, type, value, isPublic);
    }

    public static List<ComponentInfo> GetComponentInfo(GameObject obj)
    {
        var componentList = new List<ComponentInfo>();
        foreach (var component in obj.GetComponents<Component>())
        {
            if (component == null) continue;

            var serialized = new SerializedObject(component);
            var iterator = serialized.GetIterator();
            var properties = new List<string>();

            if (iterator.NextVisible(true))
            {
                do
                {
                    (string name, string type, string value, bool isPublic) = GetPropertyInfo(iterator, component);

                    if (name == null || type == null || value == null) continue;

                    var statement = $"{type} {name} = {value}";
                    if (!isPublic)
                    {
                        statement = $"PRIVATE {statement} // Use commands to modify private properties or make them public if you wrote this component";
                    }
                        
                    properties.Add(statement);

                } while (iterator.NextVisible(false));
            }
            componentList.Add(new ComponentInfo
            {
                type = component.GetType().Name,
                properties = properties
            });
        }
        return componentList;
    }

    public static string GetSceneInfoJson()
    {
        var sceneName = SceneManager.GetActiveScene().name;
        var unityVersion = Application.unityVersion;

        var transforms = GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        var objectInfos = new List<SceneObjectInfoSimple>();

        foreach (var t in transforms)
        {
            var obj = t.gameObject;

            objectInfos.Add(new()
            {
                scenePath = GetObjectPath(obj),
                tag = obj.tag,
                layer = LayerMask.LayerToName(obj.layer),
                isActive = obj.activeSelf,
                components = GetComponentInfo(obj).Select(c => c.type).ToList(),
            });
        }

        var sceneInfo = new SceneInfo
        {
            sceneName = sceneName,
            unityVersion = unityVersion,
            objects = objectInfos
        };

        // sort by path
        sceneInfo.objects.Sort((a, b) => a.scenePath.CompareTo(b.scenePath));

        return JsonUtility.ToJson(sceneInfo, true);
    }

    public static string GetDetailedObjectInfo(GameObject obj)
    {
        var objectInfo = new SceneObjectInfoDetailed
        {
            scenePath = GetObjectPath(obj),
            tag = obj.tag,
            layer = LayerMask.LayerToName(obj.layer),
            isActive = obj.activeSelf,
            components = GetComponentInfo(obj)
        };

        return JsonUtility.ToJson(objectInfo, true);
    }

    public static void SaveSceneInfoToFile(string filePath)
    {
        string sceneInfoJson = GetSceneInfoJson();
        File.WriteAllText(filePath, sceneInfoJson);
        Debug.Log("Scene info saved to " + filePath);
    }

    [MenuItem("Aider/Print Scene Info")]
    public static void PrintSceneInfo()
    {
        string filePath = "Assets/SceneInfo.json";
        Debug.Log("Scene info: " + GetSceneInfoJson());
        SaveSceneInfoToFile(filePath);
    }
}