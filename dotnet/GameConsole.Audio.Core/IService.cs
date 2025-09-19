using GameConsole.Core.Abstractions;
using GameConsole.Audio.Core;
using System.Numerics;
using System.Reactive;

namespace GameConsole.Audio.Services;

/// <summary>
/// Core audio service interface for game audio playback and management.
/// Provides unified interface for playing, controlling, and managing audio across different categories.
/// Supports basic audio operations with optional advanced capabilities through the capability system.
/// </summary>
public interface IService : GameConsole.Core.Abstractions.IService
{
    #region Core Audio Operations

    /// <summary>
    /// Plays audio from the specified path with optional configuration.
    /// </summary>
    /// <param name="path">Path or identifier of the audio resource.</param>
    /// <param name="category">Audio category for volume and processing control.</param>
    /// <param name="config">Optional playback configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Unique source identifier for the playing audio, or null if playback failed.</returns>
    Task<string?> PlayAsync(string path, AudioCategory category = AudioCategory.SFX, AudioPlaybackConfig? config = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops audio playback for the specified source.
    /// </summary>
    /// <param name="sourceId">Unique identifier of the audio source to stop.</param>
    /// <param name="fadeOut">Optional fade-out duration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the audio was successfully stopped.</returns>
    Task<bool> StopAsync(string sourceId, TimeSpan? fadeOut = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops all audio playback, optionally filtered by category.
    /// </summary>
    /// <param name="category">Optional category filter. If null, stops all audio.</param>
    /// <param name="fadeOut">Optional fade-out duration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Number of audio sources that were stopped.</returns>
    Task<int> StopAllAsync(AudioCategory? category = null, TimeSpan? fadeOut = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses audio playback for the specified source.
    /// </summary>
    /// <param name="sourceId">Unique identifier of the audio source to pause.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the audio was successfully paused.</returns>
    Task<bool> PauseAsync(string sourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes audio playback for the specified source.
    /// </summary>
    /// <param name="sourceId">Unique identifier of the audio source to resume.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the audio was successfully resumed.</returns>
    Task<bool> ResumeAsync(string sourceId, CancellationToken cancellationToken = default);

    #endregion

    #region Volume Management

    /// <summary>
    /// Sets the master volume level that affects all audio.
    /// </summary>
    /// <param name="volume">Volume level from 0.0 (silent) to 1.0 (full volume).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async operation.</returns>
    Task SetMasterVolumeAsync(float volume, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current master volume level.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Current master volume level (0.0 to 1.0).</returns>
    Task<float> GetMasterVolumeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the volume level for a specific audio category.
    /// </summary>
    /// <param name="category">Audio category to adjust.</param>
    /// <param name="volume">Volume level from 0.0 (silent) to 1.0 (full volume).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async operation.</returns>
    Task SetCategoryVolumeAsync(AudioCategory category, float volume, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the volume level for a specific audio category.
    /// </summary>
    /// <param name="category">Audio category to query.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Current category volume level (0.0 to 1.0).</returns>
    Task<float> GetCategoryVolumeAsync(AudioCategory category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the volume for a specific audio source.
    /// </summary>
    /// <param name="sourceId">Unique identifier of the audio source.</param>
    /// <param name="volume">Volume level from 0.0 (silent) to 1.0 (full volume).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the volume was successfully set.</returns>
    Task<bool> SetSourceVolumeAsync(string sourceId, float volume, CancellationToken cancellationToken = default);

    #endregion

    #region Audio State Queries

    /// <summary>
    /// Gets the current state of an audio source.
    /// </summary>
    /// <param name="sourceId">Unique identifier of the audio source.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Current audio state, or null if source not found.</returns>
    Task<AudioState?> GetSourceStateAsync(string sourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metadata about an audio resource.
    /// </summary>
    /// <param name="path">Path or identifier of the audio resource.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Audio metadata, or null if resource not found.</returns>
    Task<AudioMetadata?> GetAudioMetadataAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a list of all currently active audio sources.
    /// </summary>
    /// <param name="category">Optional category filter.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>List of active audio source identifiers.</returns>
    Task<IReadOnlyList<string>> GetActiveSourcesAsync(AudioCategory? category = null, CancellationToken cancellationToken = default);

    #endregion

    #region Event Streams

    /// <summary>
    /// Observable stream of all audio events.
    /// </summary>
    IObservable<IAudioEvent> AudioEvents { get; }

    /// <summary>
    /// Observable stream of audio state changes.
    /// </summary>
    IObservable<AudioStateChangedEvent> StateChanges { get; }

    /// <summary>
    /// Observable stream of volume change events.
    /// </summary>
    IObservable<AudioVolumeChangedEvent> VolumeChanges { get; }

    #endregion
}

/// <summary>
/// Capability interface for spatial audio features.
/// Provides 3D audio positioning and environmental audio effects.
/// </summary>
public interface ISpatialAudioCapability : ICapabilityProvider
{
    /// <summary>
    /// Sets the position and orientation of the audio listener (typically the player/camera).
    /// </summary>
    /// <param name="position">3D position of the listener.</param>
    /// <param name="forward">Forward direction vector.</param>
    /// <param name="up">Up direction vector.</param>
    /// <param name="velocity">Velocity for Doppler effect.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async operation.</returns>
    Task SetListenerTransformAsync(Vector3 position, Vector3 forward, Vector3 up, Vector3? velocity = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the 3D position of an audio source.
    /// </summary>
    /// <param name="sourceId">Unique identifier of the audio source.</param>
    /// <param name="position">New 3D position.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the position was successfully updated.</returns>
    Task<bool> SetSourcePositionAsync(string sourceId, AudioPosition position, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets environmental audio properties (reverb, echo, etc.).
    /// </summary>
    /// <param name="environment">Environmental preset to apply.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async operation.</returns>
    Task SetEnvironmentAsync(AudioEnvironment environment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current listener position and orientation.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Current listener transform information.</returns>
    Task<(Vector3 position, Vector3 forward, Vector3 up)> GetListenerTransformAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for advanced volume control and audio mixing.
/// </summary>
public interface IVolumeControlCapability : ICapabilityProvider
{
    /// <summary>
    /// Applies a volume fade over time to an audio source.
    /// </summary>
    /// <param name="sourceId">Unique identifier of the audio source.</param>
    /// <param name="targetVolume">Target volume level (0.0 to 1.0).</param>
    /// <param name="duration">Duration of the fade.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the fade was successfully started.</returns>
    Task<bool> FadeVolumeAsync(string sourceId, float targetVolume, TimeSpan duration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a volume snapshot that can be restored later.
    /// </summary>
    /// <param name="name">Name for the snapshot.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Unique identifier for the snapshot.</returns>
    Task<string> CreateVolumeSnapshotAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores volume levels from a previously created snapshot.
    /// </summary>
    /// <param name="snapshotId">Identifier of the snapshot to restore.</param>
    /// <param name="transitionDuration">Duration of the transition to snapshot volumes.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the snapshot was successfully restored.</returns>
    Task<bool> RestoreVolumeSnapshotAsync(string snapshotId, TimeSpan? transitionDuration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies dynamic range compression to an audio category.
    /// </summary>
    /// <param name="category">Audio category to apply compression to.</param>
    /// <param name="ratio">Compression ratio (1.0 = no compression, higher values = more compression).</param>
    /// <param name="threshold">Volume threshold above which compression is applied (0.0 to 1.0).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async operation.</returns>
    Task SetCompressionAsync(AudioCategory category, float ratio, float threshold, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents different environmental audio presets for spatial audio.
/// </summary>
public enum AudioEnvironment
{
    /// <summary>
    /// Default environment with no special effects.
    /// </summary>
    Default = 0,
    
    /// <summary>
    /// Small room with short reverb.
    /// </summary>
    SmallRoom = 1,
    
    /// <summary>
    /// Large hall with long reverb.
    /// </summary>
    Hall = 2,
    
    /// <summary>
    /// Outdoor environment with minimal reverb.
    /// </summary>
    Outdoor = 3,
    
    /// <summary>
    /// Underground cave with strong echo.
    /// </summary>
    Cave = 4,
    
    /// <summary>
    /// Underwater environment with muffled audio.
    /// </summary>
    Underwater = 5,
    
    /// <summary>
    /// Forest environment with natural reverb.
    /// </summary>
    Forest = 6,
    
    /// <summary>
    /// Urban environment with reflections from buildings.
    /// </summary>
    Urban = 7
}