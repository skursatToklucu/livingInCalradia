using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LivingInCalradia.AI.Memory;
using LivingInCalradia.AI.Personality;
using LivingInCalradia.Core.Application.Interfaces;
using LivingInCalradia.Core.Domain.ValueObjects;

namespace LivingInCalradia.AI.Orchestration;

/// <summary>
/// Direct Groq API orchestrator with memory and personality support.
/// Uses OpenAI-compatible REST API with dynamic personality-based prompts.
/// </summary>
public sealed class GroqOrchestrator : IAgentOrchestrator
{
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly double _temperature;
    private readonly AgentMemory _memory;
    private readonly PersonalityGenerator _personalityGenerator;
    
    public GroqOrchestrator(
        string apiKey, 
        string model = "llama-3.1-8b-instant", 
        double temperature = 0.7,
        string language = "en")
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentNullException(nameof(apiKey));
        
        _model = model;
        _temperature = temperature;
        _memory = new AgentMemory(maxMemoriesPerAgent: 5);
        _personalityGenerator = new PersonalityGenerator();
        
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.groq.com/openai/v1/"),
            Timeout = TimeSpan.FromSeconds(60)
        };
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }
    
    /// <summary>
    /// Gets the agent memory instance for external access.
    /// </summary>
    public AgentMemory Memory => _memory;
    
    /// <summary>
    /// Gets the personality generator for external access.
    /// </summary>
    public PersonalityGenerator PersonalityGenerator => _personalityGenerator;
    
    public async Task<AgentDecision> ReasonAsync(
        string agentId,
        WorldPerception perception,
        CancellationToken cancellationToken = default)
    {
        var systemPrompt = BuildPersonalityPrompt(agentId, perception);
        var memoryContext = _memory.GetMemoryContext(agentId);
        var userPrompt = BuildUserPrompt(agentId, perception, memoryContext);

        // Build JSON manually to avoid System.Text.Json issues
        var json = BuildRequestJson(systemPrompt, userPrompt);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync();
        
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Groq API error: {response.StatusCode}\n{responseJson}");
        }
        
        // Parse response manually
        var messageContent = ExtractContentFromResponse(responseJson);
        
        Console.WriteLine($"\n[AI RESPONSE]\n{messageContent}\n");
        
        // Parse action from response
        var actions = ParseActions(messageContent);
        var decision = new AgentDecision(agentId, messageContent, actions);
        
        // Store in memory
        var actionName = actions.Count > 0 ? actions[0].ActionType : "Wait";
        var shortDecision = ExtractShortDecision(messageContent);
        _memory.Remember(agentId, perception.Location, shortDecision, actionName);
        
        return decision;
    }
    
    private string BuildRequestJson(string systemPrompt, string userPrompt)
    {
        // Escape special characters for JSON
        var escapedSystem = EscapeJson(systemPrompt);
        var escapedUser = EscapeJson(userPrompt);
        
        return $@"{{
            ""model"": ""{_model}"",
            ""messages"": [
                {{""role"": ""system"", ""content"": ""{escapedSystem}""}},
                {{""role"": ""user"", ""content"": ""{escapedUser}""}}
            ],
            ""temperature"": {_temperature.ToString(System.Globalization.CultureInfo.InvariantCulture)},
            ""max_tokens"": 600
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
            // Simple parsing - find "content": "..." in response
            var contentMarker = "\"content\":";
            var contentIndex = json.LastIndexOf(contentMarker);
            
            if (contentIndex == -1)
                return "No response";
            
            var startIndex = json.IndexOf('"', contentIndex + contentMarker.Length) + 1;
            var endIndex = startIndex;
            
            // Find the closing quote, handling escaped quotes
            while (endIndex < json.Length)
            {
                if (json[endIndex] == '"' && json[endIndex - 1] != '\\')
                    break;
                endIndex++;
            }
            
            var content = json.Substring(startIndex, endIndex - startIndex);
            
            // Unescape
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
    
    private string ExtractShortDecision(string fullResponse)
    {
        // Extract the THOUGHT part for memory
        var lines = fullResponse.Split('\n');
        foreach (var line in lines)
        {
            if (line.Trim().StartsWith("THOUGHT:", StringComparison.OrdinalIgnoreCase))
            {
                return line.Substring(line.IndexOf(':') + 1).Trim();
            }
        }
        
        // Fallback: return first 100 chars
        if (fullResponse.Length > 100)
            return fullResponse.Substring(0, 100) + "...";
        return fullResponse;
    }
    
    private string BuildPersonalityPrompt(string agentId, WorldPerception perception)
    {
        var sb = new StringBuilder();
        
        // Extract info from agentId (format: "Lord_Name_Clan")
        var parts = agentId.Split('_');
        var heroType = parts.Length > 0 ? parts[0] : "Lord";
        var heroName = parts.Length > 1 ? parts[1] : "Unknown";
        var clanName = parts.Length > 2 ? parts[2] : null;
        
        var isKing = heroType.Equals("King", StringComparison.OrdinalIgnoreCase) || 
                     agentId.ToLowerInvariant().Contains("king");
        var isFactionLeader = isKing || heroType.Equals("Leader", StringComparison.OrdinalIgnoreCase);
        
        // Get or generate personality for this lord
        var personality = _personalityGenerator.GetPersonality(
            agentId, 
            clanName, 
            perception.Relations.Count > 0 ? null : null,
            isFactionLeader,
            isKing);
        
        // Build the prompt with light medieval style (formal but understandable)
        sb.AppendLine($"You are {heroName}, a noble {heroType} in the medieval world of Mount & Blade II: Bannerlord.");
        sb.AppendLine();
        sb.AppendLine("SPEECH STYLE - IMPORTANT:");
        sb.AppendLine("- Speak in a FORMAL, NOBLE manner befitting a medieval lord");
        sb.AppendLine("- Use dignified language but keep it UNDERSTANDABLE");
        sb.AppendLine("- You may use simple medieval words: 'Aye' (yes), 'Nay' (no), 'My lord', 'Indeed'");
        sb.AppendLine("- Avoid modern slang, keep sentences formal and authoritative");
        sb.AppendLine("- Example: 'I shall lead my forces to battle. The enemy will know our strength.'");
        sb.AppendLine("- NOT like this: 'Gonna attack them, they're weak lol'");
        sb.AppendLine();
        sb.AppendLine("YOUR PERSONALITY:");
        sb.AppendLine(personality.GetPersonalityDescription());
        sb.AppendLine();
        sb.AppendLine("YOUR TENDENCIES:");
        sb.AppendLine(personality.GetActionTendencies());
        sb.AppendLine();
        sb.AppendLine("IMPORTANT RULES:");
        sb.AppendLine("- Stay true to your personality traits");
        sb.AppendLine("- Your decisions should reflect your values and temperament");
        sb.AppendLine("- Consider your previous decisions for consistency");
        sb.AppendLine("- Speak with authority and dignity");
        
        return sb.ToString();
    }
    
    private string GetPersonalityPrompt(string agentId)
    {
        // Legacy method - use basic personality without perception
        var parts = agentId.Split('_');
        var heroType = parts.Length > 0 ? parts[0] : "Lord";
        var heroName = parts.Length > 1 ? parts[1] : "Unknown";
        var clanName = parts.Length > 2 ? parts[2] : null;
        
        var isKing = heroType.Equals("King", StringComparison.OrdinalIgnoreCase) || 
                     agentId.ToLowerInvariant().Contains("king");
        var isFactionLeader = isKing || heroType.Equals("Leader", StringComparison.OrdinalIgnoreCase);
        
        var personality = _personalityGenerator.GetPersonality(agentId, clanName, null, isFactionLeader, isKing);
        
        var sb = new StringBuilder();
        sb.AppendLine($"You are {heroName}, a noble {heroType} in Mount & Blade II: Bannerlord.");
        sb.AppendLine();
        sb.AppendLine("SPEECH STYLE: Formal, noble, dignified. Use 'Aye', 'Nay', 'Indeed', 'My lord'. Avoid modern slang.");
        sb.AppendLine();
        sb.AppendLine("YOUR PERSONALITY:");
        sb.AppendLine(personality.GetPersonalityDescription());
        sb.AppendLine();
        sb.AppendLine("YOUR TENDENCIES:");
        sb.AppendLine(personality.GetActionTendencies());
        
        return sb.ToString();
    }
    
    private string BuildUserPrompt(string agentId, WorldPerception perception, string memoryContext)
    {
        var sb = new StringBuilder();
        sb.AppendLine("CURRENT SITUATION:");
        sb.AppendLine($"Character: {agentId}");
        sb.AppendLine($"Location: {perception.Location}");
        sb.AppendLine($"Time: {perception.Timestamp:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"Weather: {perception.Weather}");
        sb.AppendLine();
        sb.AppendLine("ECONOMY:");
        sb.AppendLine($"Prosperity: {perception.Economy.Prosperity}");
        sb.AppendLine($"Food Supply: {perception.Economy.FoodSupply}");
        sb.AppendLine($"Tax Rate: {perception.Economy.TaxRate}%");
        sb.AppendLine();
        sb.AppendLine("RELATIONS:");
        foreach (var rel in perception.Relations)
        {
            var status = rel.Value >= 50 ? "Allied" : rel.Value >= 0 ? "Neutral" : rel.Value >= -50 ? "Tense" : "Hostile";
            sb.AppendLine($"  {rel.Key}: {rel.Value} ({status})");
        }
        sb.AppendLine();
        sb.AppendLine("MEMORY:");
        sb.AppendLine(memoryContext);
        sb.AppendLine();
        sb.AppendLine("Based on YOUR PERSONALITY and the current situation, what should you do?");
        sb.AppendLine();
        sb.AppendLine("AVAILABLE ACTIONS:");
        sb.AppendLine("- Wait: Do nothing, observe");
        sb.AppendLine("- MoveArmy: Move to a settlement or location");
        sb.AppendLine("- Attack: Engage an enemy party");
        sb.AppendLine("- Defend: Defend a settlement");
        sb.AppendLine("- Retreat: Fall back to safety");
        sb.AppendLine("- Patrol: Patrol around a settlement");
        sb.AppendLine("- StartSiege: Begin siege of enemy settlement");
        sb.AppendLine("- RecruitTroops: Recruit soldiers");
        sb.AppendLine("- Trade: Conduct trade");
        sb.AppendLine("- GiveGold: Give gold to someone");
        sb.AppendLine("- PayRansom: Pay ransom for a prisoner");
        sb.AppendLine("- ChangeRelation: Improve/worsen relation");
        sb.AppendLine("- DeclareWar: Declare war on faction");
        sb.AppendLine("- MakePeace: Make peace with faction");
        sb.AppendLine("- ChangeKingdom: Join a different kingdom");
        sb.AppendLine("- ProposeMarriage: Propose marriage");
        sb.AppendLine();
        sb.AppendLine("FORMAT YOUR RESPONSE EXACTLY AS:");
        sb.AppendLine("THOUGHT: [Your analysis - speak formally as a lord]");
        sb.AppendLine("ACTION: [One action from the list above]");
        sb.AppendLine("DETAIL: [Specific details about the action]");
        
        return sb.ToString();
    }
    
    private List<AgentAction> ParseActions(string response)
    {
        var actions = new List<AgentAction>();
        
        var lines = response.Split('\n');
        string? actionType = null;
        string? detail = null;
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            if (trimmed.StartsWith("ACTION:", StringComparison.OrdinalIgnoreCase))
            {
                actionType = trimmed.Substring(trimmed.IndexOf(':') + 1).Trim();
                
                // Clean up action type - take only the first word
                if (actionType.Contains(","))
                {
                    actionType = actionType.Split(',')[0].Trim();
                }
                if (actionType.Contains(" "))
                {
                    actionType = actionType.Split(' ')[0].Trim();
                }
            }
            else if (trimmed.StartsWith("DETAIL:", StringComparison.OrdinalIgnoreCase))
            {
                detail = trimmed.Substring(trimmed.IndexOf(':') + 1).Trim();
            }
        }
        
        if (!string.IsNullOrWhiteSpace(actionType) && 
            !actionType.Equals("Wait", StringComparison.OrdinalIgnoreCase))
        {
            var parameters = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(detail))
            {
                parameters["detail"] = detail;
            }
            actions.Add(new AgentAction(actionType, parameters));
        }
        
        // Default to Wait if no action found
        if (actions.Count == 0)
        {
            actions.Add(new AgentAction("Wait", new Dictionary<string, object> { ["duration"] = 60 }));
        }
        
        return actions;
    }
}
