namespace GameConsole.Audio.Services;

/// <summary>
/// Represents an audio stream with position tracking and seek capabilities.
/// Provides control over audio playback state and position.
/// </summary>
public interface IAudioStream : IAsyncDisposable
{
    /// <summary>
    /// Gets the unique identifier for this audio stream.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the file path or identifier of the audio source.
    /// </summary>
    string Source { get; }

    /// <summary>
    /// Gets the audio format of the stream.
    /// </summary>
    AudioFormat Format { get; }

    /// <summary>
    /// Gets the total duration of the audio stream.
    /// </summary>
    TimeSpan Duration { get; }

    /// <summary>
    /// Gets the current playback position in the audio stream.
    /// </summary>
    TimeSpan Position { get; }

    /// <summary>
    /// Gets the current volume level of the stream (0.0 to 1.0).
    /// </summary>
    float Volume { get; }

    /// <summary>
    /// Gets a value indicating whether the stream is currently playing.
    /// </summary>
    bool IsPlaying { get; }

    /// <summary>
    /// Gets a value indicating whether the stream is currently paused.
    /// </summary>
    bool IsPaused { get; }

    /// <summary>
    /// Gets a value indicating whether the stream supports seeking operations.
    /// </summary>
    bool CanSeek { get; }

    /// <summary>
    /// Starts or resumes playback of the audio stream.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task PlayAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses playback of the audio stream.
    /// The current position is preserved and playback can be resumed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task PauseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops playback of the audio stream.
    /// The position is reset to the beginning.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Seeks to a specific position in the audio stream.
    /// Only available if <see cref="CanSeek"/> is true.
    /// </summary>
    /// <param name="position">The position to seek to.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <exception cref="NotSupportedException">Thrown when seeking is not supported.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when position is invalid.</exception>
    Task SeekAsync(TimeSpan position, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the volume level of the stream.
    /// </summary>
    /// <param name="volume">The volume level (0.0 to 1.0).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when volume is outside the valid range.</exception>
    Task SetVolumeAsync(float volume, CancellationToken cancellationToken = default);

    /// <summary>
    /// Occurs when the stream position changes during playback.
    /// </summary>
    event EventHandler<AudioStreamPositionChangedEventArgs>? PositionChanged;

    /// <summary>
    /// Occurs when the stream playback state changes.
    /// </summary>
    event EventHandler<AudioStreamStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Occurs when the stream reaches the end of playback.
    /// </summary>
    event EventHandler<EventArgs>? PlaybackCompleted;
}

/// <summary>
/// Event arguments for audio stream position change events.
/// </summary>
public class AudioStreamPositionChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AudioStreamPositionChangedEventArgs"/> class.
    /// </summary>
    /// <param name="position">The current position in the stream.</param>
    /// <param name="duration">The total duration of the stream.</param>
    public AudioStreamPositionChangedEventArgs(TimeSpan position, TimeSpan duration)
    {
        Position = position;
        Duration = duration;
    }

    /// <summary>
    /// Gets the current position in the stream.
    /// </summary>
    public TimeSpan Position { get; }

    /// <summary>
    /// Gets the total duration of the stream.
    /// </summary>
    public TimeSpan Duration { get; }
}

/// <summary>
/// Event arguments for audio stream state change events.
/// </summary>
public class AudioStreamStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AudioStreamStateChangedEventArgs"/> class.
    /// </summary>
    /// <param name="previousState">The previous state of the stream.</param>
    /// <param name="currentState">The current state of the stream.</param>
    public AudioStreamStateChangedEventArgs(AudioStreamState previousState, AudioStreamState currentState)
    {
        PreviousState = previousState;
        CurrentState = currentState;
    }

    /// <summary>
    /// Gets the previous state of the stream.
    /// </summary>
    public AudioStreamState PreviousState { get; }

    /// <summary>
    /// Gets the current state of the stream.
    /// </summary>
    public AudioStreamState CurrentState { get; }
}

/// <summary>
/// Represents the current state of an audio stream.
/// </summary>
public enum AudioStreamState
{
    /// <summary>
    /// The stream is stopped and at the beginning.
    /// </summary>
    Stopped,

    /// <summary>
    /// The stream is currently playing.
    /// </summary>
    Playing,

    /// <summary>
    /// The stream is paused and can be resumed.
    /// </summary>
    Paused,

    /// <summary>
    /// The stream has reached the end of playback.
    /// </summary>
    Completed,

    /// <summary>
    /// The stream encountered an error.
    /// </summary>
    Error
}