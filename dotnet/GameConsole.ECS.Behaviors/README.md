# GameConsole.ECS.Behaviors

ECS Behavior Composition Framework for GameConsole (RFC-014-02)

## Overview

This project implements the ECS Behavior Composition Framework as specified in RFC-014-02. It provides a sophisticated system for composing complex game entity behaviors from individual ECS components, with support for:

- **Behavior Composition**: Combine multiple components into cohesive behaviors
- **Reusable Templates**: Define common entity archetypes with behavior templates
- **Dependency Resolution**: Automatic component dependency analysis and validation
- **Runtime Modification**: Dynamic behavior changes during execution
- **Validation System**: Comprehensive behavior consistency checking
- **Debugging Support**: Built-in profiling and debugging tools

## Architecture

The framework follows the GameConsole 4-tier architecture:

- **Tier 1**: Pure behavior contracts and interfaces (no implementation)
- **Tier 2**: Behavior composition infrastructure and proxies  
- **Tier 3**: ECS system adapters and behavior profiles
- **Tier 4**: Concrete behavior providers and Arch ECS integration

## Core Components

### IBehaviorComposer
Combines multiple ECS components into cohesive behaviors with support for:
- Component validation and dependency checking
- Runtime behavior modification (add/remove/update components)
- Behavior decomposition back to components

### IBehaviorTemplate  
Provides reusable patterns for creating common entity archetypes:
- Pre-defined component requirements and behavior types
- Template specialization and inheritance
- Validation of template instantiation

### IComponentDependencyResolver
Analyzes and validates component relationships:
- Dependency graph construction and cycle detection
- Component compatibility validation
- Minimum component set calculation
- Enhancement suggestions

### IBehaviorValidationService
Ensures behavior consistency and prevents invalid combinations:
- Individual behavior validation
- Behavior set conflict detection
- Runtime modification validation
- Pluggable validation rules

## Usage Example

```csharp
// Set up the behavior system
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var dependencyResolver = new ComponentDependencyResolver(loggerFactory.CreateLogger<ComponentDependencyResolver>());
var behaviorComposer = new BehaviorComposer(loggerFactory.CreateLogger<BehaviorComposer>(), dependencyResolver);
var validationService = new BehaviorValidationService(loggerFactory.CreateLogger<BehaviorValidationService>(), dependencyResolver);

await validationService.InitializeAsync();
await validationService.StartAsync();

// Create a behavior template for a "Player" entity
var playerTemplate = new BehaviorTemplateBuilder(loggerFactory.CreateLogger<BehaviorTemplate>())
    .WithName("Player")
    .WithDescription("Basic player entity with movement and health")
    .RequiresComponents(typeof(PositionComponent), typeof(HealthComponent))
    .CreatesBehaviors(typeof(CompositeBehavior))
    .WithTags("player", "controllable")
    .WithBehaviorFactory(async (composer, components, ct) =>
    {
        return await composer.ComposeBehaviorAsync<CompositeBehavior>(components, ct);
    })
    .Build();

// Create components
var components = new object[]
{
    new PositionComponent { X = 0, Y = 0 },
    new HealthComponent { CurrentHealth = 100, MaxHealth = 100 },
    new MovementComponent { Speed = 5.0f }
};

// Create behavior from template
var playerBehavior = await playerTemplate.CreateBehaviorAsync(behaviorComposer, components);

// Validate the behavior
var validationResult = await validationService.ValidateBehaviorAsync(playerBehavior);
if (validationResult.IsValid)
{
    // Activate and use the behavior
    await playerBehavior.ActivateAsync();
    
    // Runtime modification - add a new component
    var newBehavior = await behaviorComposer.ModifyBehaviorAsync(
        playerBehavior, 
        BehaviorModificationType.AddComponent, 
        new WeaponComponent { Damage = 25 });
}
```

## Key Features

### Composition over Inheritance
The framework emphasizes composition over inheritance, allowing flexible behavior creation by combining components rather than extending base classes.

### Dependency Resolution
Automatic analysis of component dependencies ensures that all required components are present and compatible.

### Runtime Modification
Behaviors can be modified at runtime by adding, removing, or updating components, with validation to ensure consistency.

### Validation System
Comprehensive validation prevents invalid behavior combinations and provides detailed feedback about issues.

### Template System
Reusable templates make it easy to create common entity types while supporting customization and specialization.

## Integration with Arch ECS

The framework integrates with Arch ECS 2.0 for high-performance entity-component-system operations while maintaining abstraction through the service layer.

## Testing

The framework includes comprehensive validation and can be tested using the existing GameConsole test infrastructure. All builds and tests pass with warnings-as-errors enabled.

## Future Enhancements

Planned enhancements include:
- Visual behavior composition tools
- Behavior serialization for save/load functionality
- Advanced profiling and debugging utilities
- Integration with Unity/Godot behavior simulation profiles