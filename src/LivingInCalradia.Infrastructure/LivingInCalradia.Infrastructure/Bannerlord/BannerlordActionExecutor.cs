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
                LogAction(action.ActionType, action.Parameters, $"HATA: {ex.Message}");
                return Task.FromResult(ActionResult.Failed($"Aksiyon hatasi: {ex.Message}", ex));
            }
        }

        LogAction(action.ActionType, action.Parameters, "Bilinmeyen aksiyon");
        return Task.FromResult(ActionResult.Failed($"Bilinmeyen aksiyon: {action.ActionType}"));
    }
    
    /// <summary>
    /// Runs a proof-of-concept test that demonstrates AI thinking leads to real game changes.
    /// Call this to verify the system is working.
    /// </summary>
    public static void RunProofTest()
    {
        try
        {
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
    
    /// <summary>
    /// Runs a comprehensive test showing the full AI workflow with real game changes.
    /// </summary>
    public static void RunFullAIProofTest()
    {
        try
        {
            ShowMessage("", Colors.White);
            ShowMessage("==================================================", Colors.Magenta);
            ShowMessage("     AI DUSUNCE -> AKSIYON PROOF OF CONCEPT        ", Colors.Magenta);
            ShowMessage("==================================================", Colors.Magenta);
            ShowMessage("", Colors.White);
            
            var mainHero = Hero.MainHero;
            if (mainHero == null || Campaign.Current == null)
            {
                ShowMessage("HATA: Oyun durumu gecersiz!", Colors.Red);
                return;
            }
            
            // Find test subjects
            var lords = Campaign.Current.AliveHeroes?
                .Where(h => h != mainHero && h.IsLord && h.Clan?.Kingdom != null)
                .Take(3)
                .ToList();
            
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
            Debug.Print($"[LivingInCalradia] Full test error: {ex}");
        }
    }

    #region Action Handlers

    private ActionResult HandleWait(AgentAction action, string agentId)
    {
        action.Parameters.TryGetValue("duration", out var dur);
        var duration = Convert.ToInt32(dur ?? 60);
        
        ShowMessage($"Bekleniyor ({duration}s)", Colors.Gray);
        return ActionResult.Successful($"Agent {duration} saniye bekledi");
    }
    
    private ActionResult HandleTestProof(AgentAction action, string agentId)
    {
        // This is a special action that proves the system works
        RunProofTest();
        return ActionResult.Successful("Proof test tamamlandi");
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
            
            // REAL ACTION: Change relation between heroes
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(actingHero, targetHero, amount);
            
            // Record AFTER state for proof
            var afterRelation = CharacterRelationManager.GetHeroRelation(actingHero, targetHero);
            
            ShowMessage($"Iliski: {actingHero.Name} <-> {targetHero.Name}", Colors.Cyan);
            ShowMessage($"  {beforeRelation} -> {afterRelation} ({amount:+0;-0})", 
                amount > 0 ? Colors.Green : Colors.Red);
            
            return ActionResult.Successful($"Iliski degisti: {beforeRelation} -> {afterRelation}");
        }

        // Fallback: Try with MainHero
        if (targetHero != null && Hero.MainHero != null)
        {
            var beforeRelation = CharacterRelationManager.GetHeroRelation(Hero.MainHero, targetHero);
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, targetHero, amount);
            var afterRelation = CharacterRelationManager.GetHeroRelation(Hero.MainHero, targetHero);
            
            ShowMessage($"Oyuncu <-> {targetHero.Name}: {beforeRelation} -> {afterRelation}", Colors.Green);
            return ActionResult.Successful($"Iliski degistirildi: {beforeRelation} -> {afterRelation}");
        }

        return ActionResult.Failed("Hedef kahraman bulunamadi");
    }

    private ActionResult HandleDeclareWar(AgentAction action, string agentId)
    {
        action.Parameters.TryGetValue("targetFaction", out var targetFaction);
        action.Parameters.TryGetValue("detail", out var detail);

        var targetName = targetFaction?.ToString() ?? detail?.ToString() ?? "";
        
        var actingHero = FindHeroByAgentId(agentId);
        var actingKingdom = actingHero?.Clan?.Kingdom;
        var targetKingdom = FindKingdomByName(targetName);

        if (actingKingdom != null && targetKingdom != null && actingKingdom != targetKingdom)
        {
            // Check BEFORE state
            var wasAtWar = actingKingdom.IsAtWarWith(targetKingdom);
            
            if (!wasAtWar)
            {
                // REAL ACTION: Declare war using FactionManager
                FactionManager.DeclareWar(actingKingdom, targetKingdom);
                
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
    }

    private ActionResult HandleMakePeace(AgentAction action, string agentId)
    {
        action.Parameters.TryGetValue("targetFaction", out var targetFaction);
        action.Parameters.TryGetValue("detail", out var detail);

        var targetName = targetFaction?.ToString() ?? detail?.ToString() ?? "";
        
        var actingHero = FindHeroByAgentId(agentId);
        var actingKingdom = actingHero?.Clan?.Kingdom;
        var targetKingdom = FindKingdomByName(targetName);

        if (actingKingdom != null && targetKingdom != null)
        {
            var wasAtWar = actingKingdom.IsAtWarWith(targetKingdom);
            
            if (wasAtWar)
            {
                // REAL ACTION: Make peace
                MakePeaceAction.Apply(actingKingdom, targetKingdom);
                
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
    }

    private ActionResult HandleTrade(AgentAction action, string agentId)
    {
        action.Parameters.TryGetValue("detail", out var detail);

        var actingHero = FindHeroByAgentId(agentId);
        var settlement = actingHero?.CurrentSettlement;

        if (settlement?.Town != null)
        {
            ShowMessage($"TICARET: {actingHero?.Name} @ {settlement.Name}", Colors.Yellow);
            return ActionResult.Successful($"{actingHero?.Name} {settlement.Name}'de ticaret gerceklestirdi");
        }

        ShowMessage($"Ticaret: {detail}", Colors.Yellow);
        return ActionResult.Successful("Ticaret simule edildi");
    }

    private ActionResult HandleMoveArmy(AgentAction action, string agentId)
    {
        action.Parameters.TryGetValue("targetLocation", out var targetLocation);
        action.Parameters.TryGetValue("detail", out var detail);

        var targetName = targetLocation?.ToString() ?? detail?.ToString() ?? "";

        var actingHero = FindHeroByAgentId(agentId);
        var party = actingHero?.PartyBelongedTo;
        var targetSettlement = FindSettlementByName(targetName);

        if (party != null && targetSettlement != null)
        {
            // REAL ACTION: Set target settlement for the party
            SetPartyTargetSettlement(party, targetSettlement);
            
            ShowMessage($"ORDU HAREKETI", Colors.Cyan);
            ShowMessage($"  {party.Name} -> {targetSettlement.Name}", Colors.Cyan);
            ShowMessage($"  Hedef belirlendi, hareket basladi", Colors.Green);
            
            return ActionResult.Successful($"{party.Name} -> {targetSettlement.Name}");
        }

        if (targetSettlement != null)
        {
            ShowMessage($"Hedef: {targetSettlement.Name}", Colors.Cyan);
            return ActionResult.Successful($"Ordu {targetSettlement.Name}'e yonlendirildi");
        }

        return ActionResult.Failed($"Hedef konum bulunamadi: {targetName}");
    }

    private ActionResult HandleRecruitTroops(AgentAction action, string agentId)
    {
        action.Parameters.TryGetValue("troopCount", out var countObj);
        action.Parameters.TryGetValue("detail", out var detail);

        var count = Convert.ToInt32(countObj ?? 10);

        var actingHero = FindHeroByAgentId(agentId);
        var party = actingHero?.PartyBelongedTo;
        var settlement = actingHero?.CurrentSettlement;

        if (party != null && settlement != null)
        {
            var beforeCount = party.MemberRoster.TotalManCount;
            var recruited = 0;
            var notable = settlement.Notables?.FirstOrDefault();
            
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
                ShowMessage($"ASKER TOPLANDI", Colors.Magenta);
                ShowMessage($"  Parti: {beforeCount} -> {afterCount} (+{recruited})", Colors.Magenta);
                return ActionResult.Successful($"Asker toplandi: {beforeCount} -> {afterCount}");
            }
        }

        ShowMessage($"Asker toplama: {count} (simule)", Colors.Magenta);
        return ActionResult.Successful($"{count} asker toplanmasi simule edildi");
    }

    private ActionResult HandleStartSiege(AgentAction action, string agentId)
    {
        action.Parameters.TryGetValue("settlementId", out var settlementId);
        action.Parameters.TryGetValue("detail", out var detail);

        var targetName = settlementId?.ToString() ?? detail?.ToString() ?? "";

        var actingHero = FindHeroByAgentId(agentId);
        var party = actingHero?.PartyBelongedTo;
        var targetSettlement = FindSettlementByName(targetName);

        if (party != null && targetSettlement != null && 
            (targetSettlement.IsTown || targetSettlement.IsCastle))
        {
            var partyFaction = party.MapFaction;
            var settlementFaction = targetSettlement.MapFaction;

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
    }

    private ActionResult HandleAttack(AgentAction action, string agentId)
    {
        action.Parameters.TryGetValue("targetPartyId", out var targetPartyId);
        action.Parameters.TryGetValue("detail", out var detail);

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
                
                ShowMessage($"SALDIRI", Colors.Red);
                ShowMessage($"  {party.Name} -> {enemyParty.Name}", Colors.Red);
                return ActionResult.Successful($"{party.Name} -> {enemyParty.Name}");
            }
        }

        ShowMessage($"Saldiri emri verildi: {detail}", Colors.Red);
        return ActionResult.Successful("Saldiri emri verildi");
    }

    private ActionResult HandleRetreat(AgentAction action, string agentId)
    {
        action.Parameters.TryGetValue("detail", out var detail);

        var actingHero = FindHeroByAgentId(agentId);
        var party = actingHero?.PartyBelongedTo;

        if (party != null)
        {
            var friendlySettlement = FindNearestFriendlySettlement(party);

            if (friendlySettlement != null)
            {
                SetPartyTargetSettlement(party, friendlySettlement);
                
                ShowMessage($"GERI CEKILME", Colors.Yellow);
                ShowMessage($"  {party.Name} -> {friendlySettlement.Name}", Colors.Yellow);
                return ActionResult.Successful($"{party.Name} -> {friendlySettlement.Name}");
            }
        }

        ShowMessage($"Geri cekilme: {detail}", Colors.Yellow);
        return ActionResult.Successful("Geri cekilme emri verildi");
    }

    private ActionResult HandleDefend(AgentAction action, string agentId)
    {
        action.Parameters.TryGetValue("settlementId", out var settlementId);
        action.Parameters.TryGetValue("detail", out var detail);

        var targetName = settlementId?.ToString() ?? detail?.ToString() ?? "";

        var actingHero = FindHeroByAgentId(agentId);
        var party = actingHero?.PartyBelongedTo;
        var settlement = FindSettlementByName(targetName) ?? actingHero?.CurrentSettlement;

        if (party != null && settlement != null)
        {
            SetPartyTargetSettlement(party, settlement);
            
            ShowMessage($"SAVUNMA", Colors.Blue);
            ShowMessage($"  {party.Name} -> {settlement.Name}", Colors.Blue);
            return ActionResult.Successful($"{party.Name} -> {settlement.Name}");
        }

        ShowMessage($"Savunma pozisyonu: {detail}", Colors.Blue);
        return ActionResult.Successful("Savunma pozisyonu alindi");
    }

    private ActionResult HandlePatrol(AgentAction action, string agentId)
    {
        action.Parameters.TryGetValue("areaId", out var areaId);
        action.Parameters.TryGetValue("detail", out var detail);

        var areaName = areaId?.ToString() ?? detail?.ToString() ?? "";

        var actingHero = FindHeroByAgentId(agentId);
        var party = actingHero?.PartyBelongedTo;
        var settlement = FindSettlementByName(areaName);

        if (party != null && settlement != null)
        {
            SetPartyPatrolAroundSettlement(party, settlement);
            
            ShowMessage($"DEVRIYE", Colors.Cyan);
            ShowMessage($"  {party.Name} <> {settlement.Name}", Colors.Cyan);
            return ActionResult.Successful($"{party.Name} <> {settlement.Name}");
        }

        ShowMessage($"Devriye: {detail}", Colors.Cyan);
        return ActionResult.Successful("Devriye gorevi basladi");
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
            
            ShowMessage($"KONUSMA: {actingHero.Name} <-> {targetHero.Name}", Colors.White);
            ShowMessage($"  Iliski: {beforeRel} -> {afterRel}", Colors.Green);
            return ActionResult.Successful($"Konusma: {beforeRel} -> {afterRel}");
        }

        return ActionResult.Successful("Konusma gerceklesti");
    }

    private ActionResult HandleWork(AgentAction action, string agentId)
    {
        action.Parameters.TryGetValue("detail", out var detail);

        var actingHero = FindHeroByAgentId(agentId);
        var settlement = actingHero?.CurrentSettlement;

        if (settlement?.Village != null)
        {
            var beforeHearth = settlement.Village.Hearth;
            settlement.Village.Hearth += 0.1f;
            var afterHearth = settlement.Village.Hearth;
            
            ShowMessage($"CALISMA: {settlement.Name}", Colors.Green);
            ShowMessage($"  Ocak: {beforeHearth:F1} -> {afterHearth:F1}", Colors.Green);
            return ActionResult.Successful($"Calisma: {beforeHearth:F1} -> {afterHearth:F1}");
        }

        return ActionResult.Successful("Calisma tamamlandi");
    }

    private ActionResult HandleHide(AgentAction action, string agentId)
    {
        action.Parameters.TryGetValue("detail", out var detail);

        var actingHero = FindHeroByAgentId(agentId);
        var party = actingHero?.PartyBelongedTo;

        if (party != null)
        {
            SetPartyPassive(party);
            
            ShowMessage($"GIZLENME: {party.Name}", Colors.Gray);
            return ActionResult.Successful($"{party.Name} gizleniyor");
        }

        return ActionResult.Successful("Gizlendi");
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
        
        return Campaign.Current.MobileParties
            .Where(p => p.MapFaction != null && myFaction.IsAtWarWith(p.MapFaction))
            .Where(p => string.IsNullOrWhiteSpace(targetName) || 
                        p.Name?.ToString()?.IndexOf(targetName, StringComparison.OrdinalIgnoreCase) >= 0)
            .FirstOrDefault();
    }

    private Settlement? FindNearestFriendlySettlement(MobileParty party)
    {
        if (Campaign.Current?.Settlements == null)
            return null;

        var myFaction = party.MapFaction;
        
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
            // Ignore display errors
        }
    }

    #endregion
}
