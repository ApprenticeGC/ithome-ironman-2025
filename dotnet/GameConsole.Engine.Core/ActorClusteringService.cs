using System.Collections.Concurrent;
using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace GameConsole.Engine.Core;

/// <summary>
/// Tier 3: Implementation of the actor clustering service that manages multiple
/// actor clusters and provides batch processing capabilities for AI agents.
/// </summary>
public class ActorClusteringService : IActorClusteringService
{
    private readonly ConcurrentDictionary<string, IActorCluster> _clusters = new();
    private readonly ILogger<ActorClusteringService>? _logger;
    private bool _isRunning = false;

    /// <summary>
    /// Gets a value indicating whether the service is currently running.
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Initializes a new instance of the ActorClusteringService class.
    /// </summary>
    /// <param name="logger">Optional logger for service operations.</param>
    public ActorClusteringService(ILogger<ActorClusteringService>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initializes the service asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async initialization operation.</returns>
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Initializing ActorClusteringService");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Starts the service asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async start operation.</returns>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Starting ActorClusteringService");
        _isRunning = true;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the service asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async stop operation.</returns>
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Stopping ActorClusteringService");
        _isRunning = false;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Disposes of the service resources asynchronously.
    /// </summary>
    /// <returns>A task representing the async disposal operation.</returns>
    public async ValueTask DisposeAsync()
    {
        _logger?.LogInformation("Disposing ActorClusteringService");
        await StopAsync();
        _clusters.Clear();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Gets all available capabilities provided by this service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns a collection of capability types.</returns>
    public Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var capabilities = new[]
        {
            typeof(IActorClusteringService),
            typeof(IActorCluster)
        };

        return Task.FromResult<IEnumerable<Type>>(capabilities);
    }

    /// <summary>
    /// Checks if the service provides a specific capability.
    /// </summary>
    /// <typeparam name="T">The type of capability to check for.</typeparam>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns true if the capability is available.</returns>
    public Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        var hasCapability = typeof(T) == typeof(IActorClusteringService) || 
                           typeof(T) == typeof(IActorCluster);
        
        return Task.FromResult(hasCapability);
    }

    /// <summary>
    /// Gets a specific capability instance from the service.
    /// </summary>
    /// <typeparam name="T">The type of capability to retrieve.</typeparam>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the capability instance, or null if not available.</returns>
    public Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (typeof(T) == typeof(IActorClusteringService))
        {
            return Task.FromResult(this as T);
        }

        return Task.FromResult<T?>(null);
    }

    /// <summary>
    /// Creates a new actor cluster with the specified identifier.
    /// </summary>
    /// <param name="clusterId">The unique identifier for the new cluster.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the created cluster, or null if creation failed.</returns>
    public Task<IActorCluster?> CreateClusterAsync(string clusterId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(clusterId))
        {
            _logger?.LogWarning("Attempted to create cluster with null or empty ID");
            return Task.FromResult<IActorCluster?>(null);
        }

        var cluster = new ActorCluster(clusterId);
        var added = _clusters.TryAdd(clusterId, cluster);

        if (added)
        {
            _logger?.LogDebug("Created cluster: {ClusterId}", clusterId);
            return Task.FromResult<IActorCluster?>(cluster);
        }

        _logger?.LogWarning("Failed to create cluster {ClusterId} - already exists", clusterId);
        return Task.FromResult<IActorCluster?>(null);
    }

    /// <summary>
    /// Destroys an existing actor cluster and removes all its actors.
    /// </summary>
    /// <param name="clusterId">The identifier of the cluster to destroy.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns true if the cluster was found and destroyed.</returns>
    public Task<bool> DestroyClusterAsync(string clusterId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(clusterId))
            return Task.FromResult(false);

        var removed = _clusters.TryRemove(clusterId, out _);
        
        if (removed)
        {
            _logger?.LogDebug("Destroyed cluster: {ClusterId}", clusterId);
        }
        else
        {
            _logger?.LogWarning("Failed to destroy cluster {ClusterId} - not found", clusterId);
        }

        return Task.FromResult(removed);
    }

    /// <summary>
    /// Gets an existing cluster by its identifier.
    /// </summary>
    /// <param name="clusterId">The identifier of the cluster to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the cluster, or null if not found.</returns>
    public Task<IActorCluster?> GetClusterAsync(string clusterId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(clusterId))
            return Task.FromResult<IActorCluster?>(null);

        _clusters.TryGetValue(clusterId, out var cluster);
        return Task.FromResult(cluster);
    }

    /// <summary>
    /// Gets all currently managed clusters.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns all clusters.</returns>
    public Task<IEnumerable<IActorCluster>> GetAllClustersAsync(CancellationToken cancellationToken = default)
    {
        var clusters = _clusters.Values.ToList();
        return Task.FromResult<IEnumerable<IActorCluster>>(clusters);
    }

    /// <summary>
    /// Updates all clusters and their actors. This is the main processing loop
    /// that should be called each frame to update cluster state.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update, in seconds.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async batch update operation.</returns>
    public async Task UpdateAllClustersAsync(float deltaTime, CancellationToken cancellationToken = default)
    {
        if (!_isRunning || _clusters.IsEmpty)
            return;

        var activeClusters = _clusters.Values.Where(c => c.IsActive).ToList();
        
        if (!activeClusters.Any())
            return;

        _logger?.LogTrace("Updating {ClusterCount} active clusters", activeClusters.Count);

        // Update all clusters in parallel for better performance
        var updateTasks = activeClusters
            .Select(cluster => cluster.UpdateClusterAsync(deltaTime, cancellationToken));

        try
        {
            await Task.WhenAll(updateTasks);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating clusters");
        }
    }

    /// <summary>
    /// Gets the total number of actors across all clusters.
    /// Useful for performance monitoring and resource management.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the total actor count.</returns>
    public Task<int> GetTotalActorCountAsync(CancellationToken cancellationToken = default)
    {
        var totalCount = _clusters.Values.Sum(cluster => cluster.Actors.Count);
        return Task.FromResult(totalCount);
    }
}