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
                "[Living in Calradia] AI Dialogue system active!", 
                Colors.Green));
        }
        catch (Exception ex)
        {
            InformationManager.DisplayMessage(new InformationMessage(
                $"[Living in Calradia] Dialogue error: {ex.Message}", 
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
            "{=ai_talk}I would like to talk with you more.",
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
            "{=ai_talk_main}[AI] I want to chat with you.",
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
            "{=ai_wait}*thinking*",
            () => _waitingForAI,
            null,
            100,
            null);
        
        // Exit option
        starter.AddPlayerLine(
            "ai_dialogue_exit",
            "ai_dialogue_options",
            "lord_pretalk",
            "{=ai_exit}Let's move on to another topic.",
            () => true,
            null,
            1,
            null,
            null);
    }
    
    private void AddPlayerDialogueOptions(CampaignGameStarter starter)
    {
        // Option 1: General greeting / How are you
        starter.AddPlayerLine(
            "ai_dialogue_opt_1",
            "ai_dialogue_options",
            "ai_dialogue_response_state",
            "{=ai_opt1}How do you fare? What news do you bring?",
            () => _isInitialized,
            () => TriggerAIResponse("How are you? What news can you share with me?"),
            90,
            null,
            null);
        
        // Option 2: Ask about the region/land
        starter.AddPlayerLine(
            "ai_dialogue_opt_2",
            "ai_dialogue_options",
            "ai_dialogue_response_state",
            "{=ai_opt2}Tell me of this land. What troubles these parts?",
            () => _isInitialized,
            () => TriggerAIResponse("What is happening in this region? Are there any troubles or opportunities?"),
            89,
            null,
            null);
        
        // Option 3: Trade and economy
        starter.AddPlayerLine(
            "ai_dialogue_opt_3",
            "ai_dialogue_options",
            "ai_dialogue_response_state",
            "{=ai_opt3}How fares trade in these lands?",
            () => _isInitialized,
            () => TriggerAIResponse("How is trade? Is there profit to be made here?"),
            88,
            null,
            null);
        
        // Option 4: War and conflict
        starter.AddPlayerLine(
            "ai_dialogue_opt_4",
            "ai_dialogue_options",
            "ai_dialogue_response_state",
            "{=ai_opt4}What do you know of the wars being waged?",
            () => _isInitialized,
            () => TriggerAIResponse("What news of war? Who fights whom, and who prevails?"),
            87,
            null,
            null);
        
        // Option 5: Seek counsel/advice
        starter.AddPlayerLine(
            "ai_dialogue_opt_5",
            "ai_dialogue_options",
            "ai_dialogue_response_state",
            "{=ai_opt5}I seek your counsel. What wisdom can you offer?",
            () => _isInitialized,
            () => TriggerAIResponse("I need advice. What would you suggest I do in these uncertain times?"),
            86,
            null,
            null);
        
        // Option 6: Threaten (if relation is negative)
        starter.AddPlayerLine(
            "ai_dialogue_opt_6",
            "ai_dialogue_options",
            "ai_dialogue_response_state",
            "{=ai_opt6}You would be wise to fear me. Remember that.",
            () => _isInitialized && GetCurrentRelation() < 0,
            () => TriggerAIResponse("I am warning you! Cross me and you shall regret it. Do you understand?"),
            85,
            null,
            null);
        
        // Option 7: Compliment/Flatter (if relation is positive)
        starter.AddPlayerLine(
            "ai_dialogue_opt_7",
            "ai_dialogue_options",
            "ai_dialogue_response_state",
            "{=ai_opt7}Your reputation precedes you. It is an honor.",
            () => _isInitialized && GetCurrentRelation() >= 0,
            () => TriggerAIResponse("I have heard great things about you. It is truly an honor to speak with you."),
            84,
            null,
            null);
        
        // Option 8: Personal question
        starter.AddPlayerLine(
            "ai_dialogue_opt_8",
            "ai_dialogue_options",
            "ai_dialogue_response_state",
            "{=ai_opt8}Tell me about yourself. Who are you?",
            () => _isInitialized,
            () => TriggerAIResponse("Tell me about yourself. What is your story? What drives you?"),
            83,
            null,
            null);
        
        // Option 9: Ask about their lord/faction (for non-leaders)
        starter.AddPlayerLine(
            "ai_dialogue_opt_9",
            "ai_dialogue_options",
            "ai_dialogue_response_state",
            "{=ai_opt9}What think you of your liege and kingdom?",
            () => _isInitialized && _currentDialogueHero != null && !_currentDialogueHero.IsKingdomLeader,
            () => TriggerAIResponse("What do you think of your king and your kingdom? Are you content with your lot?"),
            82,
            null,
            null);
        
        // Option 10: Persuasion attempt (for lords only)
        starter.AddPlayerLine(
            "ai_dialogue_opt_10",
            "ai_dialogue_options",
            "ai_dialogue_response_state",
            "{=ai_opt10}I have a proposition for you. Hear me out.",
            () => _isInitialized && _currentDialogueHero != null && _currentDialogueHero.IsLord,
            () => TriggerAIResponse("I have a proposal that may interest you. Would you be willing to work with me on a matter of importance?"),
            81,
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
                $"[AI] Starting AI chat with {_currentDialogueHero.Name}...", 
                Colors.Cyan));
        }
    }
    
    private void SetGreeting()
    {
        if (_currentDialogueHero == null)
        {
            MBTextManager.SetTextVariable("AI_GREETING", "Yes? What is it?");
            return;
        }
        
        var relation = GetCurrentRelation();
        var isKing = _currentDialogueHero.IsKingdomLeader;
        var isLord = _currentDialogueHero.IsLord;
        string greeting;
        
        if (relation >= 50)
        {
            if (isKing)
                greeting = "Ah, my trusted friend! It brings me joy to see you. Come, let us speak.";
            else if (isLord)
                greeting = "Well met, my friend! It is always a pleasure. What would you discuss?";
            else
                greeting = "Oh, hello there! Good to see you again. What can I do for you?";
        }
        else if (relation >= 20)
        {
            if (isKing)
                greeting = "You may approach. I shall hear what you have to say.";
            else if (isLord)
                greeting = "Very well, I shall give you my attention. Speak your mind.";
            else
                greeting = "Aye, what brings you to me? I am listening.";
        }
        else if (relation >= 0)
        {
            if (isKing)
                greeting = "State your business. I have matters of the realm to attend to.";
            else if (isLord)
                greeting = "Yes? What do you want? Be quick about it.";
            else
                greeting = "Hmm? You wished to speak with me?";
        }
        else if (relation >= -30)
        {
            if (isKing)
                greeting = "You dare approach me? Speak quickly, before I lose my patience.";
            else if (isLord)
                greeting = "What do you want? I have little desire to speak with you.";
            else
                greeting = "What is it? I have nothing to say to you.";
        }
        else
        {
            if (isKing)
                greeting = "You have some nerve showing your face before me. This better be important.";
            else if (isLord)
                greeting = "Begone from my sight! I have no words for the likes of you.";
            else
                greeting = "Leave me alone. I want nothing to do with you.";
        }
        
        MBTextManager.SetTextVariable("AI_GREETING", greeting);
    }
    
    private void TriggerAIResponse(string playerMessage)
    {
        if (_dialogueOrchestrator == null || _currentDialogueHero == null)
        {
            _lastAIResponse = "Hmm... *looks thoughtfully*";
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
                _lastAIResponse = response.Text;
            }
            else
            {
                _lastAIResponse = "*after thinking for a moment* Let's talk later.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AI Dialogue] Error: {ex.Message}");
            _lastAIResponse = "*looks at you silently*";
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
        
        var npcFaction = hero.Clan?.Kingdom?.Name?.ToString() ?? hero.Clan?.Name?.ToString() ?? "Independent";
        var playerFaction = playerHero?.Clan?.Kingdom?.Name?.ToString() ?? playerHero?.Clan?.Name?.ToString() ?? "Independent";
        
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
        if (isAtWar) recentEvents.Add("War status");
        if (hero.CurrentSettlement?.IsUnderSiege == true) recentEvents.Add("Under siege");
        
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
