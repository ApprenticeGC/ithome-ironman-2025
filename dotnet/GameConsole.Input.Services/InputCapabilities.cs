using GameConsole.Core.Abstractions;
using GameConsole.Input.Core;

namespace GameConsole.Input.Services;

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