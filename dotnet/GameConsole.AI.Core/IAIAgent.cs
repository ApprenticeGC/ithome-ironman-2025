using System;
using System.Threading;
using System.Threading.Tasks;
using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Core
{

/// <summary>
/// Defines the base interface for all AI agents in the GameConsole system.
/// Extends the base service interface with AI-specific capabilities and agent lifecycle management.
/// AI agents are specialized services that can perform intelligent tasks and provide AI capabilities.
/// </summary>
public interface IAIAgent : IService, ICapabilityProvider
{
    /// <summary>
    /// Gets the metadata information for this AI agent.
    /// This includes identity, version, capabilities, and other agent-specific information.
    /// </summary>
    IAIAgentMetadata Metadata { get; }

    /// <summary>
    /// Gets the current operational status of the AI agent.
    /// </summary>
    AIAgentStatus Status { get; }

    /// <summary>
    /// Gets a value indicating whether the agent is available to handle requests.
    /// An agent may be running but not available due to capacity constraints or maintenance.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Processes a request asynchronously and returns a response.
    /// This is the primary method for interacting with the AI agent.
    /// </summary>
    /// <param name="request">The request to process.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the agent's response.</returns>
    Task<IAIAgentResponse> ProcessAsync(IAIAgentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the agent can handle a specific type of request.
    /// </summary>
    /// <param name="requestType">The type of request to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns true if the agent can handle the request type.</returns>
    Task<bool> CanHandleAsync(Type requestType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the estimated time to process a request of the given type.
    /// This can be used for load balancing and request routing decisions.
    /// </summary>
    /// <param name="requestType">The type of request to estimate processing time for.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the estimated processing time, or null if estimation is not available.</returns>
    Task<TimeSpan?> GetEstimatedProcessingTimeAsync(Type requestType, CancellationToken cancellationToken = default);
}
}