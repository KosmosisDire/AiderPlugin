using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using Debug = UnityEngine.Debug;

public static class AiderRunner
{
    static Process aiderBridge;
    public static event Action OnNewAiderSessionStarted;

    // Method to check if the bridge process is running, and restart it if necessary
    public static bool EnsureAiderBridgeRunning()
    {
        if (aiderBridge == null || aiderBridge.HasExited)
        {
            // first try and refind process from editor prefs
            int pid = EditorPrefs.GetInt("Aider-BridgePID", -1);
            if (pid != -1)
            {
                try
                {
                    aiderBridge = Process.GetProcessById(pid);
                    if (!aiderBridge.HasExited)
                    {
                        Debug.Log("Found existing Aider Bridge process with PID: " + pid);
                        return true;
                    }
                }
                catch (Exception e)
                {
                    // Process not found, will start a new one
                    Debug.Log("Aider Bridge process not found with PID: " + pid + ", starting a new one. " + e.Message);
                }
            }
            
            Debug.Log("Aider Bridge is not running, starting it now.");
            aiderBridge = RunPython("Backend/aider-bridge.py");
            OnNewAiderSessionStarted?.Invoke();
            EditorPrefs.SetString("Aider-CurrentChat", "");
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

        // save PID to editor prefs
        EditorPrefs.SetInt("Aider-BridgePID", pythonProcess.Id);

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

}
