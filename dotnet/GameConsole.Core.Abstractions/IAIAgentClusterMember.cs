using GameConsole.Core.Abstractions;

namespace GameConsole.Core.Abstractions;

/// <summary>
/// Interface for AI agents that can participate in a distributed cluster.
/// Provides cluster-aware behavior and coordination capabilities.
/// </summary>
public interface IAIAgentClusterMember : ICapabilityProvider
{
    /// <summary>
    /// Gets the unique identifier for this AI agent.
    /// </summary>
    string AgentId { get; }

    /// <summary>
    /// Gets the roles this AI agent can perform in the cluster.
    /// </summary>
    ISet<string> Roles { get; }

    /// <summary>
    /// Gets the current cluster address of this AI agent.
    /// </summary>
    string? ClusterAddress { get; }

    /// <summary>
    /// Registers this AI agent with the cluster for distributed coordination.
    /// </summary>
    /// <param name="clusterService">The cluster service to register with.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the registration operation.</returns>
    Task RegisterWithClusterAsync(IClusterService clusterService, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers other AI agents in the cluster with specified roles.
    /// </summary>
    /// <param name="requiredRoles">The roles to search for.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Collection of discovered AI agents.</returns>
    Task<IEnumerable<AIAgentInfo>> DiscoverAgentsAsync(ISet<string> requiredRoles, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a coordination message to another AI agent in the cluster.
    /// </summary>
    /// <param name="targetAgentId">The target agent identifier.</param>
    /// <param name="message">The coordination message to send.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the send operation.</returns>
    Task SendCoordinationMessageAsync(string targetAgentId, AIAgentMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when a coordination message is received from another AI agent.
    /// </summary>
    event EventHandler<AIAgentMessageReceivedEventArgs>? MessageReceived;

    /// <summary>
    /// Event raised when this AI agent discovers a new agent in the cluster.
    /// </summary>
    event EventHandler<AIAgentDiscoveredEventArgs>? AgentDiscovered;

    /// <summary>
    /// Event raised when an AI agent leaves the cluster.
    /// </summary>
    event EventHandler<AIAgentLeftEventArgs>? AgentLeft;
}

/// <summary>
/// Information about an AI agent in the cluster.
/// </summary>
public class AIAgentInfo
{
    public AIAgentInfo(string agentId, string clusterAddress, ISet<string> roles, IDictionary<string, object> metadata)
    {
        AgentId = agentId ?? throw new ArgumentNullException(nameof(agentId));
        ClusterAddress = clusterAddress ?? throw new ArgumentNullException(nameof(clusterAddress));
        Roles = roles ?? throw new ArgumentNullException(nameof(roles));
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
    }

    public string AgentId { get; }
    public string ClusterAddress { get; }
    public ISet<string> Roles { get; }
    public IDictionary<string, object> Metadata { get; }
}

/// <summary>
/// Message sent between AI agents for coordination.
/// </summary>
public class AIAgentMessage
{
    public AIAgentMessage(string messageType, string sourceAgentId, IDictionary<string, object> payload, DateTime timestamp, string correlationId)
    {
        MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
        SourceAgentId = sourceAgentId ?? throw new ArgumentNullException(nameof(sourceAgentId));
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        Timestamp = timestamp;
        CorrelationId = correlationId ?? throw new ArgumentNullException(nameof(correlationId));
    }

    public string MessageType { get; }
    public string SourceAgentId { get; }
    public IDictionary<string, object> Payload { get; }
    public DateTime Timestamp { get; }
    public string CorrelationId { get; }
}

/// <summary>
/// Event arguments for AI agent message received events.
/// </summary>
public class AIAgentMessageReceivedEventArgs
{
    public AIAgentMessageReceivedEventArgs(AIAgentMessage message)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
    }

    public AIAgentMessage Message { get; }
}

/// <summary>
/// Event arguments for AI agent discovered events.
/// </summary>
public class AIAgentDiscoveredEventArgs
{
    public AIAgentDiscoveredEventArgs(AIAgentInfo agent)
    {
        Agent = agent ?? throw new ArgumentNullException(nameof(agent));
    }

    public AIAgentInfo Agent { get; }
}

/// <summary>
/// Event arguments for AI agent left events.
/// </summary>
public class AIAgentLeftEventArgs
{
    public AIAgentLeftEventArgs(AIAgentInfo agent)
    {
        Agent = agent ?? throw new ArgumentNullException(nameof(agent));
    }

    public AIAgentInfo Agent { get; }
}