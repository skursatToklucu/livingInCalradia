using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;
using LivingInCalradia.AI.Configuration;

namespace LivingInCalradia.Main.UI;

/// <summary>
/// In-game settings panel for Living in Calradia mod.
/// Uses simple Yes/No inquiries for configuration.
/// ai-config.json is managed internally - NEVER exposed to users.
/// </summary>
public static class SettingsPanel
{
    private static AIConfiguration? _config;
    private static Action? _onConfigChanged;
    
    /// <summary>
    /// Initialize with config reference for saving changes
    /// </summary>
    public static void Initialize(AIConfiguration config, Action? onConfigChanged = null)
    {
        _config = config;
        _onConfigChanged = onConfigChanged;
    }
    
    /// <summary>
    /// Shows the main settings panel popup.
    /// </summary>
    public static void ShowPanel()
    {
        var bindings = Input.LivingInCalradiaHotKeys.GetKeyBindings();
        
        // Check if hotkeys are already configured
        var isConfigured = bindings.ContainsKey("Full Proof Test") && bindings["Full Proof Test"] != "None";
        
        // Build the content text
        var sb = new StringBuilder();
        sb.AppendLine("Current Hotkey Bindings:");
        sb.AppendLine();
        
        foreach (var binding in bindings)
        {
            var keyDisplay = binding.Value;
            var status = keyDisplay == "None" ? "(disabled)" : "";
            sb.AppendLine($"  [{keyDisplay}] {binding.Key} {status}");
        }
        
        sb.AppendLine();
        sb.AppendLine("-----------------------------");
        
        if (!isConfigured)
        {
            sb.AppendLine("Hotkeys not configured yet!");
            sb.AppendLine("Apply default NumPad 1-5 setup?");
        }
        else
        {
            sb.AppendLine("Hotkeys are configured.");
            sb.AppendLine("Reset to default if needed.");
        }
        
        // Show inquiry with quick setup option
        InformationManager.ShowInquiry(
            new InquiryData(
                titleText: "Living in Calradia - Settings",
                text: sb.ToString(),
                isAffirmativeOptionShown: true,
                isNegativeOptionShown: true,
                affirmativeText: isConfigured ? "Reset to Default" : "Apply NumPad 1-5",
                negativeText: "Close",
                affirmativeAction: ApplyQuickSetup,
                negativeAction: null,
                soundEventPath: ""
            ),
            pauseGameActiveState: false
        );
        
        Debug.Print("[LivingInCalradia] Settings panel opened");
    }
    
    /// <summary>
    /// Applies quick setup - assigns NumPad 1-5 to all functions
    /// </summary>
    private static void ApplyQuickSetup()
    {
        if (_config == null) return;
        
        // Assign NumPad keys (default setup)
        _config.HotkeyFullProofTest = "NumPad1";
        _config.HotkeyTriggerAI = "NumPad2";
        _config.HotkeyQuickTest = "NumPad3";
        _config.HotkeyToggleLogs = "NumPad4";
        _config.HotkeyShowThoughts = "NumPad5";
        // Keep Insert for settings
        _config.HotkeyShowSettings = "Insert";
        
        // Reinitialize hotkeys
        Input.LivingInCalradiaHotKeys.InitializeFromConfig(
            _config.HotkeyFullProofTest,
            _config.HotkeyTriggerAI,
            _config.HotkeyQuickTest,
            _config.HotkeyToggleLogs,
            _config.HotkeyShowThoughts,
            _config.HotkeyShowSettings);
        
        // Save to file (internal - not exposed to user)
        _onConfigChanged?.Invoke();
        
        // Show success with all bindings
        var sb = new StringBuilder();
        sb.AppendLine("Hotkeys configured successfully!");
        sb.AppendLine();
        sb.AppendLine("  [NumPad1] Full AI Test");
        sb.AppendLine("  [NumPad2] Trigger Lord AI");
        sb.AppendLine("  [NumPad3] Quick Test");
        sb.AppendLine("  [NumPad4] Toggle Logs");
        sb.AppendLine("  [NumPad5] Thoughts Panel");
        sb.AppendLine("  [Insert] Settings (this menu)");
        sb.AppendLine();
        sb.AppendLine("Settings saved automatically.");
        
        InformationManager.ShowInquiry(
            new InquiryData(
                titleText: "Setup Complete!",
                text: sb.ToString(),
                isAffirmativeOptionShown: true,
                isNegativeOptionShown: false,
                affirmativeText: "OK",
                negativeText: "",
                affirmativeAction: null,
                negativeAction: null,
                soundEventPath: ""
            ),
            pauseGameActiveState: false
        );
        
        // Also show in game log
        InformationManager.DisplayMessage(new InformationMessage(
            "[LivingInCalradia] Hotkeys: NumPad1-5 configured!",
            Colors.Green));
    }
    
    /// <summary>
    /// Shows a quick status of current settings on game start.
    /// Also auto-applies default hotkeys if not configured.
    /// </summary>
    public static void ShowQuickStatus()
    {
        var bindings = Input.LivingInCalradiaHotKeys.GetKeyBindings();
        var settingsKey = bindings.ContainsKey("Show Settings") ? bindings["Show Settings"] : "Insert";
        
        // Check if hotkeys are configured
        var isConfigured = bindings.ContainsKey("Full Proof Test") && bindings["Full Proof Test"] != "None";
        
        if (!isConfigured && _config != null)
        {
            // Auto-apply default setup on first run
            ApplyDefaultSetupSilent();
            
            InformationManager.DisplayMessage(new InformationMessage(
                "[LivingInCalradia] Default hotkeys applied: NumPad1-5",
                Colors.Green));
        }
        
        InformationManager.DisplayMessage(new InformationMessage(
            $"[LivingInCalradia] Press [{settingsKey}] for settings",
            Colors.Cyan));
    }
    
    /// <summary>
    /// Silently applies default hotkey setup without showing popup
    /// </summary>
    private static void ApplyDefaultSetupSilent()
    {
        if (_config == null) return;
        
        _config.HotkeyFullProofTest = "NumPad1";
        _config.HotkeyTriggerAI = "NumPad2";
        _config.HotkeyQuickTest = "NumPad3";
        _config.HotkeyToggleLogs = "NumPad4";
        _config.HotkeyShowThoughts = "NumPad5";
        _config.HotkeyShowSettings = "Insert";
        
        Input.LivingInCalradiaHotKeys.InitializeFromConfig(
            _config.HotkeyFullProofTest,
            _config.HotkeyTriggerAI,
            _config.HotkeyQuickTest,
            _config.HotkeyToggleLogs,
            _config.HotkeyShowThoughts,
            _config.HotkeyShowSettings);
        
        _onConfigChanged?.Invoke();
    }
}
