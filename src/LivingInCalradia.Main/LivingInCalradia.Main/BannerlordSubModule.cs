using System;
using System.Threading.Tasks;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using LivingInCalradia.AI.Configuration;
using LivingInCalradia.AI.Orchestration;
using LivingInCalradia.Core.Application.Interfaces;
using LivingInCalradia.Core.Application.Services;
using LivingInCalradia.Infrastructure.Bannerlord;

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
            Log("Mod yükleniyor...");
            
            // Initialize Harmony for patching if needed
            _harmony = new Harmony(HarmonyId);
            // _harmony.PatchAll(); // Uncomment when patches are added
            
            Log("Mod ba?ar?yla yüklendi!");
        }
        catch (Exception ex)
        {
            LogError($"Mod yüklenirken hata: {ex.Message}");
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
            Log("Mod kald?r?ld?.");
        }
        catch (Exception ex)
        {
            LogError($"Mod kald?r?l?rken hata: {ex.Message}");
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
            
            // AI Dialogue behavior'? ekle
            campaignStarter.AddBehavior(new AIDialogueBehavior());
            
            Log("Campaign ba?lat?l?yor, AI sistemi ve Diyalog sistemi haz?rlan?yor...");
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
        Log("Oyun sona erdi, AI sistemi kapat?ld?.");
    }
    
    /// <summary>
    /// Called every application tick.
    /// </summary>
    protected override void OnApplicationTick(float dt)
    {
        base.OnApplicationTick(dt);
        
        // Hotkey kontrolü her zaman çal??s?n (pause'da da test yapabilelim)
        _lastKeyCheckTime += dt;
        if (_lastKeyCheckTime >= KeyCheckIntervalSeconds)
        {
            _lastKeyCheckTime = 0f;
            CheckHotkeys();
        }
        
        // Oyun duraklat?lm??sa AI çal??mas?n
        if (IsGamePaused())
            return;
        
        if (!_isInitialized || Campaign.Current == null)
            return;
        
        _lastTickTime += dt;
        
        // AI dü?ünme döngüsü - belirli aral?klarla çal??
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
            
            // TimeControlMode.Stop = Oyun tamamen durduruldu (ESC veya Space)
            var mode = Campaign.Current.TimeControlMode;
            if (mode == CampaignTimeControlMode.Stop)
            {
                return true;
            }
            
            // Game.Current kontrolü
            if (Game.Current == null)
                return true;
            
            // GameStateManager kontrolü
            var gameStateManager = Game.Current.GameStateManager;
            if (gameStateManager == null)
                return true;
            
            var activeState = gameStateManager.ActiveState;
            if (activeState == null)
                return true;
            
            // State tipini kontrol et
            var stateTypeName = activeState.GetType().Name;
            
            // SADECE MapState'de çal?? - di?er tüm state'lerde durdur
            // MissionState = Sava?
            // MenuGameState = Menü
            // MapState = Harita (AI çal??mal?)
            
            // Sava? kontrolü - MissionState varsa durdur
            if (stateTypeName.Contains("Mission"))
            {
                return true; // Sava?ta - AI durmal?
            }
            
            // Menü kontrolü
            if (stateTypeName.Contains("Menu"))
            {
                return true; // Menüde - AI durmal?
            }
            
            // Diyalog/Konu?ma kontrolü
            if (stateTypeName.Contains("Conversation") || stateTypeName.Contains("Dialog"))
            {
                return true; // Diyalogda - AI durmal?
            }
            
            // Encounter (kar??la?ma) kontrolü
            if (stateTypeName.Contains("Encounter"))
            {
                return true; // Kar??la?mada - AI durmal?
            }
            
            // Ku?atma ekran? kontrolü
            if (stateTypeName.Contains("Siege"))
            {
                return true; // Ku?atma ekran?nda - AI durmal?
            }
            
            // MapState de?ilse durdur (güvenli taraf)
            if (!stateTypeName.Contains("Map"))
            {
                return true;
            }
            
            return false;
        }
        catch
        {
            return true; // Hata durumunda güvenli tarafta kal
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
                Log("Full Proof Test ba?lat?l?yor...");
                BannerlordActionExecutor.RunFullAIProofTest();
            }
            
            // NumPad2 = Tek lord için AI dü?ünme
            if (TaleWorlds.InputSystem.Input.IsKeyPressed(TaleWorlds.InputSystem.InputKey.Numpad2))
            {
                TriggerSingleLordThinking();
            }
            
            // NumPad3 = H?zl? test
            if (TaleWorlds.InputSystem.Input.IsKeyPressed(TaleWorlds.InputSystem.InputKey.Numpad3))
            {
                Log("H?zl? Proof Test ba?lat?l?yor...");
                BannerlordActionExecutor.RunProofTest();
            }
        }
        catch
        {
            // Input system might not be available
        }
    }
    
    /// <summary>
    /// Triggers AI thinking for a single random lord
    /// </summary>
    private void TriggerSingleLordThinking()
    {
        if (!_isInitialized || _workflowService == null)
        {
            LogError("AI sistemi henüz haz?r de?il!");
            return;
        }
        
        // NumPad2'ye bas?ld???nda pause kontrolü yapma - manuel test için izin ver
        // Ama async i?lem s?ras?nda kontrol et
        
        try
        {
            var mainHero = Hero.MainHero;
            if (mainHero == null || Campaign.Current?.AliveHeroes == null)
                return;
            
            // Rastgele bir lord seç
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
                LogError("Dü?ünecek lord bulunamad?!");
                return;
            }
            
            var agentId = GetAgentId(selectedLord);
            var heroInfo = GetHeroDisplayInfo(selectedLord);
            
            Log($"{heroInfo} dü?ünüyor...");
            
            // Async olarak çal??t?r
            var lord = selectedLord; // Capture for closure
            Task.Run(async () =>
            {
                try
                {
                    var result = await _workflowService.ExecuteWorkflowAsync(agentId);
                    
                    // Async i?lem bittikten sonra pause kontrolü
                    if (IsGamePaused())
                    {
                        Log("Oyun duraklat?ld?, AI sonucu gösterilmeyecek.");
                        return;
                    }
                    
                    if (result.IsSuccessful && result.Decision != null)
                    {
                        LogAI($"{lord.Name}: {GetShortReasoning(result.Decision.Reasoning)}");
                        
                        foreach (var action in result.Decision.Actions)
                        {
                            var detail = action.Parameters.ContainsKey("detail")
                                ? action.Parameters["detail"]?.ToString()
                                : "";
                            Log($"  ? {action.ActionType}: {detail}");
                        }
                    }
                    else
                    {
                        LogError($"Workflow ba?ar?s?z: {result.Error?.Message}");
                    }
                }
                catch (Exception ex)
                {
                    LogError($"AI dü?ünme hatas?: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            LogError($"TriggerSingleLordThinking hatas?: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Initializes the AI system with all dependencies.
    /// </summary>
    private void InitializeAISystem()
    {
        try
        {
            Log("AI sistemi ba?lat?l?yor...");
            
            // Load configuration
            var config = AIConfiguration.Load();
            config.Validate();
            
            Log($"Provider: {config.Provider}, Model: {config.ModelId}");
            
            // Create orchestrator based on provider
            if (config.IsGroq)
            {
                _orchestrator = new GroqOrchestrator(config.ApiKey, config.ModelId, config.Temperature);
                Log("GroqOrchestrator kullan?l?yor (direct API)");
            }
            else
            {
                var kernelFactory = new KernelFactory()
                    .WithOpenAI(config.ApiKey, config.ModelId, config.OrganizationId);
                _orchestrator = kernelFactory.BuildOrchestrator();
                Log("SemanticKernelOrchestrator kullan?l?yor");
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
            Log("AI sistemi ba?ar?yla ba?lat?ld?!");
            Log("Hotkeys: NumPad1=FullTest, NumPad2=AI, NumPad3=QuickTest");
            
            // Display in-game message
            InformationManager.DisplayMessage(new InformationMessage(
                "Living in Calradia AI sistemi aktif! NumPad1/2/3 ile test edin.",
                Colors.Green));
        }
        catch (Exception ex)
        {
            LogError($"AI sistemi ba?lat?lamad?: {ex.Message}");
            InformationManager.DisplayMessage(new InformationMessage(
                $"LivingInCalradia HATA: {ex.Message}",
                Colors.Red));
        }
    }
    
    /// <summary>
    /// Processes AI agents asynchronously.
    /// </summary>
    private async Task ProcessAIAgentsAsync()
    {
        if (_workflowService == null || Campaign.Current == null)
            return;
        
        // Ba?lamadan önce kontrol
        if (IsGamePaused())
        {
            Log("Oyun duraklat?ld?, AI i?lemi iptal edildi.");
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
                // Her hero i?lemeden önce pause kontrolü
                if (IsGamePaused())
                {
                    Log("Oyun duraklat?ld?, AI döngüsü durduruluyor.");
                    return;
                }
                
                if (hero == mainHero || hero.IsDead)
                    continue;
                
                var agentId = GetAgentId(hero);
                var heroInfo = GetHeroDisplayInfo(hero);
                
                Log($"{heroInfo} dü?ünüyor...");
                
                var result = await _workflowService.ExecuteWorkflowAsync(agentId);
                
                // Async i?lem sonras? tekrar kontrol
                if (IsGamePaused())
                {
                    Log("Oyun duraklat?ld?, sonuç uygulanmayacak.");
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
                                Log($"{heroInfo} ? {action.ActionType}: {detail}");
                            }
                            else
                            {
                                Log($"{heroInfo} ? {action.ActionType}");
                            }
                        }
                    }
                    else
                    {
                        Log($"{heroInfo} beklemeye karar verdi");
                    }
                }
                else
                {
                    LogError($"{heroInfo} için workflow ba?ar?s?z: {result.Error?.Message}");
                }
                
                await Task.Delay(500);
            }
        }
        catch (Exception ex)
        {
            LogError($"AI i?leme hatas?: {ex.Message}");
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
            if (trimmed.StartsWith("DUSUNCE:", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("DÜ?ÜNCE:", StringComparison.OrdinalIgnoreCase))
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
            sb.Append("Kral ");
        else if (hero.IsLord)
            sb.Append("Lord ");
        else if (hero.IsNotable)
            sb.Append("Notable ");
        
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
            $"{ModName} HATA: {message}",
            Colors.Red));
        
        Debug.Print($"{ModName} ERROR: {message}");
    }
    
    #endregion
}
