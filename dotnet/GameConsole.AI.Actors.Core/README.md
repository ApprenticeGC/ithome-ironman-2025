# GameConsole.AI.Actors.Core - AI Agent Actor Clustering

This project implements **GAME-RFC-009-03: AI Agent Actor Clustering**, providing distributed AI agent management using Akka.NET clustering capabilities.

## Features

- **Cluster-aware AI Agents**: Distributed AI agents across multiple cluster nodes
- **Load Balancing**: Automatic routing of requests to available agents
- **Fault Tolerance**: Supervision strategies and automatic recovery
- **Horizontal Scaling**: Add/remove cluster nodes dynamically
- **Agent Management**: Start, stop, and monitor AI agents
- **Health Monitoring**: Backend health checks and circuit breaker protection

## Architecture

The system follows the GameConsole 4-tier architecture:

### Tier 1 - Contracts (Core Abstractions)
- `IAIActorClusterService`: Main service interface
- `AIMessage`: Base message types for actor communication
- `AgentConfig`: Configuration for AI agents

### Tier 2 - Proxies (Service Layer)
- `AIActorClusterService`: Concrete implementation of the clustering service

### Tier 3 - Behavior (Actor Layer)
- `AgentDirectorActor`: Supervisor for all AI agents in the cluster
- `AIAgentActor`: Individual AI agent instances

### Tier 4 - Providers (Infrastructure)
- Akka.NET Cluster configuration
- Sharding and routing strategies

## Usage Example

```csharp
// Configuration
var config = new ConfigurationBuilder()
    .AddInMemoryCollection(new[]
    {
        new KeyValuePair<string, string?>("AIActorCluster:SystemName", "GameConsole-AI"),
        new KeyValuePair<string, string?>("AIActorCluster:Hostname", "127.0.0.1"),
        new KeyValuePair<string, string?>("AIActorCluster:Port", "8080")
    })
    .Build();

// Create and start the clustering service
var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<AIActorClusterService>();
var clusterService = new AIActorClusterService(logger, config);

await clusterService.InitializeAsync();
await clusterService.StartAsync();

// Start an AI agent
var agentConfig = new AgentConfig
{
    AgentId = "dialogue-agent-001",
    AgentType = "dialogue",
    MaxConcurrentRequests = 10,
    Backend = new BackendConfig
    {
        Name = "OpenAI",
        Endpoint = "https://api.openai.com/v1",
        Model = "gpt-4"
    }
};

var agentStarted = await clusterService.StartAgentAsync("dialogue", agentConfig);

// Process a request through the cluster
var request = new ProcessRequest(
    Guid.NewGuid().ToString(),
    "dialogue", 
    new { Message = "Hello, how can you help me?" },
    ActorRefs.NoSender);
    
var response = await clusterService.ProcessRequestAsync(request);

// Check cluster state
var clusterState = await clusterService.GetClusterStateAsync();
Console.WriteLine($"Cluster has {clusterState.Nodes.Count} nodes");

// Clean shutdown
await clusterService.StopAsync();
```

## Cluster Configuration

The system supports various clustering configurations:

```csharp
var clusterConfig = new AIActorClusterConfig
{
    SystemName = "GameConsole-AI",
    Hostname = "127.0.0.1",
    Port = 8080,
    SeedNodes = new List<string> { "akka.tcp://GameConsole-AI@127.0.0.1:8080" },
    Roles = new List<string> { "ai-agent", "supervisor" },
    MinimumClusterSize = 1,
    Sharding = new ShardingConfig
    {
        ShardsPerAgentType = 10,
        MaxAgentsPerShard = 100,
        PassivationTimeout = TimeSpan.FromMinutes(30)
    }
};
```

## Agent Types Supported

- **dialogue**: Conversational AI agents
- **analysis**: Code and data analysis agents  
- **codegen**: Code generation agents
- **custom**: Extensible for custom agent types

## Clustering Features

### Automatic Load Balancing
Requests are automatically distributed across available agents based on:
- Agent capacity and current load
- Node health and availability
- Configurable routing strategies (round-robin, least-connections, weighted)

### Fault Tolerance
- Automatic actor restart on failure
- Circuit breaker protection for backend services
- Graceful handling of node failures
- Message replay and retry mechanisms

### Horizontal Scaling
- Add new nodes dynamically to the cluster
- Automatic agent rebalancing across nodes
- Zero-downtime scaling operations

## Testing

Run the test suite:

```bash
dotnet test GameConsole.AI.Actors.Core.Tests
```

The tests validate:
- Message type contracts
- Configuration binding
- Actor lifecycle management
- Cluster state management

## Dependencies

- **Akka.NET**: Actor framework and clustering
- **Akka.Cluster**: Distributed actor deployment
- **Akka.Cluster.Sharding**: Automatic agent distribution
- **Microsoft.Extensions.**: Logging and configuration integration
- **GameConsole.Core.Abstractions**: Base service interfaces

## Implementation Notes

This implementation focuses specifically on clustering capabilities as defined in GAME-RFC-009-03. It builds upon the actor system architecture from RFC-009 and provides:

1. **Distributed Agent Deployment**: Agents can be deployed across multiple cluster nodes
2. **Cluster-aware Routing**: Requests are routed to appropriate agents regardless of node location  
3. **Fault Tolerance**: Automatic handling of node failures and agent recovery
4. **Load Balancing**: Even distribution of work across the cluster
5. **Dynamic Scaling**: Add/remove nodes without service interruption

The implementation follows minimal change principles, integrating cleanly with existing GameConsole service patterns while adding powerful distributed capabilities for AI agent management.