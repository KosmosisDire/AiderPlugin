using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

[Serializable]
public struct ComponentInfo
{
    public string type;
    public List<string> properties;
}

[Serializable]
public struct SceneObjectInfo
{
    public string path;
    public List<ComponentInfo> components;
}

[Serializable]
public struct SceneInfo
{
    public string sceneName;
    public string unityVersion;
    public List<SceneObjectInfo> objects;
}

public static class AiderSceneInfo
{
    public static string GetTransformPath(Transform transform)
    {
        var path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }
    
    private static string GetSerializedPropertyValue(SerializedProperty proper)
    {
        switch (proper.propertyType)
        {
            case SerializedPropertyType.Integer : 
                return proper.intValue.ToString();
            case SerializedPropertyType.Float : 
                return proper.floatValue.ToString("F3");
            case SerializedPropertyType.String: 
                return proper.stringValue;
            case SerializedPropertyType.Enum:
                return proper.enumDisplayNames[proper.enumValueIndex];
            case SerializedPropertyType.Boolean:
                return proper.boolValue.ToString();
            case SerializedPropertyType.Color:
                return proper.colorValue.ToString();
            case SerializedPropertyType.Vector3:
                return proper.vector3Value.ToString();
            case SerializedPropertyType.Vector2:
                return proper.vector2Value.ToString();
            case SerializedPropertyType.Quaternion:
                return proper.quaternionValue.eulerAngles.ToString();

            default: return "[not supported!]";
        }

    }
    public static List<ComponentInfo> GetComponentInfo(GameObject obj)
    {
        var componentList = new List<ComponentInfo>();
        foreach (var component in obj.GetComponents<Component>())
        {
            if (component == null) continue;

            var serialized = new SerializedObject(component);
            var iterator = serialized.GetIterator();
            bool hasLongArray = false;
            var properties = new List<string>();

            if (iterator.NextVisible(true))
            {
                do
                {
                    if (iterator.isArray && iterator.arraySize > 10)
                    {
                        hasLongArray = true;
                        break;
                    }
                    if (iterator.name == "m_Script") continue;

                    string value = GetSerializedPropertyValue(iterator);
                    properties.Add($"{iterator.displayName}: {value}");

                } while (iterator.NextVisible(false));
            }

            if (hasLongArray) continue ;

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
        var objectInfos = new List<SceneObjectInfo>();

        foreach (var t in transforms)
        {
            var obj = t.gameObject;

            objectInfos.Add(new SceneObjectInfo
            {
                path = GetTransformPath(t),
                components = GetComponentInfo(obj)
            });
        }

        var sceneInfo = new SceneInfo
        {
            sceneName = sceneName,
            unityVersion = unityVersion,
            objects = objectInfos
        };

        return JsonUtility.ToJson(sceneInfo, true);
    }

    public static void SaveSceneInfoToFile(string filePath)
    {
        string sceneInfoJson = GetSceneInfoJson();
        File.WriteAllText(filePath, sceneInfoJson);
        Debug.Log("Scene info saved to " + filePath);
    }

    [MenuItem("Aider/Save Scene to Json File")]
    public static void PrintSceneInfo()
    {
        string filePath = "Assets/SceneInfo.json";
        SaveSceneInfoToFile(filePath);
    }
}
