using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Actors.Core.Local;

/// <summary>
/// Represents a message envelope with sender information.
/// </summary>
public record ActorMessage(object Message, IActorRef? Sender);

/// <summary>
/// Manages the lifecycle and message processing for a local actor.
/// </summary>
public class LocalActorCell : IAsyncDisposable
{
    private readonly IActor _actor;
    private readonly IActorContext _context;
    private readonly ILogger<LocalActorCell>? _logger;
    private readonly Channel<ActorMessage> _messageChannel;
    private readonly ChannelWriter<ActorMessage> _messageWriter;
    private readonly ChannelReader<ActorMessage> _messageReader;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task _messageProcessingTask;

    private volatile bool _isTerminated = false;
    private volatile bool _isStarted = false;

    public bool IsTerminated => _isTerminated;
    public bool IsStarted => _isStarted;

    public LocalActorCell(IActor actor, IActorContext context, ILogger<LocalActorCell>? logger = null)
    {
        _actor = actor ?? throw new ArgumentNullException(nameof(actor));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;

        // Create unbounded channel for message processing
        _messageChannel = Channel.CreateUnbounded<ActorMessage>();
        _messageWriter = _messageChannel.Writer;
        _messageReader = _messageChannel.Reader;
        
        _cancellationTokenSource = new CancellationTokenSource();
        
        // Start message processing loop
        _messageProcessingTask = Task.Run(ProcessMessagesAsync);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isStarted || _isTerminated)
            return;

        try
        {
            _logger?.LogDebug("Starting actor {ActorPath}", _context.Self.Path);
            await _actor.OnStartAsync(_context);
            _isStarted = true;
            _logger?.LogDebug("Started actor {ActorPath}", _context.Self.Path);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to start actor {ActorPath}", _context.Self.Path);
            throw;
        }
    }

    public async Task SendAsync(object message, IActorRef? sender, CancellationToken cancellationToken = default)
    {
        if (_isTerminated)
        {
            _logger?.LogWarning("Attempted to send message to terminated actor {ActorPath}", _context.Self.Path);
            return;
        }

        var envelope = new ActorMessage(message, sender);
        
        try
        {
            await _messageWriter.WriteAsync(envelope, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning("Message send cancelled for actor {ActorPath}", _context.Self.Path);
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to send message to actor {ActorPath}", _context.Self.Path);
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_isTerminated)
            return;

        try
        {
            _logger?.LogDebug("Stopping actor {ActorPath}", _context.Self.Path);
            
            // Signal no more messages
            _messageWriter.Complete();
            
            // Wait for message processing to complete
            await _messageProcessingTask;
            
            // Call actor stop
            await _actor.OnStopAsync(_context);
            
            _isTerminated = true;
            _logger?.LogDebug("Stopped actor {ActorPath}", _context.Self.Path);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error stopping actor {ActorPath}", _context.Self.Path);
            _isTerminated = true;
        }
    }

    private async Task ProcessMessagesAsync()
    {
        try
        {
            await foreach (var envelope in _messageReader.ReadAllAsync(_cancellationTokenSource.Token))
            {
                await ProcessSingleMessage(envelope);
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogDebug("Message processing cancelled for actor {ActorPath}", _context.Self.Path);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unhandled error in message processing loop for actor {ActorPath}", 
                _context.Self.Path);
        }
    }

    private async Task ProcessSingleMessage(ActorMessage envelope)
    {
        try
        {
            // Update context with current sender
            if (_context is LocalActorContext localContext)
            {
                localContext.SetCurrentSender(envelope.Sender);
            }

            await _actor.OnReceiveAsync(envelope.Message, _context);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing message {MessageType} in actor {ActorPath}", 
                envelope.Message.GetType().Name, _context.Self.Path);
            
            // Consider restarting the actor on failure (supervision strategy)
            await HandleActorFailure(ex);
        }
        finally
        {
            // Clear sender from context
            if (_context is LocalActorContext localContext)
            {
                localContext.SetCurrentSender(null);
            }
        }
    }

    private async Task HandleActorFailure(Exception exception)
    {
        try
        {
            // Simple restart strategy - call OnRestartAsync
            await _actor.OnRestartAsync(exception, _context);
        }
        catch (Exception restartEx)
        {
            _logger?.LogError(restartEx, "Actor restart failed for {ActorPath}, terminating", 
                _context.Self.Path);
            
            // If restart fails, terminate the actor
            _cancellationTokenSource.Cancel();
            _isTerminated = true;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_isTerminated)
        {
            await StopAsync();
        }

        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        
        try
        {
            await _messageProcessingTask;
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}