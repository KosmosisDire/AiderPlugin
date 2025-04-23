using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;


// solution found at https://discussions.unity.com/t/determine-built-in-editor-menu-items/85929
public static class MenuItemsUtility
{
    public static List<string> GetMenuItems(string menuPath)
    {
        List<string> result = new();
        
        try
        {
            string menuStructure = EditorGUIUtility.SerializeMainMenuToString();
            result = ParseMenuItems(menuStructure, menuPath);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error accessing menu structure: {e.Message}");
        }
        
        return result;
    }

    public static List<string> GetAllMenuItems()
    {
        return GetMenuItems(string.Empty);
    }
    
    public static void ExecuteMenuItem(string menuPath)
    {
        try
        {
            EditorApplication.ExecuteMenuItem(menuPath);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to execute menu item '{menuPath}': {e.Message}");
        }
    }
    
    private static List<string> ParseMenuItems(string menuStructure, string targetMenuPath)
    {
        List<string> foundMenuItems = new List<string>();
        
        if (string.IsNullOrEmpty(menuStructure))
            return foundMenuItems;
            
        string[] lines = menuStructure.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        Dictionary<int, string> menuLevels = new Dictionary<int, string>();
        bool insideTargetMenu = string.IsNullOrEmpty(targetMenuPath);
        int targetLevel = -1;
        
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;
                
            int indentLevel = CountLeadingSpaces(line) / 2;
            string menuName = ExtractMenuName(line.Trim());
            
            if (string.IsNullOrEmpty(menuName))
                continue;
                
            // If we're back to a level above our target, we're no longer in the target menu
            if (insideTargetMenu && targetLevel >= 0 && indentLevel <= targetLevel)
            {
                insideTargetMenu = false;
            }
            
            // Update menu levels dictionary
            menuLevels[indentLevel] = menuName;
            for (int level = indentLevel + 1; level < 10; level++)
            {
                menuLevels.Remove(level);
            }
            
            // Check if this is our target menu (must be top-level)
            if (!insideTargetMenu && indentLevel == 0 && 
                string.Equals(menuName, targetMenuPath, StringComparison.OrdinalIgnoreCase))
            {
                insideTargetMenu = true;
                targetLevel = indentLevel;
                continue; // Skip adding the parent menu itself
            }
            
            // If we're inside the target menu, add this item to our results
            if (insideTargetMenu && indentLevel > targetLevel)
            {
                string fullPath = BuildMenuPath(menuLevels, indentLevel);
                foundMenuItems.Add(fullPath);
            }
        }
        
        return foundMenuItems;
    }

    private static string BuildMenuPath(Dictionary<int, string> menuLevels, int currentLevel)
    {
        List<string> parts = new();
        
        for (int level = 0; level <= currentLevel; level++)
        {
            if (menuLevels.TryGetValue(level, out string menuName))
                parts.Add(menuName);
        }
        
        return string.Join("/", parts.ToArray());
    }
    
    private static int CountLeadingSpaces(string line)
    {
        int count = 0;
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == ' ')
                count++;
            else
                break;
        }
        return count;
    }
    
    private static string ExtractMenuName(string line)
    {
        // Remove keyboard shortcut
        string withoutShortcut = Regex.Replace(line, @"\s+(?:Ctrl|Alt|Shift|\+|F\d+)+.*$", "");
        
        // Remove menu indicators
        string withoutAmpersand = withoutShortcut.Replace("&", "");
        
        // Remove trailing dots
        string result = withoutAmpersand.TrimEnd('.', ' ');
        
        return result.Trim();
    }
}