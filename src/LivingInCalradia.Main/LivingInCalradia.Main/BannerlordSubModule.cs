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
    private const float TickIntervalSeconds = 30f; // AI dü?ünme aral???
    
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
            Log("Campaign ba?lat?l?yor, AI sistemi haz?rlan?yor...");
            
            if (gameStarter is CampaignGameStarter campaignStarter)
            {
                // Register campaign behaviors here if needed
                // campaignStarter.AddBehavior(new LivingInCalradiaCampaignBehavior());
            }
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
            
            // Display in-game message
            InformationManager.DisplayMessage(new InformationMessage(
                "Living in Calradia AI sistemi aktif!",
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
        
        try
        {
            // Get nearby lords or important NPCs
            var mainHero = Hero.MainHero;
            if (mainHero == null)
                return;
            
            // Find nearby heroes to process
            var nearbyHeroes = GetNearbyHeroes(mainHero, maxDistance: 100f);
            
            foreach (var hero in nearbyHeroes)
            {
                if (hero == mainHero || hero.IsDead)
                    continue;
                
                var agentId = GetAgentId(hero);
                
                Log($"AI dü?ünüyor: {hero.Name}");
                
                var result = await _workflowService.ExecuteWorkflowAsync(agentId);
                
                if (result.IsSuccessful)
                {
                    Log($"{hero.Name} karar verdi: {result.Decision?.Actions.Count ?? 0} aksiyon");
                }
                else
                {
                    LogError($"{hero.Name} için workflow ba?ar?s?z: {result.Error?.Message}");
                }
                
                // Rate limiting - bir hero i?lendikten sonra bekle
                await Task.Delay(500);
            }
        }
        catch (Exception ex)
        {
            LogError($"AI i?leme hatas?: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Gets heroes near the main hero.
    /// </summary>
    private System.Collections.Generic.List<Hero> GetNearbyHeroes(Hero mainHero, float maxDistance)
    {
        var nearbyHeroes = new System.Collections.Generic.List<Hero>();
        
        if (Campaign.Current?.AliveHeroes == null)
            return nearbyHeroes;
        
        foreach (var hero in Campaign.Current.AliveHeroes)
        {
            if (hero == mainHero || hero.IsDead)
                continue;
            
            // Sadece lordlar? ve önemli NPC'leri i?le
            if (!hero.IsLord && !hero.IsNotable)
                continue;
            
            // Mesafe kontrolü (basitle?tirilmi?)
            if (hero.CurrentSettlement == mainHero.CurrentSettlement)
            {
                nearbyHeroes.Add(hero);
            }
            
            // Maksimum 3 hero i?le (performans için)
            if (nearbyHeroes.Count >= 3)
                break;
        }
        
        return nearbyHeroes;
    }
    
    /// <summary>
    /// Creates a unique agent ID for a hero.
    /// </summary>
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
        
        // Also log to debug
        Debug.Print($"{ModName} {message}");
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
