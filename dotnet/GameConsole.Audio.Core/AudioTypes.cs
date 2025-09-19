using System.Numerics;

namespace GameConsole.Audio.Services;

/// <summary>
/// Represents an audio device (speakers, headphones, etc.).
/// </summary>
public sealed record AudioDevice(
    string Id,
    string Name,
    string Description,
    bool IsDefault,
    AudioDeviceType Type,
    int MaxChannels
);

/// <summary>
/// Types of audio devices.
/// </summary>
public enum AudioDeviceType
{
    /// <summary>Default system device.</summary>
    Default,
    /// <summary>Built-in speakers.</summary>
    Speakers,
    /// <summary>Headphones or headset.</summary>
    Headphones,
    /// <summary>USB audio device.</summary>
    USB,
    /// <summary>Bluetooth audio device.</summary>
    Bluetooth,
    /// <summary>HDMI audio output.</summary>
    HDMI,
    /// <summary>Unknown or other device type.</summary>
    Other
}

/// <summary>
/// Audio effects that can be applied to channels.
/// </summary>
[Flags]
public enum AudioEffects
{
    /// <summary>No effects applied.</summary>
    None = 0,
    /// <summary>Reverb effect.</summary>
    Reverb = 1 << 0,
    /// <summary>Echo effect.</summary>
    Echo = 1 << 1,
    /// <summary>Low-pass filter.</summary>
    LowPass = 1 << 2,
    /// <summary>High-pass filter.</summary>
    HighPass = 1 << 3,
    /// <summary>Distortion effect.</summary>
    Distortion = 1 << 4,
    /// <summary>Chorus effect.</summary>
    Chorus = 1 << 5,
    /// <summary>Compression effect.</summary>
    Compression = 1 << 6
}

/// <summary>
/// Supported audio file formats.
/// </summary>
public enum AudioFormat
{
    /// <summary>WAV format (uncompressed).</summary>
    WAV,
    /// <summary>MP3 format (compressed).</summary>
    MP3,
    /// <summary>OGG Vorbis format (compressed).</summary>
    OGG,
    /// <summary>FLAC format (lossless compressed).</summary>
    FLAC,
    /// <summary>AAC format (compressed).</summary>
    AAC,
    /// <summary>Auto-detect format from file extension.</summary>
    Auto
}

/// <summary>
/// Audio playback state information.
/// </summary>
public sealed record AudioPlaybackInfo(
    string FilePath,
    string Category,
    AudioFormat Format,
    TimeSpan Duration,
    TimeSpan Position,
    bool IsPlaying,
    bool IsPaused,
    bool IsLooping,
    float Volume
);

/// <summary>
/// 3D audio source information.
/// </summary>
public sealed record Audio3DSource(
    string FilePath,
    Vector3 Position,
    Vector3 Velocity,
    float Volume,
    float MinDistance,
    float MaxDistance,
    bool IsPlaying
);

/// <summary>
/// Audio channel configuration.
/// </summary>
public sealed record AudioChannel(
    string Name,
    float Volume,
    bool IsMuted,
    AudioEffects Effects,
    List<string> AssignedAudioSources
);

/// <summary>
/// Audio streaming configuration for large files.
/// </summary>
public sealed record AudioStreamConfig(
    int BufferSize,
    int BufferCount,
    bool PreloadNext,
    TimeSpan SeekThreshold
);