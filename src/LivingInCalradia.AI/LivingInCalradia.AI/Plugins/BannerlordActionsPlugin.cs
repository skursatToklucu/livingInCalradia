using System.ComponentModel;
using Microsoft.SemanticKernel;
using System.Threading.Tasks;

namespace LivingInCalradia.AI.Plugins;

/// <summary>
/// Semantic Kernel Native Plugin for Bannerlord game actions.
/// Enables LLM to execute in-game operations through function calling.
/// </summary>
public sealed class BannerlordActionsPlugin
{
    [KernelFunction, Description("Starts a siege on a settlement")]
    public async Task<string> StartSiegeAsync(
        [Description("The ID of the settlement to siege")] string settlementId,
        [Description("The ID of the lord commanding the siege")] string lordId)
    {
        // This will be called by the game action executor in the main layer
        await Task.CompletedTask;
        return $"Initiated siege on {settlementId} by {lordId}";
    }

    [KernelFunction, Description("Gives gold to another character")]
    public async Task<string> GiveGoldAsync(
        [Description("The ID of the giver")] string giverId,
        [Description("The ID of the receiver")] string receiverId,
        [Description("Amount of gold to give")] int amount)
    {
        await Task.CompletedTask;
        return $"{giverId} gave {amount} gold to {receiverId}";
    }

    [KernelFunction, Description("Changes the relation between two characters")]
    public async Task<string> ChangeRelationAsync(
        [Description("The first character ID")] string character1Id,
        [Description("The second character ID")] string character2Id,
        [Description("The amount to change (positive or negative)")] int relationChange)
    {
        await Task.CompletedTask;
        return $"Relation between {character1Id} and {character2Id} changed by {relationChange}";
    }

    [KernelFunction, Description("Moves an army to a location")]
    public async Task<string> MoveArmyAsync(
        [Description("The ID of the army/party")] string armyId,
        [Description("The target location name")] string targetLocation)
    {
        await Task.CompletedTask;
        return $"Army {armyId} is moving to {targetLocation}";
    }

    [KernelFunction, Description("Recruits troops for an army")]
    public async Task<string> RecruitTroopsAsync(
        [Description("The ID of the army/party")] string armyId,
        [Description("Number of troops to recruit")] int troopCount,
        [Description("The settlement to recruit from")] string settlementId)
    {
        await Task.CompletedTask;
        return $"Recruited {troopCount} troops for {armyId} from {settlementId}";
    }
}
