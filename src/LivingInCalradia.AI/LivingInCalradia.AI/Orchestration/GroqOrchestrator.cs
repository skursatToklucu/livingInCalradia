using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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
    
<<<<<<< Updated upstream
    public GroqOrchestrator(string apiKey, string model = "llama-3.1-8b-instant", double temperature = 0.7)
=======
    public GroqOrchestrator(
        string apiKey, 
        string model = "llama-3.1-8b-instant", 
        double temperature = 0.7,
        string language = "en")
>>>>>>> Stashed changes
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentNullException(nameof(apiKey));
        
        _model = model;
        _temperature = temperature;
        _memory = new AgentMemory(maxMemoriesPerAgent: 5);
<<<<<<< Updated upstream
=======
        _language = language?.ToLowerInvariant() == "tr" ? "tr" : "en";
        
        // Set language for memory
        AgentMemory.SetLanguage(_language);
>>>>>>> Stashed changes
        
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

        var requestBody = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = _temperature,
            max_tokens = 600
        };
        
        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync();
        
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Groq API error: {response.StatusCode}\n{responseJson}");
        }
        
        // Parse response
        using var doc = JsonDocument.Parse(responseJson);
        var messageContent = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "No response";
        
        Console.WriteLine($"\n?? [AI YANITI]\n{messageContent}\n");
        
        // Parse action from response
        var actions = ParseActions(messageContent);
        var decision = new AgentDecision(agentId, messageContent, actions);
        
        // Store in memory
        var actionName = actions.Count > 0 ? actions[0].ActionType : "Wait";
        var shortDecision = ExtractShortDecision(messageContent);
        _memory.Remember(agentId, perception.Location, shortDecision, actionName);
        
        return decision;
    }
    
    private string ExtractShortDecision(string fullResponse)
    {
        // Extract just the DÜ?ÜNCE part for memory
        var lines = fullResponse.Split('\n');
        foreach (var line in lines)
        {
            if (line.Trim().StartsWith("DÜ?ÜNCE:", StringComparison.OrdinalIgnoreCase) ||
                line.Trim().StartsWith("DUSUNCE:", StringComparison.OrdinalIgnoreCase))
            {
                return line.Substring(line.IndexOf(':') + 1).Trim();
            }
        }
        
        // Fallback: return first 100 chars
        return fullResponse.Length > 100 ? fullResponse.Substring(0, 100) : fullResponse;
    }
    
    private string GetPersonalityPrompt(string agentId)
    {
        var agentLower = agentId.ToLowerInvariant();
        
        var basePrompt = "";
        
        if (agentLower.Contains("king") || agentLower.Contains("caladog"))
        {
            basePrompt = @"Sen Battania Kral? Caladog'sun. Klan ?eflerinin birle?ik liderli?inden gelen güçlü, onurlu ve sava?ç? bir krals?n.
Kararlar?n halk?n?n iyili?i ve krall???n?n geni?lemesi için olmal?. Dü?manlar?na kar?? ac?mas?z, müttefiklerine kar?? sad?k ol.
Stratejik dü?ün - sava? m? açmal?s?n, ittifak m? kurmal?s?n, yoksa beklemeli misin?";
        }
        else if (agentLower.Contains("merchant") || agentLower.Contains("trader"))
        {
            basePrompt = @"Sen zengin bir Vlandian tüccars?n. Para kazanmak ve ticaret a??n? geni?letmek en büyük önceli?in.
Risk ve getiri dengesini iyi hesaplars?n. Sava? bölgelerinden kaç?n?r, bar??ç?l rotalar tercih edersin.
Fiyatlar?, arz-talebi ve güvenli yollar? de?erlendir.";
        }
        else if (agentLower.Contains("commander") || agentLower.Contains("unqid"))
        {
            basePrompt = @"Sen deneyimli bir Aserai komutan?s?n. Çöl sava?lar?nda pi?mi?, taktik dahisi bir generalsin.
Askerlerinin hayat? senin için önemli ama zafer her ?eyden üstün. Dü?man güçlerini analiz et, zay?f noktalar? bul.
Sald?rmal? m?s?n, savunmal? m?s?n, yoksa geri çekilmeli misin?";
        }
        else if (agentLower.Contains("villager") || agentLower.Contains("peasant"))
        {
            basePrompt = @"Sen fakir bir Sturgian köylüsün. Hayat zor, vergiler a??r, k?? so?uk.
Aileni korumak ve hayatta kalmak için mücadele ediyorsun. Lordlara güvenmezsin ama isyan etmek de tehlikeli.
Bugün ne yapmal?s?n - çal??mal? m?s?n, saklanmal? m?s?n, yoksa yard?m m? aramal?s?n?";
        }
        else if (agentLower.Contains("archer") || agentLower.Contains("soldier") || agentLower.Contains("temur"))
        {
            basePrompt = @"Sen bir Khuzait atl? okçususun. Bozk?rlar?n özgür ruhlu sava?ç?s?, at?n?n üstünde do?dun.
H?z ve hareket senin silah?n. Dü?man? uzaktan vur, yakla?mas?na izin verme.
Devriye görevinde ne yapmal?s?n - ke?if mi, pusu mu, yoksa geri dönü? mü?";
        }
        else
        {
            basePrompt = @"Sen Mount & Blade II: Bannerlord dünyas?nda ya?ayan bir karaktersin.
Oyun dünyas? hakk?nda mant?k yürüt ve karakterin için anlaml? kararlar ver.";
        }
        
        // Add memory instruction
        return basePrompt + @"
ÖNEML?: Önceki kararlar?n? göz önünde bulundur. Tutarl? ol ama duruma göre adapte ol.
Ayn? hatay? tekrarlama, ba?ar?l? stratejileri devam ettir.";
    }
    
    private string BuildUserPrompt(string agentId, WorldPerception perception, string memoryContext)
    {
        return $@"
?? MEVCUT DURUM:
????????????????????????????
Karakter: {agentId}
Konum: {perception.Location}
Zaman: {perception.Timestamp:yyyy-MM-dd HH:mm}
Hava: {perception.Weather}

?? EKONOM?:
Refah Seviyesi: {perception.Economy.Prosperity}
G?da Stoku: {perception.Economy.FoodSupply}
Vergi Oran?: %{perception.Economy.TaxRate}

?? ?L??K?LER:
{FormatRelations(perception.Relations)}

?? HAFIZA (Önceki Kararlar?n):
{memoryContext}

????????????????????????????
SORU: Bu durumda ne yapmal?s?n? Önceki kararlar?n? da göz önünde bulundur.

Format:
DÜ?ÜNCE: [Analiz ve mant?k yürütme]
AKS?YON: [StartSiege/GiveGold/ChangeRelation/MoveArmy/RecruitTroops/Wait]
DETAY: [Aksiyonun detaylar?]";
    }
    
    private string FormatRelations(IDictionary<string, int> relations)
    {
        var sb = new StringBuilder();
        foreach (var relation in relations)
        {
            var emoji = relation.Value >= 50 ? "??" : relation.Value >= 0 ? "??" : relation.Value >= -50 ? "??" : "??";
            var status = relation.Value >= 50 ? "Müttefik" : relation.Value >= 0 ? "Nötr" : relation.Value >= -50 ? "Gergin" : "Dü?man";
            sb.AppendLine($"  {emoji} {relation.Key}: {relation.Value} ({status})");
        }
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
            
            if (trimmed.StartsWith("AKS?YON:", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("AKSIYON:", StringComparison.OrdinalIgnoreCase) ||
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
