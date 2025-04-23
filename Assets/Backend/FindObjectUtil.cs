

using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// taken from: https://discussions.unity.com/t/how-do-i-use-gameobject-find-to-find-an-inactive-object/638045/16
public static class FindObjectUtil
{
    public static GameObject FindRootObjectWithName(string name) {
        return SceneManager.GetActiveScene()
            .GetRootGameObjects()
            .Where(obj => obj.name == name)
            .FirstOrDefault();
    }

    // Searches children, not descendants.
    public static Transform FindChildWithName(Transform parent, string query) {
        for (int i = 0; i < parent.transform.childCount; ++i) {
            var t = parent.transform.GetChild(i);
            if (t.name == query) {
                return t;
            }
        }
        return null;
    }

    // Like FindObjectUtil.FindObjectWithScenePath when using paths, but includes inactive.
    // Example: FindObjectWithScenePath("Geometry/building01/floor02")
    public static GameObject FindObjectWithScenePath(string path)
    {
        Debug.Assert(!string.IsNullOrWhiteSpace(path), "Must pass valid name");

        if (path[0] == '/') {
            path = path.Substring(1);
        }
        
        var names = path.Split('/');
        if (names.Length == 0) {
            Debug.Assert(false, "Path is invalid");
            return null;
        }

        var go = FindRootObjectWithName(names[0]);
        if (go == null) {
            return null;
        }

        var current = go.transform;
        foreach (var query in names.Skip(1)) {
            current = FindChildWithName(current, query);
            if (current == null) {
                return null;
            }
        }
        return current.gameObject;
    }
}