using GameConsole.Core.Abstractions;

namespace GameConsole.Audio.Services;

/// <summary>
/// Core audio service for game audio playbook and management.
/// Supports basic audio operations with optional advanced capabilities.
/// </summary>
public interface IService : GameConsole.Core.Abstractions.IService
{
    // Core audio functionality (required)
    /// <summary>
    /// Plays an audio file asynchronously.
    /// </summary>
    /// <param name="path">Path to the audio file to play.</param>
    /// <param name="category">Audio category for volume control (e.g., "SFX", "Music", "Voice"). Default is "SFX".</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the audio started playing successfully, false otherwise.</returns>
    Task<bool> PlayAsync(string path, string category = "SFX", CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops playing a specific audio file.
    /// </summary>
    /// <param name="path">Path to the audio file to stop.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async stop operation.</returns>
    Task StopAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops all currently playing audio.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async stop operation.</returns>
    Task StopAllAsync(CancellationToken cancellationToken = default);

    // Volume management
    /// <summary>
    /// Sets the master volume level for all audio.
    /// </summary>
    /// <param name="volume">Volume level from 0.0 (muted) to 1.0 (full volume).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async volume setting operation.</returns>
    Task SetMasterVolumeAsync(float volume, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the volume level for a specific audio category.
    /// </summary>
    /// <param name="category">Audio category to set volume for (e.g., "SFX", "Music", "Voice").</param>
    /// <param name="volume">Volume level from 0.0 (muted) to 1.0 (full volume).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async volume setting operation.</returns>
    Task SetCategoryVolumeAsync(string category, float volume, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current volume level for a specific audio category.
    /// </summary>
    /// <param name="category">Audio category to get volume for (e.g., "SFX", "Music", "Voice").</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Current volume level from 0.0 (muted) to 1.0 (full volume).</returns>
    Task<float> GetCategoryVolumeAsync(string category, CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for spatial (3D) audio features.
/// Allows services to provide 3D audio positioning and environmental effects.
/// </summary>
public interface ISpatialAudioCapability : ICapabilityProvider
{
    /// <summary>
    /// Sets the position of the audio listener in 3D space.
    /// </summary>
    /// <param name="position">3D position of the listener.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async listener positioning operation.</returns>
    Task SetListenerPositionAsync(Vector3 position, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the orientation of the audio listener in 3D space.
    /// </summary>
    /// <param name="forward">Forward direction vector of the listener.</param>
    /// <param name="up">Up direction vector of the listener.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async listener orientation operation.</returns>
    Task SetListenerOrientationAsync(Vector3 forward, Vector3 up, CancellationToken cancellationToken = default);

    /// <summary>
    /// Plays audio at a specific position in 3D space.
    /// </summary>
    /// <param name="path">Path to the audio file to play.</param>
    /// <param name="position">3D position where the audio should originate.</param>
    /// <param name="category">Audio category for volume control. Default is "SFX".</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the audio started playing successfully, false otherwise.</returns>
    Task<bool> PlayAtPositionAsync(string path, Vector3 position, string category = "SFX", CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for advanced audio effects and processing.
/// </summary>
public interface IAudioEffectsCapability : ICapabilityProvider
{
    /// <summary>
    /// Applies a low-pass filter to audio in a specific category.
    /// </summary>
    /// <param name="category">Audio category to apply the filter to.</param>
    /// <param name="cutoffFrequency">Cutoff frequency in Hz for the low-pass filter.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async filter application operation.</returns>
    Task ApplyLowPassFilterAsync(string category, float cutoffFrequency, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a reverb effect to audio in a specific category.
    /// </summary>
    /// <param name="category">Audio category to apply reverb to.</param>
    /// <param name="roomSize">Room size factor (0.0 to 1.0) for reverb calculation.</param>
    /// <param name="dampening">Dampening factor (0.0 to 1.0) for reverb decay.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async reverb application operation.</returns>
    Task ApplyReverbAsync(string category, float roomSize, float dampening, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all audio effects from a specific category.
    /// </summary>
    /// <param name="category">Audio category to clear effects from.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async effects clearing operation.</returns>
    Task ClearEffectsAsync(string category, CancellationToken cancellationToken = default);
}

/// <summary>
/// Simple 3D vector structure for spatial audio positioning.
/// </summary>
public readonly struct Vector3
{
    /// <summary>
    /// Gets the X coordinate.
    /// </summary>
    public float X { get; }

    /// <summary>
    /// Gets the Y coordinate.
    /// </summary>
    public float Y { get; }

    /// <summary>
    /// Gets the Z coordinate.
    /// </summary>
    public float Z { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Vector3"/> struct.
    /// </summary>
    /// <param name="x">X coordinate.</param>
    /// <param name="y">Y coordinate.</param>
    /// <param name="z">Z coordinate.</param>
    public Vector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>
    /// Gets a zero vector (0, 0, 0).
    /// </summary>
    public static Vector3 Zero => new(0f, 0f, 0f);

    /// <summary>
    /// Gets a forward vector (0, 0, 1).
    /// </summary>
    public static Vector3 Forward => new(0f, 0f, 1f);

    /// <summary>
    /// Gets an up vector (0, 1, 0).
    /// </summary>
    public static Vector3 Up => new(0f, 1f, 0f);

    /// <summary>
    /// Returns a string representation of the vector.
    /// </summary>
    /// <returns>String representation in format "(x, y, z)".</returns>
    public override string ToString() => $"({X}, {Y}, {Z})";
}