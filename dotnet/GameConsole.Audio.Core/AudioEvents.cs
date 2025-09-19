using System.Numerics;

namespace GameConsole.Audio.Core;

/// <summary>
/// Base interface for all audio events in the system.
/// </summary>
public interface IAudioEvent
{
    /// <summary>
    /// Unique identifier for the audio source.
    /// </summary>
    string SourceId { get; }
    
    /// <summary>
    /// Timestamp when the event occurred.
    /// </summary>
    DateTime Timestamp { get; }
    
    /// <summary>
    /// Audio category this event belongs to.
    /// </summary>
    AudioCategory Category { get; }
}

/// <summary>
/// Event fired when audio playback starts.
/// </summary>
public readonly record struct AudioStartedEvent : IAudioEvent
{
    public string SourceId { get; init; }
    public DateTime Timestamp { get; init; }
    public AudioCategory Category { get; init; }
    
    /// <summary>
    /// Path or identifier of the audio file being played.
    /// </summary>
    public string AudioPath { get; init; }
    
    /// <summary>
    /// Volume level at which the audio started.
    /// </summary>
    public float Volume { get; init; }
    
    /// <summary>
    /// Spatial position of the audio source.
    /// </summary>
    public AudioPosition? Position { get; init; }
}

/// <summary>
/// Event fired when audio playback stops.
/// </summary>
public readonly record struct AudioStoppedEvent : IAudioEvent
{
    public string SourceId { get; init; }
    public DateTime Timestamp { get; init; }
    public AudioCategory Category { get; init; }
    
    /// <summary>
    /// Reason why the audio stopped.
    /// </summary>
    public AudioStopReason Reason { get; init; }
    
    /// <summary>
    /// Whether the audio completed naturally or was interrupted.
    /// </summary>
    public bool WasCompleted { get; init; }
}

/// <summary>
/// Event fired when audio volume changes.
/// </summary>
public readonly record struct AudioVolumeChangedEvent : IAudioEvent
{
    public string SourceId { get; init; }
    public DateTime Timestamp { get; init; }
    public AudioCategory Category { get; init; }
    
    /// <summary>
    /// Previous volume level (0.0 to 1.0).
    /// </summary>
    public float PreviousVolume { get; init; }
    
    /// <summary>
    /// New volume level (0.0 to 1.0).
    /// </summary>
    public float NewVolume { get; init; }
    
    /// <summary>
    /// Whether this was a master volume change or category-specific.
    /// </summary>
    public bool IsMasterVolume { get; init; }
}

/// <summary>
/// Event fired when audio position changes (for spatial audio).
/// </summary>
public readonly record struct AudioPositionChangedEvent : IAudioEvent
{
    public string SourceId { get; init; }
    public DateTime Timestamp { get; init; }
    public AudioCategory Category { get; init; }
    
    /// <summary>
    /// Previous audio position.
    /// </summary>
    public AudioPosition PreviousPosition { get; init; }
    
    /// <summary>
    /// New audio position.
    /// </summary>
    public AudioPosition NewPosition { get; init; }
}

/// <summary>
/// Event fired when audio state changes (playing, paused, etc.).
/// </summary>
public readonly record struct AudioStateChangedEvent : IAudioEvent
{
    public string SourceId { get; init; }
    public DateTime Timestamp { get; init; }
    public AudioCategory Category { get; init; }
    
    /// <summary>
    /// Previous audio state.
    /// </summary>
    public AudioState PreviousState { get; init; }
    
    /// <summary>
    /// New audio state.
    /// </summary>
    public AudioState NewState { get; init; }
    
    /// <summary>
    /// Error message if state changed to Error.
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Represents the reason why audio playback stopped.
/// </summary>
public enum AudioStopReason
{
    /// <summary>
    /// Audio completed naturally.
    /// </summary>
    Completed = 0,
    
    /// <summary>
    /// Audio was manually stopped.
    /// </summary>
    Manual = 1,
    
    /// <summary>
    /// Audio was interrupted by higher priority audio.
    /// </summary>
    Interrupted = 2,
    
    /// <summary>
    /// Audio stopped due to an error.
    /// </summary>
    Error = 3,
    
    /// <summary>
    /// Audio was stopped due to resource limitations.
    /// </summary>
    ResourceLimit = 4
}

/// <summary>
/// Contains metadata about an audio file or resource.
/// </summary>
public readonly record struct AudioMetadata
{
    /// <summary>
    /// Duration of the audio in seconds.
    /// </summary>
    public TimeSpan Duration { get; init; }
    
    /// <summary>
    /// Audio format of the file.
    /// </summary>
    public AudioFormat Format { get; init; }
    
    /// <summary>
    /// Sample rate in Hz.
    /// </summary>
    public int SampleRate { get; init; }
    
    /// <summary>
    /// Number of audio channels (1 = mono, 2 = stereo, etc.).
    /// </summary>
    public int Channels { get; init; }
    
    /// <summary>
    /// Bit depth of the audio.
    /// </summary>
    public int BitDepth { get; init; }
    
    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSize { get; init; }
    
    /// <summary>
    /// Whether the audio supports looping.
    /// </summary>
    public bool IsLoopable { get; init; }
    
    /// <summary>
    /// Default volume level for this audio (0.0 to 1.0).
    /// </summary>
    public float DefaultVolume { get; init; }
    
    /// <summary>
    /// Recommended category for this audio.
    /// </summary>
    public AudioCategory RecommendedCategory { get; init; }
}

/// <summary>
/// Represents configuration for audio playback.
/// </summary>
public readonly record struct AudioPlaybackConfig
{
    /// <summary>
    /// Volume level (0.0 to 1.0).
    /// </summary>
    public float Volume { get; init; }
    
    /// <summary>
    /// Whether the audio should loop.
    /// </summary>
    public bool Loop { get; init; }
    
    /// <summary>
    /// Playback priority.
    /// </summary>
    public AudioPriority Priority { get; init; }
    
    /// <summary>
    /// Spatial position for the audio.
    /// </summary>
    public AudioPosition? Position { get; init; }
    
    /// <summary>
    /// Delay before starting playback.
    /// </summary>
    public TimeSpan StartDelay { get; init; }
    
    /// <summary>
    /// Duration after which to automatically stop the audio (null = play to end).
    /// </summary>
    public TimeSpan? AutoStopAfter { get; init; }
    
    /// <summary>
    /// Fade-in duration when starting playback.
    /// </summary>
    public TimeSpan FadeIn { get; init; }
    
    /// <summary>
    /// Fade-out duration when stopping playback.
    /// </summary>
    public TimeSpan FadeOut { get; init; }

    /// <summary>
    /// Initializes a new instance of the AudioPlaybackConfig struct.
    /// </summary>
    public AudioPlaybackConfig()
    {
        Volume = 1.0f;
        Loop = false;
        Priority = AudioPriority.Normal;
        Position = null;
        StartDelay = TimeSpan.Zero;
        AutoStopAfter = null;
        FadeIn = TimeSpan.Zero;
        FadeOut = TimeSpan.Zero;
    }
    
    /// <summary>
    /// Creates a default configuration.
    /// </summary>
    public static AudioPlaybackConfig Default => new();
    
    /// <summary>
    /// Creates a configuration for background music.
    /// </summary>
    public static AudioPlaybackConfig BackgroundMusic(float volume = 0.7f) => new()
    {
        Volume = volume,
        Loop = true,
        Priority = AudioPriority.Low,
        FadeIn = TimeSpan.FromSeconds(1),
        FadeOut = TimeSpan.FromSeconds(2)
    };
    
    /// <summary>
    /// Creates a configuration for sound effects.
    /// </summary>
    public static AudioPlaybackConfig SoundEffect(float volume = 1.0f, AudioPriority priority = AudioPriority.Normal) => new()
    {
        Volume = volume,
        Loop = false,
        Priority = priority
    };
}