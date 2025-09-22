using System;
using System.Collections.Generic;

namespace GameConsole.AI.Core
{
    /// <summary>
    /// Contains information about the current state of the AI agent cluster.
    /// </summary>
    public class ClusterInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterInfo"/> class.
        /// </summary>
        /// <param name="nodeId">The ID of the current cluster node.</param>
        /// <param name="totalNodes">The total number of nodes in the cluster.</param>
        /// <param name="totalAgents">The total number of agents across all nodes.</param>
        /// <param name="isLeader">Indicates whether this node is the cluster leader.</param>
        public ClusterInfo(string nodeId, int totalNodes, int totalAgents, bool isLeader)
        {
            NodeId = nodeId;
            TotalNodes = totalNodes;
            TotalAgents = totalAgents;
            IsLeader = isLeader;
            Timestamp = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Gets the ID of the current cluster node.
        /// </summary>
        public string NodeId { get; }

        /// <summary>
        /// Gets the total number of nodes in the cluster.
        /// </summary>
        public int TotalNodes { get; }

        /// <summary>
        /// Gets the total number of agents across all nodes.
        /// </summary>
        public int TotalAgents { get; }

        /// <summary>
        /// Gets a value indicating whether this node is the cluster leader.
        /// </summary>
        public bool IsLeader { get; }

        /// <summary>
        /// Gets the timestamp when this information was generated.
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Gets optional additional metadata about the cluster state.
        /// </summary>
        public Dictionary<string, object>? Metadata { get; }
    }
}