using GameConsole.Core.Abstractions;
using System.Numerics;

namespace GameConsole.Audio.Services;

/// <summary>
/// Core audio service interface for audio playback and management.
/// Supports basic audio operations with optional advanced capabilities.
/// </summary>
public interface IService : GameConsole.Core.Abstractions.IService
{
    // Core audio functionality (required)
    /// <summary>
    /// Plays an audio file from the specified path.
    /// </summary>
    /// <param name="path">Path to the audio file.</param>
    /// <param name="category">Audio category (e.g., "SFX", "Music", "Voice").</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if playback started successfully, false otherwise.</returns>
    Task<bool> PlayAsync(string path, string category = "SFX", CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops playback of a specific audio file.
    /// </summary>
    /// <param name="path">Path to the audio file to stop.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async operation.</returns>
    Task StopAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops all currently playing audio.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async operation.</returns>
    Task StopAllAsync(CancellationToken cancellationToken = default);

    // Volume management
    /// <summary>
    /// Sets the master volume for all audio.
    /// </summary>
    /// <param name="volume">Volume level (0.0 to 1.0).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async operation.</returns>
    Task SetMasterVolumeAsync(float volume, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the volume for a specific audio category.
    /// </summary>
    /// <param name="category">Audio category name.</param>
    /// <param name="volume">Volume level (0.0 to 1.0).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async operation.</returns>
    Task SetCategoryVolumeAsync(string category, float volume, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current volume for a specific audio category.
    /// </summary>
    /// <param name="category">Audio category name.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Current volume level (0.0 to 1.0).</returns>
    Task<float> GetCategoryVolumeAsync(string category, CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for spatial audio features.
/// Allows services to provide 3D positioned audio support.
/// </summary>
public interface ISpatialAudioCapability : ICapabilityProvider
{
    /// <summary>
    /// Sets the listener's position and orientation in 3D space.
    /// </summary>
    /// <param name="position">Listener position.</param>
    /// <param name="forward">Forward direction vector.</param>
    /// <param name="up">Up direction vector.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async operation.</returns>
    Task SetListenerTransformAsync(Vector3 position, Vector3 forward, Vector3 up, CancellationToken cancellationToken = default);

    /// <summary>
    /// Plays 3D positioned audio at the specified location.
    /// </summary>
    /// <param name="path">Path to the audio file.</param>
    /// <param name="position">3D position of the audio source.</param>
    /// <param name="volume">Volume level (0.0 to 1.0).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async operation.</returns>
    Task Play3DAudioAsync(string path, Vector3 position, float volume = 1.0f, CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for audio mixer features.
/// Provides advanced volume control and channel management.
/// </summary>
public interface IAudioMixerCapability : ICapabilityProvider
{
    /// <summary>
    /// Creates a new audio channel.
    /// </summary>
    /// <param name="channelName">Name of the channel to create.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async operation.</returns>
    Task CreateChannelAsync(string channelName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the volume for a specific channel.
    /// </summary>
    /// <param name="channelName">Name of the channel.</param>
    /// <param name="volume">Volume level (0.0 to 1.0).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async operation.</returns>
    Task SetChannelVolumeAsync(string channelName, float volume, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies audio effects to a channel.
    /// </summary>
    /// <param name="channelName">Name of the channel.</param>
    /// <param name="effects">Audio effects to apply.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async operation.</returns>
    Task ApplyChannelEffectsAsync(string channelName, AudioEffects effects, CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for audio device management.
/// Provides hardware abstraction and device selection.
/// </summary>
public interface IAudioDeviceCapability : ICapabilityProvider
{
    /// <summary>
    /// Gets all available audio output devices.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>List of available audio devices.</returns>
    Task<IEnumerable<AudioDevice>> GetAudioDevicesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the active audio output device.
    /// </summary>
    /// <param name="deviceId">ID of the device to set as active.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async operation.</returns>
    Task SetActiveDeviceAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the currently active audio output device.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Currently active audio device, or null if none set.</returns>
    Task<AudioDevice?> GetActiveDeviceAsync(CancellationToken cancellationToken = default);
}