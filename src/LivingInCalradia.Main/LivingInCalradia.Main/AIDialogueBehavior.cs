using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LivingInCalradia.AI.Configuration;
using LivingInCalradia.AI.Orchestration;
using LivingInCalradia.Core.Application.Interfaces;
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
    
    public override void RegisterEvents()
    {
        CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
    }
    
    public override void SyncData(IDataStore dataStore)
    {
        // No persistent data needed
    }
    
    private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
    {
        try
        {
            InitializeDialogueOrchestrator();
            AddAIDialogues(campaignGameStarter);
            
            InformationManager.DisplayMessage(new InformationMessage(
                "[Living in Calradia] AI Diyalog sistemi aktif!", 
                Colors.Green));
        }
        catch (Exception ex)
        {
            InformationManager.DisplayMessage(new InformationMessage(
                $"[Living in Calradia] Diyalog hatasi: {ex.Message}", 
                Colors.Red));
        }
    }
    
    private void InitializeDialogueOrchestrator()
    {
        try
        {
            var config = AIConfiguration.Load();
            
            if (!string.IsNullOrWhiteSpace(config.ApiKey))
            {
                _dialogueOrchestrator = new GroqDialogueOrchestrator(
                    config.ApiKey, 
                    config.ModelId, 
                    0.8); // Higher temperature for more creative dialogue
                    
                _isInitialized = true;
                Console.WriteLine("[Living in Calradia] AI Dialogue system initialized");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Living in Calradia] Failed to init dialogue: {ex.Message}");
        }
    }
    
    private void AddAIDialogues(CampaignGameStarter starter)
    {
        // Entry point - Add AI conversation option to lord conversations
        starter.AddPlayerLine(
            "ai_dialogue_start",
            "lord_talk_speak_diplomacy_2",
            "ai_dialogue_hub",
            "{=ai_talk}Seninle daha fazla konusmak istiyorum.",
            CanStartAIDialogue,
            OnStartAIDialogue,
            100,
            null,
            null);
        
        // Also add to hero_main_options for broader access
        starter.AddPlayerLine(
            "ai_dialogue_start_main",
            "hero_main_options",
            "ai_dialogue_hub",
            "{=ai_talk_main}[AI] Seninle sohbet etmek istiyorum.",
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
        starter.AddDialogLine(
            "ai_dialogue_waiting",
            "ai_dialogue_waiting_state",
            "ai_dialogue_options",
            "{=ai_wait}*dusunuyor*",
            () => _waitingForAI,
            null,
            100,
            null);
        
        // Exit option
        starter.AddPlayerLine(
            "ai_dialogue_exit",
            "ai_dialogue_options",
            "lord_pretalk",
            "{=ai_exit}Baska bir konuya gecelim.",
            () => true,
            null,
            1,
            null,
            null);
    }
    
    private void AddPlayerDialogueOptions(CampaignGameStarter starter)
    {
        // Option 1: General chat
        starter.AddPlayerLine(
            "ai_dialogue_opt_1",
            "ai_dialogue_options",
            "ai_dialogue_response_state",
            "{=ai_opt1}Nasil gidiyor? Durumun nasil?",
            () => _isInitialized,
            () => TriggerAIResponse("Nasil gidiyor? Durumun nasil?"),
            90,
            null,
            null);
        
        // Option 2: Ask about region
        starter.AddPlayerLine(
            "ai_dialogue_opt_2",
            "ai_dialogue_options",
            "ai_dialogue_response_state",
            "{=ai_opt2}Bu bolgede neler oluyor?",
            () => _isInitialized,
            () => TriggerAIResponse("Bu bolgede neler oluyor? Bana anlat."),
            89,
            null,
            null);
        
        // Option 3: Ask about trade
        starter.AddPlayerLine(
            "ai_dialogue_opt_3",
            "ai_dialogue_options",
            "ai_dialogue_response_state",
            "{=ai_opt3}Ticaret hakkinda ne dusunuyorsun?",
            () => _isInitialized,
            () => TriggerAIResponse("Ticaret hakkinda ne dusunuyorsun? Iyi para kazaniyor musun?"),
            88,
            null,
            null);
        
        // Option 4: Ask about war
        starter.AddPlayerLine(
            "ai_dialogue_opt_4",
            "ai_dialogue_options",
            "ai_dialogue_response_state",
            "{=ai_opt4}Savaslar hakkinda ne biliyorsun?",
            () => _isInitialized,
            () => TriggerAIResponse("Savaslar hakkinda ne biliyorsun? Tehlike var mi?"),
            87,
            null,
            null);
        
        // Option 5: Ask for advice
        starter.AddPlayerLine(
            "ai_dialogue_opt_5",
            "ai_dialogue_options",
            "ai_dialogue_response_state",
            "{=ai_opt5}Bana bir tavsiye ver.",
            () => _isInitialized,
            () => TriggerAIResponse("Bana bir tavsiye ver. Ne yapmaliyim?"),
            86,
            null,
            null);
        
        // Option 6: Threaten (if relation is low)
        starter.AddPlayerLine(
            "ai_dialogue_opt_6",
            "ai_dialogue_options",
            "ai_dialogue_response_state",
            "{=ai_opt6}Seni uyariyorum, dikkatli ol!",
            () => _isInitialized && GetCurrentRelation() < 0,
            () => TriggerAIResponse("Seni uyariyorum! Yolumdan cekil yoksa sonuclarina katlanirsin!"),
            85,
            null,
            null);
        
        // Option 7: Flatter (if relation is positive)
        starter.AddPlayerLine(
            "ai_dialogue_opt_7",
            "ai_dialogue_options",
            "ai_dialogue_response_state",
            "{=ai_opt7}Senin gibi biriyle tanismak onur.",
            () => _isInitialized && GetCurrentRelation() >= 0,
            () => TriggerAIResponse("Senin gibi biriyle tanismak benim icin buyuk bir onur."),
            84,
            null,
            null);
        
        // Option 8: Custom question (placeholder - would need text input UI)
        starter.AddPlayerLine(
            "ai_dialogue_opt_8",
            "ai_dialogue_options",
            "ai_dialogue_response_state",
            "{=ai_opt8}Bana kendinden bahset.",
            () => _isInitialized,
            () => TriggerAIResponse("Bana kendinden bahset. Kim oldugun ve ne yaptigin hakkinda bilgi ver."),
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
            InformationManager.DisplayMessage(new InformationMessage(
                $"[AI] {_currentDialogueHero.Name} ile AI sohbeti basliyor...", 
                Colors.Cyan));
        }
    }
    
    private void SetGreeting()
    {
        if (_currentDialogueHero == null)
        {
            MBTextManager.SetTextVariable("AI_GREETING", "Evet, buyur?");
            return;
        }
        
        var relation = GetCurrentRelation();
        string greeting;
        
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
        }
        
        MBTextManager.SetTextVariable("AI_GREETING", greeting);
    }
    
    private void TriggerAIResponse(string playerMessage)
    {
        if (_dialogueOrchestrator == null || _currentDialogueHero == null)
        {
            _lastAIResponse = "Hmm... *dusunceli bakar*";
            MBTextManager.SetTextVariable("AI_RESPONSE", _lastAIResponse);
            return;
        }
        
        _waitingForAI = true;
        
        // Run AI synchronously for dialogue (async would need more complex handling)
        try
        {
            var context = BuildDialogueContext();
            var npcRole = GetNpcRole(_currentDialogueHero);
            
            // Use Task.Run and Wait for synchronous execution in dialogue
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
            
            // Wait with timeout
            if (responseTask.Wait(TimeSpan.FromSeconds(10)))
            {
                var response = responseTask.Result;
                _lastAIResponse = SanitizeTurkishText(response.Text);
            }
            else
            {
                _lastAIResponse = "*biraz dusundukten sonra* Sonra konusalim.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AI Dialogue] Error: {ex.Message}");
            _lastAIResponse = "*sessizce bakar*";
        }
        finally
        {
            _waitingForAI = false;
        }
        
        MBTextManager.SetTextVariable("AI_RESPONSE", _lastAIResponse);
    }
    
    /// <summary>
    /// Converts Turkish special characters to ASCII equivalents for Bannerlord font compatibility.
    /// </summary>
    private string SanitizeTurkishText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        
        // Turkish character replacements
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
        {
            return new DialogueContext();
        }
        
        var playerHero = Hero.MainHero;
        var location = hero.CurrentSettlement?.Name?.ToString() ?? "Calradia";
        var relation = playerHero != null ? CharacterRelationManager.GetHeroRelation(playerHero, hero) : 0;
        
        var npcFaction = hero.Clan?.Kingdom?.Name?.ToString() ?? hero.Clan?.Name?.ToString() ?? "Bagimsiz";
        var playerFaction = playerHero?.Clan?.Kingdom?.Name?.ToString() ?? playerHero?.Clan?.Name?.ToString() ?? "Bagimsiz";
        
        var isAtWar = false;
        if (hero.Clan?.Kingdom != null && playerHero?.Clan?.Kingdom != null)
        {
            isAtWar = hero.Clan.Kingdom.IsAtWarWith(playerHero.Clan.Kingdom);
        }
        
        // Determine mood based on various factors
        var mood = "Neutral";
        if (relation >= 50) mood = "Friendly";
        else if (relation >= 0) mood = "Neutral";
        else if (relation >= -50) mood = "Suspicious";
        else mood = "Hostile";
        
        // Gather recent events
        var recentEvents = new List<string>();
        if (isAtWar) recentEvents.Add("Savas durumu");
        if (hero.CurrentSettlement?.IsUnderSiege == true) recentEvents.Add("Kusatma altinda");
        
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
