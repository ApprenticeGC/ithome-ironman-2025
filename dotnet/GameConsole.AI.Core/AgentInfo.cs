using System;
using System.Collections.Generic;

namespace GameConsole.AI.Core
{
    /// <summary>
    /// Contains information about an AI agent in the cluster.
    /// </summary>
    public class AgentInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AgentInfo"/> class.
        /// </summary>
        /// <param name="agentId">The unique identifier of the agent.</param>
        /// <param name="agentType">The type/category of the agent.</param>
        /// <param name="nodeId">The ID of the cluster node hosting this agent.</param>
        /// <param name="isActive">Indicates whether the agent is currently active.</param>
        public AgentInfo(string agentId, string agentType, string nodeId, bool isActive)
        {
            AgentId = agentId;
            AgentType = agentType;
            NodeId = nodeId;
            IsActive = isActive;
            LastSeen = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Gets the unique identifier of the agent.
        /// </summary>
        public string AgentId { get; }

        /// <summary>
        /// Gets the type/category of the agent.
        /// </summary>
        public string AgentType { get; }

        /// <summary>
        /// Gets the ID of the cluster node hosting this agent.
        /// </summary>
        public string NodeId { get; }

        /// <summary>
        /// Gets a value indicating whether the agent is currently active.
        /// </summary>
        public bool IsActive { get; }

        /// <summary>
        /// Gets the timestamp when this agent was last seen active.
        /// </summary>
        public DateTimeOffset LastSeen { get; }

        /// <summary>
        /// Gets optional metadata about the agent.
        /// </summary>
        public Dictionary<string, object>? Metadata { get; }
    }
}