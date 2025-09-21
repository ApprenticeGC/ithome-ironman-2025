# GameConsole.Audio.Core - Audio Service Contracts

## Overview

This package provides the Tier 1 audio service contracts for the GameConsole 4-tier architecture. It defines pure interfaces for audio playback, volume management, and optional advanced capabilities like 3D spatial audio and audio effects.

## Key Components

### Core Service Interface

- **`IService`** - Main audio service interface providing basic audio playback and volume control
  - Inherits from `GameConsole.Core.Abstractions.IService` for lifecycle management
  - Supports audio file playback with category-based organization
  - Provides volume control at both master and category levels
  - Uses async/await patterns with CancellationToken support

### Capability Interfaces

- **`ISpatialAudioCapability`** - 3D spatial audio positioning and listener management
- **`IAudioEffectsCapability`** - Audio effects processing (filters, reverb, etc.)

### Domain Models

- **`Vector3`** - Simple 3D vector structure for spatial audio positioning

## Usage

```csharp
// Basic audio playback
await audioService.PlayAsync("sounds/explosion.wav", "SFX");
await audioService.SetCategoryVolumeAsync("SFX", 0.8f);

// Spatial audio (if capability is supported)
if (audioService is ISpatialAudioCapability spatialAudio)
{
    await spatialAudio.SetListenerPositionAsync(new Vector3(0, 0, 0));
    await spatialAudio.PlayAtPositionAsync("sounds/ambient.wav", new Vector3(10, 0, 5));
}

// Audio effects (if capability is supported)
if (audioService is IAudioEffectsCapability effectsAudio)
{
    await effectsAudio.ApplyReverbAsync("Music", 0.6f, 0.4f);
}
```

## Dependencies

- **GameConsole.Core.Abstractions** - Base service interfaces and lifecycle management
- **.NET Standard 2.0** - Core framework dependency only

## Design Principles

### Tier 1 Contract Compliance

- **Pure Contracts**: No implementation details, only interface definitions
- **Engine Agnostic**: No Unity, Godot, or other engine-specific dependencies
- **Broad Compatibility**: Targets .NET Standard 2.0 for maximum portability
- **Async First**: All operations use async/await patterns with cancellation support

### Audio Categories

The service uses string-based category system for organizing audio:
- **"SFX"** - Sound effects (default category)
- **"Music"** - Background music and ambient sounds
- **"Voice"** - Character dialogue and narration
- **Custom categories** - Applications can define their own categories

### Capability Discovery

Advanced features are exposed through capability interfaces that can be tested at runtime:

```csharp
// Check for spatial audio support
var hasSpatialAudio = audioService is ISpatialAudioCapability;

// Check for effects support  
var hasEffects = audioService is IAudioEffectsCapability;
```

## Volume Management

Volume levels are normalized float values:
- **0.0f** - Completely muted
- **1.0f** - Full volume
- **Master Volume** - Global volume control affecting all categories
- **Category Volume** - Per-category volume control (multiplied with master volume)

## Thread Safety

All types in this package are designed to be thread-safe when used according to the documented patterns:
- Interface methods may be called from any thread
- Implementations should handle concurrent access appropriately
- Event handlers should be thread-safe

## Future Extensions

The contract design supports future enhancements:
- Advanced audio formats and codecs
- Real-time audio streaming
- Audio analysis and visualization
- Voice recognition and synthesis
- Machine learning audio processing
- Accessibility audio features

## Namespace Organization

```
GameConsole.Audio.Core
├── IService.cs           // Main service interface
├── Vector3.cs            // Spatial positioning (embedded in IService.cs)
└── README.md            // This documentation
```

This organization keeps all audio-related contracts in a single file while maintaining a clean public API surface that follows the 4-tier architecture principles.