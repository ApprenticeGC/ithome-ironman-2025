namespace GameConsole.UI.Core;

/// <summary>
/// Provides context information for UI framework operations and rendering.
/// Contains all necessary information for framework-specific rendering and behavior.
/// </summary>
public record UIContext
{
    /// <summary>
    /// Gets the command line arguments passed to the application.
    /// </summary>
    public string[] Args { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the current application state dictionary.
    /// </summary>
    public Dictionary<string, object> State { get; init; } = new();

    /// <summary>
    /// Gets the current UI framework type.
    /// </summary>
    public UIFrameworkType FrameworkType { get; init; }

    /// <summary>
    /// Gets the capabilities supported by the current framework.
    /// </summary>
    public UICapabilities SupportedCapabilities { get; init; }

    /// <summary>
    /// Gets the user preferences for UI behavior.
    /// </summary>
    public UIPreferences Preferences { get; init; } = new();

    /// <summary>
    /// Gets the current responsive mode.
    /// </summary>
    public UIResponsiveMode ResponsiveMode { get; init; } = UIResponsiveMode.Adaptive;

    /// <summary>
    /// Gets the current theme information.
    /// </summary>
    public UITheme Theme { get; init; } = new();

    /// <summary>
    /// Gets the accessibility settings.
    /// </summary>
    public UIAccessibilitySettings AccessibilitySettings { get; init; } = new();

    /// <summary>
    /// Gets additional framework-specific properties.
    /// </summary>
    public Dictionary<string, object> FrameworkProperties { get; init; } = new();

    /// <summary>
    /// Gets the current viewport information.
    /// </summary>
    public UIViewport Viewport { get; init; } = new();
}

/// <summary>
/// User preferences for UI behavior and appearance.
/// </summary>
public record UIPreferences
{
    /// <summary>
    /// Gets or sets whether animations are enabled.
    /// </summary>
    public bool EnableAnimations { get; init; } = true;

    /// <summary>
    /// Gets or sets whether sound effects are enabled.
    /// </summary>
    public bool EnableSounds { get; init; } = true;

    /// <summary>
    /// Gets or sets the preferred color scheme.
    /// </summary>
    public string ColorScheme { get; init; } = "default";

    /// <summary>
    /// Gets or sets the font size multiplier.
    /// </summary>
    public float FontSizeMultiplier { get; init; } = 1.0f;

    /// <summary>
    /// Gets or sets keyboard shortcuts preferences.
    /// </summary>
    public Dictionary<string, string> KeyboardShortcuts { get; init; } = new();

    /// <summary>
    /// Gets or sets the language/culture preference.
    /// </summary>
    public string Culture { get; init; } = "en-US";
}

/// <summary>
/// Theme information for UI styling.
/// </summary>
public record UITheme
{
    /// <summary>
    /// Gets or sets the theme name.
    /// </summary>
    public string Name { get; init; } = "default";

    /// <summary>
    /// Gets or sets whether this is a dark theme.
    /// </summary>
    public bool IsDark { get; init; } = false;

    /// <summary>
    /// Gets or sets the primary color.
    /// </summary>
    public string PrimaryColor { get; init; } = "#007ACC";

    /// <summary>
    /// Gets or sets the secondary color.
    /// </summary>
    public string SecondaryColor { get; init; } = "#5A5A5A";

    /// <summary>
    /// Gets or sets the background color.
    /// </summary>
    public string BackgroundColor { get; init; } = "#FFFFFF";

    /// <summary>
    /// Gets or sets the text color.
    /// </summary>
    public string TextColor { get; init; } = "#000000";

    /// <summary>
    /// Gets or sets custom CSS variables for web frameworks.
    /// </summary>
    public Dictionary<string, string> CssVariables { get; init; } = new();
}

/// <summary>
/// Accessibility settings for UI components.
/// </summary>
public record UIAccessibilitySettings
{
    /// <summary>
    /// Gets or sets whether high contrast mode is enabled.
    /// </summary>
    public bool HighContrast { get; init; } = false;

    /// <summary>
    /// Gets or sets whether screen reader support is enabled.
    /// </summary>
    public bool ScreenReader { get; init; } = false;

    /// <summary>
    /// Gets or sets whether reduced motion is preferred.
    /// </summary>
    public bool ReducedMotion { get; init; } = false;

    /// <summary>
    /// Gets or sets keyboard navigation preferences.
    /// </summary>
    public bool KeyboardNavigation { get; init; } = true;

    /// <summary>
    /// Gets or sets focus indicators visibility.
    /// </summary>
    public bool ShowFocusIndicators { get; init; } = true;

    /// <summary>
    /// Gets or sets the text scaling factor for accessibility.
    /// </summary>
    public float TextScaling { get; init; } = 1.0f;
}

/// <summary>
/// Viewport information for responsive design.
/// </summary>
public record UIViewport
{
    /// <summary>
    /// Gets or sets the viewport width in pixels.
    /// </summary>
    public int Width { get; init; } = 1920;

    /// <summary>
    /// Gets or sets the viewport height in pixels.
    /// </summary>
    public int Height { get; init; } = 1080;

    /// <summary>
    /// Gets or sets the device pixel ratio.
    /// </summary>
    public float PixelRatio { get; init; } = 1.0f;

    /// <summary>
    /// Gets or sets whether the viewport is in portrait orientation.
    /// </summary>
    public bool IsPortrait { get; init; } = false;

    /// <summary>
    /// Gets or sets the current breakpoint (for responsive design).
    /// </summary>
    public string Breakpoint { get; init; } = "desktop";
}