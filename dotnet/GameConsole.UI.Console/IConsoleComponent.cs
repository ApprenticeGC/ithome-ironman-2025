namespace GameConsole.UI.Console;

/// <summary>
/// Base interface for all console UI components.
/// </summary>
public interface IConsoleComponent
{
    /// <summary>
    /// Gets the unique identifier for this component.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets or sets the position of the component on the console.
    /// </summary>
    ConsolePosition Position { get; set; }

    /// <summary>
    /// Gets or sets the size of the component.
    /// </summary>
    ConsoleSize Size { get; set; }

    /// <summary>
    /// Gets or sets whether the component is visible.
    /// </summary>
    bool IsVisible { get; set; }

    /// <summary>
    /// Gets or sets whether the component can receive focus.
    /// </summary>
    bool CanReceiveFocus { get; set; }

    /// <summary>
    /// Gets or sets whether the component currently has focus.
    /// </summary>
    bool HasFocus { get; set; }

    /// <summary>
    /// Renders the component to the console buffer.
    /// </summary>
    /// <param name="buffer">Console buffer to render to.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async render operation.</returns>
    Task RenderAsync(IConsoleBuffer buffer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles input events for this component.
    /// </summary>
    /// <param name="inputEvent">Input event to handle.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the event was handled, false otherwise.</returns>
    Task<bool> HandleInputAsync(ConsoleInputEvent inputEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when the component requests focus.
    /// </summary>
    event EventHandler<EventArgs>? FocusRequested;

    /// <summary>
    /// Event raised when the component gains focus.
    /// </summary>
    event EventHandler<EventArgs>? FocusGained;

    /// <summary>
    /// Event raised when the component loses focus.
    /// </summary>
    event EventHandler<EventArgs>? FocusLost;
}

/// <summary>
/// Console menu component interface.
/// </summary>
public interface IConsoleMenu : IConsoleComponent
{
    /// <summary>
    /// Gets or sets the menu title.
    /// </summary>
    string Title { get; set; }

    /// <summary>
    /// Gets the menu items.
    /// </summary>
    IReadOnlyList<ConsoleMenuItem> Items { get; }

    /// <summary>
    /// Gets or sets the currently selected item index.
    /// </summary>
    int SelectedIndex { get; set; }

    /// <summary>
    /// Gets or sets the menu style.
    /// </summary>
    ConsoleMenuStyle Style { get; set; }

    /// <summary>
    /// Adds an item to the menu.
    /// </summary>
    /// <param name="text">Menu item text.</param>
    /// <param name="action">Action to execute when selected.</param>
    void AddItem(string text, Func<Task>? action = null);

    /// <summary>
    /// Removes an item from the menu.
    /// </summary>
    /// <param name="index">Index of item to remove.</param>
    /// <returns>True if item was removed, false otherwise.</returns>
    bool RemoveItem(int index);

    /// <summary>
    /// Clears all menu items.
    /// </summary>
    void ClearItems();

    /// <summary>
    /// Event raised when a menu item is selected.
    /// </summary>
    event EventHandler<ConsoleMenuItemSelectedEventArgs>? ItemSelected;
}

/// <summary>
/// Console table component interface.
/// </summary>
public interface IConsoleTable : IConsoleComponent
{
    /// <summary>
    /// Gets the table headers.
    /// </summary>
    IReadOnlyList<string> Headers { get; }

    /// <summary>
    /// Gets the table rows.
    /// </summary>
    IReadOnlyList<IReadOnlyList<string>> Rows { get; }

    /// <summary>
    /// Gets or sets the table style.
    /// </summary>
    ConsoleTableStyle Style { get; set; }

    /// <summary>
    /// Gets or sets the currently selected row index.
    /// </summary>
    int SelectedRowIndex { get; set; }

    /// <summary>
    /// Gets or sets whether table sorting is enabled.
    /// </summary>
    bool IsSortingEnabled { get; set; }

    /// <summary>
    /// Gets or sets whether table filtering is enabled.
    /// </summary>
    bool IsFilteringEnabled { get; set; }

    /// <summary>
    /// Adds a row to the table.
    /// </summary>
    /// <param name="values">Row values.</param>
    void AddRow(params string[] values);

    /// <summary>
    /// Removes a row from the table.
    /// </summary>
    /// <param name="index">Index of row to remove.</param>
    /// <returns>True if row was removed, false otherwise.</returns>
    bool RemoveRow(int index);

    /// <summary>
    /// Clears all table rows.
    /// </summary>
    void ClearRows();

    /// <summary>
    /// Sorts the table by the specified column.
    /// </summary>
    /// <param name="columnIndex">Column index to sort by.</param>
    /// <param name="ascending">True for ascending sort, false for descending.</param>
    void SortByColumn(int columnIndex, bool ascending = true);

    /// <summary>
    /// Filters table rows based on the specified predicate.
    /// </summary>
    /// <param name="filter">Filter predicate.</param>
    void FilterRows(Func<IReadOnlyList<string>, bool> filter);

    /// <summary>
    /// Clears any active filters.
    /// </summary>
    void ClearFilters();

    /// <summary>
    /// Event raised when a table row is selected.
    /// </summary>
    event EventHandler<ConsoleTableRowSelectedEventArgs>? RowSelected;
}

/// <summary>
/// Console progress bar component interface.
/// </summary>
public interface IConsoleProgressBar : IConsoleComponent
{
    /// <summary>
    /// Gets or sets the progress bar label.
    /// </summary>
    string Label { get; set; }

    /// <summary>
    /// Gets or sets the current progress value.
    /// </summary>
    int Value { get; set; }

    /// <summary>
    /// Gets or sets the maximum progress value.
    /// </summary>
    int MaxValue { get; set; }

    /// <summary>
    /// Gets the current progress percentage (0.0 to 1.0).
    /// </summary>
    double PercentComplete { get; }

    /// <summary>
    /// Gets or sets the progress bar style.
    /// </summary>
    ConsoleProgressBarStyle Style { get; set; }

    /// <summary>
    /// Gets or sets whether to show percentage text.
    /// </summary>
    bool ShowPercentage { get; set; }

    /// <summary>
    /// Gets or sets whether the progress bar is indeterminate.
    /// </summary>
    bool IsIndeterminate { get; set; }

    /// <summary>
    /// Increments the progress value by the specified amount.
    /// </summary>
    /// <param name="increment">Amount to increment by.</param>
    void Increment(int increment = 1);

    /// <summary>
    /// Resets the progress to zero.
    /// </summary>
    void Reset();

    /// <summary>
    /// Event raised when progress value changes.
    /// </summary>
    event EventHandler<ConsoleProgressChangedEventArgs>? ProgressChanged;
}

/// <summary>
/// Console text component interface.
/// </summary>
public interface IConsoleText : IConsoleComponent
{
    /// <summary>
    /// Gets or sets the text content.
    /// </summary>
    string Text { get; set; }

    /// <summary>
    /// Gets or sets the text style.
    /// </summary>
    ConsoleTextStyle Style { get; set; }

    /// <summary>
    /// Gets or sets whether text wrapping is enabled.
    /// </summary>
    bool WordWrap { get; set; }

    /// <summary>
    /// Gets or sets the text alignment.
    /// </summary>
    ConsoleTextAlignment Alignment { get; set; }
}