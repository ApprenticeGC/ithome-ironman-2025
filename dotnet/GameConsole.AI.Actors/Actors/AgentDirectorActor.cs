using Akka.Actor;
using Microsoft.Extensions.Logging;
using GameConsole.AI.Actors.Messages;

namespace GameConsole.AI.Actors.Actors;

/// <summary>
/// Agent Director actor - supervises and coordinates AI agent instances.
/// This is the top-level supervisor for all AI agents in the system.
/// </summary>
public class AgentDirectorActor : ReceiveActor
{
    private readonly ILogger<AgentDirectorActor> _logger;
    private readonly Dictionary<string, IActorRef> _agents = new();
    private readonly Dictionary<string, AgentMetadata> _agentMetadata = new();

    public AgentDirectorActor(ILogger<AgentDirectorActor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Receive<InvokeAgent>(HandleInvokeAgent);
        Receive<StreamAgent>(HandleStreamAgent);
        Receive<GetAvailableAgents>(HandleGetAvailableAgents);
        Receive<GetAgentInfo>(HandleGetAgentInfo);
        Receive<RegisterAgent>(HandleRegisterAgent);
        Receive<UnregisterAgent>(HandleUnregisterAgent);
        Receive<Terminated>(HandleTerminated);

        _logger.LogInformation("AgentDirectorActor started");
    }

    private void HandleInvokeAgent(InvokeAgent message)
    {
        _logger.LogDebug("Routing InvokeAgent request for {AgentId}", message.AgentId);

        if (_agents.TryGetValue(message.AgentId, out var agentRef))
        {
            agentRef.Forward(message);
        }
        else
        {
            _logger.LogWarning("Agent {AgentId} not found", message.AgentId);
            var errorResponse = new AgentResponse(message.AgentId, "", false, $"Agent {message.AgentId} not found");
            Sender.Tell(errorResponse);
        }
    }

    private void HandleStreamAgent(StreamAgent message)
    {
        _logger.LogDebug("Routing StreamAgent request for {AgentId}", message.AgentId);

        if (_agents.TryGetValue(message.AgentId, out var agentRef))
        {
            agentRef.Forward(message);
        }
        else
        {
            _logger.LogWarning("Agent {AgentId} not found for streaming", message.AgentId);
            var errorChunk = new AgentStreamChunk(message.AgentId, $"Error: Agent {message.AgentId} not found", true);
            Sender.Tell(errorChunk);
        }
    }

    private void HandleGetAvailableAgents(GetAvailableAgents message)
    {
        _logger.LogDebug("Getting available agents");
        var availableAgents = _agentMetadata.Where(kvp => kvp.Value.IsAvailable).Select(kvp => kvp.Key);
        var response = new AvailableAgentsResponse(availableAgents);
        Sender.Tell(response);
    }

    private void HandleGetAgentInfo(GetAgentInfo message)
    {
        _logger.LogDebug("Getting info for agent {AgentId}", message.AgentId);

        if (_agentMetadata.TryGetValue(message.AgentId, out var metadata))
        {
            var response = new AgentInfoResponse(message.AgentId, metadata);
            Sender.Tell(response);
        }
        else if (_agents.TryGetValue(message.AgentId, out var agentRef))
        {
            // Forward to the agent if we don't have cached metadata
            agentRef.Forward(message);
        }
        else
        {
            _logger.LogWarning("Agent {AgentId} not found for info request", message.AgentId);
            var errorMetadata = new AgentMetadata(message.AgentId, "Unknown", "Agent not found", "0.0.0", Array.Empty<string>(), false);
            var response = new AgentInfoResponse(message.AgentId, errorMetadata);
            Sender.Tell(response);
        }
    }

    private void HandleRegisterAgent(RegisterAgent message)
    {
        _logger.LogInformation("Registering agent {AgentId}", message.AgentId);

        _agents[message.AgentId] = message.AgentRef;
        _agentMetadata[message.AgentId] = message.Metadata;
        
        // Watch the agent for termination
        Context.Watch(message.AgentRef);

        Sender.Tell(new AgentRegistered(message.AgentId, true));
    }

    private void HandleUnregisterAgent(UnregisterAgent message)
    {
        _logger.LogInformation("Unregistering agent {AgentId}", message.AgentId);

        if (_agents.TryGetValue(message.AgentId, out var agentRef))
        {
            Context.Unwatch(agentRef);
            _agents.Remove(message.AgentId);
            _agentMetadata.Remove(message.AgentId);
            Sender.Tell(new AgentUnregistered(message.AgentId, true));
        }
        else
        {
            _logger.LogWarning("Attempted to unregister unknown agent {AgentId}", message.AgentId);
            Sender.Tell(new AgentUnregistered(message.AgentId, false));
        }
    }

    private void HandleTerminated(Terminated message)
    {
        var terminatedAgent = _agents.FirstOrDefault(kvp => kvp.Value.Equals(message.ActorRef));
        if (!terminatedAgent.Equals(default(KeyValuePair<string, IActorRef>)))
        {
            _logger.LogWarning("Agent {AgentId} terminated unexpectedly", terminatedAgent.Key);
            _agents.Remove(terminatedAgent.Key);
            _agentMetadata.Remove(terminatedAgent.Key);
        }
    }

    protected override SupervisorStrategy SupervisorStrategy()
    {
        return new OneForOneStrategy(
            maxNrOfRetries: 3,
            withinTimeRange: TimeSpan.FromMinutes(1),
            localOnlyDecider: exception =>
            {
                _logger.LogError(exception, "Child agent failed in AgentDirectorActor");
                return exception switch
                {
                    ActorInitializationException => Directive.Stop,
                    ActorKilledException => Directive.Stop,
                    _ => Directive.Restart
                };
            });
    }
}

/// <summary>
/// Message to register an agent with the director.
/// </summary>
public record RegisterAgent(string AgentId, IActorRef AgentRef, AgentMetadata Metadata) : AIMessage;

/// <summary>
/// Response confirming agent registration.
/// </summary>
public record AgentRegistered(string AgentId, bool Success) : AIMessage;

/// <summary>
/// Message to unregister an agent from the director.
/// </summary>
public record UnregisterAgent(string AgentId) : AIMessage;

/// <summary>
/// Response confirming agent unregistration.
/// </summary>
public record AgentUnregistered(string AgentId, bool Success) : AIMessage;