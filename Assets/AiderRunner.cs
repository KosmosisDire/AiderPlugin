using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using Debug = UnityEngine.Debug;

[InitializeOnLoad]
public class AiderRunner
{
    static Process aiderBridge;
    static Process aider;
    static Process RunPython(string pythonScriptPath)
    {
        var path = Path.Combine(Environment.CurrentDirectory, "Assets", pythonScriptPath);
        Debug.Log($"Running python script at {path}");
        // assumes python is available on the path (this may be a fine assumption to make?)
        Process pythonProcess = new()
        {
            StartInfo = new ProcessStartInfo("python", path)
            {
                UseShellExecute = true,
                CreateNoWindow = false,
            }
        };

        pythonProcess.Start();

        return pythonProcess;
    }
    [MenuItem("Aider/Run Aider Bridge")]
    public static Process RunAiderBridge()
    {
        aiderBridge = RunPython("Backend/aider-bridge.py");
        return aiderBridge;
    }

    [MenuItem("Aider/Kill Aider Bridge")]
    public static void KillAiderBridge()
    {
        if (aiderBridge != null && !aiderBridge.HasExited)
        {
            aiderBridge.Kill();
        }
    }

    [MenuItem("Aider/Run Aider")]
    public static Process RunAider()
    {
        Process aiderProcess = new()
        {
            StartInfo = new ProcessStartInfo("aider")
            {
                UseShellExecute = true,
                CreateNoWindow = false
            }
        };

        aiderProcess.Start();

        return aiderProcess;
    }

    [MenuItem("Aider/Kill Aider")]
    public static void KillAider()
    {
        if (aider != null && !aider.HasExited)
        {
            aider.Kill();
        }
    }
}