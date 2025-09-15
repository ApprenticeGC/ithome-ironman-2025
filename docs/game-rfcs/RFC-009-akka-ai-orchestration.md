# RFC-009: Akka.NET AI Orchestration

- **Start Date**: 2025-01-15
- **RFC Author**: Claude
- **Status**: Draft
- **Depends On**: RFC-007, RFC-008

## Summary

This RFC defines the actor-based AI orchestration system using Akka.NET for GameConsole's AI agent management. The system provides scalable, fault-tolerant coordination of AI agents with supervision strategies, message routing, and distributed processing capabilities.

## Motivation

GameConsole's AI system requires sophisticated orchestration to handle:

1. **Multiple AI Agents**: Coordination between dialogue, analysis, and code generation agents
2. **Concurrent Processing**: Parallel AI requests without blocking
3. **Fault Tolerance**: Graceful handling of AI backend failures
4. **Resource Management**: Efficient allocation of AI resources
5. **State Management**: Complex conversation and context tracking
6. **Scalability**: Horizontal scaling of AI processing
7. **Circuit Breaking**: Protection against cascading AI service failures

## Detailed Design

### Actor System Architecture

```
AI Actor System
┌─────────────────────────────────────────────────────────────────┐
│ Akka.NET ActorSystem ("GameConsole-AI")                        │
│                                                                 │
│ ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│ │ Agent Director  │  │ Backend Manager │  │ Context Manager │ │
│ │ (Supervisor)    │  │ (Router)        │  │ (State)         │ │
│ └─────────────────┘  └─────────────────┘  └─────────────────┘ │
│          │                     │                     │         │
│          ▼                     ▼                     ▼         │
│ ┌─────────────────────────────────────────────────────────┐   │
│ │                Agent Instances                          │   │
│ │ ┌─────────────┐ ┌─────────────┐ ┌─────────────────────┐ │   │
│ │ │ Dialogue    │ │ Asset       │ │ Code Generation     │ │   │
│ │ │ Agent       │ │ Analysis    │ │ Agent               │ │   │
│ │ │             │ │ Agent       │ │                     │ │   │
│ │ └─────────────┘ └─────────────┘ └─────────────────────┘ │   │
│ └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

### Core Actor Definitions

```csharp
// GameConsole.AI.Actors/src/Messages/AIMessages.cs
public abstract record AIMessage;

// Agent Management Messages
public record StartAgent(string AgentType, AgentConfig Config) : AIMessage;
public record StopAgent(string AgentId) : AIMessage;
public record AgentStarted(string AgentId, IActorRef ActorRef) : AIMessage;
public record AgentStopped(string AgentId, string Reason) : AIMessage;

// Processing Messages
public record ProcessRequest(
    string RequestId,
    string AgentType,
    object Request,
    IActorRef Sender) : AIMessage;

public record ProcessResponse(
    string RequestId,
    object Response,
    TimeSpan ProcessingTime) : AIMessage;

public record ProcessFailed(
    string RequestId,
    Exception Exception,
    string AgentId) : AIMessage;

// Backend Management Messages
public record BackendHealthCheck(string BackendName) : AIMessage;
public record BackendHealthStatus(string BackendName, bool IsHealthy, string Details) : AIMessage;
public record SwitchBackend(string AgentId, string NewBackendName) : AIMessage;

// Context Management Messages
public record SaveContext(string ContextId, object Context) : AIMessage;
public record LoadContext(string ContextId) : AIMessage;
public record ContextSaved(string ContextId) : AIMessage;
public record ContextLoaded(string ContextId, object Context) : AIMessage;
```

### Agent Director (Supervisor)

```csharp
// GameConsole.AI.Actors/src/AgentDirector.cs
public class AgentDirectorActor : ReceiveActor, IWithTimers
{
    private readonly Dictionary<string, IActorRef> _agents = new();
    private readonly Dictionary<string, AgentConfig> _agentConfigs = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AgentDirectorActor> _logger;

    public ITimerScheduler Timers { get; set; } = null!;

    public AgentDirectorActor(IServiceProvider serviceProvider, ILogger<AgentDirectorActor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        ReceiveAsync<StartAgent>(async msg => await HandleStartAgentAsync(msg));
        Receive<StopAgent>(HandleStopAgent);
        Receive<ProcessRequest>(HandleProcessRequest);
        Receive<Terminated>(HandleTerminated);

        // Health monitoring
        Timers.StartPeriodicTimer("health-check", new HealthCheckTick(),
            TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        Receive<HealthCheckTick>(HandleHealthCheck);
    }

    private async Task HandleStartAgentAsync(StartAgent msg)
    {
        try
        {
            if (_agents.ContainsKey(msg.Config.AgentId))
            {
                _logger.LogWarning("Agent {AgentId} already exists", msg.Config.AgentId);
                Sender.Tell(new AgentStarted(msg.Config.AgentId, _agents[msg.Config.AgentId]));
                return;
            }

            var agentProps = CreateAgentProps(msg.AgentType, msg.Config);
            var agentRef = Context.ActorOf(agentProps, msg.Config.AgentId);

            // Supervise the agent
            Context.Watch(agentRef);

            _agents[msg.Config.AgentId] = agentRef;
            _agentConfigs[msg.Config.AgentId] = msg.Config;

            _logger.LogInformation("Started agent {AgentId} of type {AgentType}",
                msg.Config.AgentId, msg.AgentType);

            Sender.Tell(new AgentStarted(msg.Config.AgentId, agentRef));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start agent {AgentId}", msg.Config.AgentId);
            Sender.Tell(new Status.Failure(ex));
        }
    }

    private void HandleProcessRequest(ProcessRequest msg)
    {
        if (!_agents.TryGetValue(msg.AgentType, out var agentRef))
        {
            // Start agent on-demand
            var config = GetDefaultAgentConfig(msg.AgentType);
            Self.Tell(new StartAgent(msg.AgentType, config));

            // Stash the request until agent is ready
            Stash.Stash();
            return;
        }

        // Forward request to appropriate agent
        agentRef.Forward(msg);
    }

    private void HandleTerminated(Terminated msg)
    {
        var agentId = FindAgentId(msg.ActorRef);
        if (agentId != null)
        {
            _logger.LogWarning("Agent {AgentId} terminated unexpectedly", agentId);

            _agents.Remove(agentId);
            var config = _agentConfigs[agentId];

            // Restart agent with exponential backoff
            var restartDelay = CalculateRestartDelay(config.RestartCount);
            Timers.StartSingleTimer($"restart-{agentId}",
                new StartAgent(config.AgentType, config with { RestartCount = config.RestartCount + 1 }),
                restartDelay);
        }
    }

    private Props CreateAgentProps(string agentType, AgentConfig config)
    {
        return agentType switch
        {
            "dialogue" => Props.Create<DialogueAgentActor>(_serviceProvider, config),
            "asset-analysis" => Props.Create<AssetAnalysisAgentActor>(_serviceProvider, config),
            "code-generation" => Props.Create<CodeGenerationAgentActor>(_serviceProvider, config),
            _ => throw new ArgumentException($"Unknown agent type: {agentType}")
        };
    }

    // Supervision strategy
    protected override SupervisorStrategy SupervisorStrategy()
    {
        return new OneForOneStrategy(
            maxNrOfRetries: 3,
            withinTimeRange: TimeSpan.FromMinutes(1),
            localOnlyDecider: ex => ex switch
            {
                AIBackendUnavailableException => Directive.Restart,
                RateLimitExceededException => Directive.Resume,
                ArgumentException => Directive.Stop,
                _ => Directive.Escalate
            });
    }

    private record HealthCheckTick;
}
```

### Base Agent Actor

```csharp
// GameConsole.AI.Actors/src/BaseAgentActor.cs
public abstract class BaseAgentActor : ReceiveActor, IWithStash
{
    protected readonly AgentConfig Config;
    protected readonly IAIBackend Backend;
    protected readonly ILogger Logger;
    protected readonly Dictionary<string, object> Context = new();

    public IStash Stash { get; set; } = null!;

    protected BaseAgentActor(IServiceProvider serviceProvider, AgentConfig config)
    {
        Config = config;
        Backend = serviceProvider.GetRequiredService<IAIBackend>();
        Logger = serviceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(GetType());

        // Common message handlers
        ReceiveAsync<ProcessRequest>(async msg => await HandleProcessRequestAsync(msg));
        Receive<BackendHealthStatus>(HandleBackendHealthStatus);

        // Initialize in healthy state
        Become(Healthy);
    }

    protected abstract Task<object> ProcessRequestAsync(object request, CancellationToken cancellationToken);

    private async Task HandleProcessRequestAsync(ProcessRequest msg)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            Logger.LogDebug("Processing request {RequestId} for agent {AgentId}",
                msg.RequestId, Config.AgentId);

            var response = await ProcessRequestAsync(msg.Request, CancellationToken.None);

            stopwatch.Stop();
            msg.Sender.Tell(new ProcessResponse(msg.RequestId, response, stopwatch.Elapsed));

            Logger.LogDebug("Completed request {RequestId} in {Duration}ms",
                msg.RequestId, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Failed to process request {RequestId}", msg.RequestId);
            msg.Sender.Tell(new ProcessFailed(msg.RequestId, ex, Config.AgentId));

            // Check if we should transition to unhealthy state
            if (ShouldTransitionToUnhealthy(ex))
            {
                Become(Unhealthy);
                Context.System.Scheduler.ScheduleTellOnce(
                    Config.HealthCheckInterval,
                    Self,
                    new BackendHealthCheck(Backend.Name),
                    Self);
            }
        }
    }

    private void Healthy()
    {
        Logger.LogDebug("Agent {AgentId} is in healthy state", Config.AgentId);

        ReceiveAsync<ProcessRequest>(async msg => await HandleProcessRequestAsync(msg));
        Receive<BackendHealthStatus>(HandleBackendHealthStatus);
    }

    private void Unhealthy()
    {
        Logger.LogWarning("Agent {AgentId} is in unhealthy state", Config.AgentId);

        Receive<ProcessRequest>(msg =>
        {
            msg.Sender.Tell(new ProcessFailed(msg.RequestId,
                new AgentUnhealthyException($"Agent {Config.AgentId} is unhealthy"),
                Config.AgentId));
        });

        Receive<BackendHealthStatus>(HandleBackendHealthStatus);
    }

    private void HandleBackendHealthStatus(BackendHealthStatus msg)
    {
        if (msg.IsHealthy && !Context.CurrentBehavior.Equals(Healthy))
        {
            Logger.LogInformation("Agent {AgentId} transitioning to healthy state", Config.AgentId);
            Become(Healthy);
            Stash.UnstashAll();
        }
        else if (!msg.IsHealthy && Context.CurrentBehavior.Equals(Healthy))
        {
            Logger.LogWarning("Agent {AgentId} transitioning to unhealthy state: {Details}",
                Config.AgentId, msg.Details);
            Become(Unhealthy);
        }
    }

    private bool ShouldTransitionToUnhealthy(Exception ex) => ex switch
    {
        AIBackendUnavailableException => true,
        RateLimitExceededException => false, // Temporary, don't mark as unhealthy
        HttpRequestException => true,
        TaskCanceledException => false, // Timeout, but not necessarily unhealthy
        _ => false
    };
}
```

### Dialogue Agent Implementation

```csharp
// GameConsole.AI.Agents/src/DialogueAgentActor.cs
public class DialogueAgentActor : BaseAgentActor
{
    private readonly IConversationMemory _memory;
    private readonly DialogueProfile _profile;

    public DialogueAgentActor(
        IServiceProvider serviceProvider,
        AgentConfig config) : base(serviceProvider, config)
    {
        _memory = serviceProvider.GetRequiredService<IConversationMemory>();
        _profile = serviceProvider.GetRequiredService<IOptionsSnapshot<DialogueProfile>>()
            .Get(config.ProfileName);
    }

    protected override async Task<object> ProcessRequestAsync(object request, CancellationToken cancellationToken)
    {
        return request switch
        {
            DialogueRequest dialogueReq => await ProcessDialogueAsync(dialogueReq, cancellationToken),
            ContextRequest contextReq => await ProcessContextAsync(contextReq, cancellationToken),
            _ => throw new ArgumentException($"Unsupported request type: {request.GetType()}")
        };
    }

    private async Task<DialogueResponse> ProcessDialogueAsync(DialogueRequest request, CancellationToken cancellationToken)
    {
        // Load conversation context
        var conversation = await _memory.LoadConversationAsync(request.ConversationId, cancellationToken);

        // Build prompt with context
        var prompt = BuildDialoguePrompt(request, conversation);

        // Create AI request
        var aiRequest = new AIRequest(
            Prompt: prompt,
            RequiredCapability: AICapability.TextGeneration,
            Parameters: new Dictionary<string, object>
            {
                ["temperature"] = _profile.Temperature,
                ["max_tokens"] = _profile.MaxTokens
            },
            QoS: new QualityOfService(
                MaxTokens: _profile.MaxTokens,
                MaxLatency: TimeSpan.FromSeconds(30),
                MaxCost: 0.10m,
                RequireOffline: false));

        // Process with backend
        var response = await Backend.ProcessAsync(aiRequest, cancellationToken);

        // Parse structured response
        var dialogueResponse = ParseDialogueResponse(response.Text);

        // Save to conversation memory
        await _memory.SaveExchangeAsync(request.ConversationId, request.UserMessage, dialogueResponse.Message, cancellationToken);

        return dialogueResponse;
    }

    private string BuildDialoguePrompt(DialogueRequest request, ConversationHistory conversation)
    {
        var promptBuilder = new StringBuilder();

        // System prompt
        promptBuilder.AppendLine(_profile.SystemPrompt);
        promptBuilder.AppendLine();

        // Conversation history
        if (conversation.Exchanges.Any())
        {
            promptBuilder.AppendLine("Conversation History:");
            foreach (var exchange in conversation.Exchanges.TakeLast(10))
            {
                promptBuilder.AppendLine($"User: {exchange.UserMessage}");
                promptBuilder.AppendLine($"Assistant: {exchange.AssistantMessage}");
            }
            promptBuilder.AppendLine();
        }

        // Current message
        promptBuilder.AppendLine($"User: {request.UserMessage}");
        promptBuilder.AppendLine("Assistant:");

        return promptBuilder.ToString();
    }
}
```

### Backend Manager (Circuit Breaker & Routing)

```csharp
// GameConsole.AI.Actors/src/BackendManagerActor.cs
public class BackendManagerActor : ReceiveActor, IWithTimers
{
    private readonly Dictionary<string, BackendStatus> _backendStatus = new();
    private readonly Dictionary<string, CircuitBreaker> _circuitBreakers = new();
    private readonly IServiceRegistry<IAIBackend> _backendRegistry;
    private readonly ILogger<BackendManagerActor> _logger;

    public ITimerScheduler Timers { get; set; } = null!;

    public BackendManagerActor(
        IServiceRegistry<IAIBackend> backendRegistry,
        ILogger<BackendManagerActor> logger)
    {
        _backendRegistry = backendRegistry;
        _logger = logger;

        Receive<BackendHealthCheck>(HandleHealthCheck);
        Receive<ProcessRequest>(HandleProcessRequest);
        Receive<BackendFailure>(HandleBackendFailure);

        // Regular health checks
        Timers.StartPeriodicTimer("health-check-all", new HealthCheckAll(),
            TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        Receive<HealthCheckAll>(HandleHealthCheckAll);

        InitializeBackends();
    }

    private void HandleProcessRequest(ProcessRequest msg)
    {
        var availableBackends = _backendRegistry.GetProviders()
            .Where(b => IsBackendHealthy(b.Name))
            .ToList();

        if (!availableBackends.Any())
        {
            msg.Sender.Tell(new ProcessFailed(msg.RequestId,
                new NoHealthyBackendsException("No healthy AI backends available"),
                "BackendManager"));
            return;
        }

        // Select best backend (could use intelligent routing from RFC-008)
        var selectedBackend = SelectBestBackend(availableBackends, msg);
        var circuitBreaker = GetCircuitBreaker(selectedBackend.Name);

        // Execute through circuit breaker
        circuitBreaker.CallThrough(async () =>
        {
            try
            {
                var response = await selectedBackend.ProcessAsync(
                    (AIRequest)msg.Request, CancellationToken.None);
                msg.Sender.Tell(new ProcessResponse(msg.RequestId, response, TimeSpan.Zero));
            }
            catch (Exception ex)
            {
                Self.Tell(new BackendFailure(selectedBackend.Name, ex));
                throw;
            }
        }).PipeTo(Self, msg.Sender, success: _ => new object(),
            failure: ex => new ProcessFailed(msg.RequestId, ex, selectedBackend.Name));
    }

    private CircuitBreaker GetCircuitBreaker(string backendName)
    {
        if (!_circuitBreakers.TryGetValue(backendName, out var circuitBreaker))
        {
            circuitBreaker = new CircuitBreaker(
                maxFailures: 5,
                callTimeout: TimeSpan.FromSeconds(30),
                resetTimeout: TimeSpan.FromMinutes(1));

            circuitBreaker.OnOpen(() =>
                _logger.LogWarning("Circuit breaker opened for backend {Backend}", backendName));

            circuitBreaker.OnHalfOpen(() =>
                _logger.LogInformation("Circuit breaker half-open for backend {Backend}", backendName));

            circuitBreaker.OnClose(() =>
                _logger.LogInformation("Circuit breaker closed for backend {Backend}", backendName));

            _circuitBreakers[backendName] = circuitBreaker;
        }

        return circuitBreaker;
    }

    private record BackendStatus(bool IsHealthy, DateTime LastCheck, string? ErrorMessage);
    private record BackendFailure(string BackendName, Exception Exception);
    private record HealthCheckAll;
}
```

### AI Actor System Integration

```csharp
// GameConsole.AI.Hosting/src/AIActorSystemService.cs
public class AIActorSystemService : IHostedService, IAsyncDisposable
{
    private ActorSystem? _actorSystem;
    private IActorRef? _agentDirector;
    private IActorRef? _backendManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AIActorSystemService> _logger;

    public AIActorSystemService(
        IServiceProvider serviceProvider,
        ILogger<AIActorSystemService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting AI Actor System");

        // Configure Akka.NET
        var config = ConfigurationFactory.ParseString(@"
            akka {
                actor {
                    provider = ""Akka.Actor.LocalActorRefProvider""
                    debug {
                        receive = on
                        autoreceive = on
                        lifecycle = on
                        event-stream = on
                    }
                }

                coordinated-shutdown {
                    phases {
                        actor-system-terminate {
                            timeout = 10s
                        }
                    }
                }
            }
        ");

        // Start actor system
        _actorSystem = ActorSystem.Create("GameConsole-AI", config);

        // Start top-level actors
        _agentDirector = _actorSystem.ActorOf(
            Props.Create<AgentDirectorActor>(_serviceProvider, _logger),
            "agent-director");

        _backendManager = _actorSystem.ActorOf(
            Props.Create<BackendManagerActor>(
                _serviceProvider.GetRequiredService<IServiceRegistry<IAIBackend>>(),
                _serviceProvider.GetRequiredService<ILogger<BackendManagerActor>>()),
            "backend-manager");

        _logger.LogInformation("AI Actor System started successfully");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping AI Actor System");

        if (_actorSystem != null)
        {
            await CoordinatedShutdown.Get(_actorSystem).Run(CoordinatedShutdown.ClrExitReason.Instance);
            _actorSystem = null;
        }

        _logger.LogInformation("AI Actor System stopped");
    }

    public async ValueTask DisposeAsync()
    {
        if (_actorSystem != null)
        {
            await _actorSystem.Terminate();
        }
    }

    // Public API for other services
    public async Task<TResponse> ProcessRequestAsync<TResponse>(
        string agentType,
        object request,
        TimeSpan? timeout = null)
    {
        if (_agentDirector == null)
            throw new InvalidOperationException("Actor system not started");

        var requestId = Guid.NewGuid().ToString();
        var processRequest = new ProcessRequest(requestId, agentType, request, ActorRefs.NoSender);

        var response = await _agentDirector.Ask<object>(processRequest, timeout ?? TimeSpan.FromSeconds(30));

        return response switch
        {
            ProcessResponse { Response: TResponse typedResponse } => typedResponse,
            ProcessFailed failed => throw new AIProcessingException(failed.Exception.Message, failed.Exception),
            _ => throw new InvalidOperationException($"Unexpected response type: {response.GetType()}")
        };
    }
}
```

## Benefits

### Fault Tolerance
- Supervisor strategies handle agent failures gracefully
- Circuit breakers protect against cascading failures
- Automatic agent restart with exponential backoff

### Scalability
- Actor-based concurrency for parallel AI processing
- Location transparency for distributed deployment
- Efficient message routing and load distribution

### State Management
- Conversation context isolation per agent
- Persistent state with event sourcing capabilities
- Clean separation of concerns between agents

### Resource Management
- Backend health monitoring and switching
- Intelligent request routing based on backend health
- Rate limiting and cost control integration

## Drawbacks

### Complexity
- Actor model learning curve for developers
- Complex message routing and state management
- Debugging distributed actor systems

### Memory Overhead
- Actor instances consume memory even when idle
- Message queuing overhead for high-throughput scenarios
- Supervision tree overhead

### Development Overhead
- Additional abstraction layers
- Actor lifecycle management complexity
- Message serialization requirements for clustering

## Alternatives Considered

### Direct Service Calls
- Simpler but lacks fault tolerance and concurrency
- **Rejected**: Cannot handle AI backend failures gracefully

### Task-Based Parallelism
- Simpler concurrency model but lacks supervision
- **Rejected**: No built-in fault tolerance mechanisms

### Message Queues (RabbitMQ/Azure Service Bus)
- Good for decoupling but requires external infrastructure
- **Rejected**: More complex deployment, less control over routing

## Migration Strategy

### Phase 1: Core Actor System
- Implement base actor system and supervision
- Create basic agent actors with simple message handling
- Add health monitoring and circuit breaker patterns

### Phase 2: Agent Implementations
- Implement specific agent types (dialogue, analysis, code generation)
- Add conversation memory and context management
- Integrate with backend selection and routing

### Phase 3: Advanced Features
- Add intelligent backend routing
- Implement advanced supervision strategies
- Add performance monitoring and metrics

### Phase 4: Production Hardening
- Add clustering support for horizontal scaling
- Implement persistent actor state with event sourcing
- Add comprehensive monitoring and alerting

## Success Metrics

- **Fault Tolerance**: 99% successful agent restart after failures
- **Performance**: Sub-100ms message routing latency
- **Concurrency**: Handle 100+ concurrent AI requests
- **Resource Usage**: <2GB memory overhead for actor system

## Future Possibilities

- **Actor Clustering**: Multi-node deployment with Akka.Cluster
- **Event Sourcing**: Persistent actor state with Akka.Persistence
- **Stream Processing**: Integration with Akka.Streams for data pipelines
- **Remote Deployment**: Cross-service actor communication