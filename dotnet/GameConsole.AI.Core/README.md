# GameConsole.AI.Core

Core abstractions and contracts for AI agent integration in the GameConsole architecture.

## Overview

This package provides Tier 1 contracts for AI agent management, including:

- **Agent Discovery**: Enumerate and query available AI agents
- **Agent Registration**: Register and unregister AI agents dynamically  
- **Agent Invocation**: Execute AI agents with text input/output
- **Capability Extensions**: Optional capabilities for streaming and conversations

## Key Interfaces

- `IService` - Core AI orchestration service
- `IStreamingCapability` - Real-time streaming of AI responses
- `IConversationCapability` - Multi-turn conversation management

## Agent Model

AI agents are described by `AgentMetadata` records containing:
- Unique ID and display name
- Capabilities flags (text generation, code, dialogue, etc.)
- Version and custom properties

This follows the GameConsole 4-tier architecture where Tier 1 provides pure contracts without implementation details.