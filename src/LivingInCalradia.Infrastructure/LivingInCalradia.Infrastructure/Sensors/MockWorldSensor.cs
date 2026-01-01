using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LivingInCalradia.Core.Application.Interfaces;
using LivingInCalradia.Core.Domain.ValueObjects;

namespace LivingInCalradia.Infrastructure.Sensors;

/// <summary>
/// Mock implementation of IWorldSensor for testing and development.
/// Generates realistic test scenarios based on agent type.
/// </summary>
public sealed class MockWorldSensor : IWorldSensor
{
    private readonly Random _random = new Random();
    
    public async Task<WorldPerception> PerceiveWorldAsync(
        string agentId, 
        CancellationToken cancellationToken = default)
    {
        // Simulate async world state query
        await Task.Delay(50, cancellationToken);
        
        // Generate scenario-specific perception based on agent ID
        return GeneratePerceptionForAgent(agentId);
    }
    
    private WorldPerception GeneratePerceptionForAgent(string agentId)
    {
        var agentLower = agentId.ToLowerInvariant();
        
        // Determine scenario based on agent type
        if (agentLower.Contains("king") || agentLower.Contains("lord"))
        {
            return GenerateLordScenario(agentId);
        }
        else if (agentLower.Contains("merchant") || agentLower.Contains("trader"))
        {
            return GenerateMerchantScenario(agentId);
        }
        else if (agentLower.Contains("commander") || agentLower.Contains("general"))
        {
            return GenerateCommanderScenario(agentId);
        }
        else if (agentLower.Contains("villager") || agentLower.Contains("peasant"))
        {
            return GenerateVillagerScenario(agentId);
        }
        else if (agentLower.Contains("archer") || agentLower.Contains("soldier"))
        {
            return GenerateSoldierScenario(agentId);
        }
        
        return GenerateDefaultScenario(agentId);
    }
    
    private WorldPerception GenerateLordScenario(string agentId)
    {
        // Lord scenario: Political decisions, war/peace, alliances
        var weather = new WeatherCondition("Clear", 18);
        var economy = new EconomicState(
            prosperity: _random.Next(3000, 6000),
            foodSupply: _random.Next(100, 300),
            taxRate: _random.Next(10, 25));
        
        var relations = new Dictionary<string, int>
        {
            ["Empire"] = _random.Next(-80, -20),      // Dü?man
            ["Vlandia"] = _random.Next(-50, 30),      // Belirsiz
            ["Sturgia"] = _random.Next(20, 80),       // Müttefik
            ["Aserai"] = _random.Next(-30, 50),       // Nötr
            ["Khuzait"] = _random.Next(-100, -50)     // Sava?ta
        };
        
        var location = GetLocationForFaction(agentId);
        
        Console.WriteLine($"[Sensor] Lord senaryosu: {location}, Dü?manlar yak?n!");
        
        return new WorldPerception(DateTime.UtcNow, weather, economy, relations, location);
    }
    
    private WorldPerception GenerateMerchantScenario(string agentId)
    {
        // Merchant scenario: Trade opportunities, market prices
        var weather = new WeatherCondition("Sunny", 22);
        var economy = new EconomicState(
            prosperity: _random.Next(4000, 8000),  // Zengin ?ehir
            foodSupply: _random.Next(50, 150),
            taxRate: _random.Next(5, 15));         // Dü?ük vergi
        
        var relations = new Dictionary<string, int>
        {
            ["LocalGuild"] = _random.Next(50, 100),
            ["Nobility"] = _random.Next(20, 60),
            ["CommonFolk"] = _random.Next(60, 90),
            ["Bandits"] = _random.Next(-100, -70)
        };
        
        var location = "Pravend Pazar?";
        
        Console.WriteLine($"[Sensor] Tüccar senaryosu: {location}, Ticaret f?rsatlar?!");
        
        return new WorldPerception(DateTime.UtcNow, weather, economy, relations, location);
    }
    
    private WorldPerception GenerateCommanderScenario(string agentId)
    {
        // Commander scenario: Battle situations, troop management
        var weather = new WeatherCondition("Stormy", 12);
        var economy = new EconomicState(
            prosperity: _random.Next(1000, 3000),  // Sava? bölgesi
            foodSupply: _random.Next(20, 80),      // K?tl?k
            taxRate: _random.Next(20, 40));        // Sava? vergisi
        
        var relations = new Dictionary<string, int>
        {
            ["EnemyArmy"] = -100,
            ["AlliedForces"] = _random.Next(70, 100),
            ["LocalPopulation"] = _random.Next(-20, 40),
            ["Mercenaries"] = _random.Next(30, 70)
        };
        
        var location = "Sava? Cephesi - Ku?atma Alt?nda";
        
        Console.WriteLine($"[Sensor] Komutan senaryosu: {location}, Sava? durumu!");
        
        return new WorldPerception(DateTime.UtcNow, weather, economy, relations, location);
    }
    
    private WorldPerception GenerateVillagerScenario(string agentId)
    {
        // Villager scenario: Daily survival, local issues
        var weather = new WeatherCondition("Rainy", 8);
        var economy = new EconomicState(
            prosperity: _random.Next(500, 1500),   // Fakir köy
            foodSupply: _random.Next(30, 70),
            taxRate: _random.Next(25, 40));        // A??r vergi
        
        var relations = new Dictionary<string, int>
        {
            ["VillageLord"] = _random.Next(-30, 20),
            ["OtherVillagers"] = _random.Next(50, 80),
            ["TaxCollector"] = _random.Next(-80, -40),
            ["Bandits"] = _random.Next(-100, -60)
        };
        
        var location = "Omor Köyü";
        
        Console.WriteLine($"[Sensor] Köylü senaryosu: {location}, Zor zamanlar!");
        
        return new WorldPerception(DateTime.UtcNow, weather, economy, relations, location);
    }
    
    private WorldPerception GenerateSoldierScenario(string agentId)
    {
        // Soldier scenario: Military life, loyalty, survival
        var weather = new WeatherCondition("Windy", 5);
        var economy = new EconomicState(
            prosperity: _random.Next(2000, 4000),
            foodSupply: _random.Next(40, 100),
            taxRate: 0);  // Askerler vergi ödemez
        
        var relations = new Dictionary<string, int>
        {
            ["Commander"] = _random.Next(60, 100),
            ["FellowSoldiers"] = _random.Next(70, 95),
            ["Enemy"] = -100,
            ["Civilians"] = _random.Next(20, 50)
        };
        
        var location = "Khuzait Bozk?rlar? - Devriye";
        
        Console.WriteLine($"[Sensor] Asker senaryosu: {location}, Tehlike yak?n!");
        
        return new WorldPerception(DateTime.UtcNow, weather, economy, relations, location);
    }
    
    private WorldPerception GenerateDefaultScenario(string agentId)
    {
        var weather = new WeatherCondition(GetRandomWeather(), _random.Next(-5, 30));
        var economy = new EconomicState(
            prosperity: _random.Next(1000, 5000),
            foodSupply: _random.Next(50, 200),
            taxRate: _random.Next(5, 30));
        
        var relations = new Dictionary<string, int>
        {
            ["Faction1"] = _random.Next(-100, 100),
            ["Faction2"] = _random.Next(-100, 100),
            ["Faction3"] = _random.Next(-100, 100)
        };
        
        return new WorldPerception(DateTime.UtcNow, weather, economy, relations, "Bilinmeyen Konum");
    }
    
    private string GetLocationForFaction(string agentId)
    {
        if (agentId.Contains("Battania")) return "Marunath Kalesi";
        if (agentId.Contains("Vlandia")) return "Pravend ?ehri";
        if (agentId.Contains("Empire")) return "Epicrotea";
        if (agentId.Contains("Sturgia")) return "Balgard";
        if (agentId.Contains("Aserai")) return "Qasira";
        if (agentId.Contains("Khuzait")) return "Makeb";
        return "Calradia";
    }
    
    private string GetRandomWeather()
    {
        var weatherTypes = new[] { "Clear", "Cloudy", "Rainy", "Snowy", "Foggy", "Stormy", "Windy" };
        return weatherTypes[_random.Next(weatherTypes.Length)];
    }
}
