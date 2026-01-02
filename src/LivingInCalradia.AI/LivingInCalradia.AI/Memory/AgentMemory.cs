using System;
using System.Collections.Generic;

namespace LivingInCalradia.AI.Memory;

/// <summary>
/// Simple in-memory conversation history for agents.
/// Allows NPCs to remember their previous decisions.
/// </summary>
public sealed class AgentMemory
{
    private readonly Dictionary<string, List<MemoryEntry>> _memories;
    private readonly int _maxMemoriesPerAgent;
    
    public AgentMemory(int maxMemoriesPerAgent = 5)
    {
        _memories = new Dictionary<string, List<MemoryEntry>>(StringComparer.OrdinalIgnoreCase);
        _maxMemoriesPerAgent = maxMemoriesPerAgent;
    }
    
    /// <summary>
    /// Records a decision made by an agent.
    /// </summary>
    public void Remember(string agentId, string situation, string decision, string action)
    {
        if (!_memories.ContainsKey(agentId))
        {
            _memories[agentId] = new List<MemoryEntry>();
        }
        
        var memories = _memories[agentId];
        
        // Add new memory
        memories.Add(new MemoryEntry
        {
            Timestamp = DateTime.UtcNow,
            Situation = situation,
            Decision = decision,
            Action = action
        });
        
        // Keep only the last N memories
        while (memories.Count > _maxMemoriesPerAgent)
        {
            memories.RemoveAt(0);
        }
    }
    
    /// <summary>
    /// Gets the memory context for an agent as a formatted string.
    /// </summary>
    public string GetMemoryContext(string agentId)
    {
        if (!_memories.TryGetValue(agentId, out var memories) || memories.Count == 0)
        {
            return "No previous decisions - this is your first decision.";
        }
        
        var lines = new List<string>
        {
            $"Your last {memories.Count} decisions:"
        };
        
        for (int i = 0; i < memories.Count; i++)
        {
            var m = memories[i];
            var timeAgo = DateTime.UtcNow - m.Timestamp;
            var timeStr = timeAgo.TotalMinutes < 1 ? "just now" : $"{timeAgo.TotalMinutes:F0} minutes ago";
            
            lines.Add($"  {i + 1}. [{timeStr}] {m.Action}: {TruncateText(m.Decision, 80)}");
        }
        
        return string.Join("\n", lines);
    }
    
    /// <summary>
    /// Clears all memories for an agent.
    /// </summary>
    public void Forget(string agentId)
    {
        _memories.Remove(agentId);
    }
    
    /// <summary>
    /// Clears all memories.
    /// </summary>
    public void ForgetAll()
    {
        _memories.Clear();
    }
    
    /// <summary>
    /// Gets the total number of memories stored.
    /// </summary>
    public int TotalMemories
    {
        get
        {
            int total = 0;
            foreach (var memories in _memories.Values)
            {
                total += memories.Count;
            }
            return total;
        }
    }
    
    private string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
        
        return text.Substring(0, maxLength - 3) + "...";
    }
}

/// <summary>
/// A single memory entry for an agent.
/// </summary>
public sealed class MemoryEntry
{
    public DateTime Timestamp { get; set; }
    public string Situation { get; set; } = string.Empty;
    public string Decision { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
}
