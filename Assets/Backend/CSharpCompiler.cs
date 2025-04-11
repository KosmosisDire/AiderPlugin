
using System;
using System.Linq;
using UnityEngine;

public static class CSharpCompiler
{
    public static object ExecuteCommand(string code)
    {
        // Create a method that wraps the code
        string wrappedCode = $@"
            using UnityEngine;
            using UnityEditor;
            using System;
            using System.Linq;
            using System.Collections.Generic;

            public class CodeExecutor
            {{
                public static void Execute()
                {{
                    {code}
                }}
            }}
        ";

        var options = new System.CodeDom.Compiler.CompilerParameters
        {
            GenerateInMemory = true
        };
        
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            if (!assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
            {
                options.ReferencedAssemblies.Add(assembly.Location);
                Debug.Log($"Adding assembly reference: {assembly.Location}");
            }
        }

        options.ReferencedAssemblies.Add(typeof(UnityEngine.Object).Assembly.Location);
        options.ReferencedAssemblies.Add(typeof(UnityEditor.AssetDatabase).Assembly.Location);

        // Compile and execute
        using (var provider = new Microsoft.CSharp.CSharpCodeProvider())
        {
            var results = provider.CompileAssemblyFromSource(options, wrappedCode);
            if (results.Errors.HasErrors)
            {
                throw new Exception("Compilation failed: " + string.Join(", ", results.Errors.Cast<System.CodeDom.Compiler.CompilerError>().Select(e => e.ErrorText)));
            }

            var assembly = results.CompiledAssembly;
            var type = assembly.GetType("CodeExecutor");
            var method = type.GetMethod("Execute");
            return method.Invoke(null, null);
        }
    }

}
        