using GameConsole.Core.Abstractions;
using GameConsole.Input.Core;

namespace GameConsole.Input.Services;

/// <summary>
/// Core input service interface for keyboard, mouse, and controller input.
/// Provides both event-driven and polling input models with device management.
/// </summary>
public interface IService : GameConsole.Core.Abstractions.IService
{
    // Core input handling (polling)
    /// <summary>
    /// Checks if a specific key is currently pressed.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the key is pressed, false otherwise.</returns>
    Task<bool> IsKeyPressedAsync(KeyCode key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current mouse position.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The current mouse position.</returns>
    Task<Vector2> GetMousePositionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific mouse button is currently pressed.
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the button is pressed, false otherwise.</returns>
    Task<bool> IsMouseButtonPressedAsync(MouseButton button, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific gamepad button is currently pressed.
    /// </summary>
    /// <param name="gamepadIndex">Index of the gamepad to check.</param>
    /// <param name="button">The gamepad button to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the button is pressed, false otherwise.</returns>
    Task<bool> IsGamepadButtonPressedAsync(int gamepadIndex, GamepadButton button, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current value of a gamepad axis.
    /// </summary>
    /// <param name="gamepadIndex">Index of the gamepad to check.</param>
    /// <param name="axis">The axis to get the value for.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The current axis value (-1.0 to 1.0).</returns>
    Task<float> GetGamepadAxisAsync(int gamepadIndex, GamepadAxis axis, CancellationToken cancellationToken = default);

    // Input events (event-driven)
    /// <summary>
    /// Event raised when keyboard input occurs.
    /// </summary>
    event EventHandler<KeyEvent>? KeyEvent;

    /// <summary>
    /// Event raised when mouse input occurs.
    /// </summary>
    event EventHandler<MouseEvent>? MouseEvent;

    /// <summary>
    /// Event raised when gamepad input occurs.
    /// </summary>
    event EventHandler<GamepadEvent>? GamepadEvent;

    // Device management
    /// <summary>
    /// Gets the number of connected gamepads.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The number of connected gamepads.</returns>
    Task<int> GetConnectedGamepadCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific gamepad is connected.
    /// </summary>
    /// <param name="gamepadIndex">Index of the gamepad to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the gamepad is connected, false otherwise.</returns>
    Task<bool> IsGamepadConnectedAsync(int gamepadIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the name of a connected gamepad.
    /// </summary>
    /// <param name="gamepadIndex">Index of the gamepad.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The name of the gamepad, or null if not connected.</returns>
    Task<string?> GetGamepadNameAsync(int gamepadIndex, CancellationToken cancellationToken = default);
}

