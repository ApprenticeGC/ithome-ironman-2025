using GameConsole.Core.Abstractions;
using GameConsole.Input.Core;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.Input.Services;

/// <summary>
/// Input recording service for macro support and input sequence recording/playback.
/// Enables recording input sequences and playing them back for automation.
/// </summary>
[Service("Input Recording", "1.0.0", "Records and plays back input sequences for macro support and automation", 
    Categories = new[] { "Input", "Recording", "Macro", "Automation" }, Lifetime = ServiceLifetime.Singleton)]
public class InputRecordingService : BaseInputService, IInputRecordingCapability
{
    private readonly ConcurrentDictionary<string, RecordingSession> _activeSessions;
    private readonly ConcurrentDictionary<string, InputSequence> _savedSequences;
    private readonly ConcurrentQueue<PlaybackSession> _playbackQueue;
    private readonly object _recordingLock = new object();
    
    private class RecordingSession
    {
        public string SessionId { get; }
        public string Name { get; }
        public List<InputEvent> Events { get; }
        public DateTime StartTime { get; }
        public bool IsRecording { get; set; }

        public RecordingSession(string sessionId, string name)
        {
            SessionId = sessionId;
            Name = name;
            Events = new List<InputEvent>();
            StartTime = DateTime.UtcNow;
            IsRecording = true;
        }
    }

    private class PlaybackSession
    {
        public InputSequence Sequence { get; }
        public DateTime StartTime { get; }
        public CancellationTokenSource CancellationTokenSource { get; }

        public PlaybackSession(InputSequence sequence)
        {
            Sequence = sequence;
            StartTime = DateTime.UtcNow;
            CancellationTokenSource = new CancellationTokenSource();
        }
    }

    public InputRecordingService(ILogger<InputRecordingService> logger) : base(logger)
    {
        _activeSessions = new ConcurrentDictionary<string, RecordingSession>();
        _savedSequences = new ConcurrentDictionary<string, InputSequence>();
        _playbackQueue = new ConcurrentQueue<PlaybackSession>();
    }

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Initializing input recording service");
        
        // Load saved sequences if they exist
        await LoadSavedSequencesAsync(cancellationToken);
        
        await Task.CompletedTask;
    }

    protected override async Task OnStartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting input recording service");
        
        // Start background tasks for recording and playback
        _ = Task.Run(async () => await ProcessPlaybackQueueAsync(cancellationToken), cancellationToken);
        
        await Task.CompletedTask;
    }

    protected override async Task OnStopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Stopping input recording service");
        
        // Stop all active recording sessions
        foreach (var session in _activeSessions.Values)
        {
            session.IsRecording = false;
        }
        
        // Cancel all active playback sessions
        while (_playbackQueue.TryDequeue(out var playbackSession))
        {
            playbackSession.CancellationTokenSource.Cancel();
            playbackSession.CancellationTokenSource.Dispose();
        }
        
        // Save sequences before stopping
        await SaveSequencesAsync(cancellationToken);
        
        await Task.CompletedTask;
    }

    // BaseInputService overrides (InputRecordingService doesn't handle direct input)
    public override Task<bool> IsKeyPressedAsync(KeyCode key, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public override Task<Vector2> GetMousePositionAsync(CancellationToken cancellationToken = default) => Task.FromResult(Vector2.Zero);
    public override Task<bool> IsMouseButtonPressedAsync(MouseButton button, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public override Task<bool> IsGamepadButtonPressedAsync(int gamepadIndex, GamepadButton button, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public override Task<float> GetGamepadAxisAsync(int gamepadIndex, GamepadAxis axis, CancellationToken cancellationToken = default) => Task.FromResult(0.0f);
    public override Task<int> GetConnectedGamepadCountAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    public override Task<bool> IsGamepadConnectedAsync(int gamepadIndex, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public override Task<string?> GetGamepadNameAsync(int gamepadIndex, CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);

    #region IInputRecordingCapability Implementation

    public Task<string> StartRecordingAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Recording name cannot be null or empty", nameof(name));

        var sessionId = Guid.NewGuid().ToString();
        var session = new RecordingSession(sessionId, name);
        
        _activeSessions[sessionId] = session;
        
        _logger.LogInformation("Started recording session: {Name} ({SessionId})", name, sessionId);
        
        return Task.FromResult(sessionId);
    }

    public Task<InputSequence> StopRecordingAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));

        if (!_activeSessions.TryRemove(sessionId, out var session))
            throw new ArgumentException($"Recording session '{sessionId}' not found", nameof(sessionId));

        session.IsRecording = false;
        
        var sequence = new InputSequence(session.Name, session.Events, session.StartTime);
        _savedSequences[session.Name] = sequence;
        
        _logger.LogInformation("Stopped recording session: {Name} ({SessionId}) - Recorded {EventCount} events", 
            session.Name, sessionId, session.Events.Count);
        
        return Task.FromResult(sequence);
    }

    public Task PlaybackSequenceAsync(InputSequence sequence, CancellationToken cancellationToken = default)
    {
        if (sequence == null)
            throw new ArgumentNullException(nameof(sequence));

        var playbackSession = new PlaybackSession(sequence);
        _playbackQueue.Enqueue(playbackSession);
        
        _logger.LogInformation("Queued playback for sequence: {Name} ({EventCount} events)", 
            sequence.Name, sequence.Events.Count);
        
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var capabilities = new[] { typeof(IInputRecordingCapability) };
        return Task.FromResult<IEnumerable<Type>>(capabilities);
    }

    public Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        var hasCapability = typeof(T) == typeof(IInputRecordingCapability);
        return Task.FromResult(hasCapability);
    }

    public Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (typeof(T) == typeof(IInputRecordingCapability))
        {
            return Task.FromResult(this as T);
        }
        return Task.FromResult<T?>(null);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Records an input event to active recording sessions.
    /// </summary>
    /// <param name="inputEvent">The input event to record.</param>
    public void RecordInputEvent(InputEvent inputEvent)
    {
        if (inputEvent == null) return;

        lock (_recordingLock)
        {
            foreach (var session in _activeSessions.Values.Where(s => s.IsRecording))
            {
                session.Events.Add(inputEvent);
                _logger.LogTrace("Recorded event {EventType} to session {Name}", 
                    inputEvent.GetType().Name, session.Name);
            }
        }
    }

    /// <summary>
    /// Gets all saved input sequences.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    public Task<IEnumerable<InputSequence>> GetSavedSequencesAsync(CancellationToken cancellationToken = default)
    {
        var sequences = _savedSequences.Values.ToList();
        
        _logger.LogTrace("Retrieved {Count} saved sequences", sequences.Count);
        
        return Task.FromResult<IEnumerable<InputSequence>>(sequences);
    }

    /// <summary>
    /// Gets a specific saved sequence by name.
    /// </summary>
    /// <param name="name">Name of the sequence to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    public Task<InputSequence?> GetSequenceByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        _savedSequences.TryGetValue(name, out var sequence);
        
        _logger.LogTrace("Retrieved sequence by name: {Name} - {Found}", name, sequence != null ? "Found" : "Not found");
        
        return Task.FromResult(sequence);
    }

    /// <summary>
    /// Deletes a saved sequence.
    /// </summary>
    /// <param name="name">Name of the sequence to delete.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    public Task DeleteSequenceAsync(string name, CancellationToken cancellationToken = default)
    {
        var removed = _savedSequences.TryRemove(name, out _);
        
        _logger.LogInformation("Deleted sequence: {Name} - {Success}", name, removed ? "Success" : "Not found");
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets information about all active recording sessions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    public Task<IEnumerable<object>> GetActiveRecordingSessionsAsync(CancellationToken cancellationToken = default)
    {
        var sessions = _activeSessions.Values
            .Select(s => new
            {
                s.SessionId,
                s.Name,
                s.StartTime,
                s.IsRecording,
                EventCount = s.Events.Count,
                Duration = DateTime.UtcNow - s.StartTime
            })
            .ToList();
        
        _logger.LogTrace("Retrieved {Count} active recording sessions", sessions.Count);
        
        return Task.FromResult<IEnumerable<object>>(sessions);
    }

    /// <summary>
    /// Cancels an active recording session without saving.
    /// </summary>
    /// <param name="sessionId">ID of the session to cancel.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    public Task CancelRecordingAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var removed = _activeSessions.TryRemove(sessionId, out var session);
        
        _logger.LogInformation("Cancelled recording session: {SessionId} - {Success}", 
            sessionId, removed ? "Success" : "Not found");
        
        return Task.CompletedTask;
    }

    #endregion

    #region Private Methods

    private async Task ProcessPlaybackQueueAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && IsRunning)
        {
            try
            {
                if (_playbackQueue.TryDequeue(out var playbackSession))
                {
                    await ExecutePlaybackAsync(playbackSession, cancellationToken);
                    playbackSession.CancellationTokenSource.Dispose();
                }
                else
                {
                    await Task.Delay(100, cancellationToken); // Wait before checking queue again
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing playback queue");
            }
        }
    }

    private async Task ExecutePlaybackAsync(PlaybackSession playbackSession, CancellationToken cancellationToken)
    {
        var sequence = playbackSession.Sequence;
        var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, 
            playbackSession.CancellationTokenSource.Token
        ).Token;

        try
        {
            _logger.LogInformation("Starting playback of sequence: {Name}", sequence.Name);

            if (!sequence.Events.Any())
            {
                _logger.LogWarning("Sequence {Name} has no events to play back", sequence.Name);
                return;
            }

            var startTime = sequence.Events.First().Timestamp;
            var playbackStartTime = DateTime.UtcNow;

            foreach (var inputEvent in sequence.Events)
            {
                combinedToken.ThrowIfCancellationRequested();

                // Calculate the delay needed to maintain original timing
                var originalDelay = inputEvent.Timestamp - startTime;
                var targetTime = playbackStartTime + originalDelay;
                var currentTime = DateTime.UtcNow;

                if (targetTime > currentTime)
                {
                    var delay = targetTime - currentTime;
                    await Task.Delay(delay, combinedToken);
                }

                // Publish the event as if it was real input
                switch (inputEvent)
                {
                    case KeyEvent keyEvent:
                        PublishKeyEvent(keyEvent);
                        break;
                    case MouseEvent mouseEvent:
                        PublishMouseEvent(mouseEvent);
                        break;
                    case GamepadEvent gamepadEvent:
                        PublishGamepadEvent(gamepadEvent);
                        break;
                }

                _logger.LogTrace("Played back event: {EventType} at {Timestamp}", 
                    inputEvent.GetType().Name, inputEvent.Timestamp);
            }

            _logger.LogInformation("Completed playback of sequence: {Name}", sequence.Name);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Playback cancelled for sequence: {Name}", sequence.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during playback of sequence: {Name}", sequence.Name);
        }
    }

    private async Task LoadSavedSequencesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // In a real implementation, this would load from a file or database
            _logger.LogDebug("Loading saved input sequences from storage");
            
            await Task.Delay(10, cancellationToken); // Simulate I/O delay
            
            _logger.LogDebug("Loaded {Count} saved input sequences", _savedSequences.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load saved input sequences");
        }
    }

    private async Task SaveSequencesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // In a real implementation, this would save to a file or database
            _logger.LogDebug("Saving input sequences to storage");
            
            await Task.Delay(10, cancellationToken); // Simulate I/O delay
            
            _logger.LogDebug("Saved {Count} input sequences", _savedSequences.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save input sequences");
        }
    }

    #endregion
}