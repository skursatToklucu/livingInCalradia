using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LivingInCalradia.AI.Memory;
using LivingInCalradia.AI.Personality;
using LivingInCalradia.Core.Application.Interfaces;

namespace LivingInCalradia.AI.Orchestration;

/// <summary>
/// AI-powered dialogue orchestrator using Groq API.
/// Generates dynamic, contextual NPC responses with personality support.
/// </summary>
public sealed class GroqDialogueOrchestrator : IDialogueOrchestrator
{
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly double _temperature;
    private readonly ConversationMemory _conversationMemory;
    private readonly PersonalityGenerator _personalityGenerator;
    
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
        _personalityGenerator = new PersonalityGenerator();
        
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
    
    /// <summary>
    /// Gets the personality generator for external access.
    /// </summary>
    public PersonalityGenerator PersonalityGenerator => _personalityGenerator;
    
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
                return DialogueResponse.Error("Hmm... I seem to have lost my train of thought.");
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
            return DialogueResponse.Error("*looks at you thoughtfully*");
        }
    }
    
    private string BuildSystemPrompt(string npcName, string npcRole, DialogueContext context)
    {
        var sb = new StringBuilder();
        
        // Get personality for this NPC
        var npcId = $"{npcRole}_{npcName}";
        var isKing = npcRole.ToLowerInvariant().Contains("king");
        var isLord = npcRole.ToLowerInvariant().Contains("lord") || isKing;
        var personality = _personalityGenerator.GetPersonality(npcId, null, context.NpcFaction, isLord, isKing);
        
        // Base identity
        sb.AppendLine($"You are {npcName}, a {GetRoleDescription(npcRole)} in Mount & Blade II: Bannerlord.");
        sb.AppendLine();
        
        // Speech style - formal but understandable
        sb.AppendLine("SPEECH STYLE - IMPORTANT:");
        sb.AppendLine("- Speak in a FORMAL, dignified manner befitting a medieval character");
        sb.AppendLine("- Keep responses SHORT (1-3 sentences)");
        sb.AppendLine("- You may use: 'Aye', 'Nay', 'Indeed', 'Very well', 'My lord'");
        sb.AppendLine("- Avoid modern slang and casual speech");
        sb.AppendLine("- Stay in character at all times");
        sb.AppendLine();
        
        // Personality traits (only for lords/important NPCs)
        if (isLord)
        {
            sb.AppendLine("YOUR PERSONALITY:");
            sb.AppendLine(personality.GetPersonalityDescription());
            sb.AppendLine();
        }
        
        // Role-specific traits
        sb.AppendLine("YOUR CHARACTER:");
        sb.AppendLine(GetPersonalityTraits(npcRole));
        sb.AppendLine();
        
        // Relationship context
        sb.AppendLine("RELATIONSHIP WITH PLAYER:");
        if (context.RelationWithPlayer >= 50)
        {
            sb.AppendLine("You consider this person a trusted friend. Be warm and helpful.");
        }
        else if (context.RelationWithPlayer >= 20)
        {
            sb.AppendLine("You have a positive view of this person. Be polite and respectful.");
        }
        else if (context.RelationWithPlayer >= 0)
        {
            sb.AppendLine("You are neutral towards this person. Be formal and businesslike.");
        }
        else if (context.RelationWithPlayer >= -30)
        {
            sb.AppendLine("You are wary of this person. Be cold and distant.");
        }
        else
        {
            sb.AppendLine("You dislike this person. Be hostile and dismissive.");
        }
        
        // War context
        if (context.IsAtWar)
        {
            sb.AppendLine();
            sb.AppendLine("WARNING: Your factions are at WAR! Be very cautious and potentially hostile.");
        }
        
        // Mood
        sb.AppendLine();
        sb.AppendLine($"Your current mood: {context.NpcMood}");
        
        return sb.ToString();
    }
    
    private string GetRoleDescription(string role)
    {
        var roleLower = role.ToLowerInvariant();
        
        if (roleLower.Contains("king"))
            return "powerful and respected king";
        if (roleLower.Contains("lord"))
            return "noble lord and warrior";
        if (roleLower.Contains("merchant"))
            return "shrewd merchant";
        if (roleLower.Contains("blacksmith"))
            return "skilled blacksmith";
        if (roleLower.Contains("tavern"))
            return "experienced tavern keeper";
        if (roleLower.Contains("villager"))
            return "humble villager";
        if (roleLower.Contains("soldier"))
            return "battle-hardened soldier";
        if (roleLower.Contains("commander"))
            return "seasoned military commander";
        if (roleLower.Contains("lady"))
            return "noble lady of high standing";
        if (roleLower.Contains("bandit"))
            return "dangerous outlaw";
        if (roleLower.Contains("gang"))
            return "ruthless gang leader";
        if (roleLower.Contains("wanderer"))
            return "mysterious wanderer";
            
        return "resident of Calradia";
    }
    
    private string GetPersonalityTraits(string role)
    {
        var roleLower = role.ToLowerInvariant();
        
        if (roleLower.Contains("king"))
            return "You are authoritative and wise. You speak as a ruler, with dignity and power. Your kingdom's interests come first.";
            
        if (roleLower.Contains("lord"))
            return "You are proud and honorable. You speak with the authority of a noble warrior. Honor and duty guide your words.";
            
        if (roleLower.Contains("merchant"))
            return "You are clever and opportunistic. You see profit in every conversation. Gold is your primary concern.";
            
        if (roleLower.Contains("blacksmith"))
            return "You are practical and hardworking. You speak little but value quality craftsmanship above all.";
            
        if (roleLower.Contains("tavern"))
            return "You are friendly and well-informed. You hear all the gossip and know everyone's business.";
            
        if (roleLower.Contains("villager"))
            return "You are humble and cautious. Life is hard, and you respect those with power. You speak simply.";
            
        if (roleLower.Contains("soldier"))
            return "You are disciplined and loyal. You follow orders and respect the chain of command. War is your life.";
            
        if (roleLower.Contains("bandit"))
            return "You are dangerous and cunning. You respect only strength. You take what you want.";
        
        if (roleLower.Contains("wanderer"))
            return "You are mysterious and experienced. You have traveled far and seen much. You speak with wisdom.";
            
        return "You are a common person trying to survive in a harsh world. You speak plainly and honestly.";
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
