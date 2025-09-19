# GameConsole.Audio.Core - Audio Service Contracts

## Overview

This package contains the Tier 1 contracts and domain models for the GameConsole audio system. It defines the interfaces and types that all audio service implementations must follow, providing a unified abstraction for game audio playback and management.

## Key Components

### Core Service Interface

**`IService`** - The main audio service contract supporting:
- Audio playback with category-based organization (SFX, Music, Voice, etc.)
- Volume management at master, category, and source levels
- Audio state tracking and metadata queries
- Observable event streams for real-time audio monitoring
- Spatial audio support through capability system

### Domain Models

#### Audio Events
- **`IAudioEvent`** - Base interface for all audio events with source ID, timestamp, and category
- **`AudioStartedEvent`** - Fired when audio playback begins
- **`AudioStoppedEvent`** - Fired when audio playback ends with stop reason
- **`AudioVolumeChangedEvent`** - Volume change notifications
- **`AudioPositionChangedEvent`** - Spatial position updates for 3D audio
- **`AudioStateChangedEvent`** - Audio state transitions (playing, paused, etc.)

#### Audio Types
- **`AudioCategory`** - Content categorization (SFX, Music, Voice, Ambient, UI)
- **`AudioState`** - Playback states (Stopped, Playing, Paused, Loading, Error)
- **`AudioFormat`** - Supported file formats (WAV, MP3, OGG, FLAC)
- **`AudioPriority`** - Playback priority levels for resource management
- **`AudioPosition`** - 3D spatial positioning with direction and velocity
- **`AudioEnvironment`** - Environmental presets for reverb and effects

#### Advanced Models
- **`AudioMetadata`** - Audio file information (duration, format, sample rate, etc.)
- **`AudioPlaybackConfig`** - Comprehensive playback configuration
- **`AudioStopReason`** - Enumeration of why audio stopped playing

### Capability Interfaces

#### `ISpatialAudioCapability`
Enables 3D audio positioning and environmental effects:
```csharp
Task SetListenerTransformAsync(Vector3 position, Vector3 forward, Vector3 up, Vector3? velocity, CancellationToken ct);
Task<bool> SetSourcePositionAsync(string sourceId, AudioPosition position, CancellationToken ct);
Task SetEnvironmentAsync(AudioEnvironment environment, CancellationToken ct);
```

#### `IVolumeControlCapability`
Provides advanced volume management:
```csharp
Task<bool> FadeVolumeAsync(string sourceId, float targetVolume, TimeSpan duration, CancellationToken ct);
Task<string> CreateVolumeSnapshotAsync(string name, CancellationToken ct);
Task<bool> RestoreVolumeSnapshotAsync(string snapshotId, TimeSpan? transitionDuration, CancellationToken ct);
Task SetCompressionAsync(AudioCategory category, float ratio, float threshold, CancellationToken ct);
```

## Usage

This package is intended to be referenced by:
- **Audio service implementations** (like GameConsole.Audio.Services)
- **Game engines** that need audio abstraction
- **Audio testing frameworks**
- **Plugin systems** requiring audio capability detection

### Example Interface Usage

```csharp
using GameConsole.Audio.Core;
using GameConsole.Audio.Services;

// Basic audio playback
IService audioService = GetAudioService();

// Play a sound effect
var sourceId = await audioService.PlayAsync("sounds/explosion.wav", AudioCategory.SFX);

// Play background music with configuration
var musicConfig = AudioPlaybackConfig.BackgroundMusic(volume: 0.8f);
await audioService.PlayAsync("music/background.ogg", AudioCategory.Music, musicConfig);

// Volume management
await audioService.SetMasterVolumeAsync(0.9f);
await audioService.SetCategoryVolumeAsync(AudioCategory.SFX, 0.7f);

// Subscribe to audio events
audioService.StateChanges.Subscribe(stateChange => 
{
    Console.WriteLine($"Audio {stateChange.SourceId} changed from {stateChange.PreviousState} to {stateChange.NewState}");
});

// Spatial audio capabilities
if (audioService is ISpatialAudioCapability spatialAudio)
{
    await spatialAudio.SetListenerTransformAsync(
        position: new Vector3(0, 0, 0),
        forward: Vector3.UnitZ,
        up: Vector3.UnitY);
    
    await spatialAudio.SetSourcePositionAsync(sourceId, 
        AudioPosition.ThreeD(new Vector3(10, 0, 0)));
}
```

## Dependencies

- **GameConsole.Core.Abstractions** - For base `IService` and `ICapabilityProvider` interfaces
- **System.Reactive** - For `IObservable<T>` event streams
- **System.Numerics** - For Vector3 spatial positioning
- **.NET 8.0** - Target framework

## Design Principles

### Category-Based Organization
Audio is organized into logical categories (SFX, Music, Voice, etc.) allowing for independent volume control and processing.

### Priority-Based Resource Management
Audio sources have priority levels to handle resource limitations gracefully.

### Spatial Audio Ready
Built-in support for 3D audio positioning with listener orientation and environmental effects.

### Event-Driven Architecture
Observable streams provide real-time audio event monitoring without polling.

### Capability-Based Extensions
Optional features are exposed through capability interfaces, maintaining contract stability.

### Configuration-Driven
Rich configuration objects allow detailed control over playback behavior.

## Audio Categories

### SFX (Sound Effects)
- Short-duration audio for game actions
- High priority, non-looping by default
- Used for UI sounds, weapon effects, collision sounds

### Music
- Background music and soundtracks
- Lower priority, often looping
- Supports crossfading and dynamic music systems

### Voice
- Dialog, narration, and voice acting
- High priority to ensure clarity
- Often ducking other audio categories

### Ambient
- Environmental sounds and atmosphere
- Low to medium priority, usually looping
- Weather, machinery, nature sounds

### UI
- User interface audio feedback
- High priority, very short duration
- Button clicks, notifications, alerts

## Spatial Audio

The audio system supports full 3D spatial audio with:

### Listener Management
- Position, orientation, and velocity tracking
- Support for camera or player-based audio perspective

### Source Positioning
- 3D position with distance attenuation
- Directional audio sources
- Doppler effect support

### Environmental Audio
- Predefined environment presets
- Reverb and echo effects
- Occlusion and obstruction simulation

## Thread Safety

All types in this package are designed to be thread-safe:
- Events may be published from audio threads
- Service methods are safe to call from any thread
- Configuration objects are immutable value types

## Performance Considerations

### Efficient Event Handling
- Minimal allocations in audio event paths
- Struct-based events for reduced GC pressure
- Reactive streams with proper disposal patterns

### Resource Management
- Priority-based audio source management
- Automatic cleanup of finished audio sources
- Memory-efficient metadata caching

## Future Extensions

The contract design supports future enhancements:
- Dynamic range compression and audio effects
- Real-time audio synthesis
- Audio streaming and procedural generation
- Machine learning-based audio optimization
- Accessibility features (visual audio cues, etc.)

## Namespace Organization

```
GameConsole.Audio.Core
├── AudioTypes.cs           // Enums and basic types
├── AudioEvents.cs          // Event classes and interfaces
└── IService.cs            // Main service interface and capabilities
```

This organization provides a clean separation between core types, events, and service contracts while maintaining a cohesive API surface.