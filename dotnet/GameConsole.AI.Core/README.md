# GameConsole.AI.Core

## Overview

GameConsole.AI.Core implements AI Agent Discovery and Registration for the GameConsole 4-tier service architecture. This package provides the core abstractions and implementations for managing AI agents within the GameConsole system.

## Features

- **AI Agent Abstraction**: Base `IAiAgent` interface for implementing AI agents
- **Agent Registry**: Service for registering and managing AI agent lifecycle
- **Agent Discovery**: Service for finding and filtering AI agents by various criteria
- **Event System**: Real-time notifications for agent registration/unregistration
- **Service Integration**: Seamless integration with existing service registry system

## Core Interfaces

### IAiAgent
Base interface for all AI agents, extending `IService` for lifecycle management:

```csharp
public interface IAiAgent : IService
{
    string AgentId { get; }
    string Name { get; }
    string Description { get; }
    IReadOnlyList<string> Capabilities { get; }
    AiAgentStatus Status { get; }
    Task<AiAgentResponse> ProcessAsync(AiAgentRequest request, CancellationToken cancellationToken = default);
}
```

### IAiAgentRegistry
Service for registering and managing AI agents:

```csharp
public interface IAiAgentRegistry : IService
{
    Task<bool> RegisterAsync(IAiAgent agent, CancellationToken cancellationToken = default);
    Task<bool> UnregisterAsync(string agentId, CancellationToken cancellationToken = default);
    Task<IAiAgent?> GetAsync(string agentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IAiAgent>> GetAllAsync(CancellationToken cancellationToken = default);
    // Events: AgentRegistered, AgentUnregistered
}
```

### IAiAgentDiscovery
Service for discovering and filtering AI agents:

```csharp
public interface IAiAgentDiscovery : IService
{
    Task<IReadOnlyList<IAiAgent>> DiscoverAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IAiAgent>> DiscoverByCapabilityAsync(string capability, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IAiAgent>> DiscoverByStatusAsync(AiAgentStatus status, CancellationToken cancellationToken = default);
    Task<IAiAgent?> DiscoverByIdAsync(string agentId, CancellationToken cancellationToken = default);
    // Events: AgentDiscovered, AgentUnavailable
}
```

## Usage Example

### 1. Service Registration

```csharp
// Register AI services with the service provider
var serviceProvider = new ServiceProvider();

// Register AI agent services
serviceProvider.RegisterSingleton<IAiAgentRegistry, AiAgentRegistry>();
serviceProvider.RegisterScoped<IAiAgentDiscovery, AiAgentDiscovery>();

// Or use attribute-based registration
serviceProvider.RegisterFromAttributes(typeof(AiAgentRegistry).Assembly);
```

### 2. AI Agent Implementation

```csharp
[Service("Chat Agent", "1.0.0", "Provides chat capabilities")]
public class ChatAiAgent : IAiAgent
{
    public string AgentId => "chat-agent-001";
    public string Name => "Chat Agent";
    public string Description => "Handles chat and conversation requests";
    public IReadOnlyList<string> Capabilities => new[] { "chat", "conversation", "text-generation" };
    public AiAgentStatus Status { get; private set; } = AiAgentStatus.Inactive;
    public bool IsRunning { get; private set; }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Status = AiAgentStatus.Initializing;
        // Initialize AI model, load resources, etc.
        await Task.Delay(100, cancellationToken); // Simulate initialization
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        Status = AiAgentStatus.Ready;
        IsRunning = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        Status = AiAgentStatus.Inactive;
        IsRunning = false;
        return Task.CompletedTask;
    }

    public async Task<AiAgentResponse> ProcessAsync(AiAgentRequest request, CancellationToken cancellationToken = default)
    {
        if (!Capabilities.Contains(request.Capability))
            return AiAgentResponse.CreateError(request.Id, $"Capability '{request.Capability}' not supported");

        Status = AiAgentStatus.Processing;
        try
        {
            // Process the request based on capability
            var result = await ProcessChatRequest(request.Data, cancellationToken);
            return AiAgentResponse.CreateSuccess(request.Id, result);
        }
        finally
        {
            Status = AiAgentStatus.Ready;
        }
    }

    private async Task<string> ProcessChatRequest(object? data, CancellationToken cancellationToken)
    {
        // Implement chat processing logic
        await Task.Delay(50, cancellationToken); // Simulate processing
        return $"Chat response for: {data}";
    }

    public ValueTask DisposeAsync()
    {
        Status = AiAgentStatus.Inactive;
        IsRunning = false;
        return ValueTask.CompletedTask;
    }
}
```

### 3. Agent Registration and Discovery

```csharp
// Get services from DI container
var registry = serviceProvider.GetRequiredService<IAiAgentRegistry>();
var discovery = serviceProvider.GetRequiredService<IAiAgentDiscovery>();

// Initialize services
await registry.InitializeAsync();
await registry.StartAsync();
await discovery.InitializeAsync();
await discovery.StartAsync();

// Create and register AI agents
var chatAgent = new ChatAiAgent();
await registry.RegisterAsync(chatAgent);

var imageAgent = new ImageProcessingAgent(); // Another agent implementation
await registry.RegisterAsync(imageAgent);

// Discover agents by capability
var chatAgents = await discovery.DiscoverByCapabilityAsync("chat");
var allAgents = await discovery.DiscoverAllAsync();
var readyAgents = await discovery.DiscoverByStatusAsync(AiAgentStatus.Ready);

// Use an agent
var agent = chatAgents.FirstOrDefault();
if (agent != null)
{
    var request = new AiAgentRequest("req-001", "chat", "Hello, AI!");
    var response = await agent.ProcessAsync(request);
    
    if (response.Success)
    {
        Console.WriteLine($"AI Response: {response.Result}");
    }
}
```

### 4. Event Handling

```csharp
// Subscribe to discovery events
discovery.AgentDiscovered += (sender, args) => 
{
    Console.WriteLine($"New agent discovered: {args.Agent.Name}");
};

discovery.AgentUnavailable += (sender, args) => 
{
    Console.WriteLine($"Agent unavailable: {args.AgentId}");
};

// Subscribe to registry events
registry.AgentRegistered += (sender, args) => 
{
    Console.WriteLine($"Agent registered: {args.Agent.Name}");
};
```

## Architecture Integration

The AI Agent system integrates with the GameConsole 4-tier architecture:

- **Tier 1 (Abstractions)**: `IAiAgent`, `IAiAgentRegistry`, `IAiAgentDiscovery` interfaces
- **Tier 2 (Core Services)**: `AiAgentRegistry`, `AiAgentDiscovery` implementations
- **Tier 3 (Business Logic)**: Custom AI agent implementations
- **Tier 4 (Providers)**: Specific AI model providers (OpenAI, Local models, etc.)

## Service Lifecycle

All AI agent services follow the standard `IService` lifecycle:

1. **Initialize**: Setup resources, load configurations
2. **Start**: Begin accepting requests and processing
3. **Stop**: Gracefully shut down operations
4. **Dispose**: Clean up resources

The registry automatically manages agent lifecycles when agents are registered or unregistered.