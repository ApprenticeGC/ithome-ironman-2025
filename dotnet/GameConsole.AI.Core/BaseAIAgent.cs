using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Core;

/// <summary>
/// Abstract base class for AI agents that provides common functionality and lifecycle management.
/// Implements IService and ICapabilityProvider interfaces with default behavior that can be overridden.
/// </summary>
public abstract class BaseAIAgent : IAIAgent
{
    private volatile bool _isRunning = false;

    /// <summary>
    /// Gets the unique identifier for this AI agent.
    /// </summary>
    public abstract string AgentId { get; }

    /// <summary>
    /// Gets the display name of the AI agent.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets the version of the AI agent.
    /// </summary>
    public abstract string Version { get; }

    /// <summary>
    /// Gets a description of what this AI agent does.
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// Gets the priority of this agent when multiple agents can handle the same capability.
    /// Higher values indicate higher priority. Default is 50.
    /// </summary>
    public virtual int Priority => 50;

    /// <summary>
    /// Gets a value indicating whether the service is currently running.
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Initializes the AI agent asynchronously.
    /// Override this method to provide custom initialization logic.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async initialization operation.</returns>
    public virtual Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Starts the AI agent asynchronously.
    /// Override this method to provide custom start logic.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async start operation.</returns>
    public virtual Task StartAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = true;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the AI agent asynchronously.
    /// Override this method to provide custom stop logic.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async stop operation.</returns>
    public virtual Task StopAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = false;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets all available capabilities provided by this AI agent.
    /// Override this method to provide the actual capabilities of the agent.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns a collection of capability types.</returns>
    public abstract Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the AI agent provides a specific capability.
    /// Default implementation checks against the capabilities returned by GetCapabilitiesAsync.
    /// Override for custom capability checking logic.
    /// </summary>
    /// <typeparam name="T">The type of capability to check for.</typeparam>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns true if the capability is available.</returns>
    public virtual async Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        var capabilities = await GetCapabilitiesAsync(cancellationToken);
        return capabilities.Contains(typeof(T)) || typeof(T).IsAssignableFrom(GetType());
    }

    /// <summary>
    /// Gets a specific capability instance from the AI agent.
    /// Default implementation returns the agent itself if it implements the capability interface.
    /// Override for custom capability provisioning logic.
    /// </summary>
    /// <typeparam name="T">The type of capability to retrieve.</typeparam>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the capability instance, or null if not available.</returns>
    public virtual Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        return Task.FromResult(this as T);
    }

    /// <summary>
    /// Processes a request using the AI agent's capabilities.
    /// Override this method to provide the actual request processing logic.
    /// </summary>
    /// <typeparam name="TRequest">The type of request to process.</typeparam>
    /// <typeparam name="TResponse">The type of response to return.</typeparam>
    /// <param name="request">The request to process.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the response.</returns>
    public abstract Task<TResponse> ProcessAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class;

    /// <summary>
    /// Checks if the AI agent can handle a specific request type.
    /// Override this method to provide custom request handling checks.
    /// </summary>
    /// <typeparam name="TRequest">The type of request to check.</typeparam>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns true if the agent can handle the request type.</returns>
    public abstract Task<bool> CanHandleAsync<TRequest>(CancellationToken cancellationToken = default)
        where TRequest : class;

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
    /// Override this method to provide custom disposal logic.
    /// </summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
    public virtual async ValueTask DisposeAsync()
    {
        if (_isRunning)
        {
            await StopAsync();
        }

        GC.SuppressFinalize(this);
    }
}