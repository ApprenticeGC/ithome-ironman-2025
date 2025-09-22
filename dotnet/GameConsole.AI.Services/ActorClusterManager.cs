using GameConsole.Core.Abstractions;
using GameConsole.Engine.Core;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.AI.Services;

/// <summary>
/// Default implementation of the Actor Cluster Manager service for managing and coordinating
/// clusters of actors with load balancing, failover, and performance optimization capabilities.
/// </summary>
public class ActorClusterManager : IActorClusterManager
{
    private readonly ILogger<ActorClusterManager> _logger;
    private readonly ConcurrentDictionary<string, ClusterConfiguration> _clusterConfigurations;
    private readonly ConcurrentDictionary<string, ClusterState> _clusterStates;
    private readonly ConcurrentDictionary<string, ClusterMetrics> _clusterMetrics;
    private readonly ConcurrentDictionary<string, List<string>> _clusterActors;
    private readonly ConcurrentDictionary<string, string> _actorToClusterMapping;
    private readonly ConcurrentDictionary<string, IActor> _registeredActors;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Timer _metricsCollectionTimer;
    private bool _isRunning;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the ActorClusterManager class.
    /// </summary>
    /// <param name="logger">Logger instance for this service.</param>
    public ActorClusterManager(ILogger<ActorClusterManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _clusterConfigurations = new ConcurrentDictionary<string, ClusterConfiguration>();
        _clusterStates = new ConcurrentDictionary<string, ClusterState>();
        _clusterMetrics = new ConcurrentDictionary<string, ClusterMetrics>();
        _clusterActors = new ConcurrentDictionary<string, List<string>>();
        _actorToClusterMapping = new ConcurrentDictionary<string, string>();
        _registeredActors = new ConcurrentDictionary<string, IActor>();
        _cancellationTokenSource = new CancellationTokenSource();
        
        // Start metrics collection timer (runs every 30 seconds by default)
        _metricsCollectionTimer = new Timer(CollectMetrics, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    #region IActorClusterManager Implementation

    public event EventHandler<ClusterEventArgs>? ClusterStateChanged;

    public IReadOnlyCollection<string> ActiveClusters
    {
        get
        {
            return _clusterStates
                .Where(kvp => kvp.Value == ClusterState.Active)
                .Select(kvp => kvp.Key)
                .ToList()
                .AsReadOnly();
        }
    }

    public async Task CreateClusterAsync(string clusterId, ClusterConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clusterId)) throw new ArgumentException("Cluster ID cannot be null or empty", nameof(clusterId));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        if (_clusterConfigurations.ContainsKey(clusterId))
        {
            throw new InvalidOperationException($"Cluster {clusterId} already exists");
        }

        _logger.LogInformation("Creating cluster {ClusterId} with strategy {Strategy}", clusterId, configuration.FormationStrategy);

        _clusterConfigurations[clusterId] = configuration;
        _clusterStates[clusterId] = ClusterState.Forming;
        _clusterActors[clusterId] = new List<string>();
        _clusterMetrics[clusterId] = new ClusterMetrics();

        // Simulate cluster formation time
        await Task.Delay(100, cancellationToken);

        ChangeClusterState(clusterId, ClusterState.Active);
        
        _logger.LogInformation("Successfully created cluster {ClusterId}", clusterId);
    }

    public async Task DissolveClusterAsync(string clusterId, bool graceful = true, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clusterId)) throw new ArgumentException("Cluster ID cannot be null or empty", nameof(clusterId));

        if (!_clusterConfigurations.ContainsKey(clusterId))
        {
            _logger.LogWarning("Attempted to dissolve non-existent cluster {ClusterId}", clusterId);
            return;
        }

        _logger.LogInformation("Dissolving cluster {ClusterId} (graceful: {Graceful})", clusterId, graceful);

        ChangeClusterState(clusterId, ClusterState.Dissolving);

        // Remove all actors from the cluster
        if (_clusterActors.TryGetValue(clusterId, out var actorIds))
        {
            var actorIdsCopy = actorIds.ToList();
            foreach (var actorId in actorIdsCopy)
            {
                await RemoveActorFromClusterAsync(actorId, cancellationToken);
                
                if (graceful && _registeredActors.TryGetValue(actorId, out var actor))
                {
                    try
                    {
                        await actor.StopAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error stopping actor {ActorId} during cluster dissolution", actorId);
                    }
                }
            }
        }

        // Clean up cluster data
        _clusterConfigurations.TryRemove(clusterId, out _);
        _clusterStates.TryRemove(clusterId, out _);
        _clusterMetrics.TryRemove(clusterId, out _);
        _clusterActors.TryRemove(clusterId, out _);

        ChangeClusterState(clusterId, ClusterState.Dissolved);
        
        _logger.LogInformation("Successfully dissolved cluster {ClusterId}", clusterId);
    }

    public async Task AddActorToClusterAsync(string actorId, string clusterId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(actorId)) throw new ArgumentException("Actor ID cannot be null or empty", nameof(actorId));
        if (string.IsNullOrWhiteSpace(clusterId)) throw new ArgumentException("Cluster ID cannot be null or empty", nameof(clusterId));

        if (!_clusterConfigurations.ContainsKey(clusterId))
        {
            throw new InvalidOperationException($"Cluster {clusterId} does not exist");
        }

        if (!_registeredActors.ContainsKey(actorId))
        {
            throw new InvalidOperationException($"Actor {actorId} is not registered with the cluster manager");
        }

        var config = _clusterConfigurations[clusterId];
        var currentActors = _clusterActors[clusterId];

        if (currentActors.Count >= config.MaxActorCount)
        {
            throw new InvalidOperationException($"Cluster {clusterId} has reached maximum actor count ({config.MaxActorCount})");
        }

        // Remove from previous cluster if any
        if (_actorToClusterMapping.TryGetValue(actorId, out var previousClusterId))
        {
            await RemoveActorFromClusterAsync(actorId, cancellationToken);
        }

        lock (currentActors)
        {
            currentActors.Add(actorId);
        }
        
        _actorToClusterMapping[actorId] = clusterId;

        // Notify the actor about cluster membership
        var actor = _registeredActors[actorId];
        await actor.JoinClusterAsync(clusterId, cancellationToken);

        _logger.LogInformation("Added actor {ActorId} to cluster {ClusterId}", actorId, clusterId);
    }

    public async Task RemoveActorFromClusterAsync(string actorId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(actorId)) throw new ArgumentException("Actor ID cannot be null or empty", nameof(actorId));

        if (!_actorToClusterMapping.TryRemove(actorId, out var clusterId))
        {
            _logger.LogWarning("Actor {ActorId} is not in any cluster", actorId);
            return;
        }

        if (_clusterActors.TryGetValue(clusterId, out var actorIds))
        {
            lock (actorIds)
            {
                actorIds.Remove(actorId);
            }
        }

        // Notify the actor about leaving the cluster
        if (_registeredActors.TryGetValue(actorId, out var actor))
        {
            await actor.LeaveClusterAsync(cancellationToken);
        }

        _logger.LogInformation("Removed actor {ActorId} from cluster {ClusterId}", actorId, clusterId);
    }

    public Task<IEnumerable<string>> GetClusterActorsAsync(string clusterId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clusterId)) throw new ArgumentException("Cluster ID cannot be null or empty", nameof(clusterId));

        if (_clusterActors.TryGetValue(clusterId, out var actorIds))
        {
            lock (actorIds)
            {
                return Task.FromResult<IEnumerable<string>>(actorIds.ToList());
            }
        }

        return Task.FromResult<IEnumerable<string>>(Enumerable.Empty<string>());
    }

    public Task<string?> GetActorClusterAsync(string actorId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(actorId)) throw new ArgumentException("Actor ID cannot be null or empty", nameof(actorId));

        _actorToClusterMapping.TryGetValue(actorId, out var clusterId);
        return Task.FromResult(clusterId);
    }

    public Task<ClusterMetrics> GetClusterMetricsAsync(string clusterId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clusterId)) throw new ArgumentException("Cluster ID cannot be null or empty", nameof(clusterId));

        if (_clusterMetrics.TryGetValue(clusterId, out var metrics))
        {
            return Task.FromResult(metrics);
        }

        throw new InvalidOperationException($"Cluster {clusterId} does not exist");
    }

    public async Task RebalanceClustersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting cluster rebalancing");

        var clusters = ActiveClusters.ToList();
        var totalActors = _registeredActors.Count;
        
        if (clusters.Count == 0 || totalActors == 0)
        {
            _logger.LogInformation("No clusters or actors to rebalance");
            return;
        }

        var targetActorsPerCluster = totalActors / clusters.Count;
        var remainder = totalActors % clusters.Count;

        foreach (var clusterId in clusters)
        {
            if (!_clusterConfigurations.TryGetValue(clusterId, out var config) || !config.LoadBalancingEnabled)
            {
                continue;
            }

            var currentActorCount = _clusterActors[clusterId].Count;
            var targetCount = targetActorsPerCluster + (remainder-- > 0 ? 1 : 0);
            
            // This is a simplified rebalancing - in practice, you'd consider actor types, load, etc.
            if (currentActorCount > targetCount + 1)
            {
                _logger.LogDebug("Cluster {ClusterId} has {CurrentCount} actors, target is {TargetCount}", 
                    clusterId, currentActorCount, targetCount);
                // Could implement actor migration here
                await Task.Delay(1, cancellationToken); // Placeholder for actual rebalancing work
            }
        }

        _logger.LogInformation("Completed cluster rebalancing");
    }

    public async Task RouteMessageToClusterAsync(string clusterId, ActorMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clusterId)) throw new ArgumentException("Cluster ID cannot be null or empty", nameof(clusterId));
        if (message == null) throw new ArgumentNullException(nameof(message));

        if (!_clusterActors.TryGetValue(clusterId, out var actorIds) || actorIds.Count == 0)
        {
            _logger.LogWarning("No actors available in cluster {ClusterId} for message routing", clusterId);
            return;
        }

        // Simple round-robin load balancing
        string targetActorId;
        lock (actorIds)
        {
            if (actorIds.Count == 0) return;
            
            var index = message.MessageId.GetHashCode() % actorIds.Count;
            targetActorId = actorIds[Math.Abs(index)];
        }

        if (_registeredActors.TryGetValue(targetActorId, out var targetActor))
        {
            await targetActor.SendMessageAsync(message, cancellationToken);
            _logger.LogDebug("Routed message to actor {ActorId} in cluster {ClusterId}", targetActorId, clusterId);
        }
    }

    public async Task BroadcastToClusterAsync(string clusterId, ActorMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clusterId)) throw new ArgumentException("Cluster ID cannot be null or empty", nameof(clusterId));
        if (message == null) throw new ArgumentNullException(nameof(message));

        var actorIds = await GetClusterActorsAsync(clusterId, cancellationToken);
        var broadcastTasks = new List<Task>();

        foreach (var actorId in actorIds)
        {
            if (_registeredActors.TryGetValue(actorId, out var actor))
            {
                broadcastTasks.Add(actor.SendMessageAsync(message, cancellationToken));
            }
        }

        await Task.WhenAll(broadcastTasks);
        _logger.LogInformation("Broadcasted message to {ActorCount} actors in cluster {ClusterId}", broadcastTasks.Count, clusterId);
    }

    public Task RegisterActorAsync(IActor actor, CancellationToken cancellationToken = default)
    {
        if (actor == null) throw new ArgumentNullException(nameof(actor));

        _registeredActors[actor.ActorId] = actor;
        _logger.LogInformation("Registered actor {ActorId}", actor.ActorId);
        
        return Task.CompletedTask;
    }

    public async Task UnregisterActorAsync(string actorId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(actorId)) throw new ArgumentException("Actor ID cannot be null or empty", nameof(actorId));

        // Remove from cluster if in one
        await RemoveActorFromClusterAsync(actorId, cancellationToken);
        
        _registeredActors.TryRemove(actorId, out _);
        _logger.LogInformation("Unregistered actor {ActorId}", actorId);
    }

    public Task<ClusterConfiguration?> GetClusterConfigurationAsync(string clusterId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clusterId)) throw new ArgumentException("Cluster ID cannot be null or empty", nameof(clusterId));

        _clusterConfigurations.TryGetValue(clusterId, out var configuration);
        return Task.FromResult(configuration);
    }

    public Task UpdateClusterConfigurationAsync(string clusterId, ClusterConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clusterId)) throw new ArgumentException("Cluster ID cannot be null or empty", nameof(clusterId));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        if (!_clusterConfigurations.ContainsKey(clusterId))
        {
            throw new InvalidOperationException($"Cluster {clusterId} does not exist");
        }

        _clusterConfigurations[clusterId] = configuration;
        _logger.LogInformation("Updated configuration for cluster {ClusterId}", clusterId);
        
        return Task.CompletedTask;
    }

    public Task<IDictionary<string, ClusterMetrics>> PerformHealthCheckAsync(CancellationToken cancellationToken = default)
    {
        var healthReport = new Dictionary<string, ClusterMetrics>();

        foreach (var clusterId in _clusterMetrics.Keys)
        {
            var metrics = _clusterMetrics[clusterId];
            
            // Update health score based on various factors
            var healthScore = CalculateHealthScore(clusterId, metrics);
            metrics.HealthScore = healthScore;
            metrics.LastUpdated = DateTimeOffset.UtcNow;
            
            healthReport[clusterId] = metrics;
        }

        _logger.LogInformation("Performed health check on {ClusterCount} clusters", healthReport.Count);
        return Task.FromResult<IDictionary<string, ClusterMetrics>>(healthReport);
    }

    #endregion

    #region IService Implementation

    public bool IsRunning => _isRunning && !_disposed;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ActorClusterManager));
        
        _logger.LogInformation("Initializing ActorClusterManager");
        
        // Initialize any required resources
        await Task.CompletedTask;
        
        _logger.LogInformation("Initialized ActorClusterManager");
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ActorClusterManager));
        
        _logger.LogInformation("Starting ActorClusterManager");
        
        _isRunning = true;
        
        // Start any background tasks
        await Task.CompletedTask;
        
        _logger.LogInformation("Started ActorClusterManager");
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) return;
        
        _logger.LogInformation("Stopping ActorClusterManager");
        
        _isRunning = false;
        
        // Stop all clusters gracefully
        var clusters = ActiveClusters.ToList();
        foreach (var clusterId in clusters)
        {
            try
            {
                await DissolveClusterAsync(clusterId, graceful: true, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dissolving cluster {ClusterId} during shutdown", clusterId);
            }
        }
        
        _logger.LogInformation("Stopped ActorClusterManager");
    }

    #endregion

    #region Private Methods

    private void ChangeClusterState(string clusterId, ClusterState newState)
    {
        _clusterStates[clusterId] = newState;
        ClusterStateChanged?.Invoke(this, new ClusterEventArgs(clusterId, newState));
        _logger.LogDebug("Cluster {ClusterId} state changed to {State}", clusterId, newState);
    }

    private float CalculateHealthScore(string clusterId, ClusterMetrics metrics)
    {
        // Simple health calculation based on multiple factors
        var factors = new List<float>();
        
        // Factor 1: Actor availability (0.0 to 1.0)
        var totalActors = metrics.ActiveActorCount + metrics.FailedActorCount;
        if (totalActors > 0)
        {
            factors.Add((float)metrics.ActiveActorCount / totalActors);
        }
        else
        {
            factors.Add(0.0f);
        }
        
        // Factor 2: Resource utilization (inverse - lower is better)
        factors.Add(1.0f - Math.Min(metrics.ResourceUtilization, 1.0f));
        
        // Factor 3: Processing performance (normalized)
        var targetProcessingTime = 100.0; // 100ms target
        var performanceFactor = Math.Min(1.0f, (float)(targetProcessingTime / Math.Max(metrics.AverageProcessingTime, 1.0)));
        factors.Add(performanceFactor);
        
        // Calculate average health score
        return factors.Count > 0 ? factors.Average() : 0.0f;
    }

    private void CollectMetrics(object? state)
    {
        if (_disposed || !_isRunning) return;

        try
        {
            foreach (var clusterId in _clusterMetrics.Keys.ToList())
            {
                var metrics = _clusterMetrics[clusterId];
                if (_clusterActors.TryGetValue(clusterId, out var actorIds))
                {
                    lock (actorIds)
                    {
                        metrics.ActiveActorCount = actorIds.Count;
                    }
                }
                
                // Update other metrics as needed
                metrics.LastUpdated = DateTimeOffset.UtcNow;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting cluster metrics");
        }
    }

    #endregion

    #region IAsyncDisposable Implementation

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        
        await StopAsync();
        
        _metricsCollectionTimer?.Dispose();
        _cancellationTokenSource.Dispose();
        _disposed = true;
        
        GC.SuppressFinalize(this);
    }

    #endregion
}