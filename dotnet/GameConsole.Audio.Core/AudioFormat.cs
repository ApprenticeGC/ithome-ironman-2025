namespace GameConsole.Audio.Services;

/// <summary>
/// Represents supported audio formats for cross-platform audio playback.
/// This enumeration is extensible to support additional formats as needed.
/// </summary>
public enum AudioFormat
{
    /// <summary>
    /// Unknown or unsupported audio format.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// WAV (Waveform Audio File Format) - Uncompressed audio format.
    /// Provides high quality audio with larger file sizes.
    /// </summary>
    Wav = 1,

    /// <summary>
    /// MP3 (MPEG Audio Layer III) - Compressed audio format.
    /// Provides good quality audio with smaller file sizes.
    /// </summary>
    Mp3 = 2,

    /// <summary>
    /// OGG Vorbis - Open source compressed audio format.
    /// Provides good quality audio with patent-free compression.
    /// </summary>
    Ogg = 3
}