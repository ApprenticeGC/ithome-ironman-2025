using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.AI.Actors.Core.Local;

/// <summary>
/// Local implementation of actor context for in-process actors.
/// </summary>
public class LocalActorContext : IActorContext
{
    private readonly LocalActorSystem _system;
    private readonly ILogger<LocalActorContext>? _logger;
    private readonly ConcurrentDictionary<string, IActorRef> _children = new();
    private readonly ConcurrentDictionary<string, IActorRef> _watchedActors = new();

    private IActorRef? _currentSender;

    public IActorRef Self { get; }
    public IActorRef? Sender => _currentSender;
    public IActorSystem System => _system;

    public LocalActorContext(IActorRef self, LocalActorSystem system, ILogger<LocalActorContext>? logger = null)
    {
        Self = self ?? throw new ArgumentNullException(nameof(self));
        _system = system ?? throw new ArgumentNullException(nameof(system));
        _logger = logger;
    }

    /// <summary>
    /// Sets the current sender for message processing (internal use only).
    /// </summary>
    internal void SetCurrentSender(IActorRef? sender)
    {
        _currentSender = sender;
    }

    public async Task<IActorRef> ActorOfAsync<T>(string? name = null, CancellationToken cancellationToken = default) 
        where T : IActor, new()
    {
        var actorName = name ?? GenerateActorName<T>();
        var childPath = $"{Self.Path}/{actorName}";

        // Check if child already exists
        if (_children.ContainsKey(actorName))
        {
            throw new InvalidOperationException($"Child actor with name '{actorName}' already exists");
        }

        try
        {
            var childActor = await _system.CreateChildActorAsync<T>(childPath, actorName, this, cancellationToken);
            _children.TryAdd(actorName, childActor);
            
            _logger?.LogDebug("Created child actor {ChildPath} from parent {ParentPath}", 
                childPath, Self.Path);
            
            return childActor;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create child actor {ChildPath} from parent {ParentPath}", 
                childPath, Self.Path);
            throw;
        }
    }

    public async Task StopAsync(IActorRef child, CancellationToken cancellationToken = default)
    {
        if (child == null) throw new ArgumentNullException(nameof(child));

        try
        {
            // Find the child in our collection
            var childEntry = _children.FirstOrDefault(kvp => kvp.Value.Path == child.Path);
            if (childEntry.Key != null)
            {
                await _system.StopActorAsync(child, cancellationToken);
                _children.TryRemove(childEntry.Key, out _);
                
                _logger?.LogDebug("Stopped child actor {ChildPath} from parent {ParentPath}", 
                    child.Path, Self.Path);
            }
            else
            {
                _logger?.LogWarning("Attempted to stop non-child actor {ActorPath} from {ParentPath}", 
                    child.Path, Self.Path);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to stop child actor {ChildPath} from parent {ParentPath}", 
                child.Path, Self.Path);
            throw;
        }
    }

    public async Task StopSelfAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _system.StopActorAsync(Self, cancellationToken);
            _logger?.LogDebug("Actor {ActorPath} stopped itself", Self.Path);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to stop self {ActorPath}", Self.Path);
            throw;
        }
    }

    public Task WatchAsync(IActorRef actor, CancellationToken cancellationToken = default)
    {
        if (actor == null) throw new ArgumentNullException(nameof(actor));

        _watchedActors.TryAdd(actor.Path, actor);
        _logger?.LogDebug("Actor {WatcherPath} is now watching {WatchedPath}", Self.Path, actor.Path);
        
        // In a full implementation, this would set up termination notifications
        return Task.CompletedTask;
    }

    public Task UnwatchAsync(IActorRef actor, CancellationToken cancellationToken = default)
    {
        if (actor == null) throw new ArgumentNullException(nameof(actor));

        _watchedActors.TryRemove(actor.Path, out _);
        _logger?.LogDebug("Actor {WatcherPath} stopped watching {WatchedPath}", Self.Path, actor.Path);
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets all child actors.
    /// </summary>
    public IEnumerable<IActorRef> Children => _children.Values.ToArray();

    /// <summary>
    /// Gets all watched actors.
    /// </summary>
    public IEnumerable<IActorRef> WatchedActors => _watchedActors.Values.ToArray();

    private static string GenerateActorName<T>() where T : IActor
    {
        return $"{typeof(T).Name}-{Guid.NewGuid():N}"[..16];
    }
}