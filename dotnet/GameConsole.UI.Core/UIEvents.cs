namespace GameConsole.UI.Core;

/// <summary>
/// Base class for all UI events in the GameConsole system.
/// </summary>
public abstract class UIEvent
{
    /// <summary>
    /// Timestamp when the event occurred.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Frame number when the event occurred.
    /// </summary>
    public long FrameNumber { get; set; }

    /// <summary>
    /// Source element that generated the event (if applicable).
    /// </summary>
    public string? SourceElementId { get; set; }
}

/// <summary>
/// Event raised when a UI element receives focus.
/// </summary>
public class UIFocusEvent : UIEvent
{
    /// <summary>
    /// The element that gained focus.
    /// </summary>
    public string ElementId { get; set; } = string.Empty;

    /// <summary>
    /// The previous focused element (if any).
    /// </summary>
    public string? PreviousElementId { get; set; }
}

/// <summary>
/// Event raised when a UI element is clicked or activated.
/// </summary>
public class UIActivationEvent : UIEvent
{
    /// <summary>
    /// The element that was activated.
    /// </summary>
    public string ElementId { get; set; } = string.Empty;

    /// <summary>
    /// The activation method (mouse click, keyboard, etc.).
    /// </summary>
    public UIActivationMethod Method { get; set; }

    /// <summary>
    /// Position where the activation occurred (for mouse clicks).
    /// </summary>
    public UIPosition? Position { get; set; }
}

/// <summary>
/// Event raised when UI layout changes.
/// </summary>
public class UILayoutEvent : UIEvent
{
    /// <summary>
    /// The element whose layout changed.
    /// </summary>
    public string ElementId { get; set; } = string.Empty;

    /// <summary>
    /// The new bounds of the element.
    /// </summary>
    public UIRect NewBounds { get; set; }

    /// <summary>
    /// The previous bounds of the element.
    /// </summary>
    public UIRect PreviousBounds { get; set; }
}

/// <summary>
/// Event raised when UI text input occurs.
/// </summary>
public class UITextInputEvent : UIEvent
{
    /// <summary>
    /// The text that was input.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// The element that received the text input.
    /// </summary>
    public string ElementId { get; set; } = string.Empty;

    /// <summary>
    /// Current cursor position within the text.
    /// </summary>
    public int CursorPosition { get; set; }
}

/// <summary>
/// Methods by which a UI element can be activated.
/// </summary>
public enum UIActivationMethod
{
    Mouse,
    Keyboard,
    Touch,
    Gamepad
}

/// <summary>
/// Represents state change information for UI elements.
/// </summary>
public class UIStateChangeEvent : UIEvent
{
    /// <summary>
    /// The element whose state changed.
    /// </summary>
    public string ElementId { get; set; } = string.Empty;

    /// <summary>
    /// The new state of the element.
    /// </summary>
    public UIState NewState { get; set; }

    /// <summary>
    /// The previous state of the element.
    /// </summary>
    public UIState PreviousState { get; set; }
}