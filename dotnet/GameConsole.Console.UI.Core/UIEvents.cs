using System;
using System.Collections.Generic;
using System.Linq;

namespace GameConsole.Console.UI.Core;

/// <summary>
/// Represents input events for UI elements.
/// </summary>
public class UIInputEvent
{
    /// <summary>
    /// The type of input event.
    /// </summary>
    public UIInputEventType Type { get; }
    
    /// <summary>
    /// The timestamp when the event occurred.
    /// </summary>
    public DateTime Timestamp { get; }
    
    /// <summary>
    /// Whether this event has been handled.
    /// </summary>
    public bool Handled { get; set; }
    
    /// <summary>
    /// Initializes a new instance of the UIInputEvent class.
    /// </summary>
    /// <param name="type">The type of input event.</param>
    /// <param name="timestamp">When the event occurred.</param>
    public UIInputEvent(UIInputEventType type, DateTime timestamp)
    {
        Type = type;
        Timestamp = timestamp;
    }
}

/// <summary>
/// Keyboard input event for UI elements.
/// </summary>
public class UIKeyboardEvent : UIInputEvent
{
    /// <summary>
    /// The key that was pressed or released.
    /// </summary>
    public char Key { get; }
    
    /// <summary>
    /// Whether the key was pressed (true) or released (false).
    /// </summary>
    public bool IsPressed { get; }
    
    /// <summary>
    /// Modifier keys that were active.
    /// </summary>
    public UIModifierKeys Modifiers { get; }
    
    /// <summary>
    /// Initializes a new instance of the UIKeyboardEvent class.
    /// </summary>
    /// <param name="key">The key that was pressed or released.</param>
    /// <param name="isPressed">Whether the key was pressed.</param>
    /// <param name="modifiers">Active modifier keys.</param>
    /// <param name="timestamp">When the event occurred.</param>
    public UIKeyboardEvent(char key, bool isPressed, UIModifierKeys modifiers, DateTime timestamp) 
        : base(UIInputEventType.Keyboard, timestamp)
    {
        Key = key;
        IsPressed = isPressed;
        Modifiers = modifiers;
    }
}

/// <summary>
/// Mouse input event for UI elements.
/// </summary>
public class UIMouseEvent : UIInputEvent
{
    /// <summary>
    /// The position of the mouse cursor.
    /// </summary>
    public ConsolePosition Position { get; }
    
    /// <summary>
    /// The mouse button involved in this event, if any.
    /// </summary>
    public UIMouseButton? Button { get; }
    
    /// <summary>
    /// Whether the button was pressed (true) or released (false).
    /// </summary>
    public bool IsPressed { get; }
    
    /// <summary>
    /// Initializes a new instance of the UIMouseEvent class.
    /// </summary>
    /// <param name="position">The mouse cursor position.</param>
    /// <param name="button">The mouse button, if any.</param>
    /// <param name="isPressed">Whether the button was pressed.</param>
    /// <param name="timestamp">When the event occurred.</param>
    public UIMouseEvent(ConsolePosition position, UIMouseButton? button, bool isPressed, DateTime timestamp) 
        : base(UIInputEventType.Mouse, timestamp)
    {
        Position = position;
        Button = button;
        IsPressed = isPressed;
    }
}

/// <summary>
/// Event arguments for UI click events.
/// </summary>
public class UIClickEventArgs : EventArgs
{
    /// <summary>
    /// The position where the click occurred.
    /// </summary>
    public ConsolePosition Position { get; }
    
    /// <summary>
    /// The mouse button that was clicked.
    /// </summary>
    public UIMouseButton Button { get; }
    
    /// <summary>
    /// Initializes a new instance of the UIClickEventArgs class.
    /// </summary>
    /// <param name="position">The click position.</param>
    /// <param name="button">The mouse button clicked.</param>
    public UIClickEventArgs(ConsolePosition position, UIMouseButton button)
    {
        Position = position;
        Button = button;
    }
}

/// <summary>
/// Types of UI input events.
/// </summary>
public enum UIInputEventType
{
    Keyboard,
    Mouse,
    Gamepad
}

/// <summary>
/// Mouse buttons for UI events.
/// </summary>
public enum UIMouseButton
{
    Left,
    Right,
    Middle
}

/// <summary>
/// Modifier keys for keyboard events.
/// </summary>
[Flags]
public enum UIModifierKeys
{
    None = 0,
    Shift = 1,
    Control = 2,
    Alt = 4
}