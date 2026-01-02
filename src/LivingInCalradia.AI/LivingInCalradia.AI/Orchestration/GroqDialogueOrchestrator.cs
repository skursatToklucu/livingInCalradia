using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LivingInCalradia.AI.Memory;
using LivingInCalradia.Core.Application.Interfaces;

namespace LivingInCalradia.AI.Orchestration;

/// <summary>
/// AI-powered dialogue orchestrator using Groq API.
/// Generates dynamic, contextual NPC responses.
/// </summary>
public sealed class GroqDialogueOrchestrator : IDialogueOrchestrator
{
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly double _temperature;
    private readonly ConversationMemory _conversationMemory;
    
    public GroqDialogueOrchestrator(
        string apiKey, 
        string model = "llama-3.1-8b-instant", 
        double temperature = 0.8)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentNullException(nameof(apiKey));
        
        _model = model;
        _temperature = temperature;
        _conversationMemory = new ConversationMemory(maxEntriesPerNpc: 10);
        
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.groq.com/openai/v1/"),
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }
    
    /// <summary>
    /// Gets the conversation memory for external access.
    /// </summary>
    public ConversationMemory ConversationMemory => _conversationMemory;
    
    public async Task<DialogueResponse> GenerateResponseAsync(
        string npcId,
        string npcName,
        string npcRole,
        string playerMessage,
        DialogueContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var systemPrompt = BuildSystemPrompt(npcName, npcRole, context);
            var conversationHistory = _conversationMemory.GetConversationHistory(npcId);
            var userPrompt = BuildUserPrompt(playerMessage, context, conversationHistory);
            
            var json = BuildRequestJson(systemPrompt, userPrompt);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[Dialogue AI] API Error: {response.StatusCode}");
                return DialogueResponse.Error("Hmm... I was going to say something but I forgot.");
            }
            
            var npcResponse = ExtractContentFromResponse(responseJson);
            
            // Parse emotion and intent from response
            var (cleanText, emotion, intent, shouldEnd) = ParseResponse(npcResponse);
            
            // Remember this conversation
            _conversationMemory.Remember(npcId, playerMessage, cleanText);
            
            Console.WriteLine($"[Dialogue AI] {npcName}: {cleanText}");
            
            return new DialogueResponse(cleanText, emotion, intent, shouldEnd);
        }
        catch (OperationCanceledException)
        {
            return DialogueResponse.Error("...");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Dialogue AI] Error: {ex.Message}");
            return DialogueResponse.Error("*looks thoughtfully*");
        }
    }
    
    private string BuildSystemPrompt(string npcName, string npcRole, DialogueContext context)
    {
        var sb = new StringBuilder();
        
        // Base personality based on role
        sb.AppendLine($"You are {npcName}, a {GetRoleDescription(npcRole)}.");
        sb.AppendLine($"You live in the world of Mount & Blade II: Bannerlord.");
        sb.AppendLine();
        
        // Role-specific personality
        sb.AppendLine(GetPersonalityTraits(npcRole));
        sb.AppendLine();
        
        // Relationship context
        if (context.RelationWithPlayer >= 50)
        {
            sb.AppendLine("You have a very good relationship with this person, you trust them and speak in a friendly manner.");
        }
        else if (context.RelationWithPlayer >= 0)
        {
            sb.AppendLine("You have a normal relationship with this person, you speak formally but politely.");
        }
        else if (context.RelationWithPlayer >= -50)
        {
            sb.AppendLine("Your relationship with this person is tense, you speak coldly and distantly.");
        }
        else
        {
            sb.AppendLine("You hate this person, you speak hostilely and threateningly.");
        }
        
        // War context
        if (context.IsAtWar)
        {
            sb.AppendLine("ATTENTION: Your kingdoms are at war! Consider this in your response.");
        }
        
        // Mood
        sb.AppendLine($"Your current mood: {context.NpcMood}");
        
        // Instructions
        sb.AppendLine();
        sb.AppendLine("RULES:");
        sb.AppendLine("- Give short and concise answers (1-3 sentences)");
        sb.AppendLine("- Speak according to your character");
        sb.AppendLine("- Use medieval language style");
        sb.AppendLine("- Show your emotions but don't overdo it");
        sb.AppendLine("- Stay true to the game world");
        
        return sb.ToString();
    }
    
    private string GetRoleDescription(string role)
    {
        var roleLower = role.ToLowerInvariant();
        
        if (roleLower.Contains("king"))
            return "powerful and honorable king";
        if (roleLower.Contains("lord"))
            return "noble lord";
        if (roleLower.Contains("merchant"))
            return "cunning merchant";
        if (roleLower.Contains("blacksmith"))
            return "master blacksmith";
        if (roleLower.Contains("tavern"))
            return "cheerful tavern keeper";
        if (roleLower.Contains("villager"))
            return "simple villager";
        if (roleLower.Contains("soldier"))
            return "experienced soldier";
        if (roleLower.Contains("commander"))
            return "seasoned commander";
        if (roleLower.Contains("lady"))
            return "noble lady";
        if (roleLower.Contains("bandit"))
            return "ruthless bandit";
        if (roleLower.Contains("gang"))
            return "street gang leader";
            
        return "Calradia resident";
    }
    
    private string GetPersonalityTraits(string role)
    {
        var roleLower = role.ToLowerInvariant();
        
        if (roleLower.Contains("king"))
            return "You are authoritative, wise, and put your kingdom's interests above all else. You address others as a ruler would.";
            
        if (roleLower.Contains("lord"))
            return "You are honorable, proud, and a warrior. You hold your honor above all else. You make your nobility felt.";
            
        if (roleLower.Contains("merchant"))
            return "You are clever, calculating, and opportunistic. You try to turn every conversation into a trade opportunity. Money is everything.";
            
        if (roleLower.Contains("blacksmith"))
            return "You are hardworking, practical, and speak little. You focus on your work. You are passionate about weapons and armor.";
            
        if (roleLower.Contains("tavern"))
            return "You are friendly, curious about gossip, and hospitable. You know everyone, you hear everything.";
            
        if (roleLower.Contains("villager"))
            return "You are humble, fearful, and respectful to lords. Life is hard, taxes are heavy. You just want to survive.";
            
        if (roleLower.Contains("soldier"))
            return "You are disciplined, loyal, and follow orders. You love telling war stories.";
            
        if (roleLower.Contains("bandit"))
            return "You are dangerous, cunning, and ruthless. You exploit the weak. Power is everything.";
            
        return "You are an ordinary person living in Calradia. You struggle with the difficulties of daily life.";
    }
    
    private string BuildUserPrompt(string playerMessage, DialogueContext context, string conversationHistory)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("SITUATION:");
        sb.AppendLine($"Location: {context.Location}");
        sb.AppendLine($"Player's Faction: {context.PlayerFaction}");
        sb.AppendLine($"Your Faction: {context.NpcFaction}");
        sb.AppendLine($"Relationship Level: {context.RelationWithPlayer}");
        
        if (!string.IsNullOrEmpty(context.CurrentSituation))
        {
            sb.AppendLine($"Current Situation: {context.CurrentSituation}");
        }
        
        if (context.RecentEvents?.Length > 0)
        {
            sb.AppendLine($"Recent Events: {string.Join(", ", context.RecentEvents)}");
        }
        
        sb.AppendLine();
        sb.AppendLine("PAST CONVERSATIONS:");
        sb.AppendLine(conversationHistory);
        sb.AppendLine();
        sb.AppendLine($"PLAYER NOW SAYS: \"{playerMessage}\"");
        sb.AppendLine();
        sb.AppendLine("Give a short response appropriate to your character:");
        
        return sb.ToString();
    }
    
    private string BuildRequestJson(string systemPrompt, string userPrompt)
    {
        var escapedSystem = EscapeJson(systemPrompt);
        var escapedUser = EscapeJson(userPrompt);
        
        return $@"{{
            ""model"": ""{_model}"",
            ""messages"": [
                {{""role"": ""system"", ""content"": ""{escapedSystem}""}},
                {{""role"": ""user"", ""content"": ""{escapedUser}""}}
            ],
            ""temperature"": {_temperature.ToString(System.Globalization.CultureInfo.InvariantCulture)},
            ""max_tokens"": 200
        }}";
    }
    
    private string EscapeJson(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        
        return text
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
    
    private string ExtractContentFromResponse(string json)
    {
        try
        {
            var contentMarker = "\"content\":";
            var contentIndex = json.LastIndexOf(contentMarker);
            
            if (contentIndex == -1)
                return "...";
            
            var startIndex = json.IndexOf('"', contentIndex + contentMarker.Length) + 1;
            var endIndex = startIndex;
            
            while (endIndex < json.Length)
            {
                if (json[endIndex] == '"' && json[endIndex - 1] != '\\')
                    break;
                endIndex++;
            }
            
            var content = json.Substring(startIndex, endIndex - startIndex);
            
            return content
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t")
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\");
        }
        catch
        {
            return "...";
        }
    }
    
    private (string text, string emotion, DialogueIntent intent, bool shouldEnd) ParseResponse(string response)
    {
        var text = response.Trim();
        var emotion = "Neutral";
        var intent = DialogueIntent.Neutral;
        var shouldEnd = false;
        
        // Detect emotion from text markers
        if (text.Contains("*angrily*") || text.Contains("*angry*"))
        {
            emotion = "Angry";
            intent = DialogueIntent.Hostile;
        }
        else if (text.Contains("*smiling*") || text.Contains("*happily*"))
        {
            emotion = "Happy";
            intent = DialogueIntent.Friendly;
        }
        else if (text.Contains("*sad*") || text.Contains("*sadly*"))
        {
            emotion = "Sad";
        }
        else if (text.Contains("*threatening*") || text.Contains("*coldly*"))
        {
            emotion = "Threatening";
            intent = DialogueIntent.Threatening;
        }
        else if (text.Contains("*pleading*") || text.Contains("*begging*"))
        {
            emotion = "Pleading";
            intent = DialogueIntent.Pleading;
        }
        
        // Detect end of conversation
        if (text.Contains("farewell") || text.Contains("leave now") || 
            text.Contains("don't want to talk") || text.Contains("get out"))
        {
            shouldEnd = true;
        }
        
        // Clean up emotion markers for display
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\*[^*]+\*", "").Trim();
        
        return (text, emotion, intent, shouldEnd);
    }
}
