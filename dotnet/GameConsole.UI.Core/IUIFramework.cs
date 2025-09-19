using GameConsole.Core.Abstractions;

namespace GameConsole.UI.Core;

/// <summary>
/// Provides framework abstraction for different UI platforms (Console, Web, Desktop).
/// </summary>
public interface IUIFramework : ICapabilityProvider
{
    /// <summary>
    /// Type of UI framework (Console, Web, Desktop).
    /// </summary>
    UIFrameworkType FrameworkType { get; }

    /// <summary>
    /// Capabilities supported by this framework.
    /// </summary>
    UICapabilities SupportedCapabilities { get; }

    /// <summary>
    /// Current UI context for this framework instance.
    /// </summary>
    UIContext Context { get; }

    /// <summary>
    /// Component factory for creating framework-specific components.
    /// </summary>
    IUIComponentFactory ComponentFactory { get; }

    /// <summary>
    /// Initializes the UI framework for the current platform.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the initialization operation.</returns>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Shuts down the UI framework and releases resources.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the shutdown operation.</returns>
    Task ShutdownAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new rendering context for component rendering.
    /// </summary>
    /// <param name="viewport">Viewport dimensions and settings.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Framework-specific rendering context.</returns>
    Task<UIContext> CreateRenderingContextAsync(UILayout viewport, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders a component tree to the framework's output.
    /// </summary>
    /// <param name="rootComponent">Root component to render.</param>
    /// <param name="context">Rendering context to use.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the rendering operation.</returns>
    Task RenderComponentTreeAsync(IUIComponent rootComponent, UIContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles framework-specific input events and dispatches them to components.
    /// </summary>
    /// <param name="inputEvent">Raw input event from the platform.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the input handling operation.</returns>
    Task HandleInputAsync(object inputEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates responsive layout based on current viewport dimensions.
    /// </summary>
    /// <param name="newViewport">New viewport dimensions.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the layout update operation.</returns>
    Task UpdateResponsiveLayoutAsync(UILayout newViewport, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets framework-specific styling capabilities and constraints.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Styling information for this framework.</returns>
    Task<UIStyleCapabilities> GetStyleCapabilitiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies accessibility features for the current framework.
    /// </summary>
    /// <param name="accessibilitySettings">Accessibility preferences.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the accessibility configuration operation.</returns>
    Task ApplyAccessibilitySettingsAsync(Dictionary<string, object> accessibilitySettings, CancellationToken cancellationToken = default);
}

/// <summary>
/// Describes the styling capabilities and limitations of a UI framework.
/// </summary>
public record UIStyleCapabilities
{
    /// <summary>
    /// Whether the framework supports color styling.
    /// </summary>
    public bool SupportsColor { get; init; }

    /// <summary>
    /// Whether the framework supports custom fonts.
    /// </summary>
    public bool SupportsFonts { get; init; }

    /// <summary>
    /// Whether the framework supports animations.
    /// </summary>
    public bool SupportsAnimations { get; init; }

    /// <summary>
    /// Whether the framework supports transparency/opacity.
    /// </summary>
    public bool SupportsTransparency { get; init; }

    /// <summary>
    /// Supported color formats (RGBA, HSL, etc.).
    /// </summary>
    public IEnumerable<string> SupportedColorFormats { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Available system fonts.
    /// </summary>
    public IEnumerable<string> AvailableFonts { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Maximum supported screen resolution.
    /// </summary>
    public (int Width, int Height)? MaxResolution { get; init; }

    /// <summary>
    /// Framework-specific style properties that are supported.
    /// </summary>
    public Dictionary<string, Type>? CustomStyleProperties { get; init; }
}

/// <summary>
/// Base implementation of IUIFramework providing common functionality.
/// </summary>
public abstract class UIFrameworkBase : IUIFramework
{
    /// <inheritdoc />
    public abstract UIFrameworkType FrameworkType { get; }

    /// <inheritdoc />
    public abstract UICapabilities SupportedCapabilities { get; }

    /// <inheritdoc />
    public UIContext Context { get; protected set; } = null!;

    /// <inheritdoc />
    public IUIComponentFactory ComponentFactory { get; protected set; } = null!;

    /// <inheritdoc />
    public virtual Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        // UI frameworks provide the IUIFramework capability
        return Task.FromResult<IEnumerable<Type>>(new[] { typeof(IUIFramework) });
    }

    /// <inheritdoc />
    public virtual Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(this is T);
    }

    /// <inheritdoc />
    public virtual Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        return Task.FromResult(this as T);
    }

    /// <inheritdoc />
    public abstract Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task ShutdownAsync(CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task<UIContext> CreateRenderingContextAsync(UILayout viewport, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task RenderComponentTreeAsync(IUIComponent rootComponent, UIContext context, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task HandleInputAsync(object inputEvent, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public virtual Task UpdateResponsiveLayoutAsync(UILayout newViewport, CancellationToken cancellationToken = default)
    {
        // Update context with new viewport
        Context = Context with { Viewport = newViewport };
        
        // Determine new breakpoint
        var breakpoint = DetermineBreakpoint(newViewport.Width ?? 0);
        Context = Context with { CurrentBreakpoint = breakpoint };
        
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public abstract Task<UIStyleCapabilities> GetStyleCapabilitiesAsync(CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public virtual Task ApplyAccessibilitySettingsAsync(Dictionary<string, object> accessibilitySettings, CancellationToken cancellationToken = default)
    {
        // Update context with accessibility settings
        var userPreferences = Context.UserPreferences ?? new Dictionary<string, object>();
        
        foreach (var setting in accessibilitySettings)
        {
            userPreferences[$"accessibility.{setting.Key}"] = setting.Value;
        }
        
        Context = Context with { UserPreferences = userPreferences };
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Determines the appropriate responsive breakpoint based on width.
    /// </summary>
    protected virtual UIBreakpoint DetermineBreakpoint(float width)
    {
        if (width <= UIBreakpoint.Mobile.MaxWidth)
            return UIBreakpoint.Mobile;
        if (width <= UIBreakpoint.Tablet.MaxWidth)
            return UIBreakpoint.Tablet;
        return UIBreakpoint.Desktop;
    }
}