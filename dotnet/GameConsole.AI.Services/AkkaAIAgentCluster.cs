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
    /// Akka.NET-based implementation of AI agent clustering service.
    /// Manages AI agents across multiple nodes using Akka.NET cluster functionality.
    /// </summary>
    public class AkkaAIAgentCluster : IAIAgentCluster
    {
        private readonly ILogger<AkkaAIAgentCluster> _logger;
        private ActorSystem? _actorSystem;
        private IActorRef? _clusterManager;
        private readonly ConcurrentDictionary<string, IAIAgent> _registeredAgents;
        private bool _isRunning;
        private volatile bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="AkkaAIAgentCluster"/> class.
        /// </summary>
        /// <param name="logger">Logger instance for diagnostics.</param>
        public AkkaAIAgentCluster(ILogger<AkkaAIAgentCluster> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _registeredAgents = new ConcurrentDictionary<string, IAIAgent>();
        }

        /// <inheritdoc />
        public bool IsRunning => _isRunning;

        /// <inheritdoc />
        public event EventHandler<ClusterMembershipChangedEventArgs>? ClusterMembershipChanged;

        /// <inheritdoc />
        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Initializing AI Agent Cluster with Akka.NET");

            var config = Akka.Configuration.ConfigurationFactory.ParseString(@"
                akka {
                    actor {
                        provider = ""Akka.Cluster.ClusterActorRefProvider, Akka.Cluster""
                    }
                    remote {
                        helios.tcp {
                            hostname = ""localhost""
                            port = 0
                        }
                    }
                    cluster {
                        seed-nodes = [""akka.tcp://AIAgentCluster@localhost:2551""]
                        auto-down-unreachable-after = 30s
                    }
                }
            ");

            _actorSystem = ActorSystem.Create("AIAgentCluster", config);
            
            // Create cluster manager actor
            _clusterManager = _actorSystem.ActorOf(
                Props.Create(() => new ClusterManagerActor(_logger, this)),
                "cluster-manager");

            _logger.LogInformation("AI Agent Cluster initialized successfully");
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_actorSystem == null)
                throw new InvalidOperationException("ActorSystem not initialized. Call InitializeAsync first.");

            _logger.LogInformation("Starting AI Agent Cluster");

            // Join the cluster
            var cluster = Cluster.Get(_actorSystem);
            cluster.Join(cluster.SelfAddress);

            _isRunning = true;
            _logger.LogInformation("AI Agent Cluster started successfully");
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Stopping AI Agent Cluster");
            _isRunning = false;

            if (_actorSystem != null)
            {
                var cluster = Cluster.Get(_actorSystem);
                cluster.Leave(cluster.SelfAddress);
                
                await _actorSystem.Terminate();
            }

            _logger.LogInformation("AI Agent Cluster stopped");
        }

        /// <inheritdoc />
        public Task<ClusterInfo> GetClusterInfoAsync(CancellationToken cancellationToken = default)
        {
            if (_actorSystem == null)
                throw new InvalidOperationException("Cluster not initialized");

            var cluster = Cluster.Get(_actorSystem);
            var members = cluster.State.Members.Where(m => m.Status == MemberStatus.Up).ToList();

            return Task.FromResult(new ClusterInfo(
                cluster.SelfAddress.ToString(),
                members.Count,
                _registeredAgents.Count,
                cluster.State.Leader?.Equals(cluster.SelfAddress) == true
            ));
        }

        /// <inheritdoc />
        public async Task RegisterAgentAsync(IAIAgent agent, CancellationToken cancellationToken = default)
        {
            if (agent == null)
                throw new ArgumentNullException(nameof(agent));

            _logger.LogInformation("Registering AI agent: {AgentId} of type {AgentType}", agent.AgentId, agent.AgentType);

            if (_registeredAgents.TryAdd(agent.AgentId, agent))
            {
                await agent.ActivateAsync(cancellationToken);
                
                var agentInfo = new AgentInfo(agent.AgentId, agent.AgentType, GetNodeId(), agent.IsActive);
                OnClusterMembershipChanged(new ClusterMembershipChangedEventArgs(
                    MembershipChangeType.AgentRegistered, GetNodeId(), agentInfo));
                
                _logger.LogInformation("Successfully registered AI agent: {AgentId}", agent.AgentId);
            }
            else
            {
                _logger.LogWarning("AI agent {AgentId} is already registered", agent.AgentId);
            }
        }

        /// <inheritdoc />
        public async Task UnregisterAgentAsync(string agentId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(agentId))
                throw new ArgumentException("Agent ID cannot be null or empty", nameof(agentId));

            _logger.LogInformation("Unregistering AI agent: {AgentId}", agentId);

            if (_registeredAgents.TryRemove(agentId, out var agent))
            {
                await agent.DeactivateAsync(cancellationToken);
                
                var agentInfo = new AgentInfo(agent.AgentId, agent.AgentType, GetNodeId(), false);
                OnClusterMembershipChanged(new ClusterMembershipChangedEventArgs(
                    MembershipChangeType.AgentUnregistered, GetNodeId(), agentInfo));
                
                _logger.LogInformation("Successfully unregistered AI agent: {AgentId}", agentId);
            }
            else
            {
                _logger.LogWarning("AI agent {AgentId} was not found for unregistration", agentId);
            }
        }

        /// <inheritdoc />
        public async Task<AIAgentResponse> RouteMessageAsync(
            AIAgentMessage message, 
            string? routingStrategy = null, 
            CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            _logger.LogDebug("Routing message {MessageId} with strategy {RoutingStrategy}", 
                message.Id, routingStrategy ?? "default");

            // Simple round-robin routing for now
            var activeAgents = _registeredAgents.Values.Where(a => a.IsActive).ToList();
            
            if (!activeAgents.Any())
            {
                return Task.FromResult(new AIAgentResponse(message.Id, "cluster", 
                    "No active agents available to process the message", false, "No active agents in cluster"));
            }

            var selectedAgent = activeAgents[message.Id.GetHashCode() % activeAgents.Count];
            return await selectedAgent.ProcessMessageAsync(message, cancellationToken);
        }

        /// <inheritdoc />
        public Task<IEnumerable<AgentInfo>> GetActiveAgentsAsync(CancellationToken cancellationToken = default)
        {
            var nodeId = GetNodeId();
            return Task.FromResult<IEnumerable<AgentInfo>>(_registeredAgents.Values
                .Where(agent => agent.IsActive)
                .Select(agent => new AgentInfo(agent.AgentId, agent.AgentType, nodeId, agent.IsActive))
                .ToList());
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            _disposed = true;
            
            if (_isRunning)
            {
                await StopAsync();
            }
        }

        private string GetNodeId()
        {
            return _actorSystem?.Settings.Config.GetString("akka.cluster.roles.0") ?? "local-node";
        }

        private void OnClusterMembershipChanged(ClusterMembershipChangedEventArgs args)
        {
            ClusterMembershipChanged?.Invoke(this, args);
        }
    }
}