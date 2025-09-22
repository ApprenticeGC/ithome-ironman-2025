namespace GameConsole.UI.Core;

/// <summary>
/// Represents a UI profile configuration that defines how the user interface should be displayed.
/// Each profile contains settings for different UI modes (TUI, GUI, etc.) and their associated configurations.
/// </summary>
public record UIProfile
{
    /// <summary>
    /// Unique identifier for the UI profile.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable name of the UI profile.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Description of what this profile is intended for.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// The UI mode this profile is designed for (e.g., "TUI", "GUI", "Mixed").
    /// </summary>
    public string Mode { get; init; } = string.Empty;

    /// <summary>
    /// Configuration settings specific to this profile.
    /// </summary>
    public UIProfileSettings Settings { get; init; } = new();

    /// <summary>
    /// When this profile was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When this profile was last modified.
    /// </summary>
    public DateTimeOffset ModifiedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Whether this profile is currently active.
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Whether this is a built-in profile that cannot be deleted.
    /// </summary>
    public bool IsBuiltIn { get; init; }
}

/// <summary>
/// Settings and configuration values for a UI profile.
/// </summary>
public record UIProfileSettings
{
    /// <summary>
    /// Theme settings for the UI.
    /// </summary>
    public UIThemeSettings Theme { get; init; } = new();

    /// <summary>
    /// Layout configuration for UI elements.
    /// </summary>
    public UILayoutSettings Layout { get; init; } = new();

    /// <summary>
    /// Input handling preferences for this profile.
    /// </summary>
    public UIInputSettings Input { get; init; } = new();

    /// <summary>
    /// Performance and rendering settings.
    /// </summary>
    public UIRenderingSettings Rendering { get; init; } = new();

    /// <summary>
    /// Custom properties for extensibility.
    /// </summary>
    public Dictionary<string, object> Properties { get; init; } = new();
}

/// <summary>
/// Theme-related settings for UI appearance.
/// </summary>
public record UIThemeSettings
{
    /// <summary>
    /// Primary color scheme name.
    /// </summary>
    public string ColorScheme { get; init; } = "Default";

    /// <summary>
    /// Font family to use for UI text.
    /// </summary>
    public string FontFamily { get; init; } = "Consolas";

    /// <summary>
    /// Base font size for UI elements.
    /// </summary>
    public int FontSize { get; init; } = 14;

    /// <summary>
    /// Whether to use dark mode.
    /// </summary>
    public bool DarkMode { get; init; } = true;
}

/// <summary>
/// Layout configuration for UI elements positioning and sizing.
/// </summary>
public record UILayoutSettings
{
    /// <summary>
    /// Width of the main UI area in pixels or percentage.
    /// </summary>
    public string Width { get; init; } = "100%";

    /// <summary>
    /// Height of the main UI area in pixels or percentage.
    /// </summary>
    public string Height { get; init; } = "100%";

    /// <summary>
    /// Padding around UI elements.
    /// </summary>
    public int Padding { get; init; } = 8;

    /// <summary>
    /// Whether to show borders around UI elements.
    /// </summary>
    public bool ShowBorders { get; init; } = true;
}

/// <summary>
/// Input handling preferences for the UI profile.
/// </summary>
public record UIInputSettings
{
    /// <summary>
    /// Preferred input method (keyboard, mouse, gamepad, etc.).
    /// </summary>
    public string PreferredInput { get; init; } = "Keyboard";

    /// <summary>
    /// Whether to enable keyboard shortcuts.
    /// </summary>
    public bool KeyboardShortcuts { get; init; } = true;

    /// <summary>
    /// Whether to enable mouse interaction.
    /// </summary>
    public bool MouseEnabled { get; init; } = true;
}

/// <summary>
/// Performance and rendering settings for the UI.
/// </summary>
public record UIRenderingSettings
{
    /// <summary>
    /// Maximum frames per second for UI updates.
    /// </summary>
    public int MaxFPS { get; init; } = 60;

    /// <summary>
    /// Whether to use hardware acceleration if available.
    /// </summary>
    public bool UseHardwareAcceleration { get; init; } = true;

    /// <summary>
    /// Quality level for UI rendering (Low, Medium, High).
    /// </summary>
    public string Quality { get; init; } = "High";
}