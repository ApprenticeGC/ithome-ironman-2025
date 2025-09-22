using Akka.Actor;
using Akka.Cluster;
using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Cluster;

/// <summary>
/// Actor that listens to cluster events and forwards them to the ClusterService.
/// </summary>
internal class ClusterEventListener : ReceiveActor
{
    private readonly ClusterService _clusterService;

    public ClusterEventListener(ClusterService clusterService)
    {
        _clusterService = clusterService ?? throw new ArgumentNullException(nameof(clusterService));

        Receive<ClusterEvent.MemberUp>(memberUp =>
        {
            var memberInfo = MapToClusterMemberInfo(memberUp.Member);
            _clusterService.OnMemberJoined(memberInfo);
        });

        Receive<ClusterEvent.MemberLeft>(memberLeft =>
        {
            var memberInfo = MapToClusterMemberInfo(memberLeft.Member);
            _clusterService.OnMemberLeft(memberInfo);
        });

        Receive<ClusterEvent.MemberExited>(memberExited =>
        {
            var memberInfo = MapToClusterMemberInfo(memberExited.Member);
            _clusterService.OnMemberLeft(memberInfo);
        });

        Receive<ClusterEvent.MemberRemoved>(memberRemoved =>
        {
            var memberInfo = MapToClusterMemberInfo(memberRemoved.Member);
            _clusterService.OnMemberLeft(memberInfo);
        });

        Receive<ClusterEvent.UnreachableMember>(unreachable =>
        {
            var memberInfo = MapToClusterMemberInfo(unreachable.Member);
            _clusterService.OnMemberUnreachable(memberInfo);
        });

        Receive<ClusterEvent.ReachableMember>(reachable =>
        {
            // Member is reachable again, we could add a specific event for this
            var memberInfo = MapToClusterMemberInfo(reachable.Member);
            _clusterService.OnMemberJoined(memberInfo);
        });
    }

    private static ClusterMemberInfo MapToClusterMemberInfo(Member member)
    {
        return new ClusterMemberInfo(
            member.Address.ToString(),
            member.UniqueAddress.Uid.ToString(),
            MapMemberStatus(member.Status),
            member.Roles.ToHashSet(),
            DateTime.UtcNow // Akka doesn't provide join time directly
        );
    }

    private static ClusterMemberStatus MapMemberStatus(MemberStatus status) => status switch
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
}