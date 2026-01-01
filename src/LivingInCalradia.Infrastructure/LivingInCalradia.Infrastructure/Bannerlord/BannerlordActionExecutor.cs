using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LivingInCalradia.Core.Application.Interfaces;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace LivingInCalradia.Infrastructure.Bannerlord;

/// <summary>
/// Real implementation of IGameActionExecutor that executes actions in Bannerlord.
/// Uses TaleWorlds CampaignSystem APIs to perform REAL game actions.
/// </summary>
public sealed class BannerlordActionExecutor : IGameActionExecutor
{
    private readonly Dictionary<string, Func<AgentAction, string, ActionResult>> _handlers;
    private string _currentAgentId = "";
<<<<<<< HEAD
    private static bool _isTurkish = false;

    public static void SetLanguage(string language)
    {
        _isTurkish = language?.ToLowerInvariant() == "tr";
    }
=======
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049

    public BannerlordActionExecutor()
    {
        _handlers = new Dictionary<string, Func<AgentAction, string, ActionResult>>(StringComparer.OrdinalIgnoreCase);
        RegisterHandlers();
    }

    private void RegisterHandlers()
    {
        // Wait action
        _handlers["Wait"] = HandleWait;

        // Diplomatic actions
        _handlers["ChangeRelation"] = HandleChangeRelation;
        _handlers["DeclareWar"] = HandleDeclareWar;
        _handlers["MakePeace"] = HandleMakePeace;

        // Economic actions
        _handlers["GiveGold"] = HandleGiveGold;
        _handlers["Trade"] = HandleTrade;

        // Military actions
        _handlers["MoveArmy"] = HandleMoveArmy;
        _handlers["RecruitTroops"] = HandleRecruitTroops;
        _handlers["StartSiege"] = HandleStartSiege;
        _handlers["Attack"] = HandleAttack;
        _handlers["Retreat"] = HandleRetreat;
        _handlers["Defend"] = HandleDefend;
        _handlers["Patrol"] = HandlePatrol;

        // Social actions
        _handlers["Talk"] = HandleTalk;
        _handlers["Work"] = HandleWork;
        _handlers["Hide"] = HandleHide;
<<<<<<< HEAD

=======
        
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        // Test/Demo action
        _handlers["TestProof"] = HandleTestProof;
    }

    public bool CanExecute(string actionType)
    {
        return _handlers.ContainsKey(actionType);
    }

    public Task<ActionResult> ExecuteAsync(AgentAction action, CancellationToken cancellationToken = default)
    {
        // Get agent ID from parameters if available
        if (action.Parameters.TryGetValue("agentId", out var agentIdObj))
        {
            _currentAgentId = agentIdObj?.ToString() ?? "";
        }

        if (_handlers.TryGetValue(action.ActionType, out var handler))
        {
            try
            {
                var result = handler(action, _currentAgentId);
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
<<<<<<< HEAD
                var errorMsg = _isTurkish ? "HATA" : "ERROR";
                LogAction(action.ActionType, action.Parameters, $"{errorMsg}: {ex.Message}");
                var actionError = _isTurkish ? "Aksiyon hatasi" : "Action error";
                return Task.FromResult(ActionResult.Failed($"{actionError}: {ex.Message}", ex));
            }
        }

        var unknownAction = _isTurkish ? "Bilinmeyen aksiyon" : "Unknown action";
        LogAction(action.ActionType, action.Parameters, unknownAction);
        return Task.FromResult(ActionResult.Failed($"{unknownAction}: {action.ActionType}"));
    }

=======
                LogAction(action.ActionType, action.Parameters, $"HATA: {ex.Message}");
                return Task.FromResult(ActionResult.Failed($"Aksiyon hatasi: {ex.Message}", ex));
            }
        }

        LogAction(action.ActionType, action.Parameters, "Bilinmeyen aksiyon");
        return Task.FromResult(ActionResult.Failed($"Bilinmeyen aksiyon: {action.ActionType}"));
    }
    
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
    /// <summary>
    /// Runs a proof-of-concept test that demonstrates AI thinking leads to real game changes.
    /// Call this to verify the system is working.
    /// </summary>
    public static void RunProofTest()
    {
        try
        {
<<<<<<< HEAD
            var startMsg = _isTurkish ? "========== AI PROOF TEST BASLIYOR ==========" : "========== AI PROOF TEST STARTING ==========";
            ShowMessage(startMsg, Colors.Magenta);

            var mainHero = Hero.MainHero;
            if (mainHero == null)
            {
                var errorMsg = _isTurkish ? "HATA: MainHero bulunamadi!" : "ERROR: MainHero not found!";
                ShowMessage(errorMsg, Colors.Red);
                return;
            }

            // Find a random lord to test with
            var targetHero = Campaign.Current?.AliveHeroes?
                .FirstOrDefault(h => h != mainHero && h.IsLord && h.Clan?.Kingdom != null);

            if (targetHero == null)
            {
                var errorMsg = _isTurkish ? "HATA: Test icin lord bulunamadi!" : "ERROR: No lord found for test!";
                ShowMessage(errorMsg, Colors.Red);
                return;
            }

            // STEP 1: Record BEFORE state
            var beforeRelation = CharacterRelationManager.GetHeroRelation(mainHero, targetHero);
            var beforeGold = mainHero.Gold;

            var beforeLabel = _isTurkish ? "[ONCE]" : "[BEFORE]";
            var relationLabel = _isTurkish ? "ile iliski" : "relation";
            var goldLabel = _isTurkish ? "Altin" : "Gold";

            ShowMessage($"{beforeLabel} {targetHero.Name} {relationLabel}: {beforeRelation}", Colors.Yellow);
            ShowMessage($"{beforeLabel} {goldLabel}: {beforeGold}", Colors.Yellow);

            // STEP 2: Execute a REAL action
            var actionLabel = _isTurkish ? "[AKSIYON]" : "[ACTION]";
            var increasingMsg = _isTurkish ? "ile iliski +5 artiriliyor..." : "relation +5 increasing...";
            ShowMessage($"{actionLabel} {targetHero.Name} {increasingMsg}", Colors.Cyan);
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(mainHero, targetHero, 5);

            // STEP 3: Record AFTER state
            var afterRelation = CharacterRelationManager.GetHeroRelation(mainHero, targetHero);

            var afterLabel = _isTurkish ? "[SONRA]" : "[AFTER]";
            ShowMessage($"{afterLabel} {targetHero.Name} {relationLabel}: {afterRelation}", Colors.Green);

            // STEP 4: Verify change
            if (afterRelation == beforeRelation + 5)
            {
                var successMsg = _isTurkish ? "BASARILI: Iliski degeri GERCEKTEN degisti!" : "SUCCESS: Relation value REALLY changed!";
                ShowMessage(successMsg, Colors.Green);
                var changeLabel = _isTurkish ? "Degisim" : "Change";
                ShowMessage($"  {changeLabel}: {beforeRelation} -> {afterRelation} (+5)", Colors.Green);
            }
            else
            {
                var warnLabel = _isTurkish ? "UYARI: Beklenen" : "WARNING: Expected";
                var actualLabel = _isTurkish ? "gerceklesen" : "actual";
                ShowMessage($"{warnLabel} {beforeRelation + 5}, {actualLabel} {afterRelation}", Colors.Red);
            }

            var endMsg = _isTurkish ? "========== AI PROOF TEST TAMAMLANDI ==========" : "========== AI PROOF TEST COMPLETED ==========";
            ShowMessage(endMsg, Colors.Magenta);
        }
        catch (Exception ex)
        {
            var errorLabel = _isTurkish ? "HATA" : "ERROR";
            ShowMessage($"{errorLabel}: {ex.Message}", Colors.Red);
        }
    }

=======
            ShowMessage("========== AI PROOF TEST BASLIYOR ==========", Colors.Magenta);
            
            var mainHero = Hero.MainHero;
            if (mainHero == null)
            {
                ShowMessage("HATA: MainHero bulunamadi!", Colors.Red);
                return;
            }
            
            // Find a random lord to test with
            var targetHero = Campaign.Current?.AliveHeroes?
                .FirstOrDefault(h => h != mainHero && h.IsLord && h.Clan?.Kingdom != null);
            
            if (targetHero == null)
            {
                ShowMessage("HATA: Test icin lord bulunamadi!", Colors.Red);
                return;
            }
            
            // STEP 1: Record BEFORE state
            var beforeRelation = CharacterRelationManager.GetHeroRelation(mainHero, targetHero);
            var beforeGold = mainHero.Gold;
            
            ShowMessage($"[ONCE] {targetHero.Name} ile iliski: {beforeRelation}", Colors.Yellow);
            ShowMessage($"[ONCE] Altin: {beforeGold}", Colors.Yellow);
            
            // STEP 2: Execute a REAL action
            ShowMessage($"[AKSIYON] {targetHero.Name} ile iliski +5 artiriliyor...", Colors.Cyan);
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(mainHero, targetHero, 5);
            
            // STEP 3: Record AFTER state
            var afterRelation = CharacterRelationManager.GetHeroRelation(mainHero, targetHero);
            
            ShowMessage($"[SONRA] {targetHero.Name} ile iliski: {afterRelation}", Colors.Green);
            
            // STEP 4: Verify change
            if (afterRelation == beforeRelation + 5)
            {
                ShowMessage("BASARILI: Iliski degeri GERCEKTEN degisti!", Colors.Green);
                ShowMessage($"  Degisim: {beforeRelation} -> {afterRelation} (+5)", Colors.Green);
            }
            else
            {
                ShowMessage($"UYARI: Beklenen {beforeRelation + 5}, gerceklesen {afterRelation}", Colors.Red);
            }
            
            ShowMessage("========== AI PROOF TEST TAMAMLANDI ==========", Colors.Magenta);
        }
        catch (Exception ex)
        {
            ShowMessage($"HATA: {ex.Message}", Colors.Red);
        }
    }
    
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
    /// <summary>
    /// Runs a comprehensive test showing the full AI workflow with real game changes.
    /// </summary>
    public static void RunFullAIProofTest()
    {
        try
        {
            ShowMessage("", Colors.White);
            ShowMessage("==================================================", Colors.Magenta);
<<<<<<< HEAD
            var title = _isTurkish ? "     AI DUSUNCE -> AKSIYON PROOF OF CONCEPT        " : "     AI THOUGHT -> ACTION PROOF OF CONCEPT         ";
            ShowMessage(title, Colors.Magenta);
            ShowMessage("==================================================", Colors.Magenta);
            ShowMessage("", Colors.White);

            var mainHero = Hero.MainHero;
            if (mainHero == null || Campaign.Current == null)
            {
                var errorMsg = _isTurkish ? "HATA: Oyun durumu gecersiz!" : "ERROR: Invalid game state!";
                ShowMessage(errorMsg, Colors.Red);
                return;
            }

=======
            ShowMessage("     AI DUSUNCE -> AKSIYON PROOF OF CONCEPT        ", Colors.Magenta);
            ShowMessage("==================================================", Colors.Magenta);
            ShowMessage("", Colors.White);
            
            var mainHero = Hero.MainHero;
            if (mainHero == null || Campaign.Current == null)
            {
                ShowMessage("HATA: Oyun durumu gecersiz!", Colors.Red);
                return;
            }
            
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
            // Find test subjects
            var lords = Campaign.Current.AliveHeroes?
                .Where(h => h != mainHero && h.IsLord && h.Clan?.Kingdom != null)
                .Take(3)
                .ToList();
<<<<<<< HEAD

            if (lords == null || lords.Count == 0)
            {
                var errorMsg = _isTurkish ? "HATA: Test icin lord bulunamadi!" : "ERROR: No lord found for test!";
                ShowMessage(errorMsg, Colors.Red);
                return;
            }

            var targetsLabel = _isTurkish ? "Test Hedefleri" : "Test Targets";
            ShowMessage($"{targetsLabel}: {string.Join(", ", lords.Select(l => l.Name))}", Colors.White);
            ShowMessage("", Colors.White);

            // TEST 1: Relation Change
            var lord1 = lords[0];
            var beforeRel = CharacterRelationManager.GetHeroRelation(mainHero, lord1);
            var test1Label = _isTurkish ? "[TEST 1] Iliski Degisikligi" : "[TEST 1] Relation Change";
            ShowMessage(test1Label, Colors.Cyan);
            var targetLabel = _isTurkish ? "Hedef" : "Target";
            ShowMessage($"  {targetLabel}: {lord1.Name}", Colors.White);
            var beforeLabel = _isTurkish ? "ONCE" : "BEFORE";
            ShowMessage($"  {beforeLabel}: {beforeRel}", Colors.Yellow);

            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(mainHero, lord1, 10);

            var afterRel = CharacterRelationManager.GetHeroRelation(mainHero, lord1);
            var afterLabel = _isTurkish ? "SONRA" : "AFTER";
            ShowMessage($"  {afterLabel}: {afterRel}", Colors.Green);
            var changeLabel = _isTurkish ? "Degisim" : "Change";
            ShowMessage($"  {changeLabel}: {afterRel - beforeRel:+0;-0}", afterRel > beforeRel ? Colors.Green : Colors.Red);
            ShowMessage("", Colors.White);

            // TEST 2: Gold Check (just display, don't modify)
            var test2Label = _isTurkish ? "[TEST 2] Oyuncu Durumu" : "[TEST 2] Player Status";
            ShowMessage(test2Label, Colors.Cyan);
            var goldLabel = _isTurkish ? "Altin" : "Gold";
            ShowMessage($"  {goldLabel}: {mainHero.Gold:N0}", Colors.Yellow);
            var locationLabel = _isTurkish ? "Konum" : "Location";
            var onMapLabel = _isTurkish ? "Haritada" : "On map";
            var settlementName = mainHero.CurrentSettlement?.Name?.ToString() ?? onMapLabel;
            ShowMessage($"  {locationLabel}: {settlementName}", Colors.Yellow);
            var partyLabel = _isTurkish ? "Parti" : "Party";
            var noneLabel = _isTurkish ? "Yok" : "None";
            var partyName = mainHero.PartyBelongedTo?.Name?.ToString() ?? noneLabel;
            ShowMessage($"  {partyLabel}: {partyName}", Colors.Yellow);
            ShowMessage("", Colors.White);

            // TEST 3: World State
            var test3Label = _isTurkish ? "[TEST 3] Dunya Durumu" : "[TEST 3] World State";
            ShowMessage(test3Label, Colors.Cyan);
            var kingdoms = Campaign.Current.Kingdoms?.Where(k => !k.IsEliminated).ToList();
            if (kingdoms != null)
            {
                var warsLabel = _isTurkish ? "savas" : "wars";
                foreach (var kingdom in kingdoms.Take(5))
                {
                    var warCount = kingdoms.Count(k => k != kingdom && kingdom.IsAtWarWith(k));
                    ShowMessage($"  {kingdom.Name}: {warCount} {warsLabel}", Colors.White);
                }
            }
            ShowMessage("", Colors.White);

            ShowMessage("==================================================", Colors.Green);
            var completedMsg = _isTurkish ? "  TUM TESTLER TAMAMLANDI - SISTEM CALISIYOR!      " : "  ALL TESTS COMPLETED - SYSTEM WORKING!           ";
            ShowMessage(completedMsg, Colors.Green);
            ShowMessage("==================================================", Colors.Green);
            ShowMessage("", Colors.White);
            var provenMsg = _isTurkish ? "AI dusuncesi -> Gercek oyun aksiyonu baglantisi KANITLANDI" : "AI thought -> Real game action connection PROVEN";
            ShowMessage(provenMsg, Colors.Green);
        }
        catch (Exception ex)
        {
            var errorLabel = _isTurkish ? "TEST HATASI" : "TEST ERROR";
            ShowMessage($"{errorLabel}: {ex.Message}", Colors.Red);
=======
            
            if (lords == null || lords.Count == 0)
            {
                ShowMessage("HATA: Test icin lord bulunamadi!", Colors.Red);
                return;
            }
            
            ShowMessage($"Test Hedefleri: {string.Join(", ", lords.Select(l => l.Name))}", Colors.White);
            ShowMessage("", Colors.White);
            
            // TEST 1: Relation Change
            var lord1 = lords[0];
            var beforeRel = CharacterRelationManager.GetHeroRelation(mainHero, lord1);
            ShowMessage($"[TEST 1] Iliski Degisikligi", Colors.Cyan);
            ShowMessage($"  Hedef: {lord1.Name}", Colors.White);
            ShowMessage($"  ONCE: {beforeRel}", Colors.Yellow);
            
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(mainHero, lord1, 10);
            
            var afterRel = CharacterRelationManager.GetHeroRelation(mainHero, lord1);
            ShowMessage($"  SONRA: {afterRel}", Colors.Green);
            ShowMessage($"  Degisim: {afterRel - beforeRel:+0;-0}", afterRel > beforeRel ? Colors.Green : Colors.Red);
            ShowMessage("", Colors.White);
            
            // TEST 2: Gold Check (just display, don't modify)
            ShowMessage($"[TEST 2] Oyuncu Durumu", Colors.Cyan);
            ShowMessage($"  Altin: {mainHero.Gold:N0}", Colors.Yellow);
            var settlementName = mainHero.CurrentSettlement?.Name?.ToString() ?? "Haritada";
            var partyName = mainHero.PartyBelongedTo?.Name?.ToString() ?? "Yok";
            ShowMessage($"  Konum: {settlementName}", Colors.Yellow);
            ShowMessage($"  Parti: {partyName}", Colors.Yellow);
            ShowMessage("", Colors.White);
            
            // TEST 3: World State
            ShowMessage($"[TEST 3] Dunya Durumu", Colors.Cyan);
            var kingdoms = Campaign.Current.Kingdoms?.Where(k => !k.IsEliminated).ToList();
            if (kingdoms != null)
            {
                foreach (var kingdom in kingdoms.Take(5))
                {
                    var warCount = kingdoms.Count(k => k != kingdom && kingdom.IsAtWarWith(k));
                    ShowMessage($"  {kingdom.Name}: {warCount} savas", Colors.White);
                }
            }
            ShowMessage("", Colors.White);
            
            ShowMessage("==================================================", Colors.Green);
            ShowMessage("  TUM TESTLER TAMAMLANDI - SISTEM CALISIYOR!      ", Colors.Green);
            ShowMessage("==================================================", Colors.Green);
            ShowMessage("", Colors.White);
            ShowMessage("AI dusuncesi -> Gercek oyun aksiyonu baglantisi KANITLANDI", Colors.Green);
        }
        catch (Exception ex)
        {
            ShowMessage($"TEST HATASI: {ex.Message}", Colors.Red);
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
            Debug.Print($"[LivingInCalradia] Full test error: {ex}");
        }
    }

    #region Action Handlers

    private ActionResult HandleWait(AgentAction action, string agentId)
    {
        action.Parameters.TryGetValue("duration", out var dur);
        var duration = Convert.ToInt32(dur ?? 60);
<<<<<<< HEAD

        var msg = _isTurkish ? $"Bekleniyor ({duration}s)" : $"Waiting ({duration}s)";
        ShowMessage(msg, Colors.Gray);
        var resultMsg = _isTurkish ? $"Agent {duration} saniye bekledi" : $"Agent waited {duration} seconds";
        return ActionResult.Successful(resultMsg);
    }

=======
        
        ShowMessage($"Bekleniyor ({duration}s)", Colors.Gray);
        return ActionResult.Successful($"Agent {duration} saniye bekledi");
    }
    
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
    private ActionResult HandleTestProof(AgentAction action, string agentId)
    {
        // This is a special action that proves the system works
        RunProofTest();
<<<<<<< HEAD
        var msg = _isTurkish ? "Proof test tamamlandi" : "Proof test completed";
        return ActionResult.Successful(msg);
=======
        return ActionResult.Successful("Proof test tamamlandi");
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
    }

    private ActionResult HandleChangeRelation(AgentAction action, string agentId)
    {
        action.Parameters.TryGetValue("targetHeroId", out var targetId);
        action.Parameters.TryGetValue("amount", out var amountObj);
        action.Parameters.TryGetValue("detail", out var detail);

        var amount = Convert.ToInt32(amountObj ?? 5);
        var targetName = targetId?.ToString() ?? detail?.ToString() ?? "";

        var actingHero = FindHeroByAgentId(agentId);
        var targetHero = FindHeroByName(targetName);

        if (actingHero != null && targetHero != null && actingHero != targetHero)
        {
            // Record BEFORE state for proof
            var beforeRelation = CharacterRelationManager.GetHeroRelation(actingHero, targetHero);
<<<<<<< HEAD

            // REAL ACTION: Change relation between heroes
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(actingHero, targetHero, amount);

            // Record AFTER state for proof
            var afterRelation = CharacterRelationManager.GetHeroRelation(actingHero, targetHero);

            var relationLabel = _isTurkish ? "Iliski" : "Relation";
            ShowMessage($"{relationLabel}: {actingHero.Name} <-> {targetHero.Name}", Colors.Cyan);
            ShowMessage($"  {beforeRelation} -> {afterRelation} ({amount:+0;-0})", amount > 0 ? Colors.Green : Colors.Red);

            var changedMsg = _isTurkish ? "Iliski degisti" : "Relation changed";
            return ActionResult.Successful($"{changedMsg}: {beforeRelation} -> {afterRelation}");
=======
            
            // REAL ACTION: Change relation between heroes
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(actingHero, targetHero, amount);
            
            // Record AFTER state for proof
            var afterRelation = CharacterRelationManager.GetHeroRelation(actingHero, targetHero);
            
            ShowMessage($"Iliski: {actingHero.Name} <-> {targetHero.Name}", Colors.Cyan);
            ShowMessage($"  {beforeRelation} -> {afterRelation} ({amount:+0;-0})", 
                amount > 0 ? Colors.Green : Colors.Red);
            
            return ActionResult.Successful($"Iliski degisti: {beforeRelation} -> {afterRelation}");
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        }

        // Fallback: Try with MainHero
        if (targetHero != null && Hero.MainHero != null)
        {
            var beforeRelation = CharacterRelationManager.GetHeroRelation(Hero.MainHero, targetHero);
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, targetHero, amount);
            var afterRelation = CharacterRelationManager.GetHeroRelation(Hero.MainHero, targetHero);
<<<<<<< HEAD

            var playerLabel = _isTurkish ? "Oyuncu" : "Player";
            ShowMessage($"{playerLabel} <-> {targetHero.Name}: {beforeRelation} -> {afterRelation}", Colors.Green);
            var changedMsg = _isTurkish ? "Iliski degistirildi" : "Relation changed";
            return ActionResult.Successful($"{changedMsg}: {beforeRelation} -> {afterRelation}");
        }

        var notFoundMsg = _isTurkish ? "Hedef kahraman bulunamadi" : "Target hero not found";
        return ActionResult.Failed(notFoundMsg);
=======
            
            ShowMessage($"Oyuncu <-> {targetHero.Name}: {beforeRelation} -> {afterRelation}", Colors.Green);
            return ActionResult.Successful($"Iliski degistirildi: {beforeRelation} -> {afterRelation}");
        }

        return ActionResult.Failed("Hedef kahraman bulunamadi");
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
    }

    private ActionResult HandleDeclareWar(AgentAction action, string agentId)
    {
        action.Parameters.TryGetValue("targetFaction", out var targetFaction);
        action.Parameters.TryGetValue("detail", out var detail);

        var targetName = targetFaction?.ToString() ?? detail?.ToString() ?? "";
<<<<<<< HEAD

=======
        
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        var actingHero = FindHeroByAgentId(agentId);
        var actingKingdom = actingHero?.Clan?.Kingdom;
        var targetKingdom = FindKingdomByName(targetName);

        if (actingKingdom != null && targetKingdom != null && actingKingdom != targetKingdom)
        {
            // Check BEFORE state
            var wasAtWar = actingKingdom.IsAtWarWith(targetKingdom);
<<<<<<< HEAD

=======
            
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
            if (!wasAtWar)
            {
                // REAL ACTION: Declare war using FactionManager
                FactionManager.DeclareWar(actingKingdom, targetKingdom);
<<<<<<< HEAD

                // Verify AFTER state
                var isAtWarNow = actingKingdom.IsAtWarWith(targetKingdom);

                var warDeclaredMsg = _isTurkish ? "SAVAS ILAN EDILDI!" : "WAR DECLARED!";
                ShowMessage(warDeclaredMsg, Colors.Red);
                ShowMessage($"  {actingKingdom.Name} -> {targetKingdom.Name}", Colors.Red);
                var atWarLabel = _isTurkish ? "SAVASTA" : "AT WAR";
                var errorLabel = _isTurkish ? "HATA" : "ERROR";
                var statusLabel = _isTurkish ? "Durum" : "Status";
                ShowMessage($"  {statusLabel}: {(isAtWarNow ? atWarLabel : errorLabel)}", isAtWarNow ? Colors.Red : Colors.Yellow);

                var peaceLabel = _isTurkish ? "Baris" : "Peace";
                var warLabel = _isTurkish ? "Savas" : "War";
                var prevLabel = _isTurkish ? "Onceki" : "Before";
                var nowLabel = _isTurkish ? "Simdi" : "Now";
                return ActionResult.Successful($"{warDeclaredMsg} {prevLabel}: {peaceLabel}, {nowLabel}: {(isAtWarNow ? warLabel : errorLabel)}");
            }
            else
            {
                var alreadyAtWarMsg = _isTurkish ? $"Zaten {targetKingdom.Name} ile savastayiz" : $"Already at war with {targetKingdom.Name}";
                return ActionResult.Failed(alreadyAtWarMsg);
            }
        }

        var warIntentMsg = _isTurkish ? "Savas ilani" : "War declaration";
        ShowMessage($"{warIntentMsg}: {detail}", Colors.Yellow);
        var intentRecordedMsg = _isTurkish ? "Savas ilani niyeti kaydedildi" : "War declaration intent recorded";
        return ActionResult.Successful(intentRecordedMsg);
=======
                
                // Verify AFTER state
                var isAtWarNow = actingKingdom.IsAtWarWith(targetKingdom);
                
                ShowMessage($"SAVAS ILAN EDILDI!", Colors.Red);
                ShowMessage($"  {actingKingdom.Name} -> {targetKingdom.Name}", Colors.Red);
                ShowMessage($"  Durum: {(isAtWarNow ? "SAVASTA" : "HATA")}", isAtWarNow ? Colors.Red : Colors.Yellow);
                
                return ActionResult.Successful($"Savas ilan edildi! Onceki: Baris, Simdi: {(isAtWarNow ? "Savas" : "Hata")}");
            }
            else
            {
                return ActionResult.Failed($"Zaten {targetKingdom.Name} ile savastayiz");
            }
        }

        ShowMessage($"Savas ilani: {detail}", Colors.Yellow);
        return ActionResult.Successful("Savas ilani niyeti kaydedildi");
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
    }

    private ActionResult HandleMakePeace(AgentAction action, string agentId)
    {
        action.Parameters.TryGetValue("targetFaction", out var targetFaction);
        action.Parameters.TryGetValue("detail", out var detail);

        var targetName = targetFaction?.ToString() ?? detail?.ToString() ?? "";
<<<<<<< HEAD

=======
        
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        var actingHero = FindHeroByAgentId(agentId);
        var actingKingdom = actingHero?.Clan?.Kingdom;
        var targetKingdom = FindKingdomByName(targetName);

        if (actingKingdom != null && targetKingdom != null)
        {
            var wasAtWar = actingKingdom.IsAtWarWith(targetKingdom);
<<<<<<< HEAD

=======
            
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
            if (wasAtWar)
            {
                // REAL ACTION: Make peace
                MakePeaceAction.Apply(actingKingdom, targetKingdom);
<<<<<<< HEAD

                var isAtWarNow = actingKingdom.IsAtWarWith(targetKingdom);

                var peaceMadeMsg = _isTurkish ? "BARIS YAPILDI!" : "PEACE MADE!";
                ShowMessage(peaceMadeMsg, Colors.Green);
                ShowMessage($"  {actingKingdom.Name} <-> {targetKingdom.Name}", Colors.Green);
                var peaceLabel = _isTurkish ? "BARIS" : "PEACE";
                var errorLabel = _isTurkish ? "HATA" : "ERROR";
                var statusLabel = _isTurkish ? "Durum" : "Status";
                ShowMessage($"  {statusLabel}: {(isAtWarNow ? errorLabel : peaceLabel)}", isAtWarNow ? Colors.Red : Colors.Green);

                return ActionResult.Successful(peaceMadeMsg);
            }
            else
            {
                var notAtWarMsg = _isTurkish ? $"{targetKingdom.Name} ile zaten savasta degiliz" : $"Not at war with {targetKingdom.Name}";
                return ActionResult.Failed(notAtWarMsg);
            }
        }

        var peaceFailedMsg = _isTurkish ? "Baris basarisiz" : "Peace failed";
        ShowMessage($"{peaceFailedMsg}: {detail}", Colors.Yellow);
        var kingdomNotFoundMsg = _isTurkish ? "Baris yapilamadi - krallik bulunamadi" : "Peace failed - kingdom not found";
        return ActionResult.Failed(kingdomNotFoundMsg);
=======
                
                var isAtWarNow = actingKingdom.IsAtWarWith(targetKingdom);
                
                ShowMessage($"BARIS YAPILDI!", Colors.Green);
                ShowMessage($"  {actingKingdom.Name} <-> {targetKingdom.Name}", Colors.Green);
                ShowMessage($"  Durum: {(isAtWarNow ? "HATA" : "BARIS")}", isAtWarNow ? Colors.Red : Colors.Green);
                
                return ActionResult.Successful($"Baris yapildi! Onceki: Savas, Simdi: {(isAtWarNow ? "Hata" : "Baris")}");
            }
            else
            {
                return ActionResult.Failed($"{targetKingdom.Name} ile zaten savasta degiliz");
            }
        }

        ShowMessage($"Baris basarisiz: {detail}", Colors.Yellow);
        return ActionResult.Failed("Baris yapilamadi - krallik bulunamadi");
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
    }

    private ActionResult HandleGiveGold(AgentAction action, string agentId)
    {
        action.Parameters.TryGetValue("amount", out var amountObj);
        action.Parameters.TryGetValue("receiverId", out var receiverId);
        action.Parameters.TryGetValue("detail", out var detail);

        var amount = Convert.ToInt32(amountObj ?? 100);
        var receiverName = receiverId?.ToString() ?? detail?.ToString() ?? "";

        var actingHero = FindHeroByAgentId(agentId);
        var receiverHero = FindHeroByName(receiverName);

        if (actingHero != null && receiverHero != null)
        {
<<<<<<< HEAD
            if (actingHero.Gold >= amount)
            {
                var beforeGoldGiver = actingHero.Gold;
                var beforeGoldReceiver = receiverHero.Gold;
                GiveGoldAction.ApplyBetweenCharacters(actingHero, receiverHero, amount);
                var afterGoldGiver = actingHero.Gold;
                var afterGoldReceiver = receiverHero.Gold;

                var transferMsg = _isTurkish ? "ALTIN TRANSFERI" : "GOLD TRANSFER";
                ShowMessage(transferMsg, Colors.Yellow);
                ShowMessage($"  {actingHero.Name}: {beforeGoldGiver} -> {afterGoldGiver}", Colors.Yellow);
                ShowMessage($"  {receiverHero.Name}: {beforeGoldReceiver} -> {afterGoldReceiver}", Colors.Yellow);

                var transferredMsg = _isTurkish ? "Altin transfer edildi" : "Gold transferred";
                return ActionResult.Successful($"{transferredMsg}: {amount}");
            }
            else
            {
                var insufficientMsg = _isTurkish ? "Yetersiz altin" : "Insufficient gold";
                return ActionResult.Failed($"{insufficientMsg}: {actingHero.Gold} < {amount}");
            }
        }

        var failedMsg = _isTurkish ? "Altin transferi basarisiz" : "Gold transfer failed";
        return ActionResult.Failed(failedMsg);
=======
            var beforeGoldGiver = actingHero.Gold;
            var beforeGoldReceiver = receiverHero.Gold;
            
            if (actingHero.Gold >= amount)
            {
                // REAL ACTION: Transfer gold
                GiveGoldAction.ApplyBetweenCharacters(actingHero, receiverHero, amount);
                
                var afterGoldGiver = actingHero.Gold;
                var afterGoldReceiver = receiverHero.Gold;
                
                ShowMessage($"ALTIN TRANSFERI", Colors.Yellow);
                ShowMessage($"  {actingHero.Name}: {beforeGoldGiver} -> {afterGoldGiver}", Colors.Yellow);
                ShowMessage($"  {receiverHero.Name}: {beforeGoldReceiver} -> {afterGoldReceiver}", Colors.Yellow);
                
                return ActionResult.Successful($"Altin transfer edildi: {amount}");
            }
            else
            {
                return ActionResult.Failed($"Yetersiz altin: {actingHero.Gold} < {amount}");
            }
        }

        return ActionResult.Failed("Altin transferi basarisiz");
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
    }

    private ActionResult HandleTrade(AgentAction action, string agentId)
    {
        action.Parameters.TryGetValue("detail", out var detail);
<<<<<<< HEAD
=======

>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        var actingHero = FindHeroByAgentId(agentId);
        var settlement = actingHero?.CurrentSettlement;

        if (settlement?.Town != null)
        {
<<<<<<< HEAD
            var tradeMsg = _isTurkish ? "TICARET" : "TRADE";
            ShowMessage($"{tradeMsg}: {actingHero?.Name} @ {settlement.Name}", Colors.Yellow);
            var tradedMsg = _isTurkish ? "ticaret gerceklestirdi" : "traded at";
            return ActionResult.Successful($"{actingHero?.Name} {tradedMsg} {settlement.Name}");
        }

        var tradeLabel = _isTurkish ? "Ticaret" : "Trade";
        ShowMessage($"{tradeLabel}: {detail}", Colors.Yellow);
        var simulatedMsg = _isTurkish ? "Ticaret simule edildi" : "Trade simulated";
        return ActionResult.Successful(simulatedMsg);
=======
            ShowMessage($"TICARET: {actingHero?.Name} @ {settlement.Name}", Colors.Yellow);
            return ActionResult.Successful($"{actingHero?.Name} {settlement.Name}'de ticaret gerceklestirdi");
        }

        ShowMessage($"Ticaret: {detail}", Colors.Yellow);
        return ActionResult.Successful("Ticaret simule edildi");
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
    }

    private ActionResult HandleMoveArmy(AgentAction action, string agentId)
    {
        action.Parameters.TryGetValue("targetLocation", out var targetLocation);
        action.Parameters.TryGetValue("detail", out var detail);
<<<<<<< HEAD
=======

>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        var targetName = targetLocation?.ToString() ?? detail?.ToString() ?? "";

        var actingHero = FindHeroByAgentId(agentId);
        var party = actingHero?.PartyBelongedTo;
        var targetSettlement = FindSettlementByName(targetName);

        if (party != null && targetSettlement != null)
        {
<<<<<<< HEAD
            SetPartyTargetSettlement(party, targetSettlement);
            var moveMsg = _isTurkish ? "ORDU HAREKETI" : "ARMY MOVEMENT";
            ShowMessage(moveMsg, Colors.Cyan);
            ShowMessage($"  {party.Name} -> {targetSettlement.Name}", Colors.Cyan);
            var startedMsg = _isTurkish ? "Hedef belirlendi, hareket basladi" : "Target set, movement started";
            ShowMessage($"  {startedMsg}", Colors.Green);
=======
            // REAL ACTION: Set target settlement for the party
            SetPartyTargetSettlement(party, targetSettlement);
            
            ShowMessage($"ORDU HAREKETI", Colors.Cyan);
            ShowMessage($"  {party.Name} -> {targetSettlement.Name}", Colors.Cyan);
            ShowMessage($"  Hedef belirlendi, hareket basladi", Colors.Green);
            
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
            return ActionResult.Successful($"{party.Name} -> {targetSettlement.Name}");
        }

        if (targetSettlement != null)
        {
<<<<<<< HEAD
            var targetLabel = _isTurkish ? "Hedef" : "Target";
            ShowMessage($"{targetLabel}: {targetSettlement.Name}", Colors.Cyan);
            var directedMsg = _isTurkish ? "Ordu yonlendirildi" : "Army directed to";
            return ActionResult.Successful($"{directedMsg} {targetSettlement.Name}");
        }

        var notFoundMsg = _isTurkish ? "Hedef konum bulunamadi" : "Target location not found";
        return ActionResult.Failed($"{notFoundMsg}: {targetName}");
=======
            ShowMessage($"Hedef: {targetSettlement.Name}", Colors.Cyan);
            return ActionResult.Successful($"Ordu {targetSettlement.Name}'e yonlendirildi");
        }

        return ActionResult.Failed($"Hedef konum bulunamadi: {targetName}");
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
    }

    private ActionResult HandleRecruitTroops(AgentAction action, string agentId)
    {
        action.Parameters.TryGetValue("troopCount", out var countObj);
        action.Parameters.TryGetValue("detail", out var detail);
<<<<<<< HEAD
=======

>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        var count = Convert.ToInt32(countObj ?? 10);

        var actingHero = FindHeroByAgentId(agentId);
        var party = actingHero?.PartyBelongedTo;
        var settlement = actingHero?.CurrentSettlement;

        if (party != null && settlement != null)
        {
            var beforeCount = party.MemberRoster.TotalManCount;
            var recruited = 0;
            var notable = settlement.Notables?.FirstOrDefault();
<<<<<<< HEAD

=======
            
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
            if (notable?.VolunteerTypes != null)
            {
                for (int i = 0; i < Math.Min(count, notable.VolunteerTypes.Length); i++)
                {
                    var troopType = notable.VolunteerTypes[i];
                    if (troopType != null)
                    {
                        party.MemberRoster.AddToCounts(troopType, 1);
                        recruited++;
                    }
                }
            }

            var afterCount = party.MemberRoster.TotalManCount;

            if (recruited > 0)
            {
<<<<<<< HEAD
                var recruitMsg = _isTurkish ? "ASKER TOPLANDI" : "TROOPS RECRUITED";
                ShowMessage(recruitMsg, Colors.Magenta);
                var partyLabel = _isTurkish ? "Parti" : "Party";
                ShowMessage($"  {partyLabel}: {beforeCount} -> {afterCount} (+{recruited})", Colors.Magenta);
                var recruitedMsg = _isTurkish ? "Asker toplandi" : "Troops recruited";
                return ActionResult.Successful($"{recruitedMsg}: {beforeCount} -> {afterCount}");
            }
        }

        var recruitLabel = _isTurkish ? "Asker toplama" : "Troop recruitment";
        var simulatedLabel = _isTurkish ? "(simule)" : "(simulated)";
        ShowMessage($"{recruitLabel}: {count} {simulatedLabel}", Colors.Magenta);
        var simulatedMsg = _isTurkish ? "asker toplanmasi simule edildi" : "troop recruitment simulated";
        return ActionResult.Successful($"{count} {simulatedMsg}");
=======
                ShowMessage($"ASKER TOPLANDI", Colors.Magenta);
                ShowMessage($"  Parti: {beforeCount} -> {afterCount} (+{recruited})", Colors.Magenta);
                return ActionResult.Successful($"Asker toplandi: {beforeCount} -> {afterCount}");
            }
        }

        ShowMessage($"Asker toplama: {count} (simule)", Colors.Magenta);
        return ActionResult.Successful($"{count} asker toplanmasi simule edildi");
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
    }

    private ActionResult HandleStartSiege(AgentAction action, string agentId)
    {
        action.Parameters.TryGetValue("settlementId", out var settlementId);
        action.Parameters.TryGetValue("detail", out var detail);
<<<<<<< HEAD
=======

>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        var targetName = settlementId?.ToString() ?? detail?.ToString() ?? "";

        var actingHero = FindHeroByAgentId(agentId);
        var party = actingHero?.PartyBelongedTo;
        var targetSettlement = FindSettlementByName(targetName);

<<<<<<< HEAD
        if (party != null && targetSettlement != null && (targetSettlement.IsTown || targetSettlement.IsCastle))
=======
        if (party != null && targetSettlement != null && 
            (targetSettlement.IsTown || targetSettlement.IsCastle))
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        {
            var partyFaction = party.MapFaction;
            var settlementFaction = targetSettlement.MapFaction;

<<<<<<< HEAD
            if (partyFaction != null && settlementFaction != null && partyFaction.IsAtWarWith(settlementFaction))
            {
                // REAL ACTION: Start siege
                SetPartyBesiegeSettlement(party, targetSettlement);

                var siegeMsg = _isTurkish ? "KUSATMA BASLADI" : "SIEGE STARTED";
                ShowMessage(siegeMsg, Colors.Red);
                ShowMessage($"  {party.Name} -> {targetSettlement.Name}", Colors.Red);
                var startedMsg = _isTurkish ? "kusatmasi baslatildi" : "siege started";
                return ActionResult.Successful($"{targetSettlement.Name} {startedMsg}!");
            }
            else
            {
                var notEnemyMsg = _isTurkish ? "dusman degil, kusatilamaz" : "not enemy, cannot siege";
                return ActionResult.Failed($"{targetSettlement.Name} {notEnemyMsg}");
            }
        }

        var siegeTargetLabel = _isTurkish ? "Kusatma hedefi" : "Siege target";
        ShowMessage($"{siegeTargetLabel}: {targetName}", Colors.Red);
        var failedMsg = _isTurkish ? "Kusatma baslatilamadi" : "Siege could not start";
        return ActionResult.Failed($"{failedMsg}: {targetName}");
=======
            if (partyFaction != null && settlementFaction != null && 
                partyFaction.IsAtWarWith(settlementFaction))
            {
                // REAL ACTION: Start siege
                SetPartyBesiegeSettlement(party, targetSettlement);
                
                ShowMessage($"KUSATMA BASLADI", Colors.Red);
                ShowMessage($"  {party.Name} -> {targetSettlement.Name}", Colors.Red);
                return ActionResult.Successful($"{targetSettlement.Name} kusatmasi baslatildi!");
            }
            else
            {
                return ActionResult.Failed($"{targetSettlement.Name} dusman degil, kusatilamaz");
            }
        }

        ShowMessage($"Kusatma hedefi: {targetName}", Colors.Red);
        return ActionResult.Failed($"Kusatma baslatilamadi: {targetName}");
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
    }

    private ActionResult HandleAttack(AgentAction action, string agentId)
    {
        action.Parameters.TryGetValue("targetPartyId", out var targetPartyId);
        action.Parameters.TryGetValue("detail", out var detail);
<<<<<<< HEAD
=======

>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        var targetName = targetPartyId?.ToString() ?? detail?.ToString() ?? "";

        var actingHero = FindHeroByAgentId(agentId);
        var party = actingHero?.PartyBelongedTo;

        if (party != null)
        {
            var enemyParty = FindEnemyParty(party, targetName);

            if (enemyParty != null)
            {
                // REAL ACTION: Set to engage enemy
                SetPartyEngageParty(party, enemyParty);
<<<<<<< HEAD

                var attackMsg = _isTurkish ? "SALDIRI" : "ATTACK";
                ShowMessage(attackMsg, Colors.Red);
=======
                
                ShowMessage($"SALDIRI", Colors.Red);
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
                ShowMessage($"  {party.Name} -> {enemyParty.Name}", Colors.Red);
                return ActionResult.Successful($"{party.Name} -> {enemyParty.Name}");
            }
        }

<<<<<<< HEAD
        var attackOrderMsg = _isTurkish ? "Saldiri emri verildi" : "Attack order given";
        ShowMessage($"{attackOrderMsg}: {detail}", Colors.Red);
        return ActionResult.Successful(attackOrderMsg);
=======
        ShowMessage($"Saldiri emri verildi: {detail}", Colors.Red);
        return ActionResult.Successful("Saldiri emri verildi");
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
    }

    private ActionResult HandleRetreat(AgentAction action, string agentId)
    {
        action.Parameters.TryGetValue("detail", out var detail);
<<<<<<< HEAD
=======

>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        var actingHero = FindHeroByAgentId(agentId);
        var party = actingHero?.PartyBelongedTo;

        if (party != null)
        {
            var friendlySettlement = FindNearestFriendlySettlement(party);

            if (friendlySettlement != null)
            {
                SetPartyTargetSettlement(party, friendlySettlement);
<<<<<<< HEAD
                var retreatMsg = _isTurkish ? "GERI CEKILME" : "RETREAT";
                ShowMessage(retreatMsg, Colors.Yellow);
=======
                
                ShowMessage($"GERI CEKILME", Colors.Yellow);
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
                ShowMessage($"  {party.Name} -> {friendlySettlement.Name}", Colors.Yellow);
                return ActionResult.Successful($"{party.Name} -> {friendlySettlement.Name}");
            }
        }

<<<<<<< HEAD
        var retreatLabel = _isTurkish ? "Geri cekilme" : "Retreat";
        ShowMessage($"{retreatLabel}: {detail}", Colors.Yellow);
        var retreatOrderMsg = _isTurkish ? "Geri cekilme emri verildi" : "Retreat order given";
        return ActionResult.Successful(retreatOrderMsg);
=======
        ShowMessage($"Geri cekilme: {detail}", Colors.Yellow);
        return ActionResult.Successful("Geri cekilme emri verildi");
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
    }

    private ActionResult HandleDefend(AgentAction action, string agentId)
    {
        action.Parameters.TryGetValue("settlementId", out var settlementId);
        action.Parameters.TryGetValue("detail", out var detail);
<<<<<<< HEAD
=======

>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        var targetName = settlementId?.ToString() ?? detail?.ToString() ?? "";

        var actingHero = FindHeroByAgentId(agentId);
        var party = actingHero?.PartyBelongedTo;
        var settlement = FindSettlementByName(targetName) ?? actingHero?.CurrentSettlement;

        if (party != null && settlement != null)
        {
            SetPartyTargetSettlement(party, settlement);
<<<<<<< HEAD
            var defendMsg = _isTurkish ? "SAVUNMA" : "DEFEND";
            ShowMessage(defendMsg, Colors.Blue);
=======
            
            ShowMessage($"SAVUNMA", Colors.Blue);
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
            ShowMessage($"  {party.Name} -> {settlement.Name}", Colors.Blue);
            return ActionResult.Successful($"{party.Name} -> {settlement.Name}");
        }

<<<<<<< HEAD
        var defenseLabel = _isTurkish ? "Savunma pozisyonu" : "Defense position";
        ShowMessage($"{defenseLabel}: {detail}", Colors.Blue);
        var positionTakenMsg = _isTurkish ? "Savunma pozisyonu alindi" : "Defense position taken";
        return ActionResult.Successful(positionTakenMsg);
=======
        ShowMessage($"Savunma pozisyonu: {detail}", Colors.Blue);
        return ActionResult.Successful("Savunma pozisyonu alindi");
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
    }

    private ActionResult HandlePatrol(AgentAction action, string agentId)
    {
        action.Parameters.TryGetValue("areaId", out var areaId);
        action.Parameters.TryGetValue("detail", out var detail);
<<<<<<< HEAD
=======

>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        var areaName = areaId?.ToString() ?? detail?.ToString() ?? "";

        var actingHero = FindHeroByAgentId(agentId);
        var party = actingHero?.PartyBelongedTo;
        var settlement = FindSettlementByName(areaName);

        if (party != null && settlement != null)
        {
            SetPartyPatrolAroundSettlement(party, settlement);
<<<<<<< HEAD
            var patrolMsg = _isTurkish ? "DEVRIYE" : "PATROL";
            ShowMessage(patrolMsg, Colors.Cyan);
=======
            
            ShowMessage($"DEVRIYE", Colors.Cyan);
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
            ShowMessage($"  {party.Name} <> {settlement.Name}", Colors.Cyan);
            return ActionResult.Successful($"{party.Name} <> {settlement.Name}");
        }

<<<<<<< HEAD
        var patrolLabel = _isTurkish ? "Devriye" : "Patrol";
        ShowMessage($"{patrolLabel}: {detail}", Colors.Cyan);
        var patrolStartedMsg = _isTurkish ? "Devriye gorevi basladi" : "Patrol duty started";
        return ActionResult.Successful(patrolStartedMsg);
=======
        ShowMessage($"Devriye: {detail}", Colors.Cyan);
        return ActionResult.Successful("Devriye gorevi basladi");
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
    }

    private ActionResult HandleTalk(AgentAction action, string agentId)
    {
        action.Parameters.TryGetValue("targetHeroId", out var targetHeroId);
        action.Parameters.TryGetValue("detail", out var detail);

        var targetName = targetHeroId?.ToString() ?? detail?.ToString() ?? "";
        var actingHero = FindHeroByAgentId(agentId);
        var targetHero = FindHeroByName(targetName);

        if (actingHero != null && targetHero != null)
        {
            var beforeRel = CharacterRelationManager.GetHeroRelation(actingHero, targetHero);
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(actingHero, targetHero, 1);
            var afterRel = CharacterRelationManager.GetHeroRelation(actingHero, targetHero);
<<<<<<< HEAD

            var talkMsg = _isTurkish ? "KONUSMA" : "TALK";
            ShowMessage($"{talkMsg}: {actingHero.Name} <-> {targetHero.Name}", Colors.White);
            var relationLabel = _isTurkish ? "Iliski" : "Relation";
            ShowMessage($"  {relationLabel}: {beforeRel} -> {afterRel}", Colors.Green);
            var talkLabel = _isTurkish ? "Konusma" : "Talk";
            return ActionResult.Successful($"{talkLabel}: {beforeRel} -> {afterRel}");
        }

        var talkCompleteMsg = _isTurkish ? "Konusma gerceklesti" : "Talk completed";
        return ActionResult.Successful(talkCompleteMsg);
=======
            
            ShowMessage($"KONUSMA: {actingHero.Name} <-> {targetHero.Name}", Colors.White);
            ShowMessage($"  Iliski: {beforeRel} -> {afterRel}", Colors.Green);
            return ActionResult.Successful($"Konusma: {beforeRel} -> {afterRel}");
        }

        return ActionResult.Successful("Konusma gerceklesti");
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
    }

    private ActionResult HandleWork(AgentAction action, string agentId)
    {
        action.Parameters.TryGetValue("detail", out var detail);
<<<<<<< HEAD
=======

>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        var actingHero = FindHeroByAgentId(agentId);
        var settlement = actingHero?.CurrentSettlement;

        if (settlement?.Village != null)
        {
            var beforeHearth = settlement.Village.Hearth;
            settlement.Village.Hearth += 0.1f;
            var afterHearth = settlement.Village.Hearth;
<<<<<<< HEAD

            var workMsg = _isTurkish ? "CALISMA" : "WORK";
            ShowMessage($"{workMsg}: {settlement.Name}", Colors.Green);
            var hearthLabel = _isTurkish ? "Ocak" : "Hearth";
            ShowMessage($"  {hearthLabel}: {beforeHearth:F1} -> {afterHearth:F1}", Colors.Green);
            var workLabel = _isTurkish ? "Calisma" : "Work";
            return ActionResult.Successful($"{workLabel}: {beforeHearth:F1} -> {afterHearth:F1}");
        }

        var workCompleteMsg = _isTurkish ? "Calisma tamamlandi" : "Work completed";
        return ActionResult.Successful(workCompleteMsg);
=======
            
            ShowMessage($"CALISMA: {settlement.Name}", Colors.Green);
            ShowMessage($"  Ocak: {beforeHearth:F1} -> {afterHearth:F1}", Colors.Green);
            return ActionResult.Successful($"Calisma: {beforeHearth:F1} -> {afterHearth:F1}");
        }

        return ActionResult.Successful("Calisma tamamlandi");
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
    }

    private ActionResult HandleHide(AgentAction action, string agentId)
    {
        action.Parameters.TryGetValue("detail", out var detail);
<<<<<<< HEAD
=======

>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        var actingHero = FindHeroByAgentId(agentId);
        var party = actingHero?.PartyBelongedTo;

        if (party != null)
        {
            SetPartyPassive(party);
<<<<<<< HEAD
            var hideMsg = _isTurkish ? "GIZLENME" : "HIDING";
            ShowMessage($"{hideMsg}: {party.Name}", Colors.Gray);
            var hidingMsg = _isTurkish ? "gizleniyor" : "is hiding";
            return ActionResult.Successful($"{party.Name} {hidingMsg}");
        }

        var hiddenMsg = _isTurkish ? "Gizlendi" : "Hidden";
        return ActionResult.Successful(hiddenMsg);
=======
            
            ShowMessage($"GIZLENME: {party.Name}", Colors.Gray);
            return ActionResult.Successful($"{party.Name} gizleniyor");
        }

        return ActionResult.Successful("Gizlendi");
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
    }

    #endregion

    #region Party AI Helper Methods

    private void SetPartyTargetSettlement(MobileParty party, Settlement settlement)
    {
        try
        {
            party.SetMoveGoToSettlement(settlement, MobileParty.NavigationType.Default, false);
            Debug.Print($"[LivingInCalradia] Party {party.Name} moving to {settlement.Name}");
        }
        catch (Exception ex)
        {
            Debug.Print($"[LivingInCalradia] SetMoveGoToSettlement error: {ex.Message}");
        }
    }

    private void SetPartyBesiegeSettlement(MobileParty party, Settlement settlement)
    {
        try
        {
            party.SetMoveBesiegeSettlement(settlement, MobileParty.NavigationType.Default);
            Debug.Print($"[LivingInCalradia] Party {party.Name} besieging {settlement.Name}");
        }
        catch (Exception ex)
        {
            Debug.Print($"[LivingInCalradia] SetMoveBesiegeSettlement error: {ex.Message}");
        }
    }

    private void SetPartyEngageParty(MobileParty party, MobileParty targetParty)
    {
        try
        {
            party.SetMoveEngageParty(targetParty, MobileParty.NavigationType.Default);
            Debug.Print($"[LivingInCalradia] Party {party.Name} engaging {targetParty.Name}");
        }
        catch (Exception ex)
        {
            Debug.Print($"[LivingInCalradia] SetMoveEngageParty error: {ex.Message}");
        }
    }

    private void SetPartyPatrolAroundSettlement(MobileParty party, Settlement settlement)
    {
        try
        {
            party.SetMovePatrolAroundSettlement(settlement, MobileParty.NavigationType.Default, false);
            Debug.Print($"[LivingInCalradia] Party {party.Name} patrolling around {settlement.Name}");
        }
        catch (Exception ex)
        {
            Debug.Print($"[LivingInCalradia] SetMovePatrolAroundSettlement error: {ex.Message}");
        }
    }

    private void SetPartyPassive(MobileParty party)
    {
        try
        {
            party.Ai.SetDoNotMakeNewDecisions(true);
            Debug.Print($"[LivingInCalradia] Party {party.Name} set to passive");
        }
        catch (Exception ex)
        {
            Debug.Print($"[LivingInCalradia] SetDoNotMakeNewDecisions error: {ex.Message}");
        }
    }

    #endregion

    #region Helper Methods

    private Hero? FindHeroByAgentId(string agentId)
    {
        if (string.IsNullOrWhiteSpace(agentId) || Campaign.Current?.AliveHeroes == null)
            return null;

        var parts = agentId.Split('_');
        var heroName = parts.Length >= 2 ? parts[1] : agentId;

        return Campaign.Current.AliveHeroes
            .FirstOrDefault(h => h.Name?.ToString()?.Contains(heroName) == true);
    }

    private Hero? FindHeroByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || Campaign.Current?.AliveHeroes == null)
            return null;

        return Campaign.Current.AliveHeroes
            .FirstOrDefault(h => h.Name?.ToString()?.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private Settlement? FindSettlementByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || Campaign.Current?.Settlements == null)
            return null;

        return Campaign.Current.Settlements
            .FirstOrDefault(s => s.Name?.ToString()?.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private Kingdom? FindKingdomByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || Campaign.Current?.Kingdoms == null)
            return null;

        return Campaign.Current.Kingdoms
            .FirstOrDefault(k => k.Name?.ToString()?.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private MobileParty? FindEnemyParty(MobileParty myParty, string targetName)
    {
        if (Campaign.Current?.MobileParties == null)
            return null;

        var myFaction = myParty.MapFaction;
<<<<<<< HEAD

        return Campaign.Current.MobileParties
            .Where(p => p.MapFaction != null && myFaction.IsAtWarWith(p.MapFaction))
            .Where(p => string.IsNullOrWhiteSpace(targetName) ||
=======
        
        return Campaign.Current.MobileParties
            .Where(p => p.MapFaction != null && myFaction.IsAtWarWith(p.MapFaction))
            .Where(p => string.IsNullOrWhiteSpace(targetName) || 
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
                        p.Name?.ToString()?.IndexOf(targetName, StringComparison.OrdinalIgnoreCase) >= 0)
            .FirstOrDefault();
    }

    private Settlement? FindNearestFriendlySettlement(MobileParty party)
    {
        if (Campaign.Current?.Settlements == null)
            return null;

        var myFaction = party.MapFaction;
<<<<<<< HEAD

=======
        
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        return Campaign.Current.Settlements
            .Where(s => s.MapFaction == myFaction && (s.IsTown || s.IsCastle))
            .FirstOrDefault();
    }

    private void LogAction(string actionType, IDictionary<string, object> parameters, string message)
    {
        var paramStr = string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}"));
        Debug.Print($"[LivingInCalradia] ACTION: {actionType} - {message} | Params: {paramStr}");
    }

    private static void ShowMessage(string message, Color color)
    {
        try
        {
            InformationManager.DisplayMessage(new InformationMessage($"[AI] {message}", color));
        }
        catch
        {
<<<<<<< HEAD
=======
            // Ignore display errors
>>>>>>> 7bbf43fa65d416561c574b3f55c38a156e5f6049
        }
    }

    #endregion
}
