using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster;
using GameConsole.AI.Core;
using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Services
{
    /// <summary>
    /// Base Akka.NET actor implementation for AI agents.
    /// Provides clustering support and message handling for distributed AI agents.
    /// </summary>
    public class AIAgentActor : ReceiveActor, IAIAgent
    {
        private readonly ILogger<AIAgentActor> _logger;
        private AIAgentState _state;
        private readonly Dictionary<string, object> _metadata;
        private DateTime _lastActivity;
        private readonly Cluster _cluster;

        public string AgentId { get; }
        public string Name { get; }
        public AIAgentState State => _state;

        public AIAgentActor(string agentId, string name, ILogger<AIAgentActor> logger)
        {
            AgentId = agentId;
            Name = name;
            _logger = logger;
            _state = AIAgentState.Initializing;
            _metadata = new Dictionary<string, object>();
            _lastActivity = DateTime.UtcNow;
            _cluster = Cluster.Get(Context.System);

            // Set up message handlers
            Receive<AIAgentMessage>(HandleAgentMessage);
            Receive<StatusRequest>(HandleStatusRequest);
            Receive<ClusterJoinRequest>(HandleClusterJoin);
            Receive<ClusterLeaveRequest>(HandleClusterLeave);
            Receive<CoordinationRequest>(HandleCoordinationRequest);
            Receive<StateSync>(HandleStateSync);
            
            _logger.LogInformation("AI Agent Actor {AgentId} ({Name}) initialized", AgentId, Name);
        }

        protected override void PreStart()
        {
            base.PreStart();
            _state = AIAgentState.Idle;
            _logger.LogInformation("AI Agent Actor {AgentId} started", AgentId);
        }

        protected override void PostStop()
        {
            _state = AIAgentState.Shutdown;
            _logger.LogInformation("AI Agent Actor {AgentId} stopped", AgentId);
            base.PostStop();
        }

        public async Task<AIAgentResponse> ProcessMessageAsync(AIAgentMessage message, CancellationToken cancellationToken = default)
        {
            var response = await Self.Ask<AIAgentResponse>(message, cancellationToken);
            return response;
        }

        public async Task<AIAgentStatus> GetStatusAsync(CancellationToken cancellationToken = default)
        {
            var statusRequest = new StatusRequest();
            var status = await Self.Ask<AIAgentStatus>(statusRequest, cancellationToken);
            return status;
        }

        private bool HandleAgentMessage(AIAgentMessage message)
        {
            try
            {
                _logger.LogDebug("Processing message {MessageId} for agent {AgentId}", message.MessageId, AgentId);
                _state = AIAgentState.Processing;
                _lastActivity = DateTime.UtcNow;

                // Process the message (this is where the AI logic would go)
                var response = ProcessMessage(message);
                
                _state = AIAgentState.Idle;
                Sender.Tell(response);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message {MessageId} for agent {AgentId}", message.MessageId, AgentId);
                _state = AIAgentState.Error;
                
                var errorResponse = new AIAgentResponse(
                    message.MessageId,
                    AgentId,
                    "Error processing message",
                    AIResponseType.Error,
                    DateTime.UtcNow,
                    false,
                    ex.Message
                );
                
                Sender.Tell(errorResponse);
                return true;
            }
        }

        private bool HandleStatusRequest(StatusRequest request)
        {
            var uptime = DateTime.UtcNow - DateTime.UtcNow.AddSeconds(-60); // Simplified for demonstration
            var metrics = new Dictionary<string, object>
            {
                ["MessagesProcessed"] = _metadata.ContainsKey("MessagesProcessed") ? _metadata["MessagesProcessed"] : 0,
                ["LastActivity"] = _lastActivity,
                ["ClusterMember"] = _cluster.State.Members.Any(m => m.Address == _cluster.SelfAddress)
            };

            var status = new AIAgentStatus(
                AgentId,
                Name,
                _state,
                _lastActivity,
                _metadata.ContainsKey("CurrentTask") ? _metadata["CurrentTask"] as string : null,
                uptime,
                metrics
            );

            Sender.Tell(status);
            return true;
        }

        private bool HandleClusterJoin(ClusterJoinRequest request)
        {
            try
            {
                _logger.LogInformation("Agent {AgentId} joining cluster at {Address}", AgentId, request.ClusterAddress);
                var address = Address.Parse(request.ClusterAddress);
                _cluster.Join(address);
                
                Sender.Tell(new ClusterJoinResponse(true, "Successfully initiated cluster join"));
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to join cluster for agent {AgentId}", AgentId);
                Sender.Tell(new ClusterJoinResponse(false, ex.Message));
                return true;
            }
        }

        private bool HandleClusterLeave(ClusterLeaveRequest request)
        {
            try
            {
                _logger.LogInformation("Agent {AgentId} leaving cluster", AgentId);
                _cluster.Leave(_cluster.SelfAddress);
                
                Sender.Tell(new ClusterLeaveResponse(true, "Successfully initiated cluster leave"));
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to leave cluster for agent {AgentId}", AgentId);
                Sender.Tell(new ClusterLeaveResponse(false, ex.Message));
                return true;
            }
        }

        private bool HandleCoordinationRequest(CoordinationRequest request)
        {
            _logger.LogDebug("Handling coordination request {TaskId} for agent {AgentId}", request.Task.TaskId, AgentId);
            _state = AIAgentState.Coordinating;

            // Coordinate with other agents
            var result = CoordinateTask(request.Task, request.ParticipatingAgents);
            
            _state = AIAgentState.Idle;
            Sender.Tell(result);
            return true;
        }

        private bool HandleStateSync(StateSync stateSync)
        {
            _logger.LogDebug("Synchronizing state for agent {AgentId}", AgentId);
            
            // Update local state based on sync data
            _state = stateSync.State;
            foreach (var kvp in stateSync.Metadata)
            {
                _metadata[kvp.Key] = kvp.Value;
            }

            Sender.Tell(new StateSyncResponse(true, "State synchronized successfully"));
            return true;
        }

        private AIAgentResponse ProcessMessage(AIAgentMessage message)
        {
            // Increment message counter
            var messageCount = (int)(_metadata.ContainsKey("MessagesProcessed") ? _metadata["MessagesProcessed"] : 0) + 1;
            _metadata["MessagesProcessed"] = messageCount;

            // Simple echo response for demonstration
            // In a real implementation, this would contain AI processing logic
            var responseContent = $"Agent {Name} processed: {message.Content}";

            return new AIAgentResponse(
                message.MessageId,
                AgentId,
                responseContent,
                AIResponseType.Information,
                DateTime.UtcNow,
                true
            );
        }

        private AICoordinationResult CoordinateTask(AIDistributedTask task, IEnumerable<string> participatingAgents)
        {
            // Simple coordination implementation
            var agents = participatingAgents.ToList();
            var results = new Dictionary<string, object>
            {
                ["CoordinatingAgent"] = AgentId,
                ["TaskType"] = task.TaskType,
                ["ParticipantCount"] = agents.Count
            };

            return new AICoordinationResult(
                task.TaskId,
                true,
                agents,
                results,
                TimeSpan.FromMilliseconds(100) // Simulated execution time
            );
        }
    }

    // Internal message types for actor communication
    public class StatusRequest { }
    
    public class ClusterJoinRequest
    {
        public string ClusterAddress { get; }
        public ClusterJoinRequest(string clusterAddress) => ClusterAddress = clusterAddress;
    }
    
    public class ClusterJoinResponse
    {
        public bool Success { get; }
        public string Message { get; }
        public ClusterJoinResponse(bool success, string message)
        {
            Success = success;
            Message = message;
        }
    }
    
    public class ClusterLeaveRequest { }
    
    public class ClusterLeaveResponse
    {
        public bool Success { get; }
        public string Message { get; }
        public ClusterLeaveResponse(bool success, string message)
        {
            Success = success;
            Message = message;
        }
    }
    
    public class CoordinationRequest
    {
        public AIDistributedTask Task { get; }
        public IEnumerable<string> ParticipatingAgents { get; }
        public CoordinationRequest(AIDistributedTask task, IEnumerable<string> participatingAgents)
        {
            Task = task;
            ParticipatingAgents = participatingAgents;
        }
    }
    
    public class StateSync
    {
        public AIAgentState State { get; }
        public Dictionary<string, object> Metadata { get; }
        public StateSync(AIAgentState state, Dictionary<string, object> metadata)
        {
            State = state;
            Metadata = metadata;
        }
    }
    
    public class StateSyncResponse
    {
        public bool Success { get; }
        public string Message { get; }
        public StateSyncResponse(bool success, string message)
        {
            Success = success;
            Message = message;
        }
    }
}