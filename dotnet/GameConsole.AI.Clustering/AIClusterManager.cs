using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Clustering;

/// <summary>
/// Implementation of AI cluster manager using Akka.NET clustering.
/// </summary>
public class AIClusterManager : IAIClusterManager
{
    private readonly ILogger<AIClusterManager> _logger;
    private ClusterStatus _status = ClusterStatus.Uninitialized;
    private readonly Dictionary<string, string> _clusterNodes = new();
    private bool _isRunning = false;
    private bool _disposed = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIClusterManager"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public AIClusterManager(ILogger<AIClusterManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public ClusterStatus Status => _status;

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public event EventHandler<ClusterMembershipChangedEventArgs>? MembershipChanged;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing AI Cluster Manager");
        
        // Initialize Akka.NET actor system here
        // For now, just simulate initialization
        await Task.Delay(100, cancellationToken);
        
        _logger.LogInformation("AI Cluster Manager initialized");
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting AI Cluster Manager");
        _status = ClusterStatus.Uninitialized;
        
        // Start cluster services here
        await Task.Delay(50, cancellationToken);
        
        _isRunning = true;
        _logger.LogInformation("AI Cluster Manager started");
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping AI Cluster Manager");
        
        _isRunning = false;
        
        if (_status == ClusterStatus.Up || _status == ClusterStatus.Joining)
        {
            await LeaveClusterAsync(cancellationToken);
        }
        
        _status = ClusterStatus.Uninitialized;
        _logger.LogInformation("AI Cluster Manager stopped");
    }

    /// <inheritdoc />
    public async Task FormClusterAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Forming new AI cluster");
        
        _status = ClusterStatus.Joining;
        
        // Simulate cluster formation
        await Task.Delay(200, cancellationToken);
        
        _status = ClusterStatus.Up;
        var localNode = "akka://ai-cluster@127.0.0.1:2552";
        _clusterNodes[localNode] = "seed";
        
        OnMembershipChanged(localNode, "MemberUp");
        
        _logger.LogInformation("AI cluster formed successfully as seed node");
    }

    /// <inheritdoc />
    public async Task JoinClusterAsync(IEnumerable<string> seedNodes, CancellationToken cancellationToken = default)
    {
        var seedNodesList = seedNodes.ToList();
        _logger.LogInformation("Joining AI cluster with seed nodes: {SeedNodes}", string.Join(", ", seedNodesList));
        
        _status = ClusterStatus.Joining;
        
        // Simulate joining cluster
        await Task.Delay(300, cancellationToken);
        
        _status = ClusterStatus.Up;
        var localNode = $"akka://ai-cluster@127.0.0.1:{Random.Shared.Next(2553, 2600)}";
        _clusterNodes[localNode] = "worker";
        
        // Add seed nodes to our view
        foreach (var seedNode in seedNodesList)
        {
            _clusterNodes[seedNode] = "seed";
        }
        
        OnMembershipChanged(localNode, "MemberUp");
        
        _logger.LogInformation("Successfully joined AI cluster");
    }

    /// <inheritdoc />
    public async Task LeaveClusterAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Leaving AI cluster");
        
        _status = ClusterStatus.Leaving;
        
        // Simulate graceful leave
        await Task.Delay(150, cancellationToken);
        
        var leavingNodes = _clusterNodes.Keys.ToList();
        _clusterNodes.Clear();
        
        foreach (var node in leavingNodes)
        {
            OnMembershipChanged(node, "MemberRemoved");
        }
        
        _status = ClusterStatus.Removed;
        _logger.LogInformation("Left AI cluster successfully");
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        try
        {
            await StopAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during disposal");
        }
        finally
        {
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Raises the MembershipChanged event.
    /// </summary>
    /// <param name="nodeAddress">Address of the node that changed.</param>
    /// <param name="changeType">Type of change.</param>
    protected virtual void OnMembershipChanged(string nodeAddress, string changeType)
    {
        _logger.LogDebug("Cluster membership changed: {NodeAddress} -> {ChangeType}", nodeAddress, changeType);
        MembershipChanged?.Invoke(this, new ClusterMembershipChangedEventArgs(nodeAddress, changeType));
    }
}