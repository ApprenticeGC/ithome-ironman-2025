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
    /// Akka.NET-based implementation of AI orchestration service.
    /// Manages AI agents in a clustered environment with actor-based communication.
    /// </summary>
    public class AkkaAIOrchestrationService : IAIOrchestrationService
    {
        private readonly ILogger<AkkaAIOrchestrationService> _logger;
        private readonly ConcurrentDictionary<string, IActorRef> _agents;
        private ActorSystem? _actorSystem;
        private Cluster? _cluster;
        private DateTime _startTime;
        private bool _disposed;

        public bool IsRunning { get; private set; }

        public AkkaAIOrchestrationService(ILogger<AkkaAIOrchestrationService> logger)
        {
            _logger = logger;
            _agents = new ConcurrentDictionary<string, IActorRef>();
            _startTime = DateTime.UtcNow;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Initializing Akka AI Orchestration Service");

            // Create actor system with clustering configuration
            var config = CreateAkkaConfig();
            _actorSystem = ActorSystem.Create("GameConsoleAI", config);
            _cluster = Cluster.Get(_actorSystem);

            _logger.LogInformation("Akka AI Orchestration Service initialized");
            await Task.CompletedTask;
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_actorSystem == null)
            {
                throw new InvalidOperationException("Service must be initialized before starting");
            }

            _logger.LogInformation("Starting Akka AI Orchestration Service");
            IsRunning = true;
            _startTime = DateTime.UtcNow;
            
            _logger.LogInformation("Akka AI Orchestration Service started");
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Stopping Akka AI Orchestration Service");
            IsRunning = false;

            // Stop all agents
            var stopTasks = _agents.Values.Select(agent => agent.GracefulStop(TimeSpan.FromSeconds(5)));
            await Task.WhenAll(stopTasks);
            
            _agents.Clear();

            // Shutdown actor system
            if (_actorSystem != null)
            {
                await _actorSystem.Terminate();
            }

            _logger.LogInformation("Akka AI Orchestration Service stopped");
        }

        public async Task<IAIAgent> CreateAgentAsync(string agentType, Dictionary<string, object> configuration, CancellationToken cancellationToken = default)
        {
            if (_actorSystem == null)
            {
                throw new InvalidOperationException("Service not initialized");
            }

            var agentId = configuration.ContainsKey("AgentId") ? configuration["AgentId"] as string ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString();
            var agentName = configuration.ContainsKey("Name") ? configuration["Name"] as string ?? $"Agent-{agentId.Substring(0, 8)}" : $"Agent-{agentId.Substring(0, 8)}";

            _logger.LogInformation("Creating AI agent {AgentId} of type {AgentType}", agentId, agentType);

            var agentRef = _actorSystem.ActorOf(Props.Create<AIAgentActor>(agentId, agentName, _logger), $"agent-{agentId}");
            _agents[agentId] = agentRef;

            var agent = new ActorRefAIAgent(agentId, agentName, agentRef);
            
            _logger.LogInformation("Created AI agent {AgentId} ({AgentName})", agentId, agentName);
            await Task.CompletedTask;
            return agent;
        }

        public async Task<IAIAgent?> GetAgentAsync(string agentId, CancellationToken cancellationToken = default)
        {
            if (_agents.TryGetValue(agentId, out var agentRef))
            {
                try
                {
                    var status = await agentRef.Ask<AIAgentStatus>(new StatusRequest(), cancellationToken);
                    return new ActorRefAIAgent(status.AgentId, status.Name, agentRef);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get status for agent {AgentId}", agentId);
                    return null;
                }
            }
            return null;
        }

        public async Task<IEnumerable<IAIAgent>> GetAllAgentsAsync(CancellationToken cancellationToken = default)
        {
            var agents = new List<IAIAgent>();
            
            foreach (var kvp in _agents)
            {
                try
                {
                    var status = await kvp.Value.Ask<AIAgentStatus>(new StatusRequest(), cancellationToken);
                    agents.Add(new ActorRefAIAgent(status.AgentId, status.Name, kvp.Value));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get status for agent {AgentId}", kvp.Key);
                }
            }

            return agents;
        }

        public async Task<bool> RemoveAgentAsync(string agentId, CancellationToken cancellationToken = default)
        {
            if (_agents.TryRemove(agentId, out var agentRef))
            {
                try
                {
                    await agentRef.GracefulStop(TimeSpan.FromSeconds(5));
                    _logger.LogInformation("Removed AI agent {AgentId}", agentId);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to gracefully stop agent {AgentId}", agentId);
                    return false;
                }
            }
            return false;
        }

        public async Task<IEnumerable<AIAgentResponse>> RouteMessageAsync(AIAgentMessage message, string? targetAgentId = null, CancellationToken cancellationToken = default)
        {
            var responses = new List<AIAgentResponse>();

            if (targetAgentId != null)
            {
                // Route to specific agent
                if (_agents.TryGetValue(targetAgentId, out var agentRef))
                {
                    try
                    {
                        var response = await agentRef.Ask<AIAgentResponse>(message, cancellationToken);
                        responses.Add(response);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to route message to agent {AgentId}", targetAgentId);
                        responses.Add(new AIAgentResponse(
                            message.MessageId,
                            targetAgentId,
                            "Failed to process message",
                            AIResponseType.Error,
                            DateTime.UtcNow,
                            false,
                            ex.Message
                        ));
                    }
                }
            }
            else
            {
                // Broadcast to all agents (simple implementation)
                var tasks = _agents.Select(async kvp =>
                {
                    try
                    {
                        return await kvp.Value.Ask<AIAgentResponse>(message, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to route message to agent {AgentId}", kvp.Key);
                        return new AIAgentResponse(
                            message.MessageId,
                            kvp.Key,
                            "Failed to process message",
                            AIResponseType.Error,
                            DateTime.UtcNow,
                            false,
                            ex.Message
                        );
                    }
                });

                responses.AddRange(await Task.WhenAll(tasks));
            }

            return responses;
        }

        public async Task<OrchestrationHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default)
        {
            var totalAgents = _agents.Count;
            var activeAgents = 0;
            var erroredAgents = 0;

            foreach (var kvp in _agents)
            {
                try
                {
                    var status = await kvp.Value.Ask<AIAgentStatus>(new StatusRequest(), TimeSpan.FromSeconds(2));
                    if (status.State == AIAgentState.Error)
                    {
                        erroredAgents++;
                    }
                    else if (status.State == AIAgentState.Idle || status.State == AIAgentState.Processing)
                    {
                        activeAgents++;
                    }
                }
                catch
                {
                    erroredAgents++;
                }
            }

            var systemMetrics = new Dictionary<string, object>
            {
                ["ActorSystemUptime"] = _actorSystem?.StartTime != null ? DateTime.UtcNow - _actorSystem.StartTime : TimeSpan.Zero,
                ["ClusterMembers"] = _cluster?.State.Members.Count ?? 0,
                ["ClusterLeader"] = _cluster?.State.Leader?.ToString() ?? "None"
            };

            return new OrchestrationHealthStatus(
                erroredAgents == 0,
                totalAgents,
                activeAgents,
                erroredAgents,
                DateTime.UtcNow - _startTime,
                systemMetrics
            );
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            await StopAsync();
            _disposed = true;
        }

        private static Akka.Configuration.Config CreateAkkaConfig()
        {
            return Akka.Configuration.ConfigurationFactory.ParseString(@"
                akka {
                    actor {
                        provider = ""Akka.Cluster.ClusterActorRefProvider, Akka.Cluster""
                    }
                    
                    remote {
                        helios.tcp {
                            port = 0
                            hostname = localhost
                        }
                    }
                    
                    cluster {
                        seed-nodes = [""akka.tcp://GameConsoleAI@localhost:2551""]
                        min-nr-of-members = 1
                        
                        auto-down-unreachable-after = 10s
                    }
                    
                    loglevel = INFO
                }
            ");
        }
    }

    /// <summary>
    /// Wrapper class to make an ActorRef behave like an IAIAgent.
    /// </summary>
    internal class ActorRefAIAgent : IAIAgent
    {
        private readonly IActorRef _actorRef;

        public string AgentId { get; }
        public string Name { get; }
        public AIAgentState State { get; private set; }

        public ActorRefAIAgent(string agentId, string name, IActorRef actorRef)
        {
            AgentId = agentId;
            Name = name;
            _actorRef = actorRef;
            State = AIAgentState.Idle;
        }

        public async Task<AIAgentResponse> ProcessMessageAsync(AIAgentMessage message, CancellationToken cancellationToken = default)
        {
            return await _actorRef.Ask<AIAgentResponse>(message, cancellationToken);
        }

        public async Task<AIAgentStatus> GetStatusAsync(CancellationToken cancellationToken = default)
        {
            var status = await _actorRef.Ask<AIAgentStatus>(new StatusRequest(), cancellationToken);
            State = status.State;
            return status;
        }
    }
}