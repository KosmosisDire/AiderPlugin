using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using Debug = UnityEngine.Debug;

public class PythonRunner
{
    static Process Run(string pythonScriptPath)
    {
        // assumes python is available in the path (this may be a fine assumption to make)
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
    [MenuItem("Python/Run Aider Bridge")]
    public static Process RunAiderBridge()
    {
        return Run("Backend/aider-bridge.py");
    }

    [MenuItem("Python/Run Aider")]
    public static Process RunAider()
    {
        // assumes python is available in the path (this may be a fine assumption to make)
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
}
