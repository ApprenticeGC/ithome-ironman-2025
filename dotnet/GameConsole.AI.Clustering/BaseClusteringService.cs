using GameConsole.AI.Clustering.Services;
using Microsoft.Extensions.Logging;
using Akka.Actor;
using Akka.Cluster;
using Akka.Configuration;

namespace GameConsole.AI.Clustering.Services;

/// <summary>
/// Base implementation for AI clustering services providing common functionality.
/// </summary>
public abstract class BaseClusteringService : IService
{
    protected readonly ILogger _logger;
    protected ActorSystem? _actorSystem;
    protected Cluster? _cluster;
    private bool _isRunning;
    private bool _disposed;

    protected BaseClusteringService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region IService Implementation

    public bool IsRunning => _isRunning && !_disposed;

    public virtual async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BaseClusteringService));
        
        _logger.LogInformation("Initializing {ServiceType}", GetType().Name);
        
        // Create actor system with cluster configuration
        var config = CreateClusterConfiguration();
        _actorSystem = ActorSystem.Create("GameConsole-AI-Cluster", config);
        _cluster = Cluster.Get(_actorSystem);
        
        await OnInitializeAsync(cancellationToken);
        _logger.LogInformation("Initialized {ServiceType}", GetType().Name);
    }

    public virtual async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BaseClusteringService));
        
        _logger.LogInformation("Starting {ServiceType}", GetType().Name);
        _isRunning = true;
        await OnStartAsync(cancellationToken);
        _logger.LogInformation("Started {ServiceType}", GetType().Name);
    }

    public virtual async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping {ServiceType}", GetType().Name);
        _isRunning = false;
        await OnStopAsync(cancellationToken);
        _logger.LogInformation("Stopped {ServiceType}", GetType().Name);
    }

    public virtual async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        _logger.LogDebug("Disposing {ServiceType}", GetType().Name);
        
        if (_isRunning)
        {
            await StopAsync();
        }

        await OnDisposeAsync();
        
        if (_actorSystem != null)
        {
            await _actorSystem.Terminate();
            _actorSystem = null;
        }
        
        _cluster = null;
        _disposed = true;
        _logger.LogDebug("Disposed {ServiceType}", GetType().Name);
    }

    #endregion

    #region Abstract Service Contract

    public abstract Task<bool> JoinClusterAsync(string nodeId, string[] capabilities, CancellationToken cancellationToken = default);
    public abstract Task LeaveClusterAsync(CancellationToken cancellationToken = default);
    public abstract Task<string> RouteMessageAsync(string message, string[] requiredCapabilities, CancellationToken cancellationToken = default);
    public abstract Task<ClusterStatus> GetClusterStatusAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Protected Template Methods

    /// <summary>
    /// Override this method to perform custom initialization logic.
    /// </summary>
    protected virtual Task OnInitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Override this method to perform custom startup logic.
    /// </summary>
    protected virtual Task OnStartAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Override this method to perform custom shutdown logic.
    /// </summary>
    protected virtual Task OnStopAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Override this method to perform custom disposal logic.
    /// </summary>
    protected virtual Task OnDisposeAsync()
    {
        return Task.CompletedTask;
    }

    #endregion

    #region Protected Helpers

    /// <summary>
    /// Creates the Akka.NET cluster configuration.
    /// </summary>
    protected virtual Config CreateClusterConfiguration()
    {
        var config = @"
akka {
    actor {
        provider = cluster
    }
    
    cluster {
        seed-nodes = [""akka.tcp://GameConsole-AI-Cluster@127.0.0.1:4053""]
        auto-down-unreachable-after = 10s
        
        sharding {
            state-store-mode = ddata
            distributed-data {
                durable.keys = []
            }
        }
    }
    
    remote {
        dot-netty.tcp {
            hostname = ""127.0.0.1""
            port = 4053
        }
    }
}";
        return ConfigurationFactory.ParseString(config);
    }

    /// <summary>
    /// Gets the actor system if initialized.
    /// </summary>
    protected ActorSystem RequireActorSystem()
    {
        return _actorSystem ?? throw new InvalidOperationException("Actor system not initialized. Call InitializeAsync first.");
    }

    /// <summary>
    /// Gets the cluster if initialized.
    /// </summary>
    protected Cluster RequireCluster()
    {
        return _cluster ?? throw new InvalidOperationException("Cluster not initialized. Call InitializeAsync first.");
    }

    #endregion
}