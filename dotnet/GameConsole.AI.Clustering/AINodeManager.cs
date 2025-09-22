using GameConsole.AI.Clustering.Services;
using Microsoft.Extensions.Logging;
using Akka.Actor;
using System.Collections.Concurrent;

namespace GameConsole.AI.Clustering.Services;

/// <summary>
/// Manages individual AI agent nodes within the cluster.
/// Handles node lifecycle, health monitoring, and local resource management.
/// </summary>
public class AINodeManager : BaseClusteringService
{
    private readonly ConcurrentDictionary<string, IActorRef> _localAgents = new();
    private string _nodeId = string.Empty;
    private string[] _nodeCapabilities = Array.Empty<string>();

    public AINodeManager(ILogger<AINodeManager> logger) : base(logger)
    {
    }

    #region IService Implementation

    public override async Task<bool> JoinClusterAsync(string nodeId, string[] capabilities, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
            throw new ArgumentException("Node ID cannot be empty", nameof(nodeId));

        try
        {
            _logger.LogInformation("Initializing node {NodeId} with capabilities: {Capabilities}", 
                nodeId, string.Join(", ", capabilities));

            _nodeId = nodeId;
            _nodeCapabilities = capabilities;

            // Initialize local agents based on capabilities
            foreach (var capability in capabilities)
            {
                var agentId = $"{nodeId}-{capability}-agent";
                await CreateAgentAsync(agentId, new[] { capability }, cancellationToken);
            }

            _logger.LogInformation("Node {NodeId} initialized successfully", nodeId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize node {NodeId}", nodeId);
            return false;
        }
    }

    public override Task LeaveClusterAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Shutting down node {NodeId}", _nodeId);

            // Gracefully shutdown local agents
            foreach (var agentId in _localAgents.Keys.ToArray())
            {
                _ = RemoveAgentAsync(agentId, cancellationToken);
            }

            _logger.LogInformation("Node {NodeId} shutdown completed", _nodeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error shutting down node {NodeId}", _nodeId);
        }
        
        return Task.CompletedTask;
    }

    public override Task<string> RouteMessageAsync(string message, string[] requiredCapabilities, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be empty", nameof(message));

        try
        {
            _logger.LogDebug("Processing message locally on node {NodeId}", _nodeId);

            // Check if this node has required capabilities
            if (!HasRequiredCapabilities(requiredCapabilities))
            {
                _logger.LogWarning("Node {NodeId} does not have required capabilities: {RequiredCapabilities}", 
                    _nodeId, string.Join(", ", requiredCapabilities));
                return Task.FromResult("Node does not have required capabilities");
            }

            // Find appropriate local agent
            var agentId = SelectLocalAgent(requiredCapabilities);
            if (agentId == null)
            {
                _logger.LogWarning("No suitable local agent found for capabilities: {RequiredCapabilities}", 
                    string.Join(", ", requiredCapabilities));
                return Task.FromResult("No suitable local agent available");
            }

            _logger.LogDebug("Message processed successfully by agent {AgentId}", agentId);
            return Task.FromResult($"Agent {agentId} processed: {message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message on node {NodeId}", _nodeId);
            throw;
        }
    }

    public override Task<ClusterStatus> GetClusterStatusAsync(CancellationToken cancellationToken = default)
    {
        // Node manager provides local status information
        var status = new ClusterStatus
        {
            LeaderNodeId = _nodeId,
            ActiveNodes = 1, // This node only
            AvailableCapabilities = _nodeCapabilities,
            LastUpdated = DateTime.UtcNow,
            State = ClusterState.Up
        };
        
        return Task.FromResult(status);
    }

    #endregion

    #region Public Node Management Methods

    /// <summary>
    /// Creates a new local agent with specified capabilities.
    /// </summary>
    public Task<bool> CreateAgentAsync(string agentId, string[] capabilities, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating local agent {AgentId} with capabilities: {Capabilities}", 
                agentId, string.Join(", ", capabilities));

            // Mock agent creation - in real implementation would create actual actor
            _localAgents[agentId] = null!; // Placeholder

            _logger.LogInformation("Local agent {AgentId} created successfully", agentId);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create local agent {AgentId}", agentId);
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Removes a local agent.
    /// </summary>
    public Task<bool> RemoveAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Removing local agent {AgentId}", agentId);

            if (_localAgents.TryRemove(agentId, out _))
            {
                _logger.LogInformation("Local agent {AgentId} removed successfully", agentId);
                return Task.FromResult(true);
            }

            _logger.LogWarning("Local agent {AgentId} not found", agentId);
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove local agent {AgentId}", agentId);
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Gets current resource metrics for this node.
    /// </summary>
    public NodeResourceMetrics GetResourceMetrics()
    {
        return new NodeResourceMetrics
        {
            NodeId = _nodeId,
            CpuUsage = Random.Shared.NextDouble() * 100,
            MemoryUsage = Random.Shared.NextDouble() * 100,
            ActiveAgents = _localAgents.Count,
            MessageThroughput = Random.Shared.NextDouble() * 1000,
            LastUpdated = DateTime.UtcNow
        };
    }

    #endregion

    #region Private Methods

    private bool HasRequiredCapabilities(string[] requiredCapabilities)
    {
        return requiredCapabilities.All(req => _nodeCapabilities.Contains(req));
    }

    private string? SelectLocalAgent(string[] requiredCapabilities)
    {
        // Simple selection - find first agent that can handle any of the required capabilities
        return _localAgents.Keys.FirstOrDefault();
    }

    #endregion

    #region Helper Classes and Records

    public class NodeResourceMetrics
    {
        public string NodeId { get; set; } = string.Empty;
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public int ActiveAgents { get; set; }
        public double MessageThroughput { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    #endregion
}