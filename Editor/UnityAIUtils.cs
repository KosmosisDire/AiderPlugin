using UnityEditor.PackageManager;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class UnityAIUtils
{
    public static readonly string packageName = "com.kosmosisdire.unity-ai";
    public static string GetPath(string path)
    {
        var newPath = Path.Combine($"{UnityEditor.PackageManager.PackageInfo.FindForPackageName(packageName).resolvedPath}/Editor", path);
        Debug.Log($"Package Name: {newPath}");
        return newPath;
    }
}