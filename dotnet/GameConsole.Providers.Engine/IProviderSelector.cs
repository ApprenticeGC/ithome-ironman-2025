using GameConsole.Core.Abstractions;

namespace GameConsole.Providers.Engine;

/// <summary>
/// Interface for selecting the best provider from a collection based on performance metrics and selection criteria.
/// </summary>
/// <typeparam name="T">The type of provider to select.</typeparam>
public interface IProviderSelector<T> where T : class
{
    /// <summary>
    /// Selects the best provider from the available providers based on current performance metrics.
    /// </summary>
    /// <param name="providers">The available providers to select from.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The selected provider, or null if no suitable provider is available.</returns>
    Task<T?> SelectProviderAsync(IEnumerable<T> providers, CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects the best provider from the available providers based on selection criteria and context.
    /// </summary>
    /// <param name="providers">The available providers to select from.</param>
    /// <param name="context">Optional context for provider selection (e.g., request type, user preferences).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The selected provider, or null if no suitable provider is available.</returns>
    Task<T?> SelectProviderAsync(IEnumerable<T> providers, object? context = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects multiple providers for load balancing based on the specified count and criteria.
    /// </summary>
    /// <param name="providers">The available providers to select from.</param>
    /// <param name="count">The number of providers to select.</param>
    /// <param name="context">Optional context for provider selection.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The selected providers for load balancing.</returns>
    Task<IEnumerable<T>> SelectProvidersAsync(IEnumerable<T> providers, int count, object? context = null, CancellationToken cancellationToken = default);
}