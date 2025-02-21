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
    static AiderRunner()
    {
        config = AiderConfigManager.LoadConfig();
    }

    static Process RunPython(string pythonScriptPath)
    {
        // assumes python is available on the path (this may be a fine assumption to make)
        Process pythonProcess = new()
        {
            StartInfo = new ProcessStartInfo("python", Path.Combine(Environment.CurrentDirectory, "Assets", pythonScriptPath))
            {
                UseShellExecute = true,
                CreateNoWindow = false
            }
        };

        pythonProcess.Start();

        return pythonProcess;
    }

    // add menu item to rerun the python script
    [MenuItem("Aider/Run Aider Bridge")]
    public static Process RunAiderBridge()
    {
        config = AiderConfigManager.LoadConfig();
        return RunPython(config.aiderBridgePath);
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

    public static List<string> GetAllModelNames()
    {
        Process process = new()
        {
            StartInfo = new ProcessStartInfo(config.aiderCmd, "--list-models /")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        List<string> modelNames = new();
        while (!process.StandardOutput.EndOfStream)
        {
            modelNames.Add(process.StandardOutput.ReadLine());
        }

        return modelNames;
    }
}
