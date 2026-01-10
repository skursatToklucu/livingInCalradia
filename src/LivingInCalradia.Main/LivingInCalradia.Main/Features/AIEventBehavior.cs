using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using LivingInCalradia.AI.Configuration;
using LivingInCalradia.AI.Orchestration;
using LivingInCalradia.Core.Application.Interfaces;
using LivingInCalradia.Core.Application.Services;
using LivingInCalradia.Infrastructure.Bannerlord;

namespace LivingInCalradia.Main.Features;

/// <summary>
/// Event-Driven AI System: Reacts to important game events instead of constant polling.
/// This dramatically reduces API calls while making AI responses more contextual.
/// </summary>
public sealed class AIEventBehavior : CampaignBehaviorBase
{
    private AgentWorkflowService? _workflowService;
    private AIConfiguration? _config;
    private bool _isInitialized;
    
    // Cooldown to prevent spam - loaded from config
    private readonly Dictionary<string, DateTime> _lordCooldowns = new Dictionary<string, DateTime>();
    private TimeSpan _cooldownPeriod = TimeSpan.FromMinutes(15); // Default, updated from config
    
    // Event queue for processing
    private readonly Queue<GameEventInfo> _eventQueue = new Queue<GameEventInfo>();
    private bool _isProcessing;
    
    public override void RegisterEvents()
    {
        // War & Peace Events
        CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
        CampaignEvents.MakePeace.AddNonSerializedListener(this, OnPeaceMade);
        
        // Battle Events
        CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnBattleEnded);
        
        // Siege Events
        CampaignEvents.OnSiegeEventStartedEvent.AddNonSerializedListener(this, OnSiegeStarted);
        CampaignEvents.OnSiegeEventEndedEvent.AddNonSerializedListener(this, OnSiegeEnded);
        
        // Settlement Events
        CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementCaptured);
        CampaignEvents.VillageBeingRaided.AddNonSerializedListener(this, OnVillageRaided);
        
        // Hero Events
        CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
        CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnHeroCaptured);
        CampaignEvents.HeroPrisonerReleased.AddNonSerializedListener(this, OnHeroReleased);
        
        // Political Events - Use OnClanChangedKingdom instead
        CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanDefected);
        CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnGameCreated);
        
        // Session Event
        CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
    }
    
    public override void SyncData(IDataStore dataStore)
    {
        // No persistent data needed
    }
    
    private void OnSessionLaunched(CampaignGameStarter starter)
    {
        try
        {
            InitializeAISystem();
            
            if (_isInitialized)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "[LivingInCalradia] Event-Driven AI System active!",
                    Colors.Green));
            }
        }
        catch (Exception ex)
        {
            Debug.Print($"[LivingInCalradia] Event system init error: {ex.Message}");
        }
    }
    
    private void OnGameCreated(CampaignGameStarter starter)
    {
        // New game created
    }
    
    private void InitializeAISystem()
    {
        try
        {
            _config = AIConfiguration.Load();
            
            if (string.IsNullOrWhiteSpace(_config.ApiKey))
            {
                Debug.Print("[LivingInCalradia] Event system: No API key");
                return;
            }
            
            // Update cooldown from config
            _cooldownPeriod = TimeSpan.FromMinutes(_config.EventCooldownMinutes);
            Debug.Print($"[LivingInCalradia] Event cooldown: {_config.EventCooldownMinutes} minutes");
            Debug.Print($"[LivingInCalradia] Skip minor battles: {_config.SkipMinorBattleEvents} (min size: {_config.MinimumBattleSize})");
            
            var orchestrator = new GroqOrchestrator(
                _config.ApiKey, 
                _config.ModelId, 
                _config.Temperature, 
                "en");
            
            var worldSensor = new BannerlordWorldSensor();
            var actionExecutor = new BannerlordActionExecutor();
            
            _workflowService = new AgentWorkflowService(
                worldSensor,
                orchestrator,
                actionExecutor);
            
            _isInitialized = true;
            Debug.Print("[LivingInCalradia] Event-Driven AI System initialized");
        }
        catch (Exception ex)
        {
            Debug.Print($"[LivingInCalradia] Event init failed: {ex.Message}");
        }
    }
    
    #region War & Peace Events
    
    private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
    {
        if (!_isInitialized) return;
        
        var kingdom1 = faction1 as Kingdom;
        var kingdom2 = faction2 as Kingdom;
        
        if (kingdom1 == null || kingdom2 == null) return;
        
        LogEvent($"WAR: {kingdom1.Name} declared war on {kingdom2.Name}!", Colors.Red);
        
        // Get lords from both kingdoms to react
        var affectedLords = GetLordsFromKingdoms(kingdom1, kingdom2, maxCount: 3);
        
        foreach (var lord in affectedLords)
        {
            QueueEventReaction(lord, GameEventType.WarDeclared, 
                $"War has been declared! {kingdom1.Name} is now at war with {kingdom2.Name}.");
        }
    }
    
    private void OnPeaceMade(IFaction faction1, IFaction faction2, MakePeaceAction.MakePeaceDetail detail)
    {
        if (!_isInitialized) return;
        
        var kingdom1 = faction1 as Kingdom;
        var kingdom2 = faction2 as Kingdom;
        
        if (kingdom1 == null || kingdom2 == null) return;
        
        LogEvent($"PEACE: {kingdom1.Name} and {kingdom2.Name} made peace!", Colors.Green);
        
        var affectedLords = GetLordsFromKingdoms(kingdom1, kingdom2, maxCount: 2);
        
        foreach (var lord in affectedLords)
        {
            QueueEventReaction(lord, GameEventType.PeaceMade,
                $"Peace has been made between {kingdom1.Name} and {kingdom2.Name}.");
        }
    }
    
    #endregion
    
    #region Battle Events
    
    private void OnBattleEnded(MapEvent mapEvent)
    {
        if (!_isInitialized || _config == null) return;
        if (mapEvent == null || !mapEvent.IsFieldBattle) return;
        
        // FILTER: Skip minor battles (looters, small parties)
        var totalStrength = mapEvent.AttackerSide.TroopCount + mapEvent.DefenderSide.TroopCount;
        if (_config.SkipMinorBattleEvents && totalStrength < _config.MinimumBattleSize)
        {
            Debug.Print($"[AIEvent] Skipping minor battle (size: {totalStrength})");
            return;
        }
        
        var winner = mapEvent.Winner;
        var loser = mapEvent.DefeatedSide == BattleSideEnum.Attacker 
            ? mapEvent.AttackerSide 
            : mapEvent.DefenderSide;
        
        // Get the leader of the winning side
        var winnerLeader = winner?.LeaderParty?.LeaderHero;
        var loserLeader = loser?.LeaderParty?.LeaderHero;
        
        // FILTER: Only react for lords (not minor heroes)
        if (winnerLeader != null && winnerLeader.IsLord && winnerLeader.Clan?.Kingdom != null)
        {
            LogEvent($"VICTORY: {winnerLeader.Name} won a battle!", Colors.Green);
            
            QueueEventReaction(winnerLeader, GameEventType.BattleWon,
                $"You have won a great battle! Your forces are victorious.");
        }
        
        if (loserLeader != null && loserLeader.IsLord && !loserLeader.IsPrisoner && loserLeader.Clan?.Kingdom != null)
        {
            LogEvent($"DEFEAT: {loserLeader.Name} lost a battle!", Colors.Red);
            
            QueueEventReaction(loserLeader, GameEventType.BattleLost,
                $"You have lost a battle. Your forces were defeated. What will you do now?");
        }
    }
    
    #endregion
    
    #region Siege Events
    
    private void OnSiegeStarted(SiegeEvent siegeEvent)
    {
        if (!_isInitialized) return;
        if (siegeEvent?.BesiegedSettlement == null) return;
        
        var settlement = siegeEvent.BesiegedSettlement;
        var besieger = siegeEvent.BesiegerCamp?.LeaderParty?.LeaderHero;
        var defender = settlement.OwnerClan?.Leader;
        
        LogEvent($"SIEGE: {settlement.Name} is under siege!", Colors.Yellow);
        
        if (besieger != null && besieger.IsLord)
        {
            QueueEventReaction(besieger, GameEventType.SiegeStarted,
                $"You have begun the siege of {settlement.Name}. How will you proceed?");
        }
        
        if (defender != null && defender.IsLord)
        {
            QueueEventReaction(defender, GameEventType.SettlementUnderSiege,
                $"Your settlement {settlement.Name} is under siege! The enemy is at the gates!");
        }
    }
    
    private void OnSiegeEnded(SiegeEvent siegeEvent)
    {
        if (!_isInitialized) return;
        // Siege ended - could add reactions here
    }
    
    #endregion
    
    #region Settlement Events
    
    private void OnSettlementCaptured(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
    {
        if (!_isInitialized) return;
        if (settlement == null) return;
        
        LogEvent($"CAPTURED: {settlement.Name} changed hands!", Colors.Magenta);
        
        if (capturerHero != null && capturerHero.IsLord)
        {
            QueueEventReaction(capturerHero, GameEventType.SettlementCaptured,
                $"You have captured {settlement.Name}! A great victory for your clan!");
        }
        
        if (oldOwner != null && oldOwner.IsLord)
        {
            QueueEventReaction(oldOwner, GameEventType.SettlementLost,
                $"You have lost {settlement.Name} to the enemy. This is a dark day.");
        }
    }
    
    private void OnVillageRaided(Village village)
    {
        if (!_isInitialized) return;
        if (village?.Settlement == null) return;
        
        var owner = village.Settlement.OwnerClan?.Leader;
        
        LogEvent($"RAID: {village.Name} is being raided!", Colors.Red);
        
        if (owner != null && owner.IsLord)
        {
            QueueEventReaction(owner, GameEventType.VillageRaided,
                $"Your village {village.Name} is being raided by enemies!");
        }
    }
    
    #endregion
    
    #region Hero Events
    
    private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
    {
        if (!_isInitialized) return;
        if (victim == null || !victim.IsLord) return;
        
        LogEvent($"DEATH: {victim.Name} has died!", Colors.Red);
        
        // React: Family members or close allies
        var spouse = victim.Spouse;
        if (spouse != null && spouse.IsAlive && spouse.IsLord)
        {
            QueueEventReaction(spouse, GameEventType.AllyDied,
                $"Your spouse {victim.Name} has died! This is devastating news.");
        }
        
        // Clan leader reaction
        var clanLeader = victim.Clan?.Leader;
        if (clanLeader != null && clanLeader != victim && clanLeader.IsAlive)
        {
            QueueEventReaction(clanLeader, GameEventType.AllyDied,
                $"Your clan member {victim.Name} has fallen. The clan mourns this loss.");
        }
    }
    
    private void OnHeroCaptured(PartyBase capturerParty, Hero prisoner)
    {
        if (!_isInitialized) return;
        if (prisoner == null || !prisoner.IsLord) return;
        
        var captor = capturerParty?.LeaderHero;
        
        LogEvent($"PRISONER: {prisoner.Name} was taken prisoner!", Colors.Yellow);
        
        if (captor != null && captor.IsLord)
        {
            QueueEventReaction(captor, GameEventType.EnemyCaptured,
                $"You have captured {prisoner.Name}! What will you do with this prisoner?");
        }
        
        // Ally of prisoner might want to pay ransom
        var clanLeader = prisoner.Clan?.Leader;
        if (clanLeader != null && clanLeader != prisoner && clanLeader.IsAlive)
        {
            QueueEventReaction(clanLeader, GameEventType.AllyCaptured,
                $"Your ally {prisoner.Name} has been captured! Will you pay the ransom?");
        }
    }
    
    private void OnHeroReleased(Hero prisoner, PartyBase party, IFaction capturerFaction, EndCaptivityDetail detail, bool isForced)
    {
        if (!_isInitialized) return;
        if (prisoner == null || !prisoner.IsLord) return;
        
        LogEvent($"RELEASED: {prisoner.Name} was released!", Colors.Green);
        
        if (prisoner.IsAlive)
        {
            QueueEventReaction(prisoner, GameEventType.Released,
                "You have been released from captivity. You are free again!");
        }
    }
    
    #endregion
    
    #region Political Events
    
    private void OnClanDefected(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
    {
        if (!_isInitialized) return;
        if (clan == null) return;
        
        var clanLeader = clan.Leader;
        
        if (oldKingdom != null && newKingdom != null)
        {
            LogEvent($"DEFECTION: {clan.Name} left {oldKingdom.Name} for {newKingdom.Name}!", Colors.Magenta);
            
            // Old kingdom's ruler reacts
            var oldRuler = oldKingdom.Leader;
            if (oldRuler != null && oldRuler != clanLeader)
            {
                QueueEventReaction(oldRuler, GameEventType.VassalDefected,
                    $"The {clan.Name} clan has betrayed us and joined {newKingdom.Name}!");
            }
            
            // New kingdom's ruler reacts
            var newRuler = newKingdom.Leader;
            if (newRuler != null && newRuler != clanLeader)
            {
                QueueEventReaction(newRuler, GameEventType.NewVassal,
                    $"The {clan.Name} clan has joined our kingdom! We grow stronger!");
            }
        }
    }
    
    #endregion
    
    #region Event Processing
    
    private void QueueEventReaction(Hero lord, GameEventType eventType, string eventDescription)
    {
        if (lord == null || !lord.IsAlive || lord.IsPrisoner) return;
        if (lord == Hero.MainHero) return; // Don't react for player
        
        var lordId = GetLordId(lord);
        
        // Check cooldown
        if (_lordCooldowns.TryGetValue(lordId, out var lastReaction))
        {
            if (DateTime.Now - lastReaction < _cooldownPeriod)
            {
                Debug.Print($"[AIEvent] {lord.Name} is on cooldown, skipping event");
                return;
            }
        }
        
        var eventInfo = new GameEventInfo
        {
            Lord = lord,
            LordId = lordId,
            EventType = eventType,
            Description = eventDescription,
            Timestamp = DateTime.Now
        };
        
        _eventQueue.Enqueue(eventInfo);
        _lordCooldowns[lordId] = DateTime.Now;
        
        // Process queue
        ProcessEventQueueAsync().ConfigureAwait(false);
    }
    
    private async Task ProcessEventQueueAsync()
    {
        if (_isProcessing || _workflowService == null) return;
        if (_eventQueue.Count == 0) return;
        
        _isProcessing = true;
        
        try
        {
            while (_eventQueue.Count > 0)
            {
                var eventInfo = _eventQueue.Dequeue();
                
                try
                {
                    Debug.Print($"[AIEvent] Processing: {eventInfo.Lord.Name} - {eventInfo.EventType}");
                    
                    // Add event context to the workflow
                    var result = await _workflowService.ExecuteWorkflowAsync(eventInfo.LordId);
                    
                    if (result.IsSuccessful && result.Decision != null)
                    {
                        var thought = GetShortThought(result.Decision.Reasoning);
                        var action = result.Decision.Actions.Count > 0 
                            ? result.Decision.Actions[0].ActionType 
                            : "Wait";
                        
                        // Get kingdom name for display
                        var kingdomName = eventInfo.Lord.Clan?.Kingdom?.Name?.ToString() ?? "Independent";
                        
                        // Log if enabled - use GLOBAL log state
                        if (AIConfiguration.GlobalThoughtLogsEnabled)
                        {
                            LogAI($"{eventInfo.Lord.Name} ({kingdomName}): {thought}");
                        }
                        
                        if (AIConfiguration.GlobalActionLogsEnabled && result.Decision.Actions.Count > 0)
                        {
                            LogAction($"{eventInfo.Lord.Name} ({kingdomName}) -> {action}");
                        }
                        
                        // Record to thoughts panel
                        LordThoughtsPanel.RecordThought(
                            eventInfo.Lord.Name.ToString(), 
                            thought, 
                            action);
                    }
                }
                catch (Exception ex)
                {
                    Debug.Print($"[AIEvent] Error processing {eventInfo.Lord.Name}: {ex.Message}");
                }
                
                // Small delay between processing
                await Task.Delay(1000);
            }
        }
        finally
        {
            _isProcessing = false;
        }
    }
    
    #endregion
    
    #region Helper Methods
    
    private List<Hero> GetLordsFromKingdoms(Kingdom kingdom1, Kingdom kingdom2, int maxCount)
    {
        var lords = new List<Hero>();
        
        // Get leaders first
        if (kingdom1.Leader != null && kingdom1.Leader.IsAlive)
            lords.Add(kingdom1.Leader);
        if (kingdom2.Leader != null && kingdom2.Leader.IsAlive)
            lords.Add(kingdom2.Leader);
        
        // Add some random lords if needed
        var random = new Random();
        
        if (lords.Count < maxCount)
        {
            foreach (var clan in kingdom1.Clans)
            {
                if (lords.Count >= maxCount) break;
                if (clan.Leader != null && clan.Leader.IsAlive && !lords.Contains(clan.Leader))
                {
                    if (random.Next(100) < 30) // 30% chance
                        lords.Add(clan.Leader);
                }
            }
        }
        
        return lords;
    }
    
    private string GetLordId(Hero hero)
    {
        var heroType = hero.IsLord ? "Lord" : hero.IsNotable ? "Notable" : "Hero";
        var faction = hero.Clan?.Name?.ToString() ?? "Unknown";
        return $"{heroType}_{hero.Name}_{faction}";
    }
    
    private string GetShortThought(string reasoning)
    {
        if (string.IsNullOrEmpty(reasoning)) return "";
        
        var lines = reasoning.Split('\n');
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("THOUGHT:", StringComparison.OrdinalIgnoreCase))
            {
                var thought = trimmed.Substring(trimmed.IndexOf(':') + 1).Trim();
                if (thought.Length > 100)
                    return thought.Substring(0, 97) + "...";
                return thought;
            }
        }
        
        var clean = reasoning.Replace("\n", " ").Replace("\r", "").Trim();
        if (clean.Length > 100)
            return clean.Substring(0, 97) + "...";
        return clean;
    }
    
    private void LogEvent(string message, Color color)
    {
        // Use GLOBAL log state instead of local config
        if (!AIConfiguration.GlobalActionLogsEnabled) return;
        
        try
        {
            InformationManager.DisplayMessage(new InformationMessage(
                $"[Event] {message}", color));
            Debug.Print($"[LivingInCalradia] Event: {message}");
        }
        catch { }
    }
    
    private void LogAI(string message)
    {
        // Use GLOBAL log state instead of local config
        if (!AIConfiguration.GlobalThoughtLogsEnabled) return;
        
        try
        {
            InformationManager.DisplayMessage(new InformationMessage(
                $"[AI] {message}", Colors.Yellow));
            Debug.Print($"[LivingInCalradia] AI: {message}");
        }
        catch { }
    }
    
    private void LogAction(string message)
    {
        // Use GLOBAL log state instead of local config
        if (!AIConfiguration.GlobalActionLogsEnabled) return;
        
        try
        {
            InformationManager.DisplayMessage(new InformationMessage(
                $"[AI Action] {message}", Colors.Cyan));
            Debug.Print($"[LivingInCalradia] Action: {message}");
        }
        catch { }
    }
    
    #endregion
}

/// <summary>
/// Types of game events that trigger AI reactions
/// </summary>
public enum GameEventType
{
    // War & Peace
    WarDeclared,
    PeaceMade,
    
    // Battles
    BattleWon,
    BattleLost,
    
    // Sieges
    SiegeStarted,
    SettlementUnderSiege,
    SettlementCaptured,
    SettlementLost,
    VillageRaided,
    
    // Heroes
    AllyDied,
    EnemyCaptured,
    AllyCaptured,
    Released,
    
    // Politics
    VassalDefected,
    NewVassal
}

/// <summary>
/// Information about a game event for AI processing
/// </summary>
public class GameEventInfo
{
    public Hero Lord { get; set; } = null!;
    public string LordId { get; set; } = "";
    public GameEventType EventType { get; set; }
    public string Description { get; set; } = "";
    public DateTime Timestamp { get; set; }
}
