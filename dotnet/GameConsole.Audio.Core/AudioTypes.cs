namespace GameConsole.Audio.Services;

/// <summary>
/// Represents the format and metadata of an audio file.
/// </summary>
public sealed record AudioFormat(
    string Extension,
    string MimeType,
    int SampleRate,
    int Channels,
    int BitsPerSample
)
{
    public static readonly AudioFormat WAV = new("wav", "audio/wav", 44100, 2, 16);
    public static readonly AudioFormat MP3 = new("mp3", "audio/mpeg", 44100, 2, 16);
    public static readonly AudioFormat OGG = new("ogg", "audio/ogg", 44100, 2, 16);
}

/// <summary>
/// Represents audio playback state information.
/// </summary>
public sealed record AudioPlaybackInfo(
    string Path,
    string Category,
    TimeSpan Duration,
    TimeSpan Position,
    float Volume,
    bool IsPlaying,
    bool IsPaused
);

/// <summary>
/// Represents audio streaming capabilities.
/// </summary>
public enum AudioStreamingMode
{
    LoadFully,
    StreamFromDisk,
    StreamFromNetwork
}

/// <summary>
/// Configuration for audio playback.
/// </summary>
public sealed record AudioPlaybackOptions(
    float Volume = 1.0f,
    bool Loop = false,
    AudioStreamingMode StreamingMode = AudioStreamingMode.LoadFully,
    TimeSpan? StartPosition = null
);