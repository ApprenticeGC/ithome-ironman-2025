using Akka.Actor;
using Akka.Configuration;
using GameConsole.AI.Clustering.Configuration;
using GameConsole.AI.Clustering.Actors;
using GameConsole.AI.Clustering.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace GameConsole.AI.Clustering.Services;

/// <summary>
/// Main service for managing AI cluster operations
/// </summary>
public class AIClusterService : IHostedService, IDisposable
{
    private readonly ILogger<AIClusterService> _logger;
    private readonly AIClusterConfig _config;
    private ActorSystem? _actorSystem;
    private IActorRef? _clusterManager;
    private IActorRef? _nodeManager;

    public AIClusterService(ILogger<AIClusterService> logger, AIClusterConfig config)
    {
        _logger = logger;
        _config = config;
    }

    /// <summary>
    /// Actor system for cluster operations
    /// </summary>
    public ActorSystem ActorSystem => _actorSystem ?? throw new InvalidOperationException("Service not started");

    /// <summary>
    /// Reference to the cluster manager actor
    /// </summary>
    public IActorRef ClusterManager => _clusterManager ?? throw new InvalidOperationException("Service not started");

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting AI Cluster Service with system name: {SystemName}", _config.ActorSystemName);

        try
        {
            // Create Akka configuration
            var akkaConfig = CreateAkkaConfiguration();
            
            // Create actor system
            _actorSystem = ActorSystem.Create(_config.ActorSystemName, akkaConfig);
            
            // Generate unique node ID
            var nodeId = $"{Environment.MachineName}-{Guid.NewGuid():N}";
            
            // Create cluster manager
            _clusterManager = _actorSystem.ActorOf(
                AIClusterManager.Props(_config),
                "cluster-manager"
            );
            
            // Create node manager
            _nodeManager = _actorSystem.ActorOf(
                AINodeManager.Props(_config, nodeId),
                "node-manager"
            );

            _logger.LogInformation("AI Cluster Service started successfully with node ID: {NodeId}", nodeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start AI Cluster Service");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping AI Cluster Service");

        try
        {
            if (_actorSystem != null)
            {
                await _actorSystem.Terminate();
                _actorSystem = null;
                _clusterManager = null;
                _nodeManager = null;
            }
            
            _logger.LogInformation("AI Cluster Service stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping AI Cluster Service");
            throw;
        }
    }

    /// <summary>
    /// Send a message to the cluster for routing
    /// </summary>
    /// <param name="capability">Required capability</param>
    /// <param name="message">Message to route</param>
    /// <param name="requestId">Unique request ID</param>
    /// <returns>Route response task</returns>
    public async Task<ClusterMessages.RouteResponse> RouteMessageAsync(
        string capability, 
        object message, 
        string? requestId = null)
    {
        if (_clusterManager == null)
            throw new InvalidOperationException("Cluster service not started");

        requestId ??= Guid.NewGuid().ToString();
        
        var routeMessage = new ClusterMessages.RouteMessage(capability, message, requestId, ActorRefs.NoSender);
        
        var response = await _clusterManager.Ask<ClusterMessages.RouteResponse>(
            routeMessage, 
            TimeSpan.FromSeconds(_config.ClusterOperationTimeoutSeconds)
        );
        
        return response;
    }

    /// <summary>
    /// Get current cluster state
    /// </summary>
    /// <returns>Cluster state information</returns>
    public async Task<ClusterMessages.ClusterState> GetClusterStateAsync()
    {
        if (_clusterManager == null)
            throw new InvalidOperationException("Cluster service not started");

        var response = await _clusterManager.Ask<ClusterMessages.ClusterState>(
            new ClusterMessages.GetClusterState(),
            TimeSpan.FromSeconds(_config.ClusterOperationTimeoutSeconds)
        );
        
        return response;
    }

    /// <summary>
    /// Request cluster scaling
    /// </summary>
    /// <param name="targetSize">Desired cluster size</param>
    /// <param name="capability">Specific capability to scale</param>
    public void ScaleCluster(int targetSize, string? capability = null)
    {
        if (_clusterManager == null)
            throw new InvalidOperationException("Cluster service not started");

        _clusterManager.Tell(new ClusterMessages.ScaleCluster(targetSize, capability));
        _logger.LogInformation("Requested cluster scaling to {TargetSize} nodes for capability: {Capability}", 
                               targetSize, capability ?? "all");
    }

    private Config CreateAkkaConfiguration()
    {
        var configString = $@"
akka {{
    actor {{
        provider = cluster
        serializers {{
            hyperion = ""Akka.Serialization.HyperionSerializer, Akka.Serialization.Hyperion""
        }}
        serialization-bindings {{
            ""System.Object"" = hyperion
        }}
    }}

    remote {{
        dot-netty.tcp {{
            port = {_config.ClusterPort}
            hostname = localhost
        }}
    }}

    cluster {{
        seed-nodes = [{string.Join(", ", _config.SeedNodes.Select(s => $"\"akka.tcp://{_config.ActorSystemName}@{s}\""))}]
        
        auto-down-unreachable-after = 10s
        
        sharding {{
            number-of-shards = {_config.NumberOfShards}
            guardian-name = sharding
        }}
        
        split-brain-resolver {{
            active-strategy = {_config.SplitBrainResolverStrategy}
        }}
    }}

    coordinated-shutdown {{
        default-phase-timeout = 30s
    }}

    loglevel = INFO
    loggers = [""Akka.Logger.Extensions.Logging.LoggingLogger, Akka.Logger.Extensions.Logging""]
}}";

        return ConfigurationFactory.ParseString(configString);
    }

    public void Dispose()
    {
        _actorSystem?.Dispose();
    }
}