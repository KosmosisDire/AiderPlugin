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

    // Method to check if the bridge process is running, and restart it if necessary
    public static bool EnsureAiderBridgeRunning()
    {
        if (aiderBridge == null || aiderBridge.HasExited)
        {
            Debug.Log("Aider Bridge is not running, starting it now.");
            aiderBridge = RunPython("Backend/aider-bridge.py");
            return true;
        }
        return false;
    }

    // Runs the Python script for the bridge
    static Process RunPython(string pythonScriptPath)
    {
        var path = Path.Combine(Environment.CurrentDirectory, "Assets", pythonScriptPath);
        Debug.Log($"Running python script at {path}");
        string pythonPath = @"C:\Users\Lupil\AppData\Local\Programs\Python\Python310\python.exe";//specified path here

        Process pythonProcess = new()
        {
            StartInfo = new ProcessStartInfo(pythonPath, path)//changed python to pythonPath
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
        return EnsureAiderBridgeRunning() ? aiderBridge : null;
    }

    // Kill the Aider Bridge process
    [MenuItem("Aider/Kill Aider Bridge")]
    public static void KillAiderBridge()
    {
        if (aiderBridge != null && !aiderBridge.HasExited)
        {
            aiderBridge.Kill();
        }
    }

    // Start Aider process
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
