using GameConsole.Core.Abstractions;

namespace GameConsole.Audio.Services;

/// <summary>
/// Core audio service interface for game audio playback and management.
/// Supports basic audio operations with optional advanced capabilities.
/// </summary>
public interface IService : GameConsole.Core.Abstractions.IService
{
    // Core audio functionality (required)
    /// <summary>
    /// Plays an audio file asynchronously.
    /// </summary>
    /// <param name="path">Path to the audio file.</param>
    /// <param name="category">Audio category for volume management (default: "SFX").</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if playback started successfully, false otherwise.</returns>
    Task<bool> PlayAsync(string path, string category = "SFX", CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops playback of a specific audio file.
    /// </summary>
    /// <param name="path">Path to the audio file to stop.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async stop operation.</returns>
    Task StopAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops all currently playing audio.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async stop all operation.</returns>
    Task StopAllAsync(CancellationToken cancellationToken = default);

    // Volume management
    /// <summary>
    /// Sets the master volume level.
    /// </summary>
    /// <param name="volume">Volume level (0.0 to 1.0).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async volume set operation.</returns>
    Task SetMasterVolumeAsync(float volume, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the volume level for a specific audio category.
    /// </summary>
    /// <param name="category">Audio category name.</param>
    /// <param name="volume">Volume level (0.0 to 1.0).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async volume set operation.</returns>
    Task SetCategoryVolumeAsync(string category, float volume, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current volume level for a specific audio category.
    /// </summary>
    /// <param name="category">Audio category name.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The current volume level for the category.</returns>
    Task<float> GetCategoryVolumeAsync(string category, CancellationToken cancellationToken = default);
}

/// <summary>
/// Optional capability interface for spatial audio positioning.
/// Allows services to provide 3D audio positioning capabilities.
/// </summary>
public interface ISpatialAudioCapability : ICapabilityProvider
{
    /// <summary>
    /// Sets the listener's position in 3D space.
    /// </summary>
    /// <param name="position">3D position of the listener.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async operation.</returns>
    Task SetListenerPositionAsync(GameConsole.Audio.Core.Vector3 position, CancellationToken cancellationToken = default);

    /// <summary>
    /// Plays audio at a specific 3D position.
    /// </summary>
    /// <param name="path">Path to the audio file.</param>
    /// <param name="position">3D position where the audio should be played.</param>
    /// <param name="volume">Volume level for this audio instance.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async operation.</returns>
    Task Play3DAudioAsync(string path, GameConsole.Audio.Core.Vector3 position, float volume, CancellationToken cancellationToken = default);
}

/// <summary>
/// Optional capability interface for advanced audio mixing operations.
/// </summary>
public interface IAudioMixingCapability : ICapabilityProvider
{
    /// <summary>
    /// Applies audio effects to a specific channel.
    /// </summary>
    /// <param name="channel">Audio channel to apply effects to.</param>
    /// <param name="effects">Dictionary of effect names and their parameters.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async operation.</returns>
    Task ApplyEffectsAsync(GameConsole.Audio.Core.AudioChannel channel, Dictionary<string, object> effects, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current audio levels for all channels.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Dictionary of channel names and their current audio levels.</returns>
    Task<Dictionary<string, float>> GetAudioLevelsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Optional capability interface for streaming audio from large files.
/// </summary>
public interface IAudioStreamingCapability : ICapabilityProvider
{
    /// <summary>
    /// Starts streaming audio from a large file.
    /// </summary>
    /// <param name="path">Path to the audio file to stream.</param>
    /// <param name="bufferSizeMs">Buffer size in milliseconds.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Stream ID for managing the stream.</returns>
    Task<string> StartStreamAsync(string path, int bufferSizeMs = 1000, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops a streaming audio session.
    /// </summary>
    /// <param name="streamId">ID of the stream to stop.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async operation.</returns>
    Task StopStreamAsync(string streamId, CancellationToken cancellationToken = default);
}