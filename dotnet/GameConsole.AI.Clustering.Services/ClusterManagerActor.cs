using Akka.Actor;
using Akka.Cluster;
using GameConsole.AI.Clustering.Core;
using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Clustering.Services;

/// <summary>
/// Actor responsible for managing cluster state and membership changes.
/// </summary>
internal class ClusterManagerActor : ReceiveActor
{
    private readonly ILogger _logger;
    private readonly Action<ClusterMembershipChangedEventArgs> _membershipChangedCallback;
    private readonly Dictionary<string, ClusterMember> _members;
    private readonly Cluster _cluster;
    private ClusterMember? _leader;
    private bool _isLeader;

    public ClusterManagerActor(ILogger logger, Action<ClusterMembershipChangedEventArgs> membershipChangedCallback)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _membershipChangedCallback = membershipChangedCallback ?? throw new ArgumentNullException(nameof(membershipChangedCallback));
        _members = new Dictionary<string, ClusterMember>();
        _cluster = Cluster.Get(Context.System);

        SetupReceiveHandlers();
        SubscribeToClusterEvents();
    }

    private void SetupReceiveHandlers()
    {
        Receive<GetClusterStateRequest>(_ => HandleGetClusterState());
        Receive<LeaveClusterRequest>(_ => HandleLeaveCluster());
        Receive<ClusterEvent.MemberUp>(memberUp => HandleMemberUp(memberUp));
        Receive<ClusterEvent.MemberJoined>(memberJoined => HandleMemberJoined(memberJoined));
        Receive<ClusterEvent.MemberLeft>(memberLeft => HandleMemberLeft(memberLeft));
        Receive<ClusterEvent.MemberExited>(memberExited => HandleMemberExited(memberExited));
        Receive<ClusterEvent.MemberRemoved>(memberRemoved => HandleMemberRemoved(memberRemoved));
        Receive<ClusterEvent.UnreachableMember>(unreachable => HandleUnreachableMember(unreachable));
        Receive<ClusterEvent.ReachableMember>(reachable => HandleReachableMember(reachable));
        Receive<ClusterEvent.LeaderChanged>(leaderChanged => HandleLeaderChanged(leaderChanged));
    }

    private void SubscribeToClusterEvents()
    {
        _cluster.Subscribe(Self, ClusterEvent.SubscriptionInitialStateMode.InitialStateAsEvents, 
            typeof(ClusterEvent.MemberUp),
            typeof(ClusterEvent.MemberJoined),
            typeof(ClusterEvent.MemberLeft),
            typeof(ClusterEvent.MemberExited),
            typeof(ClusterEvent.MemberRemoved),
            typeof(ClusterEvent.UnreachableMember),
            typeof(ClusterEvent.ReachableMember),
            typeof(ClusterEvent.LeaderChanged));
    }

    protected override void PostStop()
    {
        _cluster.Unsubscribe(Self);
        base.PostStop();
    }

    private void HandleGetClusterState()
    {
        var clusterState = new ClusterState(
            _members.Values.ToArray(),
            _leader,
            _isLeader,
            _cluster.State.Members.Any(m => m.Status == MemberStatus.Up),
            _cluster.State.Unreachable.Count,
            DateTime.UtcNow);

        Sender.Tell(new ClusterStateResponse(clusterState));
    }

    private void HandleLeaveCluster()
    {
        _logger.LogInformation("Cluster manager received leave cluster request");
        _cluster.Leave(_cluster.SelfAddress);
    }

    private void HandleMemberUp(ClusterEvent.MemberUp memberUp)
    {
        var member = CreateClusterMember(memberUp.Member, ClusterMemberStatus.Up);
        var previousMember = _members.TryGetValue(member.NodeId, out var prev) ? prev : null;
        
        _members[member.NodeId] = member;
        
        _logger.LogInformation("Cluster member up: {NodeId} at {Address}:{Port}", member.NodeId, member.Address, member.Port);
        
        NotifyMembershipChanged(member, previousMember?.Status, ClusterMemberStatus.Up);
    }

    private void HandleMemberJoined(ClusterEvent.MemberJoined memberJoined)
    {
        var member = CreateClusterMember(memberJoined.Member, ClusterMemberStatus.Joining);
        _members[member.NodeId] = member;
        
        _logger.LogInformation("Cluster member joined: {NodeId} at {Address}:{Port}", member.NodeId, member.Address, member.Port);
        
        NotifyMembershipChanged(member, null, ClusterMemberStatus.Joining);
    }

    private void HandleMemberLeft(ClusterEvent.MemberLeft memberLeft)
    {
        var member = CreateClusterMember(memberLeft.Member, ClusterMemberStatus.Leaving);
        var previousMember = _members.TryGetValue(member.NodeId, out var prev) ? prev : null;
        
        _members[member.NodeId] = member;
        
        _logger.LogInformation("Cluster member left: {NodeId} at {Address}:{Port}", member.NodeId, member.Address, member.Port);
        
        NotifyMembershipChanged(member, previousMember?.Status, ClusterMemberStatus.Leaving);
    }

    private void HandleMemberExited(ClusterEvent.MemberExited memberExited)
    {
        var member = CreateClusterMember(memberExited.Member, ClusterMemberStatus.Leaving);
        var previousMember = _members.TryGetValue(member.NodeId, out var prev) ? prev : null;
        
        _members[member.NodeId] = member;
        
        _logger.LogInformation("Cluster member exited: {NodeId} at {Address}:{Port}", member.NodeId, member.Address, member.Port);
        
        NotifyMembershipChanged(member, previousMember?.Status, ClusterMemberStatus.Leaving);
    }

    private void HandleMemberRemoved(ClusterEvent.MemberRemoved memberRemoved)
    {
        var member = CreateClusterMember(memberRemoved.Member, ClusterMemberStatus.Removed);
        var previousMember = _members.TryGetValue(member.NodeId, out var prev) ? prev : null;
        
        _members.Remove(member.NodeId);
        
        _logger.LogInformation("Cluster member removed: {NodeId} at {Address}:{Port}", member.NodeId, member.Address, member.Port);
        
        NotifyMembershipChanged(member, previousMember?.Status, ClusterMemberStatus.Removed);
    }

    private void HandleUnreachableMember(ClusterEvent.UnreachableMember unreachable)
    {
        var member = CreateClusterMember(unreachable.Member, ClusterMemberStatus.Unreachable);
        var previousMember = _members.TryGetValue(member.NodeId, out var prev) ? prev : null;
        
        _members[member.NodeId] = member;
        
        _logger.LogWarning("Cluster member unreachable: {NodeId} at {Address}:{Port}", member.NodeId, member.Address, member.Port);
        
        NotifyMembershipChanged(member, previousMember?.Status, ClusterMemberStatus.Unreachable);
    }

    private void HandleReachableMember(ClusterEvent.ReachableMember reachable)
    {
        var member = CreateClusterMember(reachable.Member, ClusterMemberStatus.Up);
        var previousMember = _members.TryGetValue(member.NodeId, out var prev) ? prev : null;
        
        _members[member.NodeId] = member;
        
        _logger.LogInformation("Cluster member reachable again: {NodeId} at {Address}:{Port}", member.NodeId, member.Address, member.Port);
        
        NotifyMembershipChanged(member, previousMember?.Status, ClusterMemberStatus.Up);
    }

    private void HandleLeaderChanged(ClusterEvent.LeaderChanged leaderChanged)
    {
        ClusterMember? newLeader = null;
        
        if (leaderChanged.Leader != null && _members.TryGetValue(leaderChanged.Leader.ToString(), out var leaderMember))
        {
            newLeader = leaderMember;
        }
        
        var wasLeader = _isLeader;
        _leader = newLeader;
        _isLeader = leaderChanged.Leader?.Equals(_cluster.SelfAddress) ?? false;
        
        _logger.LogInformation("Cluster leader changed. New leader: {Leader}, This node is leader: {IsLeader}", 
            leaderChanged.Leader?.ToString() ?? "none", _isLeader);
    }

    private ClusterMember CreateClusterMember(Member member, ClusterMemberStatus status)
    {
        var nodeId = member.Address.ToString();
        var host = member.Address.Host ?? "unknown";
        var port = member.Address.Port ?? 0;
        var roles = member.Roles.ToArray();
        
        return new ClusterMember(nodeId, host, port, status, roles, DateTime.UtcNow);
    }

    private void NotifyMembershipChanged(ClusterMember member, ClusterMemberStatus? previousStatus, ClusterMemberStatus newStatus)
    {
        try
        {
            var args = new ClusterMembershipChangedEventArgs(member, previousStatus, newStatus);
            _membershipChangedCallback(args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying cluster membership change for member {NodeId}", member.NodeId);
        }
    }
}