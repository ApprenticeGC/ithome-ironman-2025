# RFC-001: GameConsole 4-Tier Service Architecture

- **Start Date**: 2025-01-15
- **RFC Author**: Claude
- **Status**: Draft

## Summary

This RFC defines the foundational 4-tier service architecture for GameConsole, a plugin-centric game development console application. The architecture specifically applies to **service organization and implementation**, defining how services are structured, registered, and provided across the system. The architecture is inspired by the proven Carob-Bean Unity framework pattern and adapted for cross-platform game engine simulation and tooling.

## Motivation

GameConsole requires a clean, extensible architecture that supports:

1. **Plugin-centric design**: Core functionality delivered via plugins
2. **Engine abstraction**: Support Unity, Godot, and custom engine simulations
3. **Mode switching**: Seamless transition between Game and Editor modes
4. **AI integration**: Agent-based assistance for development and gameplay
5. **Multi-UI support**: CLI and TUI interfaces with runtime switching

The 4-tier pattern provides clear separation of concerns while maintaining flexibility and testability.

## Detailed Design

### Architecture Scope

The 4-tier architecture is specifically designed for **service organization and implementation** within GameConsole. This pattern applies to:

- **Service contracts and implementations**: How services are defined, registered, and provided
- **Service provider selection**: How the system chooses between multiple service implementations
- **Service lifecycle management**: How services are loaded, configured, and managed
- **Cross-platform service abstraction**: How platform-specific implementations are abstracted

**What this architecture does NOT cover**:
- General application structure (that's covered by other architectural patterns)
- UI/presentation layer organization
- Data persistence patterns
- Non-service components (utilities, models, etc.)

### Contract Ownership and Layer Visibility

- Tier 1 (contracts) and Tier 2 (proxies) are the only assemblies that define stable, engine-agnostic service interfaces consumed across the app domain. They are loaded in the default context to maintain type identity.
- Tier 3 (adapters/profiles) MAY define provider-facing interfaces strictly for adapting Tier 4 implementations. These are private handshake contracts between Tier 3 and Tier 4 and MUST NOT leak upward. Tier 1/2 do not reference or know they exist.
- When functionality is optional or varies by engine, prefer capability discovery over fragmenting Tier 1 contracts:
  - Define narrow core Tier 1 interfaces and add optional capability facets (e.g., `ISpatialAudioCapability`).
  - Callers probe via capability checks rather than relying on engine-specific contracts.
- If behavior must change (non-additive), version the contract explicitly (e.g., `IResourceServiceV2`) while keeping V1 available. Do not redefine contracts in plugins.
- For unknown or runtime-extensible behaviors, use message envelopes (command/query objects) instead of introducing new public interfaces at runtime.

### Engine Simulation via Profiles

- This repository targets a pure C# “generic game + editor” with a TUI-first interface. Engine behaviors (Unity, Godot) are simulated through Tier 3 profiles that adapt runtime semantics while preserving Tier 1 contracts.
- Tier 4 providers can be engine-specific or generic; Tier 3 chooses providers according to the active profile without changing Tier 1 API surface.

### Service Tier Classification

The 4-tier architecture applies specifically to **service organization** within GameConsole:

#### **Tier 1: Pure Service Contracts**
- **Purpose**: Define service interfaces with zero implementation details
- **Location**: `src/core/GameConsole.*.Core/src/Services/IService.cs`
- **Dependencies**: Only .NET Standard 2.0, no Unity/Godot/framework dependencies
- **Examples**: `IGameEngine`, `IAudioService`, `IInputService` service contracts

```csharp
// Example Tier 1 contract
namespace GameConsole.Audio.Services;

public interface IService : GameConsole.Services.IService
{
    Task PlayAsync(string path, string category = "SFX", CancellationToken ct = default);
    Task SetVolumeAsync(string category, float volume, CancellationToken ct = default);
}
```

#### **Tier 2: Source-Generated Service Proxies**
- **Purpose**: Compile-time proxy generation that implements Tier 1 service contracts and delegates to registry
- **Location**: Same projects as Tier 1, generated at build time
- **Dependencies**: Source generators, Microsoft.Extensions.*, reactive libraries
- **Examples**: Generated service proxies, timeout/retry helpers, service registration infrastructure

```csharp
// Example Tier 2 generated proxy - operates only on Tier 1 interfaces
public partial class AudioServiceProxy : IAudioService
{
    private readonly IServiceRegistry<IAudioService> _registry; // Tier 1 interface

    public async Task PlayAsync(string path, string category, CancellationToken ct)
    {
        // Tier 2 proxy doesn't know about Tier 3 - only delegates through registry interface
        var provider = await _registry.GetProviderAsync(ct);
        return await provider.PlayAsync(path, category, ct)
            .WithTimeout(_registry.DefaultTimeout, ct)
            .WithRetry(2, ct);
    }
}
```

#### **Tier 3: Service Adapter/Configuration Layer**
- **Purpose**: Adapts Tier 4 service implementations to Tier 1 service contracts and handles configuration
- **Location**: `src/profiles/GameConsole.Profiles.*/`
- **Dependencies**: Tier 1 service contracts, configuration libraries
- **Examples**: Unity audio service adapter, Godot input service adapter, OpenAI AI service adapter

```csharp
// Tier 3 defines its own interfaces for Tier 4 providers
public interface IUnityAudioProvider // Tier 3-defined interface
{
    Task<AudioPlaybackResult> PlaySoundAsync(UnityAudioRequest request);
    AudioCapabilities GetCapabilities();
    Task<bool> IsAvailableAsync();
}

// Tier 3 defines message contracts for Tier 4
public record UnityAudioRequest(
    string FilePath,
    UnityAudioCategory Category,
    AudioSettings Settings);

// Tier 3 adapter uses specialized registry pattern for Tier 4 selection
public class UnityAudioAdapter : IAudioService // Implements Tier 1
{
    private readonly IRegistry<IUnityAudioProvider> _tier4Registry; // Specialized registry for Tier 4
    private readonly AudioProviderConfig _config;

    public async Task PlayAsync(string path, string category, CancellationToken ct)
    {
        // Use specialized registry pattern to select appropriate Tier 4 provider
        var provider = await _tier4Registry.GetProviderAsync(new ProviderCriteria
        {
            RequiredCapabilities = new[] { GetRequiredCapability(category) },
            Strategy = _config.SelectionStrategy
        });

        // Adapt from Tier 1 contract to Tier 3-defined interface
        var request = new UnityAudioRequest(
            FilePath: path,
            Category: MapCategory(category),
            Settings: _config.GetSettingsForCategory(category));

        await provider.PlaySoundAsync(request);
    }

    private string GetRequiredCapability(string category) => category switch
    {
        "Music" => "streaming",
        "SFX" => "low-latency",
        "Voice" => "real-time",
        _ => "basic"
    };

    private UnityAudioCategory MapCategory(string category) => category switch
    {
        "SFX" => UnityAudioCategory.Effects,
        "Music" => UnityAudioCategory.Music,
        _ => UnityAudioCategory.Default
    };
}

// Tier 3 plugin registration using only Tier 1 interfaces
public class UnityAudioPlugin : IServicePlugin  // Tier 1 interface
{
    public void RegisterServices(IServiceRegistrar registrar) // Tier 1 interface
    {
        // Register the adapter - no knowledge of Tier 2 registry implementation
        registrar.Register<IAudioService>(
            () => new UnityAudioAdapter(new UnityAudioProvider(), new AudioProviderConfig()),
            metadata: new ServiceMetadata
            {
                Name = "Unity Audio",
                Priority = 90,
                Capabilities = new[] { "3d-audio", "spatial", "unity" }
            });
    }
}
```

#### **Tier 4: Concrete Service Implementations**
- **Purpose**: Engine-specific and technology-specific service implementations
- **Location**: `providers/` and `plugins/` directories
- **Dependencies**: Engine SDKs, external libraries, ONLY Tier 3 interfaces (NO Tier 1 knowledge)
- **Examples**: Unity audio service implementation, Godot input service implementation, Ollama AI service implementation

```csharp
// Example Tier 4 implementation - implements Tier 3-defined interface only
public class UnityAudioProvider : IUnityAudioProvider // Tier 3-defined interface
{
    public async Task<AudioPlaybackResult> PlaySoundAsync(UnityAudioRequest request)
    {
        // Pure Unity implementation - no knowledge of IAudioService or GameConsole
        using var audioSource = UnityEngine.GameObject.CreatePrimitive(PrimitiveType.Cube)
            .AddComponent<AudioSource>();

        var clip = await LoadAudioClipAsync(request.FilePath);
        audioSource.clip = clip;
        audioSource.volume = request.Settings.Volume;
        audioSource.Play();

        return new AudioPlaybackResult(
            Success: true,
            Duration: clip.length,
            AudioSourceId: audioSource.GetInstanceID());
    }

    public AudioCapabilities GetCapabilities() => new()
    {
        SupportsSpatialAudio = true,
        SupportsVRAudio = UnityEngine.XR.XRSettings.enabled,
        SupportedFormats = new[] { "wav", "mp3", "ogg" },
        MaxConcurrentSounds = 32
    };

    public async Task<bool> IsAvailableAsync()
    {
        // Check if Unity audio system is available
        return UnityEngine.AudioSettings.GetConfiguration().sampleRate > 0;
    }

    private async Task<AudioClip> LoadAudioClipAsync(string filePath)
    {
        // Unity-specific audio loading implementation
        using var request = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(filePath, AudioType.UNKNOWN);
        await request.SendWebRequest();
        return UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(request);
    }
}

// Tier 4 registration through Tier 3-defined registration interface
public class UnityAudioProviderPlugin : IUnityAudioProviderPlugin // Tier 3-defined
{
    public void RegisterProviders(IUnityAudioProviderRegistry registry) // Tier 3-defined
    {
        registry.Register(new UnityAudioProvider(), new ProviderMetadata
        {
            Name = "Unity Standard Audio",
            Priority = 80,
            Capabilities = new[] { "spatial", "3d", "streaming" }
        });
    }
}
```

### Project Structure

```
GameConsole/
├── src/
│   ├── host/
│   │   └── GameConsole.Host/                           # [EXE] Entry point
│   ├── core/                                           # [Tier 1+2] Contracts + Proxies
│   │   ├── GameConsole.Core.Abstractions/             # Base IService interface
│   │   ├── GameConsole.Audio.Core/                    # Audio contracts + proxies
│   │   ├── GameConsole.Input.Core/                    # Input contracts + proxies
│   │   └── [other domain categories]/
│   ├── infrastructure/                                 # [Tier 2] Runtime services
│   │   ├── GameConsole.Infrastructure.Core/           # Hosting, configuration
│   │   ├── GameConsole.Infrastructure.DI/             # Pure.DI integration
│   │   └── [other infrastructure]/
│   ├── profiles/                                       # [Tier 3] Configuration
│   │   ├── GameConsole.Profiles.Game/                 # Game mode config
│   │   └── GameConsole.Profiles.Editor/               # Editor mode config
│   └── source-generators/                              # [Build Tools]
│       └── GameConsole.SourceGenerators/              # Proxy generation
├── providers/                                          # [Tier 4] Implementations
│   ├── engines/
│   │   ├── Unity.Engine.Provider/
│   │   ├── Godot.Engine.Provider/
│   │   └── Custom.Engine.Provider/
│   ├── ui/
│   │   ├── Spectre.UI.Provider/
│   │   └── TerminalGui.UI.Provider/
│   └── ai/
│       ├── Ollama.AI.Provider/
│       └── OpenAI.AI.Provider/
└── plugins/                                            # [Dynamic] Third-party
```

### Loading Strategy

#### Static Loading (Host Context)
- GameConsole.Host.exe
- All `core/*` assemblies (Tier 1+2)
- All `infrastructure/*` assemblies (Tier 2)
- All `profiles/*` assemblies (Tier 3)

#### Dynamic Loading (Plugin Contexts)
- `providers/*` assemblies loaded based on configuration
- `plugins/*` assemblies loaded based on discovery
- Each loaded into isolated AssemblyLoadContext

### Tier Interaction Rules

The architecture forms **two logical groups**:

#### **Group 1: Runtime Framework (Tier 1 + Tier 2)**
- **Tier 1** contracts may only depend on other Tier 1 contracts and .NET Standard
- **Tier 2** may depend on Tier 1 contracts and infrastructure libraries
- This group provides the stable runtime framework and service registry system

#### **Group 2: Implementation Layer (Tier 3 + Tier 4)**
- **Tier 3** may depend on Tier 1 contracts and configuration libraries (acts as adapter/bridge)
- **Tier 4** may ONLY depend on Tier 3 interfaces and external dependencies (completely isolated from Tier 1)
- This group provides concrete implementations with Tier 3 serving as the adapter layer

#### **Cross-Group Communication**
- **Generalized registry pattern**: Independent component providing registry abstraction used by all tiers
- **Tier 2 specialization**: Generated proxies use registry pattern with Tier 1 interfaces (no Tier 3 knowledge)
- **Tier 3 specialization**: Adapters use registry pattern with their own interfaces for Tier 4 selection
- **Tier 3 interfaces**: Defines specific interfaces/message contracts that Tier 4 providers implement
- **Tier 4 registration**: Providers register through Tier 3-defined interfaces or message protocols
- **API flow**: All communication flows through appropriate interface contracts at each level

```
Group 1: Runtime Framework    │    Group 2: Implementation Layer
                              │
┌─────────────────────────┐   │   ┌─────────────────────────┐
│ Tier 2: Generated       │   │   │ Tier 3: Adapters        │
│ Proxies                 │   │   │ - Implements Tier 1     │
│ - Specialized registry  │   │   │ - Defines Tier 4 ifaces │
│ - Delegates via Tier 1  │   │   │ - Specialized registry  │
│ - NO Tier 3 knowledge   │   │   │ - Selects Tier 4        │
└─────────────────────────┘   │   └─────────────────────────┘
            │                 │               │
            ▼                 │               ▼ Tier 3 interfaces
┌─────────────────────────┐   │   ┌─────────────────────────┐
│ Tier 1: Contracts       │◄──┼───┤ Registration through   │
│ - Service interfaces    │   │   │ Tier 1 interfaces only │
│ - Registry interfaces   │   │   │                         │
│ - Pure .NET Standard    │   │   ▼                         │
└─────────────────────────┘   │   ┌─────────────────────────┐
            ▲                 │   │ Tier 4: Implementations │
            │                 │   │ - Implements Tier 3     │
┌─────────────────────────┐   │   │   interfaces only      │
│ Generalized Registry    │   │   │ - Unity/Godot/Custom    │
│ Pattern                 │   │   │ - NO GameConsole deps   │
│ - Abstract registry     │   │   │ - Registers via Tier 3  │
│ - Specialized by tiers  │   │   └─────────────────────────┘
└─────────────────────────┘
```

## Benefits

### Proven Pattern
- Based on successful Carob-Bean Unity framework
- Clear separation of concerns with established boundaries
- Predictable dependency flow

### Plugin-Centric Design
- Core system is minimal - functionality comes from providers
- Easy to add new game engines, UI systems, AI backends
- Hot-swappable implementations with supervision

### Clean Isolation
- **Tier 4 Complete Isolation**: Implementations have no knowledge of GameConsole contracts
- **Easier Integration**: Any existing library can be wrapped as Tier 4 with minimal effort
- **True Pluggability**: Providers can be developed independently without GameConsole dependencies
- **Adapter Pattern**: Tier 3 serves as clean adapter layer between contracts and implementations

### Testability
- Each tier can be tested independently
- Mock providers available at Tier 4
- Pure contracts at Tier 1 enable comprehensive unit testing

### Performance
- Source generation at Tier 2 eliminates runtime reflection
- Pure.DI provides zero-allocation dependency resolution
- Plugin loading happens only when needed

## Drawbacks

### Complexity
- Four-tier mental model requires discipline to maintain
- Source generation adds build-time complexity
- Plugin isolation may impact performance

### Learning Curve
- Developers must understand tier boundaries
- Provider selection strategy requires configuration knowledge
- Debugging across plugin boundaries can be challenging

## Alternatives Considered

### 3-Tier Architecture
- Merge Tier 1+2 into single layer
- **Rejected**: Contracts and proxies serve different purposes and lifecycles

### Monolithic Architecture
- Single assembly with all functionality
- **Rejected**: Doesn't support plugin-centric requirements

### Microservices Architecture
- Separate processes for each domain
- **Rejected**: Too much overhead for desktop application

## Prior Art

- **Carob-Bean Unity Framework**: Direct inspiration for 4-tier pattern
- **ASP.NET Core**: Layered architecture with DI
- **Akka.NET**: Actor-based plugin systems
- **Unity Package Manager**: Plugin/package loading strategies

## Unresolved Questions

1. Should Tier 2 source generation be opt-in or always enabled?
2. How should we handle version conflicts between plugin dependencies?
3. What's the optimal strategy for plugin discovery and loading order?

## Future Possibilities

- **Tier 5**: Optional cloud/remote service integration
- **Plugin Marketplace**: Centralized plugin discovery and installation
- **Hot Reload**: Runtime plugin updates without restart
- **Plugin Sandboxing**: Security boundaries for untrusted plugins
