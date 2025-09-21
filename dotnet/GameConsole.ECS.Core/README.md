# GameConsole.ECS.Core

This project contains the foundational interfaces and contracts for the Entity-Component-System (ECS) implementation in GameConsole, following RFC-014-01.

## Overview

The ECS Core provides Tier 1 contracts (pure interfaces) for:
- Entity management with unique identifiers
- Component attachment and detachment at runtime
- System execution with proper update ordering
- ECS world support for multiple concurrent worlds
- Efficient component queries and filtering
- Memory-efficient component storage abstractions

## Architecture

This follows the GameConsole 4-tier architecture:

- **Tier 1 (This Project)**: Pure service contracts and interfaces
- **Tier 2**: Mechanical proxies and infrastructure helpers (future)
- **Tier 3**: Profiles and adapters that implement behavior (future)
- **Tier 4**: Providers with concrete ECS implementations (future)

## Core Interfaces

### IEntity
Represents a game entity with unique identifier and lifecycle state.

### IComponent
Marker interface for all components (pure data containers).

### ISystem
Interface for systems that process entities and components with lifecycle management.

### IECSWorld
Main interface for world management, entity operations, and component management.

### IEntityQuery
Interface for efficient querying of entities with specific component combinations.

## Capability Interfaces

### IComponentPoolingCapability
Optional capability for memory-efficient component pooling.

### IECSProfilingCapability
Optional capability for performance monitoring and benchmarking.

### IECSSerializationCapability
Optional capability for world state persistence.

## Design Principles

- **Async-First**: All operations return Task<T> with CancellationToken support
- **Type Safety**: Generic constraints ensure compile-time type checking
- **Performance-Oriented**: Interfaces designed for efficient implementation
- **Thread Safety**: Contracts designed to support concurrent operations
- **Data-Oriented**: Components are pure data, systems contain logic
- **Memory Efficient**: Support for pooling and efficient storage patterns

## Usage

```csharp
// Create world
var world = serviceProvider.GetRequiredService<IECSWorld>();
await world.InitializeAsync();

// Create entity
var entity = await world.CreateEntityAsync();

// Add components
await world.AddComponentAsync(entity, new PositionComponent { X = 10, Y = 20 });
await world.AddComponentAsync(entity, new VelocityComponent { X = 1, Y = 0 });

// Query entities
var query = await world.CreateQueryAsync<PositionComponent, VelocityComponent>();
var entities = await query.GetEntitiesAsync();

// Add systems
await world.AddSystemAsync(new MovementSystem());

// Update world
await world.UpdateAsync(deltaTime: 0.016f);
```

## Dependencies

- GameConsole.Core.Abstractions: Base service interfaces and capability patterns

## Performance Considerations

The interfaces are designed to enable:
- Archetype-based or sparse set component storage
- SIMD optimizations in implementations
- Component pooling for memory efficiency
- Efficient batch operations
- Parallel system execution

## Thread Safety

All interfaces are designed to be thread-safe when properly implemented, supporting:
- Concurrent entity creation/destruction
- Thread-safe component access
- Parallel system execution
- Multiple concurrent worlds