using System;
using System.IO;
using System.Reflection;

namespace LivingInCalradia.AI.Configuration;

/// <summary>
/// Configuration management for AI services.
/// Loads from JSON configuration file or environment variables.
/// Supports multiple LLM providers: Groq, OpenAI, Anthropic, Azure, Ollama, OpenRouter, etc.
/// </summary>
public sealed class AIConfiguration
{
    // ========== SUPPORTED PROVIDERS =========
    
    /// <summary>
    /// Supported LLM providers and their default configurations
    /// </summary>
    public static class Providers
    {
        public const string Groq = "Groq";
        public const string OpenAI = "OpenAI";
        public const string Anthropic = "Anthropic";
        public const string Azure = "Azure";
        public const string Ollama = "Ollama";
        public const string OpenRouter = "OpenRouter";
        public const string Together = "Together";
        public const string Mistral = "Mistral";
        public const string DeepSeek = "DeepSeek";
        
        /// <summary>
        /// Get API base URL for provider
        /// </summary>
        public static string GetBaseUrl(string provider)
        {
            switch (provider?.ToLowerInvariant())
            {
                case "groq":
                    return "https://api.groq.com/openai/v1/";
                case "openai":
                    return "https://api.openai.com/v1/";
                case "anthropic":
                    return "https://api.anthropic.com/v1/";
                case "azure":
                    return ""; // Requires custom endpoint
                case "ollama":
                    return "http://localhost:11434/v1/";
                case "openrouter":
                    return "https://openrouter.ai/api/v1/";
                case "together":
                    return "https://api.together.xyz/v1/";
                case "mistral":
                    return "https://api.mistral.ai/v1/";
                case "deepseek":
                    return "https://api.deepseek.com/v1/";
                default:
                    return "https://api.groq.com/openai/v1/";
            }
        }
        
        /// <summary>
        /// Get default model for provider
        /// </summary>
        public static string GetDefaultModel(string provider)
        {
            switch (provider?.ToLowerInvariant())
            {
                case "groq":
                    return "llama-3.1-8b-instant";
                case "openai":
                    return "gpt-4o-mini";
                case "anthropic":
                    return "claude-3-haiku-20240307";
                case "ollama":
                    return "llama3.1";
                case "openrouter":
                    return "meta-llama/llama-3.1-8b-instruct:free";
                case "together":
                    return "meta-llama/Meta-Llama-3.1-8B-Instruct-Turbo";
                case "mistral":
                    return "mistral-small-latest";
                case "deepseek":
                    return "deepseek-chat";
                default:
                    return "llama-3.1-8b-instant";
            }
        }
        
        /// <summary>
        /// Check if provider uses OpenAI-compatible API
        /// </summary>
        public static bool IsOpenAICompatible(string provider)
        {
            switch (provider?.ToLowerInvariant())
            {
                case "anthropic":
                    return false; // Anthropic has different API format
                default:
                    return true; // Most providers are OpenAI-compatible
            }
        }
        
        /// <summary>
        /// Get all supported provider names
        /// </summary>
        public static string[] GetAll()
        {
            return new[] { Groq, OpenAI, Anthropic, Azure, Ollama, OpenRouter, Together, Mistral, DeepSeek };
        }
    }
    
    // ========== GLOBAL LOG STATE (Shared across all behaviors) =========
    // These static properties are used by all AI behaviors to check log state
    // Updated by BannerlordSubModule.ToggleAILogs()
    private static bool _globalThoughtLogsEnabled = true;
    private static bool _globalActionLogsEnabled = true;
    private static bool _globalDebugLogsEnabled = false;
    
    /// <summary>
    /// Global toggle for thought logs. Shared across all behaviors.
    /// </summary>
    public static bool GlobalThoughtLogsEnabled
    {
        get => _globalThoughtLogsEnabled;
        set => _globalThoughtLogsEnabled = value;
    }
    
    /// <summary>
    /// Global toggle for action logs. Shared across all behaviors.
    /// </summary>
    public static bool GlobalActionLogsEnabled
    {
        get => _globalActionLogsEnabled;
        set => _globalActionLogsEnabled = value;
    }
    
    /// <summary>
    /// Global toggle for debug logs. Shared across all behaviors.
    /// </summary>
    public static bool GlobalDebugLogsEnabled
    {
        get => _globalDebugLogsEnabled;
        set => _globalDebugLogsEnabled = value;
    }
    
    /// <summary>
    /// Sets all global log states at once. Called by BannerlordSubModule.
    /// </summary>
    public static void SetGlobalLogState(bool thoughtLogs, bool actionLogs, bool debugLogs = false)
    {
        _globalThoughtLogsEnabled = thoughtLogs;
        _globalActionLogsEnabled = actionLogs;
        _globalDebugLogsEnabled = debugLogs;
    }
    
    // ========== INSTANCE PROPERTIES =========
    
    public string ApiKey { get; set; } = string.Empty;
    public string ModelId { get; set; } = "llama-3.1-8b-instant";
    public string Provider { get; set; } = "Groq";
    
    /// <summary>
    /// Custom API base URL. If empty, uses default for the provider.
    /// Useful for Azure, self-hosted models, or proxies.
    /// </summary>
    public string ApiBaseUrl { get; set; } = string.Empty;
    
    public string? OrganizationId { get; set; }
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 500;
    
    // Logging & Display options (instance - for initial load, use Global* for runtime checks)
    public bool EnableThoughtLogs { get; set; } = true;
    public bool EnableActionLogs { get; set; } = true;
    public bool EnableDebugLogs { get; set; } = false;
    
    // Performance & Rate limiting - COST OPTIMIZED (~$2.50/day max)
    public int TickIntervalSeconds { get; set; } = 120;
    public int MaxLordsPerTick { get; set; } = 1;
    
    // Event-Driven AI - More conservative to reduce API costs
    public bool EnableEventDrivenAI { get; set; } = true;
    public int EventCooldownMinutes { get; set; } = 15;  // 10 ? 15 dakika
    
    // World AI - Enabled but slow for cost control
    public bool EnableWorldAI { get; set; } = true;
    public int WorldTickIntervalSeconds { get; set; } = 600;  // 10 dakika
    public int WorldMaxLordsPerTick { get; set; } = 1;
    public bool PrioritizeImportantLords { get; set; } = true;
    
    // Event filtering - Skip minor events
    public bool SkipMinorBattleEvents { get; set; } = true;
    public int MinimumBattleSize { get; set; } = 50;
    
    // Hotkey customization - Key names like "F1", "K", etc.
    // Set to empty string or "None" to disable
    // Insert is always the settings key by default (F10 conflicts with game)
    public string HotkeyShowSettings { get; set; } = "Insert";        // Settings panel (default: Insert)
    public string HotkeyFullProofTest { get; set; } = "";             // Disabled by default
    public string HotkeyTriggerAI { get; set; } = "";                 // Disabled by default
    public string HotkeyQuickTest { get; set; } = "";                 // Disabled by default
    public string HotkeyToggleLogs { get; set; } = "";                // Disabled by default
    public string HotkeyShowThoughts { get; set; } = "";              // Disabled by default

    // Backward compatibility
    public string OpenAIApiKey 
    { 
        get => ApiKey; 
        set => ApiKey = value; 
    }
    
    /// <summary>
    /// Gets the effective API base URL (custom or default for provider)
    /// </summary>
    public string GetEffectiveBaseUrl()
    {
        if (!string.IsNullOrWhiteSpace(ApiBaseUrl))
            return ApiBaseUrl;
        return Providers.GetBaseUrl(Provider);
    }
    
    /// <summary>
    /// Check if API key is configured
    /// </summary>
    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);

    /// <summary>
    /// Loads configuration from a JSON file.
    /// </summary>
    public static AIConfiguration Load(string configPath = "ai-config.json")
    {
        // Get the directory where the DLL is located
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDir = Path.GetDirectoryName(assemblyLocation) ?? "";
        
        Console.WriteLine($"[Config] Assembly dir: {assemblyDir}");
        
        // Try multiple paths including Bannerlord module paths
        var paths = new[]
        {
            // Same directory as DLL (Bannerlord module bin folder)
            Path.Combine(assemblyDir, configPath),
            // Parent directories (module root)
            Path.Combine(assemblyDir, "..", configPath),
            Path.Combine(assemblyDir, "..", "..", configPath),
            // Standard paths
            configPath,
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configPath),
            Path.Combine(Environment.CurrentDirectory, configPath),
            // Bannerlord specific paths
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Modules", "LivingInCalradia", "bin", "Win64_Shipping_Client", configPath),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Modules", "LivingInCalradia", configPath),
        };
        
        foreach (var path in paths)
        {
            try
            {
                var fullPath = Path.GetFullPath(path);
                Console.WriteLine($"[Config] Trying: {fullPath}");
                
                if (File.Exists(fullPath))
                {
                    Console.WriteLine($"[Config] Found config at: {fullPath}");
                    var json = File.ReadAllText(fullPath);
                    
                    // Parse JSON manually (avoid System.Text.Json issues with .NET 4.7.2)
                    var config = ParseConfigJson(json);
                    
                    if (config != null)
                    {
                        Console.WriteLine($"[Config] Loaded. Provider: {config.Provider}, HasKey: {config.HasApiKey}");
                        return config;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Config] Warning: {path}: {ex.Message}");
            }
        }
        
        Console.WriteLine("[Config] No config file found, using defaults");
        return new AIConfiguration();
    }
    
    /// <summary>
    /// Parse JSON config manually to avoid System.Text.Json issues
    /// </summary>
    private static AIConfiguration ParseConfigJson(string json)
    {
        var config = new AIConfiguration();
        
        try
        {
            config.ApiKey = ExtractJsonValue(json, "ApiKey") ?? "";
            config.Provider = ExtractJsonValue(json, "Provider") ?? "Groq";
            config.ModelId = ExtractJsonValue(json, "ModelId") ?? Providers.GetDefaultModel(config.Provider);
            config.ApiBaseUrl = ExtractJsonValue(json, "ApiBaseUrl") ?? "";
            config.OrganizationId = ExtractJsonValue(json, "OrganizationId");
            
            var tempStr = ExtractJsonValue(json, "Temperature");
            if (double.TryParse(tempStr, System.Globalization.NumberStyles.Any, 
                System.Globalization.CultureInfo.InvariantCulture, out var temp))
            {
                config.Temperature = temp;
            }
            
            var maxTokensStr = ExtractJsonValue(json, "MaxTokens");
            if (int.TryParse(maxTokensStr, out var maxTokens))
            {
                config.MaxTokens = maxTokens;
            }
            
            // Logging options
            var enableThoughtLogs = ExtractJsonValue(json, "EnableThoughtLogs");
            if (enableThoughtLogs != null)
                config.EnableThoughtLogs = enableThoughtLogs.Equals("true", StringComparison.OrdinalIgnoreCase);
            
            var enableActionLogs = ExtractJsonValue(json, "EnableActionLogs");
            if (enableActionLogs != null)
                config.EnableActionLogs = enableActionLogs.Equals("true", StringComparison.OrdinalIgnoreCase);
            
            var enableDebugLogs = ExtractJsonValue(json, "EnableDebugLogs");
            if (enableDebugLogs != null)
                config.EnableDebugLogs = enableDebugLogs.Equals("true", StringComparison.OrdinalIgnoreCase);
            
            // Performance options
            var tickIntervalStr = ExtractJsonValue(json, "TickIntervalSeconds");
            if (int.TryParse(tickIntervalStr, out var tickInterval) && tickInterval > 0)
                config.TickIntervalSeconds = tickInterval;
            
            var maxLordsStr = ExtractJsonValue(json, "MaxLordsPerTick");
            if (int.TryParse(maxLordsStr, out var maxLords) && maxLords > 0)
                config.MaxLordsPerTick = maxLords;
            
            // Event-Driven AI options
            var enableEventDriven = ExtractJsonValue(json, "EnableEventDrivenAI");
            if (enableEventDriven != null)
                config.EnableEventDrivenAI = enableEventDriven.Equals("true", StringComparison.OrdinalIgnoreCase);
            
            var eventCooldownStr = ExtractJsonValue(json, "EventCooldownMinutes");
            if (int.TryParse(eventCooldownStr, out var eventCooldown) && eventCooldown > 0)
                config.EventCooldownMinutes = eventCooldown;
            
            // World AI options
            var enableWorldAI = ExtractJsonValue(json, "EnableWorldAI");
            if (enableWorldAI != null)
                config.EnableWorldAI = enableWorldAI.Equals("true", StringComparison.OrdinalIgnoreCase);
            
            var worldTickStr = ExtractJsonValue(json, "WorldTickIntervalSeconds");
            if (int.TryParse(worldTickStr, out var worldTick) && worldTick > 0)
                config.WorldTickIntervalSeconds = worldTick;
            
            var worldMaxLordsStr = ExtractJsonValue(json, "WorldMaxLordsPerTick");
            if (int.TryParse(worldMaxLordsStr, out var worldMaxLords) && worldMaxLords > 0)
                config.WorldMaxLordsPerTick = worldMaxLords;
            
            var prioritizeStr = ExtractJsonValue(json, "PrioritizeImportantLords");
            if (prioritizeStr != null)
                config.PrioritizeImportantLords = prioritizeStr.Equals("true", StringComparison.OrdinalIgnoreCase);
            
            // Event filtering options
            var skipMinorEventsStr = ExtractJsonValue(json, "SkipMinorBattleEvents");
            if (skipMinorEventsStr != null)
                config.SkipMinorBattleEvents = skipMinorEventsStr.Equals("true", StringComparison.OrdinalIgnoreCase);
            
            var minBattleSizeStr = ExtractJsonValue(json, "MinimumBattleSize");
            if (int.TryParse(minBattleSizeStr, out var minBattleSize) && minBattleSize > 0)
                config.MinimumBattleSize = minBattleSize;
            
            // Hotkey customization
            var hotkeyFullProof = ExtractJsonValue(json, "HotkeyFullProofTest");
            if (!string.IsNullOrEmpty(hotkeyFullProof))
                config.HotkeyFullProofTest = hotkeyFullProof;
            
            var hotkeyTriggerAI = ExtractJsonValue(json, "HotkeyTriggerAI");
            if (!string.IsNullOrEmpty(hotkeyTriggerAI))
                config.HotkeyTriggerAI = hotkeyTriggerAI;
            
            var hotkeyQuickTest = ExtractJsonValue(json, "HotkeyQuickTest");
            if (!string.IsNullOrEmpty(hotkeyQuickTest))
                config.HotkeyQuickTest = hotkeyQuickTest;
            
            var hotkeyToggleLogs = ExtractJsonValue(json, "HotkeyToggleLogs");
            if (!string.IsNullOrEmpty(hotkeyToggleLogs))
                config.HotkeyToggleLogs = hotkeyToggleLogs;
            
            var hotkeyShowThoughts = ExtractJsonValue(json, "HotkeyShowThoughts");
            if (!string.IsNullOrEmpty(hotkeyShowThoughts))
                config.HotkeyShowThoughts = hotkeyShowThoughts;
            
            var hotkeyShowSettings = ExtractJsonValue(json, "HotkeyShowSettings");
            if (!string.IsNullOrEmpty(hotkeyShowSettings))
                config.HotkeyShowSettings = hotkeyShowSettings;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Config] Parse error: {ex.Message}");
        }
        
        return config;
    }
    
    /// <summary>
    /// Extract a value from JSON by key name
    /// </summary>
    private static string? ExtractJsonValue(string json, string key)
    {
        // Find "key": "value" or "key": value
        var searchKey = $"\"{key}\"";
        var keyIndex = json.IndexOf(searchKey, StringComparison.OrdinalIgnoreCase);
        
        if (keyIndex == -1)
            return null;
        
        // Find the colon after the key
        var colonIndex = json.IndexOf(':', keyIndex + searchKey.Length);
        if (colonIndex == -1)
            return null;
        
        // Skip whitespace
        var valueStart = colonIndex + 1;
        while (valueStart < json.Length && char.IsWhiteSpace(json[valueStart]))
            valueStart++;
        
        if (valueStart >= json.Length)
            return null;
        
        // Check if value is quoted string
        if (json[valueStart] == '"')
        {
            valueStart++; // Skip opening quote
            var valueEnd = valueStart;
            
            // Find closing quote (handle escaped quotes)
            while (valueEnd < json.Length)
            {
                if (json[valueEnd] == '"' && (valueEnd == 0 || json[valueEnd - 1] != '\\'))
                    break;
                valueEnd++;
            }
            
            return json.Substring(valueStart, valueEnd - valueStart);
        }
        else
        {
            // Unquoted value (number, boolean, null)
            var valueEnd = valueStart;
            while (valueEnd < json.Length && json[valueEnd] != ',' && json[valueEnd] != '}' && !char.IsWhiteSpace(json[valueEnd]))
                valueEnd++;
            
            return json.Substring(valueStart, valueEnd - valueStart);
        }
    }
    
    /// <summary>
    /// Validates that required configuration values are present.
    /// Returns false if API key is missing (doesn't throw).
    /// </summary>
    public bool TryValidate(out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            errorMessage = "API Key is required. Please configure it in Settings.";
            return false;
        }
        errorMessage = "";
        return true;
    }
    
    /// <summary>
    /// Validates that required configuration values are present.
    /// Throws if invalid.
    /// </summary>
    public void Validate()
    {
        if (!TryValidate(out var error))
        {
            throw new InvalidOperationException(error);
        }
    }
    
    /// <summary>
    /// Check if using Groq provider.
    /// </summary>
    public bool IsGroq => Provider?.Equals("Groq", StringComparison.OrdinalIgnoreCase) == true;
    
    /// <summary>
    /// Check if provider uses OpenAI-compatible API
    /// </summary>
    public bool IsOpenAICompatible => Providers.IsOpenAICompatible(Provider);
}
