using GameConsole.Core.Abstractions;
using GameConsole.Engine.Core;
using GameConsole.Plugins.Core;

namespace GameConsole.Engine.Core;

/// <summary>
/// Example AI agent metadata for the patrol agent.
/// </summary>
public class PatrolAgentMetadata : IPluginMetadata
{
    public string Id => "patrol-agent";
    public string Name => "PatrolAgent";
    public Version Version => new(1, 0, 0);
    public string Description => "An AI agent that patrols a designated area and coordinates with other patrol agents";
    public string Author => "GameConsole AI System";
    public IReadOnlyList<string> Dependencies => Array.Empty<string>();
    public IReadOnlyDictionary<string, object> Properties { get; } = new Dictionary<string, object>
    {
        ["category"] = "AI",
        ["behavior"] = "Patrol",
        ["clustering"] = true
    };
}

/// <summary>
/// Example AI agent that demonstrates patrol behavior and clustering coordination.
/// Patrols a designated area and communicates with other patrol agents for area coverage.
/// </summary>
[Plugin("patrol-agent", "PatrolAgent", "1.0.0", "Patrol AI Agent with clustering support", "GameConsole AI System")]
public class PatrolAgent : AIAgentBase
{
    private readonly PatrolAgentMetadata _metadata = new();
    private readonly List<string> _patrolPoints;
    private int _currentPatrolIndex;
    private DateTime _lastStateChange;
    private readonly Random _random = new();

    /// <inheritdoc/>
    public override IPluginMetadata Metadata => _metadata;

    /// <inheritdoc/>
    public override string ActorType => "PatrolAgent";

    /// <inheritdoc/>
    public override string BehaviorType => "Patrol";

    /// <summary>
    /// Gets the patrol area identifier for this agent.
    /// </summary>
    public string PatrolArea { get; }

    /// <summary>
    /// Gets the current patrol point the agent is moving towards.
    /// </summary>
    public string CurrentTarget => _patrolPoints[_currentPatrolIndex];

    /// <summary>
    /// Initializes a new patrol agent for the specified area.
    /// </summary>
    /// <param name="patrolArea">The area this agent is responsible for patrolling.</param>
    /// <param name="patrolPoints">List of points to patrol in order.</param>
    /// <param name="actorId">Optional actor ID.</param>
    public PatrolAgent(string patrolArea, List<string> patrolPoints, ActorId? actorId = null) 
        : base("Initializing", actorId)
    {
        PatrolArea = patrolArea ?? throw new ArgumentNullException(nameof(patrolArea));
        _patrolPoints = patrolPoints ?? throw new ArgumentNullException(nameof(patrolPoints));
        
        if (_patrolPoints.Count == 0)
            throw new ArgumentException("Must have at least one patrol point", nameof(patrolPoints));

        _currentPatrolIndex = 0;
        _lastStateChange = DateTime.UtcNow;
    }

    /// <inheritdoc/>
    protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        await base.OnInitializeAsync(cancellationToken);
        ChangeState("Idle", "Initialization complete");
    }

    /// <inheritdoc/>
    protected override async Task OnStartAsync(CancellationToken cancellationToken)
    {
        await base.OnStartAsync(cancellationToken);
        ChangeState("Patrolling", "Starting patrol duties");
    }

    /// <inheritdoc/>
    protected override async Task OnStopAsync(CancellationToken cancellationToken)
    {
        ChangeState("Stopping", "Received stop command");
        await base.OnStopAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public override async Task UpdateBehaviorAsync(double deltaTime, CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
            return;

        var timeSinceLastChange = DateTime.UtcNow - _lastStateChange;

        switch (CurrentState)
        {
            case "Patrolling":
                await HandlePatrollingState(deltaTime, cancellationToken);
                break;
            case "Investigating":
                await HandleInvestigatingState(deltaTime, cancellationToken);
                break;
            case "Coordinating":
                await HandleCoordinatingState(deltaTime, cancellationToken);
                break;
            case "Idle":
                // Randomly start patrolling after being idle for a while
                if (timeSinceLastChange.TotalSeconds > 2)
                {
                    ChangeState("Patrolling", "Resuming patrol after idle period");
                }
                break;
        }
    }

    /// <summary>
    /// Handles behavior when in the Patrolling state.
    /// </summary>
    private async Task HandlePatrollingState(double deltaTime, CancellationToken cancellationToken)
    {
        // Simulate moving to next patrol point
        var timeSinceLastChange = DateTime.UtcNow - _lastStateChange;
        
        if (timeSinceLastChange.TotalSeconds > 3) // Simulate 3 seconds to reach patrol point
        {
            _currentPatrolIndex = (_currentPatrolIndex + 1) % _patrolPoints.Count;
            _lastStateChange = DateTime.UtcNow;
            
            // Randomly encounter something interesting
            if (_random.NextDouble() < 0.3) // 30% chance
            {
                ChangeState("Investigating", $"Found something interesting near {CurrentTarget}");
                return;
            }

            // Occasionally coordinate with other agents
            if (_random.NextDouble() < 0.2) // 20% chance
            {
                ChangeState("Coordinating", "Initiating coordination with nearby agents");
            }
        }

        await Task.Delay((int)(deltaTime), cancellationToken);
    }

    /// <summary>
    /// Handles behavior when in the Investigating state.
    /// </summary>
    private async Task HandleInvestigatingState(double deltaTime, CancellationToken cancellationToken)
    {
        var timeSinceLastChange = DateTime.UtcNow - _lastStateChange;
        
        if (timeSinceLastChange.TotalSeconds > 2) // Investigate for 2 seconds
        {
            ChangeState("Patrolling", "Investigation complete, resuming patrol");
        }

        await Task.Delay((int)(deltaTime), cancellationToken);
    }

    /// <summary>
    /// Handles behavior when in the Coordinating state.
    /// </summary>
    private async Task HandleCoordinatingState(double deltaTime, CancellationToken cancellationToken)
    {
        var timeSinceLastChange = DateTime.UtcNow - _lastStateChange;
        
        if (timeSinceLastChange.TotalSeconds > 1) // Coordinate for 1 second
        {
            ChangeState("Patrolling", "Coordination complete, resuming patrol");
        }

        await Task.Delay((int)(deltaTime), cancellationToken);
    }

    /// <inheritdoc/>
    protected override async Task<ActorMessageHandleResult> OnHandleMessageAsync(IActorMessage message, CancellationToken cancellationToken)
    {
        switch (message)
        {
            case CoordinationRequest coordReq:
                await HandleCoordinationRequest(coordReq, cancellationToken);
                return ActorMessageHandleResult.Handled;

            case StateChangeNotification stateNotif:
                await HandleStateChangeNotification(stateNotif, cancellationToken);
                return ActorMessageHandleResult.Handled;

            case AgentCommand command:
                await HandleAgentCommand(command, cancellationToken);
                return ActorMessageHandleResult.Handled;

            default:
                return ActorMessageHandleResult.NotHandled;
        }
    }

    /// <summary>
    /// Handles coordination requests from other agents.
    /// </summary>
    private async Task HandleCoordinationRequest(CoordinationRequest request, CancellationToken cancellationToken)
    {
        bool canCoordinate = CurrentState == "Patrolling" || CurrentState == "Idle";
        string reason = canCoordinate ? "Available for coordination" : $"Currently {CurrentState.ToLower()}";

        var response = new CoordinationResponse(Id, request, canCoordinate, reason);
        
        // In a real implementation, we would send this back through the cluster
        // For now, we just log that we're handling the request
        if (canCoordinate && request.CoordinationType == "AreaCoverage")
        {
            ChangeState("Coordinating", $"Coordinating {request.CoordinationType} with agent {request.SenderId}");
        }

        await Task.CompletedTask; // Placeholder for actual response sending
    }

    /// <summary>
    /// Handles state change notifications from other agents.
    /// </summary>
    private async Task HandleStateChangeNotification(StateChangeNotification notification, CancellationToken cancellationToken)
    {
        // React to other agents' state changes
        if (notification.SenderId != Id && notification.NewState == "Investigating")
        {
            // Another agent is investigating, maybe we should coordinate
            if (CurrentState == "Patrolling" && _random.NextDouble() < 0.4)
            {
                ChangeState("Coordinating", $"Responding to investigation by agent {notification.SenderId}");
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Handles commands sent to this agent.
    /// </summary>
    private async Task HandleAgentCommand(AgentCommand command, CancellationToken cancellationToken)
    {
        bool success = false;
        object? result = null;
        string? errorMessage = null;

        try
        {
            switch (command.Command.ToLowerInvariant())
            {
                case "patrol":
                    if (CurrentState != "Patrolling")
                    {
                        ChangeState("Patrolling", "Received patrol command");
                        success = true;
                        result = "Started patrolling";
                    }
                    else
                    {
                        success = true;
                        result = "Already patrolling";
                    }
                    break;

                case "investigate":
                    var location = command.Parameters?.GetValueOrDefault("location") as string ?? "unknown location";
                    ChangeState("Investigating", $"Commanded to investigate {location}");
                    success = true;
                    result = $"Investigating {location}";
                    break;

                case "return":
                    ChangeState("Idle", "Commanded to return to base");
                    success = true;
                    result = "Returning to base";
                    break;

                default:
                    errorMessage = $"Unknown command: {command.Command}";
                    break;
            }
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
        }

        // Create response
        var response = new AgentCommandResponse(Id, command, success, result, errorMessage);
        
        // In a real implementation, we would send this back to the sender
        await Task.CompletedTask; // Placeholder for actual response sending
    }

    /// <inheritdoc/>
    protected override void OnBehaviorStateChanged(string previousState, string newState, string? reason = null)
    {
        base.OnBehaviorStateChanged(previousState, newState, reason);
        _lastStateChange = DateTime.UtcNow;
        
        // Broadcast state change to cluster (in a real implementation)
        // var notification = new StateChangeNotification(Id, newState, previousState, reason);
        // await BroadcastToCluster(notification);
    }
}