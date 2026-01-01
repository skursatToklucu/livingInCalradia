using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LivingInCalradia.Core.Domain.ValueObjects;

namespace LivingInCalradia.Core.Application.Interfaces;

/// <summary>
/// Brain: Orchestrates AI reasoning using LLM.
/// Implemented by Semantic Kernel in the AI layer.
/// </summary>
public interface IAgentOrchestrator
{
    /// <summary>
    /// Processes perception and decides on actions using LLM reasoning.
    /// </summary>
    Task<AgentDecision> ReasonAsync(
        string agentId,
        WorldPerception perception,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of the reasoning process.
/// </summary>
public sealed class AgentDecision
{
    public string AgentId { get; }
    public string Reasoning { get; }
    public IReadOnlyList<AgentAction> Actions { get; }

    public AgentDecision(string agentId, string reasoning, IEnumerable<AgentAction> actions)
    {
        AgentId = agentId ?? throw new ArgumentNullException(nameof(agentId));
        Reasoning = reasoning ?? throw new ArgumentNullException(nameof(reasoning));
        Actions = actions?.ToList() ?? throw new ArgumentNullException(nameof(actions));
    }
}

public sealed class AgentAction
{
    public string ActionType { get; }
    public IDictionary<string, object> Parameters { get; }

    public AgentAction(string actionType, IDictionary<string, object> parameters)
    {
        ActionType = actionType ?? throw new ArgumentNullException(nameof(actionType));
        Parameters = new Dictionary<string, object>(parameters);
    }
}
