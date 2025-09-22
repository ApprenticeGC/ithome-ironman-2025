using GameConsole.Core.Abstractions;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;

namespace GameConsole.Engine.Core;

/// <summary>
/// Base implementation of the IActor interface providing common actor functionality.
/// Handles message processing, lifecycle management, and request/response patterns.
/// </summary>
public abstract class ActorBase : IActor
{
    private readonly Channel<IActorMessage> _messageChannel;
    private readonly ChannelWriter<IActorMessage> _messageWriter;
    private readonly ChannelReader<IActorMessage> _messageReader;
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<IActorMessage>> _pendingRequests;
    private CancellationTokenSource? _processingCancellation;
    private Task? _messageProcessingTask;
    private readonly object _lifecycleLock = new();
    private bool _isInitialized;

    /// <inheritdoc/>
    public ActorId Id { get; }

    /// <inheritdoc/>
    public abstract string ActorType { get; }

    /// <inheritdoc/>
    public bool IsRunning { get; private set; }

    /// <inheritdoc/>
    public event EventHandler<ActorMessageEventArgs>? MessageProcessed;

    /// <summary>
    /// Initializes a new actor with the specified ID.
    /// </summary>
    /// <param name="actorId">Unique identifier for this actor. If not provided, generates a new ID.</param>
    protected ActorBase(ActorId? actorId = null)
    {
        Id = actorId ?? ActorId.NewId();
        
        // Create unbounded channel for message processing
        var options = new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        };
        _messageChannel = Channel.CreateUnbounded<IActorMessage>(options);
        _messageWriter = _messageChannel.Writer;
        _messageReader = _messageChannel.Reader;
        _pendingRequests = new ConcurrentDictionary<Guid, TaskCompletionSource<IActorMessage>>();
    }

    /// <inheritdoc/>
    public virtual async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        lock (_lifecycleLock)
        {
            if (_isInitialized)
                throw new InvalidOperationException($"Actor {Id} is already initialized");
            
            _isInitialized = true;
        }

        await OnInitializeAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_lifecycleLock)
        {
            if (IsRunning)
                return;

            _processingCancellation = new CancellationTokenSource();
            IsRunning = true;
        }

        // Start message processing loop
        _messageProcessingTask = ProcessMessagesAsync(_processingCancellation.Token);

        await OnStartAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task StopAsync(CancellationToken cancellationToken = default)
    {
        Task? processingTask;
        CancellationTokenSource? cancellationSource;

        lock (_lifecycleLock)
        {
            if (!IsRunning)
                return;

            IsRunning = false;
            cancellationSource = _processingCancellation;
            processingTask = _messageProcessingTask;
        }

        // Signal cancellation and wait for processing to complete
        cancellationSource?.Cancel();
        if (processingTask != null)
        {
            try
            {
                await processingTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelling
            }
        }

        await OnStopAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async ValueTask DisposeAsync()
    {
        if (IsRunning)
        {
            await StopAsync();
        }

        _messageWriter.Complete();
        _processingCancellation?.Dispose();

        // Complete all pending requests with cancellation
        foreach (var request in _pendingRequests.Values)
        {
            request.SetCanceled();
        }
        _pendingRequests.Clear();

        await OnDisposeAsync();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public async Task SendMessageAsync(IActorMessage message, CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
            throw new InvalidOperationException($"Actor {Id} is not running");

        if (!await _messageWriter.WaitToWriteAsync(cancellationToken))
            throw new InvalidOperationException($"Actor {Id} message channel is closed");

        if (!_messageWriter.TryWrite(message))
            throw new InvalidOperationException($"Failed to queue message for actor {Id}");
    }

    /// <inheritdoc/>
    public async Task<TResponse?> SendRequestAsync<TResponse>(IActorMessage request, TimeSpan timeout, CancellationToken cancellationToken = default)
        where TResponse : class, IActorMessage
    {
        var responseTask = new TaskCompletionSource<IActorMessage>();
        _pendingRequests[request.MessageId] = responseTask;

        try
        {
            await SendMessageAsync(request, cancellationToken);

            using var timeoutCancellation = new CancellationTokenSource(timeout);
            using var combinedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellation.Token);

            var response = await responseTask.Task.WaitAsync(combinedCancellation.Token);
            return response as TResponse;
        }
        catch (OperationCanceledException)
        {
            return null; // Timeout or cancellation
        }
        finally
        {
            _pendingRequests.TryRemove(request.MessageId, out _);
        }
    }

    /// <summary>
    /// Main message processing loop that runs while the actor is active.
    /// </summary>
    private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        await foreach (var message in _messageReader.ReadAllAsync(cancellationToken))
        {
            var stopwatch = Stopwatch.StartNew();
            ActorMessageHandleResult result = ActorMessageHandleResult.NotHandled;
            Exception? exception = null;

            try
            {
                // Check if this is a response to a pending request
                if (message.CorrelationId.HasValue && 
                    _pendingRequests.TryRemove(message.CorrelationId.Value, out var pendingRequest))
                {
                    pendingRequest.SetResult(message);
                    result = ActorMessageHandleResult.Handled;
                }
                else
                {
                    // Regular message processing
                    result = await HandleMessageAsync(message, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                exception = ex;
                result = ActorMessageHandleResult.Failed;
            }
            finally
            {
                stopwatch.Stop();
                MessageProcessed?.Invoke(this, new ActorMessageEventArgs(message, result, stopwatch.Elapsed, exception));
            }
        }
    }

    /// <summary>
    /// Abstract method that derived classes must implement to handle incoming messages.
    /// </summary>
    /// <param name="message">The message to handle.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Result indicating how the message was handled.</returns>
    protected abstract Task<ActorMessageHandleResult> HandleMessageAsync(IActorMessage message, CancellationToken cancellationToken);

    /// <summary>
    /// Virtual method called during initialization. Override to provide custom initialization logic.
    /// </summary>
    protected virtual Task OnInitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Virtual method called when starting. Override to provide custom start logic.
    /// </summary>
    protected virtual Task OnStartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Virtual method called when stopping. Override to provide custom stop logic.
    /// </summary>
    protected virtual Task OnStopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Virtual method called during disposal. Override to provide custom cleanup logic.
    /// </summary>
    protected virtual ValueTask OnDisposeAsync() => ValueTask.CompletedTask;
}