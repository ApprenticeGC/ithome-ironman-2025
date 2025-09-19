namespace GameConsole.UI.Core;

/// <summary>
/// Defines the supported UI frameworks and their capabilities.
/// </summary>
public enum UIFrameworkType
{
    /// <summary>
    /// Console/Terminal-based text user interface.
    /// </summary>
    Console = 0,
    
    /// <summary>
    /// Web-based user interface using HTML/CSS/JavaScript.
    /// </summary>
    Web = 1,
    
    /// <summary>
    /// Desktop native application user interface.
    /// </summary>
    Desktop = 2
}

/// <summary>
/// Capabilities that a UI framework can support.
/// </summary>
[Flags]
public enum UICapabilities
{
    None = 0,
    TextDisplay = 1 << 0,
    ColorSupport = 1 << 1,
    MouseInteraction = 1 << 2,
    KeyboardInput = 1 << 3,
    TouchInput = 1 << 4,
    FormInput = 1 << 5,
    Graphics2D = 1 << 6,
    Graphics3D = 1 << 7,
    Animation = 1 << 8,
    ResponsiveLayout = 1 << 9,
    AccessibilitySupport = 1 << 10,
    Styling = 1 << 11
}

/// <summary>
/// Represents the visual appearance and styling information for UI components.
/// </summary>
public record UIStyle
{
    /// <summary>
    /// Background color (RGBA format).
    /// </summary>
    public uint? BackgroundColor { get; init; }

    /// <summary>
    /// Foreground/text color (RGBA format).
    /// </summary>
    public uint? ForegroundColor { get; init; }

    /// <summary>
    /// Border color (RGBA format).
    /// </summary>
    public uint? BorderColor { get; init; }

    /// <summary>
    /// Font family name.
    /// </summary>
    public string? FontFamily { get; init; }

    /// <summary>
    /// Font size in platform-specific units.
    /// </summary>
    public float? FontSize { get; init; }

    /// <summary>
    /// Whether text is bold.
    /// </summary>
    public bool? IsBold { get; init; }

    /// <summary>
    /// Whether text is italic.
    /// </summary>
    public bool? IsItalic { get; init; }

    /// <summary>
    /// Margin around the component.
    /// </summary>
    public UISpacing? Margin { get; init; }

    /// <summary>
    /// Padding inside the component.
    /// </summary>
    public UISpacing? Padding { get; init; }

    /// <summary>
    /// Additional CSS/styling properties for web frameworks.
    /// </summary>
    public Dictionary<string, object>? CustomProperties { get; init; }
}

/// <summary>
/// Represents spacing values for margins and padding.
/// </summary>
public record UISpacing(float Top, float Right, float Bottom, float Left)
{
    /// <summary>
    /// Creates uniform spacing on all sides.
    /// </summary>
    public static UISpacing All(float value) => new(value, value, value, value);

    /// <summary>
    /// Creates horizontal and vertical spacing.
    /// </summary>
    public static UISpacing Symmetric(float horizontal, float vertical) => 
        new(vertical, horizontal, vertical, horizontal);
}

/// <summary>
/// Defines layout positioning and sizing information.
/// </summary>
public record UILayout
{
    /// <summary>
    /// X position relative to parent.
    /// </summary>
    public float? X { get; init; }

    /// <summary>
    /// Y position relative to parent.
    /// </summary>
    public float? Y { get; init; }

    /// <summary>
    /// Component width.
    /// </summary>
    public float? Width { get; init; }

    /// <summary>
    /// Component height.
    /// </summary>
    public float? Height { get; init; }

    /// <summary>
    /// Minimum width constraint.
    /// </summary>
    public float? MinWidth { get; init; }

    /// <summary>
    /// Minimum height constraint.
    /// </summary>
    public float? MinHeight { get; init; }

    /// <summary>
    /// Maximum width constraint.
    /// </summary>
    public float? MaxWidth { get; init; }

    /// <summary>
    /// Maximum height constraint.
    /// </summary>
    public float? MaxHeight { get; init; }

    /// <summary>
    /// Whether component is visible.
    /// </summary>
    public bool IsVisible { get; init; } = true;

    /// <summary>
    /// Z-order/layering index.
    /// </summary>
    public int ZIndex { get; init; } = 0;
}

/// <summary>
/// Represents a responsive breakpoint for adaptive layouts.
/// </summary>
public record UIBreakpoint(string Name, float MinWidth, float MaxWidth)
{
    /// <summary>
    /// Common mobile breakpoint.
    /// </summary>
    public static UIBreakpoint Mobile = new("Mobile", 0, 767);

    /// <summary>
    /// Common tablet breakpoint.
    /// </summary>
    public static UIBreakpoint Tablet = new("Tablet", 768, 1023);

    /// <summary>
    /// Common desktop breakpoint.
    /// </summary>
    public static UIBreakpoint Desktop = new("Desktop", 1024, float.MaxValue);
}

/// <summary>
/// Context information for UI rendering and framework-specific operations.
/// </summary>
public record UIContext
{
    /// <summary>
    /// The current UI framework being used.
    /// </summary>
    public required UIFrameworkType Framework { get; init; }

    /// <summary>
    /// Capabilities supported by the current framework.
    /// </summary>
    public required UICapabilities SupportedCapabilities { get; init; }

    /// <summary>
    /// Current viewport/screen dimensions.
    /// </summary>
    public required UILayout Viewport { get; init; }

    /// <summary>
    /// Current responsive breakpoint.
    /// </summary>
    public UIBreakpoint? CurrentBreakpoint { get; init; }

    /// <summary>
    /// Framework-specific rendering context (e.g., WebGL context, console buffer).
    /// </summary>
    public object? RenderingContext { get; init; }

    /// <summary>
    /// User preferences and accessibility settings.
    /// </summary>
    public Dictionary<string, object>? UserPreferences { get; init; }

    /// <summary>
    /// State data for the current UI session.
    /// </summary>
    public Dictionary<string, object>? State { get; init; }
}