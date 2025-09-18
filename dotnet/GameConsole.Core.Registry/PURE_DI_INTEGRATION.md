# Pure.DI Integration Guide

This document explains the Pure.DI integration in GameConsole.Core.Registry that provides hierarchical dependency injection with compile-time safety.

## Overview

Pure.DI provides compile-time dependency injection with zero runtime overhead and hierarchical container support. This integration complements the existing ServiceProvider while adding compile-time validation and performance optimization.

## Architecture

### ServiceComposition

The `ServiceComposition` class is the Pure.DI composition root that:

- Provides compile-time dependency validation
- Generates optimized dependency resolution code
- Supports hierarchical parent/child relationships
- Integrates with the existing service registry

### HierarchicalServiceProvider

The `HierarchicalServiceProvider` combines:

- Existing `ServiceProvider` functionality
- Pure.DI composition benefits
- Hierarchical container support
- Fallback resolution strategies

## Usage Examples

### Basic Setup

```csharp
// Create a hierarchical service provider
var baseProvider = new ServiceProvider();
var hierarchical = baseProvider.CreateHierarchical();

// Register services
hierarchical.RegisterSingleton<IMyService, MyService>();
```

### Child Containers

```csharp
// Create parent container
var parent = new ServiceProvider().CreateHierarchical();
parent.RegisterSingleton<ISharedService, SharedService>();

// Create child container for plugin/mode isolation
var child = parent.CreateChild();
child.RegisterSingleton<IPluginService, PluginService>();

// Child can access parent services through fallback
var sharedService = child.GetService(typeof(ISharedService));
```

### Service Lifetimes

```csharp
var provider = new ServiceProvider().CreateHierarchical();

// Singleton - same instance across requests
provider.RegisterSingleton<ISingletonService, SingletonService>();

// Scoped - same instance within scope
provider.RegisterScoped<IScopedService, ScopedService>();

// Transient - new instance per request
provider.RegisterTransient<ITransientService, TransientService>();
```

## Benefits

### Compile-Time Validation

Pure.DI validates dependency graphs at compile time, catching:
- Missing dependencies
- Circular dependencies  
- Type mismatches
- Invalid registrations

### Performance Optimization

- Zero-allocation service resolution for singletons
- Compile-time generated resolution code
- No runtime reflection
- Optimized dependency graphs

### Hierarchical Scoping

- Parent/child container relationships
- Service inheritance with override capability
- Plugin and mode isolation
- Fallback resolution chains

### Microsoft.Extensions.DI Compatibility

- Same API surface as Microsoft.Extensions.DI
- Drop-in replacement for existing code
- Incremental adoption possible
- Familiar registration patterns

## Implementation Details

### Pure.DI Configuration

```csharp
private static void Setup() => DI.Setup(nameof(ServiceComposition))
    // Core infrastructure registrations
    .Bind<IServiceRegistry>().To<ServiceProvider>()
    
    // Root composition for main container
    .Root<IServiceRegistry>("Registry");
```

### Hierarchical Resolution Strategy

1. Try Pure.DI composition first (compile-time validated services)
2. Fallback to base ServiceProvider
3. Fallback to parent composition if available
4. Return null if not found

### Service Lifetime Management

- **Singleton**: Single instance across all requests
- **Scoped**: Single instance within container scope  
- **Transient**: New instance per request

## Testing

The integration includes comprehensive tests covering:

- Basic service composition functionality
- Hierarchical container behavior
- Service lifetime management
- Performance characteristics
- Compile-time validation
- Circular dependency detection

## Migration Guide

### From Existing ServiceProvider

```csharp
// Before
var provider = new ServiceProvider();
provider.RegisterSingleton<IService, Service>();

// After - same API, enhanced functionality
var provider = new ServiceProvider().CreateHierarchical();
provider.RegisterSingleton<IService, Service>();
```

### Gradual Adoption

The hierarchical service provider is fully backward compatible:

1. Replace `ServiceProvider` with `ServiceProvider().CreateHierarchical()`
2. Existing registrations and resolutions work unchanged
3. Add hierarchical features incrementally
4. Benefits apply immediately without code changes

## Best Practices

1. **Use hierarchical containers for plugin isolation**
2. **Prefer compile-time validation over runtime checks**
3. **Register core services in root container**
4. **Create child containers for temporary scopes**
5. **Leverage Pure.DI's performance optimizations**

## Performance Characteristics

- **Compile-time validation**: Zero runtime validation overhead
- **Optimized resolution**: Generated code paths
- **Memory efficiency**: Minimal allocations for singletons
- **Scalability**: Hierarchical scoping supports complex scenarios

This integration provides the foundation for RFC-003's requirements while maintaining compatibility with existing code and enabling future extensibility.