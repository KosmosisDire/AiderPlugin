using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.VersionControl;

[InitializeOnLoad]
public class AiderRunner
{
    static AiderConfig config;
    static Process aiderBridge;
    static Process aider;
    
    static AiderRunner()
    {
        config = AiderConfigManager.LoadConfig();
    }

    static Process RunPython(string pythonScriptPath)
    {
        // assumes python is available on the path (this may be a fine assumption to make?)
        Process pythonProcess = new()
        {
            StartInfo = new ProcessStartInfo("python", Path.Combine(Environment.CurrentDirectory, "Assets", pythonScriptPath))
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
        config = AiderConfigManager.LoadConfig();
        aiderBridge = RunPython(config.aiderBridgePath);
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
        config = AiderConfigManager.LoadConfig();
//        Environment.SetEnvironmentVariable($"{config.providerName.ToUpper().Replace(" ", "_")}_API_KEY", config.apiKey);
        Process aiderProcess = new()
        {
            StartInfo = new ProcessStartInfo(config.aiderCmd, config.aiderArgs)
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
