using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace LivingInCalradia.Main.Features;

/// <summary>
/// Tracks and displays what lords are "thinking" in real-time.
/// This creates a visible, shareable showcase of the AI system.
/// </summary>
public static class LordThoughtsPanel
{
    private static readonly List<LordThought> _recentThoughts = new List<LordThought>();
    private const int MaxThoughts = 10;
    
    /// <summary>
    /// Records a lord's AI-generated thought for display.
    /// </summary>
    public static void RecordThought(string lordName, string thought, string action)
    {
        var entry = new LordThought
        {
            LordName = lordName,
            Thought = TruncateText(thought, 100),
            Action = action,
            Timestamp = DateTime.Now
        };
        
        _recentThoughts.Insert(0, entry);
        
        while (_recentThoughts.Count > MaxThoughts)
        {
            _recentThoughts.RemoveAt(_recentThoughts.Count - 1);
        }
    }
    
    /// <summary>
    /// Displays all recent thoughts in the game message log.
    /// Called with NumPad5.
    /// </summary>
    public static void ShowRecentThoughts()
    {
        if (_recentThoughts.Count == 0)
        {
            ShowMessage("No lord thoughts recorded yet. Trigger AI with NumPad2.", Colors.Yellow);
            return;
        }
        
        ShowMessage("", Colors.White);
        ShowMessage("========== LORD THOUGHTS ==========", Colors.Magenta);
        ShowMessage("", Colors.White);
        
        foreach (var thought in _recentThoughts)
        {
            var timeAgo = DateTime.Now - thought.Timestamp;
            var timeStr = FormatTimeAgo(timeAgo);
            
            ShowMessage($"[{timeStr}] {thought.LordName}:", Colors.Cyan);
            ShowMessage($"  Thought: \"{thought.Thought}\"", Colors.White);
            ShowMessage($"  Decision: {thought.Action}", Colors.Green);
            ShowMessage("", Colors.White);
        }
        
        ShowMessage("=======================================", Colors.Magenta);
    }
    
    /// <summary>
    /// Shows a dramatic "lord is thinking" notification.
    /// </summary>
    public static void ShowThinkingNotification(string lordName)
    {
        ShowMessage($"[AI] {lordName} is making a decision...", Colors.Yellow);
    }
    
    /// <summary>
    /// Shows the decision result with dramatic flair.
    /// </summary>
    public static void ShowDecisionNotification(string lordName, string action, string detail)
    {
        var actionEmoji = GetActionEmoji(action);
        ShowMessage($"[AI] {lordName}: {actionEmoji} {action}", Colors.Cyan);
        
        if (!string.IsNullOrEmpty(detail))
        {
            ShowMessage($"      \"{detail}\"", Colors.White);
        }
    }
    
    private static string GetActionEmoji(string action)
    {
        var actionLower = action.ToLowerInvariant();
        
        if (actionLower.Contains("attack") || actionLower.Contains("war"))
            return "[ATTACK]";
        if (actionLower.Contains("peace"))
            return "[PEACE]";
        if (actionLower.Contains("trade"))
            return "[TRADE]";
        if (actionLower.Contains("move"))
            return "[MOVE]";
        if (actionLower.Contains("recruit"))
            return "[RECRUIT]";
        if (actionLower.Contains("defend"))
            return "[DEFEND]";
        if (actionLower.Contains("siege"))
            return "[SIEGE]";
        if (actionLower.Contains("wait"))
            return "[WAIT]";
            
        return "[DECISION]";
    }
    
    private static string FormatTimeAgo(TimeSpan timeAgo)
    {
        if (timeAgo.TotalSeconds < 60)
            return "just now";
        if (timeAgo.TotalMinutes < 60)
            return $"{(int)timeAgo.TotalMinutes} min ago";
        return $"{(int)timeAgo.TotalHours} hours ago";
    }
    
    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
        return text.Substring(0, maxLength - 3) + "...";
    }
    
    private static void ShowMessage(string message, Color color)
    {
        try
        {
            InformationManager.DisplayMessage(new InformationMessage(message, color));
        }
        catch
        {
        }
    }
}

/// <summary>
/// A recorded lord thought entry.
/// </summary>
public class LordThought
{
    public string LordName { get; set; } = "";
    public string Thought { get; set; } = "";
    public string Action { get; set; } = "";
    public DateTime Timestamp { get; set; }
}
