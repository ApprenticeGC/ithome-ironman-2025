namespace GameConsole.Audio.Core;

/// <summary>
/// Represents 3D position for spatial audio.
/// </summary>
public struct Vector3
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public Vector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static Vector3 Zero => new(0, 0, 0);
}

/// <summary>
/// Audio format enumeration for supported audio file types.
/// </summary>
public enum AudioFormat
{
    WAV,
    MP3,
    OGG,
    FLAC
}

/// <summary>
/// Audio channel enumeration for mixer operations.
/// </summary>
public enum AudioChannel
{
    Master,
    Music,
    SFX,
    Voice,
    Ambient
}

/// <summary>
/// Audio device information.
/// </summary>
public class AudioDeviceInfo
{
    public required string DeviceId { get; init; }
    public required string Name { get; init; }
    public bool IsDefault { get; init; }
    public int Channels { get; init; }
    public int SampleRate { get; init; }
}

/// <summary>
/// Audio playback state enumeration.
/// </summary>
public enum PlaybackState
{
    Stopped,
    Playing,
    Paused,
    Loading
}

/// <summary>
/// Audio stream information for file streaming.
/// </summary>
public class AudioStreamInfo
{
    public required string Path { get; init; }
    public AudioFormat Format { get; init; }
    public TimeSpan Duration { get; init; }
    public long FileSizeBytes { get; init; }
    public bool SupportsStreaming { get; init; }
}