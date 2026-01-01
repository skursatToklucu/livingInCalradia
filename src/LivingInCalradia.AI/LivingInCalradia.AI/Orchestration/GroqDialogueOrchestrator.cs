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
    private readonly string _language;
    
    private bool IsTurkish => _language == "tr";
    
    public GroqDialogueOrchestrator(
        string apiKey, 
        string model = "llama-3.1-8b-instant", 
        double temperature = 0.8,
        string language = "en")
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentNullException(nameof(apiKey));
        
        _model = model;
        _temperature = temperature;
        _conversationMemory = new ConversationMemory(maxEntriesPerNpc: 10);
        _language = language?.ToLowerInvariant() == "tr" ? "tr" : "en";
        
        // Set language for memory
        ConversationMemory.SetLanguage(_language);
        
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
                var errorMsg = IsTurkish ? "Hmm... Bir sey soyleyecektim ama unuttum." : "Hmm... I was going to say something but I forgot.";
                return DialogueResponse.Error(errorMsg);
            }
            
            var npcResponse = ExtractContentFromResponse(responseJson);
            
            var (cleanText, emotion, intent, shouldEnd) = ParseResponse(npcResponse);
            
            if (IsTurkish)
            {
                cleanText = SanitizeTurkishText(cleanText);
            }
            
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
            var errorMsg = IsTurkish ? "*dusunceli bir sekilde bakar*" : "*looks at you thoughtfully*";
            return DialogueResponse.Error(errorMsg);
        }
    }
    
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
        
        if (IsTurkish)
        {
            sb.AppendLine($"Sen {npcName} adinda bir {GetRoleDescription(npcRole)}.");
            sb.AppendLine($"Mount & Blade II: Bannerlord dunyasinda yasiyorsun.");
        }
        else
        {
            sb.AppendLine($"You are {npcName}, a {GetRoleDescriptionEn(npcRole)}.");
            sb.AppendLine($"You live in the world of Mount & Blade II: Bannerlord.");
        }
        sb.AppendLine();
        
        sb.AppendLine(GetPersonalityTraits(npcRole));
        sb.AppendLine();
        
        if (IsTurkish)
        {
            if (context.RelationWithPlayer >= 50)
                sb.AppendLine("Bu kisiyle aran cok iyi, ona guveniyorsun ve dostca konusuyorsun.");
            else if (context.RelationWithPlayer >= 0)
                sb.AppendLine("Bu kisiyle aran normal, resmi ama nazik konusuyorsun.");
            else if (context.RelationWithPlayer >= -50)
                sb.AppendLine("Bu kisiyle aran gergin, soguk ve mesafeli konusuyorsun.");
            else
                sb.AppendLine("Bu kisiden nefret ediyorsun, dusmanca ve tehditkar konusuyorsun.");
            
            if (context.IsAtWar)
                sb.AppendLine("DIKKAT: Kralliklarin savasta! Bu durumu goz onunde bulundur.");
            
            sb.AppendLine($"Su anki ruh halin: {context.NpcMood}");
            sb.AppendLine();
            sb.AppendLine("KURALLAR:");
            sb.AppendLine("- Kisa ve oz cevaplar ver (1-3 cumle)");
            sb.AppendLine("- Karakterine uygun konus");
            sb.AppendLine("- Ortacag Turkcesi kullan");
            sb.AppendLine("- ONEMLI: Turkce ozel karakterler kullanma (i, s, g, u, o, c kullan)");
        }
        else
        {
            if (context.RelationWithPlayer >= 50)
                sb.AppendLine("You have an excellent relationship with this person. You trust them and speak in a friendly manner.");
            else if (context.RelationWithPlayer >= 0)
                sb.AppendLine("You have a normal relationship with this person. You speak formally but politely.");
            else if (context.RelationWithPlayer >= -50)
                sb.AppendLine("Your relationship with this person is tense. You speak coldly and distantly.");
            else
                sb.AppendLine("You hate this person. You speak in a hostile and threatening manner.");
            
            if (context.IsAtWar)
                sb.AppendLine("WARNING: Your kingdoms are at war! Consider this situation.");
            
            sb.AppendLine($"Current mood: {context.NpcMood}");
            sb.AppendLine();
            sb.AppendLine("RULES:");
            sb.AppendLine("- Give short and concise answers (1-3 sentences)");
            sb.AppendLine("- Speak according to your character");
            sb.AppendLine("- Use medieval-style English");
            sb.AppendLine("- Stay true to the game world");
        }
        
        return sb.ToString();
    }
    
    private string GetRoleDescription(string role)
    {
        var roleLower = role.ToLowerInvariant();
        
        if (roleLower.Contains("king")) return "guclu ve onurlu kral";
        if (roleLower.Contains("lord")) return "soylu lord";
        if (roleLower.Contains("merchant")) return "kurnaz tuccar";
        if (roleLower.Contains("blacksmith")) return "usta demirci";
        if (roleLower.Contains("tavern")) return "neseli hanci";
        if (roleLower.Contains("villager")) return "sade koylu";
        if (roleLower.Contains("soldier")) return "deneyimli asker";
        if (roleLower.Contains("commander")) return "tecrubeli komutan";
        if (roleLower.Contains("bandit")) return "acimasiz haydut";
            
        return "Calradia sakini";
    }
    
    private string GetRoleDescriptionEn(string role)
    {
        var roleLower = role.ToLowerInvariant();
        
        if (roleLower.Contains("king")) return "powerful and honorable king";
        if (roleLower.Contains("lord")) return "noble lord";
        if (roleLower.Contains("merchant")) return "cunning merchant";
        if (roleLower.Contains("blacksmith")) return "master blacksmith";
        if (roleLower.Contains("tavern")) return "cheerful tavern keeper";
        if (roleLower.Contains("villager")) return "simple villager";
        if (roleLower.Contains("soldier")) return "experienced soldier";
        if (roleLower.Contains("commander")) return "veteran commander";
        if (roleLower.Contains("bandit")) return "ruthless bandit";
            
        return "resident of Calradia";
    }
    
    private string GetPersonalityTraits(string role)
    {
        var roleLower = role.ToLowerInvariant();
        
        if (IsTurkish)
        {
            if (roleLower.Contains("king"))
                return "Otoriter, bilge ve kralligin cikarlarini her seyin ustunde tutuyorsun.";
            if (roleLower.Contains("lord"))
                return "Onurlu, gururlu ve savascisin. Serefini her seyin ustunde tutuyorsun.";
            if (roleLower.Contains("merchant"))
                return "Zeki, hesapci ve firsatcisin. Para her seydir.";
            if (roleLower.Contains("villager"))
                return "Mutevazi, korkak ve lordlara saygilisin. Hayat zor.";
            if (roleLower.Contains("bandit"))
                return "Tehlikeli, sinsi ve acimasizsin. Guc her seydir.";
            return "Calradia'da yasayan siradan bir insansin.";
        }
        else
        {
            if (roleLower.Contains("king"))
                return "You are authoritative and wise. The kingdom's interests come above all else.";
            if (roleLower.Contains("lord"))
                return "You are honorable, proud, and a warrior. Your honor comes above all.";
            if (roleLower.Contains("merchant"))
                return "You are clever, calculating, and opportunistic. Money is everything.";
            if (roleLower.Contains("villager"))
                return "You are humble, timid, and respectful to lords. Life is hard.";
            if (roleLower.Contains("bandit"))
                return "You are dangerous, cunning, and ruthless. Power is everything.";
            return "You are an ordinary person living in Calradia.";
        }
    }
    
    private string BuildUserPrompt(string playerMessage, DialogueContext context, string conversationHistory)
    {
        var sb = new StringBuilder();
        
        if (IsTurkish)
        {
            sb.AppendLine("DURUM:");
            sb.AppendLine($"Konum: {context.Location}");
            sb.AppendLine($"Oyuncunun Fraksiyonu: {context.PlayerFaction}");
            sb.AppendLine($"Senin Fraksiyonun: {context.NpcFaction}");
            sb.AppendLine($"Iliski Seviyesi: {context.RelationWithPlayer}");
            sb.AppendLine();
            sb.AppendLine("GECMIS KONUSMALAR:");
            sb.AppendLine(conversationHistory);
            sb.AppendLine();
            sb.AppendLine($"OYUNCU SIMDI DIYOR: \"{playerMessage}\"");
            sb.AppendLine();
            sb.AppendLine("Karakterine uygun kisa bir cevap ver:");
        }
        else
        {
            sb.AppendLine("SITUATION:");
            sb.AppendLine($"Location: {context.Location}");
            sb.AppendLine($"Player's Faction: {context.PlayerFaction}");
            sb.AppendLine($"Your Faction: {context.NpcFaction}");
            sb.AppendLine($"Relation Level: {context.RelationWithPlayer}");
            sb.AppendLine();
            sb.AppendLine("PAST CONVERSATIONS:");
            sb.AppendLine(conversationHistory);
            sb.AppendLine();
            sb.AppendLine($"PLAYER NOW SAYS: \"{playerMessage}\"");
            sb.AppendLine();
            sb.AppendLine("Give a short response fitting your character:");
        }
        
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
        
        // Detect emotion from text markers (both languages)
        if (text.Contains("*angry*") || text.Contains("*ofkeyle*") || text.Contains("*kizgin*"))
        {
            emotion = "Angry";
            intent = DialogueIntent.Hostile;
        }
        else if (text.Contains("*smiling*") || text.Contains("*gulumseyerek*") || text.Contains("*neseyle*"))
        {
            emotion = "Happy";
            intent = DialogueIntent.Friendly;
        }
        else if (text.Contains("*sad*") || text.Contains("*uzgun*") || text.Contains("*huzunle*"))
        {
            emotion = "Sad";
        }
        else if (text.Contains("*threatening*") || text.Contains("*tehditkar*") || text.Contains("*soguk*"))
        {
            emotion = "Threatening";
            intent = DialogueIntent.Threatening;
        }
        
        // Detect end of conversation (both languages)
        if (text.Contains("farewell") || text.Contains("goodbye") || text.Contains("hosca kal") || 
            text.Contains("git artik") || text.Contains("leave me"))
        {
            shouldEnd = true;
        }
        
        // Clean up emotion markers for display
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\*[^*]+\*", "").Trim();
        
        return (text, emotion, intent, shouldEnd);
    }
}
