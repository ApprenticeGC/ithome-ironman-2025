namespace GameConsole.UI.Core;

/// <summary>
/// Rendering context for UI components.
/// </summary>
public class UIRenderContext
{
    /// <summary>
    /// Gets the current viewport bounds.
    /// </summary>
    public UIRect Viewport { get; set; }
    
    /// <summary>
    /// Gets the current clipping region.
    /// </summary>
    public UIRect ClipRegion { get; set; }
    
    /// <summary>
    /// Gets the current theme.
    /// </summary>
    public UITheme Theme { get; set; }
    
    /// <summary>
    /// Gets the render target.
    /// </summary>
    public object? RenderTarget { get; set; }
    
    /// <summary>
    /// Initializes a new UIRenderContext.
    /// </summary>
    public UIRenderContext(UIRect viewport, UITheme theme, object? renderTarget = null)
    {
        Viewport = viewport;
        ClipRegion = viewport;
        Theme = theme;
        RenderTarget = renderTarget;
    }
    
    /// <summary>
    /// Creates a new context with updated clipping region.
    /// </summary>
    public UIRenderContext WithClipRegion(UIRect clipRegion) => 
        new(Viewport, Theme, RenderTarget) { ClipRegion = clipRegion };
}

/// <summary>
/// Theme configuration for UI styling.
/// </summary>
public class UITheme
{
    /// <summary>
    /// Gets or sets the default background color.
    /// </summary>
    public UIColor BackgroundColor { get; set; } = UIColor.Colors.Black;
    
    /// <summary>
    /// Gets or sets the default foreground color.
    /// </summary>
    public UIColor ForegroundColor { get; set; } = UIColor.Colors.White;
    
    /// <summary>
    /// Gets or sets the border color.
    /// </summary>
    public UIColor BorderColor { get; set; } = UIColor.Colors.Gray;
    
    /// <summary>
    /// Gets or sets the focused component color.
    /// </summary>
    public UIColor FocusColor { get; set; } = UIColor.Colors.Cyan;
    
    /// <summary>
    /// Gets or sets the disabled component color.
    /// </summary>
    public UIColor DisabledColor { get; set; } = UIColor.Colors.DarkGray;
    
    /// <summary>
    /// Gets or sets the button pressed color.
    /// </summary>
    public UIColor PressedColor { get; set; } = UIColor.Colors.Blue;
    
    /// <summary>
    /// Gets or sets component-specific styles.
    /// </summary>
    public Dictionary<string, UIStyle> ComponentStyles { get; set; } = new();
    
    /// <summary>
    /// Default console theme.
    /// </summary>
    public static UITheme Default => new();
    
    /// <summary>
    /// Dark theme variant.
    /// </summary>
    public static UITheme Dark => new()
    {
        BackgroundColor = UIColor.Colors.Black,
        ForegroundColor = UIColor.Colors.LightGray,
        BorderColor = UIColor.Colors.DarkGray,
        FocusColor = UIColor.Colors.Yellow
    };
    
    /// <summary>
    /// Light theme variant.
    /// </summary>
    public static UITheme Light => new()
    {
        BackgroundColor = UIColor.Colors.White,
        ForegroundColor = UIColor.Colors.Black,
        BorderColor = UIColor.Colors.Gray,
        FocusColor = UIColor.Colors.Blue
    };
}

/// <summary>
/// Style configuration for individual UI components.
/// </summary>
public class UIStyle
{
    /// <summary>
    /// Gets or sets the background color.
    /// </summary>
    public UIColor? BackgroundColor { get; set; }
    
    /// <summary>
    /// Gets or sets the foreground color.
    /// </summary>
    public UIColor? ForegroundColor { get; set; }
    
    /// <summary>
    /// Gets or sets the border color.
    /// </summary>
    public UIColor? BorderColor { get; set; }
    
    /// <summary>
    /// Gets or sets the padding around the component content.
    /// </summary>
    public UIThickness? Padding { get; set; }
    
    /// <summary>
    /// Gets or sets the margin around the component.
    /// </summary>
    public UIThickness? Margin { get; set; }
    
    /// <summary>
    /// Gets or sets the border thickness.
    /// </summary>
    public UIThickness? BorderThickness { get; set; }
    
    /// <summary>
    /// Gets or sets the font size (if applicable).
    /// </summary>
    public int? FontSize { get; set; }
    
    /// <summary>
    /// Gets or sets whether text is bold.
    /// </summary>
    public bool? IsBold { get; set; }
    
    /// <summary>
    /// Gets or sets whether text is italic.
    /// </summary>
    public bool? IsItalic { get; set; }
    
    /// <summary>
    /// Gets or sets whether text is underlined.
    /// </summary>
    public bool? IsUnderlined { get; set; }
}

/// <summary>
/// Represents thickness values for padding, margin, or border.
/// </summary>
public struct UIThickness
{
    /// <summary>
    /// Left thickness.
    /// </summary>
    public int Left { get; set; }
    
    /// <summary>
    /// Top thickness.
    /// </summary>
    public int Top { get; set; }
    
    /// <summary>
    /// Right thickness.
    /// </summary>
    public int Right { get; set; }
    
    /// <summary>
    /// Bottom thickness.
    /// </summary>
    public int Bottom { get; set; }
    
    /// <summary>
    /// Initializes uniform thickness.
    /// </summary>
    public UIThickness(int uniform) : this(uniform, uniform, uniform, uniform) { }
    
    /// <summary>
    /// Initializes thickness with horizontal and vertical values.
    /// </summary>
    public UIThickness(int horizontal, int vertical) : this(horizontal, vertical, horizontal, vertical) { }
    
    /// <summary>
    /// Initializes thickness with individual values.
    /// </summary>
    public UIThickness(int left, int top, int right, int bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }
    
    /// <summary>
    /// Zero thickness.
    /// </summary>
    public static UIThickness Zero => new(0);
}