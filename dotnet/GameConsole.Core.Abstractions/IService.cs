namespace GameConsole.Core.Abstractions;

/// <summary>
/// Base interface for all services in the GameConsole 4-tier architecture.
/// Defines the fundamental lifecycle operations that all services must support.
/// </summary>
public interface IService : IAsyncDisposable
{
    /// <summary>
    /// Initializes the service asynchronously.
    /// This method is called once during service setup before the service is started.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async initialization operation.</returns>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts the service asynchronously.
    /// This method is called to begin service operations after initialization.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async start operation.</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the service asynchronously.
    /// This method is called to gracefully shut down service operations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async stop operation.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value indicating whether the service is currently running.
    /// </summary>
    bool IsRunning { get; }
}
