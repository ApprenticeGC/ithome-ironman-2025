using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.AI.Actors.Core.Local;

/// <summary>
/// Local implementation of the actor system for in-process actor management.
/// </summary>
public class LocalActorSystem : IActorSystem
{
    private readonly ILogger<LocalActorSystem> _logger;
    private readonly ConcurrentDictionary<string, LocalActorCell> _actors = new();
    private readonly DeadLetterActor _deadLetterActor;
    private readonly IActorRef _deadLetterRef;

    private volatile bool _isRunning = false;
    private volatile bool _isDisposed = false;

    public bool IsRunning => _isRunning;
    public IClusterManager? Cluster => null; // No clustering in local implementation
    public IActorRef DeadLetters => _deadLetterRef;

    public LocalActorSystem(ILogger<LocalActorSystem> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Create dead letter actor
        _deadLetterActor = new DeadLetterActor(_logger);
        var deadLetterContext = new LocalActorContext(
            new DeadLetterActorRef(), 
            this, 
            logger.CreateLogger<LocalActorContext>());
        var deadLetterCell = new LocalActorCell(
            _deadLetterActor, 
            deadLetterContext, 
            logger.CreateLogger<LocalActorCell>());
        
        _deadLetterRef = new LocalActorRef("/system/deadLetters", "deadLetters", deadLetterCell);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(LocalActorSystem));

        _logger.LogInformation("Initializing LocalActorSystem");
        
        // Initialize dead letter actor
        await _deadLetterActor.OnStartAsync(new LocalActorContext(_deadLetterRef, this));
        
        _logger.LogInformation("LocalActorSystem initialized");
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(LocalActorSystem));
        if (_isRunning) return;

        _logger.LogInformation("Starting LocalActorSystem");
        _isRunning = true;
        _logger.LogInformation("LocalActorSystem started");
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning) return;

        _logger.LogInformation("Stopping LocalActorSystem");
        
        // Stop all actors
        var stopTasks = _actors.Values.Select(cell => cell.StopAsync(cancellationToken));
        await Task.WhenAll(stopTasks);
        
        _isRunning = false;
        _logger.LogInformation("LocalActorSystem stopped");
    }

    public async Task<IActorRef> ActorOfAsync<T>(string? name = null, CancellationToken cancellationToken = default) 
        where T : IActor, new()
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(LocalActorSystem));
        if (!_isRunning) throw new InvalidOperationException("ActorSystem is not running");

        var actorName = name ?? GenerateActorName<T>();
        var actorPath = $"/user/{actorName}";

        return await CreateActorAsync<T>(actorPath, actorName, cancellationToken);
    }

    public Task<IActorRef?> ActorSelectionAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Task.FromResult<IActorRef?>(null);

        // Simple path-based lookup
        var cell = _actors.Values.FirstOrDefault(c => 
            c is LocalActorCell localCell && 
            ((LocalActorRef)((LocalActorContext)localCell).Self).Path == path);

        return Task.FromResult<IActorRef?>(
            cell != null ? ((LocalActorContext)cell).Self : null);
    }

    public async Task TerminateAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        var actualTimeout = timeout ?? TimeSpan.FromSeconds(30);
        using var timeoutCts = new CancellationTokenSource(actualTimeout);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutCts.Token);

        try
        {
            await StopAsync(combinedCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            _logger.LogWarning("ActorSystem termination timed out after {Timeout}", actualTimeout);
            throw new TimeoutException($"ActorSystem termination timed out after {actualTimeout}");
        }
    }

    // ICapabilityProvider implementation
    public Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var capabilities = new[] { typeof(IActorSystem) };
        return Task.FromResult<IEnumerable<Type>>(capabilities);
    }

    public Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(typeof(T) == typeof(IActorSystem));
    }

    public Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (typeof(T) == typeof(IActorSystem))
            return Task.FromResult(this as T);
        
        return Task.FromResult<T?>(null);
    }

    /// <summary>
    /// Creates a child actor (internal use).
    /// </summary>
    internal async Task<IActorRef> CreateChildActorAsync<T>(string path, string name, 
        LocalActorContext parentContext, CancellationToken cancellationToken = default) 
        where T : IActor, new()
    {
        return await CreateActorAsync<T>(path, name, cancellationToken);
    }

    /// <summary>
    /// Stops an actor (internal use).
    /// </summary>
    internal async Task StopActorAsync(IActorRef actorRef, CancellationToken cancellationToken = default)
    {
        if (_actors.TryGetValue(actorRef.Path, out var cell))
        {
            await cell.StopAsync(cancellationToken);
            _actors.TryRemove(actorRef.Path, out _);
        }
    }

    private async Task<IActorRef> CreateActorAsync<T>(string path, string name, CancellationToken cancellationToken = default)
        where T : IActor, new()
    {
        if (_actors.ContainsKey(path))
        {
            throw new InvalidOperationException($"Actor with path '{path}' already exists");
        }

        try
        {
            // Create actor instance
            var actor = new T();
            
            // Create context and cell
            var actorRef = new LocalActorRef(path, name, null!); // Temporary ref for context creation
            var context = new LocalActorContext(actorRef, this, _logger.CreateLogger<LocalActorContext>());
            var cell = new LocalActorCell(actor, context, _logger.CreateLogger<LocalActorCell>());
            
            // Update the ref with the actual cell
            var finalActorRef = new LocalActorRef(path, name, cell, _logger.CreateLogger<LocalActorRef>());
            
            // Update context with final ref
            var finalContext = new LocalActorContext(finalActorRef, this, _logger.CreateLogger<LocalActorContext>());
            
            // Replace cell with updated context
            await cell.DisposeAsync();
            cell = new LocalActorCell(actor, finalContext, _logger.CreateLogger<LocalActorCell>());
            finalActorRef = new LocalActorRef(path, name, cell, _logger.CreateLogger<LocalActorRef>());

            // Store and start
            _actors.TryAdd(path, cell);
            await cell.StartAsync(cancellationToken);

            _logger.LogDebug("Created and started actor {ActorPath}", path);
            return finalActorRef;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create actor {ActorPath}", path);
            _actors.TryRemove(path, out _);
            throw;
        }
    }

    private static string GenerateActorName<T>() where T : IActor
    {
        return $"{typeof(T).Name}-{Guid.NewGuid():N}"[..16];
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;

        try
        {
            await TerminateAsync();
        }
        finally
        {
            _isDisposed = true;
        }
    }
}

/// <summary>
/// Simple dead letter actor that logs undeliverable messages.
/// </summary>
internal class DeadLetterActor : ActorBase
{
    private readonly ILogger _logger;

    public DeadLetterActor(ILogger logger)
    {
        _logger = logger;
    }

    public override Task OnReceiveAsync(object message, IActorContext context)
    {
        _logger.LogWarning("Dead letter: {MessageType} from {Sender}", 
            message.GetType().Name, context.Sender?.Path ?? "unknown");
        return Task.CompletedTask;
    }
}

/// <summary>
/// Special actor ref for dead letters.
/// </summary>
internal class DeadLetterActorRef : IActorRef
{
    public string Path => "/system/deadLetters";
    public string Name => "deadLetters";
    public bool IsValid => true;

    public Task TellAsync(object message, IActorRef? sender = null, CancellationToken cancellationToken = default)
    {
        // Messages sent to dead letters are just logged
        return Task.CompletedTask;
    }

    public Task<TResponse> AskAsync<TResponse>(object message, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Cannot ask dead letters");
    }
}