using System.Reactive;

namespace GameConsole.UI.Core;

/// <summary>
/// Defines UI events that can occur during user interaction.
/// </summary>
public abstract record UIEvent
{
    /// <summary>
    /// Timestamp when the event occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The component that originated this event (if applicable).
    /// </summary>
    public string? ComponentId { get; init; }

    /// <summary>
    /// Event properties for framework-specific data.
    /// </summary>
    public Dictionary<string, object>? Properties { get; init; }
}

/// <summary>
/// Event triggered when a UI component's value changes.
/// </summary>
public record UIValueChangedEvent : UIEvent
{
    /// <summary>
    /// The old value before the change.
    /// </summary>
    public required object? OldValue { get; init; }

    /// <summary>
    /// The new value after the change.
    /// </summary>
    public required object? NewValue { get; init; }
}

/// <summary>
/// Event triggered when a UI component is clicked or activated.
/// </summary>
public record UIClickEvent : UIEvent
{
    /// <summary>
    /// Mouse/touch position relative to the component.
    /// </summary>
    public (float X, float Y)? Position { get; init; }

    /// <summary>
    /// Which button was clicked (0 = primary, 1 = secondary, etc.).
    /// </summary>
    public int Button { get; init; } = 0;
}

/// <summary>
/// Event triggered when a UI component gains or loses focus.
/// </summary>
public record UIFocusEvent : UIEvent
{
    /// <summary>
    /// Whether the component gained focus (true) or lost it (false).
    /// </summary>
    public required bool HasFocus { get; init; }
}

/// <summary>
/// Event triggered when key input occurs on a UI component.
/// </summary>
public record UIKeyEvent : UIEvent
{
    /// <summary>
    /// The key that was pressed.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Whether this is a key down (true) or key up (false) event.
    /// </summary>
    public required bool IsKeyDown { get; init; }

    /// <summary>
    /// Modifier keys that were held during the event.
    /// </summary>
    public UIKeyModifiers Modifiers { get; init; } = UIKeyModifiers.None;
}

/// <summary>
/// Modifier keys that can be held during UI events.
/// </summary>
[Flags]
public enum UIKeyModifiers
{
    None = 0,
    Shift = 1 << 0,
    Control = 1 << 1,
    Alt = 1 << 2,
    Meta = 1 << 3
}

/// <summary>
/// Event triggered when data is bound to a UI component.
/// </summary>
public record UIDataBindingEvent : UIEvent
{
    /// <summary>
    /// The property name that was bound.
    /// </summary>
    public required string PropertyName { get; init; }

    /// <summary>
    /// The new data value that was bound.
    /// </summary>
    public required object? Data { get; init; }

    /// <summary>
    /// The data source path or binding expression.
    /// </summary>
    public string? BindingPath { get; init; }
}

/// <summary>
/// Event triggered during UI lifecycle operations.
/// </summary>
public record UILifecycleEvent : UIEvent
{
    /// <summary>
    /// The lifecycle stage that occurred.
    /// </summary>
    public required UILifecycleStage Stage { get; init; }
}

/// <summary>
/// Defines the various stages in a UI component's lifecycle.
/// </summary>
public enum UILifecycleStage
{
    Creating,
    Created,
    Mounting,
    Mounted,
    Updating,
    Updated,
    Unmounting,
    Unmounted,
    Disposing,
    Disposed
}