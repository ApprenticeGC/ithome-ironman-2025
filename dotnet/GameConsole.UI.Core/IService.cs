using GameConsole.Core.Abstractions;

namespace GameConsole.UI.Services;

/// <summary>
/// Core UI service interface for console-based user interface management.
/// Provides TUI-first approach with component management, event handling, and layout.
/// </summary>
public interface IService : GameConsole.Core.Abstractions.IService
{
    #region Component Management
    
    /// <summary>
    /// Creates a new UI window component.
    /// </summary>
    /// <param name="title">Window title.</param>
    /// <param name="bounds">Window bounds.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The created window component.</returns>
    Task<Core.IWindow> CreateWindowAsync(string title, Core.UIRect bounds, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new label component.
    /// </summary>
    /// <param name="text">Label text content.</param>
    /// <param name="bounds">Label bounds.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The created label component.</returns>
    Task<Core.ILabel> CreateLabelAsync(string text, Core.UIRect bounds, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new button component.
    /// </summary>
    /// <param name="text">Button text content.</param>
    /// <param name="bounds">Button bounds.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The created button component.</returns>
    Task<Core.IButton> CreateButtonAsync(string text, Core.UIRect bounds, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new text input component.
    /// </summary>
    /// <param name="placeholder">Placeholder text.</param>
    /// <param name="bounds">Input bounds.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The created text input component.</returns>
    Task<Core.ITextInput> CreateTextInputAsync(string placeholder, Core.UIRect bounds, CancellationToken cancellationToken = default);
    
    #endregion

    #region Focus Management
    
    /// <summary>
    /// Sets focus to a specific component.
    /// </summary>
    /// <param name="component">Component to focus.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetFocusAsync(Core.IUIComponent component, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the currently focused component.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The currently focused component, or null if none.</returns>
    Task<Core.IUIComponent?> GetFocusedComponentAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Moves focus to the next component in the focus chain.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task FocusNextAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Moves focus to the previous component in the focus chain.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task FocusPreviousAsync(CancellationToken cancellationToken = default);
    
    #endregion

    #region Rendering
    
    /// <summary>
    /// Renders all UI components to the current render target.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RenderAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Forces a refresh/redraw of all UI components.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RefreshAsync(CancellationToken cancellationToken = default);
    
    #endregion

    #region Events
    
    /// <summary>
    /// Event raised when a UI component is clicked.
    /// </summary>
    event EventHandler<Core.UIClickEvent>? ComponentClicked;
    
    /// <summary>
    /// Event raised when focus changes between components.
    /// </summary>
    event EventHandler<Core.UIFocusEvent>? FocusChanged;
    
    /// <summary>
    /// Event raised when a component's value changes.
    /// </summary>
    event EventHandler<Core.UIValueChangedEvent>? ValueChanged;
    
    #endregion

    #region Capabilities
    
    /// <summary>
    /// Gets the layout manager capability.
    /// </summary>
    ILayoutCapability? LayoutManager { get; }
    
    /// <summary>
    /// Gets the theme manager capability.
    /// </summary>
    IThemeCapability? ThemeManager { get; }
    
    #endregion
}

/// <summary>
/// Capability interface for layout management operations.
/// </summary>
public interface ILayoutCapability : ICapabilityProvider
{
    /// <summary>
    /// Automatically arranges components using the specified layout strategy.
    /// </summary>
    /// <param name="container">Container to arrange components within.</param>
    /// <param name="strategy">Layout strategy to use.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ArrangeComponentsAsync(Core.IUIComponent container, Core.LayoutStrategy strategy, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Calculates the preferred size for a component.
    /// </summary>
    /// <param name="component">Component to calculate size for.</param>
    /// <param name="availableSpace">Available space for the component.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The preferred size for the component.</returns>
    Task<Core.UISize> GetPreferredSizeAsync(Core.IUIComponent component, Core.UISize availableSpace, CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for theme and styling operations.
/// </summary>
public interface IThemeCapability : ICapabilityProvider
{
    /// <summary>
    /// Applies a theme to all UI components.
    /// </summary>
    /// <param name="theme">Theme to apply.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ApplyThemeAsync(Core.UITheme theme, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the current active theme.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The current theme.</returns>
    Task<Core.UITheme> GetCurrentThemeAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sets the style for a specific component.
    /// </summary>
    /// <param name="component">Component to style.</param>
    /// <param name="style">Style to apply.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetComponentStyleAsync(Core.IUIComponent component, Core.UIStyle style, CancellationToken cancellationToken = default);
}