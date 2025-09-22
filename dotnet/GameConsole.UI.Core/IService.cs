using GameConsole.Core.Abstractions;
using GameConsole.UI.Core;

namespace GameConsole.UI.Services;

/// <summary>
/// Core UI service interface for console/TUI component management.
/// Provides component creation, layout management, and rendering capabilities.
/// </summary>
public interface IService : GameConsole.Core.Abstractions.IService
{
    #region Component Management
    
    /// <summary>
    /// Creates a new UI component of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of component to create.</typeparam>
    /// <param name="config">Component configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The created component.</returns>
    Task<T> CreateComponentAsync<T>(ComponentConfiguration config, CancellationToken cancellationToken = default)
        where T : class, IUIComponent;
    
    /// <summary>
    /// Gets a component by its unique ID.
    /// </summary>
    /// <param name="componentId">The component's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The component if found, null otherwise.</returns>
    Task<IUIComponent?> GetComponentAsync(string componentId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes a component from the UI.
    /// </summary>
    /// <param name="componentId">The component's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if removed, false if not found.</returns>
    Task<bool> RemoveComponentAsync(string componentId, CancellationToken cancellationToken = default);
    
    #endregion

    #region Layout Management
    
    /// <summary>
    /// Sets the root layout container for the UI.
    /// </summary>
    /// <param name="layout">The root layout container.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async operation.</returns>
    Task SetRootLayoutAsync(ILayoutContainer layout, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the current root layout container.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The root layout container.</returns>
    Task<ILayoutContainer?> GetRootLayoutAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Invalidates the layout, forcing a recalculation on next render.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async operation.</returns>
    Task InvalidateLayoutAsync(CancellationToken cancellationToken = default);
    
    #endregion

    #region Rendering
    
    /// <summary>
    /// Renders the UI to the console/terminal.
    /// </summary>
    /// <param name="renderContext">Rendering context information.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async operation.</returns>
    Task RenderAsync(RenderContext renderContext, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the current console dimensions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Console dimensions (width, height).</returns>
    Task<(int Width, int Height)> GetConsoleDimensionsAsync(CancellationToken cancellationToken = default);
    
    #endregion

    #region Events
    
    /// <summary>
    /// Event raised when a component's state changes.
    /// </summary>
    event EventHandler<ComponentStateChangedEventArgs>? ComponentStateChanged;
    
    /// <summary>
    /// Event raised when the layout needs recalculation.
    /// </summary>
    event EventHandler<LayoutInvalidatedEventArgs>? LayoutInvalidated;
    
    #endregion

    #region Capabilities
    
    /// <summary>
    /// Gets the dialog management capability.
    /// </summary>
    IDialogManagementCapability? DialogManager { get; }
    
    /// <summary>
    /// Gets the menu management capability.
    /// </summary>
    IMenuManagementCapability? MenuManager { get; }
    
    /// <summary>
    /// Gets the theming capability.
    /// </summary>
    IThemingCapability? ThemeManager { get; }
    
    /// <summary>
    /// Gets the focus management capability.
    /// </summary>
    IFocusManagementCapability? FocusManager { get; }
    
    #endregion
}

/// <summary>
/// Capability interface for dialog management operations.
/// </summary>
public interface IDialogManagementCapability : ICapabilityProvider
{
    /// <summary>
    /// Shows a modal dialog.
    /// </summary>
    /// <param name="dialog">The dialog to show.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The dialog result.</returns>
    Task<DialogResult> ShowDialogAsync(IDialog dialog, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Closes the current modal dialog.
    /// </summary>
    /// <param name="result">The result to return.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async operation.</returns>
    Task CloseDialogAsync(DialogResult result, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the currently active dialog, if any.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The active dialog or null.</returns>
    Task<IDialog?> GetActiveDialogAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for menu management operations.
/// </summary>
public interface IMenuManagementCapability : ICapabilityProvider
{
    /// <summary>
    /// Shows a context menu at the specified position.
    /// </summary>
    /// <param name="menu">The menu to show.</param>
    /// <param name="position">Position to show the menu at.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The selected menu item result.</returns>
    Task<MenuItemResult> ShowContextMenuAsync(IContextMenu menu, ConsolePosition position, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a menu bar for the application.
    /// </summary>
    /// <param name="menuItems">Items to include in the menu bar.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The created menu bar component.</returns>
    Task<IMenuBar> CreateMenuBarAsync(IEnumerable<IMenuItem> menuItems, CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for UI theming operations.
/// </summary>
public interface IThemingCapability : ICapabilityProvider
{
    /// <summary>
    /// Sets the current theme.
    /// </summary>
    /// <param name="theme">The theme to apply.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async operation.</returns>
    Task SetThemeAsync(UITheme theme, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the current theme.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The current theme.</returns>
    Task<UITheme> GetThemeAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets available themes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Available themes.</returns>
    Task<IEnumerable<UITheme>> GetAvailableThemesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for focus management operations.
/// </summary>
public interface IFocusManagementCapability : ICapabilityProvider
{
    /// <summary>
    /// Sets focus to the specified component.
    /// </summary>
    /// <param name="componentId">ID of the component to focus.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if focus was set, false otherwise.</returns>
    Task<bool> SetFocusAsync(string componentId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the currently focused component.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The focused component or null.</returns>
    Task<IUIComponent?> GetFocusedComponentAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Moves focus to the next focusable component.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if focus was moved, false otherwise.</returns>
    Task<bool> FocusNextAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Moves focus to the previous focusable component.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if focus was moved, false otherwise.</returns>
    Task<bool> FocusPreviousAsync(CancellationToken cancellationToken = default);
}