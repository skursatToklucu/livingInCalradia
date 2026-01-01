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
                return DialogueResponse.Error("Hmm... Bir ?ey söyleyecektim ama unuttum.");
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
            return DialogueResponse.Error("*dü?ünceli bir ?ekilde bakar*");
        }
    }
    
    private string BuildSystemPrompt(string npcName, string npcRole, DialogueContext context)
    {
        var sb = new StringBuilder();
        
        // Base personality based on role
        sb.AppendLine($"Sen {npcName} ad?nda bir {GetRoleDescription(npcRole)}.");
        sb.AppendLine($"Mount & Blade II: Bannerlord dünyas?nda ya??yorsun.");
        sb.AppendLine();
        
        // Role-specific personality
        sb.AppendLine(GetPersonalityTraits(npcRole));
        sb.AppendLine();
        
        // Relationship context
        if (context.RelationWithPlayer >= 50)
        {
            sb.AppendLine("Bu ki?iyle aran çok iyi, ona güveniyorsun ve dostça konu?uyorsun.");
        }
        else if (context.RelationWithPlayer >= 0)
        {
            sb.AppendLine("Bu ki?iyle aran normal, resmi ama nazik konu?uyorsun.");
        }
        else if (context.RelationWithPlayer >= -50)
        {
            sb.AppendLine("Bu ki?iyle aran gergin, so?uk ve mesafeli konu?uyorsun.");
        }
        else
        {
            sb.AppendLine("Bu ki?iden nefret ediyorsun, dü?manca ve tehditkar konu?uyorsun.");
        }
        
        // War context
        if (context.IsAtWar)
        {
            sb.AppendLine("D?KKAT: Krall?klar?n?z sava?ta! Bu durumu göz önünde bulundur.");
        }
        
        // Mood
        sb.AppendLine($"?u anki ruh halin: {context.NpcMood}");
        
        // Instructions
        sb.AppendLine();
        sb.AppendLine("KURALLAR:");
        sb.AppendLine("- K?sa ve öz cevaplar ver (1-3 cümle)");
        sb.AppendLine("- Karakterine uygun konu?");
        sb.AppendLine("- Ortaça? Türkçesi kullan (modern kelimelerden kaç?n)");
        sb.AppendLine("- Duygunu göster ama abartma");
        sb.AppendLine("- Oyun dünyas?na sad?k kal");
        
        return sb.ToString();
    }
    
    private string GetRoleDescription(string role)
    {
        var roleLower = role.ToLowerInvariant();
        
        if (roleLower.Contains("king") || roleLower.Contains("kral"))
            return "güçlü ve onurlu kral";
        if (roleLower.Contains("lord") || roleLower.Contains("bey"))
            return "soylu lord";
        if (roleLower.Contains("merchant") || roleLower.Contains("tüccar"))
            return "kurnaz tüccar";
        if (roleLower.Contains("blacksmith") || roleLower.Contains("demirci"))
            return "usta demirci";
        if (roleLower.Contains("tavern") || roleLower.Contains("hanc?"))
            return "ne?eli hanc?";
        if (roleLower.Contains("villager") || roleLower.Contains("köylü"))
            return "sade köylü";
        if (roleLower.Contains("soldier") || roleLower.Contains("asker"))
            return "deneyimli asker";
        if (roleLower.Contains("commander") || roleLower.Contains("komutan"))
            return "tecrübeli komutan";
        if (roleLower.Contains("lady") || roleLower.Contains("han?m"))
            return "asil han?mefendi";
        if (roleLower.Contains("bandit") || roleLower.Contains("haydut"))
            return "ac?mas?z haydut";
        if (roleLower.Contains("gang") || roleLower.Contains("çete"))
            return "sokak çetesi lideri";
            
        return "Calradia sakini";
    }
    
    private string GetPersonalityTraits(string role)
    {
        var roleLower = role.ToLowerInvariant();
        
        if (roleLower.Contains("king") || roleLower.Contains("kral"))
            return "Otoriter, bilge ve krall???n ç?karlar?n? her ?eyin üstünde tutuyorsun. Kar??ndakine hükümdar gibi hitap ediyorsun.";
            
        if (roleLower.Contains("lord") || roleLower.Contains("bey"))
            return "Onurlu, gururlu ve sava?ç?s?n. ?erefini her ?eyin üstünde tutuyorsun. Soylulu?unu hissettiriyorsun.";
            
        if (roleLower.Contains("merchant") || roleLower.Contains("tüccar"))
            return "Zeki, hesapç? ve f?rsatç?s?n. Her konu?may? ticaret f?rsat?na çevirmeye çal???yorsun. Para her ?eydir.";
            
        if (roleLower.Contains("blacksmith") || roleLower.Contains("demirci"))
            return "Çal??kan, pratik ve az konu?uyorsun. ??ine odakl?s?n. Silahlar ve z?rhlar hakk?nda tutkulusun.";
            
        if (roleLower.Contains("tavern") || roleLower.Contains("hanc?"))
            return "Cana yak?n, dedikodu merakl?s? ve misafirperverin. Herkesi tan?yorsun, her ?eyi duyuyorsun.";
            
        if (roleLower.Contains("villager") || roleLower.Contains("köylü"))
            return "Mütevazi, korkak ve lordlara sayg?l?s?n. Hayat zor, vergiler a??r. Sadece hayatta kalmak istiyorsun.";
            
        if (roleLower.Contains("soldier") || roleLower.Contains("asker"))
            return "Disiplinli, sad?k ve emirlere uyars?n. Sava? hikayeleri anlatmay? seviyorsun.";
            
        if (roleLower.Contains("bandit") || roleLower.Contains("haydut"))
            return "Tehlikeli, sinsi ve ac?mas?zs?n. Zay?flar? sömürüyorsun. Güç her ?eydir.";
            
        return "Calradia'da ya?ayan s?radan bir insans?n. Günlük hayat?n zorluklar?yla mücadele ediyorsun.";
    }
    
    private string BuildUserPrompt(string playerMessage, DialogueContext context, string conversationHistory)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("DURUM:");
        sb.AppendLine($"Konum: {context.Location}");
        sb.AppendLine($"Oyuncunun Fraksiyonu: {context.PlayerFaction}");
        sb.AppendLine($"Senin Fraksiyonun: {context.NpcFaction}");
        sb.AppendLine($"?li?ki Seviyesi: {context.RelationWithPlayer}");
        
        if (!string.IsNullOrEmpty(context.CurrentSituation))
        {
            sb.AppendLine($"Mevcut Durum: {context.CurrentSituation}");
        }
        
        if (context.RecentEvents?.Length > 0)
        {
            sb.AppendLine($"Son Olaylar: {string.Join(", ", context.RecentEvents)}");
        }
        
        sb.AppendLine();
        sb.AppendLine("GEÇM?? KONU?MALAR:");
        sb.AppendLine(conversationHistory);
        sb.AppendLine();
        sb.AppendLine($"OYUNCU ??MD? D?YOR: \"{playerMessage}\"");
        sb.AppendLine();
        sb.AppendLine("Karakterine uygun k?sa bir cevap ver:");
        
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
        if (text.Contains("*öfkeyle*") || text.Contains("*k?zg?n*"))
        {
            emotion = "Angry";
            intent = DialogueIntent.Hostile;
        }
        else if (text.Contains("*gülümseyerek*") || text.Contains("*ne?eyle*"))
        {
            emotion = "Happy";
            intent = DialogueIntent.Friendly;
        }
        else if (text.Contains("*üzgün*") || text.Contains("*hüzünle*"))
        {
            emotion = "Sad";
        }
        else if (text.Contains("*tehditkar*") || text.Contains("*so?uk*"))
        {
            emotion = "Threatening";
            intent = DialogueIntent.Threatening;
        }
        else if (text.Contains("*yalvararak*") || text.Contains("*rica ederek*"))
        {
            emotion = "Pleading";
            intent = DialogueIntent.Pleading;
        }
        
        // Detect end of conversation
        if (text.Contains("ho?ça kal") || text.Contains("git art?k") || 
            text.Contains("konu?mak istemiyorum") || text.Contains("defol"))
        {
            shouldEnd = true;
        }
        
        // Clean up emotion markers for display
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\*[^*]+\*", "").Trim();
        
        return (text, emotion, intent, shouldEnd);
    }
}
