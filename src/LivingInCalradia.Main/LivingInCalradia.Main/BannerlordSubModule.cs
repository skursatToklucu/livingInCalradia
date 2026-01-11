using System;
using System.Threading.Tasks;
using HarmonyLib;
using LivingInCalradia.AI.Configuration;
using LivingInCalradia.AI.Orchestration;
using LivingInCalradia.Core.Application.Interfaces;
using LivingInCalradia.Core.Application.Services;
using LivingInCalradia.Infrastructure.Bannerlord;
using LivingInCalradia.Main.Features;
using LivingInCalradia.Main.Input;
using LivingInCalradia.Main.Localization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace LivingInCalradia.Main;

/// <summary>
/// Main entry point for the Living in Calradia mod.
/// Inherits from MBSubModuleBase to integrate with Bannerlord's module system.
/// </summary>
public class BannerlordSubModule : MBSubModuleBase
{
    private const string HarmonyId = "com.livingincalradia.main";
    private const string ModName = "[LivingInCalradia]";
    
    private Harmony? _harmony;
    private AgentWorkflowService? _workflowService;
    private IAgentOrchestrator? _orchestrator;
    private BannerlordWorldSensor? _worldSensor;
    private BannerlordActionExecutor? _actionExecutor;
    private AIConfiguration? _config;
    private bool _isInitialized;
    private readonly object _configLock = new object(); // Thread safety for config
    
    // Tick timing control
    private float _lastTickTime;
    private float _lastKeyCheckTime;
    private float _tickIntervalSeconds = 60f;
    private int _maxLordsPerTick = 1;
    private const float KeyCheckIntervalSeconds = 0.1f;
    
    /// <summary>
    /// Called when the module is first loaded.
    /// </summary>
    protected override void OnSubModuleLoad()
    {
        base.OnSubModuleLoad();
        
        try
        {
            Log(LocalizedStrings.ModLoading);
            
            // Initialize Harmony for patching if needed
            _harmony = new Harmony(HarmonyId);
            // _harmony.PatchAll(); // Uncomment when patches are added
            
            // Initialize hotkey category (will be checked during gameplay)
            LivingInCalradiaHotKeys.Initialize();
            
            Log(LocalizedStrings.ModLoaded);
        }
        catch (Exception ex)
        {
            LogError($"{LocalizedStrings.ModLoadError}: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Called when the module is unloaded.
    /// </summary>
    protected override void OnSubModuleUnloaded()
    {
        base.OnSubModuleUnloaded();
        
        try
        {
            _harmony?.UnpatchAll(HarmonyId);
            Log(LocalizedStrings.ModUnloaded);
        }
        catch (Exception ex)
        {
            LogError($"{LocalizedStrings.ModUnloadError}: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Called when a game starts (campaign or other).
    /// </summary>
    protected override void OnGameStart(Game game, IGameStarter gameStarter)
    {
        base.OnGameStart(game, gameStarter);
        
        if (game.GameType is Campaign)
        {
            var campaignStarter = (CampaignGameStarter)gameStarter;
            
            // AI Dialogue behavior
            campaignStarter.AddBehavior(new AIDialogueBehavior());
            
            // Event-Driven AI behavior (reacts to game events)
            campaignStarter.AddBehavior(new AIEventBehavior());
            
            // World AI behavior (all lords think independently)
            campaignStarter.AddBehavior(new WorldAIBehavior());
            
            Log(LocalizedStrings.CampaignStarting);
        }
    }
    
    /// <summary>
    /// Called when entering the game (after loading).
    /// </summary>
    public override void OnGameInitializationFinished(Game game)
    {
        base.OnGameInitializationFinished(game);
        
        if (game.GameType is Campaign)
        {
            InitializeAISystem();
        }
    }
    
    /// <summary>
    /// Called when the game ends.
    /// </summary>
    public override void OnGameEnd(Game game)
    {
        base.OnGameEnd(game);
        
        _isInitialized = false;
        _workflowService = null;
        Log(LocalizedStrings.GameEnded);
    }
    
    /// <summary>
    /// Called every application tick.
    /// </summary>
    protected override void OnApplicationTick(float dt)
    {
        base.OnApplicationTick(dt);
        
        _lastKeyCheckTime += dt;
        if (_lastKeyCheckTime >= KeyCheckIntervalSeconds)
        {
            _lastKeyCheckTime = 0f;
            CheckHotkeys();
        }
        
        if (IsGamePaused())
            return;
        
        if (!_isInitialized || Campaign.Current == null)
            return;
        
        _lastTickTime += dt;
        
        if (_lastTickTime >= _tickIntervalSeconds)
        {
            _lastTickTime = 0f;
            ProcessAIAgentsAsync().ConfigureAwait(false);
        }
    }
    
    /// <summary>
    /// Checks if game is paused or in a state where AI should NOT run
    /// </summary>
    private bool IsGamePaused()
    {
        try
        {
            if (Campaign.Current == null)
                return true;
            
            var mode = Campaign.Current.TimeControlMode;
            if (mode == CampaignTimeControlMode.Stop)
                return true;
            
            if (Game.Current == null)
                return true;
            
            var gameStateManager = Game.Current.GameStateManager;
            if (gameStateManager == null)
                return true;
            
            var activeState = gameStateManager.ActiveState;
            if (activeState == null)
                return true;
            
            var stateTypeName = activeState.GetType().Name;
            
            // Only work in MapState - stop in all other states
            // MissionState = Battle
            // MenuGameState = Menu
            // MapState = Map (AI should work)
            
            // Battle check - stop if MissionState
            if (stateTypeName.Contains("Mission"))
            {
                return true; // In battle - AI should stop
            }
            
            // Menu check
            if (stateTypeName.Contains("Menu"))
            {
                return true; // In menu - AI should stop
            }
            
            // Dialogue/Conversation check
            if (stateTypeName.Contains("Conversation") || stateTypeName.Contains("Dialog"))
            {
                return true; // In dialogue - AI should stop
            }
            
            // Encounter check
            if (stateTypeName.Contains("Encounter"))
            {
                return true; // In encounter - AI should stop
            }
            
            // Siege screen check
            if (stateTypeName.Contains("Siege"))
            {
                return true; // In siege screen - AI should stop
            }
            
            // If not MapState, stop (safe side)
            if (!stateTypeName.Contains("Map"))
            {
                return true;
            }
            
            return false;
        }
        catch
        {
            return true; // In case of error, stay on safe side
        }
    }
    
    /// <summary>
    /// Check hotkeys for manual testing
    /// </summary>
    private void CheckHotkeys()
    {
        try
        {
            // Full AI Proof Test
            if (LivingInCalradiaHotKeys.IsFullProofTestPressed())
            {
                Log(LocalizedStrings.FullProofTestStarting);
                BannerlordActionExecutor.RunFullAIProofTest();
            }
            
            // Trigger Single Lord AI
            if (LivingInCalradiaHotKeys.IsTriggerSingleLordAIPressed())
            {
                TriggerSingleLordThinking();
            }
            
            // Quick Test
            if (LivingInCalradiaHotKeys.IsQuickTestPressed())
            {
                Log(LocalizedStrings.QuickTestStarting);
                BannerlordActionExecutor.RunProofTest();
            }
            
            // Toggle Logs
            if (LivingInCalradiaHotKeys.IsToggleLogsPressed())
            {
                ToggleAILogs();
            }
            
            // Show Thoughts Panel
            if (LivingInCalradiaHotKeys.IsShowThoughtsPanelPressed())
            {
                LordThoughtsPanel.ShowRecentThoughts();
            }
            
            // Show Settings Panel (NEW)
            if (LivingInCalradiaHotKeys.IsShowSettingsPressed())
            {
                UI.SettingsPanel.ShowPanel();
            }
        }
        catch
        {
        }
    }
    
    /// <summary>
    /// Toggles AI thought and action logs on/off
    /// </summary>
    private void ToggleAILogs()
    {
        lock (_configLock)
        {
            if (_config == null)
            {
                LogError("Config not loaded");
                return;
            }
            
            // Toggle both thought and action logs together
            var newState = !_config.EnableThoughtLogs;
            _config.EnableThoughtLogs = newState;
            _config.EnableActionLogs = newState;
            
            // Update GLOBAL log state (affects all behaviors)
            AIConfiguration.SetGlobalLogState(newState, newState, _config.EnableDebugLogs);
            
            // Update the action executor's log setting
            BannerlordActionExecutor.SetLogsEnabled(newState);
            
            var status = newState ? "ON" : "OFF";
            var color = newState ? Colors.Green : Colors.Yellow;
            
            InformationManager.DisplayMessage(new InformationMessage(
                $"[LivingInCalradia] AI Logs: {status}",
                color));
            
            InformationManager.DisplayMessage(new InformationMessage(
                $"  Thought logs: {status} | Action logs: {status}",
                Colors.White));
            
            // Save config to file
            SaveConfigToFile();
        }
    }
    
    /// <summary>
    /// Saves the current config to file for persistence
    /// </summary>
    private void SaveConfigToFile()
    {
        try
        {
            if (_config == null) return;
            
            var configPath = GetConfigFilePath();
            if (string.IsNullOrEmpty(configPath)) return;
            
            var json = $@"{{
  ""Provider"": ""{_config.Provider}"",
  ""ApiKey"": ""{_config.ApiKey}"",
  ""ApiBaseUrl"": ""{_config.ApiBaseUrl}"",
  ""ModelId"": ""{_config.ModelId}"",
  ""Temperature"": {_config.Temperature.ToString(System.Globalization.CultureInfo.InvariantCulture)},
  ""MaxTokens"": {_config.MaxTokens},

  ""EnableThoughtLogs"": {_config.EnableThoughtLogs.ToString().ToLower()},
  ""EnableActionLogs"": {_config.EnableActionLogs.ToString().ToLower()},
  ""EnableDebugLogs"": {_config.EnableDebugLogs.ToString().ToLower()},

  ""TickIntervalSeconds"": {_config.TickIntervalSeconds},
  ""MaxLordsPerTick"": {_config.MaxLordsPerTick},

  ""EnableEventDrivenAI"": {_config.EnableEventDrivenAI.ToString().ToLower()},
  ""EventCooldownMinutes"": {_config.EventCooldownMinutes},

  ""EnableWorldAI"": {_config.EnableWorldAI.ToString().ToLower()},
  ""WorldTickIntervalSeconds"": {_config.WorldTickIntervalSeconds},
  ""WorldMaxLordsPerTick"": {_config.WorldMaxLordsPerTick},
  ""PrioritizeImportantLords"": {_config.PrioritizeImportantLords.ToString().ToLower()},

  ""SkipMinorBattleEvents"": {_config.SkipMinorBattleEvents.ToString().ToLower()},
  ""MinimumBattleSize"": {_config.MinimumBattleSize},

  ""HotkeyShowSettings"": ""{_config.HotkeyShowSettings}"",
  ""HotkeyFullProofTest"": ""{_config.HotkeyFullProofTest}"",
  ""HotkeyTriggerAI"": ""{_config.HotkeyTriggerAI}"",
  ""HotkeyQuickTest"": ""{_config.HotkeyQuickTest}"",
  ""HotkeyToggleLogs"": ""{_config.HotkeyToggleLogs}"",
  ""HotkeyShowThoughts"": ""{_config.HotkeyShowThoughts}""
}}";
            
            System.IO.File.WriteAllText(configPath, json);
            LogDebug($"Config saved to {configPath}");
        }
        catch (Exception ex)
        {
            LogDebug($"Failed to save config: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Gets the config file path
    /// </summary>
    private string GetConfigFilePath()
    {
        var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var assemblyDir = System.IO.Path.GetDirectoryName(assemblyLocation) ?? "";
        return System.IO.Path.Combine(assemblyDir, "ai-config.json");
    }
    
    /// <summary>
    /// Triggers AI thinking for a single random lord
    /// </summary>
    private void TriggerSingleLordThinking()
    {
        if (!_isInitialized || _workflowService == null)
        {
            LogError(LocalizedStrings.AISystemNotReady);
            return;
        }
        
        try
        {
            var mainHero = Hero.MainHero;
            if (mainHero == null || Campaign.Current?.AliveHeroes == null)
                return;
            
            // Select a random lord
            Hero? selectedLord = null;
            foreach (var hero in Campaign.Current.AliveHeroes)
            {
                if (hero != mainHero && hero.IsLord && hero.Clan?.Kingdom != null)
                {
                    selectedLord = hero;
                    break;
                }
            }
            
            if (selectedLord == null)
            {
                LogError(LocalizedStrings.LordNotFound);
                return;
            }
            
            var agentId = GetAgentId(selectedLord);
            
            // Show thinking notification
            LordThoughtsPanel.ShowThinkingNotification(selectedLord.Name.ToString());
            
            // Run async
            var lord = selectedLord; // Capture for closure
            Task.Run(async () =>
            {
                try
                {
                    var result = await _workflowService.ExecuteWorkflowAsync(agentId);
                    
                    // Check pause after async operation
                    if (IsGamePaused())
                    {
                        Log(LocalizedStrings.GamePausedAIResultHidden);
                        return;
                    }
                    
                    if (result.IsSuccessful && result.Decision != null)
                    {
                        var shortReasoning = GetShortReasoning(result.Decision.Reasoning);
                        
                        // Record to thoughts panel
                        var actionName = result.Decision.Actions.Count > 0 
                            ? result.Decision.Actions[0].ActionType 
                            : "Wait";
                        var actionDetail = "";
                        if (result.Decision.Actions.Count > 0 && 
                            result.Decision.Actions[0].Parameters.ContainsKey("detail"))
                        {
                            actionDetail = result.Decision.Actions[0].Parameters["detail"]?.ToString() ?? "";
                        }
                        
                        LordThoughtsPanel.RecordThought(lord.Name.ToString(), shortReasoning, actionName);
                        LordThoughtsPanel.ShowDecisionNotification(lord.Name.ToString(), actionName, actionDetail);
                        
                        foreach (var action in result.Decision.Actions)
                        {
                            var detail = action.Parameters.ContainsKey("detail")
                                ? action.Parameters["detail"]?.ToString()
                                : "";
                            Log($"  -> {action.ActionType}: {detail}");
                        }
                    }
                    else
                    {
                        LogError($"{LocalizedStrings.WorkflowFailed}: {result.Error?.Message}");
                    }
                }
                catch (Exception ex)
                {
                    LogError($"{LocalizedStrings.AIThinkingError}: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            LogError($"TriggerSingleLordThinking error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Initializes the AI system with all dependencies.
    /// </summary>
    private void InitializeAISystem()
    {
        try
        {
            Log(LocalizedStrings.AISystemStarting);
            
            // Load configuration
            _config = AIConfiguration.Load();
            
            // Initialize hotkeys from config
            LivingInCalradiaHotKeys.InitializeFromConfig(
                _config.HotkeyFullProofTest,
                _config.HotkeyTriggerAI,
                _config.HotkeyQuickTest,
                _config.HotkeyToggleLogs,
                _config.HotkeyShowThoughts,
                _config.HotkeyShowSettings);
            
            // Initialize Settings Panel with config reference for saving
            UI.SettingsPanel.Initialize(_config, SaveConfigToFile, ReinitializeOrchestrator);
            
            // Apply configuration settings
            _tickIntervalSeconds = _config.TickIntervalSeconds;
            _maxLordsPerTick = _config.MaxLordsPerTick;
            
            // Set GLOBAL log state from config
            AIConfiguration.SetGlobalLogState(
                _config.EnableThoughtLogs, 
                _config.EnableActionLogs, 
                _config.EnableDebugLogs);
            
            // Set initial log state for action executor
            BannerlordActionExecutor.SetLogsEnabled(_config.EnableActionLogs);
            
            // Check if API key is configured
            if (!_config.HasApiKey)
            {
                LogError("API Key not configured!");
                Log("Press [Insert] to configure your AI provider");
                
                // Show warning after a short delay
                Task.Run(async () =>
                {
                    await Task.Delay(2000);
                    UI.SettingsPanel.ShowApiKeyMissingWarning();
                });
                
                // Still mark as initialized so settings can be accessed
                _isInitialized = false;
                UI.SettingsPanel.ShowQuickStatus();
                return;
            }
            
            LogDebug($"Provider: {_config.Provider}, Model: {_config.ModelId}");
            LogDebug($"TickInterval: {_tickIntervalSeconds}s, MaxLords: {_maxLordsPerTick}");
            LogDebug($"Logs - Thoughts: {_config.EnableThoughtLogs}, Actions: {_config.EnableActionLogs}");
            
            // Create orchestrator based on provider
            CreateOrchestrator();
            
            // Create Bannerlord-specific implementations
            _worldSensor = new BannerlordWorldSensor();
            _actionExecutor = new BannerlordActionExecutor();
            
            // Create workflow service
            _workflowService = new AgentWorkflowService(
                _worldSensor,
                _orchestrator,
                _actionExecutor);
            
            _isInitialized = true;
            Log(LocalizedStrings.AISystemStarted);
            
            // Show configured hotkeys in a nice format
            UI.SettingsPanel.ShowQuickStatus();
            
            // Display in-game message
            InformationManager.DisplayMessage(new InformationMessage(
                LocalizedStrings.ModActiveMessage("English"),
                Colors.Green));
        }
        catch (Exception ex)
        {
            LogError($"{LocalizedStrings.AISystemError}: {ex.Message}");
            InformationManager.DisplayMessage(new InformationMessage(
                $"LivingInCalradia {LocalizedStrings.Error}: {ex.Message}",
                Colors.Red));
        }
    }
    
    /// <summary>
    /// Creates the AI orchestrator based on configured provider
    /// </summary>
    private void CreateOrchestrator()
    {
        if (_config == null || !_config.HasApiKey) return;
        
        if (_config.IsOpenAICompatible)
        {
            // Use universal orchestrator for all OpenAI-compatible APIs
            _orchestrator = new GroqOrchestrator(_config);
            LogDebug($"Created orchestrator for {_config.Provider}");
        }
        else
        {
            // Fallback to Semantic Kernel for non-compatible APIs
            var kernelFactory = new KernelFactory()
                .WithOpenAI(_config.ApiKey, _config.ModelId, _config.OrganizationId);
            _orchestrator = kernelFactory.BuildOrchestrator();
            LogDebug("Created SemanticKernel orchestrator");
        }
    }
    
    /// <summary>
    /// Reinitializes the orchestrator when provider/key changes
    /// </summary>
    private void ReinitializeOrchestrator()
    {
        if (_config == null)
        {
            LogError("ReinitializeOrchestrator: Config is null!");
            return;
        }
        
        try
        {
            Log($"Reinitializing: {_config.Provider} / {_config.ModelId}");
            
            if (!_config.HasApiKey)
            {
                _isInitialized = false;
                _orchestrator = null;
                _workflowService = null;
                Log("AI disabled - no API key");
                return;
            }
            
            // Use log settings from config (don't override user preference)
            AIConfiguration.SetGlobalLogState(_config.EnableThoughtLogs, _config.EnableActionLogs, _config.EnableDebugLogs);
            BannerlordActionExecutor.SetLogsEnabled(_config.EnableActionLogs);
            
            // Ensure world sensor exists
            if (_worldSensor == null)
            {
                _worldSensor = new BannerlordWorldSensor();
            }
            
            // Ensure action executor exists
            if (_actionExecutor == null)
            {
                _actionExecutor = new BannerlordActionExecutor();
            }
            
            // Recreate orchestrator with new config
            Log($"Creating orchestrator: {_config.Provider}");
            _orchestrator = new GroqOrchestrator(_config);
            
            // Recreate workflow service
            if (_orchestrator != null)
            {
                _workflowService = new AgentWorkflowService(
                    _worldSensor,
                    _orchestrator,
                    _actionExecutor);
                
                _isInitialized = true;
                
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[LivingInCalradia] AI Ready: {_config.Provider}",
                    Colors.Green));
                
                // Save config
                SaveConfigToFile();
            }
            else
            {
                LogError("Orchestrator creation failed!");
                _isInitialized = false;
            }
        }
        catch (Exception ex)
        {
            LogError($"Reinitialize failed: {ex.Message}");
            _isInitialized = false;
        }
    }
    
    /// <summary>
    /// Processes AI agents asynchronously.
    /// </summary>
    private async Task ProcessAIAgentsAsync()
    {
        if (_workflowService == null || Campaign.Current == null || _config == null)
            return;
        
        if (IsGamePaused())
        {
            LogDebug(LocalizedStrings.GamePausedAICancelled);
            return;
        }
        
        try
        {
            var mainHero = Hero.MainHero;
            if (mainHero == null)
                return;
            
            var nearbyHeroes = GetNearbyHeroes(mainHero, maxDistance: 100f);
            var processedCount = 0;
            
            foreach (var hero in nearbyHeroes)
            {
                // Limit lords per tick
                if (processedCount >= _maxLordsPerTick)
                    break;
                
                // Check pause before each hero
                if (IsGamePaused())
                {
                    LogDebug(LocalizedStrings.GamePausedLoopStopped);
                    return;
                }
                
                if (hero == mainHero || hero.IsDead)
                    continue;
                
                var agentId = GetAgentId(hero);
                var heroInfo = GetHeroDisplayInfo(hero);
                var heroKingdom = GetHeroKingdomDisplay(hero);
                
                LogDebug(LocalizedStrings.LordThinking(heroInfo));
                
                var result = await _workflowService.ExecuteWorkflowAsync(agentId);
                processedCount++;
                
                // Check again after async operation
                if (IsGamePaused())
                {
                    LogDebug(LocalizedStrings.GamePausedResultNotShown);
                    return;
                }
                
                if (result.IsSuccessful && result.Decision != null)
                {
                    var actions = result.Decision.Actions;
                    var reasoning = result.Decision.Reasoning;
                    var shortReasoning = GetShortReasoning(reasoning);
                    
                    // Log thoughts only if enabled - format: [AI] LordName (Kingdom): thought
                    if (_config.EnableThoughtLogs && !string.IsNullOrEmpty(shortReasoning))
                    {
                        LogAI($"{heroKingdom}: {shortReasoning}");
                    }
                    
                    // Log actions only if enabled
                    if (_config.EnableActionLogs)
                    {
                        if (actions.Count > 0)
                        {
                            foreach (var action in actions)
                            {
                                var detail = action.Parameters.ContainsKey("detail") 
                                    ? action.Parameters["detail"]?.ToString() 
                                    : "";
                                
                                if (!string.IsNullOrEmpty(detail))
                                {
                                    Log($"{heroKingdom} -> {action.ActionType}: {detail}");
                                }
                                else
                                {
                                    Log($"{heroKingdom} -> {action.ActionType}");
                                }
                            }
                        }
                        else
                        {
                            LogDebug(LocalizedStrings.LordDecidedToWait(heroInfo));
                        }
                    }
                    
                    // Always record to thoughts panel (for NumPad5)
                    var actionName = actions.Count > 0 ? actions[0].ActionType : "Wait";
                    LordThoughtsPanel.RecordThought(hero.Name.ToString(), shortReasoning, actionName);
                }
                else
                {
                    LogError($"{heroInfo}: {LocalizedStrings.WorkflowFailed}: {result.Error?.Message}");
                }
                
                await Task.Delay(500);
            }
        }
        catch (Exception ex)
        {
            LogError($"{LocalizedStrings.AIProcessingError}: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Gets hero name with kingdom for display in logs.
    /// Format: "LordName (Kingdom)" or "LordName (Independent)"
    /// </summary>
    private string GetHeroKingdomDisplay(Hero hero)
    {
        var name = hero.Name?.ToString() ?? "Unknown";
        var kingdom = hero.Clan?.Kingdom?.Name?.ToString();
        
        if (!string.IsNullOrEmpty(kingdom))
        {
            return $"{name} ({kingdom})";
        }
        
        return $"{name} (Independent)";
    }
    
    private string GetHeroDisplayInfo(Hero hero)
    {
        var sb = new System.Text.StringBuilder();
        
        if (hero.IsFactionLeader)
            sb.Append(LocalizedStrings.King);
        else if (hero.IsLord)
            sb.Append(LocalizedStrings.Lord);
        else if (hero.IsNotable)
            sb.Append(LocalizedStrings.Notable);
        
        sb.Append(hero.Name);
        
        if (hero.Clan != null)
        {
            sb.Append($" ({hero.Clan.Name}");
            if (hero.Clan.Kingdom != null)
                sb.Append($" - {hero.Clan.Kingdom.Name}");
            sb.Append(")");
        }
        
        return sb.ToString();
    }
    
    private System.Collections.Generic.List<Hero> GetNearbyHeroes(Hero mainHero, float maxDistance)
    {
        var nearbyHeroes = new System.Collections.Generic.List<Hero>();
        var random = new Random();
        
        if (Campaign.Current?.AliveHeroes == null)
            return nearbyHeroes;
        
        var eligibleHeroes = new System.Collections.Generic.List<Hero>();
        
        foreach (var hero in Campaign.Current.AliveHeroes)
        {
            if (hero == mainHero || hero.IsDead)
                continue;
            
            if (!hero.IsLord && !hero.IsNotable)
                continue;
            
            if (mainHero.CurrentSettlement != null && 
                hero.CurrentSettlement == mainHero.CurrentSettlement)
            {
                eligibleHeroes.Add(hero);
            }
            else if (mainHero.PartyBelongedTo != null && hero.PartyBelongedTo != null)
            {
                eligibleHeroes.Add(hero);
            }
        }
        
        while (nearbyHeroes.Count < 3 && eligibleHeroes.Count > 0)
        {
            var index = random.Next(eligibleHeroes.Count);
            nearbyHeroes.Add(eligibleHeroes[index]);
            eligibleHeroes.RemoveAt(index);
        }
        
        return nearbyHeroes;
    }
    
    private string GetAgentId(Hero hero)
    {
        var heroType = hero.IsLord ? "Lord" : hero.IsNotable ? "Notable" : "Hero";
        var faction = hero.Clan?.Name?.ToString() ?? "Unknown";
        return $"{heroType}_{hero.Name}_{faction}";
    }
    
    private string GetShortReasoning(string reasoning)
    {
        if (string.IsNullOrEmpty(reasoning))
            return "";
        
        var lines = reasoning.Split('\n');
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("THOUGHT:", StringComparison.OrdinalIgnoreCase))
            {
                var thought = trimmed.Substring(trimmed.IndexOf(':') + 1).Trim();
                if (thought.Length > 150)
                    return thought.Substring(0, 147) + "...";
                return thought;
            }
        }
        
        var clean = reasoning.Replace("\n", " ").Replace("\r", "").Trim();
        if (clean.Length > 150)
            return clean.Substring(0, 147) + "...";
        return clean;
    }
    
    #region Logging Helpers
    
    private void Log(string message)
    {
        InformationManager.DisplayMessage(new InformationMessage(
            $"{ModName} {message}",
            Colors.Cyan));
        
        Debug.Print($"{ModName} {message}");
    }
    
    private void LogAI(string message)
    {
        InformationManager.DisplayMessage(new InformationMessage(
            $"[AI] {message}",
            Colors.Yellow));
        
        Debug.Print($"[AI] {message}");
    }
    
    private void LogDebug(string message)
    {
        // Only show debug logs if enabled in config
        if (_config?.EnableDebugLogs == true)
        {
            InformationManager.DisplayMessage(new InformationMessage(
                $"{ModName} [DEBUG] {message}",
                Colors.Gray));
        }
        
        // Always write to debug output
        Debug.Print($"{ModName} [DEBUG] {message}");
    }
    
    private void LogError(string message)
    {
        InformationManager.DisplayMessage(new InformationMessage(
            $"{ModName} {LocalizedStrings.Error}: {message}",
            Colors.Red));
        
        Debug.Print($"{ModName} ERROR: {message}");
    }
    
    #endregion
}
