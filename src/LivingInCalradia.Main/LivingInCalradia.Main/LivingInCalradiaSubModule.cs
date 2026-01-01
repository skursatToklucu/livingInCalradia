using System;
using System.Threading;
using System.Threading.Tasks;
using LivingInCalradia.AI.Configuration;
using LivingInCalradia.AI.Orchestration;
using LivingInCalradia.Core.Application.Interfaces;
using LivingInCalradia.Core.Application.Services;
using LivingInCalradia.Infrastructure.Bannerlord;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace LivingInCalradia.Main;

/// <summary>
/// Main entry point for the Living in Calradia mod.
/// Initializes the AI system and orchestrates agent workflows.
/// </summary>
public sealed class LivingInCalradiaSubModule
{
    private AgentWorkflowService? _workflowService;
    private bool _isInitialized;
    private CancellationTokenSource? _cancellationTokenSource;
    
    // Singleton for easy access from console commands
    public static LivingInCalradiaSubModule? Instance { get; private set; }
    
    /// <summary>
    /// Initializes the AI system with all dependencies.
    /// This should be called during mod initialization.
    /// </summary>
    public void Initialize()
    {
        try
        {
            Instance = this;
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
            
            // Create REAL Bannerlord infrastructure components
            var worldSensor = new BannerlordWorldSensor();
            var actionExecutor = new BannerlordActionExecutor();
            
            Console.WriteLine("[Living in Calradia] Using BannerlordWorldSensor (real game data)");
            Console.WriteLine("[Living in Calradia] Using BannerlordActionExecutor (real game actions)");
            
            // Create workflow service
            _workflowService = new AgentWorkflowService(
                worldSensor,
                orchestrator,
                actionExecutor);
            
            _cancellationTokenSource = new CancellationTokenSource();
            
            _isInitialized = true;
            Console.WriteLine("[Living in Calradia] AI system initialized successfully!");
            Console.WriteLine("[Living in Calradia] Use 'lic.test' or 'lic.proof' in console to test.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Living in Calradia] Failed to initialize: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Runs the proof test to demonstrate AI ? Action connection.
    /// Call this from game console or hotkey.
    /// </summary>
    public static void RunProofTest()
    {
        BannerlordActionExecutor.RunProofTest();
    }
    
    /// <summary>
    /// Runs the full comprehensive AI proof test.
    /// Call this from game console or hotkey.
    /// </summary>
    public static void RunFullProofTest()
    {
        BannerlordActionExecutor.RunFullAIProofTest();
    }
    
    /// <summary>
    /// Checks if the game is currently paused or not in a valid state for AI thinking.
    /// </summary>
    private bool IsGamePaused()
    {
        try
        {
            // Check if Campaign exists and is active
            if (Campaign.Current == null)
            {
                return true;
            }
            
            // Check if game time is paused
            if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.Stop ||
                Campaign.Current.TimeControlMode == CampaignTimeControlMode.StoppablePlay)
            {
                return true;
            }
            
            // Check if game state is valid for AI
            var gameStateManager = Game.Current?.GameStateManager;
            if (gameStateManager == null)
            {
                return true;
            }
            
            // Check if we're in a valid map state
            var activeState = gameStateManager.ActiveState;
            if (activeState == null)
            {
                return true;
            }
            
            return false;
        }
        catch
        {
            // If any error occurs, assume game is paused for safety
            return true;
        }
    }
    
    /// <summary>
    /// Executes the AI workflow for a specific agent.
    /// This can be called from Bannerlord's tick system or event handlers.
    /// Will not execute if game is paused.
    /// </summary>
    public async Task ExecuteAgentThinkingAsync(string agentId)
    {
        if (!_isInitialized || _workflowService == null)
        {
            Console.WriteLine("[Living in Calradia] AI system not initialized!");
            return;
        }
        
        // Check if game is paused - don't think while paused
        if (IsGamePaused())
        {
            Console.WriteLine("[Living in Calradia] Game is paused, skipping AI thinking.");
            return;
        }
        
        try
        {
            Console.WriteLine($"\n[Living in Calradia] Agent {agentId} is thinking...");
            
            // Use cancellation token for graceful cancellation
            var token = _cancellationTokenSource?.Token ?? CancellationToken.None;
            
            var result = await _workflowService.ExecuteWorkflowAsync(agentId, token);
            
            // Check again after async operation - game might have been paused
            if (IsGamePaused())
            {
                Console.WriteLine("[Living in Calradia] Game paused during AI thinking, discarding result.");
                return;
            }
            
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
        catch (OperationCanceledException)
        {
            Console.WriteLine($"[Living in Calradia] AI thinking cancelled for {agentId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Living in Calradia] Error during agent thinking: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Cancels any ongoing AI operations. Call this when game is paused or exiting.
    /// </summary>
    public void CancelOngoingOperations()
    {
        try
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            Console.WriteLine("[Living in Calradia] Ongoing AI operations cancelled.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Living in Calradia] Error cancelling operations: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Cleanup resources when mod is unloaded.
    /// </summary>
    public void Shutdown()
    {
        try
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _isInitialized = false;
            Instance = null;
            Console.WriteLine("[Living in Calradia] AI system shut down.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Living in Calradia] Error during shutdown: {ex.Message}");
        }
    }
}
