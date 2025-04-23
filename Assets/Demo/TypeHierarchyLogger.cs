using UnityEngine;
using System;
using System.Text; // For StringBuilder

public static class TypeHierarchyLogger
{
    // Logs the full inheritance hierarchy of a given type.
    public static void LogHierarchy(Type type)
    {
        if (type == null)
        {
            Debug.LogError("Cannot log hierarchy for a null type.");
            return;
        }

        StringBuilder hierarchy = new StringBuilder();
        hierarchy.AppendLine($"Inheritance hierarchy for: {type.FullName}");

        Type currentType = type;
        int depth = 0;
        while (currentType != null)
        {
            hierarchy.Append(new string(' ', depth * 2)); // Indentation
            hierarchy.AppendLine($"- {currentType.FullName}");

            // Stop if we reach System.Object or if BaseType is null
            if (currentType == typeof(object) || currentType.BaseType == null)
            {
                break;
            }

            currentType = currentType.BaseType;
            depth++;
        }

        Debug.Log(hierarchy.ToString());
    }

    // Example usage method that can be called via executeCode
    public static void LogTextMeshProHierarchy()
    {
        // Attempt to get the type, specifying the assembly might be needed
        Type tmproType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
        if (tmproType == null)
        {
             Debug.LogError("Could not find type TMPro.TextMeshProUGUI. Ensure TextMeshPro package is imported.");
             // Fallback or try without assembly hint if needed, though less reliable
             tmproType = Type.GetType("TMPro.TextMeshProUGUI");
        }

        if (tmproType != null)
        {
            LogHierarchy(tmproType);
        }
         else
        {
             Debug.LogError("Failed to get type TMPro.TextMeshProUGUI even after trying alternatives.");
        }
    }
}
