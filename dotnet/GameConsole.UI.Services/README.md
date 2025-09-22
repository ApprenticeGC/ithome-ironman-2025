# GameConsole UI Services

This directory contains the console UI service implementations for the GameConsole 4-tier architecture.

## Services

### ConsoleRenderingService
- **Purpose**: Core console rendering service handling text-based UI output
- **Categories**: UI, Console, Rendering
- **Features**: 
  - ANSI escape code support for colors and styling
  - Efficient console drawing operations
  - Cross-platform console compatibility
  - Text positioning and alignment
  - Console buffer management

### TextComponentService
- **Purpose**: Text component creation and management for console UI
- **Categories**: UI, Text, Components
- **Features**:
  - Text component lifecycle management
  - Dynamic text content updates
  - Color and style management
  - Text alignment and positioning
  - Component hierarchy support

## Architecture

The UI services follow the 4-tier GameConsole architecture:

- **Tier 1**: `GameConsole.UI.Core` - UI component interfaces and common types
- **Tier 3**: `GameConsole.UI.Services` - Console UI service implementations (this project)

All services:
- Inherit from `BaseUIService` which provides common functionality
- Use the `ServiceAttribute` for category-based registration
- Follow async/await patterns with `CancellationToken` support
- Implement proper resource management and cleanup
- Support capability-based service discovery through `ICapabilityProvider`

## Usage

Services are designed to be registered with the `ServiceProvider` using category-based discovery:

```csharp
// Register all UI services
serviceProvider.RegisterFromAttributes(typeof(ConsoleRenderingService).Assembly, "UI");

// Get the main UI service
var uiService = serviceProvider.GetService<GameConsole.UI.Core.IService>();

// Create and display text components
var textComponent = await uiService.TextComponents?.CreateTextComponentAsync(
    "welcome", "Welcome to GameConsole!", new Position(10, 5));

await uiService.AddComponentAsync(textComponent);
await uiService.RenderAsync();
```

## Console UI Features

- **TUI-First Design**: Built specifically for terminal user interfaces
- **ANSI Color Support**: Full color support where available with graceful fallback
- **Cross-Platform**: Works on Windows, Linux, and macOS consoles
- **Component-Based**: Hierarchical UI component system
- **Efficient Rendering**: Minimized console operations for smooth performance
- **Input Integration**: Designed to work with existing Input services

## Performance Considerations

- Console operations are batched to minimize cursor movements
- ANSI escape sequences used for efficient color changes
- Component invalidation system to avoid unnecessary redraws
- Lock-based thread safety for component collections
- Efficient text positioning calculations
- Fallback handling for limited console environments

## Platform Support

- **Windows**: Full ANSI support on Windows 10+ with virtual terminal processing
- **Linux/macOS**: Native ANSI escape sequence support
- **Limited Consoles**: Graceful degradation for basic text-only output
- **CI/CD Environments**: Automatic color detection and fallback