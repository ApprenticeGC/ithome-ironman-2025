using System;
using Akka.Actor;
using Akka.Cluster;
using Microsoft.Extensions.Logging;
using GameConsole.AI.Core;

namespace GameConsole.AI.Services
{
    /// <summary>
    /// Actor responsible for managing cluster events and coordinating AI agent distribution.
    /// </summary>
    public class ClusterManagerActor : ReceiveActor
    {
        private readonly ILogger _logger;
        private readonly AkkaAIAgentCluster _cluster;
        private readonly Cluster _clusterExtension;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterManagerActor"/> class.
        /// </summary>
        /// <param name="logger">Logger instance for diagnostics.</param>
        /// <param name="cluster">The AI agent cluster service.</param>
        public ClusterManagerActor(ILogger logger, AkkaAIAgentCluster cluster)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cluster = cluster ?? throw new ArgumentNullException(nameof(cluster));
            _clusterExtension = Cluster.Get(Context.System);

            // Subscribe to cluster events
            _clusterExtension.Subscribe(Self, ClusterEvent.InitialStateAsEvents, 
                typeof(ClusterEvent.MemberUp),
                typeof(ClusterEvent.MemberRemoved),
                typeof(ClusterEvent.UnreachableMember),
                typeof(ClusterEvent.ReachableMember));

            ReceiveAsync<ClusterEvent.MemberUp>(HandleMemberUp);
            ReceiveAsync<ClusterEvent.MemberRemoved>(HandleMemberRemoved);
            ReceiveAsync<ClusterEvent.UnreachableMember>(HandleUnreachableMember);
            ReceiveAsync<ClusterEvent.ReachableMember>(HandleReachableMember);

            _logger.LogDebug("ClusterManagerActor initialized");
        }

        private async System.Threading.Tasks.Task<bool> HandleMemberUp(ClusterEvent.MemberUp memberUp)
        {
            _logger.LogInformation("Member joined cluster: {Address}", memberUp.Member.Address);
            
            // Notify the cluster service about the membership change
            var args = new ClusterMembershipChangedEventArgs(
                MembershipChangeType.NodeJoined, 
                memberUp.Member.Address.ToString());

            // Use reflection to access the private method since we can't make it public
            var method = typeof(AkkaAIAgentCluster).GetMethod("OnClusterMembershipChanged", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(_cluster, new object[] { args });

            return true;
        }

        private async System.Threading.Tasks.Task<bool> HandleMemberRemoved(ClusterEvent.MemberRemoved memberRemoved)
        {
            _logger.LogInformation("Member left cluster: {Address}", memberRemoved.Member.Address);
            
            var args = new ClusterMembershipChangedEventArgs(
                MembershipChangeType.NodeLeft, 
                memberRemoved.Member.Address.ToString());

            var method = typeof(AkkaAIAgentCluster).GetMethod("OnClusterMembershipChanged", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(_cluster, new object[] { args });

            return true;
        }

        private async System.Threading.Tasks.Task<bool> HandleUnreachableMember(ClusterEvent.UnreachableMember unreachable)
        {
            _logger.LogWarning("Member became unreachable: {Address}", unreachable.Member.Address);
            return true;
        }

        private async System.Threading.Tasks.Task<bool> HandleReachableMember(ClusterEvent.ReachableMember reachable)
        {
            _logger.LogInformation("Member became reachable again: {Address}", reachable.Member.Address);
            return true;
        }

        protected override void PostStop()
        {
            _clusterExtension.Unsubscribe(Self);
            base.PostStop();
        }
    }
}