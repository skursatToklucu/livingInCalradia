using System;
using System.Threading.Tasks;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using LivingInCalradia.AI.Configuration;
using LivingInCalradia.AI.Orchestration;
using LivingInCalradia.Core.Application.Interfaces;
using LivingInCalradia.Core.Application.Services;
using LivingInCalradia.Infrastructure.Bannerlord;
using LivingInCalradia.Main.Features;
using LivingInCalradia.Main.Localization;

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
    private bool _isInitialized;
    private string _effectiveLanguage = "en";
    
    // Tick timing control
    private float _lastTickTime;
    private float _lastKeyCheckTime;
    private const float TickIntervalSeconds = 30f;
    private const float KeyCheckIntervalSeconds = 0.1f;
    
    /// <summary>
    /// Called when the module is first loaded.
    /// </summary>
    protected override void OnSubModuleLoad()
    {
        base.OnSubModuleLoad();
        
        try
        {
            // Pre-load config to get language for early messages
            try
            {
                var config = AIConfiguration.Load();
                if (config.IsAuto)
                {
                    var gameLanguage = DetectGameLanguage();
                    config.SetGameLanguage(gameLanguage);
                }
                _effectiveLanguage = config.EffectiveLanguage;
                LocalizedStrings.SetLanguage(_effectiveLanguage);
            }
            catch
            {
                LocalizedStrings.SetLanguage("en");
            }
            
            Log(LocalizedStrings.ModLoading);
            
            // Initialize Harmony for patching if needed
            _harmony = new Harmony(HarmonyId);
            // _harmony.PatchAll(); // Uncomment when patches are added
            
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
        
        if (_lastTickTime >= TickIntervalSeconds)
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
            
            // SADECE MapState'de calis - diger tum state'lerde durdur
            // MissionState = Savas
            // MenuGameState = Menu
            // MapState = Harita (AI calismali)
            
            // Savas kontrolu - MissionState varsa durdur
            if (stateTypeName.Contains("Mission"))
            {
                return true; // Savasta - AI durmali
            }
            
            // Menu kontrolu
            if (stateTypeName.Contains("Menu"))
            {
                return true; // Menude - AI durmali
            }
            
            // Diyalog/Konusma kontrolu
            if (stateTypeName.Contains("Conversation") || stateTypeName.Contains("Dialog"))
            {
                return true; // Diyalogda - AI durmali
            }
            
            // Encounter (karsilasma) kontrolu
            if (stateTypeName.Contains("Encounter"))
            {
                return true; // Karsilasmada - AI durmali
            }
            
            // Kusatma ekrani kontrolu
            if (stateTypeName.Contains("Siege"))
            {
                return true; // Kusatma ekraninda - AI durmali
            }
            
            // MapState degilse durdur (guvenli taraf)
            if (!stateTypeName.Contains("Map"))
            {
                return true;
            }
            
            return false;
        }
        catch
        {
            return true; // Hata durumunda guvenli tarafta kal
        }
    }
    
    /// <summary>
    /// Check hotkeys for manual testing
    /// </summary>
    private void CheckHotkeys()
    {
        try
        {
            // NumPad1 = Full Proof Test
            if (TaleWorlds.InputSystem.Input.IsKeyPressed(TaleWorlds.InputSystem.InputKey.Numpad1))
            {
                Log(LocalizedStrings.FullProofTestStarting);
                BannerlordActionExecutor.RunFullAIProofTest();
            }
            
            // NumPad2 = Tek lord icin AI dusunme
            if (TaleWorlds.InputSystem.Input.IsKeyPressed(TaleWorlds.InputSystem.InputKey.Numpad2))
            {
                TriggerSingleLordThinking();
            }
            
            // NumPad3 = Hizli test
            if (TaleWorlds.InputSystem.Input.IsKeyPressed(TaleWorlds.InputSystem.InputKey.Numpad3))
            {
                Log(LocalizedStrings.QuickTestStarting);
                BannerlordActionExecutor.RunProofTest();
            }
            
            // NumPad5 = Lord dusunceleri paneli
            if (TaleWorlds.InputSystem.Input.IsKeyPressed(TaleWorlds.InputSystem.InputKey.Numpad5))
            {
                LordThoughtsPanel.ShowRecentThoughts();
            }
        }
        catch
        {
        }
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
            
            // Rastgele bir lord sec
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
            
            // Async olarak calistir
            var lord = selectedLord; // Capture for closure
            Task.Run(async () =>
            {
                try
                {
                    var result = await _workflowService.ExecuteWorkflowAsync(agentId);
                    
                    // Async islem bittikten sonra pause kontrolu
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
            var config = AIConfiguration.Load();
            config.Validate();
            
            // Auto-detect game language if set to "auto"
            if (config.IsAuto)
            {
                var gameLanguage = DetectGameLanguage();
                config.SetGameLanguage(gameLanguage);
                Log(LocalizedStrings.LanguageDetected(gameLanguage, config.EffectiveLanguage));
            }
            
            _effectiveLanguage = config.EffectiveLanguage;
            LocalizedStrings.SetLanguage(_effectiveLanguage);
<<<<<<< HEAD
            BannerlordActionExecutor.SetLanguage(_effectiveLanguage);
=======
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
            
            Log($"Provider: {config.Provider}, Model: {config.ModelId}, Language: {config.EffectiveLanguage}");
            
            // Create orchestrator based on provider
            if (config.IsGroq)
            {
                _orchestrator = new GroqOrchestrator(config.ApiKey, config.ModelId, config.Temperature, config.EffectiveLanguage);
                Log("GroqOrchestrator (direct API)");
            }
            else
            {
                var kernelFactory = new KernelFactory()
                    .WithOpenAI(config.ApiKey, config.ModelId, config.OrganizationId);
                _orchestrator = kernelFactory.BuildOrchestrator();
                Log("SemanticKernelOrchestrator");
            }
            
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
            Log("Hotkeys: NumPad1=FullTest, NumPad2=AI, NumPad3=QuickTest, NumPad5=Thoughts");
            
            // Display in-game message
            var langText = config.IsTurkish ? "Turkce" : "English";
            InformationManager.DisplayMessage(new InformationMessage(
                LocalizedStrings.ModActiveMessage(langText),
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
    /// Detects the current game language from Bannerlord settings.
    /// </summary>
    private string DetectGameLanguage()
    {
        try
        {
            var activeTextLanguage = MBTextManager.ActiveTextLanguage;
            if (!string.IsNullOrEmpty(activeTextLanguage))
                return activeTextLanguage;
        }
        catch { }
        
        try
        {
            var testText = GameTexts.FindText("str_menu_return_to_campaign_map")?.ToString() ?? "";
            
            if (testText.Contains("Kampanya") || testText.Contains("harita"))
                return "Turkish";
            if (testText.Contains("Karte") || testText.Contains("Kampagne"))
                return "German";
            if (testText.Contains("carte") || testText.Contains("campagne"))
                return "French";
            if (testText.Contains("mapa"))
                return "Spanish";
        }
        catch { }
        
        try
        {
            var culture = System.Globalization.CultureInfo.CurrentUICulture;
            if (culture.Name.StartsWith("tr", StringComparison.OrdinalIgnoreCase))
                return "Turkish";
        }
        catch { }
        
        return "English";
    }
    
    /// <summary>
    /// Processes AI agents asynchronously.
    /// </summary>
    private async Task ProcessAIAgentsAsync()
    {
        if (_workflowService == null || Campaign.Current == null)
            return;
        
        if (IsGamePaused())
        {
            Log(LocalizedStrings.GamePausedAICancelled);
            return;
        }
        
        try
        {
            var mainHero = Hero.MainHero;
            if (mainHero == null)
                return;
            
            var nearbyHeroes = GetNearbyHeroes(mainHero, maxDistance: 100f);
            
            foreach (var hero in nearbyHeroes)
            {
                // Her hero islemeden once pause kontrolu
                if (IsGamePaused())
                {
                    Log(LocalizedStrings.GamePausedLoopStopped);
                    return;
                }
                
                if (hero == mainHero || hero.IsDead)
                    continue;
                
                var agentId = GetAgentId(hero);
                var heroInfo = GetHeroDisplayInfo(hero);
                
                Log(LocalizedStrings.LordThinking(heroInfo));
                
                var result = await _workflowService.ExecuteWorkflowAsync(agentId);
                
                // Async islem sonrasi tekrar kontrol
                if (IsGamePaused())
                {
                    Log(LocalizedStrings.GamePausedResultNotShown);
                    return;
                }
                
                if (result.IsSuccessful && result.Decision != null)
                {
                    var actions = result.Decision.Actions;
                    var reasoning = result.Decision.Reasoning;
                    var shortReasoning = GetShortReasoning(reasoning);
                    
                    if (!string.IsNullOrEmpty(shortReasoning))
                    {
                        LogAI($"{hero.Name}: {shortReasoning}");
                    }
                    
                    if (actions.Count > 0)
                    {
                        foreach (var action in actions)
                        {
                            var detail = action.Parameters.ContainsKey("detail") 
                                ? action.Parameters["detail"]?.ToString() 
                                : "";
                            
                            if (!string.IsNullOrEmpty(detail))
                            {
                                Log($"{heroInfo} -> {action.ActionType}: {detail}");
                            }
                            else
                            {
                                Log($"{heroInfo} -> {action.ActionType}");
                            }
                        }
                    }
                    else
                    {
                        Log(LocalizedStrings.LordDecidedToWait(heroInfo));
                    }
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
    
    private string GetShortReasoning(string reasoning)
    {
        if (string.IsNullOrEmpty(reasoning))
            return "";
        
        var lines = reasoning.Split('\n');
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            // Support both Turkish and English
            if (trimmed.StartsWith("DUSUNCE:", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("THOUGHT:", StringComparison.OrdinalIgnoreCase))
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
    
    #region Logging Helpers
    
    private static void Log(string message)
    {
        InformationManager.DisplayMessage(new InformationMessage(
            $"{ModName} {message}",
            Colors.Cyan));
        
        Debug.Print($"{ModName} {message}");
    }
    
    private static void LogAI(string message)
    {
        InformationManager.DisplayMessage(new InformationMessage(
            $"[AI] {message}",
            Colors.Yellow));
        
        Debug.Print($"[AI] {message}");
    }
    
    private static void LogError(string message)
    {
        InformationManager.DisplayMessage(new InformationMessage(
            $"{ModName} {LocalizedStrings.Error}: {message}",
            Colors.Red));
        
        Debug.Print($"{ModName} ERROR: {message}");
    }
    
    #endregion
}
