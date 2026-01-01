using System;
using System.Collections.Generic;
using System.Text;

namespace LivingInCalradia.AI.Memory;

/// <summary>
/// Stores conversation history between player and NPCs.
/// Enables contextual dialogue that remembers past interactions.
/// </summary>
public sealed class ConversationMemory
{
    private readonly Dictionary<string, List<ConversationEntry>> _conversations;
    private readonly int _maxEntriesPerNpc;
    
    public ConversationMemory(int maxEntriesPerNpc = 10)
    {
        _conversations = new Dictionary<string, List<ConversationEntry>>(StringComparer.OrdinalIgnoreCase);
        _maxEntriesPerNpc = maxEntriesPerNpc;
    }
    
    /// <summary>
    /// Records a conversation exchange.
    /// </summary>
    public void Remember(string npcId, string playerMessage, string npcResponse)
    {
        if (!_conversations.ContainsKey(npcId))
        {
            _conversations[npcId] = new List<ConversationEntry>();
        }
        
        var entries = _conversations[npcId];
        
        entries.Add(new ConversationEntry
        {
            Timestamp = DateTime.UtcNow,
            PlayerMessage = playerMessage,
            NpcResponse = npcResponse
        });
        
        // Keep only recent conversations
        while (entries.Count > _maxEntriesPerNpc)
        {
            entries.RemoveAt(0);
        }
    }
    
    /// <summary>
    /// Gets the conversation history as a formatted string for the LLM.
    /// </summary>
    public string GetConversationHistory(string npcId)
    {
        if (!_conversations.TryGetValue(npcId, out var entries) || entries.Count == 0)
        {
            return "Bu oyuncu ile ilk konu?man?z.";
        }
        
        var sb = new StringBuilder();
        sb.AppendLine($"Son {entries.Count} konu?man?z:");
        
        foreach (var entry in entries)
        {
            var timeAgo = DateTime.UtcNow - entry.Timestamp;
            var timeStr = FormatTimeAgo(timeAgo);
            
            sb.AppendLine($"  [{timeStr}]");
            sb.AppendLine($"    Oyuncu: {TruncateText(entry.PlayerMessage, 100)}");
            sb.AppendLine($"    Sen: {TruncateText(entry.NpcResponse, 100)}");
        }
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Gets the last thing the player said to this NPC.
    /// </summary>
    public string? GetLastPlayerMessage(string npcId)
    {
        if (!_conversations.TryGetValue(npcId, out var entries) || entries.Count == 0)
        {
            return null;
        }
        
        return entries[entries.Count - 1].PlayerMessage;
    }
    
    /// <summary>
    /// Clears conversation history for an NPC.
    /// </summary>
    public void ForgetNpc(string npcId)
    {
        _conversations.Remove(npcId);
    }
    
    /// <summary>
    /// Clears all conversation history.
    /// </summary>
    public void ForgetAll()
    {
        _conversations.Clear();
    }
    
    private string FormatTimeAgo(TimeSpan timeAgo)
    {
        if (timeAgo.TotalMinutes < 1) return "az önce";
        if (timeAgo.TotalMinutes < 60) return $"{(int)timeAgo.TotalMinutes} dakika önce";
        if (timeAgo.TotalHours < 24) return $"{(int)timeAgo.TotalHours} saat önce";
        return $"{(int)timeAgo.TotalDays} gün önce";
    }
    
    private string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
        
        return text.Substring(0, maxLength - 3) + "...";
    }
}

/// <summary>
/// A single conversation exchange entry.
/// </summary>
public sealed class ConversationEntry
{
    public DateTime Timestamp { get; set; }
    public string PlayerMessage { get; set; } = string.Empty;
    public string NpcResponse { get; set; } = string.Empty;
}
