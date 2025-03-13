using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class AiderConfigWindow
{
    private string[] models;
    private string yamlConfig;
    private Dictionary<string, object> yamlData;
    private string configFilePath =  Path.Combine(Application.dataPath, "../.aider.conf.yml");
    private string modelsPath = Path.Combine(Application.dataPath, "Backend/models.txt");
    private bool shown = false;


    VisualElement root;

    public async void Show()
    {
        Update();
        shown = true;
        await root.FadeIn(0.2f);
    }

    public async void Hide()
    {
        shown = false;
        await root.FadeOut(0.2f);
    }

    public void Toggle()
    {
        if (shown) Hide();
        else Show();
    }

    public AiderConfigWindow(VisualElement parent)
    {
        yamlData = parseYamlConfig();
        models = File.ReadAllLines(modelsPath);

        root = new();
        root.AddToClassList("config-window");
        Hide();
        parent.Add(root);

        Update();
    }

    public void Update()
    {
        root.Clear();

        var titleLabel = new Label("Aider Config") 
        { 
            style = 
            { 
                unityFontStyleAndWeight = FontStyle.Bold,
                fontSize = new StyleLength(new Length(16)),
                marginBottom = new StyleLength(new Length(10))
            }
        };
        root.Add(titleLabel);

        foreach (var pair in yamlData)
        {
            var key = pair.Key;
            var value = pair.Value;

            var keyWords = key.Split('-');
            string keyDisplayName = string.Join(" ", keyWords.Select(s => s.First().ToString().ToUpper() + s.Substring(1)));
            
            // get type and use to create the proper field
            if (key == "model") // model is a dropdown list
            {
                var selectedModel = models.ToList().IndexOf(value as string);
                var modelField = new PopupField<string>("Model", models.ToList(), selectedModel);
                var providerField = new TextField("Provider") { value = GetProviderFromModel(value as string) };
                providerField.SetEnabled(false);
                modelField.RegisterValueChangedCallback(evt => 
                {
                    yamlData[key] = evt.newValue;
                    yamlData["provider"] = GetProviderFromModel(evt.newValue);
                    providerField.value = yamlData["provider"] as string;
                });
                root.Add(modelField);
                root.Add(providerField);
            }
            else if (key == "api-key")
            {
                var split = (value as string).Split("=");
                var apiKey = split.Length > 1 ? split[1].Trim() : (value as string);
                var apiKeyField = new TextField("API Key") { value = apiKey };
                apiKeyField.RegisterValueChangedCallback(evt => yamlData[key] = $"{yamlData["provider"] as string} = {evt.newValue}");
                root.Add(apiKeyField);
            }
            else if (key == "provider")
            {
                continue;
            }
            else if (value is bool v)
            {
                var toggle = new Toggle(keyDisplayName) { value = v };
                toggle.RegisterValueChangedCallback(evt => yamlData[key] = evt.newValue);
                root.Add(toggle);
            }
            else if (value is int i)
            {
                var intField = new IntegerField(keyDisplayName) { value = i };
                intField.RegisterValueChangedCallback(evt => yamlData[key] = evt.newValue);
                root.Add(intField);
            }
            else if (value is float f)
            {
                var floatField = new FloatField(keyDisplayName) { value = f };
                floatField.RegisterValueChangedCallback(evt => yamlData[key] = evt.newValue);
                root.Add(floatField);
            }
            else if (value is string s)
            {
                var textField = new TextField(keyDisplayName) { value = s };
                textField.RegisterValueChangedCallback(evt => yamlData[key] = evt.newValue);
                root.Add(textField);
            }
            else
            {
                Debug.LogWarning($"Unsupported type for key {key}");
            }
        }

        var saveButton = new Button(() => SaveConfig()) { text = "Save" };
        saveButton.AddToClassList("save-button");
        root.Add(saveButton);
    }

    private string GetProviderFromModel(string modelName)
    {
        return modelName.Contains('/') ? modelName.Split('/')[0] : "";
    }

    private Dictionary<string, object> parseYamlConfig()
    { 
        // if config doesn't exist create it
        if (!File.Exists(configFilePath))
        {
            File.WriteAllText(configFilePath, "");
            return new Dictionary<string, object>();
        }

        string yamlContent = File.ReadAllText(configFilePath);

        Regex regex = new Regex(@"(.+):\s*(.+)");
        Dictionary<string, object> configData = new();
        
        foreach (Match match in regex.Matches(yamlContent))
        {
            var key = match.Groups[1].Value;
            var valueStr = match.Groups[2].Value;
            object value = valueStr;

            if (bool.TryParse(valueStr, out bool boolValue))
            {
                value = boolValue;
            }
            else if (int.TryParse(valueStr, out int intValue))
            {
                value = intValue;
            }
            else if (float.TryParse(valueStr, out float floatValue))
            {
                value = floatValue;
            }
            else if (valueStr == "null")
            {
                value = null;
            }

            configData[key] = value;
        }

        // if there is no model or api-key, add them
        if (!configData.ContainsKey("model"))
        {
            configData["model"] = "";
        }

        if (!configData.ContainsKey("api-key"))
        {
            configData["api-key"] = "";
        }


        return configData;
    }

    private string createYamlConfig()
    {
        yamlConfig = $"########################################\n" +
                     $"# Auto-Generated by Aider Unity Plugin #\n" +
                     $"########################################\n";
        foreach (KeyValuePair<string, object> option in yamlData)
        {
            yamlConfig += $"{option.Key}: {option.Value}\n";
        }

        return yamlConfig; 
    } 

    private void SaveConfig()
    {
        File.WriteAllText(configFilePath, createYamlConfig());
        Hide();
    }

}