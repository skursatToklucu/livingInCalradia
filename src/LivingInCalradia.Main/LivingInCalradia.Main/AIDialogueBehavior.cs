using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LivingInCalradia.AI.Configuration;
using LivingInCalradia.AI.Orchestration;
using LivingInCalradia.Core.Application.Interfaces;
<<<<<<< HEAD
using LivingInCalradia.Main.Localization;
=======
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace LivingInCalradia.Main;

/// <summary>
/// Integrates AI-powered dialogues into Bannerlord's conversation system.
/// Adds dynamic dialogue options that use LLM for responses.
/// </summary>
public sealed class AIDialogueBehavior : CampaignBehaviorBase
{
    private IDialogueOrchestrator? _dialogueOrchestrator;
    private bool _isInitialized;
    private string _lastAIResponse = "";
    private Hero? _currentDialogueHero;
    private bool _waitingForAI;
<<<<<<< HEAD
    private string _language = "en";
    
    // Localized dialogue strings
    private bool IsTurkish => _language == "tr";
=======
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
    
    public override void RegisterEvents()
    {
        CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
    }
    
    public override void SyncData(IDataStore dataStore)
    {
<<<<<<< HEAD
=======
        // No persistent data needed
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
    }
    
    private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
    {
        try
        {
            InitializeDialogueOrchestrator();
            AddAIDialogues(campaignGameStarter);
            
<<<<<<< HEAD
            var msg = IsTurkish 
                ? "[Living in Calradia] AI Diyalog sistemi aktif!"
                : "[Living in Calradia] AI Dialogue system active!";
            InformationManager.DisplayMessage(new InformationMessage(msg, Colors.Green));
        }
        catch (Exception ex)
        {
            var msg = IsTurkish 
                ? $"[Living in Calradia] Diyalog hatasi: {ex.Message}"
                : $"[Living in Calradia] Dialogue error: {ex.Message}";
            InformationManager.DisplayMessage(new InformationMessage(msg, Colors.Red));
=======
            InformationManager.DisplayMessage(new InformationMessage(
                "[Living in Calradia] AI Diyalog sistemi aktif!", 
                Colors.Green));
        }
        catch (Exception ex)
        {
            InformationManager.DisplayMessage(new InformationMessage(
                $"[Living in Calradia] Diyalog hatasi: {ex.Message}", 
                Colors.Red));
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        }
    }
    
    private void InitializeDialogueOrchestrator()
    {
        try
        {
            var config = AIConfiguration.Load();
            
<<<<<<< HEAD
            // Detect language
            if (config.IsAuto)
            {
                var gameLanguage = DetectGameLanguage();
                config.SetGameLanguage(gameLanguage);
            }
            _language = config.EffectiveLanguage;
            LocalizedStrings.SetLanguage(_language);
            
=======
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
            if (!string.IsNullOrWhiteSpace(config.ApiKey))
            {
                _dialogueOrchestrator = new GroqDialogueOrchestrator(
                    config.ApiKey, 
                    config.ModelId, 
<<<<<<< HEAD
                    0.8,
                    _language);
=======
                    0.8); // Higher temperature for more creative dialogue
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
                    
                _isInitialized = true;
                Console.WriteLine("[Living in Calradia] AI Dialogue system initialized");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Living in Calradia] Failed to init dialogue: {ex.Message}");
        }
    }
    
<<<<<<< HEAD
    private string DetectGameLanguage()
    {
        try
        {
            var activeTextLanguage = MBTextManager.ActiveTextLanguage;
            if (!string.IsNullOrEmpty(activeTextLanguage))
            {
                if (activeTextLanguage.ToLower().Contains("turkish") || activeTextLanguage == "tr")
                    return "tr";
            }
        }
        catch { }
        return "en";
    }
    
    private void AddAIDialogues(CampaignGameStarter starter)
    {
        // Entry point - Add AI conversation option to lord conversations
        var talkMore = IsTurkish ? "Seninle daha fazla konusmak istiyorum." : "I want to talk more with you.";
=======
    private void AddAIDialogues(CampaignGameStarter starter)
    {
        // Entry point - Add AI conversation option to lord conversations
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        starter.AddPlayerLine(
            "ai_dialogue_start",
            "lord_talk_speak_diplomacy_2",
            "ai_dialogue_hub",
<<<<<<< HEAD
            "{=ai_talk}" + talkMore,
=======
            "{=ai_talk}Seninle daha fazla konusmak istiyorum.",
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
            CanStartAIDialogue,
            OnStartAIDialogue,
            100,
            null,
            null);
        
        // Also add to hero_main_options for broader access
<<<<<<< HEAD
        var chatWith = IsTurkish ? "[AI] Seninle sohbet etmek istiyorum." : "[AI] I want to chat with you.";
=======
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        starter.AddPlayerLine(
            "ai_dialogue_start_main",
            "hero_main_options",
            "ai_dialogue_hub",
<<<<<<< HEAD
            "{=ai_talk_main}" + chatWith,
=======
            "{=ai_talk_main}[AI] Seninle sohbet etmek istiyorum.",
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
            CanStartAIDialogue,
            OnStartAIDialogue,
            100,
            null,
            null);
        
        // AI Dialogue Hub - NPC's initial response
        starter.AddDialogLine(
            "ai_dialogue_hub_response",
            "ai_dialogue_hub",
            "ai_dialogue_options",
            "{=ai_hub_resp}{AI_GREETING}",
            () => true,
            SetGreeting,
            100,
            null);
        
        // Player options in AI dialogue
        AddPlayerDialogueOptions(starter);
        
        // AI Response line
        starter.AddDialogLine(
            "ai_dialogue_response",
            "ai_dialogue_response_state",
            "ai_dialogue_options",
            "{=ai_resp}{AI_RESPONSE}",
            () => !string.IsNullOrEmpty(_lastAIResponse),
            null,
            100,
            null);
        
        // Waiting for AI response
<<<<<<< HEAD
        var thinking = IsTurkish ? "*dusunuyor*" : "*thinking*";
=======
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        starter.AddDialogLine(
            "ai_dialogue_waiting",
            "ai_dialogue_waiting_state",
            "ai_dialogue_options",
<<<<<<< HEAD
            "{=ai_wait}" + thinking,
=======
            "{=ai_wait}*dusunuyor*",
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
            () => _waitingForAI,
            null,
            100,
            null);
        
        // Exit option
<<<<<<< HEAD
        var exitText = IsTurkish ? "Baska bir konuya gecelim." : "Let's talk about something else.";
=======
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        starter.AddPlayerLine(
            "ai_dialogue_exit",
            "ai_dialogue_options",
            "lord_pretalk",
<<<<<<< HEAD
            "{=ai_exit}" + exitText,
=======
            "{=ai_exit}Baska bir konuya gecelim.",
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
            () => true,
            null,
            1,
            null,
            null);
    }
    
    private void AddPlayerDialogueOptions(CampaignGameStarter starter)
    {
        // Option 1: General chat
<<<<<<< HEAD
        var opt1 = IsTurkish ? "Nasil gidiyor? Durumun nasil?" : "How are you doing? How's your situation?";
        var opt1Prompt = IsTurkish ? "Nasil gidiyor? Durumun nasil?" : "How are you doing? How's your situation?";
=======
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        starter.AddPlayerLine(
            "ai_dialogue_opt_1",
            "ai_dialogue_options",
            "ai_dialogue_response_state",
<<<<<<< HEAD
            "{=ai_opt1}" + opt1,
            () => _isInitialized,
            () => TriggerAIResponse(opt1Prompt),
=======
            "{=ai_opt1}Nasil gidiyor? Durumun nasil?",
            () => _isInitialized,
            () => TriggerAIResponse("Nasil gidiyor? Durumun nasil?"),
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
            90,
            null,
            null);
        
        // Option 2: Ask about region
<<<<<<< HEAD
        var opt2 = IsTurkish ? "Bu bolgede neler oluyor?" : "What's happening in this region?";
        var opt2Prompt = IsTurkish ? "Bu bolgede neler oluyor? Bana anlat." : "What's happening in this region? Tell me.";
=======
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        starter.AddPlayerLine(
            "ai_dialogue_opt_2",
            "ai_dialogue_options",
            "ai_dialogue_response_state",
<<<<<<< HEAD
            "{=ai_opt2}" + opt2,
            () => _isInitialized,
            () => TriggerAIResponse(opt2Prompt),
=======
            "{=ai_opt2}Bu bolgede neler oluyor?",
            () => _isInitialized,
            () => TriggerAIResponse("Bu bolgede neler oluyor? Bana anlat."),
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
            89,
            null,
            null);
        
        // Option 3: Ask about trade
<<<<<<< HEAD
        var opt3 = IsTurkish ? "Ticaret hakkinda ne dusunuyorsun?" : "What do you think about trade?";
        var opt3Prompt = IsTurkish ? "Ticaret hakkinda ne dusunuyorsun? Iyi para kazaniyor musun?" : "What do you think about trade? Are you making good money?";
=======
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        starter.AddPlayerLine(
            "ai_dialogue_opt_3",
            "ai_dialogue_options",
            "ai_dialogue_response_state",
<<<<<<< HEAD
            "{=ai_opt3}" + opt3,
            () => _isInitialized,
            () => TriggerAIResponse(opt3Prompt),
=======
            "{=ai_opt3}Ticaret hakkinda ne dusunuyorsun?",
            () => _isInitialized,
            () => TriggerAIResponse("Ticaret hakkinda ne dusunuyorsun? Iyi para kazaniyor musun?"),
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
            88,
            null,
            null);
        
        // Option 4: Ask about war
<<<<<<< HEAD
        var opt4 = IsTurkish ? "Savaslar hakkinda ne biliyorsun?" : "What do you know about the wars?";
        var opt4Prompt = IsTurkish ? "Savaslar hakkinda ne biliyorsun? Tehlike var mi?" : "What do you know about the wars? Is there danger?";
=======
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        starter.AddPlayerLine(
            "ai_dialogue_opt_4",
            "ai_dialogue_options",
            "ai_dialogue_response_state",
<<<<<<< HEAD
            "{=ai_opt4}" + opt4,
            () => _isInitialized,
            () => TriggerAIResponse(opt4Prompt),
=======
            "{=ai_opt4}Savaslar hakkinda ne biliyorsun?",
            () => _isInitialized,
            () => TriggerAIResponse("Savaslar hakkinda ne biliyorsun? Tehlike var mi?"),
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
            87,
            null,
            null);
        
        // Option 5: Ask for advice
<<<<<<< HEAD
        var opt5 = IsTurkish ? "Bana bir tavsiye ver." : "Give me some advice.";
        var opt5Prompt = IsTurkish ? "Bana bir tavsiye ver. Ne yapmaliyim?" : "Give me some advice. What should I do?";
=======
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        starter.AddPlayerLine(
            "ai_dialogue_opt_5",
            "ai_dialogue_options",
            "ai_dialogue_response_state",
<<<<<<< HEAD
            "{=ai_opt5}" + opt5,
            () => _isInitialized,
            () => TriggerAIResponse(opt5Prompt),
=======
            "{=ai_opt5}Bana bir tavsiye ver.",
            () => _isInitialized,
            () => TriggerAIResponse("Bana bir tavsiye ver. Ne yapmaliyim?"),
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
            86,
            null,
            null);
        
        // Option 6: Threaten (if relation is low)
<<<<<<< HEAD
        var opt6 = IsTurkish ? "Seni uyariyorum, dikkatli ol!" : "I'm warning you, be careful!";
        var opt6Prompt = IsTurkish ? "Seni uyariyorum! Yolumdan cekil yoksa sonuclarina katlanirsin!" : "I'm warning you! Get out of my way or face the consequences!";
=======
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        starter.AddPlayerLine(
            "ai_dialogue_opt_6",
            "ai_dialogue_options",
            "ai_dialogue_response_state",
<<<<<<< HEAD
            "{=ai_opt6}" + opt6,
            () => _isInitialized && GetCurrentRelation() < 0,
            () => TriggerAIResponse(opt6Prompt),
=======
            "{=ai_opt6}Seni uyariyorum, dikkatli ol!",
            () => _isInitialized && GetCurrentRelation() < 0,
            () => TriggerAIResponse("Seni uyariyorum! Yolumdan cekil yoksa sonuclarina katlanirsin!"),
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
            85,
            null,
            null);
        
        // Option 7: Flatter (if relation is positive)
<<<<<<< HEAD
        var opt7 = IsTurkish ? "Senin gibi biriyle tanismak onur." : "It's an honor to meet someone like you.";
        var opt7Prompt = IsTurkish ? "Senin gibi biriyle tanismak benim icin buyuk bir onur." : "It's a great honor for me to meet someone like you.";
=======
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        starter.AddPlayerLine(
            "ai_dialogue_opt_7",
            "ai_dialogue_options",
            "ai_dialogue_response_state",
<<<<<<< HEAD
            "{=ai_opt7}" + opt7,
            () => _isInitialized && GetCurrentRelation() >= 0,
            () => TriggerAIResponse(opt7Prompt),
=======
            "{=ai_opt7}Senin gibi biriyle tanismak onur.",
            () => _isInitialized && GetCurrentRelation() >= 0,
            () => TriggerAIResponse("Senin gibi biriyle tanismak benim icin buyuk bir onur."),
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
            84,
            null,
            null);
        
<<<<<<< HEAD
        // Option 8: Tell me about yourself
        var opt8 = IsTurkish ? "Bana kendinden bahset." : "Tell me about yourself.";
        var opt8Prompt = IsTurkish ? "Bana kendinden bahset. Kim oldugun ve ne yaptigin hakkinda bilgi ver." : "Tell me about yourself. Give me information about who you are and what you do.";
=======
        // Option 8: Custom question (placeholder - would need text input UI)
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        starter.AddPlayerLine(
            "ai_dialogue_opt_8",
            "ai_dialogue_options",
            "ai_dialogue_response_state",
<<<<<<< HEAD
            "{=ai_opt8}" + opt8,
            () => _isInitialized,
            () => TriggerAIResponse(opt8Prompt),
=======
            "{=ai_opt8}Bana kendinden bahset.",
            () => _isInitialized,
            () => TriggerAIResponse("Bana kendinden bahset. Kim oldugun ve ne yaptigin hakkinda bilgi ver."),
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
            83,
            null,
            null);
    }
    
    private bool CanStartAIDialogue()
    {
        return _isInitialized && 
               Hero.OneToOneConversationHero != null &&
               Campaign.Current != null;
    }
    
    private void OnStartAIDialogue()
    {
        _currentDialogueHero = Hero.OneToOneConversationHero;
        _lastAIResponse = "";
        
        if (_currentDialogueHero != null)
        {
<<<<<<< HEAD
            var msg = IsTurkish 
                ? $"[AI] {_currentDialogueHero.Name} ile AI sohbeti basliyor..."
                : $"[AI] Starting AI conversation with {_currentDialogueHero.Name}...";
            InformationManager.DisplayMessage(new InformationMessage(msg, Colors.Cyan));
=======
            InformationManager.DisplayMessage(new InformationMessage(
                $"[AI] {_currentDialogueHero.Name} ile AI sohbeti basliyor...", 
                Colors.Cyan));
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        }
    }
    
    private void SetGreeting()
    {
        if (_currentDialogueHero == null)
        {
<<<<<<< HEAD
            var defaultGreeting = IsTurkish ? "Evet, buyur?" : "Yes, what is it?";
            MBTextManager.SetTextVariable("AI_GREETING", defaultGreeting);
=======
            MBTextManager.SetTextVariable("AI_GREETING", "Evet, buyur?");
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
            return;
        }
        
        var relation = GetCurrentRelation();
        string greeting;
        
<<<<<<< HEAD
        if (IsTurkish)
        {
            if (relation >= 50)
                greeting = "Ah, dostum! Seni gormek ne guzel. Buyur, konusalim.";
            else if (relation >= 0)
                greeting = "Evet? Benimle konusmak istiyormuzsun. Dinliyorum.";
            else if (relation >= -50)
                greeting = "Ne istiyorsun? Fazla zamanim yok.";
            else
                greeting = "Sen ha? Ne cesaret! Neyse, soyle bakalim ne istiyorsun?";
        }
        else
        {
            if (relation >= 50)
                greeting = "Ah, my friend! It's good to see you. Come, let's talk.";
            else if (relation >= 0)
                greeting = "Yes? You want to speak with me. I'm listening.";
            else if (relation >= -50)
                greeting = "What do you want? I don't have much time.";
            else
                greeting = "You? What nerve! Fine, tell me what you want.";
=======
        if (relation >= 50)
        {
            greeting = "Ah, dostum! Seni gormek ne guzel. Buyur, konusalim.";
        }
        else if (relation >= 0)
        {
            greeting = "Evet? Benimle konusmak istiyormuzsun. Dinliyorum.";
        }
        else if (relation >= -50)
        {
            greeting = "Ne istiyorsun? Fazla zamanim yok.";
        }
        else
        {
            greeting = "Sen ha? Ne cesaret! Neyse, soyle bakalim ne istiyorsun?";
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        }
        
        MBTextManager.SetTextVariable("AI_GREETING", greeting);
    }
    
    private void TriggerAIResponse(string playerMessage)
    {
        if (_dialogueOrchestrator == null || _currentDialogueHero == null)
        {
<<<<<<< HEAD
            _lastAIResponse = IsTurkish ? "Hmm... *dusunceli bakar*" : "Hmm... *looks thoughtful*";
=======
            _lastAIResponse = "Hmm... *dusunceli bakar*";
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
            MBTextManager.SetTextVariable("AI_RESPONSE", _lastAIResponse);
            return;
        }
        
        _waitingForAI = true;
        
<<<<<<< HEAD
=======
        // Run AI synchronously for dialogue (async would need more complex handling)
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        try
        {
            var context = BuildDialogueContext();
            var npcRole = GetNpcRole(_currentDialogueHero);
            
<<<<<<< HEAD
=======
            // Use Task.Run and Wait for synchronous execution in dialogue
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
            var responseTask = Task.Run(async () =>
            {
                return await _dialogueOrchestrator.GenerateResponseAsync(
                    _currentDialogueHero.StringId,
                    _currentDialogueHero.Name.ToString(),
                    npcRole,
                    playerMessage,
                    context,
                    CancellationToken.None);
            });
            
<<<<<<< HEAD
            if (responseTask.Wait(TimeSpan.FromSeconds(10)))
            {
                var response = responseTask.Result;
                _lastAIResponse = IsTurkish ? SanitizeTurkishText(response.Text) : response.Text;
            }
            else
            {
                _lastAIResponse = IsTurkish ? "*biraz dusundukten sonra* Sonra konusalim." : "*after thinking* Let's talk later.";
=======
            // Wait with timeout
            if (responseTask.Wait(TimeSpan.FromSeconds(10)))
            {
                var response = responseTask.Result;
                _lastAIResponse = SanitizeTurkishText(response.Text);
            }
            else
            {
                _lastAIResponse = "*biraz dusundukten sonra* Sonra konusalim.";
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AI Dialogue] Error: {ex.Message}");
<<<<<<< HEAD
            _lastAIResponse = IsTurkish ? "*sessizce bakar*" : "*looks at you silently*";
=======
            _lastAIResponse = "*sessizce bakar*";
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        }
        finally
        {
            _waitingForAI = false;
        }
        
        MBTextManager.SetTextVariable("AI_RESPONSE", _lastAIResponse);
    }
    
<<<<<<< HEAD
=======
    /// <summary>
    /// Converts Turkish special characters to ASCII equivalents for Bannerlord font compatibility.
    /// </summary>
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
    private string SanitizeTurkishText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        
<<<<<<< HEAD
=======
        // Turkish character replacements
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
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
    
    private DialogueContext BuildDialogueContext()
    {
        var hero = _currentDialogueHero;
        if (hero == null)
<<<<<<< HEAD
            return new DialogueContext();
=======
        {
            return new DialogueContext();
        }
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        
        var playerHero = Hero.MainHero;
        var location = hero.CurrentSettlement?.Name?.ToString() ?? "Calradia";
        var relation = playerHero != null ? CharacterRelationManager.GetHeroRelation(playerHero, hero) : 0;
        
<<<<<<< HEAD
        var npcFaction = hero.Clan?.Kingdom?.Name?.ToString() ?? hero.Clan?.Name?.ToString() ?? (IsTurkish ? "Bagimsiz" : "Independent");
        var playerFaction = playerHero?.Clan?.Kingdom?.Name?.ToString() ?? playerHero?.Clan?.Name?.ToString() ?? (IsTurkish ? "Bagimsiz" : "Independent");
=======
        var npcFaction = hero.Clan?.Kingdom?.Name?.ToString() ?? hero.Clan?.Name?.ToString() ?? "Bagimsiz";
        var playerFaction = playerHero?.Clan?.Kingdom?.Name?.ToString() ?? playerHero?.Clan?.Name?.ToString() ?? "Bagimsiz";
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        
        var isAtWar = false;
        if (hero.Clan?.Kingdom != null && playerHero?.Clan?.Kingdom != null)
        {
            isAtWar = hero.Clan.Kingdom.IsAtWarWith(playerHero.Clan.Kingdom);
        }
        
<<<<<<< HEAD
=======
        // Determine mood based on various factors
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        var mood = "Neutral";
        if (relation >= 50) mood = "Friendly";
        else if (relation >= 0) mood = "Neutral";
        else if (relation >= -50) mood = "Suspicious";
        else mood = "Hostile";
        
<<<<<<< HEAD
        var recentEvents = new List<string>();
        if (isAtWar) recentEvents.Add(IsTurkish ? "Savas durumu" : "War situation");
        if (hero.CurrentSettlement?.IsUnderSiege == true) recentEvents.Add(IsTurkish ? "Kusatma altinda" : "Under siege");
=======
        // Gather recent events
        var recentEvents = new List<string>();
        if (isAtWar) recentEvents.Add("Savas durumu");
        if (hero.CurrentSettlement?.IsUnderSiege == true) recentEvents.Add("Kusatma altinda");
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        
        return new DialogueContext
        {
            Location = location,
            RelationWithPlayer = relation,
            NpcFaction = npcFaction,
            PlayerFaction = playerFaction,
            IsAtWar = isAtWar,
            NpcMood = mood,
            RecentEvents = recentEvents.ToArray()
        };
    }
    
    private string GetNpcRole(Hero hero)
    {
        if (hero.IsKingdomLeader)
            return "King";
        if (hero.IsLord)
            return "Lord";
        if (hero.IsNotable)
        {
            if (hero.CurrentSettlement?.IsTown == true)
                return "Merchant";
            if (hero.CurrentSettlement?.IsVillage == true)
                return "Villager";
        }
        if (hero.IsWanderer)
            return "Wanderer";
        if (hero.IsMinorFactionHero)
            return "MinorFactionLeader";
            
        return "Commoner";
    }
    
    private int GetCurrentRelation()
    {
        if (_currentDialogueHero == null || Hero.MainHero == null)
            return 0;
            
        return CharacterRelationManager.GetHeroRelation(Hero.MainHero, _currentDialogueHero);
    }
}
