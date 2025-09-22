namespace GameConsole.UI.Core;

/// <summary>
/// Base class for all UI elements.
/// </summary>
public abstract class UIElement
{
    /// <summary>
    /// Unique identifier for the UI element.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display text for the element.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Position of the element.
    /// </summary>
    public UIPosition Position { get; set; }

    /// <summary>
    /// Size of the element.
    /// </summary>
    public UISize Size { get; set; }

    /// <summary>
    /// Current state of the element.
    /// </summary>
    public UIState State { get; set; } = UIState.Normal;

    /// <summary>
    /// Whether the element is visible.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Whether the element can receive focus.
    /// </summary>
    public bool CanFocus { get; set; } = true;

    /// <summary>
    /// Parent element (if any).
    /// </summary>
    public string? ParentId { get; set; }

    /// <summary>
    /// Z-order for layering elements.
    /// </summary>
    public int ZOrder { get; set; }
}

/// <summary>
/// Represents a clickable button element.
/// </summary>
public class UIButton : UIElement
{
    /// <summary>
    /// Action to execute when the button is clicked.
    /// </summary>
    public string? OnClickAction { get; set; }

    /// <summary>
    /// Keyboard shortcut for the button.
    /// </summary>
    public string? Shortcut { get; set; }
}

/// <summary>
/// Represents a text input field.
/// </summary>
public class UITextBox : UIElement
{
    /// <summary>
    /// Placeholder text shown when empty.
    /// </summary>
    public string PlaceholderText { get; set; } = string.Empty;

    /// <summary>
    /// Current cursor position within the text.
    /// </summary>
    public int CursorPosition { get; set; }

    /// <summary>
    /// Whether the text box is read-only.
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Maximum length of text allowed.
    /// </summary>
    public int MaxLength { get; set; } = int.MaxValue;

    /// <summary>
    /// Whether to mask the input (for passwords).
    /// </summary>
    public bool IsMasked { get; set; }

    /// <summary>
    /// Character to use for masking.
    /// </summary>
    public char MaskCharacter { get; set; } = '*';
}

/// <summary>
/// Represents a static text label.
/// </summary>
public class UILabel : UIElement
{
    /// <summary>
    /// Text alignment for the label.
    /// </summary>
    public UIAlignment Alignment { get; set; } = UIAlignment.Left;

    /// <summary>
    /// Whether text should wrap to fit within bounds.
    /// </summary>
    public bool WordWrap { get; set; }
}

/// <summary>
/// Represents a container panel for other elements.
/// </summary>
public class UIPanel : UIElement
{
    /// <summary>
    /// Child elements contained within this panel.
    /// </summary>
    public List<string> ChildElementIds { get; set; } = new();

    /// <summary>
    /// Whether the panel has a border.
    /// </summary>
    public bool HasBorder { get; set; } = true;

    /// <summary>
    /// Title displayed in the panel border (if any).
    /// </summary>
    public string? Title { get; set; }
}

/// <summary>
/// Represents a menu with selectable items.
/// </summary>
public class UIMenu : UIElement
{
    /// <summary>
    /// Menu items.
    /// </summary>
    public List<UIMenuItem> Items { get; set; } = new();

    /// <summary>
    /// Currently selected item index.
    /// </summary>
    public int SelectedIndex { get; set; }

    /// <summary>
    /// Whether the menu supports multi-selection.
    /// </summary>
    public bool AllowMultiSelect { get; set; }
}

/// <summary>
/// Represents an item within a menu.
/// </summary>
public class UIMenuItem
{
    /// <summary>
    /// Unique identifier for the menu item.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display text for the item.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Action to execute when selected.
    /// </summary>
    public string? OnSelectAction { get; set; }

    /// <summary>
    /// Whether the item is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Whether the item is currently selected.
    /// </summary>
    public bool IsSelected { get; set; }

    /// <summary>
    /// Keyboard shortcut for the item.
    /// </summary>
    public string? Shortcut { get; set; }
}

/// <summary>
/// Represents a progress bar element.
/// </summary>
public class UIProgressBar : UIElement
{
    /// <summary>
    /// Current progress value (0.0 to 1.0).
    /// </summary>
    public float Value { get; set; }

    /// <summary>
    /// Minimum value.
    /// </summary>
    public float Minimum { get; set; } = 0.0f;

    /// <summary>
    /// Maximum value.
    /// </summary>
    public float Maximum { get; set; } = 1.0f;

    /// <summary>
    /// Whether to show percentage text.
    /// </summary>
    public bool ShowPercentage { get; set; } = true;

    /// <summary>
    /// Custom text to display on the progress bar.
    /// </summary>
    public string? CustomText { get; set; }
}