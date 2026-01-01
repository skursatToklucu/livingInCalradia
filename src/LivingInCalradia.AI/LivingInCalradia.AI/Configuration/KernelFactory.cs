using System;
using System.Net.Http;
using LivingInCalradia.AI.Orchestration;
using LivingInCalradia.AI.Plugins;
using Microsoft.SemanticKernel;

namespace LivingInCalradia.AI.Configuration;

/// <summary>
/// Factory for building configured Semantic Kernel instances.
/// Follows builder pattern for clean initialization.
/// For OpenAI only - Groq uses GroqOrchestrator directly.
/// </summary>
public sealed class KernelFactory
{
    private string? _apiKey;
    private string? _modelId;
    private string? _orgId;
    
    /// <summary>
    /// Configure for OpenAI API.
    /// </summary>
    public KernelFactory WithOpenAI(string apiKey, string modelId = "gpt-4", string? organizationId = null)
    {
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _modelId = modelId;
        _orgId = organizationId;
        return this;
    }
    
    public Kernel Build()
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("API key must be configured before building the kernel.");
        }
        
        var builder = Kernel.CreateBuilder();
        
        builder.AddOpenAIChatCompletion(
            modelId: _modelId!,
            apiKey: _apiKey!,
            orgId: _orgId);
        
        // Register Bannerlord Actions Plugin
        builder.Plugins.AddFromType<BannerlordActionsPlugin>();
        
        return builder.Build();
    }
    
    /// <summary>
    /// Creates a pre-configured orchestrator instance.
    /// </summary>
    public SemanticKernelOrchestrator BuildOrchestrator()
    {
        var kernel = Build();
        return new SemanticKernelOrchestrator(kernel);
    }
}
