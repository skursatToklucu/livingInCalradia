using System;
using System.Collections.Generic;
using System.Linq;

namespace LivingInCalradia.Core.Domain.ValueObjects;

/// <summary>
/// Represents the perceived world state by an agent.
/// Immutable value object following DDD principles.
/// </summary>
public sealed class WorldPerception
{
    public DateTime Timestamp { get; }
    public WeatherCondition Weather { get; }
    public EconomicState Economy { get; }
    public IDictionary<string, int> Relations { get; }
    public string Location { get; }

    public WorldPerception(
        DateTime timestamp,
        WeatherCondition weather,
        EconomicState economy,
        IDictionary<string, int> relations,
        string location)
    {
        Timestamp = timestamp;
        Weather = weather;
        Economy = economy;
        Relations = new Dictionary<string, int>(relations);
        Location = location ?? throw new ArgumentNullException(nameof(location));
    }

    public string ToSemanticContext()
    {
        var relationsText = string.Join(", ", Relations.Select(r => $"{r.Key}: {r.Value}"));

        return $@"Current Time: {Timestamp:yyyy-MM-dd HH:mm}
Location: {Location}
Weather: {Weather}
Economy: Prosperity {Economy.Prosperity}, Food {Economy.FoodSupply}
Relations: {relationsText}";
    }
}

public sealed class WeatherCondition
{
    public string Type { get; }
    public int Temperature { get; }

    public WeatherCondition(string type, int temperature)
    {
        Type = type ?? "Clear";
        Temperature = temperature;
    }

    public override string ToString() => $"{Type} ({Temperature}°C)";
}

public sealed class EconomicState
{
    public int Prosperity { get; }
    public int FoodSupply { get; }
    public int TaxRate { get; }

    public EconomicState(int prosperity, int foodSupply, int taxRate)
    {
        Prosperity = prosperity;
        FoodSupply = foodSupply;
        TaxRate = taxRate;
    }
}
