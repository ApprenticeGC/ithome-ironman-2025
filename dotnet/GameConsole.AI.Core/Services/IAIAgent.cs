using GameConsole.Core.Abstractions;
using GameConsole.AI.Models;

namespace GameConsole.AI.Services;

/// <summary>
/// Defines the core interface for AI agents in the GameConsole framework.
/// Combines service lifecycle management with capability discovery for AI-specific functionality.
/// </summary>
public interface IAIAgent : IService, ICapabilityProvider
{
    /// <summary>
    /// Gets the metadata information for this AI agent.
    /// </summary>
    AIAgentMetadata Metadata { get; }

    /// <summary>
    /// Gets the current execution context for this AI agent.
    /// </summary>
    IAIContext? Context { get; }

    /// <summary>
    /// Configures the AI agent with the specified execution context.
    /// This method should be called after InitializeAsync but before StartAsync.
    /// </summary>
    /// <param name="context">The execution context for the agent.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async configuration operation.</returns>
    Task ConfigureAsync(IAIContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes the AI agent with the specified input and returns a response.
    /// </summary>
    /// <param name="input">The input data for the agent.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the agent response.</returns>
    Task<string> InvokeAsync(string input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes the AI agent with streaming response capability.
    /// </summary>
    /// <param name="input">The input data for the agent.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>An async enumerable of response chunks.</returns>
    IAsyncEnumerable<string> StreamAsync(string input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current performance metrics for this AI agent.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns performance metrics.</returns>
    Task<AIPerformanceMetrics> GetPerformanceMetricsAsync(CancellationToken cancellationToken = default);
}