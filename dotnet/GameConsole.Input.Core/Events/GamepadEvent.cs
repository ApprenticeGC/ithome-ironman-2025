using GameConsole.Input.Core.Types;

namespace GameConsole.Input.Core.Events;

/// <summary>
/// Represents a gamepad input event.
/// </summary>
public class GamepadEvent : InputEventBase
{
    /// <summary>
    /// Initializes a new instance of the GamepadEvent class for button events.
    /// </summary>
    /// <param name="deviceId">The gamepad device ID.</param>
    /// <param name="playerId">The player ID associated with this gamepad.</param>
    /// <param name="button">The gamepad button.</param>
    /// <param name="state">The button state.</param>
    /// <param name="timestamp">The timestamp when the event occurred.</param>
    public GamepadEvent(string deviceId, int playerId, GamepadButton button, InputState state, DateTimeOffset? timestamp = null)
        : base(deviceId, timestamp)
    {
        PlayerId = playerId;
        EventType = GamepadEventType.Button;
        Button = button;
        ButtonState = state;
    }

    /// <summary>
    /// Initializes a new instance of the GamepadEvent class for axis events.
    /// </summary>
    /// <param name="deviceId">The gamepad device ID.</param>
    /// <param name="playerId">The player ID associated with this gamepad.</param>
    /// <param name="axis">The gamepad axis.</param>
    /// <param name="value">The axis value.</param>
    /// <param name="timestamp">The timestamp when the event occurred.</param>
    public GamepadEvent(string deviceId, int playerId, GamepadAxis axis, float value, DateTimeOffset? timestamp = null)
        : base(deviceId, timestamp)
    {
        PlayerId = playerId;
        EventType = GamepadEventType.Axis;
        Axis = axis;
        AxisValue = value;
    }

    /// <summary>
    /// Initializes a new instance of the GamepadEvent class for connection events.
    /// </summary>
    /// <param name="deviceId">The gamepad device ID.</param>
    /// <param name="playerId">The player ID associated with this gamepad.</param>
    /// <param name="connected">Whether the gamepad was connected or disconnected.</param>
    /// <param name="timestamp">The timestamp when the event occurred.</param>
    public GamepadEvent(string deviceId, int playerId, bool connected, DateTimeOffset? timestamp = null)
        : base(deviceId, timestamp)
    {
        PlayerId = playerId;
        EventType = connected ? GamepadEventType.Connected : GamepadEventType.Disconnected;
    }

    /// <summary>
    /// Gets the player ID associated with this gamepad.
    /// </summary>
    public int PlayerId { get; }

    /// <summary>
    /// Gets the type of gamepad event.
    /// </summary>
    public GamepadEventType EventType { get; }

    /// <summary>
    /// Gets the gamepad button (for button events).
    /// </summary>
    public GamepadButton? Button { get; }

    /// <summary>
    /// Gets the button state (for button events).
    /// </summary>
    public InputState? ButtonState { get; }

    /// <summary>
    /// Gets the gamepad axis (for axis events).
    /// </summary>
    public GamepadAxis? Axis { get; }

    /// <summary>
    /// Gets the axis value (for axis events).
    /// </summary>
    public float? AxisValue { get; }

    /// <summary>
    /// Gets a value indicating whether this is a button event.
    /// </summary>
    public bool IsButtonEvent => EventType == GamepadEventType.Button;

    /// <summary>
    /// Gets a value indicating whether this is an axis event.
    /// </summary>
    public bool IsAxisEvent => EventType == GamepadEventType.Axis;

    /// <summary>
    /// Gets a value indicating whether this is a connection event.
    /// </summary>
    public bool IsConnectionEvent => EventType == GamepadEventType.Connected || EventType == GamepadEventType.Disconnected;

    /// <summary>
    /// Gets a value indicating whether this is a button press event.
    /// </summary>
    public bool IsButtonPressed => IsButtonEvent && ButtonState == InputState.Pressed;

    /// <summary>
    /// Gets a value indicating whether this is a button release event.
    /// </summary>
    public bool IsButtonReleased => IsButtonEvent && ButtonState == InputState.Released;
}

/// <summary>
/// Represents the type of gamepad event.
/// </summary>
public enum GamepadEventType
{
    /// <summary>Gamepad button event.</summary>
    Button,
    /// <summary>Gamepad axis event.</summary>
    Axis,
    /// <summary>Gamepad connected event.</summary>
    Connected,
    /// <summary>Gamepad disconnected event.</summary>
    Disconnected
}