using Akka.Actor;
using Akka.Cluster;
using Akka.Configuration;
using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Clustering;

/// <summary>
/// Implementation of AI cluster manager using Akka.NET clustering.
/// Manages cluster formation, node discovery, and cluster-wide coordination.
/// </summary>
public class AIClusterManager : IAIClusterManager
{
    private readonly ILogger<AIClusterManager> _logger;
    private ActorSystem? _actorSystem;
    private Cluster? _cluster;
    private bool _isRunning;
    private readonly string _systemName;
    private readonly Config? _config;

    public AIClusterManager(ILogger<AIClusterManager> logger, string systemName = "AIClusterSystem", Config? config = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _systemName = systemName;
        _config = config;
    }

    public ActorSystem ActorSystem => _actorSystem ?? throw new InvalidOperationException("Actor system not initialized");

    public bool IsRunning => _isRunning;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing AI Cluster Manager");
        
        try
        {
            var config = _config ?? GetDefaultClusterConfig();
            _actorSystem = ActorSystem.Create(_systemName, config);
            _cluster = Cluster.Get(_actorSystem);
            
            _logger.LogInformation("AI Cluster Manager initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize AI Cluster Manager");
            throw;
        }
        
        await Task.CompletedTask;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_actorSystem == null || _cluster == null)
            throw new InvalidOperationException("Cluster manager not initialized");

        _logger.LogInformation("Starting AI Cluster Manager");
        
        try
        {
            // Start the cluster
            _cluster.RegisterOnMemberUp(() =>
            {
                _logger.LogInformation("Node joined cluster successfully");
            });

            _cluster.RegisterOnMemberRemoved(member =>
            {
                _logger.LogInformation("Node {Address} removed from cluster", member.Address);
            });

            _isRunning = true;
            _logger.LogInformation("AI Cluster Manager started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start AI Cluster Manager");
            throw;
        }
        
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning) return;

        _logger.LogInformation("Stopping AI Cluster Manager");
        
        try
        {
            if (_cluster != null)
            {
                _cluster.Leave(_cluster.SelfAddress);
                await Task.Delay(2000, cancellationToken); // Allow time for graceful leave
            }

            _isRunning = false;
            _logger.LogInformation("AI Cluster Manager stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping AI Cluster Manager");
            throw;
        }
    }

    public async Task<ClusterState> GetClusterStateAsync()
    {
        if (_cluster == null)
            throw new InvalidOperationException("Cluster not initialized");

        var state = _cluster.State;
        var members = state.Members.Select(m => m.Address).ToList();
        var leader = state.Leader;
        var isHealthy = state.Unreachable.IsEmpty && members.Count > 0;

        return new ClusterState(members, leader, isHealthy, members.Count);
    }

    public async Task JoinClusterAsync(Address nodeAddress)
    {
        if (_cluster == null)
            throw new InvalidOperationException("Cluster not initialized");

        _logger.LogInformation("Joining cluster at {Address}", nodeAddress);
        _cluster.Join(nodeAddress);
        await Task.CompletedTask;
    }

    public async Task LeaveClusterAsync()
    {
        if (_cluster == null)
            throw new InvalidOperationException("Cluster not initialized");

        _logger.LogInformation("Leaving cluster");
        _cluster.Leave(_cluster.SelfAddress);
        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_isRunning)
        {
            await StopAsync();
        }

        _actorSystem?.Dispose();
        _actorSystem = null;
        _cluster = null;
    }

    private static Config GetDefaultClusterConfig()
    {
        return ConfigurationFactory.ParseString(@"
            akka {
                actor {
                    provider = cluster
                }
                remote {
                    dot-netty.tcp {
                        hostname = ""127.0.0.1""
                        port = 0
                    }
                }
                cluster {
                    seed-nodes = []
                    auto-down-unreachable-after = 10s
                    sharding {
                        state-store-mode = ddata
                    }
                }
            }
        ");
    }
}