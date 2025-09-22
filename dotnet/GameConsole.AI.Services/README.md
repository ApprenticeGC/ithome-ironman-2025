# GameConsole.AI.Services

Implementation of AI agent services for the GameConsole architecture.

## Overview

This package provides Tier 2-3 implementations of AI agent management services, including:

- **BasicAIService**: Core implementation with agent discovery, registration, and invocation
- **Built-in Agents**: Default agents for text generation, code assistance, and dialogue
- **Streaming Support**: Real-time streaming of AI responses
- **Conversation Management**: Multi-turn conversation contexts

## Key Features

- Thread-safe agent registration and discovery
- Mock AI responses for development and testing
- Conversation context management with history tracking
- Capability-based extensions (streaming, conversations)
- Comprehensive logging for debugging and monitoring

## Default Agents

The service registers three built-in agents:

1. **text-generator**: General text generation and creative writing
2. **code-assistant**: Code analysis and generation
3. **dialogue-master**: Character dialogue and NPC interactions

## Usage

```csharp
var service = new BasicAIService(logger);
await service.InitializeAsync();
await service.StartAsync();

// Discover agents
var agents = service.GetAvailableAgents();

// Get agent info
var agent = await service.GetAgentInfoAsync("text-generator");

// Invoke agent
var response = await service.InvokeAgentAsync("text-generator", "Write a story");
```

This follows the GameConsole 4-tier architecture as a Tier 2-3 implementation providing concrete behavior while maintaining service contracts.