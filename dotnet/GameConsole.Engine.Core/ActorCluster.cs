using GameConsole.Core.Abstractions;
using System.Collections.Concurrent;

namespace GameConsole.Engine.Core;

/// <summary>
/// Implementation of IActorCluster that manages groups of related actors.
/// Provides registration, message routing, and coordination services.
/// </summary>
public class ActorCluster : IActorCluster
{
    private readonly ConcurrentDictionary<ActorId, WeakReference<IActor>> _members;
    private readonly object _lifecycleLock = new();

    /// <inheritdoc/>
    public ClusterId Id { get; }

    /// <inheritdoc/>
    public string ClusterName { get; }

    /// <inheritdoc/>
    public string ActorType { get; }

    /// <inheritdoc/>
    public bool IsRunning { get; private set; }

    /// <inheritdoc/>
    public int MemberCount => _members.Count;

    /// <inheritdoc/>
    public event EventHandler<ClusterMembershipEventArgs>? MembershipChanged;

    /// <summary>
    /// Initializes a new actor cluster.
    /// </summary>
    /// <param name="clusterName">Human-readable name for the cluster.</param>
    /// <param name="actorType">Type of actors this cluster manages.</param>
    /// <param name="clusterId">Optional cluster ID. If not provided, generates a new ID.</param>
    public ActorCluster(string clusterName, string actorType, ClusterId? clusterId = null)
    {
        Id = clusterId ?? ClusterId.NewId();
        ClusterName = clusterName ?? throw new ArgumentNullException(nameof(clusterName));
        ActorType = actorType ?? throw new ArgumentNullException(nameof(actorType));
        _members = new ConcurrentDictionary<ActorId, WeakReference<IActor>>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        lock (_lifecycleLock)
        {
            if (IsRunning)
                throw new InvalidOperationException($"Cluster {Id} is already initialized");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_lifecycleLock)
        {
            if (IsRunning)
                return Task.CompletedTask;

            IsRunning = true;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        lock (_lifecycleLock)
        {
            if (!IsRunning)
                return Task.CompletedTask;

            IsRunning = false;
        }

        // Clear all member references
        _members.Clear();
        
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        if (IsRunning)
        {
            _ = StopAsync();
        }

        _members.Clear();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<ActorId>> GetMemberIdsAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
            throw new InvalidOperationException($"Cluster {Id} is not running");

        CleanupDeadReferences();
        return Task.FromResult(_members.Keys.AsEnumerable());
    }

    /// <inheritdoc/>
    public Task RegisterActorAsync(IActor actor, CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
            throw new InvalidOperationException($"Cluster {Id} is not running");

        if (actor == null)
            throw new ArgumentNullException(nameof(actor));

        if (actor.ActorType != ActorType)
            throw new ArgumentException($"Actor type '{actor.ActorType}' does not match cluster type '{ActorType}'");

        var weakRef = new WeakReference<IActor>(actor);
        if (_members.TryAdd(actor.Id, weakRef))
        {
            MembershipChanged?.Invoke(this, new ClusterMembershipEventArgs(Id, actor.Id, isJoining: true));
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task UnregisterActorAsync(ActorId actorId, CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
            throw new InvalidOperationException($"Cluster {Id} is not running");

        if (_members.TryRemove(actorId, out _))
        {
            MembershipChanged?.Invoke(this, new ClusterMembershipEventArgs(Id, actorId, isJoining: false));
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> HasMemberAsync(ActorId actorId, CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
            throw new InvalidOperationException($"Cluster {Id} is not running");

        // Check if we have the actor and it's still alive
        if (_members.TryGetValue(actorId, out var weakRef) && weakRef.TryGetTarget(out _))
        {
            return Task.FromResult(true);
        }

        // Clean up dead reference if it exists
        if (weakRef != null)
        {
            _members.TryRemove(actorId, out _);
        }

        return Task.FromResult(false);
    }

    /// <inheritdoc/>
    public async Task BroadcastMessageAsync(IActorMessage message, ActorId? excludeActor = null, CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
            throw new InvalidOperationException($"Cluster {Id} is not running");

        if (message == null)
            throw new ArgumentNullException(nameof(message));

        CleanupDeadReferences();

        var tasks = new List<Task>();

        foreach (var kvp in _members)
        {
            var actorId = kvp.Key;
            var weakRef = kvp.Value;

            // Skip excluded actor
            if (excludeActor.HasValue && actorId == excludeActor.Value)
                continue;

            if (weakRef.TryGetTarget(out var actor))
            {
                tasks.Add(actor.SendMessageAsync(message, cancellationToken));
            }
            else
            {
                // Clean up dead reference
                _members.TryRemove(actorId, out _);
            }
        }

        if (tasks.Any())
        {
            await Task.WhenAll(tasks);
        }
    }

    /// <inheritdoc/>
    public async Task SendMessageToActorAsync(ActorId actorId, IActorMessage message, CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
            throw new InvalidOperationException($"Cluster {Id} is not running");

        if (message == null)
            throw new ArgumentNullException(nameof(message));

        if (_members.TryGetValue(actorId, out var weakRef) && weakRef.TryGetTarget(out var actor))
        {
            await actor.SendMessageAsync(message, cancellationToken);
        }
        else
        {
            // Clean up dead reference if it exists
            if (weakRef != null)
            {
                _members.TryRemove(actorId, out _);
            }
            
            throw new ArgumentException($"Actor {actorId} is not a member of cluster {Id}");
        }
    }

    /// <summary>
    /// Removes dead weak references from the member collection.
    /// Should be called periodically to prevent memory leaks.
    /// </summary>
    private void CleanupDeadReferences()
    {
        var deadKeys = new List<ActorId>();

        foreach (var kvp in _members)
        {
            if (!kvp.Value.TryGetTarget(out _))
            {
                deadKeys.Add(kvp.Key);
            }
        }

        foreach (var key in deadKeys)
        {
            if (_members.TryRemove(key, out _))
            {
                MembershipChanged?.Invoke(this, new ClusterMembershipEventArgs(Id, key, isJoining: false));
            }
        }
    }
}