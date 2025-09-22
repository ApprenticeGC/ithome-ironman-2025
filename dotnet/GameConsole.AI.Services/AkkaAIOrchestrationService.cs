using Akka.Actor;
using Akka.Cluster;
using GameConsole.AI.Core;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.AI.Services;

/// <summary>
/// Akka.NET-based implementation of AI orchestration service.
/// Manages AI agents in a clustered environment with actor-based communication.
/// </summary>
public class AkkaAIOrchestrationService : IAIOrchestrationService, IAIClusteringCapability, IAICoordinationCapability
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

        var agentId = configuration.GetValueOrDefault("AgentId", Guid.NewGuid().ToString()) as string ?? Guid.NewGuid().ToString();
        var agentName = configuration.GetValueOrDefault("Name", $"Agent-{agentId[..8]}") as string ?? $"Agent-{agentId[..8]}";

        _logger.LogInformation("Creating AI agent {AgentId} of type {AgentType}", agentId, agentType);

        var agentRef = _actorSystem.ActorOf(Props.Create<AIAgentActor>(agentId, agentName, _logger), $"agent-{agentId}");
        _agents[agentId] = agentRef;

        var agent = new ActorRefAIAgent(agentId, agentName, agentRef);
        
        _logger.LogInformation("Created AI agent {AgentId} ({AgentName})", agentId, agentName);
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
            ["ActorSystemUptime"] = DateTime.UtcNow - _actorSystem?.StartTime,
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

    // IAIClusteringCapability implementation
    public async Task JoinClusterAsync(string clusterAddress, CancellationToken cancellationToken = default)
    {
        if (_cluster == null)
        {
            throw new InvalidOperationException("Cluster not available");
        }

        _logger.LogInformation("Joining cluster at {Address}", clusterAddress);
        var address = Address.Parse(clusterAddress);
        _cluster.Join(address);
    }

    public async Task LeaveClusterAsync(CancellationToken cancellationToken = default)
    {
        if (_cluster == null)
        {
            throw new InvalidOperationException("Cluster not available");
        }

        _logger.LogInformation("Leaving cluster");
        _cluster.Leave(_cluster.SelfAddress);
    }

    public async Task<AIClusterInfo?> GetClusterInfoAsync(CancellationToken cancellationToken = default)
    {
        if (_cluster == null)
        {
            return null;
        }

        var members = _cluster.State.Members.Select(m => new AIAgentInfo(
            m.Address.ToString(),
            $"Member-{m.Address.Port}",
            m.Address.ToString(),
            AIAgentState.Idle, // Simplified - would need actual agent state
            DateTime.UtcNow,
            new Dictionary<string, string> { ["Roles"] = string.Join(",", m.Roles) }
        )).ToList();

        return new AIClusterInfo(
            _cluster.SelfAddress.System,
            "GameConsole AI Cluster",
            members,
            _cluster.State.Leader?.ToString() ?? "",
            _cluster.State.Members.Min(m => m.UpNumber) != 0 ? DateTime.UtcNow : DateTime.UtcNow, // Simplified
            _cluster.State.Members.Count > 0 ? ClusterState.Active : ClusterState.Forming
        );
    }

    public async Task<IEnumerable<AIAgentInfo>> DiscoverClusterAgentsAsync(CancellationToken cancellationToken = default)
    {
        var clusterInfo = await GetClusterInfoAsync(cancellationToken);
        return clusterInfo?.Members ?? Enumerable.Empty<AIAgentInfo>();
    }

    // IAICoordinationCapability implementation
    public async Task<AICoordinationResult> CoordinateTaskAsync(AIDistributedTask task, IEnumerable<string> participatingAgents, CancellationToken cancellationToken = default)
    {
        var agents = participatingAgents.ToList();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("Coordinating task {TaskId} with {AgentCount} agents", task.TaskId, agents.Count);

        var coordinationTasks = agents.Select(async agentId =>
        {
            if (_agents.TryGetValue(agentId, out var agentRef))
            {
                try
                {
                    return await agentRef.Ask<AICoordinationResult>(
                        new CoordinationRequest(task, agents), 
                        task.Timeout, 
                        cancellationToken
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to coordinate with agent {AgentId}", agentId);
                    return new AICoordinationResult(task.TaskId, false, new[] { agentId }, new Dictionary<string, object>(), TimeSpan.Zero, ex.Message);
                }
            }
            return new AICoordinationResult(task.TaskId, false, new[] { agentId }, new Dictionary<string, object>(), TimeSpan.Zero, "Agent not found");
        });

        var results = await Task.WhenAll(coordinationTasks);
        var successfulResults = results.Where(r => r.Success).ToList();

        return new AICoordinationResult(
            task.TaskId,
            successfulResults.Count >= task.RequiredParticipants,
            agents,
            new Dictionary<string, object>
            {
                ["SuccessfulParticipants"] = successfulResults.Count,
                ["TotalParticipants"] = agents.Count,
                ["Results"] = successfulResults
            },
            DateTime.UtcNow - startTime
        );
    }

    public async Task<string> ElectLeaderAsync(IEnumerable<string> candidateAgents, CancellationToken cancellationToken = default)
    {
        // Simple leader election - in practice this would use Akka's cluster leader election
        var candidates = candidateAgents.ToList();
        
        if (!candidates.Any())
        {
            throw new InvalidOperationException("No candidate agents provided for leader election");
        }

        // For simplicity, elect the first available candidate
        var leader = candidates.First();
        _logger.LogInformation("Elected agent {AgentId} as leader from {CandidateCount} candidates", leader, candidates.Count);
        
        return leader;
    }

    public async Task SynchronizeStateAsync(AIAgentState stateData, IEnumerable<string> targetAgents, CancellationToken cancellationToken = default)
    {
        var agents = targetAgents.ToList();
        _logger.LogInformation("Synchronizing state to {AgentCount} agents", agents.Count);

        var syncTasks = agents.Select(async agentId =>
        {
            if (_agents.TryGetValue(agentId, out var agentRef))
            {
                try
                {
                    var stateSync = new StateSync(stateData, new Dictionary<string, object>());
                    await agentRef.Ask<StateSyncResponse>(stateSync, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to sync state with agent {AgentId}", agentId);
                }
            }
        });

        await Task.WhenAll(syncTasks);
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