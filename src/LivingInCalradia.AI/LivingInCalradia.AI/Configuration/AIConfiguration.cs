using System;
using System.IO;
using System.Reflection;

namespace LivingInCalradia.AI.Configuration;

/// <summary>
/// Configuration management for AI services.
/// Loads from JSON configuration file or environment variables.
/// </summary>
public sealed class AIConfiguration
{
    public string ApiKey { get; set; } = string.Empty;
    public string ModelId { get; set; } = "llama-3.1-8b-instant";
    public string Provider { get; set; } = "Groq";
    public string? OrganizationId { get; set; }
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 500;
    
    // Backward compatibility
    public string OpenAIApiKey 
    { 
        get => ApiKey; 
        set => ApiKey = value; 
    }

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
                    
                    if (config != null && !string.IsNullOrWhiteSpace(config.ApiKey))
                    {
                        Console.WriteLine($"[Config] Loaded successfully. Provider: {config.Provider}");
                        return config;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Config] Warning: {path}: {ex.Message}");
            }
        }
        
        Console.WriteLine("[Config] ERROR: No config file found!");
        
        // Return empty config - will fail validation
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
            // Simple JSON parsing
            config.ApiKey = ExtractJsonValue(json, "ApiKey") ?? "";
            config.Provider = ExtractJsonValue(json, "Provider") ?? "Groq";
            config.ModelId = ExtractJsonValue(json, "ModelId") ?? "llama-3.1-8b-instant";
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
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new InvalidOperationException(
                "API Key is required. Set it in ai-config.json file in the mod's bin folder.");
        }
    }
    
    /// <summary>
    /// Check if using Groq provider.
    /// </summary>
    public bool IsGroq => Provider?.Equals("Groq", StringComparison.OrdinalIgnoreCase) == true;
}
