using System;
using System.Collections.Generic;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;

namespace LivingInCalradia.Main.Input;

/// <summary>
/// Configurable hotkey system for Living in Calradia mod.
/// Hotkeys can be customized via ai-config.json file.
/// Press Insert (default) to see current bindings.
/// </summary>
public static class LivingInCalradiaHotKeys
{
    // Default key mappings - Most are None (disabled) by default
    private static InputKey _fullProofTestKey = InputKey.Invalid;
    private static InputKey _triggerAIKey = InputKey.Invalid;
    private static InputKey _quickTestKey = InputKey.Invalid;
    private static InputKey _toggleLogsKey = InputKey.Invalid;
    private static InputKey _showThoughtsKey = InputKey.Invalid;
    private static InputKey _showSettingsKey = InputKey.Insert; // Changed from F10 to Insert
    
    // Key state tracking to prevent repeated triggers while holding
    private static readonly Dictionary<InputKey, bool> _keyWasDown = new Dictionary<InputKey, bool>();
    
    private static bool _isInitialized;
    
    /// <summary>
    /// Initializes the hotkey system with default key mappings.
    /// Insert is always enabled for settings.
    /// </summary>
    public static void Initialize()
    {
        if (_isInitialized) return;
        
        _showSettingsKey = InputKey.Insert;
        _isInitialized = true;
        
        Debug.Print("[LivingInCalradia] Hotkeys initialized - Press Insert for settings");
    }
    
    /// <summary>
    /// Initializes the hotkey system with custom key mappings from config.
    /// </summary>
    public static void InitializeFromConfig(
        string? fullProofTestKey,
        string? triggerAIKey,
        string? quickTestKey,
        string? toggleLogsKey,
        string? showThoughtsKey,
        string? showSettingsKey)
    {
        _fullProofTestKey = ParseKey(fullProofTestKey, InputKey.Invalid);
        _triggerAIKey = ParseKey(triggerAIKey, InputKey.Invalid);
        _quickTestKey = ParseKey(quickTestKey, InputKey.Invalid);
        _toggleLogsKey = ParseKey(toggleLogsKey, InputKey.Invalid);
        _showThoughtsKey = ParseKey(showThoughtsKey, InputKey.Invalid);
        
        // Insert is default for settings (F10 might conflict with game)
        if (!string.IsNullOrWhiteSpace(showSettingsKey) && 
            !showSettingsKey.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            _showSettingsKey = ParseKey(showSettingsKey, InputKey.Insert);
        }
        else
        {
            _showSettingsKey = InputKey.Insert;
        }
        
        // Clear key states
        _keyWasDown.Clear();
        
        _isInitialized = true;
        
        Debug.Print($"[LivingInCalradia] Hotkeys initialized:");
        Debug.Print($"  Settings={_showSettingsKey}");
        Debug.Print($"  FullProofTest={_fullProofTestKey}");
        Debug.Print($"  TriggerAI={_triggerAIKey}");
        Debug.Print($"  QuickTest={_quickTestKey}");
        Debug.Print($"  ToggleLogs={_toggleLogsKey}");
        Debug.Print($"  ShowThoughts={_showThoughtsKey}");
    }
    
    private static InputKey ParseKey(string? keyName, InputKey defaultKey)
    {
        if (string.IsNullOrWhiteSpace(keyName))
            return defaultKey;
        
        if (keyName.Equals("None", StringComparison.OrdinalIgnoreCase))
            return InputKey.Invalid;
        
        if (Enum.TryParse<InputKey>(keyName, true, out var result))
            return result;
        
        Debug.Print($"[LivingInCalradia] Unknown key: {keyName}, using default: {defaultKey}");
        return defaultKey;
    }
    
    /// <summary>
    /// Checks if a key was just pressed (transition from up to down).
    /// Uses state tracking to fire only once per press.
    /// </summary>
    private static bool IsKeyJustPressed(InputKey key)
    {
        if (key == InputKey.Invalid) return false;
        
        try
        {
            bool isCurrentlyDown = TaleWorlds.InputSystem.Input.IsKeyDown(key);
            
            // Get previous state
            bool wasDown = false;
            if (_keyWasDown.ContainsKey(key))
            {
                wasDown = _keyWasDown[key];
            }
            
            // Update state
            _keyWasDown[key] = isCurrentlyDown;
            
            // Return true only on transition from up to down
            bool justPressed = isCurrentlyDown && !wasDown;
            
            if (justPressed)
            {
                Debug.Print($"[LivingInCalradia] Key pressed: {key}");
            }
            
            return justPressed;
        }
        catch (Exception ex)
        {
            Debug.Print($"[LivingInCalradia] Key check error: {ex.Message}");
            return false;
        }
    }
    
    public static bool IsFullProofTestPressed()
    {
        return IsKeyJustPressed(_fullProofTestKey);
    }
    
    public static bool IsTriggerSingleLordAIPressed()
    {
        return IsKeyJustPressed(_triggerAIKey);
    }
    
    public static bool IsQuickTestPressed()
    {
        return IsKeyJustPressed(_quickTestKey);
    }
    
    public static bool IsToggleLogsPressed()
    {
        return IsKeyJustPressed(_toggleLogsKey);
    }
    
    public static bool IsShowThoughtsPanelPressed()
    {
        return IsKeyJustPressed(_showThoughtsKey);
    }
    
    public static bool IsShowSettingsPressed()
    {
        // Always use the configured key (default: Insert)
        return IsKeyJustPressed(_showSettingsKey);
    }
    
    public static Dictionary<string, string> GetKeyBindings()
    {
        var settingsKey = _showSettingsKey == InputKey.Invalid ? "Insert" : _showSettingsKey.ToString();
        
        return new Dictionary<string, string>
        {
            { "Show Settings", settingsKey },
            { "Full Proof Test", _fullProofTestKey == InputKey.Invalid ? "None" : _fullProofTestKey.ToString() },
            { "Single Lord AI", _triggerAIKey == InputKey.Invalid ? "None" : _triggerAIKey.ToString() },
            { "Quick Test", _quickTestKey == InputKey.Invalid ? "None" : _quickTestKey.ToString() },
            { "Toggle Logs", _toggleLogsKey == InputKey.Invalid ? "None" : _toggleLogsKey.ToString() },
            { "Thoughts Panel", _showThoughtsKey == InputKey.Invalid ? "None" : _showThoughtsKey.ToString() }
        };
    }
}
