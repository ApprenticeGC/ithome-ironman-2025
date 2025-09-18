namespace GameConsole.Core.Abstractions;

/// <summary>
/// Interface for services that can provide capabilities to other services in the system.
/// Enables service discovery and capability-based service selection.
/// </summary>
public interface ICapabilityProvider
{
    /// <summary>
    /// Gets all available capabilities provided by this service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns a collection of capability types.</returns>
    Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the service provides a specific capability.
    /// </summary>
    /// <typeparam name="T">The type of capability to check for.</typeparam>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns true if the capability is available.</returns>
    Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific capability instance from the service.
    /// </summary>
    /// <typeparam name="T">The type of capability to retrieve.</typeparam>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the capability instance, or null if not available.</returns>
    Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class;
}
