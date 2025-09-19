namespace GameConsole.Input.Core;

/// <summary>
/// State of an input key or button.
/// </summary>
public enum InputState
{
    /// <summary>
    /// Input is not pressed.
    /// </summary>
    Released,
    
    /// <summary>
    /// Input was just pressed this frame.
    /// </summary>
    Pressed,
    
    /// <summary>
    /// Input is being held down.
    /// </summary>
    Held,
    
    /// <summary>
    /// Input was just released this frame.
    /// </summary>
    JustReleased
}

/// <summary>
/// Base class for all input events.
/// </summary>
public abstract class InputEvent
{
    /// <summary>
    /// Timestamp when the event occurred.
    /// </summary>
    public DateTime Timestamp { get; }
    
    /// <summary>
    /// Frame number when the event occurred.
    /// </summary>
    public long Frame { get; }
    
    /// <summary>
    /// Initializes a new instance of the InputEvent class.
    /// </summary>
    /// <param name="timestamp">When the event occurred.</param>
    /// <param name="frame">Frame number when the event occurred.</param>
    protected InputEvent(DateTime timestamp, long frame)
    {
        Timestamp = timestamp;
        Frame = frame;
    }
}

/// <summary>
/// Represents a keyboard input event.
/// </summary>
public class KeyEvent : InputEvent
{
    /// <summary>
    /// The key that generated this event.
    /// </summary>
    public KeyCode Key { get; }
    
    /// <summary>
    /// The state of the key.
    /// </summary>
    public InputState State { get; }
    
    /// <summary>
    /// Modifier keys that were pressed during this event.
    /// </summary>
    public KeyModifiers Modifiers { get; }
    
    /// <summary>
    /// Initializes a new instance of the KeyEvent class.
    /// </summary>
    /// <param name="key">The key that generated the event.</param>
    /// <param name="state">The state of the key.</param>
    /// <param name="modifiers">Modifier keys pressed.</param>
    /// <param name="timestamp">When the event occurred.</param>
    /// <param name="frame">Frame number when the event occurred.</param>
    public KeyEvent(KeyCode key, InputState state, KeyModifiers modifiers, DateTime timestamp, long frame)
        : base(timestamp, frame)
    {
        Key = key;
        State = state;
        Modifiers = modifiers;
    }
}

/// <summary>
/// Represents modifier keys that can be combined.
/// </summary>
[Flags]
public enum KeyModifiers
{
    None = 0,
    Shift = 1 << 0,
    Control = 1 << 1,
    Alt = 1 << 2,
    Command = 1 << 3
}

/// <summary>
/// Represents a mouse input event.
/// </summary>
public class MouseEvent : InputEvent
{
    /// <summary>
    /// Mouse position when the event occurred.
    /// </summary>
    public Vector2 Position { get; }
    
    /// <summary>
    /// Mouse delta movement since the last event.
    /// </summary>
    public Vector2 Delta { get; }
    
    /// <summary>
    /// Scroll wheel delta if this is a scroll event.
    /// </summary>
    public Vector2 ScrollDelta { get; }
    
    /// <summary>
    /// The button involved in this event, if any.
    /// </summary>
    public MouseButton? Button { get; }
    
    /// <summary>
    /// The state of the button, if any.
    /// </summary>
    public InputState? ButtonState { get; }
    
    /// <summary>
    /// Initializes a new instance of the MouseEvent class.
    /// </summary>
    /// <param name="position">Mouse position.</param>
    /// <param name="delta">Mouse movement delta.</param>
    /// <param name="scrollDelta">Scroll wheel delta.</param>
    /// <param name="button">Button involved, if any.</param>
    /// <param name="buttonState">Button state, if any.</param>
    /// <param name="timestamp">When the event occurred.</param>
    /// <param name="frame">Frame number when the event occurred.</param>
    public MouseEvent(Vector2 position, Vector2 delta, Vector2 scrollDelta, MouseButton? button, InputState? buttonState, DateTime timestamp, long frame)
        : base(timestamp, frame)
    {
        Position = position;
        Delta = delta;
        ScrollDelta = scrollDelta;
        Button = button;
        ButtonState = buttonState;
    }
}

/// <summary>
/// Represents a gamepad input event.
/// </summary>
public class GamepadEvent : InputEvent
{
    /// <summary>
    /// Index of the gamepad that generated this event.
    /// </summary>
    public int GamepadIndex { get; }
    
    /// <summary>
    /// The button involved in this event, if any.
    /// </summary>
    public GamepadButton? Button { get; }
    
    /// <summary>
    /// The state of the button, if any.
    /// </summary>
    public InputState? ButtonState { get; }
    
    /// <summary>
    /// The axis involved in this event, if any.
    /// </summary>
    public GamepadAxis? Axis { get; }
    
    /// <summary>
    /// The value of the axis, if any.
    /// </summary>
    public float? AxisValue { get; }
    
    /// <summary>
    /// Initializes a new instance of the GamepadEvent class.
    /// </summary>
    /// <param name="gamepadIndex">Index of the gamepad.</param>
    /// <param name="button">Button involved, if any.</param>
    /// <param name="buttonState">Button state, if any.</param>
    /// <param name="axis">Axis involved, if any.</param>
    /// <param name="axisValue">Axis value, if any.</param>
    /// <param name="timestamp">When the event occurred.</param>
    /// <param name="frame">Frame number when the event occurred.</param>
    public GamepadEvent(int gamepadIndex, GamepadButton? button, InputState? buttonState, GamepadAxis? axis, float? axisValue, DateTime timestamp, long frame)
        : base(timestamp, frame)
    {
        GamepadIndex = gamepadIndex;
        Button = button;
        ButtonState = buttonState;
        Axis = axis;
        AxisValue = axisValue;
    }
}