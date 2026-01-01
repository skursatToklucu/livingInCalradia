using System.Threading;
using System.Threading.Tasks;

namespace LivingInCalradia.Core.Application.Interfaces;

/// <summary>
/// Orchestrates AI-powered dialogue generation for NPCs.
/// Enables dynamic, context-aware conversations.
/// </summary>
public interface IDialogueOrchestrator
{
    /// <summary>
    /// Generates an AI response for a dialogue context.
    /// </summary>
    /// <param name="npcId">The NPC's unique identifier</param>
    /// <param name="npcName">The NPC's display name</param>
    /// <param name="npcRole">The NPC's role (Lord, Merchant, Villager, etc.)</param>
    /// <param name="playerMessage">What the player said/asked</param>
    /// <param name="context">Additional context (location, relations, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The AI-generated response</returns>
    Task<DialogueResponse> GenerateResponseAsync(
        string npcId,
        string npcName,
        string npcRole,
        string playerMessage,
        DialogueContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Context information for dialogue generation.
/// </summary>
public sealed class DialogueContext
{
    public string Location { get; set; } = string.Empty;
    public int RelationWithPlayer { get; set; }
    public string NpcFaction { get; set; } = string.Empty;
    public string PlayerFaction { get; set; } = string.Empty;
    public bool IsAtWar { get; set; }
    public string CurrentSituation { get; set; } = string.Empty;
    public string[] RecentEvents { get; set; } = System.Array.Empty<string>();
    public string NpcMood { get; set; } = "Neutral";
}

/// <summary>
/// AI-generated dialogue response.
/// </summary>
public sealed class DialogueResponse
{
    public string Text { get; }
    public string Emotion { get; }
    public DialogueIntent Intent { get; }
    public bool ShouldEndConversation { get; }

    public DialogueResponse(string text, string emotion = "Neutral", DialogueIntent intent = DialogueIntent.Neutral, bool shouldEnd = false)
    {
        Text = text ?? string.Empty;
        Emotion = emotion;
        Intent = intent;
        ShouldEndConversation = shouldEnd;
    }

    public static DialogueResponse Error(string message) 
        => new DialogueResponse(message, "Confused", DialogueIntent.Neutral, false);
}

/// <summary>
/// The intent behind an NPC's dialogue response.
/// </summary>
public enum DialogueIntent
{
    Neutral,
    Friendly,
    Hostile,
    Bargaining,
    Informative,
    Threatening,
    Pleading,
    Dismissive
}
