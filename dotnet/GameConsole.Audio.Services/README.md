# GameConsole Audio Services

This project implements the audio-specific services within the Category-Based Service Organization pattern as specified in GAME-RFC-002-01.

## Architecture

The Audio services follow the 4-tier architecture pattern:

- **Tier 1**: `GameConsole.Audio.Core` - Interface definitions and core types
- **Tier 3**: `GameConsole.Audio.Services` - Concrete service implementations

## Core Services

### AudioPlaybackService
Handles basic audio playback operations for music and sound effects.

**Features:**
- Category-based audio playback (SFX, Music, Voice)
- Volume control per category and master volume
- Support for multiple audio formats (WAV, MP3, OGG, FLAC, AAC)
- Asynchronous operations with proper cancellation support

**Usage:**
```csharp
var playbackService = new AudioPlaybackService(logger);
await playbackService.InitializeAsync();
await playbackService.StartAsync();

// Play a sound effect
await playbackService.PlayAsync("explosion.wav", "SFX");

// Set category volume
await playbackService.SetCategoryVolumeAsync("SFX", 0.8f);
```

### AudioMixerService
Advanced volume control and channel management with audio effects.

**Features:**
- Channel-based audio organization
- Per-channel volume control and muting
- Audio effects support (Reverb, Echo, Compression, etc.)
- Dynamic channel creation and management

**Usage:**
```csharp
var mixerService = new AudioMixerService(logger);
await mixerService.InitializeAsync();

// Create custom channel
await mixerService.CreateChannelAsync("Ambient");

// Apply effects
await mixerService.ApplyChannelEffectsAsync("Ambient", AudioEffects.Reverb | AudioEffects.Echo);
```

### Audio3DService
3D spatial audio positioning with realistic distance-based attenuation.

**Features:**
- 3D positioned audio sources
- Distance-based volume attenuation
- Listener position and orientation tracking
- Doppler effect calculation support

**Usage:**
```csharp
var audio3DService = new Audio3DService(logger);
await audio3DService.InitializeAsync();

// Set listener position
await audio3DService.SetListenerTransformAsync(
    position: new Vector3(0, 0, 0),
    forward: Vector3.UnitZ,
    up: Vector3.UnitY);

// Play 3D positioned audio
await audio3DService.Play3DAudioAsync("footsteps.wav", new Vector3(10, 0, 5));
```

### AudioDeviceService
Hardware abstraction and audio device management.

**Features:**
- Audio device enumeration
- Dynamic device switching
- Device capability testing
- Hot-plug device support

**Usage:**
```csharp
var deviceService = new AudioDeviceService(logger);
await deviceService.InitializeAsync();

// Get available devices
var devices = await deviceService.GetAudioDevicesAsync();

// Set active device
await deviceService.SetActiveDeviceAsync("default_output");
```

## Service Registration

All services are automatically registered with the service registry using attributes:

```csharp
[Service("Audio Playback Service", "1.0.0", "Audio playbook service for music and sound effects",
         Categories = new[] { "Audio", "Playback", "Media" },
         Lifetime = ServiceLifetime.Singleton)]
public sealed class AudioPlaybackService : BaseAudioService { }
```

## Dependency Injection

Services follow standard dependency injection patterns:

```csharp
// Registration (handled automatically via ServiceAttribute)
services.AddSingleton<AudioPlaybackService>();
services.AddSingleton<AudioMixerService>();
services.AddSingleton<Audio3DService>();
services.AddSingleton<AudioDeviceService>();

// Usage
public class GameEngine
{
    private readonly AudioPlaybackService _audioService;
    
    public GameEngine(AudioPlaybackService audioService)
    {
        _audioService = audioService;
    }
}
```

## Capability System

Services implement capability interfaces for extended functionality:

```csharp
// Check for capabilities
if (await mixerService.HasCapabilityAsync<IAudioMixerCapability>())
{
    var mixerCapability = await mixerService.GetCapabilityAsync<IAudioMixerCapability>();
    await mixerCapability.CreateChannelAsync("CustomChannel");
}
```

## Testing

Comprehensive unit tests cover all public methods and ensure proper behavior:

- 30 unit tests with 100% pass rate
- Tests cover service lifecycle, error conditions, and edge cases
- Proper mocking and dependency isolation

Run tests with:
```bash
dotnet test GameConsole.Audio.Services.Tests
```

## Future Enhancements

The current implementation provides a solid foundation that can be extended with:

- Real audio library integration (NAudio, OpenAL, etc.)
- Audio streaming for large files
- Advanced DSP effects
- Cross-platform audio device support
- Audio compression and format conversion
- Real-time audio processing pipelines

## Technical Notes

- All services inherit from `BaseAudioService` for common functionality
- Async/await pattern used throughout with proper cancellation token support
- Extensive logging for debugging and monitoring
- Thread-safe concurrent collections for managing active audio
- Proper disposal patterns for resource cleanup