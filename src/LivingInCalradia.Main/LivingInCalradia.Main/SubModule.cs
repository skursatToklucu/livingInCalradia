using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using LivingInCalradia.Infrastructure.Bannerlord;

namespace LivingInCalradia.Main;

/// <summary>
/// Bannerlord SubModule entry point.
/// This class is loaded by Bannerlord's mod system.
/// </summary>
public class SubModule : MBSubModuleBase
{
    private LivingInCalradiaSubModule? _aiSystem;
    private float _lastThinkTime;
    private float _lastKeyCheckTime;
    private const float ThinkIntervalSeconds = 60f;
    private const float KeyCheckIntervalSeconds = 0.1f;
    
    protected override void OnSubModuleLoad()
    {
        base.OnSubModuleLoad();
        
        InformationManager.DisplayMessage(new InformationMessage(
            "[Living in Calradia] Mod yükleniyor...", 
            Colors.Cyan));
    }
    
    protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
    {
        base.OnGameStart(game, gameStarterObject);
        
        if (game.GameType is Campaign)
        {
            var campaignStarter = (CampaignGameStarter)gameStarterObject;
            
            // AI behavior'? ekle
            campaignStarter.AddBehavior(new LivingInCalradiaCampaignBehavior(this));
            
            InformationManager.DisplayMessage(new InformationMessage(
                "[Living in Calradia] AI Behavior eklendi!", 
                Colors.Green));
        }
    }
    
    protected override void OnBeforeInitialModuleScreenSetAsRoot()
    {
        base.OnBeforeInitialModuleScreenSetAsRoot();
        
        InformationManager.DisplayMessage(new InformationMessage(
            "[Living in Calradia] v1.0 - AI NPCs | NumPad1=Test, NumPad2=AI", 
            Colors.Magenta));
    }
    
    public void InitializeAISystem()
    {
        try
        {
            _aiSystem = new LivingInCalradiaSubModule();
            _aiSystem.Initialize();
            
            InformationManager.DisplayMessage(new InformationMessage(
                "[Living in Calradia] AI sistemi haz?r! NumPad1=Test, NumPad2=AI Dü?ün", 
                Colors.Green));
        }
        catch (Exception ex)
        {
            InformationManager.DisplayMessage(new InformationMessage(
                $"[Living in Calradia] AI Hata: {ex.Message}", 
                Colors.Red));
        }
    }
    
    public void OnTick(float dt)
    {
        _lastKeyCheckTime += dt;
        
        // Key check interval
        if (_lastKeyCheckTime >= KeyCheckIntervalSeconds)
        {
            _lastKeyCheckTime = 0f;
            CheckHotkeys();
        }
    }
    
    private void CheckHotkeys()
    {
        try
        {
            // NumPad1 = Full Proof Test
            if (TaleWorlds.InputSystem.Input.IsKeyPressed(TaleWorlds.InputSystem.InputKey.Numpad1))
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "[Living in Calradia] Full Proof Test ba?lat?l?yor...", 
                    Colors.Cyan));
                
                BannerlordActionExecutor.RunFullAIProofTest();
            }
            
            // NumPad2 = AI Dü?ünme
            if (TaleWorlds.InputSystem.Input.IsKeyPressed(TaleWorlds.InputSystem.InputKey.Numpad2))
            {
                TriggerRandomLordThinking();
            }
            
            // NumPad3 = H?zl? test
            if (TaleWorlds.InputSystem.Input.IsKeyPressed(TaleWorlds.InputSystem.InputKey.Numpad3))
            {
                BannerlordActionExecutor.RunProofTest();
            }
        }
        catch
        {
            // Input system might not be available
        }
    }
    
    private void TriggerRandomLordThinking()
    {
        if (_aiSystem == null)
        {
            InformationManager.DisplayMessage(new InformationMessage(
                "[Living in Calradia] AI sistemi henüz haz?r de?il!", 
                Colors.Red));
            return;
        }
        
        try
        {
            var lords = Campaign.Current?.AliveHeroes;
            if (lords == null) return;
            
            Hero? selectedLord = null;
            foreach (var hero in lords)
            {
                if (hero.IsLord && hero.Clan?.Kingdom != null && hero != Hero.MainHero)
                {
                    selectedLord = hero;
                    break;
                }
            }
            
            if (selectedLord == null)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "[Living in Calradia] Dü?ünecek lord bulunamad?!", 
                    Colors.Red));
                return;
            }
            
            var kingdomName = selectedLord.Clan?.Kingdom?.Name?.ToString() ?? "NoKingdom";
            var agentId = $"Lord_{selectedLord.Name}_{kingdomName}";
            
            InformationManager.DisplayMessage(new InformationMessage(
                $"[Living in Calradia] {selectedLord.Name} dü?ünüyor...", 
                Colors.Yellow));
            
            _ = _aiSystem.ExecuteAgentThinkingAsync(agentId);
        }
        catch (Exception ex)
        {
            InformationManager.DisplayMessage(new InformationMessage(
                $"[Living in Calradia] Hata: {ex.Message}", 
                Colors.Red));
        }
    }
}

/// <summary>
/// Campaign behavior for AI system integration.
/// </summary>
public class LivingInCalradiaCampaignBehavior : CampaignBehaviorBase
{
    private readonly SubModule _subModule;
    
    public LivingInCalradiaCampaignBehavior(SubModule subModule)
    {
        _subModule = subModule;
    }
    
    public override void RegisterEvents()
    {
        CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
    }
    
    public override void SyncData(IDataStore dataStore)
    {
        // No save/load needed
    }
    
    private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
    {
        _subModule.InitializeAISystem();
    }
    
    private void OnTick(float dt)
    {
        _subModule.OnTick(dt);
    }
}
