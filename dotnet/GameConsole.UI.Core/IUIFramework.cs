using GameConsole.Core.Abstractions;

namespace GameConsole.UI.Core;

/// <summary>
/// Main interface for UI framework abstraction supporting Console, Web, and Desktop interfaces.
/// Provides the primary entry point for multi-modal UI operations and framework management.
/// </summary>
public interface IUIFramework : IService, ICapabilityProvider
{
    /// <summary>
    /// Gets the framework type (Console, Web, Desktop, etc.).
    /// </summary>
    UIFrameworkType FrameworkType { get; }

    /// <summary>
    /// Gets the capabilities supported by this framework.
    /// </summary>
    UICapabilities SupportedCapabilities { get; }

    /// <summary>
    /// Gets the current UI context.
    /// </summary>
    UIContext CurrentContext { get; }

    /// <summary>
    /// Gets the component factory for this framework.
    /// </summary>
    IUIComponentFactory ComponentFactory { get; }

    /// <summary>
    /// Observable stream of UI framework events.
    /// </summary>
    IObservable<UIFrameworkEvent> FrameworkEvents { get; }

    /// <summary>
    /// Observable stream of all UI events from components.
    /// </summary>
    IObservable<UIEvent> UIEvents { get; }

    /// <summary>
    /// Gets whether the framework supports responsive design.
    /// </summary>
    bool SupportsResponsiveDesign { get; }

    /// <summary>
    /// Gets whether the framework supports real-time updates.
    /// </summary>
    bool SupportsRealTimeUpdates { get; }

    /// <summary>
    /// Initializes the UI framework with the specified context.
    /// </summary>
    /// <param name="context">UI context for initialization.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async initialization.</returns>
    Task InitializeFrameworkAsync(UIContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the framework context and reconfigures as needed.
    /// </summary>
    /// <param name="context">New UI context.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async context update.</returns>
    Task UpdateContextAsync(UIContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders the entire UI or a specific component tree.
    /// </summary>
    /// <param name="rootComponent">The root component to render (null for full UI).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async render operation.</returns>
    Task<UIRenderResult> RenderAsync(IUIComponent? rootComponent = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a partial update/re-render of specific components.
    /// </summary>
    /// <param name="components">Components to update.</param>
    /// <param name="updatePriority">Priority for the update operation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async update operation.</returns>
    Task UpdateComponentsAsync(
        IEnumerable<IUIComponent> components,
        UIUpdatePriority updatePriority = UIUpdatePriority.Normal,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles user input events and routes them to appropriate components.
    /// </summary>
    /// <param name="inputEvent">The input event to handle.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async input handling.</returns>
    Task HandleInputEventAsync(object inputEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Manages focus within the UI framework.
    /// </summary>
    /// <param name="component">Component to focus (null to clear focus).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async focus operation.</returns>
    Task SetFocusAsync(IUIComponent? component, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the currently focused component.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the focused component.</returns>
    Task<IUIComponent?> GetFocusedComponentAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Navigates between different UI views or states.
    /// </summary>
    /// <param name="navigationTarget">The target to navigate to.</param>
    /// <param name="navigationData">Additional data for navigation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async navigation operation.</returns>
    Task NavigateAsync(
        string navigationTarget,
        Dictionary<string, object>? navigationData = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Shows a modal dialog or popup.
    /// </summary>
    /// <param name="dialog">The dialog component to show.</param>
    /// <param name="modal">Whether the dialog is modal.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async dialog operation that returns the dialog result.</returns>
    Task<object?> ShowDialogAsync(
        IUIComponent dialog,
        bool modal = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes a dialog or popup.
    /// </summary>
    /// <param name="dialog">The dialog to close.</param>
    /// <param name="result">The dialog result.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async close operation.</returns>
    Task CloseDialogAsync(
        IUIComponent dialog,
        object? result = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a theme to the entire framework or specific components.
    /// </summary>
    /// <param name="theme">The theme to apply.</param>
    /// <param name="components">Specific components to theme (null for entire framework).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async theming operation.</returns>
    Task ApplyThemeAsync(
        UITheme theme,
        IEnumerable<IUIComponent>? components = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets performance metrics for the UI framework.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns performance metrics.</returns>
    Task<UIPerformanceMetrics> GetPerformanceMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the current UI state and configuration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async validation operation.</returns>
    Task<UIValidationResult> ValidateFrameworkAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables or disables accessibility features.
    /// </summary>
    /// <param name="settings">Accessibility settings to apply.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async accessibility configuration.</returns>
    Task ConfigureAccessibilityAsync(UIAccessibilitySettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Captures a screenshot or snapshot of the current UI state.
    /// </summary>
    /// <param name="component">Specific component to capture (null for entire UI).</param>
    /// <param name="format">Output format for the capture.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async capture operation.</returns>
    Task<UICapture> CaptureAsync(
        IUIComponent? component = null,
        UIMediaFormat format = UIMediaFormat.PNG,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a global event handler for UI events.
    /// </summary>
    /// <param name="eventType">The type of event to handle.</param>
    /// <param name="handler">The event handler function.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async registration operation.</returns>
    Task RegisterGlobalEventHandlerAsync(
        Type eventType,
        Func<UIEvent, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters a global event handler.
    /// </summary>
    /// <param name="eventType">The type of event to stop handling.</param>
    /// <param name="handler">The event handler to remove.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async unregistration operation.</returns>
    Task UnregisterGlobalEventHandlerAsync(
        Type eventType,
        Func<UIEvent, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Performance metrics for UI framework operations.
/// </summary>
public record UIPerformanceMetrics
{
    /// <summary>
    /// Gets the average render time in milliseconds.
    /// </summary>
    public double AverageRenderTime { get; init; }

    /// <summary>
    /// Gets the frames per second (for animated UIs).
    /// </summary>
    public double FramesPerSecond { get; init; }

    /// <summary>
    /// Gets the current memory usage in bytes.
    /// </summary>
    public long MemoryUsage { get; init; }

    /// <summary>
    /// Gets the number of active components.
    /// </summary>
    public int ActiveComponentCount { get; init; }

    /// <summary>
    /// Gets the number of pending UI updates.
    /// </summary>
    public int PendingUpdateCount { get; init; }

    /// <summary>
    /// Gets the number of event handlers registered.
    /// </summary>
    public int EventHandlerCount { get; init; }

    /// <summary>
    /// Gets framework-specific performance metrics.
    /// </summary>
    public Dictionary<string, object> FrameworkSpecificMetrics { get; init; } = new();
}

/// <summary>
/// Represents a UI capture (screenshot, etc.).
/// </summary>
public record UICapture
{
    /// <summary>
    /// Gets the capture data.
    /// </summary>
    public byte[] Data { get; init; } = Array.Empty<byte>();

    /// <summary>
    /// Gets the capture format.
    /// </summary>
    public UIMediaFormat Format { get; init; }

    /// <summary>
    /// Gets the capture dimensions.
    /// </summary>
    public (int Width, int Height) Dimensions { get; init; }

    /// <summary>
    /// Gets the timestamp when the capture was taken.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets metadata about the capture.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Supported media formats for UI captures.
/// </summary>
public enum UIMediaFormat
{
    /// <summary>
    /// PNG image format.
    /// </summary>
    PNG,

    /// <summary>
    /// JPEG image format.
    /// </summary>
    JPEG,

    /// <summary>
    /// SVG vector format.
    /// </summary>
    SVG,

    /// <summary>
    /// PDF document format.
    /// </summary>
    PDF,

    /// <summary>
    /// HTML format.
    /// </summary>
    HTML,

    /// <summary>
    /// Plain text format.
    /// </summary>
    Text
}