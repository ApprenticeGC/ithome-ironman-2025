namespace GameConsole.UI.Core;

/// <summary>
/// Base class for all UI events.
/// </summary>
public abstract class UIEvent
{
    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets the source component that generated the event.
    /// </summary>
    public IUIComponent? Source { get; set; }
    
    /// <summary>
    /// Gets or sets whether the event has been handled.
    /// </summary>
    public bool Handled { get; set; }
}

/// <summary>
/// Event raised when a UI component is clicked.
/// </summary>
public class UIClickEvent : UIEvent
{
    /// <summary>
    /// Gets the position where the click occurred.
    /// </summary>
    public UIPoint Position { get; set; }
    
    /// <summary>
    /// Gets which mouse button was clicked.
    /// </summary>
    public UIMouseButton Button { get; set; } = UIMouseButton.Left;
    
    /// <summary>
    /// Gets the number of clicks (1 for single, 2 for double, etc.).
    /// </summary>
    public int ClickCount { get; set; } = 1;
}

/// <summary>
/// Event raised when focus changes between UI components.
/// </summary>
public class UIFocusEvent : UIEvent
{
    /// <summary>
    /// Gets the component that lost focus (null if none).
    /// </summary>
    public IUIComponent? PreviousFocused { get; set; }
    
    /// <summary>
    /// Gets the component that gained focus (null if none).
    /// </summary>
    public IUIComponent? NewFocused { get; set; }
    
    /// <summary>
    /// Gets whether focus is being gained or lost.
    /// </summary>
    public bool IsFocusGained { get; set; }
}

/// <summary>
/// Event raised when a component's value changes.
/// </summary>
public class UIValueChangedEvent : UIEvent
{
    /// <summary>
    /// Gets the previous value.
    /// </summary>
    public object? OldValue { get; set; }
    
    /// <summary>
    /// Gets the new value.
    /// </summary>
    public object? NewValue { get; set; }
}

/// <summary>
/// Event raised when text changes in a text input component.
/// </summary>
public class UITextChangedEvent : UIValueChangedEvent
{
    /// <summary>
    /// Gets the previous text value.
    /// </summary>
    public new string? OldValue => base.OldValue as string;
    
    /// <summary>
    /// Gets the new text value.
    /// </summary>
    public new string? NewValue => base.NewValue as string;
    
    /// <summary>
    /// Gets the type of text change that occurred.
    /// </summary>
    public UITextChangeType ChangeType { get; set; }
    
    /// <summary>
    /// Gets the position where the change occurred.
    /// </summary>
    public int ChangePosition { get; set; }
    
    /// <summary>
    /// Gets the length of text that was changed.
    /// </summary>
    public int ChangeLength { get; set; }
}

/// <summary>
/// Event raised for keyboard input on UI components.
/// </summary>
public class UIKeyEvent : UIEvent
{
    /// <summary>
    /// Gets the key that was pressed or released.
    /// </summary>
    public UIKey Key { get; set; }
    
    /// <summary>
    /// Gets the type of key event.
    /// </summary>
    public UIKeyEventType EventType { get; set; }
    
    /// <summary>
    /// Gets modifier keys that were held during the event.
    /// </summary>
    public UIKeyModifiers Modifiers { get; set; }
    
    /// <summary>
    /// Gets the character representation of the key, if applicable.
    /// </summary>
    public char? Character { get; set; }
}

/// <summary>
/// Mouse button enumeration for UI events.
/// </summary>
public enum UIMouseButton
{
    Left,
    Right,
    Middle,
    X1,
    X2
}

/// <summary>
/// Type of text change in text input components.
/// </summary>
public enum UITextChangeType
{
    Insert,
    Delete,
    Replace,
    Clear
}

/// <summary>
/// Key event types.
/// </summary>
public enum UIKeyEventType
{
    KeyDown,
    KeyUp,
    KeyPress
}

/// <summary>
/// Key codes for UI input handling.
/// </summary>
public enum UIKey
{
    None = 0,
    Enter,
    Escape,
    Space,
    Backspace,
    Delete,
    Tab,
    Home,
    End,
    PageUp,
    PageDown,
    ArrowLeft,
    ArrowRight,
    ArrowUp,
    ArrowDown,
    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
    A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
    Digit0, Digit1, Digit2, Digit3, Digit4, Digit5, Digit6, Digit7, Digit8, Digit9
}

/// <summary>
/// Key modifier flags.
/// </summary>
[Flags]
public enum UIKeyModifiers
{
    None = 0,
    Shift = 1,
    Control = 2,
    Alt = 4,
    Meta = 8
}