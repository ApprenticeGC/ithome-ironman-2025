using GameConsole.Core.Abstractions;

namespace GameConsole.Providers.Engine;

/// <summary>
/// Interface for load balancing providers that distribute requests across multiple provider instances.
/// </summary>
/// <typeparam name="T">The type of provider being load balanced.</typeparam>
public interface ILoadBalancingProvider<T> : IService where T : class
{
    /// <summary>
    /// Adds a provider to the load balancing pool.
    /// </summary>
    /// <param name="provider">The provider to add.</param>
    /// <param name="weight">Optional weight for weighted load balancing (default is 1).</param>
    void AddProvider(T provider, int weight = 1);

    /// <summary>
    /// Removes a provider from the load balancing pool.
    /// </summary>
    /// <param name="provider">The provider to remove.</param>
    /// <returns>True if the provider was removed, false if it wasn't found.</returns>
    bool RemoveProvider(T provider);

    /// <summary>
    /// Gets the next provider according to the load balancing algorithm.
    /// </summary>
    /// <param name="context">Optional context for provider selection.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The selected provider, or null if no providers are available.</returns>
    Task<T?> GetNextProviderAsync(object? context = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an operation using load balancing with automatic fallback.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the operation.</typeparam>
    /// <param name="operation">The operation to execute against a provider.</param>
    /// <param name="context">Optional context for provider selection.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The result of the operation.</returns>
    Task<TResult> ExecuteAsync<TResult>(
        Func<T, CancellationToken, Task<TResult>> operation,
        object? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an operation using load balancing with automatic fallback.
    /// </summary>
    /// <param name="operation">The operation to execute against a provider.</param>
    /// <param name="context">Optional context for provider selection.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ExecuteAsync(
        Func<T, CancellationToken, Task> operation,
        object? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all currently available providers in the load balancing pool.
    /// </summary>
    /// <returns>Collection of available providers with their weights.</returns>
    IEnumerable<ProviderInfo<T>> GetAvailableProviders();

    /// <summary>
    /// Gets the current load balancing algorithm being used.
    /// </summary>
    LoadBalancingAlgorithm Algorithm { get; }

    /// <summary>
    /// Gets performance statistics for the load balancing pool.
    /// </summary>
    LoadBalancingStatistics Statistics { get; }

    /// <summary>
    /// Event raised when a provider is added to the pool.
    /// </summary>
    event EventHandler<ProviderAddedEventArgs<T>>? ProviderAdded;

    /// <summary>
    /// Event raised when a provider is removed from the pool.
    /// </summary>
    event EventHandler<ProviderRemovedEventArgs<T>>? ProviderRemoved;
}

/// <summary>
/// Information about a provider in the load balancing pool.
/// </summary>
/// <typeparam name="T">The type of provider.</typeparam>
public sealed class ProviderInfo<T> where T : class
{
    /// <summary>
    /// Gets the provider instance.
    /// </summary>
    public required T Provider { get; init; }

    /// <summary>
    /// Gets the weight assigned to the provider for load balancing.
    /// </summary>
    public int Weight { get; init; } = 1;

    /// <summary>
    /// Gets whether the provider is currently healthy and available.
    /// </summary>
    public bool IsHealthy { get; init; } = true;

    /// <summary>
    /// Gets the number of active operations currently using this provider.
    /// </summary>
    public int ActiveOperations { get; init; }

    /// <summary>
    /// Gets when this provider was added to the pool.
    /// </summary>
    public DateTimeOffset AddedTime { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Load balancing algorithms supported by the load balancing provider.
/// </summary>
public enum LoadBalancingAlgorithm
{
    /// <summary>
    /// Round-robin: Cycles through providers in order.
    /// </summary>
    RoundRobin,

    /// <summary>
    /// Weighted round-robin: Cycles through providers based on their assigned weights.
    /// </summary>
    WeightedRoundRobin,

    /// <summary>
    /// Least connections: Selects the provider with the fewest active operations.
    /// </summary>
    LeastConnections,

    /// <summary>
    /// Performance-based: Selects providers based on their performance metrics.
    /// </summary>
    PerformanceBased,

    /// <summary>
    /// Random: Selects providers randomly.
    /// </summary>
    Random,

    /// <summary>
    /// Weighted random: Selects providers randomly based on their assigned weights.
    /// </summary>
    WeightedRandom
}

/// <summary>
/// Statistics for load balancing operations.
/// </summary>
public sealed class LoadBalancingStatistics
{
    /// <summary>
    /// Gets the total number of requests processed.
    /// </summary>
    public long TotalRequests { get; init; }

    /// <summary>
    /// Gets the number of successful requests.
    /// </summary>
    public long SuccessfulRequests { get; init; }

    /// <summary>
    /// Gets the number of failed requests.
    /// </summary>
    public long FailedRequests { get; init; }

    /// <summary>
    /// Gets the average response time across all providers.
    /// </summary>
    public TimeSpan AverageResponseTime { get; init; }

    /// <summary>
    /// Gets the number of fallback attempts made.
    /// </summary>
    public long FallbackAttempts { get; init; }

    /// <summary>
    /// Gets the distribution of requests across providers.
    /// </summary>
    public IReadOnlyDictionary<string, long> RequestDistribution { get; init; } = new Dictionary<string, long>();
}

/// <summary>
/// Event arguments for when a provider is added to the load balancing pool.
/// </summary>
/// <typeparam name="T">The type of provider.</typeparam>
public sealed class ProviderAddedEventArgs<T> : EventArgs where T : class
{
    /// <summary>
    /// Gets the provider that was added.
    /// </summary>
    public required T Provider { get; init; }

    /// <summary>
    /// Gets the weight assigned to the provider.
    /// </summary>
    public int Weight { get; init; }

    /// <summary>
    /// Gets when the provider was added.
    /// </summary>
    public DateTimeOffset AddedTime { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Event arguments for when a provider is removed from the load balancing pool.
/// </summary>
/// <typeparam name="T">The type of provider.</typeparam>
public sealed class ProviderRemovedEventArgs<T> : EventArgs where T : class
{
    /// <summary>
    /// Gets the provider that was removed.
    /// </summary>
    public required T Provider { get; init; }

    /// <summary>
    /// Gets when the provider was removed.
    /// </summary>
    public DateTimeOffset RemovedTime { get; init; } = DateTimeOffset.UtcNow;
}