# UI Profile Configuration System

## Overview

The UI Profile Configuration System (RFC-011-02) provides a flexible way to configure different UI behavior modes within the GameConsole framework. It enables simulation of Unity/Godot behaviors by switching between different UI profiles at runtime.

## Architecture

The system follows the 4-tier service architecture:

- **Tier 1 (Core Abstractions)**: `GameConsole.UI.Core`
  - `IUIProfile` - Defines a UI profile with settings
  - `IUIProfileProvider` - Capability provider for profile management
  - `UIProfileSettings` - Configuration data model

- **Tier 3 (Business Logic)**: `GameConsole.UI.Services`
  - `UIProfileService` - Main service for profile management
  - `UIProfile` - Concrete implementation of profile

## Default Profiles

The system includes three default profiles:

### 1. TUI Default (`tui-default`)
- **Rendering Mode**: TUI
- **Input Mode**: Console
- **Graphics Backend**: None
- **TUI Mode**: True
- **Priority**: 100
- **Description**: Default text-based user interface profile

### 2. Unity Style (`unity-style`)
- **Rendering Mode**: Unity
- **Input Mode**: Unity
- **Graphics Backend**: OpenGL
- **TUI Mode**: False
- **Priority**: 50
- **Engine Mode**: Unity (custom property)
- **Description**: UI profile that simulates Unity engine behavior

### 3. Godot Style (`godot-style`)
- **Rendering Mode**: Godot
- **Input Mode**: Godot
- **Graphics Backend**: Vulkan
- **TUI Mode**: False
- **Priority**: 50
- **Engine Mode**: Godot (custom property)
- **Description**: UI profile that simulates Godot engine behavior

## Usage Example

```csharp
using GameConsole.UI.Core;
using GameConsole.UI.Services;
using Microsoft.Extensions.Logging;

// Create service (in real application, this comes from DI)
var logger = loggerFactory.CreateLogger<UIProfileService>();
var profileService = new UIProfileService(logger);

// Initialize service
await profileService.InitializeAsync();
await profileService.StartAsync();

// List available profiles
var profiles = await profileService.GetProfilesAsync();
foreach (var profile in profiles)
{
    Console.WriteLine($"{profile.Name}: {profile.Description}");
}

// Get current active profile
var activeProfile = await profileService.GetActiveProfileAsync();
Console.WriteLine($"Active: {activeProfile?.Name}");

// Switch to a different profile
await profileService.ActivateProfileAsync("unity-style");

// Use capability pattern for service discovery
var hasCapability = await profileService.HasCapabilityAsync<IUIProfileProvider>();
var capability = await profileService.GetCapabilityAsync<IUIProfileProvider>();

// Clean up
await profileService.StopAsync();
await profileService.DisposeAsync();
```

## Service Registration

To integrate with the dependency injection container, register the service:

```csharp
// Example DI registration (adjust based on your DI container)
services.AddSingleton<UIProfileService>();
services.AddSingleton<IUIProfileProvider>(provider => provider.GetRequiredService<UIProfileService>());
services.AddSingleton<IService>(provider => provider.GetRequiredService<UIProfileService>());
```

## Configuration Support

The service supports loading profiles from configuration:

```json
{
  "UIProfiles": {
    "CustomProfile": {
      "Name": "Custom Profile",
      "Description": "A custom UI profile",
      "RenderingMode": "Custom",
      "InputMode": "Custom",
      "GraphicsBackend": "DirectX",
      "TuiMode": false,
      "Priority": 75
    }
  }
}
```

## Capability Pattern

The service implements the capability pattern for service discovery:

```csharp
// Check if service provides UI profile capabilities
var capabilities = await service.GetCapabilitiesAsync();
var hasUIProfileCapability = capabilities.Contains(typeof(IUIProfileProvider));

// Get the specific capability
var uiProfileProvider = await service.GetCapabilityAsync<IUIProfileProvider>();
```

## Thread Safety

The UIProfileService uses concurrent collections and proper locking to ensure thread-safe operations:
- Profile storage uses `ConcurrentDictionary<string, UIProfile>`
- Profile activation includes proper state management
- All async operations are cancellation-token aware

## Logging

The service provides comprehensive logging at different levels:
- **Information**: Service lifecycle, profile activation
- **Debug**: Profile creation, configuration loading
- **Trace**: Profile queries and retrievals
- **Warning**: Invalid operations, missing profiles

## Testing

Run the included tests to verify functionality:

```bash
cd dotnet
dotnet test GameConsole.UI.Services.Tests --logger "console;verbosity=normal"
```

The test suite includes:
- Service initialization tests
- Profile switching tests  
- Capability provider tests
- Error handling tests
- Usage example demonstration

## Future Extensions

The system is designed for extensibility:
- Add new profile types by extending `UIProfileSettings`
- Load profiles from external sources (databases, files, web APIs)
- Implement profile inheritance or composition
- Add profile validation and constraints
- Support hot-swapping of profiles without service restart

## Integration Points

The UI Profile system integrates with:
- **Service Registry**: For dependency injection
- **Configuration System**: For external profile definitions
- **Plugin System**: Profile-specific plugin loading
- **Graphics Services**: Backend selection based on profile
- **Input Services**: Input handling mode configuration