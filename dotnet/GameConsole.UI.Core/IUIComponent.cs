namespace GameConsole.UI.Core;

/// <summary>
/// Base interface for all UI components.
/// </summary>
public interface IUIComponent
{
    /// <summary>
    /// Gets the unique identifier for this component.
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Gets or sets the component's position.
    /// </summary>
    Position Position { get; set; }
    
    /// <summary>
    /// Gets or sets the component's size.
    /// </summary>
    Size Size { get; set; }
    
    /// <summary>
    /// Gets or sets whether the component is visible.
    /// </summary>
    bool IsVisible { get; set; }
    
    /// <summary>
    /// Gets or sets whether the component is enabled for interaction.
    /// </summary>
    bool IsEnabled { get; set; }
    
    /// <summary>
    /// Gets the parent container, if any.
    /// </summary>
    IContainer? Parent { get; }
    
    /// <summary>
    /// Renders the component to the specified render target.
    /// </summary>
    /// <param name="target">The render target.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RenderAsync(IRenderTarget target, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for UI containers that can hold child components.
/// </summary>
public interface IContainer : IUIComponent
{
    /// <summary>
    /// Gets the collection of child components.
    /// </summary>
    IReadOnlyCollection<IUIComponent> Children { get; }
    
    /// <summary>
    /// Adds a child component to this container.
    /// </summary>
    /// <param name="child">The child component to add.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task AddChildAsync(IUIComponent child, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes a child component from this container.
    /// </summary>
    /// <param name="child">The child component to remove.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RemoveChildAsync(IUIComponent child, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the layout manager for this container.
    /// </summary>
    ILayoutManager? LayoutManager { get; set; }
    
    /// <summary>
    /// Performs layout for all child components.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task PerformLayoutAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for window components.
/// </summary>
public interface IWindow : IContainer, IAsyncDisposable
{
    /// <summary>
    /// Gets or sets the window title.
    /// </summary>
    string Title { get; set; }
    
    /// <summary>
    /// Gets or sets whether the window can be resized.
    /// </summary>
    bool IsResizable { get; set; }
    
    /// <summary>
    /// Gets or sets whether the window has a border.
    /// </summary>
    bool HasBorder { get; set; }
    
    /// <summary>
    /// Shows the window.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ShowAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Hides the window.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task HideAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Brings the window to the front.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task BringToFrontAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for render targets where UI components can be drawn.
/// </summary>
public interface IRenderTarget
{
    /// <summary>
    /// Gets the width of the render target.
    /// </summary>
    int Width { get; }
    
    /// <summary>
    /// Gets the height of the render target.
    /// </summary>
    int Height { get; }
    
    /// <summary>
    /// Clears the render target.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ClearAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for layout managers that arrange components within containers.
/// </summary>
public interface ILayoutManager
{
    /// <summary>
    /// Performs layout for the specified container and its children.
    /// </summary>
    /// <param name="container">The container to layout.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task PerformLayoutAsync(IContainer container, CancellationToken cancellationToken = default);
}