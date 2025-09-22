using Akka.Actor;
using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Cluster;

/// <summary>
/// Base implementation for AI agents that can participate in a distributed cluster.
/// Provides common cluster-aware behavior and coordination capabilities.
/// </summary>
public abstract class AIAgentClusterMemberBase : ReceiveActor, IAIAgentClusterMember
{
    protected readonly ILogger Logger;
    private readonly Dictionary<string, AIAgentInfo> _discoveredAgents = new();
    private IClusterService? _clusterService;

    /// <summary>
    /// Initializes a new instance of the AIAgentClusterMemberBase class.
    /// </summary>
    /// <param name="logger">Logger for the AI agent.</param>
    /// <param name="agentId">Unique identifier for this AI agent.</param>
    /// <param name="roles">Roles this AI agent can perform.</param>
    protected AIAgentClusterMemberBase(ILogger logger, string agentId, ISet<string> roles)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        AgentId = agentId ?? throw new ArgumentNullException(nameof(agentId));
        Roles = roles ?? throw new ArgumentNullException(nameof(roles));

        // Set up message handlers
        Receive<AIAgentMessage>(HandleMessage);
        Receive<AgentDiscoveryMessage>(HandleAgentDiscovery);
    }

    /// <inheritdoc />
    public string AgentId { get; }

    /// <inheritdoc />
    public ISet<string> Roles { get; }

    /// <inheritdoc />
    public string? ClusterAddress => Context.Self.Path.ToStringWithAddress();

    /// <inheritdoc />
    public event EventHandler<AIAgentMessageReceivedEventArgs>? MessageReceived;

    /// <inheritdoc />
    public event EventHandler<AIAgentDiscoveredEventArgs>? AgentDiscovered;

    /// <inheritdoc />
    public event EventHandler<AIAgentLeftEventArgs>? AgentLeft;

    /// <inheritdoc />
    public async Task RegisterWithClusterAsync(IClusterService clusterService, CancellationToken cancellationToken = default)
    {
        _clusterService = clusterService ?? throw new ArgumentNullException(nameof(clusterService));

        Logger.LogInformation("Registering AI agent {AgentId} with cluster", AgentId);

        // Subscribe to cluster events
        _clusterService.MemberJoined += OnClusterMemberJoined;
        _clusterService.MemberLeft += OnClusterMemberLeft;

        // Announce our presence to existing cluster members
        await AnnounceSelfToClusterAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AIAgentInfo>> DiscoverAgentsAsync(ISet<string> requiredRoles, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Discovering AI agents with roles: {Roles}", string.Join(", ", requiredRoles));

        // Send discovery message to all cluster members
        var discoveryMessage = new AgentDiscoveryMessage(AgentId, requiredRoles);
        Context.System.EventStream.Publish(discoveryMessage);

        // Return currently known agents that match the roles
        return _discoveredAgents.Values.Where(agent => 
            requiredRoles.All(role => agent.Roles.Contains(role)));
    }

    /// <inheritdoc />
    public async Task SendCoordinationMessageAsync(string targetAgentId, AIAgentMessage message, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Sending coordination message to agent {TargetAgentId}", targetAgentId);

        if (_discoveredAgents.TryGetValue(targetAgentId, out var targetAgent))
        {
            var targetPath = ActorPath.Parse(targetAgent.ClusterAddress);
            var targetRef = Context.ActorSelection(targetPath);
            targetRef.Tell(message);
        }
        else
        {
            Logger.LogWarning("Target agent {TargetAgentId} not found in discovered agents", targetAgentId);
        }
    }

    /// <inheritdoc />
    public virtual async Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        return new[] { typeof(IAIAgentClusterMember) };
    }

    /// <inheritdoc />
    public virtual async Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        return typeof(T) == typeof(IAIAgentClusterMember);
    }

    /// <inheritdoc />
    public virtual async Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (typeof(T) == typeof(IAIAgentClusterMember))
        {
            return this as T;
        }
        return null;
    }

    /// <summary>
    /// Handles coordination messages received from other AI agents.
    /// Override this method to implement custom message handling logic.
    /// </summary>
    /// <param name="message">The received coordination message.</param>
    protected virtual void HandleMessage(AIAgentMessage message)
    {
        Logger.LogDebug("Received coordination message from {SourceAgent}: {MessageType}", 
            message.SourceAgentId, message.MessageType);

        MessageReceived?.Invoke(this, new AIAgentMessageReceivedEventArgs(message));
    }

    private void HandleAgentDiscovery(AgentDiscoveryMessage discovery)
    {
        // Respond if we have any of the requested roles
        if (discovery.RequiredRoles.Any(role => Roles.Contains(role)))
        {
            var agentInfo = new AIAgentInfo(
                AgentId,
                ClusterAddress ?? "",
                Roles,
                GetAgentMetadata());

            var response = new AIAgentMessage(
                "AgentDiscoveryResponse",
                AgentId,
                new Dictionary<string, object> { ["AgentInfo"] = agentInfo },
                DateTime.UtcNow,
                Guid.NewGuid().ToString());

            Sender.Tell(response);
        }
    }

    private async Task AnnounceSelfToClusterAsync(CancellationToken cancellationToken)
    {
        if (_clusterService == null) return;

        var members = await _clusterService.GetClusterMembersAsync(cancellationToken);
        foreach (var member in members.Where(m => m.Address != ClusterAddress))
        {
            var announcement = new AIAgentMessage(
                "AgentAnnouncement",
                AgentId,
                new Dictionary<string, object> 
                { 
                    ["Roles"] = Roles,
                    ["Metadata"] = GetAgentMetadata()
                },
                DateTime.UtcNow,
                Guid.NewGuid().ToString());

            // Send announcement to each cluster member
            // In a real implementation, this would use a more sophisticated routing mechanism
        }
    }

    private void OnClusterMemberJoined(object? sender, ClusterMemberJoinedEventArgs e)
    {
        Logger.LogDebug("Cluster member joined: {Address}", e.Member.Address);
    }

    private void OnClusterMemberLeft(object? sender, ClusterMemberLeftEventArgs e)
    {
        Logger.LogDebug("Cluster member left: {Address}", e.Member.Address);

        // Remove any agents on this node
        var agentsOnNode = _discoveredAgents.Values
            .Where(agent => agent.ClusterAddress.Contains(e.Member.Address))
            .ToList();

        foreach (var agent in agentsOnNode)
        {
            _discoveredAgents.Remove(agent.AgentId);
            AgentLeft?.Invoke(this, new AIAgentLeftEventArgs(agent));
        }
    }

    /// <summary>
    /// Gets metadata about this AI agent for discovery purposes.
    /// Override this method to provide custom metadata.
    /// </summary>
    /// <returns>A dictionary of metadata key-value pairs.</returns>
    protected virtual IDictionary<string, object> GetAgentMetadata()
    {
        return new Dictionary<string, object>
        {
            ["Type"] = GetType().Name,
            ["CreatedAt"] = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Internal message used for agent discovery within the cluster.
/// </summary>
internal class AgentDiscoveryMessage
{
    public AgentDiscoveryMessage(string requesterAgentId, ISet<string> requiredRoles)
    {
        RequesterAgentId = requesterAgentId ?? throw new ArgumentNullException(nameof(requesterAgentId));
        RequiredRoles = requiredRoles ?? throw new ArgumentNullException(nameof(requiredRoles));
    }

    public string RequesterAgentId { get; }
    public ISet<string> RequiredRoles { get; }
}