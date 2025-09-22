using Akka.Actor;

namespace GameConsole.AI.Actors.Core.Messages;

/// <summary>
/// Base marker interface for all AI-related messages in the actor system.
/// </summary>
public abstract record AIMessage;

#region Agent Management Messages

/// <summary>
/// Message to start an AI agent with the specified configuration.
/// </summary>
/// <param name="AgentType">The type of AI agent to start (e.g., "dialogue", "analysis", "codegen").</param>
/// <param name="Config">Configuration parameters for the agent.</param>
public record StartAgent(string AgentType, AgentConfig Config) : AIMessage;

/// <summary>
/// Message to stop a specific AI agent.
/// </summary>
/// <param name="AgentId">The unique identifier of the agent to stop.</param>
public record StopAgent(string AgentId) : AIMessage;

/// <summary>
/// Notification that an AI agent has been successfully started.
/// </summary>
/// <param name="AgentId">The unique identifier of the started agent.</param>
/// <param name="ActorRef">Reference to the started agent actor.</param>
public record AgentStarted(string AgentId, IActorRef ActorRef) : AIMessage;

/// <summary>
/// Notification that an AI agent has been stopped.
/// </summary>
/// <param name="AgentId">The unique identifier of the stopped agent.</param>
/// <param name="Reason">The reason for stopping the agent.</param>
public record AgentStopped(string AgentId, string Reason) : AIMessage;

#endregion

#region Processing Messages

/// <summary>
/// Message to request processing from an AI agent.
/// </summary>
/// <param name="RequestId">Unique identifier for the request.</param>
/// <param name="AgentType">Type of agent to handle the request.</param>
/// <param name="Request">The actual request payload.</param>
/// <param name="Sender">Reference to the requesting actor.</param>
public record ProcessRequest(
    string RequestId,
    string AgentType,
    object Request,
    IActorRef Sender) : AIMessage;

/// <summary>
/// Message containing the response from an AI agent.
/// </summary>
/// <param name="RequestId">Unique identifier matching the original request.</param>
/// <param name="Response">The response payload from the agent.</param>
/// <param name="ProcessingTime">Time taken to process the request.</param>
public record ProcessResponse(
    string RequestId,
    object Response,
    TimeSpan ProcessingTime) : AIMessage;

/// <summary>
/// Message indicating that processing has failed.
/// </summary>
/// <param name="RequestId">Unique identifier matching the original request.</param>
/// <param name="Exception">The exception that caused the failure.</param>
/// <param name="AgentId">The identifier of the agent that failed to process.</param>
public record ProcessFailed(
    string RequestId,
    Exception Exception,
    string AgentId) : AIMessage;

#endregion

#region Backend Management Messages

/// <summary>
/// Message to check the health of an AI backend service.
/// </summary>
/// <param name="BackendName">Name of the backend service to check.</param>
public record BackendHealthCheck(string BackendName) : AIMessage;

/// <summary>
/// Response indicating the health status of a backend service.
/// </summary>
/// <param name="BackendName">Name of the backend service.</param>
/// <param name="IsHealthy">Whether the backend is healthy.</param>
/// <param name="ResponseTime">Response time of the health check.</param>
/// <param name="ErrorMessage">Error message if unhealthy.</param>
public record BackendHealthResponse(
    string BackendName, 
    bool IsHealthy, 
    TimeSpan ResponseTime, 
    string? ErrorMessage = null) : AIMessage;

#endregion

#region Clustering Messages

/// <summary>
/// Message to request cluster node information.
/// </summary>
public record GetClusterState : AIMessage;

/// <summary>
/// Response containing current cluster state information.
/// </summary>
/// <param name="Nodes">List of nodes in the cluster.</param>
/// <param name="Leader">The current cluster leader node.</param>
/// <param name="Unreachable">List of unreachable nodes.</param>
public record ClusterStateResponse(
    IReadOnlyList<string> Nodes,
    string? Leader,
    IReadOnlyList<string> Unreachable) : AIMessage;

/// <summary>
/// Message to redistribute agents across cluster nodes.
/// </summary>
public record RebalanceAgents : AIMessage;

/// <summary>
/// Notification that agent rebalancing has completed.
/// </summary>
/// <param name="MovedAgents">Number of agents that were moved.</param>
/// <param name="Duration">Time taken to complete rebalancing.</param>
public record RebalanceCompleted(int MovedAgents, TimeSpan Duration) : AIMessage;

#endregion