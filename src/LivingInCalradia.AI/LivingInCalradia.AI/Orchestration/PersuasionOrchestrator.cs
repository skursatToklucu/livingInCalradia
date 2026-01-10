using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LivingInCalradia.AI.Memory;
using LivingInCalradia.AI.Personality;
using LivingInCalradia.Core.Application.Interfaces;

namespace LivingInCalradia.AI.Orchestration;

/// <summary>
/// Handles player persuasion attempts to influence NPC decisions.
/// NPCs evaluate player requests based on their personality and relationship.
/// </summary>
public sealed class PersuasionOrchestrator
{
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly double _temperature;
    private readonly PersonalityGenerator _personalityGenerator;
    
    public PersuasionOrchestrator(
        string apiKey,
        string model = "llama-3.1-8b-instant",
        double temperature = 0.7)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentNullException(nameof(apiKey));
        
        _model = model;
        _temperature = temperature;
        _personalityGenerator = new PersonalityGenerator();
        
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.groq.com/openai/v1/"),
            Timeout = TimeSpan.FromSeconds(60)
        };
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }
    
    /// <summary>
    /// Player attempts to persuade an NPC to take a specific action.
    /// Returns the NPC's response and whether they agreed.
    /// </summary>
    public async Task<PersuasionResult> TryPersuadeAsync(
        string npcId,
        string npcName,
        string playerRequest,
        int relationWithPlayer,
        string? kingdomName = null,
        bool isKing = false,
        CancellationToken cancellationToken = default)
    {
        var personality = _personalityGenerator.GetPersonality(
            npcId, null, kingdomName, isKing, isKing);
        
        var systemPrompt = BuildPersuasionSystemPrompt(npcName, personality, relationWithPlayer);
        var userPrompt = BuildPersuasionUserPrompt(playerRequest, relationWithPlayer);
        
        var json = BuildRequestJson(systemPrompt, userPrompt);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync();
        
        if (!response.IsSuccessStatusCode)
        {
            return new PersuasionResult
            {
                Success = false,
                NpcResponse = "I cannot discuss this right now.",
                ActionToTake = null,
                Reasoning = $"API Error: {response.StatusCode}"
            };
        }
        
        var messageContent = ExtractContentFromResponse(responseJson);
        return ParsePersuasionResponse(messageContent);
    }
    
    private string BuildPersuasionSystemPrompt(string npcName, LordPersonality personality, int relation)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"You are {npcName}, a noble lord in the medieval world of Mount & Blade II: Bannerlord.");
        sb.AppendLine();
        sb.AppendLine("SPEECH STYLE - IMPORTANT:");
        sb.AppendLine("- Speak in a FORMAL, NOBLE manner befitting a medieval lord");
        sb.AppendLine("- Use dignified language but keep it UNDERSTANDABLE");
        sb.AppendLine("- You may use: 'Aye' (yes), 'Nay' (no), 'My lord', 'Indeed', 'Very well'");
        sb.AppendLine("- Avoid modern slang, keep sentences formal and authoritative");
        sb.AppendLine("- Example: 'I shall consider your words carefully. This is a bold request.'");
        sb.AppendLine();
        sb.AppendLine("YOUR PERSONALITY:");
        sb.AppendLine(personality.GetPersonalityDescription());
        sb.AppendLine();
        sb.AppendLine("YOUR TENDENCIES:");
        sb.AppendLine(personality.GetActionTendencies());
        sb.AppendLine();
        sb.AppendLine($"YOUR RELATIONSHIP WITH THE PLAYER: {relation}");
        
        if (relation >= 50)
            sb.AppendLine("You consider the player a trusted friend and ally.");
        else if (relation >= 20)
            sb.AppendLine("You have a positive view of the player.");
        else if (relation >= 0)
            sb.AppendLine("You are neutral towards the player.");
        else if (relation >= -30)
            sb.AppendLine("You are wary of the player.");
        else
            sb.AppendLine("You dislike the player and are suspicious of their motives.");
        
        sb.AppendLine();
        sb.AppendLine("IMPORTANT RULES:");
        sb.AppendLine("- Evaluate the player's request based on YOUR personality");
        sb.AppendLine("- Consider if the request benefits YOU and YOUR kingdom");
        sb.AppendLine("- Your relationship with the player affects your willingness");
        sb.AppendLine("- If you ACCEPT, you will ACTUALLY perform the action");
        sb.AppendLine("- You can NEGOTIATE, ASK FOR SOMETHING IN RETURN, or REFUSE");
        sb.AppendLine("- Stay in character - proud lords don't like being ordered around");
        
        return sb.ToString();
    }
    
    private string BuildPersuasionUserPrompt(string playerRequest, int relation)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("The player approaches you and says:");
        sb.AppendLine($"\"{playerRequest}\"");
        sb.AppendLine();
        sb.AppendLine("How do you respond? Consider:");
        sb.AppendLine("1. Does this request align with your goals?");
        sb.AppendLine("2. Is this in your best interest?");
        sb.AppendLine("3. Do you trust the player enough?");
        sb.AppendLine("4. What would someone with YOUR personality do?");
        sb.AppendLine();
        sb.AppendLine("FORMAT YOUR RESPONSE EXACTLY AS:");
        sb.AppendLine("DECISION: [ACCEPT/REFUSE/NEGOTIATE]");
        sb.AppendLine("RESPONSE: [What you say to the player - be formal and in character]");
        sb.AppendLine("ACTION: [If accepting, what action will you take? e.g., Attack, MoveArmy, DeclareWar, MakePeace, etc. If refusing, write 'None']");
        sb.AppendLine("REASONING: [Your internal thoughts - why you decided this way]");
        
        return sb.ToString();
    }
    
    private PersuasionResult ParsePersuasionResponse(string response)
    {
        var result = new PersuasionResult();
        
        var lines = response.Split('\n');
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            if (trimmed.StartsWith("DECISION:", StringComparison.OrdinalIgnoreCase))
            {
                var decision = trimmed.Substring(trimmed.IndexOf(':') + 1).Trim().ToUpperInvariant();
                result.Success = decision.Contains("ACCEPT");
                result.Negotiating = decision.Contains("NEGOTIATE");
            }
            else if (trimmed.StartsWith("RESPONSE:", StringComparison.OrdinalIgnoreCase))
            {
                result.NpcResponse = trimmed.Substring(trimmed.IndexOf(':') + 1).Trim();
            }
            else if (trimmed.StartsWith("ACTION:", StringComparison.OrdinalIgnoreCase))
            {
                var action = trimmed.Substring(trimmed.IndexOf(':') + 1).Trim();
                if (!action.Equals("None", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(action))
                {
                    result.ActionToTake = action;
                }
            }
            else if (trimmed.StartsWith("REASONING:", StringComparison.OrdinalIgnoreCase))
            {
                result.Reasoning = trimmed.Substring(trimmed.IndexOf(':') + 1).Trim();
            }
        }
        
        // Fallback if parsing failed
        if (string.IsNullOrEmpty(result.NpcResponse))
        {
            result.NpcResponse = response.Length > 200 ? response.Substring(0, 200) : response;
        }
        
        return result;
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
            ""max_tokens"": 500
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
                return "No response";
            
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
            return "Parse error";
        }
    }
}

/// <summary>
/// Result of a player persuasion attempt.
/// </summary>
public sealed class PersuasionResult
{
    /// <summary>
    /// Whether the NPC accepted the player's request.
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Whether the NPC wants to negotiate.
    /// </summary>
    public bool Negotiating { get; set; }
    
    /// <summary>
    /// What the NPC says to the player (in character).
    /// </summary>
    public string NpcResponse { get; set; } = "";
    
    /// <summary>
    /// The action the NPC will take if they accepted.
    /// </summary>
    public string? ActionToTake { get; set; }
    
    /// <summary>
    /// The NPC's internal reasoning (for debugging/display).
    /// </summary>
    public string? Reasoning { get; set; }
}
