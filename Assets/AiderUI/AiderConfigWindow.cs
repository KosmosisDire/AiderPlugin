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
    private string provider;
    private string yamlConfig;
    private Dictionary<string, object> yamlData;
    private string configFilePath =  Path.Combine(Application.dataPath, "../.aider.conf.yml");
    private bool shown = false;

    public static readonly Dictionary<string, string> models = new Dictionary<string, string>
    {
        ["openai/gpt-4"] = "GPT-4",
        ["openai/gpt-4o"] = "GPT-4o",
        ["openai/gpt-4-turbo"] = "GPT-4 Turbo",
        ["openai/gpt-3.5-turbo"] = "GPT-3.5 Turbo",
        ["openai/o1"] = "O1", 
        ["openai/o1-mini"] = "O1 Mini",
        ["openai/o3-mini"] = "O3 Mini",
        ["anthropic/claude-3-5-sonnet-20241022"] = "Claude 3.5 Sonnet",
        ["anthropic/claude-3-5-haiku-20241022"] = "Claude 3.5 Haiku",
        ["anthropic/claude-3-7-sonnet-20250219"] = "Claude 3.7 Sonnet",
        ["meta-llama/Meta-Llama-3-70B-Instruct"] = "Llama 3 70B",
        ["meta-llama/Meta-Llama-3-8B-Instruct"] = "Llama 3 8B",
        ["meta-llama/Meta-Llama-3.1-70B-Instruct"] = "Llama 3.1 70B",
        ["meta-llama/Meta-Llama-3.1-405B-Instruct"] = "Llama 3.1 405B",
        ["mistral/mistral-large-latest"] = "Mistral Large",
        ["mistral/mistral-medium-latest"] = "Mistral Medium",
        ["mistral/mistral-small-latest"] = "Mistral Small",
        ["mistral/codestral-latest"] = "Codestral",
        ["mistral/open-mixtral-8x7b"] = "Mixtral 8x7B",
        ["gemini/gemini-1.5-pro"] = "Gemini 1.5 Pro",
        ["gemini/gemini-1.5-flash"] = "Gemini 1.5 Flash",
        ["gemini/gemini-2.0-pro-exp-02-05"] = "Gemini 2.0 Pro",
        ["gemini/gemini-2.0-flash"] = "Gemini 2.0 Flash",
        ["bedrock/anthropic.claude-3-sonnet-20240229-v1:0"] = "Claude 3 Sonnet (Bedrock)",
        ["bedrock/anthropic.claude-3-opus-20240229-v1:0"] = "Claude 3 Opus (Bedrock)",
        ["bedrock/anthropic.claude-3-haiku-20240307-v1:0"] = "Claude 3 Haiku (Bedrock)",
        ["bedrock/meta.llama3-70b-instruct-v1:0"] = "Llama 3 70B (Bedrock)",
        ["bedrock/cohere.command-r-plus-v1:0"] = "Command R+ (Bedrock)",
        ["bedrock/mistral.mistral-large-2402-v1:0"] = "Mistral Large (Bedrock)",
        ["azure/gpt-4o"] = "GPT-4o (Azure)",
        ["azure/gpt-4"] = "GPT-4 (Azure)",
        ["azure/o1"] = "O1 (Azure)",
        ["cohere_chat/command-r-plus"] = "Command R+",
        ["cohere_chat/command-r"] = "Command R",
        ["groq/llama-3.1-70b-versatile"] = "Llama 3.1 70B (Groq)",
        ["groq/llama-3.1-405b-reasoning"] = "Llama 3.1 405B (Groq)",
        ["groq/mixtral-8x7b-32768"] = "Mixtral 8x7B (Groq)",
        ["deepseek/deepseek-coder"] = "DeepSeek Coder",
        ["deepseek/deepseek-chat"] = "DeepSeek Chat",
        ["xai/grok-2-latest"] = "Grok 2",
        ["together_ai/meta-llama/Meta-Llama-3.1-405B-Instruct-Turbo"] = "Llama 3.1 405B Turbo (Together AI)",
        ["perplexity/sonar-small-chat"] = "Sonar Small (Perplexity)"
    };


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
        yamlData = ParseYamlConfig();

        root = new();
        root.AddToClassList("config-window");
        Hide();
        parent.Add(root);

        Update();
    }

    public void Update()
    {
        yamlData = ParseYamlConfig();
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

        Label editTip = new("You may also modify the config file at <b>.aider.conf.yml</b> in your project root (outside Assets).");
        editTip.style.whiteSpace = WhiteSpace.PreWrap;
        editTip.style.marginBottom = 15;
        editTip.AddToClassList("tip");
        root.Add(editTip);

        foreach (var pair in yamlData)
        {
            var key = pair.Key;
            var value = pair.Value;

            var keyWords = key.Split('-');
            string keyDisplayName = string.Join(" ", keyWords.Select(s => s.First().ToString().ToUpper() + s.Substring(1)));
            
            // get type and use to create the proper field
            if (key == "model") // model is a dropdown list
            {
                var selectedModel = models.FirstOrDefault(m => m.Key == value as string).Key ?? models.Keys.First();
                var modelField = new PopupField<string>("Model", models.Keys.ToList(), selectedModel, 
                (input) => models[input], (input) => models[input]);
                var providerField = new TextField("Provider") { value = GetProviderFromModel(value as string) };
                providerField.SetEnabled(false);
                modelField.RegisterValueChangedCallback(evt => 
                {
                    yamlData[key] = evt.newValue;
                    provider = GetProviderFromModel(evt.newValue);
                    Debug.Log(provider);
                    yamlData["api-key"] = $"{provider} = {GetAPIKeyFromValue(yamlData["api-key"] as string)}";
                    providerField.value = provider;
                });
                root.Add(modelField);
                root.Add(providerField);
            }
            else if (key == "api-key")
            {
                var apiKey = GetAPIKeyFromValue(value as string);
                var apiKeyField = new TextField("API Key") { value = apiKey };
                apiKeyField.RegisterValueChangedCallback(evt => yamlData[key] = $"{provider} = {evt.newValue}");
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

    private string GetAPIKeyFromValue(string value)
    {
        var split = (value as string).Split("=");
        var apiKey = split.Length > 1 ? split[1].Trim() : (value as string);
        return apiKey;
    }

    private Dictionary<string, object> ParseYamlConfig()
    { 
        // if config doesn't exist create it
        if (!File.Exists(configFilePath))
        {
            File.WriteAllText(configFilePath, "");
            return new Dictionary<string, object>();
        }

        string yamlContent = File.ReadAllText(configFilePath);

        Regex regex = new Regex(@"^(?!#)(.+):\s*(.+)", RegexOptions.Multiline);
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

    private string CreateYamlConfig()
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
        File.WriteAllText(configFilePath, CreateYamlConfig());
        Hide();
    }

}