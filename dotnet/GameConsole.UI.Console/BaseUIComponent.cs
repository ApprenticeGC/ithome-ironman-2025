namespace GameConsole.UI.Console;

/// <summary>
/// Base implementation for console UI components.
/// </summary>
public abstract class BaseUIComponent : IUIComponent
{

    private static readonly object _focusLock = new();
    private static IUIComponent? _focusedComponent;
    
    protected BaseUIComponent(string id)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        DesiredBounds = new Rectangle(0, 0, -1, -1); // Auto-size by default
    }
    
    public string Id { get; }
    public Rectangle DesiredBounds { get; set; }
    public Rectangle ActualBounds { get; private set; }
    public bool IsVisible { get; set; } = true;
    public bool CanFocus { get; set; } = false;
    
    public bool HasFocus 
    { 
        get 
        {
            lock (_focusLock)
            {
                return _focusedComponent == this;
            }
        }
    }
    
    public void SetBounds(Rectangle bounds)
    {
        ActualBounds = bounds;
        OnBoundsChanged(bounds);
    }
    
    public abstract void Render(IConsoleUIFramework framework);
    
    public virtual bool HandleInput(UIKeyEventArgs input)
    {
        if (!CanFocus || !HasFocus || !IsVisible)
            return false;
            
        return OnHandleInput(input);
    }
    
    /// <summary>
    /// Attempts to give focus to this component.
    /// </summary>
    /// <returns>True if focus was successfully acquired; false otherwise.</returns>
    public bool Focus()
    {
        if (!CanFocus || !IsVisible)
            return false;
            
        lock (_focusLock)
        {
            var previousFocus = _focusedComponent as BaseUIComponent;
            _focusedComponent = this;
            
            previousFocus?.OnLostFocus();
            OnGainedFocus();
            
            return true;
        }
    }
    
    /// <summary>
    /// Removes focus from this component.
    /// </summary>
    public void Blur()
    {
        lock (_focusLock)
        {
            if (_focusedComponent == this)
            {
                _focusedComponent = null;
                OnLostFocus();
            }
        }
    }
    
    /// <summary>
    /// Called when the component's bounds are changed.
    /// </summary>
    /// <param name="bounds">New bounds.</param>
    protected virtual void OnBoundsChanged(Rectangle bounds) { }
    
    /// <summary>
    /// Called when the component should handle input.
    /// </summary>
    /// <param name="input">Input event to handle.</param>
    /// <returns>True if input was handled; false otherwise.</returns>
    protected virtual bool OnHandleInput(UIKeyEventArgs input) => false;
    
    /// <summary>
    /// Called when the component gains focus.
    /// </summary>
    protected virtual void OnGainedFocus() { }
    
    /// <summary>
    /// Called when the component loses focus.
    /// </summary>
    protected virtual void OnLostFocus() { }
    
    /// <summary>
    /// Helper method to clear the component's area before rendering.
    /// </summary>
    /// <param name="framework">UI framework for clearing.</param>
    protected void ClearArea(IConsoleUIFramework framework)
    {
        if (ActualBounds.IsEmpty) return;
        framework.ClearArea(ActualBounds.X, ActualBounds.Y, ActualBounds.Width, ActualBounds.Height);
    }
    
    /// <summary>
    /// Helper method to write text within the component bounds with word wrapping.
    /// </summary>
    /// <param name="framework">UI framework for writing.</param>
    /// <param name="text">Text to write.</param>
    /// <param name="x">Relative X position within component.</param>
    /// <param name="y">Relative Y position within component.</param>
    /// <param name="maxWidth">Maximum width for text wrapping.</param>
    /// <param name="foreground">Foreground color.</param>
    /// <param name="background">Background color.</param>
    /// <param name="style">Text style.</param>
    /// <returns>Number of lines written.</returns>
    protected int WriteTextWrapped(IConsoleUIFramework framework, string text, int x, int y, int maxWidth, 
        ConsoleColor? foreground = null, ConsoleColor? background = null, TextStyle style = TextStyle.None)
    {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0) return 0;
        
        var lines = WrapText(text, maxWidth);
        int linesWritten = 0;
        
        foreach (var line in lines)
        {
            if (y + linesWritten >= ActualBounds.Height) break;
            
            framework.WriteAt(
                ActualBounds.X + x,
                ActualBounds.Y + y + linesWritten,
                line,
                foreground,
                background,
                style
            );
            linesWritten++;
        }
        
        return linesWritten;
    }
    
    private static IEnumerable<string> WrapText(string text, int maxWidth)
    {
        if (text.Length <= maxWidth)
        {
            yield return text;
            yield break;
        }
        
        int index = 0;
        while (index < text.Length)
        {
            int length = Math.Min(maxWidth, text.Length - index);
            
            // Try to break at a space if possible
            if (index + length < text.Length)
            {
                int lastSpace = text.LastIndexOf(' ', index + length - 1, length);
                if (lastSpace > index)
                {
                    length = lastSpace - index;
                }
            }
            
            yield return text.Substring(index, length);
            index += length;
            
            // Skip the space we broke on
            if (index < text.Length && text[index] == ' ')
                index++;
        }
    }
}