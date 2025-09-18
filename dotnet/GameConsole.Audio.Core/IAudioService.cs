using GameConsole.Core.Abstractions;
using System.Numerics;

namespace GameConsole.Audio.Services;

/// <summary>
/// Main interface for audio playback and management.
/// Supports basic audio operations with optional advanced capabilities.
/// </summary>
public interface IAudioService : IService
{
    // Core audio functionality (required)
    Task<bool> PlayAsync(string path, string category = "SFX", CancellationToken ct = default);
    Task StopAsync(string path, CancellationToken ct = default);
    Task StopAllAsync(CancellationToken ct = default);

    // Volume management
    Task SetMasterVolumeAsync(float volume, CancellationToken ct = default);
    Task SetCategoryVolumeAsync(string category, float volume, CancellationToken ct = default);
    Task<float> GetCategoryVolumeAsync(string category, CancellationToken ct = default);
}

/// <summary>
/// Optional capability for spatial audio positioning.
/// </summary>
public interface ISpatialAudioCapability : ICapabilityProvider
{
    Task SetListenerPositionAsync(Vector3 position, CancellationToken ct = default);
    Task SetListenerOrientationAsync(Vector3 forward, Vector3 up, CancellationToken ct = default);
    Task Play3DAudioAsync(string path, Vector3 position, float volume, CancellationToken ct = default);
}

/// <summary>
/// Optional capability for advanced audio device management.
/// </summary>
public interface IAudioDeviceCapability : ICapabilityProvider
{
    Task<IEnumerable<AudioDevice>> GetAvailableDevicesAsync(CancellationToken ct = default);
    Task<AudioDevice?> GetCurrentDeviceAsync(CancellationToken ct = default);
    Task SetCurrentDeviceAsync(AudioDevice device, CancellationToken ct = default);
}

/// <summary>
/// Represents an audio output device.
/// </summary>
public sealed record AudioDevice(
    string Id,
    string Name,
    bool IsDefault,
    AudioDeviceType Type
);

/// <summary>
/// Types of audio output devices.
/// </summary>
public enum AudioDeviceType
{
    Unknown,
    Speakers,
    Headphones,
    HDMI,
    USB,
    Bluetooth
}