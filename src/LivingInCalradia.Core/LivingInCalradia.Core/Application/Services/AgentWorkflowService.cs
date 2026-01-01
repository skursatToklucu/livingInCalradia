using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LivingInCalradia.Core.Application.Interfaces;
using LivingInCalradia.Core.Domain.ValueObjects;

namespace LivingInCalradia.Core.Application.Services;

/// <summary>
/// Application Service that orchestrates the full agentic workflow.
/// Senses -> Reasons -> Acts pattern.
/// </summary>
public class AgentWorkflowService
{
    private readonly IWorldSensor _worldSensor;
    private readonly IAgentOrchestrator _orchestrator;
    private readonly IGameActionExecutor _actionExecutor;
    
    public AgentWorkflowService(
        IWorldSensor worldSensor,
        IAgentOrchestrator orchestrator,
        IGameActionExecutor actionExecutor)
    {
        _worldSensor = worldSensor ?? throw new ArgumentNullException(nameof(worldSensor));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _actionExecutor = actionExecutor ?? throw new ArgumentNullException(nameof(actionExecutor));
    }
    
    /// <summary>
    /// Executes one complete cycle of the agentic workflow for an NPC.
    /// </summary>
    public async Task<WorkflowResult> ExecuteWorkflowAsync(
        string agentId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Step 1: Sense the world
            var perception = await _worldSensor.PerceiveWorldAsync(agentId, cancellationToken);
            
            // Step 2: Reason using LLM
            var decision = await _orchestrator.ReasonAsync(agentId, perception, cancellationToken);
            
            // Step 3: Execute actions with agentId context
            var results = new List<ActionResult>();
            foreach (var action in decision.Actions)
            {
                // Inject agentId into action parameters for executor
                if (!action.Parameters.ContainsKey("agentId"))
                {
                    action.Parameters["agentId"] = agentId;
                }
                
                if (!_actionExecutor.CanExecute(action.ActionType))
                {
                    results.Add(ActionResult.Failed($"Unknown action: {action.ActionType}"));
                    continue;
                }
                
                var result = await _actionExecutor.ExecuteAsync(action, cancellationToken);
                results.Add(result);
            }
            
            return WorkflowResult.Success(agentId, perception, decision, results);
        }
        catch (Exception ex)
        {
            return WorkflowResult.Failure(agentId, ex);
        }
    }
}

public sealed class WorkflowResult
{
    public bool IsSuccessful { get; }
    public string AgentId { get; }
    public WorldPerception? Perception { get; }
    public AgentDecision? Decision { get; }
    public IReadOnlyList<ActionResult>? ActionResults { get; }
    public Exception? Error { get; }
    
    private WorkflowResult(
        bool success, 
        string agentId, 
        WorldPerception? perception, 
        AgentDecision? decision, 
        IReadOnlyList<ActionResult>? actionResults, 
        Exception? error)
    {
        IsSuccessful = success;
        AgentId = agentId;
        Perception = perception;
        Decision = decision;
        ActionResults = actionResults;
        Error = error;
    }
    
    public static WorkflowResult Success(
        string agentId, 
        WorldPerception perception, 
        AgentDecision decision, 
        IReadOnlyList<ActionResult> actionResults)
        => new(true, agentId, perception, decision, actionResults, null);
    
    public static WorkflowResult Failure(string agentId, Exception error)
        => new(false, agentId, null, null, null, error);
}
