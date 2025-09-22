# GameConsole.UI.Core

UI Framework Abstraction Layer for GameConsole supporting Console, Web, and Desktop interfaces.

## Overview

This library provides a multi-modal UI framework abstraction that enables cross-framework component development and rendering. It supports Console (CLI/TUI), Web, and Desktop frameworks through a unified set of interfaces and abstractions.

## Key Features

- **Framework Agnostic**: Write components once, render on any supported framework
- **Responsive Design**: Built-in support for adaptive layouts across different UI modalities  
- **Event System**: Comprehensive event handling for interactions, data binding, and lifecycle
- **Component Library**: Rich set of UI component interfaces (buttons, inputs, menus, etc.)
- **Styling Support**: Cross-framework styling abstraction with CSS-like properties
- **Accessibility**: Built-in accessibility features across all supported frameworks
- **Virtual DOM Concepts**: Performance optimizations through efficient rendering patterns

## Core Interfaces

### IUIFramework
Main framework abstraction that manages UI lifecycle, rendering, and component creation.

```csharp
public interface IUIFramework : IService
{
    FrameworkType FrameworkType { get; }
    UICapabilities SupportedCapabilities { get; }
    IUIRenderer Renderer { get; }
    IUIComponentFactory ComponentFactory { get; }
    
    Task<bool> ActivateAsync(UIContext context, CancellationToken cancellationToken = default);
    Task<RenderResult> RenderRootAsync(IUIComponent rootComponent, UIContext context, CancellationToken cancellationToken = default);
    // ... additional methods
}
```

### IUIComponent
Base interface for all UI components with cross-framework compatibility.

```csharp
public interface IUIComponent : IAsyncDisposable
{
    string Id { get; }
    ComponentType ComponentType { get; }
    Dictionary<string, object> State { get; }
    StyleContext Style { get; set; }
    
    Task<RenderResult> RenderAsync(IUIRenderer renderer, UIContext context, CancellationToken cancellationToken = default);
    Task SetStateAsync<T>(string key, T value, CancellationToken cancellationToken = default);
    // ... events and additional methods
}
```

### IUIComponentFactory
Factory for creating framework-appropriate components.

```csharp
public interface IUIComponentFactory
{
    FrameworkType FrameworkType { get; }
    IReadOnlySet<ComponentType> SupportedComponents { get; }
    
    Task<IUIButton> CreateButtonAsync(string id, string text, Dictionary<string, object>? configuration = null, CancellationToken cancellationToken = default);
    Task<IUITextInput> CreateTextInputAsync(string id, string? placeholder = null, string? initialValue = null, Dictionary<string, object>? configuration = null, CancellationToken cancellationToken = default);
    // ... methods for other component types
}
```

## Framework Types

- **Console**: Text-based CLI and TUI interfaces
- **Web**: Browser-based web applications  
- **Desktop**: Native desktop GUI applications

## Component Types

The library supports a comprehensive set of UI components:

- **Button**: Clickable buttons with command support
- **TextInput**: Text input fields with validation
- **Label**: Text display labels
- **Panel**: Container panels with layout options
- **Menu**: Selection menus with single/multi-select
- **List**: Data lists with templating support
- **Table**: Tabular data display with row selection
- **ProgressBar**: Progress indication (determinate/indeterminate)
- **Checkbox**: Boolean selection controls
- **Dropdown**: Dropdown selection controls
- **Dialog**: Modal dialogs with actions

## Usage Example

```csharp
// Create a UI context
var context = UIContext.Create(
    mode: UIMode.TUI,
    frameworkType: FrameworkType.Console,
    capabilities: UICapabilities.TextInput | UICapabilities.MouseInteraction
);

// Initialize framework (implementation-specific)
var framework = new ConsoleUIFramework();
await framework.InitializeAsync();
await framework.ActivateAsync(context);

// Create components through factory
var button = await framework.ComponentFactory.CreateButtonAsync("btn-1", "Click Me");
var input = await framework.ComponentFactory.CreateTextInputAsync("input-1", "Enter text...");

// Handle events
button.Clicked += async (evt) => 
{
    var text = input.GetState<string>("Value");
    Console.WriteLine($"Button clicked with input: {text}");
};

// Create a panel to hold components
var panel = await framework.ComponentFactory.CreatePanelAsync("main-panel");
await panel.AddChildAsync(input);
await panel.AddChildAsync(button);

// Render the UI
await framework.RenderRootAsync(panel, context);
```

## Event System

The library provides a comprehensive event system for UI interactions:

- **UIInteractionEvent**: User interactions (clicks, selections, etc.)
- **UIDataBindingEvent**: Component state changes
- **UIFocusEvent**: Focus changes between components
- **UIValidationEvent**: Form validation results
- **UILifecycleEvent**: Component lifecycle (created, mounted, updated, etc.)

## Styling

Cross-framework styling support through `StyleContext`:

```csharp
var style = StyleContext.Empty
    .WithProperty("color", "blue")
    .WithProperty("fontSize", 16)
    .WithProperty("padding", "8px");

component.Style = style;
```

## Architecture

The UI framework follows the 4-tier GameConsole architecture:

- **Tier 1**: Core contracts and interfaces (this library)
- **Tier 2**: Proxy implementations 
- **Tier 3**: Framework-specific service implementations
- **Tier 4**: Provider implementations (Console, Web, Desktop)

## Dependencies

- GameConsole.Core.Abstractions: Base service interfaces
- GameConsole.Input.Core: Input event handling

## Target Framework

- .NET 8.0

## License

See the repository license for details.