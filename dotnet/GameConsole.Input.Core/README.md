# GameConsole.Input.Core - Input Service Contracts

## Overview

This package contains the Tier 1 contracts and domain models for the GameConsole input system. It defines the interfaces and types that all input service implementations must follow.

## Key Components

### Core Service Interface

**`IService`** - The main input service contract supporting:
- Keyboard input polling and events
- Mouse position and button tracking
- Gamepad button and axis management
- Device enumeration and hot-plugging
- Observable event streams for real-time input

### Domain Models

#### Input Events
- **`InputEvent`** - Base class for all input events with timestamp and frame info
- **`KeyEvent`** - Keyboard input with key, state, and modifiers
- **`MouseEvent`** - Mouse input with position, delta, scroll, and button info
- **`GamepadEvent`** - Gamepad input with button/axis data and controller index

#### Input Types
- **`KeyCode`** - Comprehensive keyboard key enumeration
- **`MouseButton`** - Mouse button identifiers (Left, Right, Middle, etc.)
- **`GamepadButton`** - Standard controller button layout (A, B, X, Y, etc.)
- **`GamepadAxis`** - Analog input axes (sticks, triggers)
- **`InputState`** - Input state tracking (Released, Pressed, Held, JustReleased)

#### Advanced Models
- **`InputHistory`** - Time-windowed collection of input events for analysis
- **`InputPrediction`** - AI prediction results with confidence levels
- **`InputSequence`** - Recorded input sequences for macro playback
- **`InputMappingConfiguration`** - Key binding configuration with profiles

### Capability Interfaces

#### `IPredictiveInputCapability`
Enables AI-powered input prediction and suggestions:
```csharp
Task<InputPrediction> PredictNextInputAsync(InputHistory history, CancellationToken ct);
Task<IEnumerable<InputSuggestion>> GetInputSuggestionsAsync(IEnumerable<InputEvent> partialInput, CancellationToken ct);
```

#### `IInputRecordingCapability`
Provides macro recording and playback:
```csharp
Task<string> StartRecordingAsync(string name, CancellationToken ct);
Task<InputSequence> StopRecordingAsync(string sessionId, CancellationToken ct);
Task PlaybackSequenceAsync(InputSequence sequence, CancellationToken ct);
```

#### `IInputMappingCapability`
Enables customizable key bindings:
```csharp
Task MapInputAsync(string physicalInput, string logicalAction, CancellationToken ct);
Task<InputMappingConfiguration> GetMappingConfigurationAsync(CancellationToken ct);
Task SaveMappingConfigurationAsync(InputMappingConfiguration config, CancellationToken ct);
```

## Usage

This package is intended to be referenced by:
- **Service implementations** (like GameConsole.Input.Services)
- **Game engines** that need input abstraction
- **Testing frameworks** for input simulation
- **Plugin systems** requiring input capability detection

### Example Interface Usage

```csharp
using GameConsole.Input.Core;
using GameConsole.Input.Services;

// Poll input state
IService inputService = GetInputService();
bool isJumping = await inputService.IsKeyPressedAsync(KeyCode.Space);
Vector2 mousePos = await inputService.GetMousePositionAsync();

// Subscribe to events
inputService.KeyEvents.Subscribe(keyEvent => 
{
    if (keyEvent.Key == KeyCode.Escape && keyEvent.State == InputState.Pressed)
        ShowMenu();
});

// Check capabilities
if (inputService is IInputMappingCapability mappingCap)
{
    await mappingCap.MapInputAsync("Keyboard:F", "Flashlight");
}
```

## Dependencies

- **GameConsole.Core.Abstractions** - For base `IService` and `ICapabilityProvider` interfaces
- **.NET Standard 2.0** - Target framework for maximum compatibility

## Design Principles

### Cross-Platform Abstraction
All types are designed to work across different platforms and input systems without platform-specific dependencies.

### Event-Driven + Polling
Supports both .NET event streams and traditional polling for maximum flexibility.

### Extensible Architecture
Capability interfaces allow services to expose additional features without breaking existing contracts.

### Testability
All interfaces support mocking and simulation for unit testing scenarios.

### Performance Focused
Minimal allocations, efficient event handling, and async patterns throughout.

## Vector Types

The package includes lightweight vector types for input data:

- **`Vector2`** - 2D vector for mouse positions, analog stick values
- **`Vector3`** - 3D vector for spatial input (future-ready for VR/AR)

These are simple value types with minimal functionality, focused on input scenarios.

## Thread Safety

All types in this package are designed to be thread-safe when used according to the documented patterns:
- Events may be published from background threads
- Polling methods are safe to call from any thread
- Configuration objects use concurrent collections internally

## Future Extensions

The contract design supports future enhancements:
- VR/AR input devices
- Custom input hardware
- Machine learning input prediction
- Input accessibility features
- Advanced gesture recognition

## Namespace Organization

```
GameConsole.Input.Core
├── Vector.cs              // Vector2, Vector3 types
├── InputTypes.cs          // Enums: KeyCode, MouseButton, GamepadButton, etc.
├── InputEvents.cs         // Event classes: KeyEvent, MouseEvent, GamepadEvent
├── InputModels.cs         // Advanced models: InputHistory, InputSequence, etc.
└── IService.cs           // Main service interface and capability interfaces
```

This organization keeps related types together while maintaining a clean public API surface.