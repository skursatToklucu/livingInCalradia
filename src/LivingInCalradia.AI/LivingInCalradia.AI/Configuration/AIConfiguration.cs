using System;
using System.IO;
using System.Text.Json;

namespace LivingInCalradia.AI.Configuration;

/// <summary>
/// Configuration management for AI services.
/// Loads from JSON configuration file or environment variables.
/// </summary>
public sealed class AIConfiguration
{
    public string ApiKey { get; set; } = string.Empty;
    public string ModelId { get; set; } = "llama-3.1-70b-versatile";
    public string Provider { get; set; } = "Groq"; // "OpenAI" or "Groq"
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
    /// Falls back to environment variables if file doesn't exist.
    /// </summary>
    public static AIConfiguration Load(string configPath = "ai-config.json")
    {
        // Try multiple paths
        var paths = new[]
        {
            configPath,
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configPath),
            Path.Combine(Environment.CurrentDirectory, configPath)
        };
        
        foreach (var path in paths)
        {
            try
            {
                if (File.Exists(path))
                {
                    Console.WriteLine($"[Config] Found config at: {path}");
                    var json = File.ReadAllText(path);
                    
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    var config = JsonSerializer.Deserialize<AIConfiguration>(json, options);
                    if (config != null && !string.IsNullOrWhiteSpace(config.ApiKey))
                    {
                        Console.WriteLine($"[Config] Loaded successfully. Provider: {config.Provider}");
                        return config;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Config] Warning: Could not load from {path}: {ex.Message}");
            }
        }
        
        Console.WriteLine("[Config] No config file found, checking environment variables...");
        Console.WriteLine($"[Config] Current directory: {Environment.CurrentDirectory}");
        Console.WriteLine($"[Config] Base directory: {AppDomain.CurrentDomain.BaseDirectory}");
        
        // Fallback to environment variables
        var envApiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY") 
                     ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") 
                     ?? string.Empty;
        
        var provider = Environment.GetEnvironmentVariable("GROQ_API_KEY") != null ? "Groq" : "OpenAI";
        
        return new AIConfiguration
        {
            ApiKey = envApiKey,
            Provider = provider,
            ModelId = provider == "Groq" 
                ? "llama-3.1-70b-versatile" 
                : Environment.GetEnvironmentVariable("OPENAI_MODEL_ID") ?? "gpt-4",
            OrganizationId = Environment.GetEnvironmentVariable("OPENAI_ORG_ID")
        };
    }
    
    /// <summary>
    /// Validates that required configuration values are present.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new InvalidOperationException(
                "API Key is required. Set it in ai-config.json or GROQ_API_KEY/OPENAI_API_KEY environment variable.");
        }
    }
    
    /// <summary>
    /// Check if using Groq provider.
    /// </summary>
    public bool IsGroq => Provider?.Equals("Groq", StringComparison.OrdinalIgnoreCase) == true;
}
