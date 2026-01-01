using System;
using System.Threading;
using System.Threading.Tasks;

namespace LivingInCalradia.Core.Application.Interfaces;

/// <summary>
/// Nervous System: Executes actions in the game world.
/// Implemented by Bannerlord integration layer.
/// </summary>
public interface IGameActionExecutor
{
    /// <summary>
    /// Executes a single action in the game world.
    /// </summary>
    Task<ActionResult> ExecuteAsync(
        AgentAction action, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if an action type is available/registered.
    /// </summary>
    bool CanExecute(string actionType);
}

/// <summary>
/// Result of executing an action in the game.
/// </summary>
public sealed class ActionResult
{
    public bool Success { get; }
    public string Message { get; }
    public Exception? Error { get; }
    
    private ActionResult(bool success, string message, Exception? error = null)
    {
        Success = success;
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Error = error;
    }
    
    public static ActionResult Successful(string message) => new(true, message);
    public static ActionResult Failed(string message, Exception? error = null) => new(false, message, error);
}
