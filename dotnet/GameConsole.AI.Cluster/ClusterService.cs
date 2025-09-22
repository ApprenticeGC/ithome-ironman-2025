using Akka.Actor;
using Akka.Cluster;
using Akka.Configuration;
using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Cluster;

/// <summary>
/// Akka.NET-based cluster service implementation for distributed AI agent coordination.
/// Provides cluster membership management and actor system coordination.
/// </summary>
[Service("ai-cluster", "1.0.0", "AI Agent Cluster Service", Categories = new[] { "cluster" }, Lifetime = ServiceLifetime.Singleton)]
public class ClusterService : IClusterService
{
    private readonly ILogger<ClusterService> _logger;
    private readonly ClusterConfiguration _configuration;
    private ActorSystem? _actorSystem;
    private Akka.Cluster.Cluster? _cluster;
    private bool _isRunning;

    /// <summary>
    /// Initializes a new instance of the ClusterService class.
    /// </summary>
    /// <param name="logger">Logger for the cluster service.</param>
    /// <param name="configuration">Cluster configuration settings.</param>
    public ClusterService(ILogger<ClusterService> logger, ClusterConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public event EventHandler<ClusterMemberJoinedEventArgs>? MemberJoined;

    /// <inheritdoc />
    public event EventHandler<ClusterMemberLeftEventArgs>? MemberLeft;

    /// <inheritdoc />
    public event EventHandler<ClusterMemberUnreachableEventArgs>? MemberUnreachable;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing ClusterService with cluster name: {ClusterName}", _configuration.ClusterName);

        var akkaConfig = CreateAkkaConfiguration();
        _actorSystem = ActorSystem.Create(_configuration.ClusterName, akkaConfig);
        _cluster = Akka.Cluster.Cluster.Get(_actorSystem);

        // Subscribe to cluster events
        _cluster.Subscribe(
            _actorSystem.ActorOf(Props.Create(() => new ClusterEventListener(this)), "cluster-event-listener"),
            ClusterEvent.IMemberEvent.Instance);

        _logger.LogDebug("ClusterService initialized successfully");
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_cluster == null)
            throw new InvalidOperationException("Cluster service must be initialized before starting.");

        _logger.LogInformation("Starting ClusterService on {Hostname}:{Port}", _configuration.Hostname, _configuration.Port);

        if (_configuration.SeedNodes.Any())
        {
            await JoinClusterAsync(_configuration.SeedNodes, cancellationToken);
        }
        else
        {
            // Self-join if no seed nodes specified
            _cluster.Join(_cluster.SelfAddress);
        }

        _isRunning = true;
        _logger.LogInformation("ClusterService started successfully");
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
            return;

        _logger.LogInformation("Stopping ClusterService");

        if (_cluster != null)
        {
            await LeaveClusterAsync(cancellationToken);
        }

        if (_actorSystem != null)
        {
            await _actorSystem.Terminate();
        }

        _isRunning = false;
        _logger.LogInformation("ClusterService stopped successfully");
    }

    /// <inheritdoc />
    public async Task<ClusterMemberInfo> GetClusterMemberAsync(CancellationToken cancellationToken = default)
    {
        if (_cluster == null)
            throw new InvalidOperationException("Cluster service is not initialized.");

        var selfMember = _cluster.SelfMember;
        return MapToClusterMemberInfo(selfMember);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ClusterMemberInfo>> GetClusterMembersAsync(CancellationToken cancellationToken = default)
    {
        if (_cluster == null)
            throw new InvalidOperationException("Cluster service is not initialized.");

        return _cluster.State.Members.Select(MapToClusterMemberInfo);
    }

    /// <inheritdoc />
    public async Task JoinClusterAsync(IEnumerable<string> seedNodes, CancellationToken cancellationToken = default)
    {
        if (_cluster == null)
            throw new InvalidOperationException("Cluster service is not initialized.");

        _logger.LogInformation("Joining cluster with seed nodes: {SeedNodes}", string.Join(", ", seedNodes));

        var addresses = seedNodes.Select(Address.Parse).ToList();
        _cluster.JoinSeedNodes(addresses);

        // Wait for cluster to be up
        await WaitForClusterUp(cancellationToken);
        
        _logger.LogInformation("Successfully joined cluster");
    }

    /// <inheritdoc />
    public async Task LeaveClusterAsync(CancellationToken cancellationToken = default)
    {
        if (_cluster == null)
            return;

        _logger.LogInformation("Leaving cluster");

        _cluster.Leave(_cluster.SelfAddress);

        // Wait for graceful shutdown
        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }

    private Config CreateAkkaConfiguration()
    {
        var configString = $@"
            akka {{
                actor {{
                    provider = cluster
                }}
                remote {{
                    log-remote-lifecycle-events = DEBUG
                    dot-netty.tcp {{
                        hostname = ""{_configuration.Hostname}""
                        port = {_configuration.Port}
                    }}
                }}
                cluster {{
                    seed-nodes = [{string.Join(",", _configuration.SeedNodes.Select(s => $"\"{s}\""))}]
                    roles = [{string.Join(",", _configuration.Roles.Select(r => $"\"{r}\""))}]
                    min-nr-of-members = {_configuration.MinimumMembers}
                }}
            }}
        ";

        return ConfigurationFactory.ParseString(configString);
    }

    private async Task WaitForClusterUp(CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        while (!cancellationToken.IsCancellationRequested && 
               DateTime.UtcNow - startTime < _configuration.JoinTimeout)
        {
            if (_cluster?.State.Members.Any(m => m.Status == MemberStatus.Up) == true)
            {
                return;
            }

            await Task.Delay(100, cancellationToken);
        }

        throw new TimeoutException($"Failed to join cluster within {_configuration.JoinTimeout}");
    }

    internal static ClusterMemberInfo MapToClusterMemberInfo(Member member)
    {
        return new ClusterMemberInfo(
            Address: member.Address.ToString(),
            NodeId: member.UniqueAddress.Uid.ToString(),
            Status: MapMemberStatus(member.Status),
            Roles: member.Roles.ToHashSet(),
            JoinedAt: DateTime.UtcNow // Akka doesn't provide join time directly
        );
    }

    internal static ClusterMemberStatus MapMemberStatus(MemberStatus status) => status switch
    {
        MemberStatus.Joining => ClusterMemberStatus.Joining,
        MemberStatus.WeaklyUp => ClusterMemberStatus.WeaklyUp,
        MemberStatus.Up => ClusterMemberStatus.Up,
        MemberStatus.Leaving => ClusterMemberStatus.Leaving,
        MemberStatus.Exiting => ClusterMemberStatus.Exiting,
        MemberStatus.Down => ClusterMemberStatus.Down,
        MemberStatus.Removed => ClusterMemberStatus.Removed,
        _ => ClusterMemberStatus.Down
    };

    internal void OnMemberJoined(ClusterMemberInfo member)
    {
        _logger.LogInformation("Cluster member joined: {Address}", member.Address);
        MemberJoined?.Invoke(this, new ClusterMemberJoinedEventArgs(member));
    }

    internal void OnMemberLeft(ClusterMemberInfo member)
    {
        _logger.LogInformation("Cluster member left: {Address}", member.Address);
        MemberLeft?.Invoke(this, new ClusterMemberLeftEventArgs(member));
    }

    internal void OnMemberUnreachable(ClusterMemberInfo member)
    {
        _logger.LogWarning("Cluster member unreachable: {Address}", member.Address);
        MemberUnreachable?.Invoke(this, new ClusterMemberUnreachableEventArgs(member));
    }
}