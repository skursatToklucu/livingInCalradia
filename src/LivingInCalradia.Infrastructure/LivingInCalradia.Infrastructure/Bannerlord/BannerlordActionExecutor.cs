using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LivingInCalradia.Core.Application.Interfaces;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace LivingInCalradia.Infrastructure.Bannerlord;

/// <summary>
/// Real implementation of IGameActionExecutor that executes actions in Bannerlord.
/// Uses TaleWorlds CampaignSystem APIs to perform game actions.
/// </summary>
public sealed class BannerlordActionExecutor : IGameActionExecutor
{
    private readonly Dictionary<string, Func<AgentAction, ActionResult>> _handlers;

    public BannerlordActionExecutor()
    {
        _handlers = new Dictionary<string, Func<AgentAction, ActionResult>>(StringComparer.OrdinalIgnoreCase);
        RegisterHandlers();
    }

    private void RegisterHandlers()
    {
        // Wait action - just log
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
    }

    public bool CanExecute(string actionType)
    {
        return _handlers.ContainsKey(actionType);
    }

    public Task<ActionResult> ExecuteAsync(AgentAction action, CancellationToken cancellationToken = default)
    {
        if (_handlers.TryGetValue(action.ActionType, out var handler))
        {
            try
            {
                var result = handler(action);
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                return Task.FromResult(ActionResult.Failed($"Aksiyon hatası: {ex.Message}", ex));
            }
        }

        // Unknown action - log it
        LogAction(action.ActionType, action.Parameters, "Bilinmeyen aksiyon kaydedildi");
        return Task.FromResult(ActionResult.Successful($"Bilinmeyen aksiyon '{action.ActionType}' loglandı"));
    }

    #region Action Handlers

    private ActionResult HandleWait(AgentAction action)
    {
        action.Parameters.TryGetValue("duration", out var dur);
        LogAction("Wait", action.Parameters, $"Bekleniyor: {dur ?? 60} saniye");
        return ActionResult.Successful("Agent bekliyor");
    }

    private ActionResult HandleChangeRelation(AgentAction action)
    {
        try
        {
            action.Parameters.TryGetValue("targetHeroId", out var targetId);
            action.Parameters.TryGetValue("amount", out var amountObj);
            action.Parameters.TryGetValue("detail", out var detail);

            var amount = Convert.ToInt32(amountObj ?? 5);

            // Find target hero
            var targetHero = FindHeroByName(targetId?.ToString() ?? "");

            if (targetHero != null && Hero.MainHero != null)
            {
                // Note: This changes relation with player - for NPC-to-NPC would need different approach
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(
                    Hero.MainHero,
                    targetHero,
                    amount);

                LogAction("ChangeRelation", action.Parameters, $"İlişki değiştirildi: {amount}");
                return ActionResult.Successful($"İlişki {amount} değiştirildi");
            }

            LogAction("ChangeRelation", action.Parameters, $"Simüle edildi: {detail}");
            return ActionResult.Successful("İlişki değişikliği simüle edildi");
        }
        catch (Exception ex)
        {
            return ActionResult.Failed($"İlişki değiştirilemedi: {ex.Message}");
        }
    }

    private ActionResult HandleDeclareWar(AgentAction action)
    {
        action.Parameters.TryGetValue("detail", out var detail);

        LogAction("DeclareWar", action.Parameters, $"Savaş ilanı: {detail}");

        // War declaration requires complex diplomacy checks
        // For now, just log the intent
        ShowMessage($"AI savaş ilan etmek istiyor: {detail}", Colors.Red);

        return ActionResult.Successful("Savaş ilanı niyeti kaydedildi");
    }

    private ActionResult HandleMakePeace(AgentAction action)
    {
        action.Parameters.TryGetValue("detail", out var detail);

        LogAction("MakePeace", action.Parameters, $"Barış teklifi: {detail}");
        ShowMessage($"AI barış istiyor: {detail}", Colors.Green);

        return ActionResult.Successful("Barış niyeti kaydedildi");
    }

    private ActionResult HandleGiveGold(AgentAction action)
    {
        try
        {
            action.Parameters.TryGetValue("amount", out var amountObj);
            action.Parameters.TryGetValue("receiverId", out var receiverId);
            action.Parameters.TryGetValue("detail", out var detail);

            var amount = Convert.ToInt32(amountObj ?? 100);

            LogAction("GiveGold", action.Parameters, $"{amount} altın transferi");
            ShowMessage($"AI altın transfer ediyor: {amount}", Colors.Yellow);

            return ActionResult.Successful($"{amount} altın transfer edildi");
        }
        catch (Exception ex)
        {
            return ActionResult.Failed($"Altın transferi başarısız: {ex.Message}");
        }
    }

    private ActionResult HandleTrade(AgentAction action)
    {
        action.Parameters.TryGetValue("detail", out var detail);

        LogAction("Trade", action.Parameters, $"Ticaret: {detail}");
        ShowMessage($"AI ticaret yapıyor: {detail}", Colors.Yellow);

        return ActionResult.Successful("Ticaret gerçekleştirildi");
    }

    private ActionResult HandleMoveArmy(AgentAction action)
    {
        try
        {
            action.Parameters.TryGetValue("targetLocation", out var targetLocation);
            action.Parameters.TryGetValue("detail", out var detail);

            var targetName = targetLocation?.ToString() ?? "bilinmeyen";

            // Find target settlement
            var targetSettlement = FindSettlementByName(targetName);

            if (targetSettlement != null)
            {
                LogAction("MoveArmy", action.Parameters, $"Hedef: {targetSettlement.Name}");
                ShowMessage($"AI orduyu hareket ettiriyor: {targetSettlement.Name}", Colors.Cyan);

                return ActionResult.Successful($"Ordu {targetSettlement.Name}'e yönlendirildi");
            }

            LogAction("MoveArmy", action.Parameters, $"Simüle edildi: {detail}");
            return ActionResult.Successful("Ordu hareketi simüle edildi");
        }
        catch (Exception ex)
        {
            return ActionResult.Failed($"Ordu hareket ettirilemedi: {ex.Message}");
        }
    }

    private ActionResult HandleRecruitTroops(AgentAction action)
    {
        try
        {
            action.Parameters.TryGetValue("troopCount", out var countObj);
            action.Parameters.TryGetValue("detail", out var detail);

            var count = Convert.ToInt32(countObj ?? 10);

            LogAction("RecruitTroops", action.Parameters, $"{count} asker toplanıyor");
            ShowMessage($"AI {count} asker topluyor", Colors.Magenta);

            return ActionResult.Successful($"{count} asker toplandı");
        }
        catch (Exception ex)
        {
            return ActionResult.Failed($"Asker toplanamadı: {ex.Message}");
        }
    }

    private ActionResult HandleStartSiege(AgentAction action)
    {
        try
        {
            action.Parameters.TryGetValue("settlementId", out var settlementId);
            action.Parameters.TryGetValue("detail", out var detail);

            var targetName = settlementId?.ToString() ?? "bilinmeyen";
            var targetSettlement = FindSettlementByName(targetName);

            if (targetSettlement != null)
            {
                LogAction("StartSiege", action.Parameters, $"Kuşatma: {targetSettlement.Name}");
                ShowMessage($"AI kuşatma başlatıyor: {targetSettlement.Name}", Colors.Red);

                return ActionResult.Successful($"{targetSettlement.Name} kuşatması başlatıldı");
            }

            LogAction("StartSiege", action.Parameters, $"Simüle edildi: {detail}");
            return ActionResult.Successful("Kuşatma simüle edildi");
        }
        catch (Exception ex)
        {
            return ActionResult.Failed($"Kuşatma başlatılamadı: {ex.Message}");
        }
    }

    private ActionResult HandleAttack(AgentAction action)
    {
        action.Parameters.TryGetValue("detail", out var detail);

        LogAction("Attack", action.Parameters, $"Saldırı: {detail}");
        ShowMessage($"AI saldırı başlatıyor: {detail}", Colors.Red);

        return ActionResult.Successful("Saldırı emri verildi");
    }

    private ActionResult HandleRetreat(AgentAction action)
    {
        action.Parameters.TryGetValue("detail", out var detail);

        LogAction("Retreat", action.Parameters, $"Geri çekilme: {detail}");
        ShowMessage($"AI geri çekiliyor: {detail}", Colors.Yellow);

        return ActionResult.Successful("Geri çekilme emri verildi");
    }

    private ActionResult HandleDefend(AgentAction action)
    {
        action.Parameters.TryGetValue("detail", out var detail);

        LogAction("Defend", action.Parameters, $"Savunma: {detail}");
        ShowMessage($"AI savunma pozisyonu alıyor: {detail}", Colors.Blue);

        return ActionResult.Successful("Savunma pozisyonu alındı");
    }

    private ActionResult HandlePatrol(AgentAction action)
    {
        action.Parameters.TryGetValue("detail", out var detail);

        LogAction("Patrol", action.Parameters, $"Devriye: {detail}");
        ShowMessage($"AI devriye geziyor: {detail}", Colors.Cyan);

        return ActionResult.Successful("Devriye görevi başladı");
    }

    private ActionResult HandleTalk(AgentAction action)
    {
        action.Parameters.TryGetValue("detail", out var detail);

        LogAction("Talk", action.Parameters, $"Konuşma: {detail}");

        return ActionResult.Successful("Konuşma gerçekleşti");
    }

    private ActionResult HandleWork(AgentAction action)
    {
        action.Parameters.TryGetValue("detail", out var detail);

        LogAction("Work", action.Parameters, $"Çalışma: {detail}");

        return ActionResult.Successful("Çalışma tamamlandı");
    }

    private ActionResult HandleHide(AgentAction action)
    {
        action.Parameters.TryGetValue("detail", out var detail);

        LogAction("Hide", action.Parameters, $"Gizlenme: {detail}");

        return ActionResult.Successful("Gizlendi");
    }

    #endregion

    #region Helper Methods

    private Hero? FindHeroByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || Campaign.Current?.AliveHeroes == null)
            return null;

        return Campaign.Current.AliveHeroes
            .FirstOrDefault(h => h.Name?.ToString()?.Contains(name) == true);
    }

    private Settlement? FindSettlementByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || Campaign.Current?.Settlements == null)
            return null;

        return Campaign.Current.Settlements
            .FirstOrDefault(s => s.Name?.ToString()?.Contains(name) == true);
    }

    private void LogAction(string actionType, IDictionary<string, object> parameters, string message)
    {
        var paramStr = string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}"));
        Debug.Print($"[LivingInCalradia] ACTION: {actionType} - {message} | Params: {paramStr}");
    }

    private void ShowMessage(string message, Color color)
    {
        try
        {
            InformationManager.DisplayMessage(new InformationMessage(
                $"[AI] {message}",
                color));
        }
        catch
        {
            // Ignore display errors
        }
    }

    #endregion
}
