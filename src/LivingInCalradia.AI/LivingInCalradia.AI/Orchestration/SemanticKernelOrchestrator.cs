using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LivingInCalradia.Core.Application.Interfaces;
using LivingInCalradia.Core.Domain.ValueObjects;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace LivingInCalradia.AI.Orchestration;

/// <summary>
/// Semantic Kernel-based implementation of the AI orchestrator.
/// Uses LLM for reasoning and function calling for action planning.
/// </summary>
public sealed class SemanticKernelOrchestrator : IAgentOrchestrator
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatService;
    private readonly OpenAIPromptExecutionSettings _executionSettings;
    
    public SemanticKernelOrchestrator(Kernel kernel)
    {
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _chatService = kernel.GetRequiredService<IChatCompletionService>();
        
        _executionSettings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            Temperature = 0.7,
            MaxTokens = 500
        };
    }
    
    public async Task<AgentDecision> ReasonAsync(
        string agentId,
        WorldPerception perception,
        CancellationToken cancellationToken = default)
    {
        var chatHistory = new ChatHistory();
        
        // System prompt: Define the agent's role and behavior
        chatHistory.AddSystemMessage(@"You are an AI controlling an NPC in Mount & Blade II: Bannerlord.
Your role is to reason about the game world and decide on actions that make sense for your character.
You have access to functions that can modify the game state.
Think step-by-step about what your character would do given the current situation.
Always provide your reasoning before taking actions.");
        
        // User prompt: Current world state
        var worldContext = $@"Agent ID: {agentId}
{perception.ToSemanticContext()}

Based on this information, what should this character do next?
Provide your reasoning and then use the available functions to take action.";
        
        chatHistory.AddUserMessage(worldContext);
        
        // Get LLM response with function calling
        var result = await _chatService.GetChatMessageContentAsync(
            chatHistory,
            _executionSettings,
            _kernel,
            cancellationToken);
        
        // Extract reasoning
        var reasoning = result.Content ?? "No reasoning provided";
        
        // Parse function calls into AgentActions
        var actions = new List<AgentAction>();
        
        if (result.Metadata != null && result.Metadata.ContainsKey("FunctionToolCalls"))
        {
            // Extract function calls from metadata (SK 1.0 pattern)
            var functionCalls = result.Metadata["FunctionToolCalls"];
            if (functionCalls != null)
            {
                // Parse the function calls - this will be refined based on actual SK metadata structure
                actions.Add(new AgentAction("LogReasoning", new Dictionary<string, object>
                {
                    ["reasoning"] = reasoning
                }));
            }
        }
        
        // If no actions were generated, create a "wait" action
        if (!actions.Any())
        {
            actions.Add(new AgentAction("Wait", new Dictionary<string, object>
            {
                ["duration"] = 60
            }));
        }
        
        return new AgentDecision(agentId, reasoning, actions);
    }
}
