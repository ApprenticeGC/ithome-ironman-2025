using GameConsole.AI.Clustering.Core;
using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Clustering.Services;

/// <summary>
/// Service that provides cluster membership and leadership capabilities.
/// </summary>
public class ClusterCapabilityService : IService, ICapabilityProvider
{
    private readonly IClusterCoordinator _coordinator;
    private readonly List<Func<ClusterMembershipChangedEventArgs, Task>> _membershipCallbacks;
    private readonly List<Func<ClusterMember?, ClusterMember?, Task>> _leadershipCallbacks;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClusterCapabilityService"/> class.
    /// </summary>
    /// <param name="coordinator">The cluster coordinator.</param>
    public ClusterCapabilityService(IClusterCoordinator coordinator)
    {
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        _membershipCallbacks = new List<Func<ClusterMembershipChangedEventArgs, Task>>();
        _leadershipCallbacks = new List<Func<ClusterMember?, ClusterMember?, Task>>();
    }

    /// <inheritdoc />
    public bool IsRunning { get; private set; }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ClusterCapabilityService));

        await _coordinator.InitializeAsync(cancellationToken);
        
        // Subscribe to cluster events
        _coordinator.ClusterMembershipChanged += OnClusterMembershipChanged;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ClusterCapabilityService));

        await _coordinator.StartAsync(cancellationToken);
        await _coordinator.JoinClusterAsync(cancellationToken);
        
        IsRunning = true;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            return;

        IsRunning = false;
        
        await _coordinator.LeaveClusterAsync(cancellationToken);
        await _coordinator.StopAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        await StopAsync();
        
        if (_coordinator != null)
        {
            _coordinator.ClusterMembershipChanged -= OnClusterMembershipChanged;
            await _coordinator.DisposeAsync();
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Type>>(new[] { typeof(IClusterMembershipCapability), typeof(IClusterLeadershipCapability) });
    }

    /// <inheritdoc />
    public Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(typeof(T) == typeof(IClusterMembershipCapability) || typeof(T) == typeof(IClusterLeadershipCapability));
    }

    /// <inheritdoc />
    public Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (typeof(T) == typeof(IClusterMembershipCapability))
            return Task.FromResult(new ClusterMembershipCapability(_coordinator, _membershipCallbacks) as T);
        
        if (typeof(T) == typeof(IClusterLeadershipCapability))
            return Task.FromResult(new ClusterLeadershipCapability(_coordinator, _leadershipCallbacks) as T);

        return Task.FromResult<T?>(null);
    }

    private async void OnClusterMembershipChanged(object? sender, ClusterMembershipChangedEventArgs e)
    {
        // Notify all registered callbacks
        var tasks = _membershipCallbacks.Select(callback => 
        {
            try
            {
                return callback(e);
            }
            catch
            {
                return Task.CompletedTask;
            }
        });

        await Task.WhenAll(tasks);
    }
}

/// <summary>
/// Implementation of cluster membership capability.
/// </summary>
internal class ClusterMembershipCapability : IClusterMembershipCapability
{
    private readonly IClusterCoordinator _coordinator;
    private readonly List<Func<ClusterMembershipChangedEventArgs, Task>> _callbacks;

    public ClusterMembershipCapability(IClusterCoordinator coordinator, List<Func<ClusterMembershipChangedEventArgs, Task>> callbacks)
    {
        _coordinator = coordinator;
        _callbacks = callbacks;
    }

    public async Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        return new[] { typeof(IClusterMembershipCapability) };
    }

    public async Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        return typeof(T) == typeof(IClusterMembershipCapability);
    }

    public async Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        return typeof(T) == typeof(IClusterMembershipCapability) ? this as T : null;
    }

    public async Task<IEnumerable<ClusterMember>> GetMembersAsync(CancellationToken cancellationToken = default)
    {
        var state = await _coordinator.GetClusterStateAsync(cancellationToken);
        return state.Members;
    }

    public async Task<IEnumerable<ClusterMember>> GetMembersByRolesAsync(IEnumerable<string> roles, CancellationToken cancellationToken = default)
    {
        var state = await _coordinator.GetClusterStateAsync(cancellationToken);
        var roleSet = roles.ToHashSet();
        
        return state.Members.Where(m => m.Roles.Any(role => roleSet.Contains(role)));
    }

    public async Task RegisterMembershipCallbackAsync(Func<ClusterMembershipChangedEventArgs, Task> callback, CancellationToken cancellationToken = default)
    {
        if (callback != null)
        {
            _callbacks.Add(callback);
        }
    }
}

/// <summary>
/// Implementation of cluster leadership capability.
/// </summary>
internal class ClusterLeadershipCapability : IClusterLeadershipCapability
{
    private readonly IClusterCoordinator _coordinator;
    private readonly List<Func<ClusterMember?, ClusterMember?, Task>> _callbacks;

    public ClusterLeadershipCapability(IClusterCoordinator coordinator, List<Func<ClusterMember?, ClusterMember?, Task>> callbacks)
    {
        _coordinator = coordinator;
        _callbacks = callbacks;
    }

    public async Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        return new[] { typeof(IClusterLeadershipCapability) };
    }

    public async Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        return typeof(T) == typeof(IClusterLeadershipCapability);
    }

    public async Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        return typeof(T) == typeof(IClusterLeadershipCapability) ? this as T : null;
    }

    public async Task<ClusterMember?> GetLeaderAsync(CancellationToken cancellationToken = default)
    {
        var state = await _coordinator.GetClusterStateAsync(cancellationToken);
        return state.Leader;
    }

    public async Task<bool> IsLeaderAsync(CancellationToken cancellationToken = default)
    {
        var state = await _coordinator.GetClusterStateAsync(cancellationToken);
        return state.IsLeader;
    }

    public async Task RegisterLeadershipCallbackAsync(Func<ClusterMember?, ClusterMember?, Task> callback, CancellationToken cancellationToken = default)
    {
        if (callback != null)
        {
            _callbacks.Add(callback);
        }
    }

    public async Task<bool> CoordinateOperationAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        if (!await IsLeaderAsync(cancellationToken))
            return false;

        try
        {
            await operation();
            return true;
        }
        catch
        {
            return false;
        }
    }
}