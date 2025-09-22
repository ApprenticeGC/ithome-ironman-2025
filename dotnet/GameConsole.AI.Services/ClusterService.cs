using Akka.Actor;
using Akka.Cluster;
using Akka.Configuration;
using GameConsole.AI.Core;
using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Services;

/// <summary>
/// Service for managing Akka.NET cluster functionality within the GameConsole architecture.
/// Handles cluster membership, member discovery, and cluster state management.
/// </summary>
public class ClusterService : BaseAIService, IClusterService
{
    private readonly IActorSystemService _actorSystemService;
    private Cluster? _cluster;
    private GameConsole.AI.Core.IActorRef? _clusterListener;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ClusterService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="actorSystemService">The actor system service.</param>
    public ClusterService(ILogger<ClusterService> logger, IActorSystemService actorSystemService)
        : base(logger)
    {
        _actorSystemService = actorSystemService ?? throw new ArgumentNullException(nameof(actorSystemService));
    }

    #region IClusterService Implementation

    /// <summary>
    /// Gets the current cluster state.
    /// </summary>
    public ClusterState State { get; private set; } = ClusterState.NotInCluster;

    /// <summary>
    /// Gets a value indicating whether this node is part of a cluster.
    /// </summary>
    public bool IsInCluster => State != ClusterState.NotInCluster && State != ClusterState.Removed;

    /// <summary>
    /// Gets a value indicating whether this node is the cluster leader.
    /// </summary>
    public bool IsLeader => _cluster?.State?.Leader != null && _cluster.SelfAddress.Equals(_cluster.State.Leader);

    /// <summary>
    /// Gets the cluster members currently known to this node.
    /// </summary>
    public IReadOnlyCollection<ClusterMember> Members
    {
        get
        {
            if (_cluster?.State == null)
                return Array.Empty<ClusterMember>();

            return _cluster.State.Members
                .Select(member => new ClusterMember(
                    member.Address.ToString(),
                    member.Roles.ToHashSet(),
                    MapMemberStatus(member.Status)))
                .ToList();
        }
    }

    /// <summary>
    /// Joins the cluster at the specified address.
    /// </summary>
    /// <param name="seedNodes">The seed node addresses to join.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async join operation.</returns>
    public async Task JoinClusterAsync(IEnumerable<string> seedNodes, CancellationToken cancellationToken = default)
    {
        if (_cluster == null)
            throw new InvalidOperationException("Cluster is not initialized. Call InitializeAsync first.");

        if (seedNodes == null)
            throw new ArgumentNullException(nameof(seedNodes));

        var seedNodesList = seedNodes.ToList();
        if (!seedNodesList.Any())
            throw new ArgumentException("At least one seed node must be provided.", nameof(seedNodes));

        _logger.LogInformation("Joining cluster with seed nodes: {SeedNodes}", string.Join(", ", seedNodesList));

        try
        {
            var addresses = seedNodesList.Select(Address.Parse).ToList();
            
            // Join the first seed node
            _cluster.Join(addresses.First());
            
            State = ClusterState.Joining;
            OnClusterStateChanged(ClusterState.NotInCluster, ClusterState.Joining);

            _logger.LogInformation("Cluster join initiated");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join cluster");
            throw;
        }
    }

    /// <summary>
    /// Leaves the cluster gracefully.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async leave operation.</returns>
    public async Task LeaveClusterAsync(CancellationToken cancellationToken = default)
    {
        if (_cluster == null)
            throw new InvalidOperationException("Cluster is not initialized.");

        if (!IsInCluster)
        {
            _logger.LogWarning("Node is not in a cluster");
            return;
        }

        _logger.LogInformation("Leaving cluster");

        try
        {
            _cluster.Leave(_cluster.SelfAddress);
            
            State = ClusterState.Leaving;
            OnClusterStateChanged(State, ClusterState.Leaving);

            _logger.LogInformation("Cluster leave initiated");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to leave cluster");
            throw;
        }
    }

    /// <summary>
    /// Event raised when the cluster state changes.
    /// </summary>
    public event EventHandler<ClusterStateChangedEventArgs>? ClusterStateChanged;

    /// <summary>
    /// Event raised when a member joins the cluster.
    /// </summary>
    public event EventHandler<ClusterMemberEventArgs>? MemberJoined;

    /// <summary>
    /// Event raised when a member leaves the cluster.
    /// </summary>
    public event EventHandler<ClusterMemberEventArgs>? MemberLeft;

    /// <summary>
    /// Event raised when a member is unreachable.
    /// </summary>
    public event EventHandler<ClusterMemberEventArgs>? MemberUnreachable;

    /// <summary>
    /// Event raised when a member becomes reachable again.
    /// </summary>
    public event EventHandler<ClusterMemberEventArgs>? MemberReachable;

    #endregion

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        if (_actorSystemService is not ActorSystemService actorSystemService)
            throw new InvalidOperationException("ActorSystemService must be an instance of ActorSystemService");

        var actorSystem = actorSystemService.ActorSystem;
        if (actorSystem == null)
            throw new InvalidOperationException("Actor system is not initialized");

        _logger.LogDebug("Initializing cluster service");
        
        _cluster = Cluster.Get(actorSystem);
        
        // Create cluster listener actor
        var actorSys = actorSystemService.ActorSystem;
        var clusterListenerProps = Akka.Actor.Props.Create(() => new ClusterListenerActor(this, _logger));
        var clusterListenerActor = actorSys.ActorOf(clusterListenerProps, "cluster-listener");
        _clusterListener = new ActorRefWrapper(clusterListenerActor);

        _logger.LogDebug("Cluster service initialized");
        await Task.CompletedTask;
    }

    protected override async Task OnStartAsync(CancellationToken cancellationToken)
    {
        if (_cluster == null)
            throw new InvalidOperationException("Cluster is not initialized");

        _logger.LogDebug("Starting cluster service");
        
        // Subscribe to cluster events
        if (_clusterListener is ActorRefWrapper wrapper)
        {
            _cluster.Subscribe(wrapper.UnderlyingActor, ClusterEvent.InitialStateAsEvents, 
                typeof(ClusterEvent.MemberUp),
                typeof(ClusterEvent.MemberLeft),
                typeof(ClusterEvent.MemberExited),
                typeof(ClusterEvent.MemberRemoved),
                typeof(ClusterEvent.UnreachableMember),
                typeof(ClusterEvent.ReachableMember));
        }

        _logger.LogDebug("Cluster service started");
        await Task.CompletedTask;
    }

    protected override async Task OnStopAsync(CancellationToken cancellationToken)
    {
        if (_cluster != null)
        {
            _logger.LogDebug("Stopping cluster service");
            
            // Unsubscribe from cluster events
            if (_clusterListener is ActorRefWrapper wrapper)
            {
                _cluster.Unsubscribe(wrapper.UnderlyingActor);
            }

            // Leave cluster if we're in one
            if (IsInCluster)
            {
                await LeaveClusterAsync(cancellationToken);
            }

            _logger.LogDebug("Cluster service stopped");
        }
        
        await Task.CompletedTask;
    }

    private void OnClusterStateChanged(ClusterState previousState, ClusterState newState)
    {
        ClusterStateChanged?.Invoke(this, new ClusterStateChangedEventArgs(previousState, newState));
    }

    private void OnMemberJoined(ClusterMember member)
    {
        MemberJoined?.Invoke(this, new ClusterMemberEventArgs(member));
    }

    private void OnMemberLeft(ClusterMember member)
    {
        MemberLeft?.Invoke(this, new ClusterMemberEventArgs(member));
    }

    private void OnMemberUnreachable(ClusterMember member)
    {
        MemberUnreachable?.Invoke(this, new ClusterMemberEventArgs(member));
    }

    private void OnMemberReachable(ClusterMember member)
    {
        MemberReachable?.Invoke(this, new ClusterMemberEventArgs(member));
    }

    private static ClusterState MapMemberStatus(MemberStatus status)
    {
        return status switch
        {
            MemberStatus.Joining => ClusterState.Joining,
            MemberStatus.Up => ClusterState.Up,
            MemberStatus.Leaving => ClusterState.Leaving,
            MemberStatus.Exiting => ClusterState.Exiting,
            MemberStatus.Removed => ClusterState.Removed,
            _ => ClusterState.NotInCluster
        };
    }

    /// <summary>
    /// Internal actor for handling cluster events.
    /// </summary>
    private class ClusterListenerActor : ReceiveActor
    {
        private readonly ClusterService _clusterService;
        private readonly ILogger _logger;

        public ClusterListenerActor(ClusterService clusterService, ILogger logger)
        {
            _clusterService = clusterService;
            _logger = logger;
            
            SetupReceiveHandlers();
        }

        public ClusterListenerActor()
        {
            // This constructor is required for Akka.NET actor creation
            _clusterService = null!; // Will be set via dependency injection or messaging
            _logger = null!; // Will be set via dependency injection or messaging
        }

        public static Akka.Actor.Props Props(ClusterService clusterService, ILogger logger)
        {
            return Akka.Actor.Props.Create(() => new ClusterListenerActor(clusterService, logger));
        }

        protected override void PreStart()
        {
            base.PreStart();
            if (_clusterService == null || _logger == null)
            {
                // Handle case where dependencies weren't passed through constructor
                _logger?.LogWarning("ClusterListenerActor created without dependencies");
            }
        }

        private void SetupReceiveHandlers()
        {

            Receive<ClusterEvent.MemberUp>(up =>
            {
                _logger.LogInformation("Member up: {Address} with roles {Roles}", up.Member.Address, string.Join(",", up.Member.Roles));
                var member = new ClusterMember(up.Member.Address.ToString(), up.Member.Roles.ToHashSet(), ClusterState.Up);
                _clusterService.OnMemberJoined(member);
            });

            Receive<ClusterEvent.MemberLeft>(left =>
            {
                _logger.LogInformation("Member left: {Address}", left.Member.Address);
                var member = new ClusterMember(left.Member.Address.ToString(), left.Member.Roles.ToHashSet(), ClusterState.Leaving);
                _clusterService.OnMemberLeft(member);
            });

            Receive<ClusterEvent.MemberExited>(exited =>
            {
                _logger.LogInformation("Member exited: {Address}", exited.Member.Address);
                var member = new ClusterMember(exited.Member.Address.ToString(), exited.Member.Roles.ToHashSet(), ClusterState.Exiting);
                _clusterService.OnMemberLeft(member);
            });

            Receive<ClusterEvent.MemberRemoved>(removed =>
            {
                _logger.LogInformation("Member removed: {Address}", removed.Member.Address);
                var member = new ClusterMember(removed.Member.Address.ToString(), removed.Member.Roles.ToHashSet(), ClusterState.Removed);
                _clusterService.OnMemberLeft(member);
            });

            Receive<ClusterEvent.UnreachableMember>(unreachable =>
            {
                _logger.LogWarning("Member unreachable: {Address}", unreachable.Member.Address);
                var member = new ClusterMember(unreachable.Member.Address.ToString(), unreachable.Member.Roles.ToHashSet(), ClusterState.Unreachable);
                _clusterService.OnMemberUnreachable(member);
            });

            Receive<ClusterEvent.ReachableMember>(reachable =>
            {
                _logger.LogInformation("Member reachable again: {Address}", reachable.Member.Address);
                var member = new ClusterMember(reachable.Member.Address.ToString(), reachable.Member.Roles.ToHashSet(), ClusterState.Up);
                _clusterService.OnMemberReachable(member);
            });
        }
    }
}