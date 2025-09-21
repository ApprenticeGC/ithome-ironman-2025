namespace GameConsole.Providers.Engine;

/// <summary>
/// Enumeration of provider selection algorithms.
/// </summary>
public enum SelectionAlgorithm
{
    /// <summary>
    /// Select provider based on health score and performance metrics.
    /// </summary>
    HealthBased,

    /// <summary>
    /// Round-robin selection among available providers.
    /// </summary>
    RoundRobin,

    /// <summary>
    /// Weighted selection based on provider priorities.
    /// </summary>
    Weighted,

    /// <summary>
    /// Random selection among available providers.
    /// </summary>
    Random,

    /// <summary>
    /// Select provider with lowest current load.
    /// </summary>
    LeastLoad
}

/// <summary>
/// Criteria for provider selection.
/// </summary>
public class ProviderSelectionCriteria
{
    /// <summary>
    /// Required capabilities that the provider must support.
    /// </summary>
    public IReadOnlySet<string>? RequiredCapabilities { get; init; }

    /// <summary>
    /// Minimum health score threshold (0-100).
    /// </summary>
    public double MinHealthScore { get; init; } = 50.0;

    /// <summary>
    /// Maximum acceptable response time in milliseconds.
    /// </summary>
    public TimeSpan? MaxResponseTime { get; init; }

    /// <summary>
    /// Selection algorithm to use.
    /// </summary>
    public SelectionAlgorithm Algorithm { get; init; } = SelectionAlgorithm.HealthBased;

    /// <summary>
    /// Whether to exclude providers that are currently failing.
    /// </summary>
    public bool ExcludeUnhealthy { get; init; } = true;
}

/// <summary>
/// Interface for selecting providers based on various criteria and algorithms.
/// </summary>
public interface IProviderSelector
{
    /// <summary>
    /// Selects the best provider based on the specified criteria.
    /// </summary>
    /// <param name="serviceType">The service type to get a provider for.</param>
    /// <param name="criteria">Optional selection criteria.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The selected provider instance, or null if no suitable provider is available.</returns>
    Task<object?> SelectProviderAsync(Type serviceType, ProviderSelectionCriteria? criteria = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects the best provider based on the specified criteria.
    /// </summary>
    /// <typeparam name="TService">The service type to get a provider for.</typeparam>
    /// <param name="criteria">Optional selection criteria.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The selected provider instance, or null if no suitable provider is available.</returns>
    Task<TService?> SelectProviderAsync<TService>(ProviderSelectionCriteria? criteria = null, CancellationToken cancellationToken = default)
        where TService : class;

    /// <summary>
    /// Gets all available providers for the specified service type that match the criteria.
    /// </summary>
    /// <param name="serviceType">The service type to get providers for.</param>
    /// <param name="criteria">Optional selection criteria.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>List of matching providers ordered by selection preference.</returns>
    Task<IReadOnlyList<object>> GetAvailableProvidersAsync(Type serviceType, ProviderSelectionCriteria? criteria = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available providers for the specified service type that match the criteria.
    /// </summary>
    /// <typeparam name="TService">The service type to get providers for.</typeparam>
    /// <param name="criteria">Optional selection criteria.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>List of matching providers ordered by selection preference.</returns>
    Task<IReadOnlyList<TService>> GetAvailableProvidersAsync<TService>(ProviderSelectionCriteria? criteria = null, CancellationToken cancellationToken = default)
        where TService : class;
}