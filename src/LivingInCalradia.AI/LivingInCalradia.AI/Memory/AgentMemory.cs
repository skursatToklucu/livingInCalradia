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
    private static bool _isTurkish = false;
    
    public static void SetLanguage(string language)
    {
        _isTurkish = language?.ToLowerInvariant() == "tr";
    }
    
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
        
        memories.Add(new MemoryEntry
        {
            Timestamp = DateTime.UtcNow,
            Situation = situation,
            Decision = decision,
            Action = action
        });
        
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
<<<<<<< Updated upstream
            return "Önceki kararlar?n yok - bu senin ilk karar?n.";
        }
        
        var lines = new List<string>
        {
            $"Son {memories.Count} karar?n:"
        };
=======
            return _isTurkish 
                ? "Onceki kararlarin yok - bu senin ilk kararin."
                : "You have no previous decisions - this is your first decision.";
        }
        
        var lastDecisionsLabel = _isTurkish ? $"Son {memories.Count} kararin:" : $"Your last {memories.Count} decisions:";
        var lines = new List<string> { lastDecisionsLabel };
>>>>>>> Stashed changes
        
        for (int i = 0; i < memories.Count; i++)
        {
            var m = memories[i];
            var timeAgo = DateTime.UtcNow - m.Timestamp;
<<<<<<< Updated upstream
            var timeStr = timeAgo.TotalMinutes < 1 ? "az önce" : $"{timeAgo.TotalMinutes:F0} dakika önce";
=======
            string timeStr;
            
            if (_isTurkish)
            {
                timeStr = timeAgo.TotalMinutes < 1 ? "az once" : $"{timeAgo.TotalMinutes:F0} dakika once";
            }
            else
            {
                timeStr = timeAgo.TotalMinutes < 1 ? "just now" : $"{timeAgo.TotalMinutes:F0} minutes ago";
            }
>>>>>>> Stashed changes
            
            lines.Add($"  {i + 1}. [{timeStr}] {m.Action}: {TruncateText(m.Decision, 80)}");
        }
        
        return string.Join("\n", lines);
    }
    
    public void Forget(string agentId)
    {
        _memories.Remove(agentId);
    }
    
    public void ForgetAll()
    {
        _memories.Clear();
    }
    
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
