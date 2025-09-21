using GameConsole.Core.Abstractions;

namespace GameConsole.UI.Core;

/// <summary>
/// Factory interface for creating UI components in a framework-agnostic way.
/// Provides methods to create, configure, and manage UI components across different frameworks.
/// </summary>
public interface IUIComponentFactory : ICapabilityProvider
{
    /// <summary>
    /// Gets the supported UI framework type for this factory.
    /// </summary>
    UIFrameworkType SupportedFrameworkType { get; }

    /// <summary>
    /// Gets the capabilities supported by components created by this factory.
    /// </summary>
    UICapabilities SupportedCapabilities { get; }

    /// <summary>
    /// Gets all available component types that this factory can create.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns available component descriptors.</returns>
    Task<IEnumerable<UIComponentDescriptor>> GetAvailableComponentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a UI component of the specified type.
    /// </summary>
    /// <param name="componentType">The type name of the component to create.</param>
    /// <param name="id">The unique identifier for the component.</param>
    /// <param name="properties">Initial properties for the component.</param>
    /// <param name="context">UI context for component creation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async creation operation.</returns>
    Task<IUIComponent> CreateComponentAsync(
        string componentType,
        string id,
        Dictionary<string, object>? properties = null,
        UIContext? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a UI component of the specified generic type.
    /// </summary>
    /// <typeparam name="T">The component type to create.</typeparam>
    /// <param name="id">The unique identifier for the component.</param>
    /// <param name="properties">Initial properties for the component.</param>
    /// <param name="context">UI context for component creation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async creation operation.</returns>
    Task<T> CreateComponentAsync<T>(
        string id,
        Dictionary<string, object>? properties = null,
        UIContext? context = null,
        CancellationToken cancellationToken = default) where T : class, IUIComponent;

    /// <summary>
    /// Creates multiple components from a component tree definition.
    /// </summary>
    /// <param name="componentTree">The component tree to create.</param>
    /// <param name="context">UI context for component creation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async creation operation.</returns>
    Task<IUIComponent> CreateComponentTreeAsync(
        UIComponentTreeDefinition componentTree,
        UIContext? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clones an existing component with optional property overrides.
    /// </summary>
    /// <param name="source">The component to clone.</param>
    /// <param name="newId">The ID for the cloned component.</param>
    /// <param name="propertyOverrides">Properties to override in the clone.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async cloning operation.</returns>
    Task<IUIComponent> CloneComponentAsync(
        IUIComponent source,
        string newId,
        Dictionary<string, object>? propertyOverrides = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a component definition before creation.
    /// </summary>
    /// <param name="componentType">The component type to validate.</param>
    /// <param name="properties">The properties to validate.</param>
    /// <param name="context">UI context for validation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async validation operation.</returns>
    Task<UIValidationResult> ValidateComponentDefinitionAsync(
        string componentType,
        Dictionary<string, object>? properties = null,
        UIContext? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a custom component type with the factory.
    /// </summary>
    /// <param name="componentType">The component type to register.</param>
    /// <param name="descriptor">The component descriptor.</param>
    /// <param name="factory">The factory function to create the component.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async registration operation.</returns>
    Task RegisterComponentTypeAsync(
        Type componentType,
        UIComponentDescriptor descriptor,
        Func<string, Dictionary<string, object>?, UIContext?, CancellationToken, Task<IUIComponent>> factory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters a custom component type from the factory.
    /// </summary>
    /// <param name="componentType">The component type to unregister.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async unregistration operation.</returns>
    Task UnregisterComponentTypeAsync(Type componentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the factory can create a component of the specified type.
    /// </summary>
    /// <param name="componentType">The component type to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async check operation.</returns>
    Task<bool> CanCreateComponentAsync(string componentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metadata about a specific component type.
    /// </summary>
    /// <param name="componentType">The component type to get metadata for.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns component metadata.</returns>
    Task<UIComponentDescriptor?> GetComponentDescriptorAsync(string componentType, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines a component tree structure for bulk creation.
/// </summary>
public record UIComponentTreeDefinition
{
    /// <summary>
    /// Gets the root component definition.
    /// </summary>
    public UIComponentDefinition Root { get; init; } = new();

    /// <summary>
    /// Gets additional metadata for the tree.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Defines a single component within a component tree.
/// </summary>
public record UIComponentDefinition
{
    /// <summary>
    /// Gets the component type name.
    /// </summary>
    public string ComponentType { get; init; } = string.Empty;

    /// <summary>
    /// Gets the component ID.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Gets the component properties.
    /// </summary>
    public Dictionary<string, object> Properties { get; init; } = new();

    /// <summary>
    /// Gets the child component definitions.
    /// </summary>
    public List<UIComponentDefinition> Children { get; init; } = new();

    /// <summary>
    /// Gets the component style.
    /// </summary>
    public UIStyle? Style { get; init; }

    /// <summary>
    /// Gets the component layout.
    /// </summary>
    public UILayout? Layout { get; init; }

    /// <summary>
    /// Gets event handler definitions.
    /// </summary>
    public Dictionary<string, string> EventHandlers { get; init; } = new();

    /// <summary>
    /// Gets conditional rendering expressions.
    /// </summary>
    public string? RenderCondition { get; init; }

    /// <summary>
    /// Gets data binding expressions.
    /// </summary>
    public Dictionary<string, string> DataBindings { get; init; } = new();
}

/// <summary>
/// Base capability interface for factory-specific capabilities.
/// </summary>
public interface IUIComponentFactoryCapability : ICapabilityProvider
{
    /// <summary>
    /// Gets the factory that provides this capability.
    /// </summary>
    IUIComponentFactory Factory { get; }
}

/// <summary>
/// Capability for creating components from templates or markup.
/// </summary>
public interface IUITemplateCapability : IUIComponentFactoryCapability
{
    /// <summary>
    /// Creates components from a template string.
    /// </summary>
    /// <param name="template">The template string (HTML, XAML, etc.).</param>
    /// <param name="context">UI context for template processing.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async template processing.</returns>
    Task<IUIComponent> CreateFromTemplateAsync(
        string template,
        UIContext? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a template string.
    /// </summary>
    /// <param name="template">The template to validate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async validation.</returns>
    Task<UIValidationResult> ValidateTemplateAsync(string template, CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability for data binding and reactive components.
/// </summary>
public interface IUIDataBindingCapability : IUIComponentFactoryCapability
{
    /// <summary>
    /// Binds a component property to a data source.
    /// </summary>
    /// <param name="component">The component to bind.</param>
    /// <param name="propertyName">The property to bind.</param>
    /// <param name="dataSource">The data source.</param>
    /// <param name="binding">The binding expression.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async binding operation.</returns>
    Task BindPropertyAsync(
        IUIComponent component,
        string propertyName,
        object dataSource,
        string binding,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unbinds a component property from its data source.
    /// </summary>
    /// <param name="component">The component to unbind.</param>
    /// <param name="propertyName">The property to unbind.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async unbinding operation.</returns>
    Task UnbindPropertyAsync(
        IUIComponent component,
        string propertyName,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability for styling and theming components.
/// </summary>
public interface IUIStylingCapability : IUIComponentFactoryCapability
{
    /// <summary>
    /// Applies a theme to a component.
    /// </summary>
    /// <param name="component">The component to style.</param>
    /// <param name="theme">The theme to apply.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async styling operation.</returns>
    Task ApplyThemeAsync(
        IUIComponent component,
        UITheme theme,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies CSS-like styling to a component.
    /// </summary>
    /// <param name="component">The component to style.</param>
    /// <param name="styles">The styles to apply.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async styling operation.</returns>
    Task ApplyStylesAsync(
        IUIComponent component,
        UIStyle styles,
        CancellationToken cancellationToken = default);
}