


using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class UnityAIUtils
{
    public static readonly string packageName = "com.kosmosisdire.unity-ai";

    public static string GetPath(string path)
    {
        var newPath = Path.Combine($"Packages/{packageName}/Editor", path);
        return newPath;
    }
}