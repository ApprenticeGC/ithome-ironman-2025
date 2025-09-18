using GameConsole.Core.Abstractions;
using GameConsole.Input.Core.Devices;
using GameConsole.Input.Core.Events;
using GameConsole.Input.Core.Types;
using System.Reactive;

namespace GameConsole.Input.Core;

/// <summary>
/// Main interface for the input service providing comprehensive input handling capabilities.
/// Supports keyboard, mouse, and gamepad input with both event-driven and polling models.
/// </summary>
public interface IInputService : IService, ICapabilityProvider
{
    #region Device Management

    /// <summary>
    /// Gets all currently connected input devices.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A collection of connected input devices.</returns>
    Task<IEnumerable<IInputDevice>> GetConnectedDevicesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets input devices of a specific type.
    /// </summary>
    /// <typeparam name="T">The device type to retrieve.</typeparam>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A collection of devices of the specified type.</returns>
    Task<IEnumerable<T>> GetDevicesOfTypeAsync<T>(CancellationToken cancellationToken = default) where T : class, IInputDevice;

    /// <summary>
    /// Gets a specific input device by its ID.
    /// </summary>
    /// <param name="deviceId">The device ID to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The input device, or null if not found.</returns>
    Task<IInputDevice?> GetDeviceAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the primary keyboard device.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The primary keyboard device, or null if none available.</returns>
    Task<IKeyboard?> GetPrimaryKeyboardAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the primary mouse device.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The primary mouse device, or null if none available.</returns>
    Task<IMouse?> GetPrimaryMouseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets gamepad devices for specific player indices.
    /// </summary>
    /// <param name="playerIndex">The player index (0-based), or null for all gamepads.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Gamepad devices for the specified player or all gamepads.</returns>
    Task<IEnumerable<IGamepad>> GetGamepadsAsync(int? playerIndex = null, CancellationToken cancellationToken = default);

    #endregion

    #region Event-Driven Input

    /// <summary>
    /// Observable stream of all input events from all devices.
    /// </summary>
    IObservable<IInputEvent> AllInputEvents { get; }

    /// <summary>
    /// Observable stream of keyboard events.
    /// </summary>
    IObservable<KeyEvent> KeyEvents { get; }

    /// <summary>
    /// Observable stream of mouse events.
    /// </summary>
    IObservable<MouseEvent> MouseEvents { get; }

    /// <summary>
    /// Observable stream of gamepad events.
    /// </summary>
    IObservable<GamepadEvent> GamepadEvents { get; }

    /// <summary>
    /// Observable stream of device connection/disconnection events.
    /// </summary>
    IObservable<DeviceConnectionEvent> DeviceEvents { get; }

    #endregion

    #region Polling Interface

    /// <summary>
    /// Updates the input system state. Should be called once per frame for consistent timing.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the last update.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task UpdateAsync(TimeSpan deltaTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific key is currently pressed on the primary keyboard.
    /// </summary>
    /// <param name="keyCode">The key to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the key is pressed, false otherwise.</returns>
    Task<bool> IsKeyPressedAsync(KeyCode keyCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific key was just pressed this frame on the primary keyboard.
    /// </summary>
    /// <param name="keyCode">The key to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the key was just pressed, false otherwise.</returns>
    Task<bool> IsKeyDownAsync(KeyCode keyCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific key was just released this frame on the primary keyboard.
    /// </summary>
    /// <param name="keyCode">The key to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the key was just released, false otherwise.</returns>
    Task<bool> IsKeyUpAsync(KeyCode keyCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current mouse position from the primary mouse.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The current mouse position.</returns>
    Task<Vector2> GetMousePositionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the mouse movement delta since the last frame from the primary mouse.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The mouse movement delta.</returns>
    Task<Vector2> GetMouseDeltaAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific mouse button is currently pressed on the primary mouse.
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the button is pressed, false otherwise.</returns>
    Task<bool> IsMouseButtonPressedAsync(MouseButton button, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific mouse button was just pressed this frame on the primary mouse.
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the button was just pressed, false otherwise.</returns>
    Task<bool> IsMouseButtonDownAsync(MouseButton button, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific mouse button was just released this frame on the primary mouse.
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the button was just released, false otherwise.</returns>
    Task<bool> IsMouseButtonUpAsync(MouseButton button, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific gamepad button is currently pressed.
    /// </summary>
    /// <param name="button">The gamepad button to check.</param>
    /// <param name="playerIndex">The player index (0-based), or null for primary gamepad.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the button is pressed, false otherwise.</returns>
    Task<bool> IsGamepadButtonPressedAsync(GamepadButton button, int? playerIndex = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current value of a gamepad axis.
    /// </summary>
    /// <param name="axis">The gamepad axis to query.</param>
    /// <param name="playerIndex">The player index (0-based), or null for primary gamepad.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The current axis value (-1.0 to 1.0 for sticks, 0.0 to 1.0 for triggers).</returns>
    Task<float> GetGamepadAxisValueAsync(GamepadAxis axis, int? playerIndex = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the left stick position as a Vector2.
    /// </summary>
    /// <param name="playerIndex">The player index (0-based), or null for primary gamepad.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The left stick position.</returns>
    Task<Vector2> GetGamepadLeftStickAsync(int? playerIndex = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the right stick position as a Vector2.
    /// </summary>
    /// <param name="playerIndex">The player index (0-based), or null for primary gamepad.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The right stick position.</returns>
    Task<Vector2> GetGamepadRightStickAsync(int? playerIndex = null, CancellationToken cancellationToken = default);

    #endregion

    #region Configuration

    /// <summary>
    /// Sets the dead zone for analog inputs on a specific device.
    /// </summary>
    /// <param name="deviceId">The device ID.</param>
    /// <param name="deadZone">The dead zone value (0.0 to 1.0).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetDeadZoneAsync(string deviceId, float deadZone, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current dead zone for a specific device.
    /// </summary>
    /// <param name="deviceId">The device ID.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The current dead zone value.</returns>
    Task<float> GetDeadZoneAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the update frequency for input polling.
    /// </summary>
    /// <param name="frequency">The update frequency in Hz.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetUpdateFrequencyAsync(int frequency, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current input update frequency.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The current update frequency in Hz.</returns>
    Task<int> GetUpdateFrequencyAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Input State History

    /// <summary>
    /// Gets the input state history for a specific device.
    /// </summary>
    /// <param name="deviceId">The device ID.</param>
    /// <param name="duration">How far back to retrieve history.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Input state history for the device.</returns>
    Task<IEnumerable<IInputEvent>> GetInputHistoryAsync(string deviceId, TimeSpan duration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears input history for a specific device.
    /// </summary>
    /// <param name="deviceId">The device ID, or null for all devices.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ClearInputHistoryAsync(string? deviceId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the maximum duration to keep input history.
    /// </summary>
    /// <param name="duration">The maximum history duration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetHistoryDurationAsync(TimeSpan duration, CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Represents a device connection or disconnection event.
/// </summary>
public class DeviceConnectionEvent : InputEventBase
{
    /// <summary>
    /// Initializes a new instance of the DeviceConnectionEvent class.
    /// </summary>
    /// <param name="device">The device that was connected or disconnected.</param>
    /// <param name="connected">Whether the device was connected or disconnected.</param>
    /// <param name="timestamp">The timestamp when the event occurred.</param>
    public DeviceConnectionEvent(IInputDevice device, bool connected, DateTimeOffset? timestamp = null)
        : base(device?.DeviceId ?? "unknown", timestamp)
    {
        Device = device ?? throw new ArgumentNullException(nameof(device));
        Connected = connected;
    }

    /// <summary>
    /// Gets the device that was connected or disconnected.
    /// </summary>
    public IInputDevice Device { get; }

    /// <summary>
    /// Gets a value indicating whether the device was connected (true) or disconnected (false).
    /// </summary>
    public bool Connected { get; }

    /// <summary>
    /// Gets a value indicating whether this is a connection event.
    /// </summary>
    public bool IsConnection => Connected;

    /// <summary>
    /// Gets a value indicating whether this is a disconnection event.
    /// </summary>
    public bool IsDisconnection => !Connected;
}