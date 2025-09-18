namespace GameConsole.Audio.Services;

/// <summary>
/// Represents an audio output device in the system.
/// Supports hot-plugging and device enumeration.
/// </summary>
public interface IAudioDevice
{
    /// <summary>
    /// Gets the unique identifier of the audio device.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the display name of the audio device.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a value indicating whether the device is currently available and can be used for audio output.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Gets a value indicating whether this is the default audio device for the system.
    /// </summary>
    bool IsDefault { get; }

    /// <summary>
    /// Gets the supported audio formats for this device.
    /// </summary>
    IEnumerable<AudioFormat> SupportedFormats { get; }
}

/// <summary>
/// Provides enumeration and monitoring of audio output devices.
/// Supports hot-plugging detection for dynamic device management.
/// </summary>
public interface IAudioDeviceEnumerator
{
    /// <summary>
    /// Gets all available audio output devices.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns a collection of available audio devices.</returns>
    Task<IEnumerable<IAudioDevice>> GetAvailableDevicesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default audio output device.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the default audio device, or null if none available.</returns>
    Task<IAudioDevice?> GetDefaultDeviceAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an audio device by its unique identifier.
    /// </summary>
    /// <param name="deviceId">The unique identifier of the device.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the device, or null if not found.</returns>
    Task<IAudioDevice?> GetDeviceByIdAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Occurs when audio devices are added or removed from the system (hot-plugging).
    /// </summary>
    event EventHandler<AudioDeviceChangedEventArgs>? DeviceChanged;
}

/// <summary>
/// Event arguments for audio device change events.
/// </summary>
public class AudioDeviceChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AudioDeviceChangedEventArgs"/> class.
    /// </summary>
    /// <param name="device">The audio device that changed.</param>
    /// <param name="changeType">The type of change that occurred.</param>
    public AudioDeviceChangedEventArgs(IAudioDevice device, AudioDeviceChangeType changeType)
    {
        Device = device;
        ChangeType = changeType;
    }

    /// <summary>
    /// Gets the audio device that changed.
    /// </summary>
    public IAudioDevice Device { get; }

    /// <summary>
    /// Gets the type of change that occurred.
    /// </summary>
    public AudioDeviceChangeType ChangeType { get; }
}

/// <summary>
/// Specifies the type of audio device change.
/// </summary>
public enum AudioDeviceChangeType
{
    /// <summary>
    /// An audio device was added to the system.
    /// </summary>
    Added,

    /// <summary>
    /// An audio device was removed from the system.
    /// </summary>
    Removed,

    /// <summary>
    /// The default audio device changed.
    /// </summary>
    DefaultChanged,

    /// <summary>
    /// An audio device's properties changed (e.g., availability).
    /// </summary>
    PropertiesChanged
}