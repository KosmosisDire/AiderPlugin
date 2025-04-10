using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;

public static class AiderSceneInfo
{
    [System.Serializable]
    public class SceneObjectInfo
    {
        public string name;
        public int instanceID;
        public string guid;
        public string path;
        public List<ComponentInfo> components;

        public List<ChildObjectInfo> childObject; 

        [System.Serializable]
        public class ChildObjectInfo
        {
            public string name;
            public string path;
            public string guid;
        }
    }

    [System.Serializable]
    public class ComponentInfo
    {
        public string type;
        public string script;
    }

    [System.Serializable]
    public class SceneInfo
    {
        public string sceneName;
        public string unityVersion;
        public List<SceneObjectInfo> objects;
    }

    [System.Serializable]
    public class ObjectListWrapper
    {
        public List<SceneObjectInfo> objects;
    }

    [System.Serializable]
    public class CombinedInfo
    {
        public string sceneName;
        public string unityVersion;
        public List<SceneObjectInfo> sceneObjects;
        public List<SceneObjectInfo> selectedObjects;
    }

    private static string GetObjectPath(GameObject obj)
    {

        string assetpath = AssetDatabase.GetAssetPath(obj);
        if (!string.IsNullOrEmpty(assetpath))
        {
            return assetpath; // if its an asset return the path
        }
        
        string path = obj.name; // start with object name if its not an asset
        Transform parent = obj.transform.parent;

        // traverse parent objects to build path

        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;

    }

    // Gathers information about the Gameobject 
    private static SceneObjectInfo GetObjectInfo(GameObject obj, int maxDepth = 10)
{
    if (maxDepth <= 0)
    {
        return null;
    }

    SceneObjectInfo objectInfo = new SceneObjectInfo
    {
        name = obj.name,
        instanceID = obj.GetInstanceID(),
        guid = GetGUID(obj),
        components = GetComponentInfo(obj),
        childObject = new List<SceneObjectInfo.ChildObjectInfo>(),
        path = GetObjectPath(obj),
    };

    // Add child objects recursively to the object info
    AddChildObjects(obj, objectInfo, 1, maxDepth);

    return objectInfo;
}

private static void AddChildObjects(GameObject parent, SceneObjectInfo parentInfo, int currentDepth, int maxDepth)
{
    foreach (Transform child in parent.transform)
    {
        SceneObjectInfo.ChildObjectInfo childInfo = new SceneObjectInfo.ChildObjectInfo
        {
            name = child.name,
            guid = GetGUID(child.gameObject),
            path = GetObjectPath(child.gameObject),
        };

        // Add this child to the parent's childObject list
        parentInfo.childObject.Add(childInfo);

        // Recursively add children of this child if any
        AddChildObjects(child.gameObject, parentInfo, currentDepth + 1, maxDepth);
    }
}

    // Returns list of components attached to GameObject
    private static List<ComponentInfo> GetComponentInfo(GameObject obj)
    {
        List<ComponentInfo> components = new List<ComponentInfo>();

        foreach (Component component in obj.GetComponents<Component>())
        {
            ComponentInfo compData = new ComponentInfo
            {
                type = component.GetType().Name,
                script = null
            };
            // If component is a MonoBehaviour, it should get the script's file name
            if (component is MonoBehaviour monoBehaviour)
            {
                MonoScript script = MonoScript.FromMonoBehaviour(monoBehaviour);
                if (script != null )
                {
                    compData.script = script.name;
                    compData.type = null;
                }
            }
            
            if (compData.type != null || compData.script != null)
            {
                components.Add(compData);

            }
                      
        }
        return components;
    }
    // Gets the GUID of a GameObject if prefab or asset
   private static string GetGUID(GameObject obj)

    {
        // checks if the object is a prefab instance
        string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj);

        // returns the GUID of the prefab asset
        if (!string.IsNullOrEmpty(prefabPath))
        {
            return AssetDatabase.AssetPathToGUID(prefabPath);
        }

        // checks if the object is an asset 
        string assetPath = AssetDatabase.GetAssetPath(obj);
        
        // if it's an asset, return the GUID
        if (!string.IsNullOrEmpty(assetPath))
        {
            return AssetDatabase.AssetPathToGUID(assetPath);
        }

        // for non-assets just return null
        return null;
    }


    private static List<SceneObjectInfo> LimitDataSize(List<SceneObjectInfo> objects, int maxSize)
    {
        if(objects.Count > maxSize)
        {
            Debug.LogWarning($"Data size is too large. Changing it to limit of {maxSize} objects.");
            return objects.GetRange(0, maxSize);
        }
        return objects;
    }


    //Gets the scene information as a json
    public static string GetSceneInfoAsJson()
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] allObjects = scene.GetRootGameObjects();
        List<SceneObjectInfo> sceneObjects = new List<SceneObjectInfo>();

        foreach (GameObject obj in allObjects)
        {
            sceneObjects.Add(GetObjectInfo(obj));
        }
        // Limit data size to 500 objects max
        sceneObjects = LimitDataSize(sceneObjects, 500);
        
        SceneInfo sceneInfo = new SceneInfo
        {
            sceneName = scene.name,
            unityVersion = Application.unityVersion,
            objects = sceneObjects
        };
        
        string json = JsonUtility.ToJson(sceneInfo, true);
        Debug.Log("Scene JSON:\n" + json);
        return json;
    }
    // Gets selected objects info as JSON
    public static string GetSelectedObjectsJson()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        Debug.Log($"Selected Objects Counter: {selectedObjects.Length}");

        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("No Objects Selected.");
            return "{}";
        }

        List<SceneObjectInfo> selectedList = new List<SceneObjectInfo>();
        foreach (GameObject obj in selectedObjects)
        {
            selectedList.Add(GetObjectInfo(obj));
        }
        // Limit the selected objects to 100 objects max
        selectedList = LimitDataSize(selectedList, 100);

        ObjectListWrapper wrapper = new ObjectListWrapper { objects = selectedList };
        string json = JsonUtility.ToJson(wrapper, true);
        Debug.Log("Selected Objects JSON:\n" + json);
        return json;
    }
    //Combines scene info and selected objects into into one JSON structure
    public static string GetCombinedJson()
    {
        string sceneJson = GetSceneInfoAsJson();
        string selectedObjectsJson = GetSelectedObjectsJson();

        SceneInfo sceneInfo = JsonUtility.FromJson<SceneInfo>(sceneJson);
        ObjectListWrapper selectedInfo = JsonUtility.FromJson<ObjectListWrapper>(selectedObjectsJson);

        if (sceneInfo == null || selectedInfo == null)
        {
            Debug.LogError("Deserialization failed!");
            return "{}";
        }
        

        CombinedInfo combinedInfo = new CombinedInfo
        {
            sceneName = sceneInfo.sceneName,
            unityVersion = sceneInfo.unityVersion,
            sceneObjects = sceneInfo.objects ?? new List<SceneObjectInfo>(),
            selectedObjects = selectedInfo.objects ?? new List<SceneObjectInfo>()
        };

        string combinedJson = JsonUtility.ToJson(combinedInfo, true);
        return combinedJson;
    }
    //Save the combined JSON to a file
    public static void SaveCombinedInfoToFile(string fileName)
    {
        string combinedJson = GetCombinedJson();

        if (combinedJson.Length > 50000)
        {
            Debug.LogWarning($"The combined json file is too large. It will not be saved");
            return;
        }


        string filePath = Path.Combine(Application.dataPath, fileName);
        File.WriteAllText(filePath, combinedJson);
        AssetDatabase.Refresh();
        Debug.Log($"JSON data saved to: {filePath}");
    }
 
    public static void SaveSceneAndSelectedObjectsInfoToFile(string fileName)
    {
        SaveCombinedInfoToFile(fileName);
    }
}
