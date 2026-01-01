using System.Threading;
using System.Threading.Tasks;
using LivingInCalradia.Core.Domain.ValueObjects;

namespace LivingInCalradia.Core.Application.Interfaces;

/// <summary>
/// Senses: Extracts the game world state into structured perception.
/// Abstraction for Bannerlord API integration.
/// </summary>
public interface IWorldSensor
{
    /// <summary>
    /// Perceives the current state of the world for a specific agent.
    /// </summary>
    Task<WorldPerception> PerceiveWorldAsync(string agentId, CancellationToken cancellationToken = default);
}
