using System.Text;
using Akka.Actor;
using Akka.Configuration;
using GameConsole.AI.Clustering.Core;
using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Clustering.Services;

/// <summary>
/// Akka.NET-based implementation of the AI agent actor clustering coordinator.
/// </summary>
public class AkkaClusterCoordinator : IClusterCoordinator
{
    private readonly ClusterConfiguration _configuration;
    private readonly ILogger<AkkaClusterCoordinator> _logger;
    private ActorSystem? _actorSystem;
    private IActorRef? _clusterManager;
    private bool _disposed;
    private bool _isRunning;

    /// <summary>
    /// Initializes a new instance of the <see cref="AkkaClusterCoordinator"/> class.
    /// </summary>
    /// <param name="configuration">The cluster configuration.</param>
    /// <param name="logger">The logger.</param>
    public AkkaClusterCoordinator(ClusterConfiguration configuration, ILogger<AkkaClusterCoordinator> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public ClusterConfiguration Configuration => _configuration;

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public event EventHandler<ClusterMembershipChangedEventArgs>? ClusterMembershipChanged;

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AkkaClusterCoordinator));

        _logger.LogInformation("Initializing AI agent actor cluster coordinator");

        var config = CreateAkkaConfiguration();
        _actorSystem = ActorSystem.Create(_configuration.ClusterName, config);
        
        _clusterManager = _actorSystem.ActorOf(
            Props.Create(() => new ClusterManagerActor(_logger, OnClusterMembershipChanged)), 
            "cluster-manager");

        _logger.LogInformation("AI agent actor cluster coordinator initialized");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AkkaClusterCoordinator));
        
        if (_actorSystem == null)
            throw new InvalidOperationException("Coordinator must be initialized before starting");

        _logger.LogInformation("Starting AI agent actor cluster coordinator");
        
        _isRunning = true;
        
        _logger.LogInformation("AI agent actor cluster coordinator started");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            return;

        _logger.LogInformation("Stopping AI agent actor cluster coordinator");
        
        _isRunning = false;

        if (_actorSystem != null)
        {
            await _actorSystem.Terminate();
        }

        _logger.LogInformation("AI agent actor cluster coordinator stopped");
    }

    /// <inheritdoc />
    public async Task<ClusterState> GetClusterStateAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AkkaClusterCoordinator));
        
        if (_clusterManager == null)
            throw new InvalidOperationException("Coordinator must be initialized and started");

        try
        {
            var response = await _clusterManager.Ask<ClusterStateResponse>(new GetClusterStateRequest(), TimeSpan.FromSeconds(5));
            return response.State;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cluster state");
            // Return a default empty state
            return new ClusterState(
                Array.Empty<ClusterMember>(),
                null,
                false,
                false,
                0,
                DateTime.UtcNow);
        }
    }

    /// <inheritdoc />
    public Task JoinClusterAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AkkaClusterCoordinator));
        
        _logger.LogInformation("Joining AI agent actor cluster");
        
        // Akka.Cluster handles joining automatically based on configuration
        // This is mostly a placeholder for additional joining logic
        
        _logger.LogInformation("Successfully joined AI agent actor cluster");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task LeaveClusterAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            return Task.CompletedTask;

        _logger.LogInformation("Leaving AI agent actor cluster");
        
        if (_clusterManager != null)
        {
            _clusterManager.Tell(new LeaveClusterRequest());
        }
        
        _logger.LogInformation("Successfully left AI agent actor cluster");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        await StopAsync();
        _disposed = true;
        
        GC.SuppressFinalize(this);
    }

    private Config CreateAkkaConfiguration()
    {
        var hoconConfig = new StringBuilder();
        
        hoconConfig.AppendLine("akka {");
        hoconConfig.AppendLine("  actor {");
        hoconConfig.AppendLine("    provider = cluster");
        hoconConfig.AppendLine("  }");
        hoconConfig.AppendLine("  remote {");
        hoconConfig.AppendLine("    dot-netty.tcp {");
        hoconConfig.AppendLine($"      hostname = \"{_configuration.BindAddress}\"");
        hoconConfig.AppendLine($"      port = {_configuration.BindPort}");
        hoconConfig.AppendLine("    }");
        hoconConfig.AppendLine("  }");
        hoconConfig.AppendLine("  cluster {");
        
        if (_configuration.SeedNodes.Count > 0)
        {
            hoconConfig.AppendLine("    seed-nodes = [");
            foreach (var seedNode in _configuration.SeedNodes)
            {
                hoconConfig.AppendLine($"      \"{seedNode}\"");
            }
            hoconConfig.AppendLine("    ]");
        }
        
        if (_configuration.Roles.Count > 0)
        {
            hoconConfig.AppendLine("    roles = [");
            foreach (var role in _configuration.Roles)
            {
                hoconConfig.AppendLine($"      \"{role}\"");
            }
            hoconConfig.AppendLine("    ]");
        }
        
        hoconConfig.AppendLine($"    min-nr-of-members = {_configuration.MinClusterSize}");
        hoconConfig.AppendLine("  }");
        hoconConfig.AppendLine("}");

        return ConfigurationFactory.ParseString(hoconConfig.ToString());
    }

    private void OnClusterMembershipChanged(ClusterMembershipChangedEventArgs args)
    {
        ClusterMembershipChanged?.Invoke(this, args);
    }
}

// Message classes for actor communication
internal class GetClusterStateRequest { }
internal class ClusterStateResponse
{
    public ClusterStateResponse(ClusterState state)
    {
        State = state;
    }
    
    public ClusterState State { get; }
}
internal class LeaveClusterRequest { }