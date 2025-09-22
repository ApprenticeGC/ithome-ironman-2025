using GameConsole.AI.Clustering.Services;
using Microsoft.Extensions.Logging;
using Akka.Actor;
using System.Collections.Concurrent;

namespace GameConsole.AI.Clustering.Services;

/// <summary>
/// Intelligent message routing service for AI agents in the cluster.
/// Implements various routing strategies and load balancing algorithms.
/// </summary>
public class ClusterAIRouter : BaseClusteringService, IAdvancedRoutingCapability
{
    private readonly ConcurrentDictionary<string, RouteInfo> _routes = new();
    private readonly ConcurrentDictionary<string, RoutingStats> _routingStats = new();
    private RoutingStrategy _defaultStrategy = RoutingStrategy.RoundRobin;
    private readonly Dictionary<string, int> _roundRobinCounters = new();

    public ClusterAIRouter(ILogger<ClusterAIRouter> logger) : base(logger)
    {
    }

    #region IService Implementation

    public override Task<bool> JoinClusterAsync(string nodeId, string[] capabilities, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Initializing Cluster AI Router for node {NodeId}", nodeId);

            // Initialize routing tables
            foreach (var capability in capabilities)
            {
                RegisterRoute(capability, "default-node");
                _roundRobinCounters[capability] = 0;
            }

            _logger.LogInformation("Cluster AI Router initialized for node {NodeId}", nodeId);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Cluster AI Router for node {NodeId}", nodeId);
            return Task.FromResult(false);
        }
    }

    public override Task LeaveClusterAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Shutting down Cluster AI Router");

            _routes.Clear();
            _routingStats.Clear();

            _logger.LogInformation("Cluster AI Router shutdown completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error shutting down Cluster AI Router");
        }
        
        return Task.CompletedTask;
    }

    public override Task<string> RouteMessageAsync(string message, string[] requiredCapabilities, CancellationToken cancellationToken = default)
    {
        return RouteWithStrategyAsync(message, _defaultStrategy, cancellationToken);
    }

    public override Task<ClusterStatus> GetClusterStatusAsync(CancellationToken cancellationToken = default)
    {
        var availableCapabilities = _routes.Keys.ToArray();
        var activeRoutes = _routes.Count(r => r.Value.IsActive);

        var status = new ClusterStatus
        {
            LeaderNodeId = "router-node",
            ActiveNodes = activeRoutes,
            AvailableCapabilities = availableCapabilities,
            LastUpdated = DateTime.UtcNow,
            State = ClusterState.Up
        };
        
        return Task.FromResult(status);
    }

    #endregion

    #region ICapabilityProvider Implementation

    public Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Type>>(new Type[] { typeof(IAdvancedRoutingCapability) });
    }

    public Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(typeof(T) == typeof(IAdvancedRoutingCapability));
    }

    public Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (typeof(T) == typeof(IAdvancedRoutingCapability))
        {
            return Task.FromResult(this as T);
        }
        return Task.FromResult<T?>(null);
    }

    #endregion

    #region IAdvancedRoutingCapability Implementation

    public Task<string> RouteWithStrategyAsync(string message, RoutingStrategy routingStrategy, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be empty", nameof(message));

        try
        {
            _logger.LogDebug("Routing message using strategy {Strategy}", routingStrategy);

            var routingKey = ExtractRoutingKey(message);
            var targetNode = SelectTargetNode(routingKey, routingStrategy);

            if (targetNode == null)
            {
                _logger.LogWarning("No target node found for routing key {RoutingKey} with strategy {Strategy}", 
                    routingKey, routingStrategy);
                return Task.FromResult("No target node available");
            }

            // Record routing statistics
            RecordRoutingStats(targetNode, routingStrategy);

            var response = $"Routed to {targetNode}: {message}";

            _logger.LogDebug("Message routed successfully to node {TargetNode} using strategy {Strategy}", 
                targetNode, routingStrategy);
            
            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing message with strategy {Strategy}", routingStrategy);
            throw;
        }
    }

    public Task<string[]> BroadcastMessageAsync(string message, string[] capabilities, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be empty", nameof(message));

        try
        {
            _logger.LogInformation("Broadcasting message to nodes with capabilities: {Capabilities}", 
                string.Join(", ", capabilities));

            var targetNodes = FindNodesWithCapabilities(capabilities);
            
            if (targetNodes.Length == 0)
            {
                _logger.LogWarning("No nodes found with required capabilities: {Capabilities}", 
                    string.Join(", ", capabilities));
                return Task.FromResult(new[] { "No nodes with required capabilities" });
            }

            // Generate responses from all target nodes
            var responses = targetNodes.Select(node => $"Broadcast to {node}: {message}").ToArray();

            _logger.LogInformation("Message broadcasted to {NodeCount} nodes", targetNodes.Length);
            return Task.FromResult(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting message");
            throw;
        }
    }

    #endregion

    #region Public Router Management Methods

    /// <summary>
    /// Registers a route for a specific capability to a target node.
    /// </summary>
    public void RegisterRoute(string capability, string nodeId, int weight = 1)
    {
        _logger.LogInformation("Registering route for capability {Capability} to node {NodeId} with weight {Weight}", 
            capability, nodeId, weight);

        var routeInfo = new RouteInfo
        {
            Capability = capability,
            NodeId = nodeId,
            Weight = weight,
            IsActive = true,
            RegisteredAt = DateTime.UtcNow,
            LastUsed = DateTime.UtcNow
        };

        _routes[GetRouteKey(capability, nodeId)] = routeInfo;
    }

    /// <summary>
    /// Unregisters a route.
    /// </summary>
    public void UnregisterRoute(string capability, string nodeId)
    {
        var routeKey = GetRouteKey(capability, nodeId);
        if (_routes.TryRemove(routeKey, out var route))
        {
            _logger.LogInformation("Unregistered route for capability {Capability} from node {NodeId}", 
                capability, nodeId);
        }
    }

    /// <summary>
    /// Updates the default routing strategy.
    /// </summary>
    public void SetDefaultRoutingStrategy(RoutingStrategy strategy)
    {
        _logger.LogInformation("Changed default routing strategy from {OldStrategy} to {NewStrategy}", 
            _defaultStrategy, strategy);
        _defaultStrategy = strategy;
    }

    /// <summary>
    /// Gets routing statistics.
    /// </summary>
    public Dictionary<string, RoutingStats> GetRoutingStatistics()
    {
        return new Dictionary<string, RoutingStats>(_routingStats);
    }

    #endregion

    #region Private Methods

    private string ExtractRoutingKey(string message)
    {
        // Simple routing key extraction - in real implementation would be more sophisticated
        return message.Length > 10 ? message.Substring(0, 10) : message;
    }

    private string? SelectTargetNode(string routingKey, RoutingStrategy strategy)
    {
        var activeRoutes = _routes.Values.Where(r => r.IsActive).ToArray();
        if (activeRoutes.Length == 0) return null;

        return strategy switch
        {
            RoutingStrategy.RoundRobin => SelectRoundRobin(activeRoutes, routingKey),
            RoutingStrategy.LeastLoaded => SelectLeastLoaded(activeRoutes),
            RoutingStrategy.CapabilityBased => SelectCapabilityBased(activeRoutes, routingKey),
            RoutingStrategy.ConsistentHashing => SelectConsistentHash(activeRoutes, routingKey),
            RoutingStrategy.Random => SelectRandom(activeRoutes),
            _ => SelectRoundRobin(activeRoutes, routingKey)
        };
    }

    private string SelectRoundRobin(RouteInfo[] routes, string routingKey)
    {
        var key = routingKey.Length > 0 ? routingKey[0].ToString() : "default";
        if (!_roundRobinCounters.ContainsKey(key))
            _roundRobinCounters[key] = 0;

        var index = _roundRobinCounters[key] % routes.Length;
        _roundRobinCounters[key]++;
        return routes[index].NodeId;
    }

    private string SelectLeastLoaded(RouteInfo[] routes)
    {
        // Mock implementation - would use actual load metrics
        var leastLoadedRoute = routes.OrderBy(r => _routingStats.GetValueOrDefault(r.NodeId)?.MessageCount ?? 0).First();
        return leastLoadedRoute.NodeId;
    }

    private string SelectCapabilityBased(RouteInfo[] routes, string routingKey)
    {
        // Select based on capability match - simplified implementation
        var matchingRoute = routes.FirstOrDefault(r => r.Capability.Contains(routingKey.Substring(0, Math.Min(3, routingKey.Length))));
        return matchingRoute?.NodeId ?? routes.First().NodeId;
    }

    private string SelectConsistentHash(RouteInfo[] routes, string routingKey)
    {
        var hash = routingKey.GetHashCode();
        var index = Math.Abs(hash) % routes.Length;
        return routes[index].NodeId;
    }

    private string SelectRandom(RouteInfo[] routes)
    {
        var index = Random.Shared.Next(routes.Length);
        return routes[index].NodeId;
    }

    private string[] FindNodesWithCapabilities(string[] capabilities)
    {
        return _routes.Values
            .Where(r => r.IsActive && capabilities.Any(cap => r.Capability.Contains(cap)))
            .Select(r => r.NodeId)
            .Distinct()
            .ToArray();
    }

    private void RecordRoutingStats(string nodeId, RoutingStrategy strategy)
    {
        var stats = _routingStats.GetOrAdd(nodeId, _ => new RoutingStats { NodeId = nodeId });
        stats.MessageCount++;
        stats.LastRoutedAt = DateTime.UtcNow;
        stats.LastStrategy = strategy;
    }

    private static string GetRouteKey(string capability, string nodeId) => $"{capability}:{nodeId}";

    #endregion

    #region Helper Classes

    public class RouteInfo
    {
        public string Capability { get; set; } = string.Empty;
        public string NodeId { get; set; } = string.Empty;
        public int Weight { get; set; } = 1;
        public bool IsActive { get; set; } = true;
        public DateTime RegisteredAt { get; set; }
        public DateTime LastUsed { get; set; }
    }

    public class RoutingStats
    {
        public string NodeId { get; set; } = string.Empty;
        public int MessageCount { get; set; }
        public DateTime LastRoutedAt { get; set; }
        public RoutingStrategy LastStrategy { get; set; }
        public TimeSpan AverageResponseTime { get; set; }
    }

    #endregion
}