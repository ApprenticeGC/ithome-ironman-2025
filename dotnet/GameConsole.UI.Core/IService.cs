using GameConsole.Core.Abstractions;
using System.Reactive;

namespace GameConsole.UI.Core;

/// <summary>
/// Core UI service interface for framework-agnostic UI operations.
/// Provides unified interface for managing UI frameworks and components across Console, Web, and Desktop.
/// </summary>
public interface IService : GameConsole.Core.Abstractions.IService
{
    #region Framework Management

    /// <summary>
    /// Gets the currently active UI framework.
    /// </summary>
    IUIFramework? CurrentFramework { get; }

    /// <summary>
    /// Gets all registered UI frameworks.
    /// </summary>
    IReadOnlyList<IUIFramework> RegisteredFrameworks { get; }

    /// <summary>
    /// Registers a UI framework with the service.
    /// </summary>
    /// <param name="framework">Framework to register.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the registration operation.</returns>
    Task RegisterFrameworkAsync(IUIFramework framework, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a specific UI framework by type.
    /// </summary>
    /// <param name="frameworkType">Type of framework to activate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the activation operation.</returns>
    Task ActivateFrameworkAsync(UIFrameworkType frameworkType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Switches to a different UI framework at runtime.
    /// </summary>
    /// <param name="targetFramework">Framework to switch to.</param>
    /// <param name="preserveState">Whether to preserve current UI state during switch.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the framework switch operation.</returns>
    Task SwitchFrameworkAsync(UIFrameworkType targetFramework, bool preserveState = true, CancellationToken cancellationToken = default);

    #endregion

    #region Component Management

    /// <summary>
    /// Creates a new UI component using the current framework.
    /// </summary>
    /// <param name="componentType">Type of component to create.</param>
    /// <param name="properties">Initial component properties.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The created component.</returns>
    Task<IUIComponent> CreateComponentAsync(string componentType, Dictionary<string, object>? properties = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a component by its ID.
    /// </summary>
    /// <param name="componentId">ID of the component to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The component with the specified ID, or null if not found.</returns>
    Task<IUIComponent?> GetComponentAsync(string componentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes and disposes a component.
    /// </summary>
    /// <param name="componentId">ID of the component to remove.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the removal operation.</returns>
    Task RemoveComponentAsync(string componentId, CancellationToken cancellationToken = default);

    #endregion

    #region Rendering Operations

    /// <summary>
    /// Sets the root component for the UI tree.
    /// </summary>
    /// <param name="rootComponent">Component to use as root.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the operation.</returns>
    Task SetRootComponentAsync(IUIComponent rootComponent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders the entire UI tree.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the rendering operation.</returns>
    Task RenderAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Forces a re-render of the entire UI tree.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the re-render operation.</returns>
    Task InvalidateAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Event Handling

    /// <summary>
    /// Observable stream of all UI events from the current framework.
    /// </summary>
    IObservable<UIEvent> UIEvents { get; }

    /// <summary>
    /// Subscribe to UI events of a specific type.
    /// </summary>
    /// <typeparam name="T">Type of UI event to subscribe to.</typeparam>
    /// <returns>Observable stream of the specified event type.</returns>
    IObservable<T> ObserveEvents<T>() where T : UIEvent;

    #endregion

    #region Data Binding

    /// <summary>
    /// Gets the data binding capability if supported.
    /// </summary>
    IUIDataBindingCapability? DataBinding { get; }

    /// <summary>
    /// Gets the theme management capability if supported.
    /// </summary>
    IUIThemeCapability? ThemeManager { get; }

    /// <summary>
    /// Gets the responsive layout capability if supported.
    /// </summary>
    IUIResponsiveCapability? ResponsiveLayout { get; }

    #endregion

    #region Framework Detection

    /// <summary>
    /// Detects the most appropriate UI framework for the current environment.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The recommended framework type for the current environment.</returns>
    Task<UIFrameworkType> DetectOptimalFrameworkAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific framework type is available in the current environment.
    /// </summary>
    /// <param name="frameworkType">Framework type to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the framework is available.</returns>
    Task<bool> IsFrameworkAvailableAsync(UIFrameworkType frameworkType, CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Capability interface for data binding functionality.
/// </summary>
public interface IUIDataBindingCapability : ICapabilityProvider
{
    /// <summary>
    /// Binds a component property to a data source.
    /// </summary>
    /// <param name="component">Component to bind.</param>
    /// <param name="propertyName">Property name to bind.</param>
    /// <param name="dataSource">Data source object.</param>
    /// <param name="sourcePath">Property path in the data source.</param>
    /// <param name="bindingMode">Type of binding (OneWay, TwoWay, etc.).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the binding operation.</returns>
    Task BindPropertyAsync(IUIComponent component, string propertyName, object dataSource, string sourcePath, UIBindingMode bindingMode = UIBindingMode.OneWay, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a property binding.
    /// </summary>
    /// <param name="component">Component to unbind.</param>
    /// <param name="propertyName">Property name to unbind.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the unbind operation.</returns>
    Task UnbindPropertyAsync(IUIComponent component, string propertyName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates all bindings for a specific data source.
    /// </summary>
    /// <param name="dataSource">Data source that changed.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the update operation.</returns>
    Task UpdateBindingsAsync(object dataSource, CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for theme and styling management.
/// </summary>
public interface IUIThemeCapability : ICapabilityProvider
{
    /// <summary>
    /// Gets the current active theme.
    /// </summary>
    string? CurrentTheme { get; }

    /// <summary>
    /// Gets all available themes.
    /// </summary>
    IReadOnlyList<string> AvailableThemes { get; }

    /// <summary>
    /// Applies a theme to the UI.
    /// </summary>
    /// <param name="themeName">Name of the theme to apply.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the theme application operation.</returns>
    Task ApplyThemeAsync(string themeName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a custom theme from style definitions.
    /// </summary>
    /// <param name="themeName">Name for the new theme.</param>
    /// <param name="styleDefinitions">Style definitions for the theme.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the theme creation operation.</returns>
    Task CreateThemeAsync(string themeName, Dictionary<string, UIStyle> styleDefinitions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the style for a specific component type in the current theme.
    /// </summary>
    /// <param name="componentType">Component type to get style for.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Style for the component type, or null if not defined.</returns>
    Task<UIStyle?> GetThemeStyleAsync(string componentType, CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for responsive layout management.
/// </summary>
public interface IUIResponsiveCapability : ICapabilityProvider
{
    /// <summary>
    /// Gets the current responsive breakpoint.
    /// </summary>
    UIBreakpoint? CurrentBreakpoint { get; }

    /// <summary>
    /// Configures responsive breakpoints.
    /// </summary>
    /// <param name="breakpoints">Collection of breakpoints to use.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the configuration operation.</returns>
    Task ConfigureBreakpointsAsync(IEnumerable<UIBreakpoint> breakpoints, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies responsive styles based on the current breakpoint.
    /// </summary>
    /// <param name="component">Component to apply responsive styles to.</param>
    /// <param name="responsiveStyles">Breakpoint-specific styles.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the application operation.</returns>
    Task ApplyResponsiveStyleAsync(IUIComponent component, Dictionary<string, UIStyle> responsiveStyles, CancellationToken cancellationToken = default);

    /// <summary>
    /// Observable stream of breakpoint changes.
    /// </summary>
    IObservable<UIBreakpoint> BreakpointChanges { get; }
}

/// <summary>
/// Defines the data binding mode for component properties.
/// </summary>
public enum UIBindingMode
{
    /// <summary>
    /// Data flows from source to target only.
    /// </summary>
    OneWay,
    
    /// <summary>
    /// Data flows both ways between source and target.
    /// </summary>
    TwoWay,
    
    /// <summary>
    /// Data is set once from source to target.
    /// </summary>
    OneTime
}