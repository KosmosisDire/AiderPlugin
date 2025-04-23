using UnityEngine;
using System.IO;
using UnityEditor;
using System.Threading.Tasks;
public static class ProjectWindowContextMenu
{
    [MenuItem("Assets/Add to Aider Context", false, 20)]
    private static async Task AddtoContext()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        await Client.AddFile(path);
        AiderChatWindow window = EditorWindow.GetWindow<AiderChatWindow>();
        window.contextList.Update(await Client.GetContextList());
        Debug.Log($"Added {path} to the context");
    }

    [MenuItem("Assets/Add to Aider Context", true)]
    private static bool ValidateFile()
    {
        return Selection.activeObject != null && File.Exists(AssetDatabase.GetAssetPath(Selection.activeObject));
    }
}