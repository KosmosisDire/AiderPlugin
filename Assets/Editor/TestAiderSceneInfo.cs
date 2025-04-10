using UnityEditor;
using UnityEngine;

public class AiderSceneInfoEditor : EditorWindow
{
    [MenuItem("Aider/Save Scene to JSON File")]
    public static void SaveSceneAndSelectedObjectsInfo()
    {
        string fileName = "scene_and_selected_objects_info.json"; // Json's filename
        AiderSceneInfo.SaveSceneAndSelectedObjectsInfoToFile(fileName);
    }
}
