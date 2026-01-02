using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using LivingInCalradia.Core.Application.Interfaces;
using LivingInCalradia.Core.Domain.ValueObjects;

namespace LivingInCalradia.Infrastructure.Bannerlord;

/// <summary>
/// Real implementation of IWorldSensor that reads from Bannerlord's Campaign system.
/// Extracts world state from TaleWorlds APIs.
/// </summary>
public sealed class BannerlordWorldSensor : IWorldSensor
{
    /// <summary>
    /// Perceives the current world state for a specific agent from Bannerlord's Campaign.
    /// </summary>
    public Task<WorldPerception> PerceiveWorldAsync(
        string agentId, 
        CancellationToken cancellationToken = default)
    {
        var perception = PerceiveWorld(agentId);
        return Task.FromResult(perception);
    }
    
    private WorldPerception PerceiveWorld(string agentId)
    {
        var campaign = Campaign.Current;
        
        if (campaign == null)
        {
            return CreateFallbackPerception(agentId);
        }
        
        // Find the hero by parsing agent ID
        var hero = FindHeroByAgentId(agentId);
        
        if (hero == null)
        {
            return CreateFallbackPerception(agentId);
        }
        
        // Get weather from current location
        var weather = GetWeatherCondition(hero);
        
        // Get economic state
        var economy = GetEconomicState(hero);
        
        // Get relations
        var relations = GetRelations(hero);
        
        // Get location
        var location = GetLocation(hero);
        
        return new WorldPerception(
            DateTime.UtcNow,
            weather,
            economy,
            relations,
            location);
    }
    
    private Hero? FindHeroByAgentId(string agentId)
    {
        if (Campaign.Current?.AliveHeroes == null)
            return null;
        
        // Parse agent ID format: "Type_Name_Faction"
        var parts = agentId.Split('_');
        if (parts.Length < 2)
            return null;
        
        var heroName = parts.Length >= 2 ? parts[1] : agentId;
        
        // Find hero by name match
        foreach (var hero in Campaign.Current.AliveHeroes)
        {
            if (hero?.Name?.ToString()?.Contains(heroName) == true)
            {
                return hero;
            }
        }
        
        return null;
    }
    
    private WeatherCondition GetWeatherCondition(Hero hero)
    {
        try
        {
            // Simplified weather based on season
            var season = CampaignTime.Now.GetSeasonOfYear;
            
            var (weatherType, temperature) = season switch
            {
                CampaignTime.Seasons.Winter => ("Snowy", -5),
                CampaignTime.Seasons.Spring => ("Rainy", 15),
                CampaignTime.Seasons.Summer => ("Clear", 28),
                CampaignTime.Seasons.Autumn => ("Cloudy", 12),
                _ => ("Clear", 20)
            };
            
            return new WeatherCondition(weatherType, temperature);
        }
        catch
        {
            return new WeatherCondition("Clear", 20);
        }
    }
    
    private EconomicState GetEconomicState(Hero hero)
    {
        try
        {
            var settlement = hero.CurrentSettlement ?? hero.HomeSettlement;
            
            if (settlement?.Town != null)
            {
                var town = settlement.Town;
                return new EconomicState(
                    prosperity: (int)town.Prosperity,
                    foodSupply: (int)town.FoodStocks,
                    taxRate: 15); // Default tax rate
            }
            else if (settlement?.Village != null)
            {
                var village = settlement.Village;
                return new EconomicState(
                    prosperity: (int)(village.Hearth * 10),
                    foodSupply: 100, // Villages don't track food directly
                    taxRate: 20); // Default village tax rate
            }
            
            // Default for heroes not in settlements
            return new EconomicState(
                prosperity: 3000,
                foodSupply: 100,
                taxRate: 15);
        }
        catch
        {
            return new EconomicState(3000, 100, 15);
        }
    }
    
    private Dictionary<string, int> GetRelations(Hero hero)
    {
        var relations = new Dictionary<string, int>();
        
        try
        {
            // Get relations with major factions
            if (Campaign.Current?.Kingdoms != null)
            {
                var count = 0;
                foreach (var kingdom in Campaign.Current.Kingdoms)
                {
                    if (kingdom.IsEliminated || count >= 5)
                        continue;
                    
                    var relationValue = GetFactionRelation(hero, kingdom);
                    relations[kingdom.Name.ToString()] = relationValue;
                    count++;
                }
            }
            
            // Add relation with player if hero is not player
            var mainHero = Hero.MainHero;
            if (hero != mainHero && mainHero != null)
            {
                var playerRelation = CharacterRelationManager.GetHeroRelation(hero, mainHero);
                relations["Player"] = playerRelation;
            }
        }
        catch
        {
            // Fallback relations
            relations["Empire"] = 0;
            relations["Vlandia"] = 0;
            relations["Sturgia"] = 0;
        }
        
        return relations;
    }
    
    private int GetFactionRelation(Hero hero, Kingdom kingdom)
    {
        try
        {
            // Check if hero's faction is at war or allied
            var heroFaction = hero.Clan?.Kingdom;
            
            if (heroFaction == null)
                return 0;
            
            if (heroFaction == kingdom)
                return 100; // Same faction
            
            if (heroFaction.IsAtWarWith(kingdom))
                return -100; // At war
            
            // Check stance - simplified
            var stance = heroFaction.GetStanceWith(kingdom);
            
            if (stance != null)
            {
                // Use stance value as base relation indicator
                var value = (float)stance.BehaviorPriority * 20;
                return (int)Math.Max(-50, Math.Min(50, value));
            }
            
            return 0; // Neutral
        }
        catch
        {
            return 0;
        }
    }
    
    private string GetLocation(Hero hero)
    {
        try
        {
            if (hero.CurrentSettlement != null)
            {
                var settlement = hero.CurrentSettlement;
                var settlementType = settlement.IsTown ? "City" : 
                                     settlement.IsCastle ? "Castle" : 
                                     settlement.IsVillage ? "Village" : "Settlement";
                
                return $"{settlement.Name} ({settlementType})";
            }
            
            if (hero.PartyBelongedTo != null)
            {
                var party = hero.PartyBelongedTo;
                
                if (party.BesiegedSettlement != null)
                {
                    return $"Siege of {party.BesiegedSettlement.Name}";
                }
                
                if (party.TargetSettlement != null)
                {
                    return $"En route to {party.TargetSettlement.Name}";
                }
                
                return "On campaign";
            }
            
            if (hero.HomeSettlement != null)
            {
                return $"{hero.HomeSettlement.Name} (Home)";
            }
            
            return "Calradia";
        }
        catch
        {
            return "Unknown Location";
        }
    }
    
    private WorldPerception CreateFallbackPerception(string agentId)
    {
        return new WorldPerception(
            DateTime.UtcNow,
            new WeatherCondition("Clear", 20),
            new EconomicState(3000, 100, 15),
            new Dictionary<string, int>
            {
                ["Empire"] = 0,
                ["Vlandia"] = 0,
                ["Sturgia"] = 0
            },
            "Calradia");
    }
}
