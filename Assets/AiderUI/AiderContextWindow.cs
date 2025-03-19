using UnityEngine;
using System.IO;
using UnityEditor;
public static class ProjectWindowContextMenu
{
    [MenuItem("Assets/Add to Aider Context", false, 20)]
    private static void AddtoContext()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        Client.AddFile(path);
        AiderChatWindow window = EditorWindow.GetWindow<AiderChatWindow>();
        window.contextList.Update(Client.GetContextList());
        Debug.Log($"Added {path} to the context");
    }

    [MenuItem("Assets/Add to Aider Context", true)]
    private static bool ValidateFile()
    {
        return Selection.activeObject != null && File.Exists(AssetDatabase.GetAssetPath(Selection.activeObject));
    }
}