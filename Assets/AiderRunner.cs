using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using Debug = UnityEngine.Debug;

[InitializeOnLoad]
public class AiderRunner
{
    static Process aiderBridge;
    static Process aider;

    // Method to check if the bridge process is running, and restart it if necessary
    public static async Task<bool> EnsureAiderBridgeRunning()
    {
        if (aiderBridge == null || aiderBridge.HasExited)
        {
            Debug.Log("Aider Bridge is not running, starting it now.");
            aiderBridge = RunPython("Backend/aider-bridge.py");
            await Task.Delay(1000); // Wait for the bridge to start
            return true;
        }
        return false;
    }

    // Runs the Python script for the bridge
    static Process RunPython(string pythonScriptPath)
    {
        var path = Path.Combine(Environment.CurrentDirectory, "Assets", pythonScriptPath);

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
    public static async Task<Process> RunAiderBridge()
    {
        return await EnsureAiderBridgeRunning() ? aiderBridge : null;
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
