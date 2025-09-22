using GameConsole.Engine.Core;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.AI.Services;

/// <summary>
/// Base implementation for actors in the AI Agent Actor system.
/// Provides fundamental actor capabilities including message processing,
/// state management, and cluster integration.
/// </summary>
public abstract class BaseActor : IActor
{
    protected readonly ILogger _logger;
    private readonly ConcurrentQueue<ActorMessage> _messageQueue;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Dictionary<string, object> _metrics;
    private ActorState _state;
    private string? _clusterId;
    private bool _disposed;
    private Task? _messageProcessingTask;

    /// <summary>
    /// Initializes a new instance of the BaseActor class.
    /// </summary>
    /// <param name="actorId">Unique identifier for this actor.</param>
    /// <param name="logger">Logger instance for this actor.</param>
    protected BaseActor(string actorId, ILogger logger)
    {
        ActorId = actorId ?? throw new ArgumentNullException(nameof(actorId));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _messageQueue = new ConcurrentQueue<ActorMessage>();
        _cancellationTokenSource = new CancellationTokenSource();
        _metrics = new Dictionary<string, object>();
        _state = ActorState.Created;
    }

    #region IActor Implementation

    public event EventHandler<ActorStateChangedEventArgs>? StateChanged;

    public string ActorId { get; }

    public ActorState State => _state;

    public string? ClusterId => _clusterId;

    public virtual async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BaseActor));
        
        ChangeState(ActorState.Initializing);
        
        _logger.LogInformation("Initializing actor {ActorId}", ActorId);
        
        try
        {
            await OnInitializeAsync(cancellationToken);
            _logger.LogInformation("Initialized actor {ActorId}", ActorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize actor {ActorId}", ActorId);
            ChangeState(ActorState.Faulted);
            throw;
        }
    }

    public virtual async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BaseActor));
        if (_state != ActorState.Initializing) throw new InvalidOperationException("Actor must be initialized before starting");
        
        _logger.LogInformation("Starting actor {ActorId}", ActorId);
        
        try
        {
            // Start message processing loop
            _messageProcessingTask = ProcessMessagesAsync(_cancellationTokenSource.Token);
            
            await OnStartAsync(cancellationToken);
            
            ChangeState(ActorState.Active);
            _logger.LogInformation("Started actor {ActorId}", ActorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start actor {ActorId}", ActorId);
            ChangeState(ActorState.Faulted);
            throw;
        }
    }

    public virtual async Task PauseAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BaseActor));
        if (_state != ActorState.Active) throw new InvalidOperationException("Actor must be active to pause");
        
        _logger.LogInformation("Pausing actor {ActorId}", ActorId);
        
        try
        {
            await OnPauseAsync(cancellationToken);
            ChangeState(ActorState.Paused);
            _logger.LogInformation("Paused actor {ActorId}", ActorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pause actor {ActorId}", ActorId);
            ChangeState(ActorState.Faulted);
            throw;
        }
    }

    public virtual async Task ResumeAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BaseActor));
        if (_state != ActorState.Paused) throw new InvalidOperationException("Actor must be paused to resume");
        
        _logger.LogInformation("Resuming actor {ActorId}", ActorId);
        
        try
        {
            await OnResumeAsync(cancellationToken);
            ChangeState(ActorState.Active);
            _logger.LogInformation("Resumed actor {ActorId}", ActorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resume actor {ActorId}", ActorId);
            ChangeState(ActorState.Faulted);
            throw;
        }
    }

    public virtual async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) return;
        
        _logger.LogInformation("Stopping actor {ActorId}", ActorId);
        
        try
        {
            ChangeState(ActorState.Stopping);
            
            // Cancel message processing
            _cancellationTokenSource.Cancel();
            
            if (_messageProcessingTask != null)
            {
                await _messageProcessingTask;
            }
            
            await OnStopAsync(cancellationToken);
            
            ChangeState(ActorState.Stopped);
            _logger.LogInformation("Stopped actor {ActorId}", ActorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping actor {ActorId}", ActorId);
            ChangeState(ActorState.Faulted);
        }
    }

    public virtual Task SendMessageAsync(ActorMessage message, CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BaseActor));
        if (message == null) throw new ArgumentNullException(nameof(message));
        
        if (_state == ActorState.Active)
        {
            _messageQueue.Enqueue(message);
            UpdateMetric("MessagesReceived", GetMetricValue("MessagesReceived") + 1);
        }
        
        return Task.CompletedTask;
    }

    public virtual Task SendMessageToActorAsync(string targetActorId, ActorMessage message, CancellationToken cancellationToken = default)
    {
        // This would typically be handled by the cluster manager
        _logger.LogWarning("SendMessageToActorAsync not implemented in base actor. Target: {TargetActorId}, Message: {MessageType}", 
            targetActorId, message.GetType().Name);
        return Task.CompletedTask;
    }

    public virtual Task JoinClusterAsync(string clusterId, CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BaseActor));
        
        _clusterId = clusterId;
        _logger.LogInformation("Actor {ActorId} joined cluster {ClusterId}", ActorId, clusterId);
        return Task.CompletedTask;
    }

    public virtual Task LeaveClusterAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BaseActor));
        
        var previousClusterId = _clusterId;
        _clusterId = null;
        _logger.LogInformation("Actor {ActorId} left cluster {ClusterId}", ActorId, previousClusterId);
        return Task.CompletedTask;
    }

    public virtual Task<IDictionary<string, object>> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BaseActor));
        
        var metrics = new Dictionary<string, object>(_metrics)
        {
            ["ActorId"] = ActorId,
            ["State"] = _state.ToString(),
            ["ClusterId"] = _clusterId ?? "None",
            ["QueuedMessages"] = _messageQueue.Count,
            ["LastUpdated"] = DateTimeOffset.UtcNow
        };
        
        return Task.FromResult<IDictionary<string, object>>(metrics);
    }

    #endregion

    #region Protected Virtual Methods

    /// <summary>
    /// Called during actor initialization. Override to provide custom initialization logic.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async initialization operation.</returns>
    protected virtual Task OnInitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the actor starts. Override to provide custom start logic.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async start operation.</returns>
    protected virtual Task OnStartAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the actor is paused. Override to provide custom pause logic.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async pause operation.</returns>
    protected virtual Task OnPauseAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the actor is resumed. Override to provide custom resume logic.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async resume operation.</returns>
    protected virtual Task OnResumeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the actor stops. Override to provide custom stop logic.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async stop operation.</returns>
    protected virtual Task OnStopAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when a message needs to be processed. Override to provide custom message handling.
    /// </summary>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async message processing operation.</returns>
    protected virtual Task OnProcessMessageAsync(ActorMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Processing message {MessageType} from {SenderId} for actor {ActorId}", 
            message.GetType().Name, message.SenderId ?? "Unknown", ActorId);
        return Task.CompletedTask;
    }

    #endregion

    #region Private Methods

    private void ChangeState(ActorState newState)
    {
        var previousState = _state;
        _state = newState;
        
        StateChanged?.Invoke(this, new ActorStateChangedEventArgs(ActorId, previousState, newState));
        UpdateMetric("StateChanges", GetMetricValue("StateChanges") + 1);
    }

    private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (_messageQueue.TryDequeue(out var message) && _state == ActorState.Active)
                {
                    await OnProcessMessageAsync(message, cancellationToken);
                    UpdateMetric("MessagesProcessed", GetMetricValue("MessagesProcessed") + 1);
                }
                else
                {
                    // Wait a bit if no messages or not active
                    await Task.Delay(10, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message in actor {ActorId}", ActorId);
                // Continue processing other messages
            }
        }
    }

    private void UpdateMetric(string key, long value)
    {
        lock (_metrics)
        {
            _metrics[key] = value;
        }
    }

    private long GetMetricValue(string key)
    {
        lock (_metrics)
        {
            return _metrics.TryGetValue(key, out var value) && value is long longValue ? longValue : 0;
        }
    }

    #endregion

    #region IAsyncDisposable Implementation

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        
        await StopAsync();
        
        _cancellationTokenSource.Dispose();
        _disposed = true;
        
        GC.SuppressFinalize(this);
    }

    #endregion
}