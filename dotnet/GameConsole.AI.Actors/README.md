# GameConsole AI Actor System

This library provides an Akka.NET-based actor system foundation for AI orchestration and distributed processing in the GameConsole architecture.

## Overview

The AI Actor System implements RFC-009: Akka.NET AI Orchestration, providing:

- **Scalable AI Agent Management**: Supervisor-based architecture for managing multiple AI agents
- **Fault Tolerance**: Built-in supervision strategies and error handling
- **Distributed Processing**: Support for clustering across multiple nodes
- **Flexible Configuration**: Comprehensive configuration for actor systems, clustering, and mailboxes
- **Service Integration**: Full integration with GameConsole's IService and ICapabilityProvider interfaces

## Core Components

### AIActorSystem
Main service class that manages the Akka.NET actor system lifecycle and provides integration with the GameConsole service infrastructure.

```csharp
public class AIActorSystem : IService, ICapabilityProvider
```

**Features:**
- Implements `IService` for lifecycle management (Initialize, Start, Stop)
- Implements `ICapabilityProvider` for service discovery
- Configurable clustering support
- Supervisor actor management
- Agent registration and routing

### BaseAIActor
Abstract base class for all AI agent actors, providing common functionality and patterns.

```csharp
public abstract class BaseAIActor : ReceiveActor
```

**Features:**
- Standard message handling (InvokeAgent, StreamAgent, GetAgentInfo)
- Lifecycle event handling (PreStart, PreRestart, PostRestart, PostStop)
- Built-in error handling and logging
- Supervision strategy for child actors

### Supervisor Actors

#### AgentDirectorActor
Top-level supervisor that manages and routes messages to AI agent instances.

- Agent registration and discovery
- Message routing to appropriate agents
- Agent health monitoring
- Graceful termination handling

#### ContextManagerActor
Manages conversation state and context for AI interactions.

- Conversation lifecycle management
- Context storage and retrieval
- Automatic cleanup of expired conversations
- Thread-safe context operations

## Configuration

### ActorSystemConfiguration
Comprehensive configuration class for customizing actor system behavior:

```csharp
public class ActorSystemConfiguration
{
    public string SystemName { get; set; } = "GameConsole-AI";
    public ActorSystemConfig ActorSystem { get; set; } = new();
    public ClusterConfig Clustering { get; set; } = new();
    public MailboxConfig Mailbox { get; set; } = new();
    public SupervisionConfig Supervision { get; set; } = new();
    public LoggingConfig Logging { get; set; } = new();
}
```

**Key Configuration Areas:**
- **Actor System**: Dispatcher threads, throughput, creation timeout
- **Clustering**: Roles, seed nodes, minimum cluster size
- **Mailbox**: Custom mailbox types, stash capacity
- **Supervision**: Retry policies, escalation timeout
- **Logging**: Log levels, lifecycle events, dead letters

## Message Types

The system uses a comprehensive set of message types for AI operations:

### Core Messages
- `InvokeAgent`: Request agent processing with input
- `AgentResponse`: Response from agent processing
- `StreamAgent`: Request streaming response from agent
- `AgentStreamChunk`: Streaming response chunk

### Management Messages
- `GetAvailableAgents`: Query for available agents
- `GetAgentInfo`: Get metadata about specific agent
- `RegisterAgent`: Register new agent with director
- `UnregisterAgent`: Remove agent from director

### Context Messages
- `CreateConversation`: Start new conversation
- `EndConversation`: End existing conversation
- `GetConversationContext`: Retrieve conversation state
- `UpdateConversationContext`: Update conversation state

## Usage Example

### 1. Basic Setup

```csharp
// Configure dependency injection
var services = new ServiceCollection();
services.AddLogging();

// Configure AI Actor System
var config = new ActorSystemConfiguration
{
    SystemName = "MyAI",
    Clustering = new ClusterConfig { Enabled = false }
};
services.AddSingleton(config);

var serviceProvider = services.BuildServiceProvider();

// Create and initialize AI Actor System
var aiActorSystem = new AIActorSystem(logger, config, serviceProvider);
await aiActorSystem.InitializeAsync();
await aiActorSystem.StartAsync();
```

### 2. Create Custom AI Agent

```csharp
public class MyAIAgent : BaseAIActor
{
    public MyAIAgent(ILogger<MyAIAgent> logger) : base(logger) { }

    protected override AgentResponse ProcessInvokeAgent(InvokeAgent message)
    {
        // Implement your AI logic here
        var result = ProcessUserInput(message.Input);
        return new AgentResponse(message.AgentId, result, true);
    }

    protected override void ProcessStreamAgent(StreamAgent message)
    {
        // Implement streaming response logic
        foreach (var chunk in GenerateStreamingResponse(message.Input))
        {
            Sender.Tell(new AgentStreamChunk(message.AgentId, chunk, false));
        }
        Sender.Tell(new AgentStreamChunk(message.AgentId, "", true)); // End stream
    }

    protected override AgentMetadata GetAgentMetadata()
    {
        return new AgentMetadata(
            "my-agent", "My AI Agent", "Custom AI agent", "1.0.0",
            new[] { "custom", "ai" }, true);
    }
}
```

### 3. Register and Use Agent

```csharp
// Register agent
var agentProps = Props.Create(() => new MyAIAgent(logger));
var metadata = new AgentMetadata(/* ... */);
await aiActorSystem.RegisterAgentAsync("my-agent", agentProps, metadata);

// Use agent
var director = aiActorSystem.GetAgentDirector();
var response = await director.Ask<AgentResponse>(
    new InvokeAgent("my-agent", "Hello AI!"),
    TimeSpan.FromSeconds(10));
```

## Clustering Support

The system supports Akka.NET clustering for distributed AI processing:

```csharp
var config = new ActorSystemConfiguration
{
    Clustering = new ClusterConfig
    {
        Enabled = true,
        Roles = new[] { "ai-worker" },
        SeedNodes = new[] { "akka.tcp://GameConsole-AI@node1:2552" },
        MinimumClusterSize = 2,
        Hostname = "localhost",
        Port = 2552
    }
};
```

## Testing

The library includes comprehensive test coverage using Akka.TestKit:

```csharp
public class MyActorTests : TestKit
{
    [Fact]
    public void Should_HandleMessage()
    {
        var actor = ActorOf<MyActor>();
        actor.Tell(new TestMessage());
        ExpectMsg<TestResponse>();
    }
}
```

## Integration with GameConsole Services

The AI Actor System integrates seamlessly with GameConsole's service architecture:

- **IService**: Standard lifecycle management (Initialize, Start, Stop)
- **ICapabilityProvider**: Service discovery and capability queries
- **Service Registry**: Automatic registration with GameConsole service registry

## Performance Considerations

- **Mailbox Configuration**: Use appropriate mailbox types for different workloads
- **Dispatcher Tuning**: Configure thread pool sizes based on workload
- **Supervision Strategies**: Balance between fault tolerance and performance
- **Message Batching**: Consider batching for high-throughput scenarios

## Error Handling

The system provides multiple layers of error handling:

1. **Actor Level**: Try-catch in message handlers with error responses
2. **Supervision Level**: Automatic restart/stop strategies for failed actors
3. **System Level**: Graceful degradation and circuit breaking
4. **Logging**: Comprehensive error logging and monitoring

## Monitoring and Observability

Built-in support for monitoring:

- Actor lifecycle events
- Message processing metrics
- Dead letter monitoring
- Cluster membership events
- Custom telemetry integration points

## Examples

See the `Examples` folder for complete working examples:

- `SampleAIAgents.cs`: Example AI agent implementations
- `AIActorSystemExample.cs`: Complete usage example with setup and demonstration

## Dependencies

- Akka.NET 1.5.31
- Akka.Cluster 1.5.31  
- Akka.Persistence 1.5.31
- Microsoft.Extensions.Logging.Abstractions 8.0.0
- Microsoft.Extensions.Configuration.Abstractions 8.0.0
- Microsoft.Extensions.DependencyInjection.Abstractions 8.0.0
- GameConsole.Core.Abstractions (local dependency)

## License

This library is part of the GameConsole project. See project license for details.