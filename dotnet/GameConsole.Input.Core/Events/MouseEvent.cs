using GameConsole.Input.Core.Types;

namespace GameConsole.Input.Core.Events;

/// <summary>
/// Represents a mouse input event.
/// </summary>
public class MouseEvent : InputEventBase
{
    /// <summary>
    /// Initializes a new instance of the MouseEvent class.
    /// </summary>
    /// <param name="deviceId">The mouse device ID.</param>
    /// <param name="eventType">The type of mouse event.</param>
    /// <param name="position">The mouse position when the event occurred.</param>
    /// <param name="button">The mouse button involved (for button events).</param>
    /// <param name="state">The state of the button (for button events).</param>
    /// <param name="scrollDelta">The scroll wheel delta (for scroll events).</param>
    /// <param name="timestamp">The timestamp when the event occurred.</param>
    public MouseEvent(string deviceId, MouseEventType eventType, Vector2 position,
                     MouseButton? button = null, InputState? state = null, 
                     Vector2? scrollDelta = null, DateTimeOffset? timestamp = null)
        : base(deviceId, timestamp)
    {
        EventType = eventType;
        Position = position;
        Button = button;
        State = state;
        ScrollDelta = scrollDelta ?? Vector2.Zero;
    }

    /// <summary>
    /// Gets the type of mouse event.
    /// </summary>
    public MouseEventType EventType { get; }

    /// <summary>
    /// Gets the mouse position when the event occurred.
    /// </summary>
    public Vector2 Position { get; }

    /// <summary>
    /// Gets the mouse button involved in this event (null for non-button events).
    /// </summary>
    public MouseButton? Button { get; }

    /// <summary>
    /// Gets the state of the button (null for non-button events).
    /// </summary>
    public InputState? State { get; }

    /// <summary>
    /// Gets the scroll wheel delta (zero for non-scroll events).
    /// </summary>
    public Vector2 ScrollDelta { get; }

    /// <summary>
    /// Gets a value indicating whether this is a button press event.
    /// </summary>
    public bool IsButtonPressed => EventType == MouseEventType.ButtonPress && State == InputState.Pressed;

    /// <summary>
    /// Gets a value indicating whether this is a button release event.
    /// </summary>
    public bool IsButtonReleased => EventType == MouseEventType.ButtonRelease && State == InputState.Released;

    /// <summary>
    /// Gets a value indicating whether this is a mouse move event.
    /// </summary>
    public bool IsMove => EventType == MouseEventType.Move;

    /// <summary>
    /// Gets a value indicating whether this is a scroll event.
    /// </summary>
    public bool IsScroll => EventType == MouseEventType.Scroll;
}

/// <summary>
/// Represents the type of mouse event.
/// </summary>
public enum MouseEventType
{
    /// <summary>Mouse button was pressed.</summary>
    ButtonPress,
    /// <summary>Mouse button was released.</summary>
    ButtonRelease,
    /// <summary>Mouse was moved.</summary>
    Move,
    /// <summary>Mouse wheel was scrolled.</summary>
    Scroll
}