# GameConsole.AI.Core

This library provides the core abstractions and contracts for AI agent integration in the GameConsole system. It follows the established 4-tier service architecture and implements the plugin-centric design patterns used throughout the GameConsole framework.

## Overview

The AI Core library implements GAME-RFC-007-02: AI Agent Discovery and Registration, providing foundational contracts for:

- **Agent Discovery**: Finding available AI agents by capability, tags, or other criteria
- **Agent Registration**: Managing the lifecycle of AI agents in the system
- **Capability-based Architecture**: Extensible design using the ICapabilityProvider pattern

## Key Components

### Core Types

- **`AgentMetadata`**: Describes an AI agent's identity, capabilities, and configuration
- **`AgentCapability`**: Represents a capability that an AI agent can provide
- **`AgentDiscoveryResult`**: Contains the results of an agent discovery operation
- **`AgentDiscoveryCriteria`**: Specifies filtering criteria for agent discovery

### Service Interfaces

- **`IService`**: Main AI service interface extending `GameConsole.Core.Abstractions.IService`
  - Basic agent lifecycle management
  - Agent count and metadata retrieval
  - Event notifications for agent lifecycle changes

### Capability Interfaces

- **`IAgentDiscoveryCapability`**: Advanced agent discovery functionality
  - Discovery by capability type, tags, or custom criteria
  - Paginated results and filtering
  - Capability type enumeration

- **`IAgentRegistrationCapability`**: Agent registration and management
  - Register/unregister agents
  - Update agent metadata and status
  - Retrieve agent instances by type

## Usage Example

```csharp
// Create agent metadata
var metadata = new AgentMetadata("my-agent", "My AI Agent", "1.0.0", "A sample AI agent")
{
    Capabilities = new[]
    {
        new AgentCapability("TextGeneration", typeof(ITextGenerationCapability), "Generates text")
    },
    Tags = new[] { "nlp", "text" },
    Priority = 5
};

// Discovery criteria
var criteria = new AgentDiscoveryCriteria
{
    CapabilityType = typeof(ITextGenerationCapability),
    Tags = new[] { "nlp" },
    EnabledOnly = true,
    MaxResults = 10
};

// Use through service (implementation not included in Core)
// var result = await aiService.Discovery.DiscoverAgentsAsync(criteria);
```

## Architecture Alignment

This library follows the GameConsole architecture principles:

- **Tier 1 Contracts**: Provides stable public contracts for AI functionality
- **Category-based Organization**: AI domain follows established patterns like Input/Graphics
- **Plugin-centric Design**: Extensible through capability providers
- **Async/Await Patterns**: All operations support cancellation tokens
- **Event-driven Updates**: Notifications for agent lifecycle changes

## Dependencies

- **GameConsole.Core.Abstractions**: Base service and capability provider interfaces
- **.NET Standard 2.0**: Cross-platform compatibility

## Testing

The library includes comprehensive unit tests covering:
- Core type construction and validation
- Interface inheritance and patterns
- Integration scenarios
- Edge cases and error conditions

Run tests with:
```bash
dotnet test GameConsole.AI.Core.Tests
```

## Future Extensions

This foundational library enables future implementations:
- Concrete AI service implementations
- Specific AI agent providers (OpenAI, local models, etc.)
- Advanced discovery algorithms
- Agent orchestration and workflow management