using System;
using System.Collections.Generic;

namespace LivingInCalradia.AI.Personality;

/// <summary>
/// Lord personality traits that influence AI decision making.
/// Each lord has a unique combination of traits that affects their behavior.
/// </summary>
public sealed class LordPersonality
{
    // Core Traits (0-100 scale)
    public int Valor { get; set; } = 50;        // Brave vs Cautious
    public int Honor { get; set; } = 50;        // Honorable vs Pragmatic
    public int Mercy { get; set; } = 50;        // Merciful vs Ruthless
    public int Generosity { get; set; } = 50;   // Generous vs Greedy
    public int Calculating { get; set; } = 50;  // Strategic vs Impulsive
    
    // Behavioral Traits
    public int Ambition { get; set; } = 50;     // Power-seeking vs Content
    public int Loyalty { get; set; } = 50;      // Loyal to faction vs Self-serving
    public int Aggression { get; set; } = 50;   // Warlike vs Peaceful
    
    // Social Traits
    public int Charm { get; set; } = 50;        // Diplomatic vs Blunt
    public int Pride { get; set; } = 50;        // Proud vs Humble
    
    /// <summary>
    /// Gets the personality description for AI prompts
    /// </summary>
    public string GetPersonalityDescription()
    {
        var traits = new List<string>();
        
        // Valor
        if (Valor >= 75) traits.Add("BRAVE and FEARLESS - never backs down from a fight");
        else if (Valor >= 60) traits.Add("courageous in battle");
        else if (Valor <= 25) traits.Add("CAUTIOUS and RISK-AVERSE - avoids unnecessary danger");
        else if (Valor <= 40) traits.Add("prefers to fight only when odds are favorable");
        
        // Honor
        if (Honor >= 75) traits.Add("HONORABLE - keeps promises, fights fairly, values reputation");
        else if (Honor >= 60) traits.Add("respects tradition and oaths");
        else if (Honor <= 25) traits.Add("PRAGMATIC - will break promises if beneficial");
        else if (Honor <= 40) traits.Add("flexible with moral boundaries");
        
        // Mercy
        if (Mercy >= 75) traits.Add("MERCIFUL - spares enemies, releases prisoners");
        else if (Mercy >= 60) traits.Add("shows compassion when possible");
        else if (Mercy <= 25) traits.Add("RUTHLESS - shows no mercy to enemies");
        else if (Mercy <= 40) traits.Add("harsh but not cruel");
        
        // Generosity
        if (Generosity >= 75) traits.Add("GENEROUS - shares wealth, rewards followers well");
        else if (Generosity >= 60) traits.Add("fair with gold distribution");
        else if (Generosity <= 25) traits.Add("GREEDY - hoards gold, reluctant to spend");
        else if (Generosity <= 40) traits.Add("careful with money");
        
        // Calculating
        if (Calculating >= 75) traits.Add("CALCULATING - thinks ahead, strategic");
        else if (Calculating >= 60) traits.Add("plans before acting");
        else if (Calculating <= 25) traits.Add("IMPULSIVE - acts on emotion");
        else if (Calculating <= 40) traits.Add("sometimes acts rashly");
        
        // Ambition
        if (Ambition >= 75) traits.Add("AMBITIOUS - desires power and glory");
        else if (Ambition >= 60) traits.Add("seeks advancement");
        else if (Ambition <= 25) traits.Add("CONTENT - happy with current position");
        else if (Ambition <= 40) traits.Add("modest goals");
        
        // Loyalty
        if (Loyalty >= 75) traits.Add("LOYAL - devoted to kingdom and liege");
        else if (Loyalty >= 60) traits.Add("generally faithful");
        else if (Loyalty <= 25) traits.Add("SELF-SERVING - will defect if beneficial");
        else if (Loyalty <= 40) traits.Add("loyalty can be bought");
        
        // Aggression
        if (Aggression >= 75) traits.Add("WARLIKE - prefers military solutions");
        else if (Aggression >= 60) traits.Add("favors strength");
        else if (Aggression <= 25) traits.Add("PEACEFUL - prefers diplomacy");
        else if (Aggression <= 40) traits.Add("avoids conflict when possible");
        
        // Charm
        if (Charm >= 75) traits.Add("CHARISMATIC - skilled diplomat, persuasive");
        else if (Charm >= 60) traits.Add("socially adept");
        else if (Charm <= 25) traits.Add("BLUNT - poor with words, direct");
        else if (Charm <= 40) traits.Add("straightforward speaker");
        
        // Pride
        if (Pride >= 75) traits.Add("PROUD - sensitive to slights, values status");
        else if (Pride >= 60) traits.Add("conscious of reputation");
        else if (Pride <= 25) traits.Add("HUMBLE - doesn't seek glory");
        else if (Pride <= 40) traits.Add("modest demeanor");
        
        if (traits.Count == 0)
            return "balanced personality with no extreme traits";
        
        return string.Join(", ", traits);
    }
    
    /// <summary>
    /// Gets action tendencies based on personality
    /// </summary>
    public string GetActionTendencies()
    {
        var tendencies = new List<string>();
        
        // Combat tendencies
        if (Valor >= 70 && Aggression >= 70)
            tendencies.Add("Prefers ATTACK over defense");
        else if (Valor <= 30 || Aggression <= 30)
            tendencies.Add("Prefers DEFEND and RETREAT over risky attacks");
        
        // Economic tendencies
        if (Generosity <= 30)
            tendencies.Add("Reluctant to GIVE GOLD or PAY RANSOM");
        else if (Generosity >= 70)
            tendencies.Add("Willing to spend gold for allies and causes");
        
        // Diplomatic tendencies
        if (Honor >= 70 && Loyalty >= 70)
            tendencies.Add("Will NOT DEFECT from kingdom easily");
        else if (Loyalty <= 30 && Ambition >= 70)
            tendencies.Add("May DEFECT if offered better position");
        
        // Mercy tendencies
        if (Mercy >= 70)
            tendencies.Add("Prefers to release prisoners");
        else if (Mercy <= 30)
            tendencies.Add("May execute or ransom prisoners");
        
        // Marriage tendencies
        if (Calculating >= 70)
            tendencies.Add("MARRIAGE decisions based on strategic value");
        else if (Calculating <= 30)
            tendencies.Add("May marry for love or impulse");
        
        if (tendencies.Count == 0)
            return "No strong action preferences";
        
        return string.Join(". ", tendencies);
    }
}

/// <summary>
/// Generates and caches lord personalities based on their characteristics.
/// Uses deterministic generation based on lord ID for consistency.
/// </summary>
public sealed class PersonalityGenerator
{
    private readonly Dictionary<string, LordPersonality> _personalities = new Dictionary<string, LordPersonality>();
    
    /// <summary>
    /// Gets or generates a personality for a lord.
    /// Personality is deterministic - same lord always gets same personality.
    /// </summary>
    public LordPersonality GetPersonality(string lordId, string? clanName = null, string? kingdomName = null, bool isFactionLeader = false, bool isKing = false)
    {
        if (_personalities.TryGetValue(lordId, out var existing))
            return existing;
        
        var personality = GeneratePersonality(lordId, clanName, kingdomName, isFactionLeader, isKing);
        _personalities[lordId] = personality;
        return personality;
    }
    
    private LordPersonality GeneratePersonality(string lordId, string? clanName, string? kingdomName, bool isFactionLeader, bool isKing)
    {
        // Use hash of lordId for deterministic randomness
        var hash = GetStableHash(lordId);
        var random = new Random(hash);
        
        var personality = new LordPersonality
        {
            // Generate base traits with some randomness
            Valor = random.Next(20, 80),
            Honor = random.Next(20, 80),
            Mercy = random.Next(20, 80),
            Generosity = random.Next(20, 80),
            Calculating = random.Next(20, 80),
            Ambition = random.Next(20, 80),
            Loyalty = random.Next(30, 90),
            Aggression = random.Next(20, 80),
            Charm = random.Next(20, 80),
            Pride = random.Next(20, 80)
        };
        
        // Adjust based on role
        if (isKing)
        {
            // Kings are more ambitious, proud, and calculating
            personality.Ambition = Math.Min(100, personality.Ambition + 20);
            personality.Pride = Math.Min(100, personality.Pride + 15);
            personality.Calculating = Math.Min(100, personality.Calculating + 10);
        }
        else if (isFactionLeader)
        {
            // Clan leaders are more ambitious
            personality.Ambition = Math.Min(100, personality.Ambition + 10);
            personality.Loyalty = Math.Min(100, personality.Loyalty + 10);
        }
        
        // Adjust based on kingdom culture (if known)
        if (!string.IsNullOrEmpty(kingdomName))
        {
            ApplyCultureModifiers(personality, kingdomName);
        }
        
        return personality;
    }
    
    private void ApplyCultureModifiers(LordPersonality personality, string kingdomName)
    {
        var kingdom = kingdomName.ToLowerInvariant();
        
        if (kingdom.Contains("battania") || kingdom.Contains("celtic"))
        {
            // Battanians: Brave, proud, traditional
            personality.Valor = Math.Min(100, personality.Valor + 15);
            personality.Pride = Math.Min(100, personality.Pride + 10);
            personality.Honor = Math.Min(100, personality.Honor + 5);
        }
        else if (kingdom.Contains("vlandia") || kingdom.Contains("feudal"))
        {
            // Vlandians: Ambitious, calculating knights
            personality.Ambition = Math.Min(100, personality.Ambition + 10);
            personality.Calculating = Math.Min(100, personality.Calculating + 10);
            personality.Honor = Math.Min(100, personality.Honor + 5);
        }
        else if (kingdom.Contains("empire") || kingdom.Contains("roman"))
        {
            // Empire: Calculating, diplomatic, civilized
            personality.Calculating = Math.Min(100, personality.Calculating + 15);
            personality.Charm = Math.Min(100, personality.Charm + 10);
            personality.Aggression = Math.Max(0, personality.Aggression - 5);
        }
        else if (kingdom.Contains("sturgia") || kingdom.Contains("nord"))
        {
            // Sturgians: Brave, warlike, loyal
            personality.Valor = Math.Min(100, personality.Valor + 20);
            personality.Aggression = Math.Min(100, personality.Aggression + 15);
            personality.Loyalty = Math.Min(100, personality.Loyalty + 10);
        }
        else if (kingdom.Contains("khuzait") || kingdom.Contains("mongol"))
        {
            // Khuzaits: Swift, pragmatic, ambitious
            personality.Calculating = Math.Min(100, personality.Calculating + 10);
            personality.Ambition = Math.Min(100, personality.Ambition + 10);
            personality.Honor = Math.Max(0, personality.Honor - 10); // More pragmatic
        }
        else if (kingdom.Contains("aserai") || kingdom.Contains("desert"))
        {
            // Aserai: Charming merchants, generous
            personality.Charm = Math.Min(100, personality.Charm + 15);
            personality.Generosity = Math.Min(100, personality.Generosity + 10);
            personality.Calculating = Math.Min(100, personality.Calculating + 5);
        }
    }
    
    private int GetStableHash(string text)
    {
        // Simple stable hash that won't change between runs
        unchecked
        {
            int hash = 17;
            foreach (char c in text)
            {
                hash = hash * 31 + c;
            }
            return Math.Abs(hash);
        }
    }
}
