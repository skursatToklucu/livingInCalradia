using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using LivingInCalradia.AI.Configuration;
using LivingInCalradia.AI.Orchestration;
using LivingInCalradia.Core.Application.Services;
using LivingInCalradia.Infrastructure.Bannerlord;

namespace LivingInCalradia.Main.Features;

/// <summary>
/// World AI System: All lords in the world think independently.
/// This creates a living, breathing world where NPCs make decisions
/// regardless of player proximity.
/// </summary>
public sealed class WorldAIBehavior : CampaignBehaviorBase
{
    private AgentWorkflowService? _workflowService;
    private AIConfiguration? _config;
    private bool _isInitialized;
    
    // Tick timing
    private float _lastWorldTickTime;
    
    // Track which lords have thought recently to spread out AI calls
    private readonly Dictionary<string, DateTime> _lordLastThoughtTime = new Dictionary<string, DateTime>();
    private TimeSpan _lordCooldown = TimeSpan.FromMinutes(10); // Each lord thinks every 10 min max
    
    // Processing state
    private bool _isProcessing;
    private readonly Random _random = new Random();
    
    public override void RegisterEvents()
    {
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
            
            if (_isInitialized && _config?.EnableWorldAI == true)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "[LivingInCalradia] World AI System active - All lords think!",
                    Colors.Green));
            }
        }
        catch (Exception ex)
        {
            Debug.Print($"[LivingInCalradia] World AI init error: {ex.Message}");
        }
    }
    
    private void InitializeAISystem()
    {
        try
        {
            _config = AIConfiguration.Load();
            
            if (string.IsNullOrWhiteSpace(_config.ApiKey))
            {
                Debug.Print("[LivingInCalradia] World AI: No API key");
                return;
            }
            
            if (!_config.EnableWorldAI)
            {
                Debug.Print("[LivingInCalradia] World AI: Disabled in config");
                return;
            }
            
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
            _lordCooldown = TimeSpan.FromMinutes(Math.Max(5, _config.EventCooldownMinutes * 2));
            
            Debug.Print($"[LivingInCalradia] World AI initialized. Tick: {_config.WorldTickIntervalSeconds}s, Lords/tick: {_config.WorldMaxLordsPerTick}");
        }
        catch (Exception ex)
        {
            Debug.Print($"[LivingInCalradia] World AI init failed: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Called from BannerlordSubModule's OnApplicationTick
    /// </summary>
    public void OnTick(float dt)
    {
        if (!_isInitialized || _config == null || !_config.EnableWorldAI)
            return;
        
        if (Campaign.Current == null)
            return;
        
        _lastWorldTickTime += dt;
        
        if (_lastWorldTickTime >= _config.WorldTickIntervalSeconds)
        {
            _lastWorldTickTime = 0f;
            ProcessWorldAIAsync().ConfigureAwait(false);
        }
    }
    
    private async Task ProcessWorldAIAsync()
    {
        if (_isProcessing || _workflowService == null || _config == null)
            return;
        
        if (Campaign.Current?.AliveHeroes == null)
            return;
        
        // Don't run during battles, menus, etc.
        if (IsGameBusy())
            return;
        
        _isProcessing = true;
        
        try
        {
            // Select lords to process this tick
            var lordsToProcess = SelectLordsForThinking(_config.WorldMaxLordsPerTick);
            
            if (lordsToProcess.Count == 0)
            {
                Debug.Print("[WorldAI] No eligible lords to process");
                _isProcessing = false;
                return;
            }
            
            Debug.Print($"[WorldAI] Processing {lordsToProcess.Count} lords...");
            
            foreach (var lord in lordsToProcess)
            {
                if (IsGameBusy())
                    break;
                
                try
                {
                    var lordId = GetLordId(lord);
                    
                    // Mark as recently thought
                    _lordLastThoughtTime[lordId] = DateTime.Now;
                    
                    // Get kingdom name for display
                    var kingdomName = lord.Clan?.Kingdom?.Name?.ToString() ?? "Independent";
                    var lordDisplay = $"{lord.Name} ({kingdomName})";
                    
                    Debug.Print($"[WorldAI] {lordDisplay} is thinking...");
                    
                    var result = await _workflowService.ExecuteWorkflowAsync(lordId);
                    
                    if (result.IsSuccessful && result.Decision != null)
                    {
                        var thought = GetShortThought(result.Decision.Reasoning);
                        var action = result.Decision.Actions.Count > 0 
                            ? result.Decision.Actions[0].ActionType 
                            : "Wait";
                        
                        // Log if enabled - use GLOBAL log state
                        if (AIConfiguration.GlobalThoughtLogsEnabled)
                        {
                            LogWorldAI($"{lordDisplay}: {thought}");
                        }
                        
                        if (AIConfiguration.GlobalActionLogsEnabled && result.Decision.Actions.Count > 0)
                        {
                            var actionInfo = GetActionInfo(result.Decision.Actions[0]);
                            LogWorldAction($"{lordDisplay} -> {action}: {actionInfo}");
                        }
                        
                        // Record to thoughts panel
                        LordThoughtsPanel.RecordThought(
                            lord.Name.ToString(), 
                            thought, 
                            action);
                    }
                    else
                    {
                        Debug.Print($"[WorldAI] {lordDisplay} failed: {result.Error?.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.Print($"[WorldAI] Error processing {lord.Name}: {ex.Message}");
                }
                
                // Delay between lords to avoid rate limiting
                await Task.Delay(2000);
            }
        }
        catch (Exception ex)
        {
            Debug.Print($"[WorldAI] ProcessWorldAIAsync error: {ex.Message}");
        }
        finally
        {
            _isProcessing = false;
        }
    }
    
    /// <summary>
    /// Selects lords for AI processing this tick.
    /// Prioritizes important lords (kings, faction leaders) if configured.
    /// </summary>
    private List<Hero> SelectLordsForThinking(int maxCount)
    {
        var selectedLords = new List<Hero>();
        var eligibleLords = new List<Hero>();
        var importantLords = new List<Hero>();
        
        var mainHero = Hero.MainHero;
        var now = DateTime.Now;
        
        foreach (var hero in Campaign.Current.AliveHeroes)
        {
            // Skip player
            if (hero == mainHero) continue;
            
            // Only lords
            if (!hero.IsLord) continue;
            
            // Skip dead, prisoner, or wandering
            if (hero.IsDead || hero.IsPrisoner) continue;
            
            // Must have a clan
            if (hero.Clan == null) continue;
            
            var lordId = GetLordId(hero);
            
            // Check cooldown - skip if thought recently
            if (_lordLastThoughtTime.TryGetValue(lordId, out var lastThought))
            {
                if (now - lastThought < _lordCooldown)
                    continue;
            }
            
            // Categorize lords
            if (_config?.PrioritizeImportantLords == true)
            {
                if (hero.IsFactionLeader || hero.Clan?.Kingdom?.Leader == hero)
                {
                    importantLords.Add(hero);
                }
                else
                {
                    eligibleLords.Add(hero);
                }
            }
            else
            {
                eligibleLords.Add(hero);
            }
        }
        
        // First, pick from important lords
        while (selectedLords.Count < maxCount && importantLords.Count > 0)
        {
            var index = _random.Next(importantLords.Count);
            selectedLords.Add(importantLords[index]);
            importantLords.RemoveAt(index);
        }
        
        // Then fill with regular lords
        while (selectedLords.Count < maxCount && eligibleLords.Count > 0)
        {
            var index = _random.Next(eligibleLords.Count);
            selectedLords.Add(eligibleLords[index]);
            eligibleLords.RemoveAt(index);
        }
        
        return selectedLords;
    }
    
    private bool IsGameBusy()
    {
        try
        {
            if (Campaign.Current == null)
                return true;
            
            if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.Stop)
                return true;
            
            if (Game.Current?.GameStateManager?.ActiveState == null)
                return true;
            
            var stateName = Game.Current.GameStateManager.ActiveState.GetType().Name;
            
            // Only run on map
            if (!stateName.Contains("Map"))
                return true;
            
            // Skip during missions, menus, conversations
            if (stateName.Contains("Mission") || 
                stateName.Contains("Menu") || 
                stateName.Contains("Conversation") ||
                stateName.Contains("Encounter") ||
                stateName.Contains("Siege"))
                return true;
            
            return false;
        }
        catch
        {
            return true;
        }
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
                if (thought.Length > 80)
                    return thought.Substring(0, 77) + "...";
                return thought;
            }
        }
        
        var clean = reasoning.Replace("\n", " ").Replace("\r", "").Trim();
        if (clean.Length > 80)
            return clean.Substring(0, 77) + "...";
        return clean;
    }
    
    private string GetActionInfo(Core.Application.Interfaces.AgentAction action)
    {
        if (action.Parameters.TryGetValue("detail", out var detail))
            return detail?.ToString() ?? "";
        return "";
    }
    
    private void LogWorldAI(string message)
    {
        // Use GLOBAL log state
        if (!AIConfiguration.GlobalThoughtLogsEnabled) return;
        
        try
        {
            InformationManager.DisplayMessage(new InformationMessage(
                $"[?? World] {message}", Colors.Magenta));
            Debug.Print($"[WorldAI] {message}");
        }
        catch { }
    }
    
    private void LogWorldAction(string message)
    {
        // Use GLOBAL log state
        if (!AIConfiguration.GlobalActionLogsEnabled) return;
        
        try
        {
            InformationManager.DisplayMessage(new InformationMessage(
                $"[?? Action] {message}", Colors.Cyan));
            Debug.Print($"[WorldAI Action] {message}");
        }
        catch { }
    }
}
