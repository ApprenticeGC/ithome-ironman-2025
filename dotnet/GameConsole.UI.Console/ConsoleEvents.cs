using GameConsole.Input.Core;

namespace GameConsole.UI.Console;

/// <summary>
/// Console input event for UI components.
/// </summary>
public readonly struct ConsoleInputEvent
{
    /// <summary>
    /// Type of input event.
    /// </summary>
    public ConsoleInputEventType Type { get; }

    /// <summary>
    /// Key code for keyboard events.
    /// </summary>
    public KeyCode? Key { get; }

    /// <summary>
    /// Character for text input events.
    /// </summary>
    public char? Character { get; }

    /// <summary>
    /// Mouse button for mouse events.
    /// </summary>
    public MouseButton? MouseButton { get; }

    /// <summary>
    /// Mouse position for mouse events.
    /// </summary>
    public ConsolePosition? MousePosition { get; }

    /// <summary>
    /// Modifier keys pressed during the event.
    /// </summary>
    public ConsoleInputModifiers Modifiers { get; }

    /// <summary>
    /// Initializes a keyboard input event.
    /// </summary>
    public ConsoleInputEvent(KeyCode key, ConsoleInputModifiers modifiers = ConsoleInputModifiers.None)
    {
        Type = ConsoleInputEventType.KeyPress;
        Key = key;
        Character = null;
        MouseButton = null;
        MousePosition = null;
        Modifiers = modifiers;
    }

    /// <summary>
    /// Initializes a text input event.
    /// </summary>
    public ConsoleInputEvent(char character, ConsoleInputModifiers modifiers = ConsoleInputModifiers.None)
    {
        Type = ConsoleInputEventType.TextInput;
        Key = null;
        Character = character;
        MouseButton = null;
        MousePosition = null;
        Modifiers = modifiers;
    }

    /// <summary>
    /// Initializes a mouse input event.
    /// </summary>
    public ConsoleInputEvent(MouseButton mouseButton, ConsolePosition mousePosition, ConsoleInputModifiers modifiers = ConsoleInputModifiers.None)
    {
        Type = ConsoleInputEventType.MouseClick;
        Key = null;
        Character = null;
        MouseButton = mouseButton;
        MousePosition = mousePosition;
        Modifiers = modifiers;
    }

    /// <summary>
    /// Initializes a mouse move event.
    /// </summary>
    public ConsoleInputEvent(ConsolePosition mousePosition)
    {
        Type = ConsoleInputEventType.MouseMove;
        Key = null;
        Character = null;
        MouseButton = null;
        MousePosition = mousePosition;
        Modifiers = ConsoleInputModifiers.None;
    }
}

/// <summary>
/// Console input event types.
/// </summary>
public enum ConsoleInputEventType
{
    KeyPress,
    TextInput,
    MouseClick,
    MouseMove,
    Focus,
    Blur
}

/// <summary>
/// Input modifier flags.
/// </summary>
[Flags]
public enum ConsoleInputModifiers
{
    None = 0,
    Shift = 1,
    Control = 2,
    Alt = 4
}

/// <summary>
/// Console menu item data.
/// </summary>
public readonly struct ConsoleMenuItem
{
    /// <summary>
    /// Menu item text.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Action to execute when selected.
    /// </summary>
    public Func<Task>? Action { get; }

    /// <summary>
    /// Whether the item is enabled.
    /// </summary>
    public bool IsEnabled { get; }

    /// <summary>
    /// Initializes a new menu item.
    /// </summary>
    public ConsoleMenuItem(string text, Func<Task>? action = null, bool isEnabled = true)
    {
        Text = text;
        Action = action;
        IsEnabled = isEnabled;
    }
}

/// <summary>
/// Event arguments for menu item selection.
/// </summary>
public class ConsoleMenuItemSelectedEventArgs : EventArgs
{
    /// <summary>
    /// Selected item index.
    /// </summary>
    public int ItemIndex { get; }

    /// <summary>
    /// Selected menu item.
    /// </summary>
    public ConsoleMenuItem Item { get; }

    /// <summary>
    /// Initializes event arguments for menu item selection.
    /// </summary>
    public ConsoleMenuItemSelectedEventArgs(int itemIndex, ConsoleMenuItem item)
    {
        ItemIndex = itemIndex;
        Item = item;
    }
}

/// <summary>
/// Event arguments for table row selection.
/// </summary>
public class ConsoleTableRowSelectedEventArgs : EventArgs
{
    /// <summary>
    /// Selected row index.
    /// </summary>
    public int RowIndex { get; }

    /// <summary>
    /// Selected row data.
    /// </summary>
    public IReadOnlyList<string> RowData { get; }

    /// <summary>
    /// Initializes event arguments for table row selection.
    /// </summary>
    public ConsoleTableRowSelectedEventArgs(int rowIndex, IReadOnlyList<string> rowData)
    {
        RowIndex = rowIndex;
        RowData = rowData;
    }
}

/// <summary>
/// Event arguments for progress change events.
/// </summary>
public class ConsoleProgressChangedEventArgs : EventArgs
{
    /// <summary>
    /// Old progress value.
    /// </summary>
    public int OldValue { get; }

    /// <summary>
    /// New progress value.
    /// </summary>
    public int NewValue { get; }

    /// <summary>
    /// Maximum progress value.
    /// </summary>
    public int MaxValue { get; }

    /// <summary>
    /// Progress percentage (0.0 to 1.0).
    /// </summary>
    public double PercentComplete => MaxValue > 0 ? (double)NewValue / MaxValue : 0.0;

    /// <summary>
    /// Initializes event arguments for progress change.
    /// </summary>
    public ConsoleProgressChangedEventArgs(int oldValue, int newValue, int maxValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
        MaxValue = maxValue;
    }
}