# Pure.DI Container Setup

This document covers the Pure.DI configuration patterns used in the GameConsole.Core.Registry project.

## Overview

Pure.DI provides compile-time dependency injection with zero runtime overhead and hierarchical container support. This implementation satisfies the GAME-RFC-003-01 requirements for:

- Compile-time dependency graph generation
- Hierarchical service scoping (Singleton, Scoped, Transient)
- Service lifetime management
- Circular dependency detection at compile time
- Performance optimization for game engine requirements

## Architecture

### ServiceComposition

The `ServiceComposition` class serves as the Pure.DI composition root:

```csharp
public partial class ServiceComposition : IServiceProvider
{
    // Pure.DI generates implementation code here
}
```

Key features:
- **Hierarchical Support**: Uses `SetParent()` to chain containers
- **Compile-time Safety**: Pure.DI validates dependencies at build time
- **Thread Safety**: Enabled via `Hint.ThreadSafe`
- **Fallback Resolution**: Unresolved services fall back to parent container

### Service Lifetimes

The `ServiceLifetimePolicies` class provides Pure.DI lifetime management:

- **Singleton**: One instance per container (configuration, loggers, registries)
- **Scoped**: One instance per scope (disposable services, request-scoped services)  
- **Transient**: New instance every time (stateless services, lightweight objects)

## Usage Patterns

### Basic Container Setup

```csharp
var composition = new ServiceComposition();

// Get services directly
var serviceProvider = composition.GetService(typeof(IServiceProvider));

// Or use as IServiceProvider
IServiceProvider provider = composition;
var service = provider.GetService<IMyService>();
```

### Hierarchical Containers

```csharp
// Create parent container with base services
var parentProvider = new ServiceProvider();
parentProvider.RegisterSingleton<ILogger, ConsoleLogger>();

// Create child container that inherits from parent
var childComposition = new ServiceComposition();
childComposition.SetParent(parentProvider);

// Child can resolve services from parent
var logger = childComposition.GetService(typeof(ILogger)); // Returns ConsoleLogger
```

### Service Scoping

```csharp
var composition = new ServiceComposition();

// Create a scope for scoped services
using var scope = composition.CreateScope();
var scopedProvider = scope.ServiceProvider;

// Scoped services are disposed when scope is disposed
```

## Compile-time Features

### Dependency Validation

Pure.DI validates dependencies at compile time:

```csharp
private static void Setup() => DI.Setup(nameof(ServiceComposition))
    .Bind<IMyService>().To<MyService>()  // Validates MyService constructor
    .Bind<IDependency>().To<Dependency>() // Validates Dependency is available
    .Root<IMyService>("MyService");
```

If `MyService` requires `IDependency` but it's not registered, you get a compile error.

### Performance Hints

```csharp
private static void Setup() => DI.Setup(nameof(ServiceComposition))
    .Hint(Hint.ThreadSafe, "On")         // Generate thread-safe code
    .Hint(Hint.Resolve, "Off")           // Disable automatic resolution
    .DefaultLifetime(Pure.DI.Lifetime.Transient);
```

### Circular Dependency Detection

Pure.DI detects circular dependencies at compile time:

```csharp
// This would cause a compile error:
.Bind<IServiceA>().To<ServiceA>()  // ServiceA depends on IServiceB
.Bind<IServiceB>().To<ServiceB>()  // ServiceB depends on IServiceA
```

## Integration with Existing Code

### Compatibility with IServiceRegistry

The ServiceComposition works alongside the existing ServiceProvider:

```csharp
// Use existing ServiceProvider for registration
var serviceProvider = new ServiceProvider();
serviceProvider.RegisterSingleton<IMyService, MyService>();

// Use ServiceComposition for Pure.DI benefits
var composition = new ServiceComposition();
composition.SetParent(serviceProvider);

// ServiceComposition can resolve from ServiceProvider
var service = composition.GetService(typeof(IMyService));
```

### Migration Strategy

1. **Start Small**: Use ServiceComposition for new services
2. **Gradual Migration**: Move existing services to Pure.DI bindings
3. **Maintain Compatibility**: Keep ServiceProvider for complex scenarios
4. **Full Migration**: Eventually replace ServiceProvider with ServiceComposition

## Performance Characteristics

### Compile-time Benefits

- **Zero Allocation**: Service resolution doesn't allocate memory
- **No Reflection**: All service creation uses generated code  
- **Inlining**: Compiler can inline service creation paths
- **Dead Code Elimination**: Unused services aren't included in output

### Runtime Benefits

- **Faster Startup**: No runtime registration overhead
- **Predictable Performance**: No runtime dependency graph building
- **Memory Efficient**: No runtime service descriptors or metadata
- **Thread Safe**: Generated code is inherently thread-safe

## Advanced Scenarios

### Plugin Containers

For plugin isolation, create separate compositions:

```csharp
// Host container
var hostComposition = new ServiceComposition();

// Plugin container with host as parent
var pluginComposition = new ServiceComposition();
pluginComposition.SetParent(hostComposition);

// Plugin services can use host services but not vice versa
```

### Mode-Based Containers

For Game vs Editor modes:

```csharp
// Shared services
var baseComposition = new ServiceComposition();

// Game mode container
var gameComposition = new ServiceComposition();
gameComposition.SetParent(baseComposition);

// Editor mode container  
var editorComposition = new ServiceComposition();
editorComposition.SetParent(baseComposition);
```

## Troubleshooting

### Common Issues

1. **Compile Errors**: Usually indicate missing service registrations
2. **Circular Dependencies**: Resolve by introducing interfaces or breaking cycles
3. **Performance**: Use appropriate lifetimes (avoid Transient for heavy objects)

### Debugging

Pure.DI generates source files you can examine:
- Look in `obj/Debug/net8.0/Pure.DI/` for generated code
- Use debugger to step through generated service creation
- Check compiler output for Pure.DI warnings and errors

## References

- [Pure.DI Documentation](https://github.com/DevTeam/Pure.DI)
- [GameConsole Architecture Documentation](../docs/architecture.md)
- [Service Registry Patterns](./ServiceProvider.cs)