using GameConsole.Core.Abstractions;

namespace GameConsole.Audio.Services;

/// <summary>
/// Defines the core audio service interface for the GameConsole 4-tier architecture.
/// Provides comprehensive audio playback and management capabilities with cross-platform abstraction.
/// Supports basic audio operations with optional advanced capabilities through the capability provider pattern.
/// </summary>
/// <example>
/// Basic audio playback usage:
/// <code>
/// // Play a sound effect
/// await audioService.PlayAsync("assets/sounds/explosion.wav", "SFX");
/// 
/// // Control volume
/// await audioService.SetMasterVolumeAsync(0.8f);
/// await audioService.SetCategoryVolumeAsync("Music", 0.6f);
/// 
/// // Stop specific audio
/// await audioService.StopAsync("assets/sounds/explosion.wav");
/// </code>
/// 
/// Advanced stream management:
/// <code>
/// // Create and control audio streams
/// var stream = await audioService.CreateStreamAsync("assets/music/bgm.mp3");
/// await stream.PlayAsync();
/// await stream.SeekAsync(TimeSpan.FromSeconds(30));
/// await stream.SetVolumeAsync(0.5f);
/// </code>
/// 
/// Device enumeration:
/// <code>
/// // Get available audio devices
/// var enumerator = await audioService.GetCapabilityAsync&lt;IAudioDeviceEnumerator&gt;();
/// if (enumerator != null)
/// {
///     var devices = await enumerator.GetAvailableDevicesAsync();
///     var defaultDevice = await enumerator.GetDefaultDeviceAsync();
/// }
/// </code>
/// </example>
public interface IAudioService : IService
{
    #region Core Audio Functionality

    /// <summary>
    /// Plays an audio file asynchronously.
    /// </summary>
    /// <param name="path">The file path or identifier of the audio to play.</param>
    /// <param name="category">The audio category for volume control (default: "SFX").</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns true if playback started successfully.</returns>
    /// <exception cref="ArgumentNullException">Thrown when path is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the audio file is not found.</exception>
    /// <exception cref="NotSupportedException">Thrown when the audio format is not supported.</exception>
    Task<bool> PlayAsync(string path, string category = "SFX", CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops playback of a specific audio file.
    /// </summary>
    /// <param name="path">The file path or identifier of the audio to stop.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task StopAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses playback of a specific audio file.
    /// The audio can be resumed from the current position.
    /// </summary>
    /// <param name="path">The file path or identifier of the audio to pause.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task PauseAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes playback of a previously paused audio file.
    /// </summary>
    /// <param name="path">The file path or identifier of the audio to resume.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ResumeAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops all currently playing audio.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task StopAllAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Volume Control

    /// <summary>
    /// Sets the master volume level for all audio output.
    /// </summary>
    /// <param name="volume">The master volume level (0.0 to 1.0).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when volume is outside the valid range.</exception>
    Task SetMasterVolumeAsync(float volume, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current master volume level.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the master volume level (0.0 to 1.0).</returns>
    Task<float> GetMasterVolumeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the volume level for a specific audio category.
    /// </summary>
    /// <param name="category">The audio category name.</param>
    /// <param name="volume">The volume level for the category (0.0 to 1.0).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when category is null or empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when volume is outside the valid range.</exception>
    Task SetCategoryVolumeAsync(string category, float volume, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the volume level for a specific audio category.
    /// </summary>
    /// <param name="category">The audio category name.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the volume level for the category (0.0 to 1.0).</returns>
    /// <exception cref="ArgumentNullException">Thrown when category is null or empty.</exception>
    Task<float> GetCategoryVolumeAsync(string category, CancellationToken cancellationToken = default);

    #endregion

    #region Audio Stream Management

    /// <summary>
    /// Creates an audio stream for advanced playback control.
    /// Provides position tracking, seeking, and individual volume control.
    /// </summary>
    /// <param name="path">The file path or identifier of the audio source.</param>
    /// <param name="category">The audio category for volume control (default: "SFX").</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns an audio stream instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when path is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the audio file is not found.</exception>
    /// <exception cref="NotSupportedException">Thrown when the audio format is not supported.</exception>
    Task<IAudioStream> CreateStreamAsync(string path, string category = "SFX", CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all currently active audio streams.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns a collection of active audio streams.</returns>
    Task<IEnumerable<IAudioStream>> GetActiveStreamsAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Audio Format Support

    /// <summary>
    /// Gets all supported audio formats.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns a collection of supported audio formats.</returns>
    Task<IEnumerable<AudioFormat>> GetSupportedFormatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific audio format is supported.
    /// </summary>
    /// <param name="format">The audio format to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns true if the format is supported.</returns>
    Task<bool> IsFormatSupportedAsync(AudioFormat format, CancellationToken cancellationToken = default);

    #endregion

    #region Device Management

    /// <summary>
    /// Sets the active audio output device.
    /// </summary>
    /// <param name="deviceId">The unique identifier of the audio device to use.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when deviceId is null or empty.</exception>
    /// <exception cref="ArgumentException">Thrown when the device is not found or not available.</exception>
    Task SetActiveDeviceAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the currently active audio output device.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the active audio device, or null if none is set.</returns>
    Task<IAudioDevice?> GetActiveDeviceAsync(CancellationToken cancellationToken = default);

    #endregion
}