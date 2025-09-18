using GameConsole.Input.Core.Types;
using GameConsole.Input.Core.Events;

namespace GameConsole.Input.Core.Devices;

/// <summary>
/// Base interface for all input devices.
/// </summary>
public interface IInputDevice
{
    /// <summary>
    /// Gets the unique identifier for this device.
    /// </summary>
    string DeviceId { get; }

    /// <summary>
    /// Gets the human-readable name of the device.
    /// </summary>
    string DeviceName { get; }

    /// <summary>
    /// Gets the device type.
    /// </summary>
    InputDeviceType DeviceType { get; }

    /// <summary>
    /// Gets a value indicating whether the device is currently connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Gets the last time this device had any input activity.
    /// </summary>
    DateTimeOffset LastActivity { get; }

    /// <summary>
    /// Gets additional metadata about the device.
    /// </summary>
    IReadOnlyDictionary<string, object> Metadata { get; }
}

/// <summary>
/// Represents the type of input device.
/// </summary>
public enum InputDeviceType
{
    /// <summary>Keyboard device.</summary>
    Keyboard,
    /// <summary>Mouse device.</summary>
    Mouse,
    /// <summary>Gamepad/controller device.</summary>
    Gamepad,
    /// <summary>Touch input device.</summary>
    Touch,
    /// <summary>Unknown or custom device type.</summary>
    Unknown
}

/// <summary>
/// Represents a keyboard input device.
/// </summary>
public interface IKeyboard : IInputDevice
{
    /// <summary>
    /// Gets the current state of all keys.
    /// </summary>
    IReadOnlyDictionary<KeyCode, InputStateInfo> KeyStates { get; }

    /// <summary>
    /// Checks if a specific key is currently pressed.
    /// </summary>
    /// <param name="keyCode">The key to check.</param>
    /// <returns>True if the key is pressed, false otherwise.</returns>
    bool IsKeyPressed(KeyCode keyCode);

    /// <summary>
    /// Checks if a specific key was just pressed this frame.
    /// </summary>
    /// <param name="keyCode">The key to check.</param>
    /// <returns>True if the key was just pressed, false otherwise.</returns>
    bool IsKeyDown(KeyCode keyCode);

    /// <summary>
    /// Checks if a specific key was just released this frame.
    /// </summary>
    /// <param name="keyCode">The key to check.</param>
    /// <returns>True if the key was just released, false otherwise.</returns>
    bool IsKeyUp(KeyCode keyCode);

    /// <summary>
    /// Gets the current modifier key state.
    /// </summary>
    /// <returns>The currently active modifier keys.</returns>
    KeyModifiers GetModifiers();
}

/// <summary>
/// Represents a mouse input device.
/// </summary>
public interface IMouse : IInputDevice
{
    /// <summary>
    /// Gets the current mouse position.
    /// </summary>
    Vector2 Position { get; }

    /// <summary>
    /// Gets the mouse movement delta since the last frame.
    /// </summary>
    Vector2 Delta { get; }

    /// <summary>
    /// Gets the scroll wheel delta since the last frame.
    /// </summary>
    Vector2 ScrollDelta { get; }

    /// <summary>
    /// Gets the current state of all mouse buttons.
    /// </summary>
    IReadOnlyDictionary<MouseButton, InputStateInfo> ButtonStates { get; }

    /// <summary>
    /// Checks if a specific mouse button is currently pressed.
    /// </summary>
    /// <param name="button">The button to check.</param>
    /// <returns>True if the button is pressed, false otherwise.</returns>
    bool IsButtonPressed(MouseButton button);

    /// <summary>
    /// Checks if a specific mouse button was just pressed this frame.
    /// </summary>
    /// <param name="button">The button to check.</param>
    /// <returns>True if the button was just pressed, false otherwise.</returns>
    bool IsButtonDown(MouseButton button);

    /// <summary>
    /// Checks if a specific mouse button was just released this frame.
    /// </summary>
    /// <param name="button">The button to check.</param>
    /// <returns>True if the button was just released, false otherwise.</returns>
    bool IsButtonUp(MouseButton button);
}

/// <summary>
/// Represents a gamepad input device.
/// </summary>
public interface IGamepad : IInputDevice
{
    /// <summary>
    /// Gets the player index associated with this gamepad.
    /// </summary>
    int PlayerIndex { get; }

    /// <summary>
    /// Gets the current state of all gamepad buttons.
    /// </summary>
    IReadOnlyDictionary<GamepadButton, InputStateInfo> ButtonStates { get; }

    /// <summary>
    /// Gets the current state of all gamepad axes.
    /// </summary>
    IReadOnlyDictionary<GamepadAxis, float> AxisStates { get; }

    /// <summary>
    /// Checks if a specific gamepad button is currently pressed.
    /// </summary>
    /// <param name="button">The button to check.</param>
    /// <returns>True if the button is pressed, false otherwise.</returns>
    bool IsButtonPressed(GamepadButton button);

    /// <summary>
    /// Checks if a specific gamepad button was just pressed this frame.
    /// </summary>
    /// <param name="button">The button to check.</param>
    /// <returns>True if the button was just pressed, false otherwise.</returns>
    bool IsButtonDown(GamepadButton button);

    /// <summary>
    /// Checks if a specific gamepad button was just released this frame.
    /// </summary>
    /// <param name="button">The button to check.</param>
    /// <returns>True if the button was just released, false otherwise.</returns>
    bool IsButtonUp(GamepadButton button);

    /// <summary>
    /// Gets the current value of a specific axis.
    /// </summary>
    /// <param name="axis">The axis to query.</param>
    /// <returns>The current axis value (-1.0 to 1.0 for sticks, 0.0 to 1.0 for triggers).</returns>
    float GetAxisValue(GamepadAxis axis);

    /// <summary>
    /// Gets the left stick position as a Vector2.
    /// </summary>
    Vector2 LeftStick { get; }

    /// <summary>
    /// Gets the right stick position as a Vector2.
    /// </summary>
    Vector2 RightStick { get; }

    /// <summary>
    /// Gets the left trigger value.
    /// </summary>
    float LeftTrigger { get; }

    /// <summary>
    /// Gets the right trigger value.
    /// </summary>
    float RightTrigger { get; }
}

/// <summary>
/// Contains detailed information about the state of an input.
/// </summary>
public readonly struct InputStateInfo
{
    /// <summary>
    /// Initializes a new instance of the InputStateInfo struct.
    /// </summary>
    /// <param name="isPressed">Whether the input is currently pressed.</param>
    /// <param name="pressedThisFrame">Whether the input was pressed this frame.</param>
    /// <param name="releasedThisFrame">Whether the input was released this frame.</param>
    /// <param name="pressDuration">How long the input has been pressed.</param>
    /// <param name="lastPressTime">When the input was last pressed.</param>
    /// <param name="lastReleaseTime">When the input was last released.</param>
    public InputStateInfo(bool isPressed, bool pressedThisFrame, bool releasedThisFrame,
                         TimeSpan pressDuration, DateTimeOffset? lastPressTime, DateTimeOffset? lastReleaseTime)
    {
        IsPressed = isPressed;
        PressedThisFrame = pressedThisFrame;
        ReleasedThisFrame = releasedThisFrame;
        PressDuration = pressDuration;
        LastPressTime = lastPressTime;
        LastReleaseTime = lastReleaseTime;
    }

    /// <summary>
    /// Gets a value indicating whether the input is currently pressed.
    /// </summary>
    public bool IsPressed { get; }

    /// <summary>
    /// Gets a value indicating whether the input was pressed this frame.
    /// </summary>
    public bool PressedThisFrame { get; }

    /// <summary>
    /// Gets a value indicating whether the input was released this frame.
    /// </summary>
    public bool ReleasedThisFrame { get; }

    /// <summary>
    /// Gets how long the input has been continuously pressed.
    /// </summary>
    public TimeSpan PressDuration { get; }

    /// <summary>
    /// Gets the timestamp when the input was last pressed.
    /// </summary>
    public DateTimeOffset? LastPressTime { get; }

    /// <summary>
    /// Gets the timestamp when the input was last released.
    /// </summary>
    public DateTimeOffset? LastReleaseTime { get; }
}