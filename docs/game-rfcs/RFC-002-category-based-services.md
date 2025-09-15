# RFC-002: Category-Based Service Organization

- **Start Date**: 2025-01-15
- **RFC Author**: Claude
- **Status**: Draft
- **Depends On**: RFC-001

## Summary

This RFC defines the category-based organization pattern for GameConsole services, inspired by the Carob-Bean framework's domain separation approach. Each service category (Audio, Input, Physics, etc.) is organized as a self-contained package with contracts, proxies, and extensions.

## Motivation

GameConsole needs a scalable way to organize services that:

1. **Domain Separation**: Clear boundaries between Audio, Input, Physics, etc.
2. **Independent Development**: Categories can be developed and tested separately
3. **NuGet Packaging**: Each category can be distributed as individual packages
4. **Extension Points**: Categories can be enhanced with AI and other capabilities
5. **Carob-Bean Compatibility**: Follows proven organizational patterns

Without clear category organization, services would become a monolithic mess that's difficult to maintain and extend.

## Detailed Design

### Category Structure

Each service category follows this standardized structure:

```
GameConsole.{Category}.Core/
├── src/
│   ├── Services/
│   │   ├── IService.cs                    # Tier 1: Core contract
│   │   └── Proxy/                         # Tier 2: Generated proxies
│   │       └── {Category}ServiceProxy.generated.cs
│   ├── Capabilities/                      # Optional capability interfaces
│   │   ├── I{Advanced}Capability.cs
│   │   └── I{Specific}Capability.cs
│   ├── Models/                           # Domain-specific types
│   │   ├── {Category}Context.cs
│   │   └── {Category}Request.cs
│   └── Extensions/                       # Optional extensions (AI, etc.)
│       └── AI/
│           └── I{Category}AgentCapability.cs
├── GameConsole.{Category}.Core.csproj    # Package definition
└── README.md                             # Category documentation
```

### Core Service Categories

#### Audio Services
```csharp
// GameConsole.Audio.Core/src/Services/IService.cs
namespace GameConsole.Audio.Services;

/// <summary>
/// Core audio service for game audio playback and management.
/// Supports basic audio operations with optional advanced capabilities.
/// </summary>
public interface IService : GameConsole.Services.IService
{
    // Core audio functionality (required)
    Task<bool> PlayAsync(string path, string category = "SFX", CancellationToken ct = default);
    Task StopAsync(string path, CancellationToken ct = default);
    Task StopAllAsync(CancellationToken ct = default);

    // Volume management
    Task SetMasterVolumeAsync(float volume, CancellationToken ct = default);
    Task SetCategoryVolumeAsync(string category, float volume, CancellationToken ct = default);
    Task<float> GetCategoryVolumeAsync(string category, CancellationToken ct = default);
}

// Optional capabilities
public interface ISpatialAudioCapability : ICapabilityProvider
{
    Task SetListenerPositionAsync(Vector3 position, CancellationToken ct = default);
    Task Play3DAudioAsync(string path, Vector3 position, float volume, CancellationToken ct = default);
}
```

#### Input Services
```csharp
// GameConsole.Input.Core/src/Services/IService.cs
namespace GameConsole.Input.Services;

public interface IService : GameConsole.Services.IService
{
    // Core input handling
    Task<bool> IsKeyPressedAsync(KeyCode key, CancellationToken ct = default);
    Task<Vector2> GetMousePositionAsync(CancellationToken ct = default);
    Task<bool> IsMouseButtonPressedAsync(MouseButton button, CancellationToken ct = default);

    // Input events
    IObservable<KeyEvent> KeyEvents { get; }
    IObservable<MouseEvent> MouseEvents { get; }
    IObservable<GamepadEvent> GamepadEvents { get; }
}

// Optional capabilities
public interface IPredictiveInputCapability : ICapabilityProvider
{
    Task<InputPrediction> PredictNextInputAsync(InputHistory history, CancellationToken ct = default);
    Task<bool> IsIntentDetectedAsync(PlayerIntent intent, CancellationToken ct = default);
}
```

#### Physics Services
```csharp
// GameConsole.Physics.Core/src/Services/IService.cs
namespace GameConsole.Physics.Services;

public interface IService : GameConsole.Services.IService
{
    // Core physics simulation
    Task StepSimulationAsync(float deltaTime, CancellationToken ct = default);
    Task<RaycastHit?> RaycastAsync(Vector3 origin, Vector3 direction, float maxDistance, CancellationToken ct = default);
    Task<IEnumerable<Collider>> OverlapSphereAsync(Vector3 center, float radius, CancellationToken ct = default);

    // Physics object management
    Task<PhysicsBodyId> CreateBodyAsync(PhysicsBodyDefinition definition, CancellationToken ct = default);
    Task<bool> DestroyBodyAsync(PhysicsBodyId bodyId, CancellationToken ct = default);
}

// Optional capabilities
public interface IAdvancedPhysicsCapability : ICapabilityProvider
{
    Task SetGravityAsync(Vector3 gravity, CancellationToken ct = default);
    Task<bool> EnableFluidSimulationAsync(bool enabled, CancellationToken ct = default);
}
```

### AI-Enhanced Categories

Categories can be extended with AI capabilities without modifying core contracts:

```csharp
// GameConsole.Audio.Core/src/Extensions/AI/IAudioAgentCapability.cs
namespace GameConsole.Audio.Extensions.AI;

/// <summary>
/// AI-powered audio enhancement capabilities.
/// Provides intelligent audio direction and procedural soundscape generation.
/// </summary>
public interface IAudioAgentCapability : ICapabilityProvider
{
    // Intelligent audio direction
    Task<AudioDirective> GetAudioDirectiveAsync(GameContext context, CancellationToken ct = default);

    // Procedural soundscape generation
    Task<SoundscapeDefinition> GenerateSoundscapeAsync(EnvironmentContext environment, CancellationToken ct = default);

    // Dynamic music adaptation
    Task<MusicTransition> AdaptMusicAsync(PlayerState state, EmotionalContext emotion, CancellationToken ct = default);
}
```

### Category Dependencies

Categories have explicit dependency relationships:

```
Core Dependencies:
├── GameConsole.Core.Abstractions        (base IService, ICapabilityProvider)
└── GameConsole.Reactive.Core           (observables, reactive extensions)

Game Engine Categories:
├── GameConsole.Audio.Core
├── GameConsole.Input.Core
├── GameConsole.Physics.Core
├── GameConsole.Rendering.Core
└── GameConsole.Resource.Core

AI Enhancement Categories:
├── GameConsole.AI.Core                  (base AI contracts)
└── [Category].Extensions.AI             (AI enhancements per category)

Infrastructure Categories:
├── GameConsole.Messaging.Core           (MessagePipe contracts)
├── GameConsole.Configuration.Core       (configuration management)
├── GameConsole.Storage.Core             (data persistence)
└── GameConsole.Diagnostics.Core         (logging, telemetry)
```

### Package Configuration

Each category is packaged independently:

```xml
<!-- GameConsole.Audio.Core.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <PackageId>GameConsole.Audio.Core</PackageId>
    <PackageVersion>1.0.0</PackageVersion>
    <Description>GameConsole audio service contracts and extensions</Description>
  </PropertyGroup>

  <!-- Core dependencies -->
  <ItemGroup>
    <PackageReference Include="GameConsole.Core.Abstractions" Version="1.0.0" />
    <PackageReference Include="System.Reactive" Version="6.0.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.0" />
  </ItemGroup>

  <!-- Source generators for proxies -->
  <ItemGroup>
    <PackageReference Include="GameConsole.SourceGenerators" Version="1.0.0" PrivateAssets="all" />
  </ItemGroup>

  <!-- Generated proxy files -->
  <ItemGroup>
    <Compile Include="src/Services/Proxy/*.generated.cs" />
  </ItemGroup>
</Project>
```

### Category Registration

Categories register themselves in the DI container:

```csharp
// GameConsole.Audio.Core/src/ServiceCollectionExtensions.cs
namespace GameConsole.Audio;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAudioServices(this IServiceCollection services)
    {
        // Register core audio service proxy
        services.AddSingleton<IAudioService, AudioServiceProxy>();

        // Register service registry for provider selection
        services.AddSingleton<IServiceRegistry<IAudioService>>();

        // Register AI extensions if available
        services.TryAddSingleton<IAudioAgentCapability, DefaultAudioAgent>();

        return services;
    }
}
```

## Benefits

### Clear Domain Boundaries
- Each category owns its specific functionality
- No cross-category implementation dependencies
- Easy to reason about service responsibilities

### Independent Development
- Categories can be developed by separate teams
- Testing can focus on individual categories
- Deployment can be incremental per category

### NuGet Distribution
- Categories can be distributed as individual packages
- Version management per category
- Consumer projects can pick only needed categories

### Extensibility
- AI capabilities can be added without modifying core contracts
- Custom capabilities can extend categories
- Plugin providers can enhance specific categories

## Drawbacks

### Package Proliferation
- Many small packages to manage
- Versioning complexity across categories
- Potential for dependency conflicts

### Discoverability
- Developers need to know which categories exist
- Feature discovery across categories
- Documentation maintenance overhead

## Implementation Strategy

### Phase 1: Core Categories
1. GameConsole.Core.Abstractions
2. GameConsole.Audio.Core
3. GameConsole.Input.Core
4. GameConsole.Resource.Core

### Phase 2: Game Engine Categories
1. GameConsole.Physics.Core
2. GameConsole.Rendering.Core
3. Provider implementations for Phase 1 categories

### Phase 3: Infrastructure Categories
1. GameConsole.Messaging.Core
2. GameConsole.Configuration.Core
3. GameConsole.Storage.Core

### Phase 4: AI Enhancement
1. GameConsole.AI.Core
2. AI extensions for existing categories
3. AI provider implementations

## Alternatives Considered

### Monolithic Service Assembly
- Single assembly with all services
- **Rejected**: Doesn't support independent development or NuGet distribution

### Namespace-Only Organization
- Categories as namespaces within single assembly
- **Rejected**: Doesn't provide packaging benefits or clear boundaries

### Feature-Based Organization
- Organize by features instead of technical domains
- **Rejected**: Carob-Bean pattern is domain-based and proven

## Success Metrics

- **Category Independence**: Each category can be built and tested separately
- **NuGet Adoption**: Categories can be consumed as individual packages
- **Extension Success**: AI capabilities can be added without core changes
- **Provider Diversity**: Multiple providers per category can coexist

## Future Possibilities

- **Category Marketplace**: Discover and install community categories
- **Cross-Category AI**: AI agents that coordinate across multiple categories
- **Dynamic Category Loading**: Load categories based on runtime requirements
- **Category Analytics**: Usage metrics and performance monitoring per category