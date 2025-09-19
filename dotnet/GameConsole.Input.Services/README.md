# GameConsole Input Services - RFC-002-02 Implementation

## Overview

This implementation provides comprehensive input handling services following the GameConsole 4-tier architecture. The input services support keyboard, mouse, gamepad, input mapping, and macro recording capabilities.

## Architecture

### GameConsole.Input.Core (Tier 1 - Contracts)
Contains the foundational interfaces and domain models:

- **IService** - Main input service contract
- **InputEvents** - KeyEvent, MouseEvent, GamepadEvent
- **InputTypes** - KeyCode, MouseButton, GamepadButton/Axis enums
- **InputModels** - InputHistory, InputPrediction, InputSequence, InputMappingConfiguration
- **Capability Interfaces** - IPredictiveInputCapability, IInputRecordingCapability, IInputMappingCapability

### GameConsole.Input.Services (Tier 4 - Implementations)
Concrete service implementations:

- **KeyboardInputService** - Handles keyboard input with simulation
- **MouseInputService** - Manages mouse/trackpad input with position tracking
- **GamepadInputService** - Supports multiple controllers with hot-plugging
- **InputMappingService** - Provides customizable key bindings with profiles
- **InputRecordingService** - Enables macro recording and playback

## Key Features

### 🎮 Multi-Device Support
- Keyboard with all standard keys and modifiers
- Mouse with buttons, position tracking, and scroll wheel
- Gamepad with buttons, analog axes, and multiple controller support
- Hot-plugging detection for dynamic device management

### ⚡ Dual Input Models
- **Event-driven**: Subscribe to IObservable streams for real-time input events
- **Polling**: Query current input states asynchronously

### 🔧 Advanced Capabilities
- **Input Mapping**: Runtime key remapping with multiple configuration profiles
- **Macro Recording**: Record and playback input sequences for automation
- **Predictive Input**: Foundation for AI-powered input prediction (extensible)

### 🏗️ Architecture Benefits
- **Cross-platform ready**: Abstractions ready for SDL2/DirectInput integration
- **Performance optimized**: Input buffering foundations for lag compensation
- **Accessibility friendly**: Designed to support alternative input methods
- **Testable**: Comprehensive simulation for unit testing without hardware

## Usage Examples

### Basic Input Polling
```csharp
// Initialize services
var keyboardService = new KeyboardInputService(logger);
await keyboardService.InitializeAsync();
await keyboardService.StartAsync();

// Check if a key is pressed
bool isWPressed = await keyboardService.IsKeyPressedAsync(KeyCode.W);
if (isWPressed)
{
    // Handle forward movement
}
```

### Event-Driven Input
```csharp
// Subscribe to key events
keyboardService.KeyEvents.Subscribe(keyEvent =>
{
    if (keyEvent.Key == KeyCode.Space && keyEvent.State == InputState.Pressed)
    {
        // Handle jump action
    }
});

// Subscribe to mouse events
mouseService.MouseEvents.Subscribe(mouseEvent =>
{
    if (mouseEvent.Button == MouseButton.Left && mouseEvent.ButtonState == InputState.Pressed)
    {
        // Handle click action at mouseEvent.Position
    }
});
```

### Input Mapping Configuration
```csharp
var mappingService = new InputMappingService(logger);
await mappingService.InitializeAsync();
await mappingService.StartAsync();

// Map physical inputs to logical actions
await mappingService.MapInputAsync("Keyboard:W", "Move Forward");
await mappingService.MapInputAsync("Gamepad:LeftStickY", "Move Forward/Backward");

// Resolve inputs at runtime
string? action = await mappingService.ResolveInputMappingAsync("Keyboard:W");
// Returns: "Move Forward"
```

### Macro Recording
```csharp
var recordingService = new InputRecordingService(logger);
await recordingService.InitializeAsync();
await recordingService.StartAsync();

// Start recording a macro
string sessionId = await recordingService.StartRecordingAsync("QuickBuild");

// Record some input events (normally these come from actual input)
recordingService.RecordInputEvent(new KeyEvent(KeyCode.B, InputState.Pressed, KeyModifiers.Control, DateTime.UtcNow, 1));

// Stop recording and get the sequence
InputSequence sequence = await recordingService.StopRecordingAsync(sessionId);

// Play back the recorded sequence
await recordingService.PlaybackSequenceAsync(sequence);
```

## Service Registration

All services are decorated with `ServiceAttribute` for automatic registration:

```csharp
[Service("Keyboard Input", "1.0.0", "Handles keyboard input events and key state polling", 
    Categories = new[] { "Input", "Keyboard" }, Lifetime = ServiceLifetime.Singleton)]
public class KeyboardInputService : BaseInputService
```

## Testing

The implementation includes comprehensive integration tests demonstrating:
- Service lifecycle management
- Input state polling and event streams
- Service integration scenarios
- Mapping and recording functionality

Run tests with: `dotnet test ./dotnet --no-build`

## Extension Points

### For Real Hardware Integration
Replace simulation methods in each service with actual platform calls:
- Windows: DirectInput/Raw Input APIs
- Cross-platform: SDL2 integration
- Linux: evdev or X11 input
- macOS: IOKit or Cocoa input events

### For AI Integration
Implement `IPredictiveInputCapability` in services that need AI-powered features:
- Input prediction for lag compensation
- Smart macro suggestions
- Adaptive input sensitivity

### For Custom Devices
Extend the base types:
- Add new `InputDevice` enum values
- Create custom event types inheriting from `InputEvent`
- Implement device-specific services inheriting from `BaseInputService`

## Performance Considerations

- Services use background tasks for input simulation/monitoring
- Events use System.Reactive for efficient stream processing
- Input buffering foundations ready for high-frequency input
- Async/await patterns throughout for non-blocking operations
- Proper disposal patterns for resource cleanup

## Next Steps

1. Replace simulation with real platform input APIs
2. Add input validation and sanitization for security
3. Implement dead zone configuration for analog inputs
4. Add input latency measurement capabilities
5. Create Unity/Godot provider implementations (Tier 4)