using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LivingInCalradia.AI.Memory;
using LivingInCalradia.Core.Application.Interfaces;
using LivingInCalradia.Core.Domain.ValueObjects;

namespace LivingInCalradia.AI.Orchestration;

/// <summary>
/// Direct Groq API orchestrator with memory support.
/// Uses OpenAI-compatible REST API with personality-based prompts.
/// </summary>
public sealed class GroqOrchestrator : IAgentOrchestrator
{
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly double _temperature;
    private readonly AgentMemory _memory;
    
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
    
    public async Task<AgentDecision> ReasonAsync(
        string agentId,
        WorldPerception perception,
        CancellationToken cancellationToken = default)
    {
        var systemPrompt = GetPersonalityPrompt(agentId);
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
    
    private string GetPersonalityPrompt(string agentId)
    {
        var agentLower = agentId.ToLowerInvariant();
        var basePrompt = "";
        
        if (agentLower.Contains("king") || agentLower.Contains("caladog"))
        {
            basePrompt = "You are King Caladog of Battania. You are a powerful, honorable warrior king. Your decisions should benefit your people and expand your kingdom.";
        }
        else if (agentLower.Contains("merchant") || agentLower.Contains("trader"))
        {
            basePrompt = "You are a wealthy merchant. Making money and expanding your trade network is your top priority.";
        }
        else if (agentLower.Contains("commander") || agentLower.Contains("general"))
        {
            basePrompt = "You are an experienced commander. A tactical genius. Your soldiers' lives matter, but victory above all.";
        }
        else if (agentLower.Contains("villager") || agentLower.Contains("peasant"))
        {
            basePrompt = "You are a peasant. Life is hard, taxes are heavy. You struggle to protect your family and survive.";
        }
        else
        {
            basePrompt = "You are a character living in the world of Mount & Blade II: Bannerlord. Make decisions using logic and reason.";
        }
        
        return basePrompt + " Consider your previous decisions. Be consistent.";
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
        sb.AppendLine("QUESTION: What should you do in this situation?");
        sb.AppendLine();
        sb.AppendLine("Format:");
        sb.AppendLine("THOUGHT: [Analysis]");
        sb.AppendLine("ACTION: [Wait/MoveArmy/Trade/Attack/Defend/Recruit]");
        sb.AppendLine("DETAIL: [Details]");
        
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
