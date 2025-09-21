using System.Reactive;

namespace GameConsole.UI.Core;

/// <summary>
/// Base class for all UI events.
/// </summary>
public abstract record UIEvent
{
    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the unique identifier for this event.
    /// </summary>
    public string EventId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets the source component that generated this event.
    /// </summary>
    public string? SourceComponentId { get; init; }

    /// <summary>
    /// Gets whether this event bubbles up the component hierarchy.
    /// </summary>
    public bool Bubbles { get; init; } = true;

    /// <summary>
    /// Gets whether this event can be cancelled.
    /// </summary>
    public bool Cancelable { get; init; } = false;

    /// <summary>
    /// Gets or sets whether this event has been cancelled.
    /// </summary>
    public bool Cancelled { get; set; } = false;
}

/// <summary>
/// Event fired when a UI component is rendered or re-rendered.
/// </summary>
public record UIRenderEvent : UIEvent
{
    /// <summary>
    /// Gets the component that was rendered.
    /// </summary>
    public string ComponentId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the render duration in milliseconds.
    /// </summary>
    public double RenderDuration { get; init; }

    /// <summary>
    /// Gets whether this was an initial render or update.
    /// </summary>
    public bool IsInitialRender { get; init; }
}

/// <summary>
/// Event fired when a user interacts with a UI component.
/// </summary>
public record UIInteractionEvent : UIEvent
{
    /// <summary>
    /// Gets the type of interaction.
    /// </summary>
    public UIInteractionType InteractionType { get; init; }

    /// <summary>
    /// Gets the component that was interacted with.
    /// </summary>
    public string ComponentId { get; init; } = string.Empty;

    /// <summary>
    /// Gets additional interaction data.
    /// </summary>
    public Dictionary<string, object> Data { get; init; } = new();
}

/// <summary>
/// Event fired when a component's state changes.
/// </summary>
public record UIStateChangeEvent : UIEvent
{
    /// <summary>
    /// Gets the component whose state changed.
    /// </summary>
    public string ComponentId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the property that changed.
    /// </summary>
    public string PropertyName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the old value.
    /// </summary>
    public object? OldValue { get; init; }

    /// <summary>
    /// Gets the new value.
    /// </summary>
    public object? NewValue { get; init; }
}

/// <summary>
/// Event fired when the UI framework changes modes or configuration.
/// </summary>
public record UIFrameworkEvent : UIEvent
{
    /// <summary>
    /// Gets the type of framework event.
    /// </summary>
    public UIFrameworkEventType EventType { get; init; }

    /// <summary>
    /// Gets the old framework type (if changing frameworks).
    /// </summary>
    public UIFrameworkType? OldFrameworkType { get; init; }

    /// <summary>
    /// Gets the new framework type (if changing frameworks).
    /// </summary>
    public UIFrameworkType? NewFrameworkType { get; init; }

    /// <summary>
    /// Gets additional event data.
    /// </summary>
    public Dictionary<string, object> EventData { get; init; } = new();
}

/// <summary>
/// Event fired when an error occurs in the UI system.
/// </summary>
public record UIErrorEvent : UIEvent
{
    /// <summary>
    /// Gets the error that occurred.
    /// </summary>
    public Exception Error { get; init; } = new InvalidOperationException();

    /// <summary>
    /// Gets the component where the error occurred (if applicable).
    /// </summary>
    public string? ComponentId { get; init; }

    /// <summary>
    /// Gets the error severity.
    /// </summary>
    public UIErrorSeverity Severity { get; init; } = UIErrorSeverity.Error;

    /// <summary>
    /// Gets whether the error was handled.
    /// </summary>
    public bool Handled { get; set; } = false;
}

/// <summary>
/// Types of UI interactions.
/// </summary>
public enum UIInteractionType
{
    /// <summary>
    /// Click or tap interaction.
    /// </summary>
    Click,

    /// <summary>
    /// Double-click interaction.
    /// </summary>
    DoubleClick,

    /// <summary>
    /// Hover or mouse over.
    /// </summary>
    Hover,

    /// <summary>
    /// Focus received.
    /// </summary>
    Focus,

    /// <summary>
    /// Focus lost.
    /// </summary>
    Blur,

    /// <summary>
    /// Key pressed.
    /// </summary>
    KeyPress,

    /// <summary>
    /// Text input.
    /// </summary>
    Input,

    /// <summary>
    /// Value changed.
    /// </summary>
    Change,

    /// <summary>
    /// Form submission.
    /// </summary>
    Submit,

    /// <summary>
    /// Drag start.
    /// </summary>
    DragStart,

    /// <summary>
    /// Drag end.
    /// </summary>
    DragEnd,

    /// <summary>
    /// Drop operation.
    /// </summary>
    Drop
}

/// <summary>
/// Types of UI framework events.
/// </summary>
public enum UIFrameworkEventType
{
    /// <summary>
    /// Framework is initializing.
    /// </summary>
    Initializing,

    /// <summary>
    /// Framework initialization completed.
    /// </summary>
    Initialized,

    /// <summary>
    /// Framework is starting.
    /// </summary>
    Starting,

    /// <summary>
    /// Framework started successfully.
    /// </summary>
    Started,

    /// <summary>
    /// Framework is stopping.
    /// </summary>
    Stopping,

    /// <summary>
    /// Framework stopped.
    /// </summary>
    Stopped,

    /// <summary>
    /// Switching to different framework.
    /// </summary>
    SwitchingFramework,

    /// <summary>
    /// Theme changed.
    /// </summary>
    ThemeChanged,

    /// <summary>
    /// Viewport changed.
    /// </summary>
    ViewportChanged,

    /// <summary>
    /// Configuration updated.
    /// </summary>
    ConfigurationUpdated
}

/// <summary>
/// Error severity levels.
/// </summary>
public enum UIErrorSeverity
{
    /// <summary>
    /// Information message.
    /// </summary>
    Info,

    /// <summary>
    /// Warning message.
    /// </summary>
    Warning,

    /// <summary>
    /// Error message.
    /// </summary>
    Error,

    /// <summary>
    /// Critical error.
    /// </summary>
    Critical
}