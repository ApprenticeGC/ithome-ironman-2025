using System;

namespace GameConsole.AI.Core
{
    /// <summary>
    /// Event arguments for cluster membership changes.
    /// </summary>
    public class ClusterMembershipChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterMembershipChangedEventArgs"/> class.
        /// </summary>
        /// <param name="changeType">The type of membership change.</param>
        /// <param name="nodeId">The ID of the node that changed.</param>
        /// <param name="agentInfo">Optional information about an agent if the change was agent-specific.</param>
        public ClusterMembershipChangedEventArgs(MembershipChangeType changeType, string nodeId, AgentInfo? agentInfo = null)
        {
            ChangeType = changeType;
            NodeId = nodeId;
            AgentInfo = agentInfo;
            Timestamp = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Gets the type of membership change.
        /// </summary>
        public MembershipChangeType ChangeType { get; }

        /// <summary>
        /// Gets the ID of the node that changed.
        /// </summary>
        public string NodeId { get; }

        /// <summary>
        /// Gets optional information about an agent if the change was agent-specific.
        /// </summary>
        public AgentInfo? AgentInfo { get; }

        /// <summary>
        /// Gets the timestamp when the change occurred.
        /// </summary>
        public DateTimeOffset Timestamp { get; }
    }

    /// <summary>
    /// Specifies the type of cluster membership change.
    /// </summary>
    public enum MembershipChangeType
    {
        /// <summary>
        /// A new node joined the cluster.
        /// </summary>
        NodeJoined,

        /// <summary>
        /// A node left the cluster.
        /// </summary>
        NodeLeft,

        /// <summary>
        /// A new agent was registered.
        /// </summary>
        AgentRegistered,

        /// <summary>
        /// An agent was unregistered.
        /// </summary>
        AgentUnregistered,

        /// <summary>
        /// An agent's status changed (e.g., became active/inactive).
        /// </summary>
        AgentStatusChanged
    }
}