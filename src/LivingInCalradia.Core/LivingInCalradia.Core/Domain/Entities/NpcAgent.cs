using System;

namespace LivingInCalradia.Core.Domain.Entities;

/// <summary>
/// Represents an NPC with agency in Calradia.
/// Follows DDD Entity pattern with unique identity.
/// </summary>
public class NpcAgent
{
    public string Id { get; }
    public string Name { get; private set; }
    public AgentType Type { get; private set; }
    public AgentState State { get; private set; }

    public NpcAgent(string id, string name, AgentType type)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Agent ID cannot be empty", nameof(id));

        Id = id;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Type = type;
        State = AgentState.Idle;
    }

    public void UpdateState(AgentState newState)
    {
        State = newState;
    }
}

public enum AgentType
{
    Lord,
    Villager,
    Soldier,
    Merchant
}

public enum AgentState
{
    Idle,
    Thinking,
    Acting,
    Waiting
}
