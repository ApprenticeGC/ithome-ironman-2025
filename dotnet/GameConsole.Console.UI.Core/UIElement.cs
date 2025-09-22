using System;

namespace GameConsole.Console.UI.Core;

/// <summary>
/// Base abstract class for all UI elements.
/// </summary>
public abstract class UIElement : IUIElement
{
    private bool _isVisible = true;
    private bool _isEnabled = true;
    private bool _canFocus = true;
    private bool _hasFocus = false;
    
    /// <summary>
    /// Gets the unique identifier for this UI element.
    /// </summary>
    public string Id { get; }
    
    /// <summary>
    /// Gets or sets the position of this element.
    /// </summary>
    public ConsolePosition Position { get; set; }
    
    /// <summary>
    /// Gets or sets the size of this element.
    /// </summary>
    public ConsoleSize Size { get; set; }
    
    /// <summary>
    /// Gets the bounds of this element.
    /// </summary>
    public ConsoleBounds Bounds => new(Position, Size);
    
    /// <summary>
    /// Gets or sets whether this element is visible.
    /// </summary>
    public bool IsVisible 
    { 
        get => _isVisible;
        set => _isVisible = value;
    }
    
    /// <summary>
    /// Gets or sets whether this element is enabled for interaction.
    /// </summary>
    public bool IsEnabled 
    { 
        get => _isEnabled;
        set => _isEnabled = value;
    }
    
    /// <summary>
    /// Gets or sets whether this element can receive focus.
    /// </summary>
    public bool CanFocus 
    { 
        get => _canFocus;
        set => _canFocus = value;
    }
    
    /// <summary>
    /// Gets whether this element currently has focus.
    /// </summary>
    public bool HasFocus => _hasFocus;
    
    /// <summary>
    /// Gets the parent container of this element, if any.
    /// </summary>
    public IUIContainer? Parent { get; internal set; }
    
    /// <summary>
    /// Event raised when this element gains focus.
    /// </summary>
    public event EventHandler? GotFocus;
    
    /// <summary>
    /// Event raised when this element loses focus.
    /// </summary>
    public event EventHandler? LostFocus;
    
    /// <summary>
    /// Event raised when this element is clicked.
    /// </summary>
    public event EventHandler<UIClickEventArgs>? Click;
    
    /// <summary>
    /// Initializes a new instance of the UIElement class.
    /// </summary>
    /// <param name="id">The unique identifier for this element.</param>
    /// <param name="position">The initial position.</param>
    /// <param name="size">The initial size.</param>
    protected UIElement(string id, ConsolePosition position, ConsoleSize size)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Position = position;
        Size = size;
    }
    
    /// <summary>
    /// Renders this element to the provided console buffer.
    /// </summary>
    /// <param name="buffer">The console buffer to render to.</param>
    public virtual void Render(IConsoleBuffer buffer)
    {
        if (!IsVisible)
            return;
            
        OnRender(buffer);
    }
    
    /// <summary>
    /// Handles input events for this element.
    /// </summary>
    /// <param name="inputEvent">The input event to handle.</param>
    /// <returns>True if the event was handled, false otherwise.</returns>
    public virtual bool HandleInput(UIInputEvent inputEvent)
    {
        if (!IsEnabled || !IsVisible)
            return false;
            
        return OnHandleInput(inputEvent);
    }
    
    /// <summary>
    /// Sets focus on this element.
    /// </summary>
    /// <returns>True if focus was successfully set, false otherwise.</returns>
    public virtual bool Focus()
    {
        if (!CanFocus || !IsEnabled || !IsVisible || _hasFocus)
            return false;
            
        _hasFocus = true;
        OnGotFocus();
        GotFocus?.Invoke(this, EventArgs.Empty);
        return true;
    }
    
    /// <summary>
    /// Removes focus from this element.
    /// </summary>
    public virtual void Blur()
    {
        if (!_hasFocus)
            return;
            
        _hasFocus = false;
        OnLostFocus();
        LostFocus?.Invoke(this, EventArgs.Empty);
    }
    
    /// <summary>
    /// Called when this element should render itself.
    /// </summary>
    /// <param name="buffer">The console buffer to render to.</param>
    protected abstract void OnRender(IConsoleBuffer buffer);
    
    /// <summary>
    /// Called when this element should handle an input event.
    /// </summary>
    /// <param name="inputEvent">The input event to handle.</param>
    /// <returns>True if the event was handled, false otherwise.</returns>
    protected virtual bool OnHandleInput(UIInputEvent inputEvent)
    {
        // Handle mouse clicks
        if (inputEvent is UIMouseEvent mouseEvent && 
            mouseEvent.Button == UIMouseButton.Left && 
            mouseEvent.IsPressed &&
            Bounds.Contains(mouseEvent.Position))
        {
            OnClick(mouseEvent.Position, UIMouseButton.Left);
            Click?.Invoke(this, new UIClickEventArgs(mouseEvent.Position, UIMouseButton.Left));
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Called when this element gains focus.
    /// </summary>
    protected virtual void OnGotFocus() { }
    
    /// <summary>
    /// Called when this element loses focus.
    /// </summary>
    protected virtual void OnLostFocus() { }
    
    /// <summary>
    /// Called when this element is clicked.
    /// </summary>
    /// <param name="position">The click position.</param>
    /// <param name="button">The mouse button clicked.</param>
    protected virtual void OnClick(ConsolePosition position, UIMouseButton button) { }
}