using System;

namespace GameConsole.UI.Core;

/// <summary>
/// UI event types for user interaction.
/// </summary>
public abstract class UIEvent
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    public bool Handled { get; set; } = false;
}

/// <summary>
/// Mouse-related UI event.
/// </summary>
public class MouseUIEvent : UIEvent
{
    public Position MousePosition { get; init; }
    public MouseButton Button { get; init; }
    
    public MouseUIEvent(Position mousePosition, MouseButton button = MouseButton.None)
    {
        MousePosition = mousePosition;
        Button = button;
    }
}

/// <summary>
/// Click event on UI component.
/// </summary>
public class ClickEvent : MouseUIEvent
{
    public string ComponentId { get; init; }
    
    public ClickEvent(string componentId, Position mousePosition, MouseButton button = MouseButton.Left)
        : base(mousePosition, button)
    {
        ComponentId = componentId;
    }
}

/// <summary>
/// Mouse hover event on UI component.
/// </summary>
public class HoverEvent : MouseUIEvent
{
    public string ComponentId { get; init; }
    public bool IsEntering { get; init; }
    
    public HoverEvent(string componentId, Position mousePosition, bool isEntering = true)
        : base(mousePosition)
    {
        ComponentId = componentId;
        IsEntering = isEntering;
    }
}

/// <summary>
/// Keyboard input event for text input.
/// </summary>
public class TextInputEvent : UIEvent
{
    public string ComponentId { get; init; }
    public char Character { get; init; }
    
    public TextInputEvent(string componentId, char character)
    {
        ComponentId = componentId;
        Character = character;
    }
}

/// <summary>
/// Key press event for UI navigation and shortcuts.
/// </summary>
public class KeyPressEvent : UIEvent
{
    public KeyCode Key { get; init; }
    public KeyModifiers Modifiers { get; init; }
    
    public KeyPressEvent(KeyCode key, KeyModifiers modifiers = KeyModifiers.None)
    {
        Key = key;
        Modifiers = modifiers;
    }
}

/// <summary>
/// Mouse button types.
/// </summary>
public enum MouseButton
{
    None,
    Left,
    Right,
    Middle
}

/// <summary>
/// Key codes for keyboard input.
/// </summary>
public enum KeyCode
{
    Unknown,
    Tab,
    Enter,
    Escape,
    Space,
    Backspace,
    Delete,
    ArrowUp,
    ArrowDown,
    ArrowLeft,
    ArrowRight,
    Home,
    End,
    PageUp,
    PageDown,
    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
    A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
    Num0, Num1, Num2, Num3, Num4, Num5, Num6, Num7, Num8, Num9
}

/// <summary>
/// Key modifier flags.
/// </summary>
[Flags]
public enum KeyModifiers
{
    None = 0,
    Shift = 1,
    Ctrl = 2,
    Alt = 4,
    Meta = 8
}