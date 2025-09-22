namespace GameConsole.UI.Core;

/// <summary>
/// Interface for button components.
/// </summary>
public interface IButton : IUIComponent
{
    /// <summary>
    /// Text displayed on the button.
    /// </summary>
    string Text { get; set; }
    
    /// <summary>
    /// Whether the button is currently pressed.
    /// </summary>
    bool IsPressed { get; }
    
    /// <summary>
    /// Button foreground color.
    /// </summary>
    ConsoleColor? ForegroundColor { get; set; }
    
    /// <summary>
    /// Button background color.
    /// </summary>
    ConsoleColor? BackgroundColor { get; set; }
    
    /// <summary>
    /// Observable that fires when the button is clicked.
    /// </summary>
    IObservable<IButton> Clicked { get; }
}

/// <summary>
/// Interface for text input components.
/// </summary>
public interface ITextBox : IUIComponent
{
    /// <summary>
    /// Current text content.
    /// </summary>
    string Text { get; set; }
    
    /// <summary>
    /// Placeholder text shown when empty.
    /// </summary>
    string Placeholder { get; set; }
    
    /// <summary>
    /// Maximum number of characters allowed.
    /// </summary>
    int MaxLength { get; set; }
    
    /// <summary>
    /// Whether the text box is read-only.
    /// </summary>
    bool ReadOnly { get; set; }
    
    /// <summary>
    /// Current cursor position within the text.
    /// </summary>
    int CursorPosition { get; set; }
    
    /// <summary>
    /// Observable that fires when the text changes.
    /// </summary>
    IObservable<string> TextChanged { get; }
}

/// <summary>
/// Interface for label/text display components.
/// </summary>
public interface ILabel : IUIComponent
{
    /// <summary>
    /// Text content to display.
    /// </summary>
    string Text { get; set; }
    
    /// <summary>
    /// Text foreground color.
    /// </summary>
    ConsoleColor? ForegroundColor { get; set; }
    
    /// <summary>
    /// Text background color.
    /// </summary>
    ConsoleColor? BackgroundColor { get; set; }
    
    /// <summary>
    /// Text alignment within the label bounds.
    /// </summary>
    HorizontalAlignment TextAlignment { get; set; }
    
    /// <summary>
    /// Whether text should wrap to multiple lines.
    /// </summary>
    bool WordWrap { get; set; }
}

/// <summary>
/// Interface for list/menu components.
/// </summary>
public interface IListBox : IUIComponent
{
    /// <summary>
    /// Items in the list.
    /// </summary>
    IReadOnlyList<object> Items { get; }
    
    /// <summary>
    /// Currently selected item index (-1 if none selected).
    /// </summary>
    int SelectedIndex { get; set; }
    
    /// <summary>
    /// Currently selected item (null if none selected).
    /// </summary>
    object? SelectedItem { get; }
    
    /// <summary>
    /// Add an item to the list.
    /// </summary>
    Task AddItemAsync(object item, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Remove an item from the list.
    /// </summary>
    Task RemoveItemAsync(object item, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Clear all items from the list.
    /// </summary>
    Task ClearAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Observable that fires when selection changes.
    /// </summary>
    IObservable<object?> SelectionChanged { get; }
}

/// <summary>
/// Interface for checkbox components.
/// </summary>
public interface ICheckBox : IUIComponent
{
    /// <summary>
    /// Text label for the checkbox.
    /// </summary>
    string Text { get; set; }
    
    /// <summary>
    /// Whether the checkbox is checked.
    /// </summary>
    bool IsChecked { get; set; }
    
    /// <summary>
    /// Observable that fires when the checked state changes.
    /// </summary>
    IObservable<bool> CheckedChanged { get; }
}

/// <summary>
/// Interface for panel/container components.
/// </summary>
public interface IPanel : IUIComponent
{
    /// <summary>
    /// Background color for the panel.
    /// </summary>
    ConsoleColor? BackgroundColor { get; set; }
    
    /// <summary>
    /// Border style for the panel.
    /// </summary>
    BorderStyle BorderStyle { get; set; }
    
    /// <summary>
    /// Border color for the panel.
    /// </summary>
    ConsoleColor? BorderColor { get; set; }
    
    /// <summary>
    /// Layout type for arranging child components.
    /// </summary>
    LayoutType Layout { get; set; }
    
    /// <summary>
    /// Add a child component to this panel.
    /// </summary>
    Task AddChildAsync(IUIComponent child, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Remove a child component from this panel.
    /// </summary>
    Task RemoveChildAsync(IUIComponent child, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all child components.
    /// </summary>
    IReadOnlyList<IUIComponent> GetChildren();
}