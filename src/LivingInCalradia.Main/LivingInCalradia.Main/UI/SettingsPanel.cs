using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using LivingInCalradia.AI.Configuration;

namespace LivingInCalradia.Main.UI;

public static class SettingsPanel
{
    private static AIConfiguration? _config;
    private static Action? _onConfigChanged;
    private static Action? _onProviderChanged;
    
    private static readonly string[] Providers = new []
    {
        "Groq", "OpenRouter", "Ollama", "OpenAI", "Anthropic", "Together", "Mistral", "DeepSeek"
    };
    
    public static void Initialize(AIConfiguration config, Action? onConfigChanged = null, Action? onProviderChanged = null)
    {
        _config = config;
        _onConfigChanged = onConfigChanged;
        _onProviderChanged = onProviderChanged;
    }
    
    public static void ShowPanel()
    {
        if (_config == null) return;
        
        var logsStatus = _config.EnableThoughtLogs ? "ON" : "OFF";
        var text = $"Provider: {_config.Provider}\n" +
                   $"Model: {_config.ModelId}\n" +
                   $"API Key: {GetApiKeyDisplay()}\n" +
                   $"Logs: {logsStatus}";
        
        InformationManager.ShowInquiry(
            new InquiryData(
                titleText: "Living in Calradia",
                text: text,
                isAffirmativeOptionShown: true,
                isNegativeOptionShown: true,
                affirmativeText: "Provider",
                negativeText: "API Key",
                affirmativeAction: CycleProvider,
                negativeAction: ShowApiKeyInput,
                soundEventPath: ""
            ),
            pauseGameActiveState: false
        );
    }
    
    private static void CycleProvider()
    {
        if (_config == null) return;
        
        var currentIndex = Array.IndexOf(Providers, _config.Provider);
        var nextIndex = (currentIndex + 1) % Providers.Length;
        var nextProvider = Providers[nextIndex];
        
        _config.Provider = nextProvider;
        _config.ModelId = AIConfiguration.Providers.GetDefaultModel(nextProvider);
        _config.ApiBaseUrl = AIConfiguration.Providers.GetBaseUrl(nextProvider);
        
        if (nextProvider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
        {
            _config.ApiKey = "ollama";
        }
        
        _onConfigChanged?.Invoke();
        _onProviderChanged?.Invoke();
        
        InformationManager.DisplayMessage(new InformationMessage(
            $"[LivingInCalradia] {nextProvider} / {_config.ModelId}",
            Colors.Green));
        
        ShowPanel();
    }
    
    private static void ShowApiKeyInput()
    {
        if (_config == null) return;
        
        InformationManager.ShowTextInquiry(
            new TextInquiryData(
                titleText: "API Key",
                text: "",
                isAffirmativeOptionShown: true,
                isNegativeOptionShown: true,
                affirmativeText: "Save",
                negativeText: "Cancel",
                affirmativeAction: OnApiKeySaved,
                negativeAction: null,
                shouldInputBeObfuscated: false
            ),
            pauseGameActiveState: false
        );
    }
    
    private static void OnApiKeySaved(string apiKey)
    {
        if (_config == null) return;
        
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            _config.ApiKey = apiKey.Trim();
            
            _onConfigChanged?.Invoke();
            _onProviderChanged?.Invoke();
            
            InformationManager.DisplayMessage(new InformationMessage(
                "[LivingInCalradia] API Key saved",
                Colors.Green));
            
            ShowModelInput();
        }
    }
    
    private static void ShowModelInput()
    {
        if (_config == null) return;
        
        InformationManager.ShowTextInquiry(
            new TextInquiryData(
                titleText: "Model",
                text: $"Current: {_config.ModelId}",
                isAffirmativeOptionShown: true,
                isNegativeOptionShown: true,
                affirmativeText: "Save",
                negativeText: "Skip",
                affirmativeAction: OnModelSaved,
                negativeAction: null,
                shouldInputBeObfuscated: false
            ),
            pauseGameActiveState: false
        );
    }
    
    private static void OnModelSaved(string model)
    {
        if (_config == null) return;
        
        if (!string.IsNullOrWhiteSpace(model))
        {
            _config.ModelId = model.Trim();
            _onConfigChanged?.Invoke();
            _onProviderChanged?.Invoke();
            
            InformationManager.DisplayMessage(new InformationMessage(
                $"[LivingInCalradia] Model: {_config.ModelId}",
                Colors.Green));
        }
    }
    
    private static string GetApiKeyDisplay()
    {
        if (_config == null || string.IsNullOrEmpty(_config.ApiKey))
            return "(not set)";
        
        if (_config.ApiKey.Length < 8)
            return "(set)";
        
        return _config.ApiKey.Substring(0, 4) + "..." + _config.ApiKey.Substring(_config.ApiKey.Length - 4);
    }
    
    public static void ShowApiKeyMissingWarning()
    {
        if (_config == null) return;
        
        InformationManager.ShowInquiry(
            new InquiryData(
                titleText: "Living in Calradia",
                text: "API Key required",
                isAffirmativeOptionShown: true,
                isNegativeOptionShown: false,
                affirmativeText: "Setup",
                negativeText: "",
                affirmativeAction: ShowApiKeyInput,
                negativeAction: null,
                soundEventPath: ""
            ),
            pauseGameActiveState: false
        );
    }
    
    public static void ShowQuickStatus()
    {
        if (_config == null) return;
        
        if (!_config.HasApiKey)
        {
            InformationManager.DisplayMessage(new InformationMessage(
                "[LivingInCalradia] [Insert] Setup",
                Colors.Yellow));
        }
        else
        {
            InformationManager.DisplayMessage(new InformationMessage(
                $"[LivingInCalradia] {_config.Provider} Ready",
                Colors.Green));
        }
    }
}
