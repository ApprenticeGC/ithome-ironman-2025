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

/// <summary>
/// Capability interface for predictive input features.
/// Allows services to provide AI-powered input prediction and suggestion.
/// </summary>
public interface IPredictiveInputCapability : ICapabilityProvider
{
    /// <summary>
    /// Predicts the next likely input based on input history.
    /// </summary>
    /// <param name="history">Recent input history to analyze.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Predicted next input event.</returns>
    Task<InputPrediction> PredictNextInputAsync(InputHistory history, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets input suggestions for auto-completion scenarios.
    /// </summary>
    /// <param name="partialInput">Partial input sequence.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Suggested input completions.</returns>
    Task<IEnumerable<InputSuggestion>> GetInputSuggestionsAsync(IEnumerable<InputEvent> partialInput, CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for input macro recording and playback.
/// </summary>
public interface IInputRecordingCapability : ICapabilityProvider
{
    /// <summary>
    /// Starts recording input events.
    /// </summary>
    /// <param name="name">Name for the recording.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Recording session ID.</returns>
    Task<string> StartRecordingAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the current recording session.
    /// </summary>
    /// <param name="sessionId">Recording session ID.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The recorded input sequence.</returns>
    Task<InputSequence> StopRecordingAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Plays back a recorded input sequence.
    /// </summary>
    /// <param name="sequence">The input sequence to play back.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the playback operation.</returns>
    Task PlaybackSequenceAsync(InputSequence sequence, CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for advanced input mapping and customization.
/// </summary>
public interface IInputMappingCapability : ICapabilityProvider
{
    /// <summary>
    /// Maps a physical input to a logical action.
    /// </summary>
    /// <param name="physicalInput">The physical input (key, button, etc.).</param>
    /// <param name="logicalAction">The logical action to map to.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the mapping operation.</returns>
    Task MapInputAsync(string physicalInput, string logicalAction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current input mapping configuration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Current input mapping configuration.</returns>
    Task<InputMappingConfiguration> GetMappingConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the current input mapping configuration.
    /// </summary>
    /// <param name="configuration">Configuration to save.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the save operation.</returns>
    Task SaveMappingConfigurationAsync(InputMappingConfiguration configuration, CancellationToken cancellationToken = default);
}