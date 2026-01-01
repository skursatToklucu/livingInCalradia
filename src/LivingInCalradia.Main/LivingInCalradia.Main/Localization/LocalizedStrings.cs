namespace LivingInCalradia.Main.Localization;

/// <summary>
/// Localized strings for the mod UI and logs.
/// Supports Turkish (tr) and English (en).
/// </summary>
public static class LocalizedStrings
{
    private static string _language = "en";
    
    /// <summary>
    /// Sets the current language. Call this once during initialization.
    /// </summary>
    public static void SetLanguage(string language)
    {
        _language = language?.ToLowerInvariant() == "tr" ? "tr" : "en";
    }
    
    public static bool IsTurkish => _language == "tr";
    
    // Mod loading messages
    public static string ModLoading => IsTurkish ? "Mod yukleniyor..." : "Mod loading...";
    public static string ModLoaded => IsTurkish ? "Mod basariyla yuklendi!" : "Mod loaded successfully!";
    public static string ModUnloaded => IsTurkish ? "Mod kaldirildi." : "Mod unloaded.";
    public static string ModLoadError => IsTurkish ? "Mod yuklenirken hata" : "Error loading mod";
    public static string ModUnloadError => IsTurkish ? "Mod kaldirilirken hata" : "Error unloading mod";
    
    // Campaign messages
    public static string CampaignStarting => IsTurkish 
        ? "Campaign baslatiliyor, AI sistemi ve Diyalog sistemi hazirlaniyor..." 
        : "Campaign starting, AI system and Dialogue system preparing...";
    public static string GameEnded => IsTurkish 
        ? "Oyun sona erdi, AI sistemi kapatildi." 
        : "Game ended, AI system closed.";
    
    // AI system messages
    public static string AISystemStarting => IsTurkish ? "AI sistemi baslatiliyor..." : "AI system starting...";
    public static string AISystemStarted => IsTurkish ? "AI sistemi basariyla baslatildi!" : "AI system started successfully!";
    public static string AISystemNotReady => IsTurkish ? "AI sistemi henuz hazir degil!" : "AI system not ready yet!";
    public static string AISystemError => IsTurkish ? "AI sistemi baslatilamadi" : "Failed to start AI system";
    
    // Language detection
    public static string LanguageDetected(string detected, string effective) => IsTurkish 
        ? $"Oyun dili algilandi: {detected} -> {effective}" 
        : $"Game language detected: {detected} -> {effective}";
    
    // Test messages
    public static string FullProofTestStarting => IsTurkish ? "Full Proof Test baslatiliyor..." : "Full Proof Test starting...";
    public static string QuickTestStarting => IsTurkish ? "Hizli Proof Test baslatiliyor..." : "Quick Proof Test starting...";
    
    // Lord thinking
    public static string LordNotFound => IsTurkish ? "Dusunecek lord bulunamadi!" : "No lord found to think!";
    public static string LordThinking(string name) => IsTurkish ? $"{name} dusunuyor..." : $"{name} is thinking...";
    public static string LordDecidedToWait(string name) => IsTurkish ? $"{name} beklemeye karar verdi" : $"{name} decided to wait";
    
    // Pause messages
    public static string GamePausedAICancelled => IsTurkish 
        ? "Oyun duraklatildi, AI islemi iptal edildi." 
        : "Game paused, AI operation cancelled.";
    public static string GamePausedLoopStopped => IsTurkish 
        ? "Oyun duraklatildi, AI dongusu durduruluyor." 
        : "Game paused, AI loop stopping.";
    public static string GamePausedResultNotShown => IsTurkish 
        ? "Oyun duraklatildi, sonuc uygulanmayacak." 
        : "Game paused, result will not be applied.";
    public static string GamePausedAIResultHidden => IsTurkish 
        ? "Oyun duraklatildi, AI sonucu gosterilmeyecek." 
        : "Game paused, AI result will not be shown.";
    
    // Workflow messages
    public static string WorkflowFailed => IsTurkish ? "Workflow basarisiz" : "Workflow failed";
    public static string AIThinkingError => IsTurkish ? "AI dusunme hatasi" : "AI thinking error";
    public static string AIProcessingError => IsTurkish ? "AI isleme hatasi" : "AI processing error";
    
    // Hero titles
    public static string King => IsTurkish ? "Kral " : "King ";
    public static string Lord => "Lord ";
    public static string Notable => "Notable ";
    
    // Active message
    public static string ModActiveMessage(string langText) => IsTurkish 
        ? $"Living in Calradia AI aktif! ({langText}) NumPad1-5 ile test edin." 
        : $"Living in Calradia AI active! ({langText}) Test with NumPad1-5.";
    
    // Error prefix
    public static string Error => IsTurkish ? "HATA" : "ERROR";
}
