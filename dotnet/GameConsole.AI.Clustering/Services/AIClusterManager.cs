using Microsoft.Extensions.Logging;
using GameConsole.AI.Clustering.Interfaces;
using GameConsole.AI.Clustering.Models;
using Akka.Actor;
using Akka.Configuration;
using Akka.Cluster;

namespace GameConsole.AI.Clustering.Services;

/// <summary>
/// Default implementation of IAIClusterManager for cluster coordination and configuration management.
/// </summary>
public class AIClusterManager : IAIClusterManager
{
    private readonly ILogger<AIClusterManager> _logger;
    private ActorSystem? _actorSystem;
    private ClusterConfiguration _configuration = null!;
    private bool _isRunning;
    private readonly Dictionary<string, ClusterNode> _clusterNodes = new();
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the AIClusterManager.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public AIClusterManager(ILogger<AIClusterManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public ClusterConfiguration Configuration => _configuration;

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public event EventHandler<ClusterNode>? NodeJoined;

    /// <inheritdoc />
    public event EventHandler<string>? NodeLeft;

    /// <inheritdoc />
    public event EventHandler<ClusterConfiguration>? ConfigurationChanged;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing AIClusterManager");
        
        // Basic initialization - will be enhanced when actor system foundation is available
        await Task.CompletedTask;
        
        _logger.LogInformation("AIClusterManager initialized");
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting AIClusterManager");
        
        _isRunning = true;
        
        _logger.LogInformation("AIClusterManager started");
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping AIClusterManager");
        
        _isRunning = false;
        
        if (_actorSystem != null)
        {
            await _actorSystem.Terminate();
            _actorSystem = null;
        }
        
        _logger.LogInformation("AIClusterManager stopped");
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isRunning)
        {
            await StopAsync();
        }
    }

    /// <inheritdoc />
    public async Task InitializeClusterAsync(ClusterConfiguration configuration, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing cluster with configuration: {ClusterName}", configuration.ClusterName);
        
        _configuration = configuration;
        
        // Create Akka.NET configuration for clustering
        var akkaConfig = ConfigurationFactory.ParseString($@"
            akka {{
                actor {{
                    provider = cluster
                }}
                cluster {{
                    seed-nodes = [{string.Join(",", configuration.SeedNodes.Select(s => $"\"{s}\""))}]
                    min-nr-of-members = {configuration.MinimumNodes}
                    auto-down-unreachable-after = 30s
                }}
                remote {{
                    dot-netty.tcp {{
                        port = 0
                        hostname = localhost
                    }}
                }}
            }}");
        
        _actorSystem = ActorSystem.Create(configuration.ClusterName, akkaConfig);
        
        ConfigurationChanged?.Invoke(this, configuration);
        
        _logger.LogInformation("Cluster configuration initialized");
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task FormClusterAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Forming cluster");
        
        if (_actorSystem == null)
        {
            throw new InvalidOperationException("Cluster must be initialized before forming");
        }
        
        var cluster = Cluster.Get(_actorSystem);
        cluster.Join(cluster.SelfAddress);
        
        _logger.LogInformation("Cluster formation initiated");
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task LeaveClusterAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Leaving cluster");
        
        if (_actorSystem != null)
        {
            var cluster = Cluster.Get(_actorSystem);
            cluster.Leave(cluster.SelfAddress);
        }
        
        _logger.LogInformation("Left cluster");
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ClusterNode>> GetClusterNodesAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<ClusterNode>>(_clusterNodes.Values.ToList());
        }
    }

    /// <inheritdoc />
    public async Task AddNodeAsync(ClusterNode node, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding node {NodeId} to cluster", node.NodeId);
        
        lock (_lock)
        {
            _clusterNodes[node.NodeId] = node;
        }
        
        NodeJoined?.Invoke(this, node);
        
        _logger.LogInformation("Node {NodeId} added to cluster", node.NodeId);
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task RemoveNodeAsync(string nodeId, bool graceful = true, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Removing node {NodeId} from cluster (graceful: {Graceful})", nodeId, graceful);
        
        lock (_lock)
        {
            _clusterNodes.Remove(nodeId);
        }
        
        NodeLeft?.Invoke(this, nodeId);
        
        _logger.LogInformation("Node {NodeId} removed from cluster", nodeId);
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task UpdateConfigurationAsync(ClusterConfiguration configuration, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating cluster configuration");
        
        _configuration = configuration;
        ConfigurationChanged?.Invoke(this, configuration);
        
        _logger.LogInformation("Cluster configuration updated");
        await Task.CompletedTask;
    }
}