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
    private readonly string _language;
    
    public GroqOrchestrator(
        string apiKey, 
        string model = "llama-3.1-8b-instant", 
        double temperature = 0.7,
        string language = "tr")
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentNullException(nameof(apiKey));
        
        _model = model;
        _temperature = temperature;
        _memory = new AgentMemory(maxMemoriesPerAgent: 5);
        _language = language;
        
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
        
        // Sanitize Turkish characters if using Turkish
        if (_language == "tr")
        {
            messageContent = SanitizeTurkishText(messageContent);
        }
        
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
    
    /// <summary>
    /// Converts Turkish special characters to ASCII equivalents for Bannerlord font compatibility.
    /// </summary>
    private string SanitizeTurkishText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        
        return text
            .Replace('?', 'i')
            .Replace('?', 'I')
            .Replace('?', 's')
            .Replace('?', 'S')
            .Replace('?', 'g')
            .Replace('?', 'G')
            .Replace('ü', 'u')
            .Replace('Ü', 'U')
            .Replace('ö', 'o')
            .Replace('Ö', 'O')
            .Replace('ç', 'c')
            .Replace('Ç', 'C');
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
        var thoughtKey = _language == "tr" ? "DUSUNCE:" : "THOUGHT:";
        var lines = fullResponse.Split('\n');
        foreach (var line in lines)
        {
            if (line.Trim().StartsWith(thoughtKey, StringComparison.OrdinalIgnoreCase))
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
        
        if (_language == "en")
        {
            return GetEnglishPersonalityPrompt(agentLower);
        }
        
        return GetTurkishPersonalityPrompt(agentLower);
    }
    
    private string GetTurkishPersonalityPrompt(string agentLower)
    {
        var basePrompt = "";
        
        if (agentLower.Contains("king") || agentLower.Contains("caladog"))
        {
            basePrompt = "Sen Battania Krali Caladog'sun. Guclu, onurlu ve savasci bir kralsin. Kararlarin halkin iyiligi ve kralliginin genislemesi icin olmali.";
        }
        else if (agentLower.Contains("merchant") || agentLower.Contains("trader"))
        {
            basePrompt = "Sen zengin bir tuccarsin. Para kazanmak ve ticaret agini genisletmek en buyuk onceligin.";
        }
        else if (agentLower.Contains("commander") || agentLower.Contains("general"))
        {
            basePrompt = "Sen deneyimli bir komutansin. Taktik dahisi bir generalsin. Askerlerinin hayati onemli ama zafer her seyden ustun.";
        }
        else if (agentLower.Contains("villager") || agentLower.Contains("peasant"))
        {
            basePrompt = "Sen bir koylusun. Hayat zor, vergiler agir. Aileni korumak ve hayatta kalmak icin mucadele ediyorsun.";
        }
        else
        {
            basePrompt = "Sen Mount & Blade II: Bannerlord dunyasinda yasayan bir karaktersin. Mantik yuruterek kararlar ver.";
        }
        
        return basePrompt + " Onceki kararlarini goz onunde bulundur. Tutarli ol. ONEMLI: Turkce ozel karakterler KULLANMA (i, s, g, u, o, c kullan).";
    }
    
    private string GetEnglishPersonalityPrompt(string agentLower)
    {
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
        if (_language == "en")
        {
            return BuildEnglishUserPrompt(agentId, perception, memoryContext);
        }
        
        return BuildTurkishUserPrompt(agentId, perception, memoryContext);
    }
    
    private string BuildTurkishUserPrompt(string agentId, WorldPerception perception, string memoryContext)
    {
        var sb = new StringBuilder();
        sb.AppendLine("MEVCUT DURUM:");
        sb.AppendLine($"Karakter: {agentId}");
        sb.AppendLine($"Konum: {perception.Location}");
        sb.AppendLine($"Zaman: {perception.Timestamp:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"Hava: {perception.Weather}");
        sb.AppendLine();
        sb.AppendLine("EKONOMI:");
        sb.AppendLine($"Refah: {perception.Economy.Prosperity}");
        sb.AppendLine($"Gida: {perception.Economy.FoodSupply}");
        sb.AppendLine($"Vergi: %{perception.Economy.TaxRate}");
        sb.AppendLine();
        sb.AppendLine("ILISKILER:");
        foreach (var rel in perception.Relations)
        {
            var status = rel.Value >= 50 ? "Muttefik" : rel.Value >= 0 ? "Notr" : rel.Value >= -50 ? "Gergin" : "Dusman";
            sb.AppendLine($"  {rel.Key}: {rel.Value} ({status})");
        }
        sb.AppendLine();
        sb.AppendLine("HAFIZA:");
        sb.AppendLine(memoryContext);
        sb.AppendLine();
        sb.AppendLine("SORU: Bu durumda ne yapmalisin?");
        sb.AppendLine();
        sb.AppendLine("Format:");
        sb.AppendLine("DUSUNCE: [Analiz]");
        sb.AppendLine("AKSIYON: [Wait/MoveArmy/Trade/Attack/Defend/Recruit]");
        sb.AppendLine("DETAY: [Detaylar]");
        
        return sb.ToString();
    }
    
    private string BuildEnglishUserPrompt(string agentId, WorldPerception perception, string memoryContext)
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
            
            if (trimmed.StartsWith("AKSIYON:", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("ACTION:", StringComparison.OrdinalIgnoreCase))
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
            else if (trimmed.StartsWith("DETAY:", StringComparison.OrdinalIgnoreCase) ||
                     trimmed.StartsWith("DETAIL:", StringComparison.OrdinalIgnoreCase))
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
