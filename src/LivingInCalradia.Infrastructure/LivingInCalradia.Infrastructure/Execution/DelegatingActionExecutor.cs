using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LivingInCalradia.Core.Application.Interfaces;

namespace LivingInCalradia.Infrastructure.Execution;

/// <summary>
/// Action executor that delegates to registered action handlers.
/// Uses a strategy pattern for extensible action handling.
/// </summary>
public sealed class DelegatingActionExecutor : IGameActionExecutor
{
    private readonly Dictionary<string, Func<AgentAction, CancellationToken, Task<ActionResult>>> _actionHandlers;
    
    public DelegatingActionExecutor()
    {
        _actionHandlers = new Dictionary<string, Func<AgentAction, CancellationToken, Task<ActionResult>>>(
            StringComparer.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// Registers a handler for a specific action type.
    /// </summary>
    public void RegisterHandler(
        string actionType, 
        Func<AgentAction, CancellationToken, Task<ActionResult>> handler)
    {
        if (string.IsNullOrWhiteSpace(actionType))
            throw new ArgumentException("Action type cannot be empty", nameof(actionType));
        
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));
        
        _actionHandlers[actionType] = handler;
    }
    
    /// <summary>
    /// Convenience method to register synchronous handlers.
    /// </summary>
    public void RegisterHandler(
        string actionType,
        Func<AgentAction, ActionResult> handler)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));
        
        RegisterHandler(actionType, (action, ct) => Task.FromResult(handler(action)));
    }
    
    public bool CanExecute(string actionType)
    {
        return _actionHandlers.ContainsKey(actionType);
    }
    
    public async Task<ActionResult> ExecuteAsync(
        AgentAction action, 
        CancellationToken cancellationToken = default)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));
        
        if (!_actionHandlers.TryGetValue(action.ActionType, out var handler))
        {
            return ActionResult.Failed($"No handler registered for action type: {action.ActionType}");
        }
        
        try
        {
            return await handler(action, cancellationToken);
        }
        catch (Exception ex)
        {
            return ActionResult.Failed($"Error executing action {action.ActionType}: {ex.Message}", ex);
        }
    }
}
