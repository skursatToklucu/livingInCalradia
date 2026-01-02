namespace LivingInCalradia.Main.Localization;

/// <summary>
/// Localized strings for the mod UI and logs.
/// English only.
/// </summary>
public static class LocalizedStrings
{
    /// <summary>
    /// Sets the current language. Always English.
    /// </summary>
    public static void SetLanguage(string language)
    {
        // English only - no-op
    }
    
    public static bool IsTurkish => false;
    
    // Mod loading messages
    public static string ModLoading => "Mod loading...";
    public static string ModLoaded => "Mod loaded successfully!";
    public static string ModUnloaded => "Mod unloaded.";
    public static string ModLoadError => "Error loading mod";
    public static string ModUnloadError => "Error unloading mod";
    
    // Campaign messages
    public static string CampaignStarting => "Campaign starting, AI system and Dialogue system preparing...";
    public static string GameEnded => "Game ended, AI system closed.";
    
    // AI system messages
    public static string AISystemStarting => "AI system starting...";
    public static string AISystemStarted => "AI system started successfully!";
    public static string AISystemNotReady => "AI system not ready yet!";
    public static string AISystemError => "Failed to start AI system";
    
    // Language detection
    public static string LanguageDetected(string detected, string effective) => 
        $"Game language detected: {detected} -> {effective}";
    
    // Test messages
    public static string FullProofTestStarting => "Full Proof Test starting...";
    public static string QuickTestStarting => "Quick Proof Test starting...";
    
    // Lord thinking
    public static string LordNotFound => "No lord found to think!";
    public static string LordThinking(string name) => $"{name} is thinking...";
    public static string LordDecidedToWait(string name) => $"{name} decided to wait";
    
    // Pause messages
    public static string GamePausedAICancelled => "Game paused, AI operation cancelled.";
    public static string GamePausedLoopStopped => "Game paused, AI loop stopping.";
    public static string GamePausedResultNotShown => "Game paused, result will not be applied.";
    public static string GamePausedAIResultHidden => "Game paused, AI result will not be shown.";
    
    // Workflow messages
    public static string WorkflowFailed => "Workflow failed";
    public static string AIThinkingError => "AI thinking error";
    public static string AIProcessingError => "AI processing error";
    
    // Hero titles
    public static string King => "King ";
    public static string Lord => "Lord ";
    public static string Notable => "Notable ";
    
    // Active message
    public static string ModActiveMessage(string langText) => 
        $"Living in Calradia AI active! ({langText}) Test with NumPad1-5.";
    
    // Error prefix
    public static string Error => "ERROR";
}
