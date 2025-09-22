namespace GameConsole.UI.Core;

/// <summary>
/// Base interface for all UI components in the console interface system.
/// </summary>
public interface IUIComponent
{
    /// <summary>
    /// Gets the unique identifier for this component.
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Gets or sets the component's bounds (position and size).
    /// </summary>
    UIRect Bounds { get; set; }
    
    /// <summary>
    /// Gets or sets whether the component is visible.
    /// </summary>
    bool IsVisible { get; set; }
    
    /// <summary>
    /// Gets or sets whether the component is enabled (can receive input).
    /// </summary>
    bool IsEnabled { get; set; }
    
    /// <summary>
    /// Gets or sets whether the component can receive focus.
    /// </summary>
    bool CanFocus { get; set; }
    
    /// <summary>
    /// Gets whether the component currently has focus.
    /// </summary>
    bool HasFocus { get; }
    
    /// <summary>
    /// Gets the parent component, if any.
    /// </summary>
    IUIComponent? Parent { get; }
    
    /// <summary>
    /// Gets the child components.
    /// </summary>
    IReadOnlyList<IUIComponent> Children { get; }
    
    /// <summary>
    /// Event raised when the component is clicked.
    /// </summary>
    event EventHandler<UIClickEvent>? Clicked;
    
    /// <summary>
    /// Event raised when the component gains or loses focus.
    /// </summary>
    event EventHandler<UIFocusEvent>? FocusChanged;
    
    /// <summary>
    /// Adds a child component to this component.
    /// </summary>
    /// <param name="child">Child component to add.</param>
    void AddChild(IUIComponent child);
    
    /// <summary>
    /// Removes a child component from this component.
    /// </summary>
    /// <param name="child">Child component to remove.</param>
    /// <returns>True if the child was removed, false otherwise.</returns>
    bool RemoveChild(IUIComponent child);
    
    /// <summary>
    /// Renders the component.
    /// </summary>
    /// <param name="context">Rendering context.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RenderAsync(UIRenderContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for window components that can contain other UI elements.
/// </summary>
public interface IWindow : IUIComponent
{
    /// <summary>
    /// Gets or sets the window title.
    /// </summary>
    string Title { get; set; }
    
    /// <summary>
    /// Gets or sets whether the window has a border.
    /// </summary>
    bool HasBorder { get; set; }
    
    /// <summary>
    /// Gets or sets whether the window can be resized.
    /// </summary>
    bool IsResizable { get; set; }
    
    /// <summary>
    /// Gets or sets whether the window is modal.
    /// </summary>
    bool IsModal { get; set; }
}

/// <summary>
/// Interface for label components that display text.
/// </summary>
public interface ILabel : IUIComponent
{
    /// <summary>
    /// Gets or sets the label text content.
    /// </summary>
    string Text { get; set; }
    
    /// <summary>
    /// Gets or sets the text alignment.
    /// </summary>
    UITextAlignment TextAlignment { get; set; }
    
    /// <summary>
    /// Gets or sets whether text should wrap.
    /// </summary>
    bool WordWrap { get; set; }
}

/// <summary>
/// Interface for button components that can be clicked.
/// </summary>
public interface IButton : IUIComponent
{
    /// <summary>
    /// Gets or sets the button text content.
    /// </summary>
    string Text { get; set; }
    
    /// <summary>
    /// Gets whether the button is currently pressed.
    /// </summary>
    bool IsPressed { get; }
    
    /// <summary>
    /// Event raised when the button is pressed.
    /// </summary>
    event EventHandler? Pressed;
    
    /// <summary>
    /// Event raised when the button is released.
    /// </summary>
    event EventHandler? Released;
}

/// <summary>
/// Interface for text input components.
/// </summary>
public interface ITextInput : IUIComponent
{
    /// <summary>
    /// Gets or sets the current text value.
    /// </summary>
    string Text { get; set; }
    
    /// <summary>
    /// Gets or sets the placeholder text shown when empty.
    /// </summary>
    string Placeholder { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum length of input text.
    /// </summary>
    int MaxLength { get; set; }
    
    /// <summary>
    /// Gets or sets whether the input is read-only.
    /// </summary>
    bool IsReadOnly { get; set; }
    
    /// <summary>
    /// Gets or sets the current cursor position.
    /// </summary>
    int CursorPosition { get; set; }
    
    /// <summary>
    /// Event raised when the text value changes.
    /// </summary>
    event EventHandler<UITextChangedEvent>? TextChanged;
}