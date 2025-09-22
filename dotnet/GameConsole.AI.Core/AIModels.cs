using System;
using System.Collections.Generic;

namespace GameConsole.AI.Core
{
    /// <summary>
    /// Represents the current state of an AI agent.
    /// </summary>
    public enum AIAgentState
    {
        /// <summary>
        /// Agent is not yet initialized.
        /// </summary>
        Uninitialized,

        /// <summary>
        /// Agent is initializing.
        /// </summary>
        Initializing,

        /// <summary>
        /// Agent is idle and ready to process requests.
        /// </summary>
        Idle,

        /// <summary>
        /// Agent is actively processing a request.
        /// </summary>
        Processing,

        /// <summary>
        /// Agent is coordinating with other agents.
        /// </summary>
        Coordinating,

        /// <summary>
        /// Agent encountered an error.
        /// </summary>
        Error,

        /// <summary>
        /// Agent is shutting down.
        /// </summary>
        ShuttingDown,

        /// <summary>
        /// Agent has been shut down.
        /// </summary>
        Shutdown
    }

    /// <summary>
    /// Message sent to an AI agent for processing.
    /// </summary>
    public class AIAgentMessage
    {
        public string MessageId { get; }
        public string Content { get; }
        public string MessageType { get; }
        public DateTime Timestamp { get; }
        public Dictionary<string, object>? Metadata { get; }

        public AIAgentMessage(string messageId, string content, string messageType, DateTime timestamp, Dictionary<string, object>? metadata = null)
        {
            MessageId = messageId;
            Content = content;
            MessageType = messageType;
            Timestamp = timestamp;
            Metadata = metadata;
        }
    }

    /// <summary>
    /// Response from an AI agent after processing a message.
    /// </summary>
    public class AIAgentResponse
    {
        public string MessageId { get; }
        public string AgentId { get; }
        public string Content { get; }
        public AIResponseType ResponseType { get; }
        public DateTime Timestamp { get; }
        public bool Success { get; }
        public string? ErrorMessage { get; }
        public Dictionary<string, object>? Metadata { get; }

        public AIAgentResponse(string messageId, string agentId, string content, AIResponseType responseType, DateTime timestamp, bool success, string? errorMessage = null, Dictionary<string, object>? metadata = null)
        {
            MessageId = messageId;
            AgentId = agentId;
            Content = content;
            ResponseType = responseType;
            Timestamp = timestamp;
            Success = success;
            ErrorMessage = errorMessage;
            Metadata = metadata;
        }
    }

    /// <summary>
    /// Type of response from an AI agent.
    /// </summary>
    public enum AIResponseType
    {
        /// <summary>
        /// Informational response.
        /// </summary>
        Information,

        /// <summary>
        /// Action or command response.
        /// </summary>
        Action,

        /// <summary>
        /// Query result response.
        /// </summary>
        QueryResult,

        /// <summary>
        /// Error response.
        /// </summary>
        Error,

        /// <summary>
        /// Coordination request to other agents.
        /// </summary>
        Coordination
    }

    /// <summary>
    /// Current status information for an AI agent.
    /// </summary>
    public class AIAgentStatus
    {
        public string AgentId { get; }
        public string Name { get; }
        public AIAgentState State { get; }
        public DateTime LastActivity { get; }
        public string? CurrentTask { get; }
        public TimeSpan Uptime { get; }
        public Dictionary<string, object> Metrics { get; }

        public AIAgentStatus(string agentId, string name, AIAgentState state, DateTime lastActivity, string? currentTask, TimeSpan uptime, Dictionary<string, object> metrics)
        {
            AgentId = agentId;
            Name = name;
            State = state;
            LastActivity = lastActivity;
            CurrentTask = currentTask;
            Uptime = uptime;
            Metrics = metrics;
        }
    }

    /// <summary>
    /// Information about an AI agent in the cluster.
    /// </summary>
    public class AIAgentInfo
    {
        public string AgentId { get; }
        public string Name { get; }
        public string Address { get; }
        public AIAgentState State { get; }
        public DateTime LastSeen { get; }
        public IReadOnlyDictionary<string, string> Capabilities { get; }

        public AIAgentInfo(string agentId, string name, string address, AIAgentState state, DateTime lastSeen, IReadOnlyDictionary<string, string> capabilities)
        {
            AgentId = agentId;
            Name = name;
            Address = address;
            State = state;
            LastSeen = lastSeen;
            Capabilities = capabilities;
        }
    }

    /// <summary>
    /// Information about an AI agent cluster.
    /// </summary>
    public class AIClusterInfo
    {
        public string ClusterId { get; }
        public string ClusterName { get; }
        public IReadOnlyList<AIAgentInfo> Members { get; }
        public string LeaderAgentId { get; }
        public DateTime ClusterFormationTime { get; }
        public ClusterState State { get; }

        public AIClusterInfo(string clusterId, string clusterName, IReadOnlyList<AIAgentInfo> members, string leaderAgentId, DateTime clusterFormationTime, ClusterState state)
        {
            ClusterId = clusterId;
            ClusterName = clusterName;
            Members = members;
            LeaderAgentId = leaderAgentId;
            ClusterFormationTime = clusterFormationTime;
            State = state;
        }
    }

    /// <summary>
    /// State of an AI agent cluster.
    /// </summary>
    public enum ClusterState
    {
        /// <summary>
        /// Cluster is forming.
        /// </summary>
        Forming,

        /// <summary>
        /// Cluster is active and operational.
        /// </summary>
        Active,

        /// <summary>
        /// Cluster is rebalancing after member changes.
        /// </summary>
        Rebalancing,

        /// <summary>
        /// Cluster has issues and is degraded.
        /// </summary>
        Degraded,

        /// <summary>
        /// Cluster is shutting down.
        /// </summary>
        ShuttingDown
    }

    /// <summary>
    /// Represents a distributed task that requires coordination between multiple agents.
    /// </summary>
    public class AIDistributedTask
    {
        public string TaskId { get; }
        public string TaskType { get; }
        public Dictionary<string, object> Parameters { get; }
        public TimeSpan Timeout { get; }
        public int RequiredParticipants { get; }

        public AIDistributedTask(string taskId, string taskType, Dictionary<string, object> parameters, TimeSpan timeout, int requiredParticipants = 1)
        {
            TaskId = taskId;
            TaskType = taskType;
            Parameters = parameters;
            Timeout = timeout;
            RequiredParticipants = requiredParticipants;
        }
    }

    /// <summary>
    /// Result of coordinated task execution.
    /// </summary>
    public class AICoordinationResult
    {
        public string TaskId { get; }
        public bool Success { get; }
        public IReadOnlyList<string> ParticipatingAgents { get; }
        public Dictionary<string, object> Results { get; }
        public TimeSpan ExecutionTime { get; }
        public string? ErrorMessage { get; }

        public AICoordinationResult(string taskId, bool success, IReadOnlyList<string> participatingAgents, Dictionary<string, object> results, TimeSpan executionTime, string? errorMessage = null)
        {
            TaskId = taskId;
            Success = success;
            ParticipatingAgents = participatingAgents;
            Results = results;
            ExecutionTime = executionTime;
            ErrorMessage = errorMessage;
        }
    }
}