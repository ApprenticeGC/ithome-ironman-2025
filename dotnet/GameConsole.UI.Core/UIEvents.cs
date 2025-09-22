namespace GameConsole.UI.Core;

/// <summary>
/// Base class for all UI events.
/// </summary>
public abstract class UIEvent
{
    public DateTime Timestamp { get; }
    public string EventType { get; }
    public IUIComponent? Target { get; }
    public bool Handled { get; set; }
    
    protected UIEvent(string eventType, IUIComponent? target = null)
    {
        EventType = eventType;
        Target = target;
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// Event fired when a key is pressed while a component has focus.
/// </summary>
public class KeyEvent : UIEvent
{
    public ConsoleKey Key { get; }
    public ConsoleModifiers Modifiers { get; }
    public char KeyChar { get; }
    
    public KeyEvent(ConsoleKey key, ConsoleModifiers modifiers, char keyChar, IUIComponent? target = null)
        : base("Key", target)
    {
        Key = key;
        Modifiers = modifiers;
        KeyChar = keyChar;
    }
}

/// <summary>
/// Event fired when a component is clicked (via Enter key or mouse in future).
/// </summary>
public class ClickEvent : UIEvent
{
    public Point Position { get; }
    
    public ClickEvent(Point position, IUIComponent? target = null)
        : base("Click", target)
    {
        Position = position;
    }
}

/// <summary>
/// Event fired when a component gains or loses focus.
/// </summary>
public class FocusEvent : UIEvent
{
    public bool GainedFocus { get; }
    
    public FocusEvent(bool gainedFocus, IUIComponent? target = null)
        : base(gainedFocus ? "FocusGained" : "FocusLost", target)
    {
        GainedFocus = gainedFocus;
    }
}

/// <summary>
/// Event fired when text is entered in a text input component.
/// </summary>
public class TextInputEvent : UIEvent
{
    public string Text { get; }
    
    public TextInputEvent(string text, IUIComponent? target = null)
        : base("TextInput", target)
    {
        Text = text;
    }
}

/// <summary>
/// Event fired when a selection changes in a list or menu.
/// </summary>
public class SelectionEvent : UIEvent
{
    public int SelectedIndex { get; }
    public object? SelectedItem { get; }
    
    public SelectionEvent(int selectedIndex, object? selectedItem, IUIComponent? target = null)
        : base("Selection", target)
    {
        SelectedIndex = selectedIndex;
        SelectedItem = selectedItem;
    }
}