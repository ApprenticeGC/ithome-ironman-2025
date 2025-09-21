# GameConsole.UI.Core

Multi-modal UI framework abstraction layer supporting Console, Web, and Desktop interfaces.

## Overview

This project implements the core contracts and interfaces for the GameConsole UI framework abstraction layer as specified in RFC-010-01. It provides a framework-agnostic way to define, create, and manage UI components across different UI frameworks (Console/TUI, Web, Desktop GUI).

## Architecture

The UI framework follows the 4-tier GameConsole architecture:

- **Tier 1 (This project)**: Core interface contracts and abstractions
- **Tier 2**: Proxy generators (future implementation)
- **Tier 3**: Framework adapters and profiles (future implementation)
- **Tier 4**: Framework-specific providers (Spectre.Console, Blazor, WPF, etc.)

## Key Interfaces

### IUIFramework
The main interface for UI framework operations:
- Framework lifecycle management (Initialize, Start, Stop)
- Component rendering and updates
- Input event handling
- Focus management
- Theme application
- Accessibility configuration

### IUIComponent
Base interface for all UI components:
- Cross-framework component abstraction
- Virtual DOM-like rendering
- Property management and validation
- Event handling
- Child component management

### IUIComponentFactory
Factory for creating UI components:
- Framework-agnostic component creation
- Component tree creation from definitions
- Template support (capability interface)
- Data binding support (capability interface)
- Styling capabilities

## Core Types

### UIFrameworkType
Supported framework types:
- `Console` - Text-based user interface (TUI)
- `Web` - HTML/CSS/JavaScript interfaces
- `Desktop` - Native GUI applications
- `Mobile` - Mobile application interfaces
- `Headless` - API-only interfaces

### UICapabilities
Framework capability flags:
- Text input and editing
- File selection
- Progress display
- Interactive navigation
- Real-time updates
- Keyboard shortcuts
- Mouse interaction
- Color display
- Form input
- Media display
- Accessibility features
- And more...

### UIContext
Runtime context for framework operations:
- Command line arguments
- Application state
- Framework configuration
- User preferences
- Theme information
- Accessibility settings
- Viewport information

## Features

### Multi-Modal Support
- **Console**: Text-based interfaces with TUI libraries
- **Web**: Browser-based interfaces with HTML/CSS/JavaScript
- **Desktop**: Native GUI applications (WPF, WinUI, etc.)

### Responsive Design
- Adaptive layouts that adjust to available space
- Responsive layouts with breakpoints
- Fluid layouts that scale continuously
- Framework-specific optimizations

### Accessibility
- High contrast mode support
- Screen reader compatibility
- Keyboard navigation
- Focus indicators
- Text scaling
- Reduced motion preferences

### Performance
- Virtual DOM concepts for efficient rendering
- Update prioritization system
- Performance metrics tracking
- Memory usage monitoring

### Extensibility
- Capability-based service discovery
- Custom component registration
- Template and markup support
- Data binding capabilities
- Custom styling and theming

## Event System

Comprehensive event system for UI interactions:
- `UIRenderEvent` - Component rendering events
- `UIInteractionEvent` - User interaction events
- `UIStateChangeEvent` - Property and state changes
- `UIFrameworkEvent` - Framework lifecycle events
- `UIErrorEvent` - Error handling and reporting

## Usage Example

```csharp
// Create a UI framework instance (implementation-specific)
var framework = serviceProvider.GetRequiredService<IUIFramework>();

// Create a context
var context = new UIContext
{
    FrameworkType = UIFrameworkType.Console,
    SupportedCapabilities = UICapabilities.TextInput | UICapabilities.ColorDisplay
};

// Initialize the framework
await framework.InitializeFrameworkAsync(context);

// Create components using the factory
var button = await framework.ComponentFactory.CreateComponentAsync<IUIButton>(
    "my-button",
    new Dictionary<string, object> { ["Text"] = "Click Me!" }
);

// Render the UI
await framework.RenderAsync(button);
```

## Dependencies

- `GameConsole.Core.Abstractions` - Base service interfaces
- `System.Reactive` - Reactive extensions for event streams

## Testing

The project includes comprehensive tests covering:
- Type definitions and enums
- Context initialization and behavior
- Event system functionality
- Interface contracts

Run tests with:
```bash
dotnet test GameConsole.UI.Core.Tests
```

## Future Extensions

This core layer enables future implementations:

1. **Framework Providers** (Tier 4):
   - Spectre.Console for rich console UIs
   - Blazor for web interfaces
   - WPF/WinUI for desktop applications

2. **Proxy Generators** (Tier 2):
   - Source-generated proxies for performance
   - Automatic retry and timeout handling
   - Service discovery integration

3. **Profile Adapters** (Tier 3):
   - Game mode configurations
   - Editor mode configurations
   - Role-based UI customizations

## Architecture Notes

- Uses adapter pattern for framework integration
- Supports CSS/styling abstraction for visual frameworks
- Implements virtual DOM concepts for performance
- Designed with accessibility as a first-class concern
- Follows established service patterns from the GameConsole ecosystem