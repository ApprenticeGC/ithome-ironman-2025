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
    /// Gets or sets the position of the component.
    /// </summary>
    Position Position { get; set; }
    
    /// <summary>
    /// Gets or sets the size of the component.
    /// </summary>
    Size Size { get; set; }
    
    /// <summary>
    /// Gets the bounds rectangle of the component.
    /// </summary>
    Rectangle Bounds { get; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the component is visible.
    /// </summary>
    bool IsVisible { get; set; }
    
    /// <summary>
    /// Renders the component using the provided UI service.
    /// </summary>
    /// <param name="uiService">The UI service to render with.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async render operation.</returns>
    Task RenderAsync(IService uiService, CancellationToken cancellationToken = default);
}

/// <summary>
/// Base abstract implementation of a UI component.
/// </summary>
public abstract class UIComponentBase : IUIComponent
{
    /// <summary>
    /// Initializes a new instance of the UIComponentBase class.
    /// </summary>
    /// <param name="id">The unique identifier for the component.</param>
    /// <param name="position">The initial position of the component.</param>
    /// <param name="size">The initial size of the component.</param>
    protected UIComponentBase(string id, Position position, Size size)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Position = position;
        Size = size;
        IsVisible = true;
    }
    
    /// <inheritdoc />
    public string Id { get; }
    
    /// <inheritdoc />
    public Position Position { get; set; }
    
    /// <inheritdoc />
    public Size Size { get; set; }
    
    /// <inheritdoc />
    public bool IsVisible { get; set; }
    
    /// <inheritdoc />
    public Rectangle Bounds => new Rectangle(Position, Size);
    
    /// <inheritdoc />
    public abstract Task RenderAsync(IService uiService, CancellationToken cancellationToken = default);
}