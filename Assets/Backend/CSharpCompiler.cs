
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using System.Text;
using Assembly = System.Reflection.Assembly;
using System.Threading.Tasks;

public static class CSharpCompiler
{
    public static async Task<Assembly> CompileCode(string sourceCode, string className)
    {
        // Create a temporary assembly name
        string tempAssemblyName = "InMemoryAssembly_" + Guid.NewGuid().ToString("N");
        
        // We need to create a temporary file for compilation, but outside the Assets folder
        string tempDir = Path.Combine(Path.GetTempPath(), "UnityTempCompile");
        Directory.CreateDirectory(tempDir);
        string tempFile = Path.Combine(tempDir, $"{className}.cs");
        string tempDll = Path.Combine(tempDir, $"{tempAssemblyName}.dll");
        
        try
        {
            // Write code to temp file
            File.WriteAllText(tempFile, sourceCode);
            
            // Set up compilation options with Unity's pipeline
            var assemblyBuilder = new AssemblyBuilder(
                tempDll,
                new[] { tempFile }
            );
            
            // Add references to Unity assemblies
            string[] references = CompilationPipeline.GetPrecompiledAssemblyPaths(
                CompilationPipeline.PrecompiledAssemblySources.UnityEngine
            ).Concat(
                CompilationPipeline.GetPrecompiledAssemblyPaths(
                    CompilationPipeline.PrecompiledAssemblySources.UserAssembly
                )
            ).ToArray();
            
            assemblyBuilder.additionalReferences = references;
            
            // Compile the assembly
            if (!assemblyBuilder.Build())
            {
                throw new Exception("Compilation failed because Unity is laready compiling.");
            }

            bool finished = false;
            CompilerMessage[] messages = null;
            assemblyBuilder.buildFinished += (string assemblyPath, CompilerMessage[] msg) =>
            {
                messages = msg;
                finished = true;
            };

            while (!finished)
            {
                await Task.Delay(100);
            }

            var errors = messages.Where(m => m.type == CompilerMessageType.Error)
            .Select(m => $"Line {m.line}: {m.message}")
            .ToArray();

            if (errors.Length > 0)
            {
                StringBuilder sb = new StringBuilder("Compilation failed:\n");
                foreach (var error in errors)
                {
                    sb.AppendLine(error);
                }
                throw new Exception(sb.ToString());
            }
            
            // Load the assembly from the output path
            byte[] assemblyData = File.ReadAllBytes(tempDll);
            return Assembly.Load(assemblyData);
        }
        finally
        {
            // Clean up
            try
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
                if (File.Exists(tempDll)) File.Delete(tempDll);
                if (Directory.Exists(tempDir) && !Directory.EnumerateFileSystemEntries(tempDir).Any())
                    Directory.Delete(tempDir);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to clean up temp files: {ex.Message}");
            }
        }
    }
    
    public static object ExecuteMethod(Assembly assembly, string typeName, string methodName, params object[] parameters)
    {
        Type type = assembly.GetType(typeName);
        if (type == null)
            throw new Exception($"Type {typeName} not found in the compiled assembly.");
        
        MethodInfo method = type.GetMethod(methodName);
        if (method == null)
            throw new Exception($"Method {methodName} not found in type {typeName}.");
        
        if (method.IsStatic)
        {
            return method.Invoke(null, parameters);
        }
        else
        {
            var instance = Activator.CreateInstance(type);
            return method.Invoke(instance, parameters);
        }
    }

    public static async Task<object> ExecuteCommand(string code)
    {
        // remove lines of code starting with using
        var lines = code.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var filteredLines = lines.Where(line => !line.TrimStart().StartsWith("using")).ToArray();
        code = string.Join("\n", filteredLines);
        
        // Create a method that wraps the code
        string wrappedCode = $@"
            using UnityEngine;
            using UnityEditor;
            using System;
            using System.Linq;
            using System.Collections.Generic;
            using System.IO;
            using UnityEngine.SceneManagement;
            using UnityEngine.UIElements;
            using UnityEngine.UI;
            using UnityEngine.Events;

            public class CodeExecutor
            {{
                public static void Execute()
                {{
                    {code}
                }}
            }}
        ";

        var assembly = await CompileCode(wrappedCode, "CodeExecutor");
        var result = ExecuteMethod(assembly, "CodeExecutor", "Execute");
        return result;

    }

}
        