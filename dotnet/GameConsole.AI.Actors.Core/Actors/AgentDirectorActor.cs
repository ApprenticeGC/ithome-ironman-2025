using Akka.Actor;
using Akka.Cluster;
using Akka.Cluster.Sharding;
using GameConsole.AI.Actors.Core.Messages;
using GameConsole.AI.Actors.Core.Configuration;
using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Actors.Core.Actors;

/// <summary>
/// Agent Director actor responsible for supervising and managing AI agents in the cluster.
/// This actor serves as the top-level supervisor for all AI agent instances.
/// </summary>
public class AgentDirectorActor : ReceiveActor
{
    private readonly ILogger<AgentDirectorActor> _logger;
    private readonly AIActorClusterConfig _config;
    private readonly Dictionary<string, IActorRef> _activeAgents = new();
    private readonly Dictionary<string, AgentConfig> _agentConfigs = new();

    public AgentDirectorActor(ILogger<AgentDirectorActor> logger, AIActorClusterConfig config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));

        SetupMessageHandlers();
        
        _logger.LogInformation("AgentDirectorActor started on node {NodeAddress}", 
            Cluster.Get(Context.System).SelfAddress);
    }

    private void SetupMessageHandlers()
    {
        Receive<StartAgent>(HandleStartAgent);
        Receive<StopAgent>(HandleStopAgent);
        Receive<ProcessRequest>(HandleProcessRequest);
        Receive<GetClusterState>(HandleGetClusterState);
        Receive<RebalanceAgents>(HandleRebalanceAgents);
        Receive<Terminated>(HandleTerminated);
    }

    private void HandleStartAgent(StartAgent message)
    {
        try
        {
            _logger.LogInformation("Starting agent {AgentId} of type {AgentType}", 
                message.Config.AgentId, message.Config.AgentType);

            if (_activeAgents.ContainsKey(message.Config.AgentId))
            {
                _logger.LogWarning("Agent {AgentId} is already running", message.Config.AgentId);
                Sender.Tell(new AgentStopped(message.Config.AgentId, "Agent already exists"));
                return;
            }

            // Create the agent actor based on type
            var agentProps = CreateAgentProps(message.Config);
            var agentRef = Context.ActorOf(agentProps, message.Config.AgentId);
            
            // Monitor the agent for termination
            Context.Watch(agentRef);
            
            // Store agent reference and configuration
            _activeAgents[message.Config.AgentId] = agentRef;
            _agentConfigs[message.Config.AgentId] = message.Config;

            var response = new AgentStarted(message.Config.AgentId, agentRef);
            Sender.Tell(response);

            _logger.LogInformation("Successfully started agent {AgentId}", message.Config.AgentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start agent {AgentId}", message.Config.AgentId);
            Sender.Tell(new ProcessFailed(Guid.NewGuid().ToString(), ex, message.Config.AgentId));
        }
    }

    private void HandleStopAgent(StopAgent message)
    {
        try
        {
            _logger.LogInformation("Stopping agent {AgentId}", message.AgentId);

            if (!_activeAgents.TryGetValue(message.AgentId, out var agentRef))
            {
                _logger.LogWarning("Agent {AgentId} not found", message.AgentId);
                Sender.Tell(new AgentStopped(message.AgentId, "Agent not found"));
                return;
            }

            // Stop the agent gracefully
            Context.Stop(agentRef);
            
            var response = new AgentStopped(message.AgentId, "Stopped by request");
            Sender.Tell(response);

            _logger.LogInformation("Successfully stopped agent {AgentId}", message.AgentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop agent {AgentId}", message.AgentId);
            Sender.Tell(new ProcessFailed(Guid.NewGuid().ToString(), ex, message.AgentId));
        }
    }

    private void HandleProcessRequest(ProcessRequest message)
    {
        try
        {
            _logger.LogDebug("Routing process request {RequestId} for agent type {AgentType}", 
                message.RequestId, message.AgentType);

            // Find an available agent of the requested type
            var availableAgent = FindAvailableAgent(message.AgentType);
            
            if (availableAgent == null)
            {
                _logger.LogWarning("No available agents of type {AgentType} for request {RequestId}", 
                    message.AgentType, message.RequestId);
                    
                var failedResponse = new ProcessFailed(
                    message.RequestId, 
                    new InvalidOperationException($"No available agents of type {message.AgentType}"), 
                    message.AgentType);
                    
                Sender.Tell(failedResponse);
                return;
            }

            // Forward the request to the selected agent
            availableAgent.Forward(message);
            
            _logger.LogDebug("Forwarded request {RequestId} to agent", message.RequestId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to route process request {RequestId}", message.RequestId);
            Sender.Tell(new ProcessFailed(message.RequestId, ex, "AgentDirector"));
        }
    }

    private void HandleGetClusterState(GetClusterState message)
    {
        try
        {
            var cluster = Cluster.Get(Context.System);
            var state = cluster.State;
            
            var nodes = state.Members
                .Where(m => m.Status == MemberStatus.Up)
                .Select(m => m.Address.ToString())
                .ToList();
                
            var unreachable = state.Unreachable
                .Select(m => m.Address.ToString())
                .ToList();
                
            var response = new ClusterStateResponse(nodes, state.Leader?.ToString(), unreachable);
            Sender.Tell(response);
            
            _logger.LogDebug("Returned cluster state: {NodeCount} nodes, {UnreachableCount} unreachable", 
                nodes.Count, unreachable.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cluster state");
            Sender.Tell(new ProcessFailed(Guid.NewGuid().ToString(), ex, "AgentDirector"));
        }
    }

    private void HandleRebalanceAgents(RebalanceAgents message)
    {
        try
        {
            _logger.LogInformation("Starting agent rebalancing across cluster nodes");
            
            var startTime = DateTime.UtcNow;
            var movedAgents = 0;
            
            // In a real implementation, this would redistribute agents based on cluster load
            // For now, we'll just return a completed status
            
            var duration = DateTime.UtcNow - startTime;
            var response = new RebalanceCompleted(movedAgents, duration);
            Sender.Tell(response);
            
            _logger.LogInformation("Completed agent rebalancing: {MovedAgents} agents moved in {Duration}", 
                movedAgents, duration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rebalance agents");
            Sender.Tell(new ProcessFailed(Guid.NewGuid().ToString(), ex, "AgentDirector"));
        }
    }

    private void HandleTerminated(Terminated message)
    {
        var agentId = message.ActorRef.Path.Name;
        
        if (_activeAgents.ContainsKey(agentId))
        {
            _activeAgents.Remove(agentId);
            _agentConfigs.Remove(agentId);
            
            _logger.LogInformation("Agent {AgentId} terminated and removed from active agents", agentId);
        }
    }

    private Props CreateAgentProps(AgentConfig config)
    {
        // Factory method to create different types of agents based on configuration
        // For now, we'll create a generic AI agent actor
        return AIAgentActor.Props(_logger, config);
    }

    private IActorRef? FindAvailableAgent(string agentType)
    {
        // Simple round-robin selection for agents of the requested type
        return _activeAgents.Values
            .Where(agent => _agentConfigs.Values
                .Any(config => config.AgentType == agentType))
            .FirstOrDefault();
    }

    public static Props Props(ILogger<AgentDirectorActor> logger, AIActorClusterConfig config)
    {
        return Akka.Actor.Props.Create(() => new AgentDirectorActor(logger, config));
    }
}