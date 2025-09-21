namespace GameConsole.Providers.Engine;

/// <summary>
/// Interface for load balancing provider that distributes requests across multiple providers.
/// </summary>
public interface ILoadBalancingProvider
{
    /// <summary>
    /// Executes an operation across multiple providers with load balancing.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="criteria">Provider selection criteria.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The result of the operation.</returns>
    Task<TResult> ExecuteAsync<TService, TResult>(
        Func<TService, CancellationToken, Task<TResult>> operation,
        ProviderSelectionCriteria? criteria = null,
        CancellationToken cancellationToken = default)
        where TService : class;

    /// <summary>
    /// Executes an operation across multiple providers with load balancing.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="criteria">Provider selection criteria.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    Task ExecuteAsync<TService>(
        Func<TService, CancellationToken, Task> operation,
        ProviderSelectionCriteria? criteria = null,
        CancellationToken cancellationToken = default)
        where TService : class;

    /// <summary>
    /// Gets the current load distribution across providers.
    /// </summary>
    /// <param name="serviceType">The service type to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Dictionary of provider load metrics keyed by provider ID.</returns>
    Task<IReadOnlyDictionary<string, double>> GetLoadDistributionAsync(Type serviceType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current load distribution across providers.
    /// </summary>
    /// <typeparam name="TService">The service type to check.</typeparam>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Dictionary of provider load metrics keyed by provider ID.</returns>
    Task<IReadOnlyDictionary<string, double>> GetLoadDistributionAsync<TService>(CancellationToken cancellationToken = default)
        where TService : class;
}