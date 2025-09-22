using GameConsole.Input.Core;

namespace GameConsole.UI.Core;

/// <summary>
/// Represents user preferences for UI behavior and appearance.
/// </summary>
public record UIPreferences(
    bool HighContrast = false,
    bool ReducedMotion = false,
    string PreferredTheme = "Default",
    bool EnableAccessibility = true,
    Dictionary<string, string>? CustomSettings = null
);

/// <summary>
/// Represents styling context for cross-framework component rendering.
/// </summary>
public record StyleContext(
    Dictionary<string, object> Properties,
    string? Theme = null,
    bool IsResponsive = true,
    Dictionary<string, object>? MediaQueries = null
)
{
    /// <summary>
    /// Creates an empty style context.
    /// </summary>
    public static StyleContext Empty => new(new Dictionary<string, object>());
    
    /// <summary>
    /// Gets a style property value.
    /// </summary>
    public T? GetProperty<T>(string key, T? defaultValue = default)
    {
        if (Properties.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }
    
    /// <summary>
    /// Sets a style property value.
    /// </summary>
    public StyleContext WithProperty(string key, object value)
    {
        var newProperties = new Dictionary<string, object>(Properties)
        {
            [key] = value
        };
        return this with { Properties = newProperties };
    }
}

/// <summary>
/// Provides context for framework-specific UI rendering and operations.
/// Contains state, preferences, and capabilities for the current UI session.
/// </summary>
public record UIContext(
    string[] Args,
    Dictionary<string, object> State,
    UIMode CurrentMode,
    UIPreferences Preferences,
    UICapabilities SupportedCapabilities = UICapabilities.None,
    FrameworkType FrameworkType = FrameworkType.Console,
    StyleContext? Style = null,
    CancellationToken CancellationToken = default
)
{
    /// <summary>
    /// Event dispatcher for UI events within this context.
    /// </summary>
    public IUIEventDispatcher? EventDispatcher { get; init; }
    
    /// <summary>
    /// Input event stream for handling user input.
    /// </summary>
    public IObservable<InputEvent>? InputEvents { get; init; }
    
    /// <summary>
    /// Gets a state value by key.
    /// </summary>
    public T? GetState<T>(string key, T? defaultValue = default)
    {
        if (State.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }
    
    /// <summary>
    /// Sets a state value.
    /// </summary>
    public UIContext WithState(string key, object value)
    {
        var newState = new Dictionary<string, object>(State)
        {
            [key] = value
        };
        return this with { State = newState };
    }
    
    /// <summary>
    /// Creates a new context with updated style.
    /// </summary>
    public UIContext WithStyle(StyleContext style) => this with { Style = style };
    
    /// <summary>
    /// Creates a new context with additional capabilities.
    /// </summary>
    public UIContext WithCapabilities(UICapabilities capabilities) => 
        this with { SupportedCapabilities = SupportedCapabilities | capabilities };
    
    /// <summary>
    /// Checks if a capability is supported in this context.
    /// </summary>
    public bool HasCapability(UICapabilities capability) => 
        (SupportedCapabilities & capability) == capability;
    
    /// <summary>
    /// Creates a minimal UI context for testing or basic operations.
    /// </summary>
    public static UIContext Create(
        UIMode mode = UIMode.CLI,
        FrameworkType frameworkType = FrameworkType.Console,
        UICapabilities capabilities = UICapabilities.None) =>
        new(
            Args: Array.Empty<string>(),
            State: new Dictionary<string, object>(),
            CurrentMode: mode,
            Preferences: new UIPreferences(),
            SupportedCapabilities: capabilities,
            FrameworkType: frameworkType,
            Style: StyleContext.Empty
        );
}