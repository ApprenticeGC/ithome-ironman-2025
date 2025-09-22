# GameConsole.AI.Core

This library provides AI agent discovery and registration functionality for the GameConsole system, implementing RFC-007.

## Overview

The AI Core library extends the existing plugin architecture to support specialized AI agents in games. It provides automatic discovery, validation, and registration of AI components such as NPCs, decision systems, and behavior trees.

## Key Components

### IAIAgent Interface
Extends `IPlugin` with AI-specific functionality:
- **Capabilities**: Declares what the AI agent can do (PathFinding, DecisionMaking, etc.)
- **State Management**: Tracks agent state (Uninitialized, Ready, Executing, Paused, Error, Disposed)
- **Execution Context**: Provides execution environment with game time and services
- **Lifecycle Management**: Initialize, start, stop, reset, and cleanup operations

### AIAgentAttribute
Declarative metadata for AI agents:
```csharp
[AIAgent("pathfinder-basic", "Basic PathFinder", "1.0.0", 
    "A basic pathfinding AI agent", "Author", 
    AIAgentCapability.PathFinding, 
    BehaviorType = "Navigation", Priority = 10)]
public class SamplePathFindingAgent : IAIAgent
{
    // Implementation
}
```

### AIAgentCapability Enum
Defines standard AI capabilities:
- `PathFinding`: Navigation and route planning
- `DecisionMaking`: Behavior trees and decision systems
- `Animation`: Character animation and state management
- `Dialogue`: Conversation and narrative systems
- `Combat`: Combat and tactical AI
- `EnvironmentInteraction`: Environmental interaction

### AIAgentDiscovery Service (RFC-007-02)
Automatically discovers AI agents in assemblies:
- Scans types that implement `IAIAgent`
- Validates implementations and metadata
- Filters invalid or abstract types
- Provides detailed validation feedback
- Handles reflection errors gracefully

## Usage Example

```csharp
// Create and initialize the discovery service
var logger = serviceProvider.GetService<ILogger<AIAgentDiscovery>>();
var discovery = new AIAgentDiscovery(logger);
await discovery.InitializeAsync();
await discovery.StartAsync();

// Discover AI agents in current assembly
var assembly = Assembly.GetExecutingAssembly();
var discoveredAgents = discovery.DiscoverAgents(assembly);

foreach (var agent in discoveredAgents)
{
    Console.WriteLine($"Found AI Agent: {agent.Name} ({agent.Id})");
    Console.WriteLine($"Capabilities: {agent.Capabilities}");
    Console.WriteLine($"Type: {agent.AgentType.Name}");
}

// Validate a specific type
var isValid = discovery.ValidateAgentType(typeof(MyAIAgent));
var validationResult = discovery.ValidateAgentTypeDetailed(typeof(MyAIAgent));
```

## Architecture Integration

The AI Core system integrates with existing GameConsole components:
- **GameConsole.Core.Abstractions**: Base `IService` interface
- **GameConsole.Plugins.Core**: Extended plugin architecture
- **GameConsole.Core.Registry**: Future integration for registration

## Testing

The library includes comprehensive tests:
- Unit tests for discovery service functionality
- Sample AI agent implementations for testing
- Validation tests for invalid agent types
- Integration tests with assembly scanning

Run tests with: `dotnet test GameConsole.AI.Core.Tests`