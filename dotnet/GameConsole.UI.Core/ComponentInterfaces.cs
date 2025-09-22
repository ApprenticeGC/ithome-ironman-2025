namespace GameConsole.UI.Core;

/// <summary>
/// Interface for button components that can be clicked or activated.
/// </summary>
public interface IUIButton : IUIComponent
{
    /// <summary>
    /// Text displayed on the button.
    /// </summary>
    string Text { get; set; }
    
    /// <summary>
    /// Command to execute when the button is clicked.
    /// </summary>
    string? Command { get; set; }
    
    /// <summary>
    /// Parameters to pass with the command.
    /// </summary>
    Dictionary<string, object>? CommandParameters { get; set; }
    
    /// <summary>
    /// Event fired when the button is clicked.
    /// </summary>
    event Func<UIInteractionEvent, Task>? Clicked;
}

/// <summary>
/// Interface for text input components.
/// </summary>
public interface IUITextInput : IUIComponent
{
    /// <summary>
    /// Current value of the text input.
    /// </summary>
    string Value { get; set; }
    
    /// <summary>
    /// Placeholder text shown when the input is empty.
    /// </summary>
    string? Placeholder { get; set; }
    
    /// <summary>
    /// Maximum length of input allowed.
    /// </summary>
    int? MaxLength { get; set; }
    
    /// <summary>
    /// Whether the input is read-only.
    /// </summary>
    bool IsReadOnly { get; set; }
    
    /// <summary>
    /// Input type (text, password, email, etc.)
    /// </summary>
    string InputType { get; set; }
    
    /// <summary>
    /// Event fired when the text value changes.
    /// </summary>
    event Func<UIDataBindingEvent, Task>? ValueChanged;
}

/// <summary>
/// Interface for label components that display text.
/// </summary>
public interface IUILabel : IUIComponent
{
    /// <summary>
    /// Text content of the label.
    /// </summary>
    string Text { get; set; }
    
    /// <summary>
    /// Associated component ID for accessibility.
    /// </summary>
    string? ForComponent { get; set; }
    
    /// <summary>
    /// Text alignment within the label.
    /// </summary>
    string TextAlign { get; set; }
}

/// <summary>
/// Interface for panel components that act as containers for other components.
/// </summary>
public interface IUIPanel : IUIComponent
{
    /// <summary>
    /// Title of the panel, if displayed.
    /// </summary>
    string? Title { get; set; }
    
    /// <summary>
    /// Layout type for arranging child components.
    /// </summary>
    string Layout { get; set; }
    
    /// <summary>
    /// Whether the panel has a visible border.
    /// </summary>
    bool HasBorder { get; set; }
    
    /// <summary>
    /// Whether the panel can be collapsed.
    /// </summary>
    bool IsCollapsible { get; set; }
    
    /// <summary>
    /// Whether the panel is currently collapsed.
    /// </summary>
    bool IsCollapsed { get; set; }
}

/// <summary>
/// Interface for menu components with selectable options.
/// </summary>
public interface IUIMenu : IUIComponent
{
    /// <summary>
    /// Menu items available for selection.
    /// </summary>
    IReadOnlyList<string> Items { get; }
    
    /// <summary>
    /// Currently selected item index.
    /// </summary>
    int? SelectedIndex { get; set; }
    
    /// <summary>
    /// Currently selected item value.
    /// </summary>
    string? SelectedItem { get; set; }
    
    /// <summary>
    /// Whether multiple selections are allowed.
    /// </summary>
    bool AllowMultipleSelection { get; set; }
    
    /// <summary>
    /// Adds a menu item.
    /// </summary>
    Task AddItemAsync(string item, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes a menu item.
    /// </summary>
    Task RemoveItemAsync(string item, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Clears all menu items.
    /// </summary>
    Task ClearItemsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Event fired when menu selection changes.
    /// </summary>
    event Func<UIInteractionEvent, Task>? SelectionChanged;
}

/// <summary>
/// Interface for list components that display collections of items.
/// </summary>
public interface IUIList : IUIComponent
{
    /// <summary>
    /// Items in the list.
    /// </summary>
    IReadOnlyList<object> Items { get; }
    
    /// <summary>
    /// Currently selected item indices.
    /// </summary>
    IReadOnlyList<int> SelectedIndices { get; }
    
    /// <summary>
    /// Whether multiple selections are allowed.
    /// </summary>
    bool AllowMultipleSelection { get; set; }
    
    /// <summary>
    /// Template for rendering list items.
    /// </summary>
    string? ItemTemplate { get; set; }
    
    /// <summary>
    /// Adds an item to the list.
    /// </summary>
    Task AddItemAsync(object item, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes an item from the list.
    /// </summary>
    Task RemoveItemAsync(object item, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Clears all items from the list.
    /// </summary>
    Task ClearItemsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sets the selected items.
    /// </summary>
    Task SetSelectedAsync(IEnumerable<int> indices, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Event fired when list selection changes.
    /// </summary>
    event Func<UIInteractionEvent, Task>? SelectionChanged;
}

/// <summary>
/// Interface for table components that display tabular data.
/// </summary>
public interface IUITable : IUIComponent
{
    /// <summary>
    /// Column headers for the table.
    /// </summary>
    IReadOnlyList<string> Headers { get; }
    
    /// <summary>
    /// Rows of data in the table.
    /// </summary>
    IReadOnlyList<IReadOnlyList<object>> Rows { get; }
    
    /// <summary>
    /// Currently selected row indices.
    /// </summary>
    IReadOnlyList<int> SelectedRows { get; }
    
    /// <summary>
    /// Whether row selection is enabled.
    /// </summary>
    bool AllowRowSelection { get; set; }
    
    /// <summary>
    /// Whether multiple row selection is allowed.
    /// </summary>
    bool AllowMultipleSelection { get; set; }
    
    /// <summary>
    /// Sets the table headers.
    /// </summary>
    Task SetHeadersAsync(IEnumerable<string> headers, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds a row to the table.
    /// </summary>
    Task AddRowAsync(IEnumerable<object> row, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes a row from the table.
    /// </summary>
    Task RemoveRowAsync(int rowIndex, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Clears all rows from the table.
    /// </summary>
    Task ClearRowsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sets the selected rows.
    /// </summary>
    Task SetSelectedRowsAsync(IEnumerable<int> rowIndices, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Event fired when row selection changes.
    /// </summary>
    event Func<UIInteractionEvent, Task>? RowSelectionChanged;
}

/// <summary>
/// Interface for progress bar components.
/// </summary>
public interface IUIProgressBar : IUIComponent
{
    /// <summary>
    /// Current progress value (0.0 to 1.0).
    /// </summary>
    double Value { get; set; }
    
    /// <summary>
    /// Minimum value for the progress bar.
    /// </summary>
    double Minimum { get; set; }
    
    /// <summary>
    /// Maximum value for the progress bar.
    /// </summary>
    double Maximum { get; set; }
    
    /// <summary>
    /// Text to display on or near the progress bar.
    /// </summary>
    string? Text { get; set; }
    
    /// <summary>
    /// Whether the progress bar shows indeterminate progress.
    /// </summary>
    bool IsIndeterminate { get; set; }
    
    /// <summary>
    /// Event fired when progress value changes.
    /// </summary>
    event Func<UIDataBindingEvent, Task>? ProgressChanged;
}

/// <summary>
/// Interface for checkbox components.
/// </summary>
public interface IUICheckbox : IUIComponent
{
    /// <summary>
    /// Label text for the checkbox.
    /// </summary>
    string Label { get; set; }
    
    /// <summary>
    /// Whether the checkbox is checked.
    /// </summary>
    bool IsChecked { get; set; }
    
    /// <summary>
    /// Whether the checkbox is in an indeterminate state (partially checked).
    /// </summary>
    bool? IsIndeterminate { get; set; }
    
    /// <summary>
    /// Event fired when the checked state changes.
    /// </summary>
    event Func<UIDataBindingEvent, Task>? CheckedChanged;
}

/// <summary>
/// Interface for dropdown/select components.
/// </summary>
public interface IUIDropdown : IUIComponent
{
    /// <summary>
    /// Available options in the dropdown.
    /// </summary>
    IReadOnlyList<object> Options { get; }
    
    /// <summary>
    /// Currently selected option.
    /// </summary>
    object? SelectedOption { get; set; }
    
    /// <summary>
    /// Index of the currently selected option.
    /// </summary>
    int? SelectedIndex { get; set; }
    
    /// <summary>
    /// Placeholder text shown when no option is selected.
    /// </summary>
    string? Placeholder { get; set; }
    
    /// <summary>
    /// Template for displaying options.
    /// </summary>
    string? OptionTemplate { get; set; }
    
    /// <summary>
    /// Adds an option to the dropdown.
    /// </summary>
    Task AddOptionAsync(object option, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes an option from the dropdown.
    /// </summary>
    Task RemoveOptionAsync(object option, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Clears all options from the dropdown.
    /// </summary>
    Task ClearOptionsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Event fired when selection changes.
    /// </summary>
    event Func<UIInteractionEvent, Task>? SelectionChanged;
}

/// <summary>
/// Interface for dialog/modal components.
/// </summary>
public interface IUIDialog : IUIComponent
{
    /// <summary>
    /// Title of the dialog.
    /// </summary>
    string Title { get; set; }
    
    /// <summary>
    /// Content/body text of the dialog.
    /// </summary>
    string? Content { get; set; }
    
    /// <summary>
    /// Whether the dialog is currently open/visible.
    /// </summary>
    bool IsOpen { get; set; }
    
    /// <summary>
    /// Whether the dialog can be closed by clicking outside or pressing escape.
    /// </summary>
    bool IsModal { get; set; }
    
    /// <summary>
    /// Available action buttons in the dialog.
    /// </summary>
    IReadOnlyList<string> Actions { get; }
    
    /// <summary>
    /// Shows/opens the dialog.
    /// </summary>
    Task ShowAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Closes/hides the dialog.
    /// </summary>
    Task CloseAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds an action button to the dialog.
    /// </summary>
    Task AddActionAsync(string action, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Event fired when the dialog is closed.
    /// </summary>
    event Func<UIInteractionEvent, Task>? Closed;
    
    /// <summary>
    /// Event fired when an action button is clicked.
    /// </summary>
    event Func<UIInteractionEvent, Task>? ActionClicked;
}