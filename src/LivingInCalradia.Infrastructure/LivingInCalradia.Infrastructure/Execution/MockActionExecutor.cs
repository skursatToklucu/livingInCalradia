using System;
using System.Threading;
using System.Threading.Tasks;
using LivingInCalradia.Core.Application.Interfaces;

namespace LivingInCalradia.Infrastructure.Execution;

/// <summary>
/// Mock implementation for testing the action execution pipeline.
/// Logs actions with rich formatting.
/// </summary>
public sealed class MockActionExecutor : IGameActionExecutor
{
    private readonly DelegatingActionExecutor _executor;
    private int _actionCount = 0;
    
    public MockActionExecutor()
    {
        _executor = new DelegatingActionExecutor();
        RegisterMockHandlers();
    }
    
    public int ActionCount => _actionCount;
    
    private void RegisterMockHandlers()
    {
        // Wait action
        _executor.RegisterHandler("Wait", action =>
        {
            _actionCount++;
            action.Parameters.TryGetValue("duration", out var dur);
            var duration = dur ?? 60;
            Console.WriteLine($"   ??  [Wait] Bekleniyor: {duration} saniye");
            return ActionResult.Successful($"Agent waiting for {duration} seconds");
        });
        
        // Log reasoning (passive action)
        _executor.RegisterHandler("LogReasoning", action =>
        {
            _actionCount++;
            action.Parameters.TryGetValue("reasoning", out var r);
            var reasoning = r?.ToString() ?? "No reasoning";
            Console.WriteLine($"   ?? [Log] Dü?ünce kaydedildi");
            return ActionResult.Successful("Reasoning logged");
        });
        
        // StartSiege
        _executor.RegisterHandler("StartSiege", action =>
        {
            _actionCount++;
            action.Parameters.TryGetValue("settlementId", out var sId);
            action.Parameters.TryGetValue("detail", out var detail);
            var settlementId = sId?.ToString() ?? "Bilinmeyen Kale";
            Console.WriteLine($"   ??  [StartSiege] KU?ATMA BA?LATILDI!");
            Console.WriteLine($"       Hedef: {settlementId}");
            if (detail != null) Console.WriteLine($"       Detay: {detail}");
            return ActionResult.Successful($"Siege started on {settlementId}");
        });
        
        // GiveGold
        _executor.RegisterHandler("GiveGold", action =>
        {
            _actionCount++;
            action.Parameters.TryGetValue("amount", out var amt);
            action.Parameters.TryGetValue("detail", out var detail);
            var amount = amt ?? "?";
            Console.WriteLine($"   ?? [GiveGold] ALTIN TRANSFER?!");
            Console.WriteLine($"       Miktar: {amount} alt?n");
            if (detail != null) Console.WriteLine($"       Detay: {detail}");
            return ActionResult.Successful($"Transferred {amount} gold");
        });
        
        // ChangeRelation
        _executor.RegisterHandler("ChangeRelation", action =>
        {
            _actionCount++;
            action.Parameters.TryGetValue("detail", out var detail);
            Console.WriteLine($"   ?? [ChangeRelation] D?PLOMAT?K HAMLE!");
            if (detail != null) Console.WriteLine($"       Detay: {detail}");
            return ActionResult.Successful("Relation changed");
        });
        
        // MoveArmy
        _executor.RegisterHandler("MoveArmy", action =>
        {
            _actionCount++;
            action.Parameters.TryGetValue("targetLocation", out var tgt);
            action.Parameters.TryGetValue("detail", out var detail);
            var target = tgt?.ToString() ?? "Bilinmeyen Konum";
            Console.WriteLine($"   ?? [MoveArmy] ORDU HAREKET?!");
            Console.WriteLine($"       Hedef: {target}");
            if (detail != null) Console.WriteLine($"       Detay: {detail}");
            return ActionResult.Successful($"Army moving to {target}");
        });
        
        // RecruitTroops
        _executor.RegisterHandler("RecruitTroops", action =>
        {
            _actionCount++;
            action.Parameters.TryGetValue("troopCount", out var cnt);
            action.Parameters.TryGetValue("detail", out var detail);
            var count = cnt ?? "?";
            Console.WriteLine($"   ???  [RecruitTroops] ASKER TOPLAMA!");
            Console.WriteLine($"       Miktar: {count} asker");
            if (detail != null) Console.WriteLine($"       Detay: {detail}");
            return ActionResult.Successful($"Recruited {count} troops");
        });
        
        // Trade - Yeni aksiyon
        _executor.RegisterHandler("Trade", action =>
        {
            _actionCount++;
            action.Parameters.TryGetValue("detail", out var detail);
            Console.WriteLine($"   ?? [Trade] T?CARET YAPILIYOR!");
            if (detail != null) Console.WriteLine($"       Detay: {detail}");
            return ActionResult.Successful("Trade completed");
        });
        
        // Patrol - Yeni aksiyon
        _executor.RegisterHandler("Patrol", action =>
        {
            _actionCount++;
            action.Parameters.TryGetValue("detail", out var detail);
            Console.WriteLine($"   ?? [Patrol] DEVR?YE GÖREV?!");
            if (detail != null) Console.WriteLine($"       Detay: {detail}");
            return ActionResult.Successful("Patrol completed");
        });
        
        // Retreat - Yeni aksiyon
        _executor.RegisterHandler("Retreat", action =>
        {
            _actionCount++;
            action.Parameters.TryGetValue("detail", out var detail);
            Console.WriteLine($"   ?? [Retreat] GER? ÇEK?LME!");
            if (detail != null) Console.WriteLine($"       Detay: {detail}");
            return ActionResult.Successful("Retreated successfully");
        });
        
        // Attack - Yeni aksiyon
        _executor.RegisterHandler("Attack", action =>
        {
            _actionCount++;
            action.Parameters.TryGetValue("detail", out var detail);
            Console.WriteLine($"   ??  [Attack] SALDIRI!");
            if (detail != null) Console.WriteLine($"       Detay: {detail}");
            return ActionResult.Successful("Attack initiated");
        });
        
        // Defend - Yeni aksiyon
        _executor.RegisterHandler("Defend", action =>
        {
            _actionCount++;
            action.Parameters.TryGetValue("detail", out var detail);
            Console.WriteLine($"   ???  [Defend] SAVUNMA POZ?SYONU!");
            if (detail != null) Console.WriteLine($"       Detay: {detail}");
            return ActionResult.Successful("Defense position taken");
        });
        
        // Hide - Yeni aksiyon (köylüler için)
        _executor.RegisterHandler("Hide", action =>
        {
            _actionCount++;
            action.Parameters.TryGetValue("detail", out var detail);
            Console.WriteLine($"   ?? [Hide] G?ZLEN?YOR!");
            if (detail != null) Console.WriteLine($"       Detay: {detail}");
            return ActionResult.Successful("Hidden successfully");
        });
        
        // Work - Yeni aksiyon (köylüler için)
        _executor.RegisterHandler("Work", action =>
        {
            _actionCount++;
            action.Parameters.TryGetValue("detail", out var detail);
            Console.WriteLine($"   ????? [Work] ÇALI?IYOR!");
            if (detail != null) Console.WriteLine($"       Detay: {detail}");
            return ActionResult.Successful("Work completed");
        });
    }
    
    public bool CanExecute(string actionType)
    {
        // Accept any action type - unknown ones will be logged
        return true;
    }
    
    public async Task<ActionResult> ExecuteAsync(AgentAction action, CancellationToken cancellationToken = default)
    {
        // Simulate async execution
        await Task.Delay(10, cancellationToken);
        
        if (_executor.CanExecute(action.ActionType))
        {
            return await _executor.ExecuteAsync(action, cancellationToken);
        }
        
        // Handle unknown actions gracefully
        _actionCount++;
        Console.WriteLine($"   ? [Unknown: {action.ActionType}] Bilinmeyen aksiyon!");
        action.Parameters.TryGetValue("detail", out var detail);
        if (detail != null) Console.WriteLine($"       Detay: {detail}");
        return ActionResult.Successful($"Unknown action '{action.ActionType}' logged");
    }
}
