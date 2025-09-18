using GameConsole.Input.Core.Types;

namespace GameConsole.Input.Core.Events;

/// <summary>
/// Base interface for all input events.
/// </summary>
public interface IInputEvent
{
    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the device ID that generated this event.
    /// </summary>
    string DeviceId { get; }

    /// <summary>
    /// Gets a value indicating whether this event was handled.
    /// </summary>
    bool IsHandled { get; set; }
}

/// <summary>
/// Base class for input events providing common functionality.
/// </summary>
public abstract class InputEventBase : IInputEvent
{
    /// <summary>
    /// Initializes a new instance of the InputEventBase class.
    /// </summary>
    /// <param name="deviceId">The device ID that generated this event.</param>
    /// <param name="timestamp">The timestamp when the event occurred.</param>
    protected InputEventBase(string deviceId, DateTimeOffset? timestamp = null)
    {
        DeviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
        Timestamp = timestamp ?? DateTimeOffset.UtcNow;
    }

    /// <inheritdoc />
    public DateTimeOffset Timestamp { get; }

    /// <inheritdoc />
    public string DeviceId { get; }

    /// <inheritdoc />
    public bool IsHandled { get; set; }
}

/// <summary>
/// Represents the state of a key or button input.
/// </summary>
public enum InputState
{
    /// <summary>The input was just pressed this frame.</summary>
    Pressed,
    /// <summary>The input is currently held down.</summary>
    Held,
    /// <summary>The input was just released this frame.</summary>
    Released
}

/// <summary>
/// Represents a keyboard input event.
/// </summary>
public class KeyEvent : InputEventBase
{
    /// <summary>
    /// Initializes a new instance of the KeyEvent class.
    /// </summary>
    /// <param name="deviceId">The keyboard device ID.</param>
    /// <param name="keyCode">The key that was pressed or released.</param>
    /// <param name="state">The current state of the key.</param>
    /// <param name="modifiers">The modifier keys that were active.</param>
    /// <param name="timestamp">The timestamp when the event occurred.</param>
    public KeyEvent(string deviceId, KeyCode keyCode, InputState state, 
                   KeyModifiers modifiers = KeyModifiers.None, DateTimeOffset? timestamp = null)
        : base(deviceId, timestamp)
    {
        KeyCode = keyCode;
        State = state;
        Modifiers = modifiers;
    }

    /// <summary>
    /// Gets the key that was pressed or released.
    /// </summary>
    public KeyCode KeyCode { get; }

    /// <summary>
    /// Gets the current state of the key.
    /// </summary>
    public InputState State { get; }

    /// <summary>
    /// Gets the modifier keys that were active when this event occurred.
    /// </summary>
    public KeyModifiers Modifiers { get; }

    /// <summary>
    /// Gets a value indicating whether this is a key press event.
    /// </summary>
    public bool IsPressed => State == InputState.Pressed;

    /// <summary>
    /// Gets a value indicating whether this is a key release event.
    /// </summary>
    public bool IsReleased => State == InputState.Released;

    /// <summary>
    /// Gets a value indicating whether this key is being held.
    /// </summary>
    public bool IsHeld => State == InputState.Held;
}

/// <summary>
/// Represents modifier keys that can be combined using bitwise operations.
/// </summary>
[Flags]
public enum KeyModifiers
{
    /// <summary>No modifier keys.</summary>
    None = 0,
    /// <summary>Shift key modifier.</summary>
    Shift = 1 << 0,
    /// <summary>Control key modifier.</summary>
    Control = 1 << 1,
    /// <summary>Alt key modifier.</summary>
    Alt = 1 << 2,
    /// <summary>Command/Windows key modifier.</summary>
    Command = 1 << 3,
    /// <summary>Caps Lock modifier.</summary>
    CapsLock = 1 << 4,
    /// <summary>Num Lock modifier.</summary>
    NumLock = 1 << 5
}