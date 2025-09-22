namespace GameConsole.Configuration.Security;

/// <summary>
/// Represents a UI profile configuration that defines the visual and behavioral
/// settings for the user interface, supporting both TUI and GUI modes.
/// </summary>
public class UIProfileConfiguration
{
    /// <summary>
    /// Dictionary mapping UI setting keys to their values.
    /// </summary>
    public IReadOnlyDictionary<string, string> Settings { get; }
    
    /// <summary>
    /// Name of this UI profile configuration.
    /// </summary>
    public string ProfileName { get; }
    
    /// <summary>
    /// When this configuration was last modified.
    /// </summary>
    public DateTime LastModified { get; }
    
    /// <summary>
    /// The theme applied to this UI profile.
    /// </summary>
    public UITheme Theme { get; }
    
    /// <summary>
    /// The UI layout mode for this profile.
    /// </summary>
    public UILayoutMode LayoutMode { get; }
    
    private readonly Dictionary<string, string> _settings;
    
    /// <summary>
    /// Initializes a new instance of the UIProfileConfiguration class.
    /// </summary>
    /// <param name="profileName">Name of the profile.</param>
    /// <param name="theme">UI theme for the profile.</param>
    /// <param name="layoutMode">UI layout mode for the profile.</param>
    /// <param name="settings">Initial settings.</param>
    public UIProfileConfiguration(string profileName, UITheme theme = UITheme.Default, 
        UILayoutMode layoutMode = UILayoutMode.Auto, Dictionary<string, string>? settings = null)
    {
        ProfileName = profileName ?? throw new ArgumentNullException(nameof(profileName));
        Theme = theme;
        LayoutMode = layoutMode;
        _settings = settings ?? new Dictionary<string, string>();
        Settings = new System.Collections.ObjectModel.ReadOnlyDictionary<string, string>(_settings);
        LastModified = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Adds or updates a setting.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The setting value.</param>
    public void SetSetting(string key, string value)
    {
        _settings[key] = value;
    }
    
    /// <summary>
    /// Removes a setting.
    /// </summary>
    /// <param name="key">The setting key to remove.</param>
    public void RemoveSetting(string key)
    {
        _settings.Remove(key);
    }
    
    /// <summary>
    /// Gets the value of a setting.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <returns>The setting value, or null if not found.</returns>
    public string? GetSetting(string key)
    {
        return _settings.TryGetValue(key, out var value) ? value : null;
    }
}

/// <summary>
/// Defines the available UI themes.
/// </summary>
public enum UITheme
{
    /// <summary>
    /// Default system theme.
    /// </summary>
    Default,
    
    /// <summary>
    /// Dark theme with dark backgrounds and light text.
    /// </summary>
    Dark,
    
    /// <summary>
    /// Light theme with light backgrounds and dark text.
    /// </summary>
    Light,
    
    /// <summary>
    /// High contrast theme for accessibility.
    /// </summary>
    HighContrast,
    
    /// <summary>
    /// Custom theme with user-defined settings.
    /// </summary>
    Custom
}

/// <summary>
/// Defines the available UI layout modes.
/// </summary>
public enum UILayoutMode
{
    /// <summary>
    /// Automatically select the best layout mode.
    /// </summary>
    Auto,
    
    /// <summary>
    /// Text-based user interface mode.
    /// </summary>
    TUI,
    
    /// <summary>
    /// Graphical user interface mode.
    /// </summary>
    GUI,
    
    /// <summary>
    /// Hybrid mode combining TUI and GUI elements.
    /// </summary>
    Hybrid
}

/// <summary>
/// Contains predefined UI setting keys for common configuration options.
/// </summary>
public static class UISettingKeys
{
    /// <summary>
    /// Font family setting key.
    /// </summary>
    public const string FontFamily = "UI.FontFamily";
    
    /// <summary>
    /// Font size setting key.
    /// </summary>
    public const string FontSize = "UI.FontSize";
    
    /// <summary>
    /// Animation enabled setting key.
    /// </summary>
    public const string AnimationsEnabled = "UI.AnimationsEnabled";
    
    /// <summary>
    /// Sound effects enabled setting key.
    /// </summary>
    public const string SoundEffectsEnabled = "UI.SoundEffectsEnabled";
    
    /// <summary>
    /// Screen reader support enabled setting key.
    /// </summary>
    public const string ScreenReaderEnabled = "Accessibility.ScreenReaderEnabled";
    
    /// <summary>
    /// UI provider selection setting key.
    /// </summary>
    public const string UIProvider = "UI.Provider";
    
    /// <summary>
    /// Color scheme setting key.
    /// </summary>
    public const string ColorScheme = "UI.ColorScheme";
    
    /// <summary>
    /// Window opacity setting key.
    /// </summary>
    public const string WindowOpacity = "UI.WindowOpacity";
}