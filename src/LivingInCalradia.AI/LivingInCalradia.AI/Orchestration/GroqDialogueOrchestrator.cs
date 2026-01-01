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
                return DialogueResponse.Error("Hmm... Bir sey soyleyecektim ama unuttum.");
            }
            
            var npcResponse = ExtractContentFromResponse(responseJson);
            
            // Parse emotion and intent from response
            var (cleanText, emotion, intent, shouldEnd) = ParseResponse(npcResponse);
            
            // Sanitize Turkish characters for Bannerlord font compatibility
            cleanText = SanitizeTurkishText(cleanText);
            
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
            return DialogueResponse.Error("*dusunceli bir sekilde bakar*");
        }
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
    
    private string BuildSystemPrompt(string npcName, string npcRole, DialogueContext context)
    {
        var sb = new StringBuilder();
        
        // Base personality based on role
        sb.AppendLine($"Sen {npcName} adinda bir {GetRoleDescription(npcRole)}.");
        sb.AppendLine($"Mount & Blade II: Bannerlord dunyasinda yasiyorsun.");
        sb.AppendLine();
        
        // Role-specific personality
        sb.AppendLine(GetPersonalityTraits(npcRole));
        sb.AppendLine();
        
        // Relationship context
        if (context.RelationWithPlayer >= 50)
        {
            sb.AppendLine("Bu kisiyle aran cok iyi, ona guveniyorsun ve dostca konusuyorsun.");
        }
        else if (context.RelationWithPlayer >= 0)
        {
            sb.AppendLine("Bu kisiyle aran normal, resmi ama nazik konusuyorsun.");
        }
        else if (context.RelationWithPlayer >= -50)
        {
            sb.AppendLine("Bu kisiyle aran gergin, soguk ve mesafeli konusuyorsun.");
        }
        else
        {
            sb.AppendLine("Bu kisiden nefret ediyorsun, dusmanca ve tehditkar konusuyorsun.");
        }
        
        // War context
        if (context.IsAtWar)
        {
            sb.AppendLine("DIKKAT: Kralliklarin savasta! Bu durumu goz onunde bulundur.");
        }
        
        // Mood
        sb.AppendLine($"Su anki ruh halin: {context.NpcMood}");
        
        // Instructions
        sb.AppendLine();
        sb.AppendLine("KURALLAR:");
        sb.AppendLine("- Kisa ve oz cevaplar ver (1-3 cumle)");
        sb.AppendLine("- Karakterine uygun konus");
        sb.AppendLine("- Ortacag Turkcesi kullan (modern kelimelerden kacin)");
        sb.AppendLine("- Duygunu goster ama abartma");
        sb.AppendLine("- Oyun dunyasina sadik kal");
        sb.AppendLine("- ONEMLI: Turkce ozel karakterler kullanma (i, s, g, u, o, c kullan - ?, ?, ?, ü, ö, ç KULLANMA)");
        
        return sb.ToString();
    }
    
    private string GetRoleDescription(string role)
    {
        var roleLower = role.ToLowerInvariant();
        
        if (roleLower.Contains("king") || roleLower.Contains("kral"))
            return "guclu ve onurlu kral";
        if (roleLower.Contains("lord") || roleLower.Contains("bey"))
            return "soylu lord";
        if (roleLower.Contains("merchant") || roleLower.Contains("tuccar"))
            return "kurnaz tuccar";
        if (roleLower.Contains("blacksmith") || roleLower.Contains("demirci"))
            return "usta demirci";
        if (roleLower.Contains("tavern") || roleLower.Contains("hanci"))
            return "neseli hanci";
        if (roleLower.Contains("villager") || roleLower.Contains("koylu"))
            return "sade koylu";
        if (roleLower.Contains("soldier") || roleLower.Contains("asker"))
            return "deneyimli asker";
        if (roleLower.Contains("commander") || roleLower.Contains("komutan"))
            return "tecrubeli komutan";
        if (roleLower.Contains("lady") || roleLower.Contains("hanim"))
            return "asil hanimefendi";
        if (roleLower.Contains("bandit") || roleLower.Contains("haydut"))
            return "acimasiz haydut";
        if (roleLower.Contains("gang") || roleLower.Contains("cete"))
            return "sokak cetesi lideri";
            
        return "Calradia sakini";
    }
    
    private string GetPersonalityTraits(string role)
    {
        var roleLower = role.ToLowerInvariant();
        
        if (roleLower.Contains("king") || roleLower.Contains("kral"))
            return "Otoriter, bilge ve kralligin cikarlarini her seyin ustunde tutuyorsun. Karsindakine hukumdar gibi hitap ediyorsun.";
            
        if (roleLower.Contains("lord") || roleLower.Contains("bey"))
            return "Onurlu, gururlu ve savascisin. Serefini her seyin ustunde tutuyorsun. Soylulugunu hissettiriyorsun.";
            
        if (roleLower.Contains("merchant") || roleLower.Contains("tuccar"))
            return "Zeki, hesapci ve firsatcisin. Her konusmayi ticaret firsatina cevirmeye calisiyorsun. Para her seydir.";
            
        if (roleLower.Contains("blacksmith") || roleLower.Contains("demirci"))
            return "Caliskan, pratik ve az konusuyorsun. Isine odaklisin. Silahlar ve zirhlar hakkinda tutkulusun.";
            
        if (roleLower.Contains("tavern") || roleLower.Contains("hanci"))
            return "Cana yakin, dedikodu meraklisi ve misafirperversin. Herkesi taniyorsun, her seyi duyuyorsun.";
            
        if (roleLower.Contains("villager") || roleLower.Contains("koylu"))
            return "Mutevazi, korkak ve lordlara saygilisin. Hayat zor, vergiler agir. Sadece hayatta kalmak istiyorsun.";
            
        if (roleLower.Contains("soldier") || roleLower.Contains("asker"))
            return "Disiplinli, sadik ve emirlere uyarsin. Savas hikayeleri anlatmayi seviyorsun.";
            
        if (roleLower.Contains("bandit") || roleLower.Contains("haydut"))
            return "Tehlikeli, sinsi ve acimasizsin. Zayiflari somuruyorsun. Guc her seydir.";
            
        return "Calradia'da yasayan siradan bir insansin. Gunluk hayatin zorluklariyla mucadele ediyorsun.";
    }
    
    private string BuildUserPrompt(string playerMessage, DialogueContext context, string conversationHistory)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("DURUM:");
        sb.AppendLine($"Konum: {context.Location}");
        sb.AppendLine($"Oyuncunun Fraksiyonu: {context.PlayerFaction}");
        sb.AppendLine($"Senin Fraksiyonun: {context.NpcFaction}");
        sb.AppendLine($"Iliski Seviyesi: {context.RelationWithPlayer}");
        
        if (!string.IsNullOrEmpty(context.CurrentSituation))
        {
            sb.AppendLine($"Mevcut Durum: {context.CurrentSituation}");
        }
        
        if (context.RecentEvents?.Length > 0)
        {
            sb.AppendLine($"Son Olaylar: {string.Join(", ", context.RecentEvents)}");
        }
        
        sb.AppendLine();
        sb.AppendLine("GECMIS KONUSMALAR:");
        sb.AppendLine(conversationHistory);
        sb.AppendLine();
        sb.AppendLine($"OYUNCU SIMDI DIYOR: \"{playerMessage}\"");
        sb.AppendLine();
        sb.AppendLine("Karakterine uygun kisa bir cevap ver (Turkce ozel karakter KULLANMA):");
        
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
        if (text.Contains("*ofkeyle*") || text.Contains("*kizgin*"))
        {
            emotion = "Angry";
            intent = DialogueIntent.Hostile;
        }
        else if (text.Contains("*gulumseyerek*") || text.Contains("*neseyle*"))
        {
            emotion = "Happy";
            intent = DialogueIntent.Friendly;
        }
        else if (text.Contains("*uzgun*") || text.Contains("*huzunle*"))
        {
            emotion = "Sad";
        }
        else if (text.Contains("*tehditkar*") || text.Contains("*soguk*"))
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
        if (text.Contains("hosca kal") || text.Contains("git artik") || 
            text.Contains("konusmak istemiyorum") || text.Contains("defol"))
        {
            shouldEnd = true;
        }
        
        // Clean up emotion markers for display
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\*[^*]+\*", "").Trim();
        
        return (text, emotion, intent, shouldEnd);
    }
}
