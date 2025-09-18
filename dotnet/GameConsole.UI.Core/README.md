# GameConsole.UI.Core - UI Framework Abstraction Layer

This project implements RFC-010-01: Create UI Framework Abstraction Layer, providing multi-modal UI framework abstraction supporting Console, Web, and Desktop interfaces.

## Overview

The UI Core library provides a comprehensive abstraction layer that enables GameConsole to work across different UI frameworks while maintaining a consistent API. The implementation follows the adapter pattern and supports virtual DOM concepts for performance optimization.

## Key Components

### 1. UIFrameworkType and UICapabilities

**UIFrameworkType** enum defines the supported framework types:
- `Console` - Console-based text user interface
- `Web` - Web-based user interface using HTML/CSS/JavaScript  
- `Desktop` - Desktop GUI user interface using native windowing systems

**UICapabilities** flags enum defines framework capabilities:
- `TextInput`, `FileSelection`, `ProgressDisplay`, `InteractiveNavigation`
- `RealTimeUpdates`, `KeyboardShortcuts`, `MouseInteraction`
- `ColorDisplay`, `FormInput`, `TableDisplay`
- `ResponsiveDesign`, `Accessibility`

### 2. UIContext

Framework-specific rendering context that provides:
- Command line arguments and state management
- Framework type and supported capabilities
- Framework-specific properties
- Immutable record pattern with builder methods (`WithState`, `WithProperty`)
- Capability checking (`SupportsCapability`)

### 3. IUIFramework Interface

Core framework abstraction interface extending `IService`:
- Framework type identification and capability reporting
- Asynchronous initialization and component factory creation
- Framework-specific component rendering and event handling
- Capability updates and styling/theming support
- Event system for capability changes

### 4. IUIComponent Interface

Cross-framework component interface providing:
- Component lifecycle management (initialization, rendering, updates)
- Hierarchical component structure with parent-child relationships
- Property system with typed getters and async setters
- Data binding with change notifications
- Visibility and enabled state management
- Event system for component interactions and data changes
- Async disposal pattern for resource cleanup

### 5. UIComponentFactory

Framework-agnostic component creation factory:
- Factory pattern implementation for creating framework-specific components
- Support for component creation with data, properties, or both
- Component type validation and capability checking
- Default properties management for component types
- Extensible registration system for custom component types

### 6. BasicUIComponent

Default implementation of `IUIComponent`:
- Full implementation of the component interface
- Event-driven architecture with proper notification system
- Thread-safe property and child management
- Comprehensive lifecycle management
- Built-in support for common component types (text, button, input, container, list, table, progress, menu)

## Features

### Cross-Framework Compatibility
- Single API works across Console, Web, and Desktop frameworks
- Framework-specific adaptations through the adapter pattern
- Capability-based feature detection and graceful degradation

### Responsive Design Support
- Framework-agnostic responsive design capabilities
- Context-aware rendering based on available screen space and input methods
- Automatic adaptation to different display modalities

### Event Handling and Data Binding
- Comprehensive event system for user interactions and state changes
- Two-way data binding with change notifications
- Framework-agnostic event routing and handling

### Performance Optimization
- Virtual DOM concepts for efficient rendering
- Minimal object allocation through immutable patterns
- Asynchronous operations with proper cancellation support

### Accessibility Features
- Built-in accessibility capability detection
- Framework-agnostic accessibility features
- Support for assistive technologies across different UI frameworks

## Architecture Integration

This implementation follows the GameConsole 4-tier architecture:
- **Tier 1**: Core abstractions and interfaces (IUIFramework, IUIComponent)
- **Tier 2**: Framework adapters and proxies (future implementations)
- **Tier 3**: UI profiles and configurations (future implementations)  
- **Tier 4**: Concrete framework providers (Console, Web, Desktop implementations)

## Dependencies

- `GameConsole.Core.Abstractions` - Base service interfaces and patterns
- Compatible with RFC-001-01 (Base service interfaces)
- Ready for integration with RFC-002-02 (Input category services)

## Usage Example

```csharp
// Create a UI context for Console framework
var context = UIContext.Create(
    UIFrameworkType.Console, 
    UICapabilities.TextInput | UICapabilities.ColorDisplay
);

// Create a component factory
var factory = new UIComponentFactory(UIFrameworkType.Console);

// Create and configure components
var button = await factory.CreateComponentAsync("button", context);
await button.SetPropertyAsync("text", "Click Me");
await button.SetPropertyAsync("color", "blue");

// Handle component events
button.ComponentEvent += (sender, args) => {
    if (args.EventType == "clicked") {
        Console.WriteLine("Button was clicked!");
    }
};

// Render the component
var renderData = await button.RenderAsync(context);
```

## Testing

The implementation includes comprehensive unit tests covering:
- UIContext creation, immutability, and capability checking
- UIComponentFactory component creation and validation
- BasicUIComponent lifecycle, events, and data binding
- Cross-framework compatibility scenarios
- Error handling and edge cases

Total test coverage: 27 test methods validating all major functionality.

## Future Extensions

This abstraction layer provides the foundation for:
- Concrete framework implementations (Console TUI, Web SPA, Desktop WPF/Avalonia)
- Advanced UI components (charts, grids, rich text editors)
- Theme and styling systems
- Animation and transition frameworks
- Internationalization and localization support