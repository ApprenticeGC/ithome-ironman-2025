using System.Numerics;

namespace GameConsole.Audio.Core;

/// <summary>
/// Represents different categories of audio content.
/// </summary>
public enum AudioCategory
{
    /// <summary>
    /// Sound effects and UI audio.
    /// </summary>
    SFX = 0,
    
    /// <summary>
    /// Background music and soundtrack.
    /// </summary>
    Music = 1,
    
    /// <summary>
    /// Voice acting and dialogue.
    /// </summary>
    Voice = 2,
    
    /// <summary>
    /// Ambient sounds and environmental audio.
    /// </summary>
    Ambient = 3,
    
    /// <summary>
    /// User interface sounds.
    /// </summary>
    UI = 4
}

/// <summary>
/// Represents the current state of an audio source.
/// </summary>
public enum AudioState
{
    /// <summary>
    /// Audio is stopped.
    /// </summary>
    Stopped = 0,
    
    /// <summary>
    /// Audio is currently playing.
    /// </summary>
    Playing = 1,
    
    /// <summary>
    /// Audio is paused.
    /// </summary>
    Paused = 2,
    
    /// <summary>
    /// Audio is loading.
    /// </summary>
    Loading = 3,
    
    /// <summary>
    /// Audio failed to load or play.
    /// </summary>
    Error = 4
}

/// <summary>
/// Represents the format of an audio file.
/// </summary>
public enum AudioFormat
{
    /// <summary>
    /// Unknown or unsupported format.
    /// </summary>
    Unknown = 0,
    
    /// <summary>
    /// WAV format (uncompressed).
    /// </summary>
    WAV = 1,
    
    /// <summary>
    /// MP3 format (compressed).
    /// </summary>
    MP3 = 2,
    
    /// <summary>
    /// OGG Vorbis format (compressed).
    /// </summary>
    OGG = 3,
    
    /// <summary>
    /// FLAC format (lossless compressed).
    /// </summary>
    FLAC = 4
}

/// <summary>
/// Represents priority levels for audio playback when resources are limited.
/// </summary>
public enum AudioPriority
{
    /// <summary>
    /// Low priority audio that can be dropped if needed.
    /// </summary>
    Low = 0,
    
    /// <summary>
    /// Normal priority audio.
    /// </summary>
    Normal = 1,
    
    /// <summary>
    /// High priority audio that should rarely be interrupted.
    /// </summary>
    High = 2,
    
    /// <summary>
    /// Critical audio that should never be interrupted (e.g., important dialogue).
    /// </summary>
    Critical = 3
}

/// <summary>
/// Contains information about spatial audio positioning.
/// </summary>
public readonly record struct AudioPosition
{
    /// <summary>
    /// 3D position of the audio source.
    /// </summary>
    public Vector3 Position { get; init; }
    
    /// <summary>
    /// Direction the audio source is facing (for directional sources).
    /// </summary>
    public Vector3 Direction { get; init; }
    
    /// <summary>
    /// Velocity of the audio source (for Doppler effect).
    /// </summary>
    public Vector3 Velocity { get; init; }
    
    /// <summary>
    /// Whether this is a 2D or 3D positioned audio source.
    /// </summary>
    public bool Is3D { get; init; }
    
    /// <summary>
    /// Creates a 2D audio position (no spatial positioning).
    /// </summary>
    public static AudioPosition TwoD => new() { Is3D = false };
    
    /// <summary>
    /// Creates a 3D audio position with the specified coordinates.
    /// </summary>
    /// <param name="position">3D position.</param>
    /// <param name="direction">Optional direction vector.</param>
    /// <param name="velocity">Optional velocity vector.</param>
    public static AudioPosition ThreeD(Vector3 position, Vector3? direction = null, Vector3? velocity = null) => new()
    {
        Position = position,
        Direction = direction ?? Vector3.UnitZ,
        Velocity = velocity ?? Vector3.Zero,
        Is3D = true
    };
}