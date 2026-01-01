using System;
using System.Threading.Tasks;
using LivingInCalradia.AI.Configuration;
using LivingInCalradia.AI.Orchestration;
using LivingInCalradia.Core.Application.Interfaces;
using LivingInCalradia.Core.Application.Services;
using LivingInCalradia.Infrastructure.Execution;
using LivingInCalradia.Infrastructure.Sensors;

namespace LivingInCalradia.Main;

/// <summary>
/// Main entry point for the Living in Calradia mod.
/// Initializes the AI system and orchestrates agent workflows.
/// </summary>
public sealed class LivingInCalradiaSubModule
{
    private AgentWorkflowService? _workflowService;
    private bool _isInitialized;
    
    /// <summary>
    /// Initializes the AI system with all dependencies.
    /// This should be called during mod initialization.
    /// </summary>
    public void Initialize()
    {
        try
        {
            Console.WriteLine("[Living in Calradia] Initializing AI system...");
            
            // Load configuration
            var config = AIConfiguration.Load();
            config.Validate();
            
            Console.WriteLine($"[Living in Calradia] Using provider: {config.Provider}");
            Console.WriteLine($"[Living in Calradia] Model: {config.ModelId}");
            
            // Create orchestrator based on provider
            IAgentOrchestrator orchestrator;
            
            if (config.IsGroq)
            {
                // Use direct Groq orchestrator (bypasses SK for custom endpoint)
                orchestrator = new GroqOrchestrator(config.ApiKey, config.ModelId, config.Temperature);
                Console.WriteLine("[Living in Calradia] Using GroqOrchestrator (direct API)");
            }
            else
            {
                // Use Semantic Kernel for OpenAI
                var kernelFactory = new KernelFactory()
                    .WithOpenAI(config.ApiKey, config.ModelId, config.OrganizationId);
                orchestrator = kernelFactory.BuildOrchestrator();
                Console.WriteLine("[Living in Calradia] Using SemanticKernelOrchestrator");
            }
            
            // Create infrastructure components
            var worldSensor = new MockWorldSensor();
            var actionExecutor = new MockActionExecutor();
            
            // Create workflow service
            _workflowService = new AgentWorkflowService(
                worldSensor,
                orchestrator,
                actionExecutor);
            
            _isInitialized = true;
            Console.WriteLine("[Living in Calradia] AI system initialized successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Living in Calradia] Failed to initialize: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Executes the AI workflow for a specific agent.
    /// This can be called from Bannerlord's tick system or event handlers.
    /// </summary>
    public async Task ExecuteAgentThinkingAsync(string agentId)
    {
        if (!_isInitialized || _workflowService == null)
        {
            Console.WriteLine("[Living in Calradia] AI system not initialized!");
            return;
        }
        
        try
        {
            Console.WriteLine($"\n[Living in Calradia] Agent {agentId} is thinking...");
            
            var result = await _workflowService.ExecuteWorkflowAsync(agentId);
            
            if (result.IsSuccessful)
            {
                Console.WriteLine($"[Living in Calradia] Agent {agentId} completed workflow:");
                Console.WriteLine($"  Reasoning: {result.Decision?.Reasoning}");
                Console.WriteLine($"  Actions executed: {result.ActionResults?.Count ?? 0}");
                
                if (result.ActionResults != null)
                {
                    foreach (var actionResult in result.ActionResults)
                    {
                        var status = actionResult.Success ? "?" : "?";
                        Console.WriteLine($"    {status} {actionResult.Message}");
                    }
                }
            }
            else
            {
                Console.WriteLine($"[Living in Calradia] Workflow failed for {agentId}: {result.Error?.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Living in Calradia] Error during agent thinking: {ex.Message}");
        }
    }
}
