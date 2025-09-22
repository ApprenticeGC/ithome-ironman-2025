using System.Text;

namespace GameConsole.UI.Core;

/// <summary>
/// Base interface for all UI components.
/// </summary>
public interface IUIComponent
{
    /// <summary>
    /// Unique identifier for the component.
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Parent component, if any.
    /// </summary>
    IUIComponent? Parent { get; set; }
    
    /// <summary>
    /// Child components.
    /// </summary>
    IReadOnlyList<IUIComponent> Children { get; }
    
    /// <summary>
    /// Component bounds (position and size).
    /// </summary>
    ComponentBounds Bounds { get; set; }
    
    /// <summary>
    /// Component styling.
    /// </summary>
    ComponentStyle Style { get; set; }
    
    /// <summary>
    /// Component visibility.
    /// </summary>
    Visibility Visibility { get; set; }
    
    /// <summary>
    /// Whether the component can receive focus.
    /// </summary>
    bool CanFocus { get; set; }
    
    /// <summary>
    /// Whether the component currently has focus.
    /// </summary>
    bool HasFocus { get; set; }
    
    /// <summary>
    /// Current component state.
    /// </summary>
    ComponentState State { get; set; }
    
    /// <summary>
    /// Component padding.
    /// </summary>
    Spacing Padding { get; set; }
    
    /// <summary>
    /// Component margin.
    /// </summary>
    Spacing Margin { get; set; }
    
    /// <summary>
    /// Adds a child component.
    /// </summary>
    /// <param name="child">Component to add.</param>
    void AddChild(IUIComponent child);
    
    /// <summary>
    /// Removes a child component.
    /// </summary>
    /// <param name="child">Component to remove.</param>
    /// <returns>True if removed, false if not found.</returns>
    bool RemoveChild(IUIComponent child);
    
    /// <summary>
    /// Renders the component to a string buffer.
    /// </summary>
    /// <param name="context">Rendering context.</param>
    /// <returns>Rendered component as string.</returns>
    Task<string> RenderAsync(RenderContext context);
    
    /// <summary>
    /// Calculates the preferred size of the component.
    /// </summary>
    /// <param name="availableSize">Available space for the component.</param>
    /// <returns>Preferred size.</returns>
    ComponentSize CalculatePreferredSize(ComponentSize availableSize);
    
    /// <summary>
    /// Updates the component layout.
    /// </summary>
    /// <param name="availableBounds">Available bounds for layout.</param>
    void UpdateLayout(ComponentBounds availableBounds);
    
    /// <summary>
    /// Event raised when the component is clicked.
    /// </summary>
    event EventHandler<ComponentClickEventArgs>? Click;
    
    /// <summary>
    /// Event raised when a key is pressed while the component has focus.
    /// </summary>
    event EventHandler<ComponentKeyEventArgs>? KeyPress;
    
    /// <summary>
    /// Event raised when the component state changes.
    /// </summary>
    event EventHandler<ComponentStateChangedEventArgs>? StateChanged;
}

/// <summary>
/// Interface for components that can contain and layout other components.
/// </summary>
public interface ILayoutContainer : IUIComponent
{
    /// <summary>
    /// Layout direction (horizontal or vertical).
    /// </summary>
    LayoutDirection Direction { get; set; }
    
    /// <summary>
    /// Horizontal alignment for child components.
    /// </summary>
    HorizontalAlignment HorizontalAlignment { get; set; }
    
    /// <summary>
    /// Vertical alignment for child components.
    /// </summary>
    VerticalAlignment VerticalAlignment { get; set; }
    
    /// <summary>
    /// Spacing between child components.
    /// </summary>
    int ChildSpacing { get; set; }
    
    /// <summary>
    /// Arranges child components within the container.
    /// </summary>
    /// <param name="availableBounds">Available space for children.</param>
    void ArrangeChildren(ComponentBounds availableBounds);
}

/// <summary>
/// Interface for components that display text.
/// </summary>
public interface ITextComponent : IUIComponent
{
    /// <summary>
    /// Text content to display.
    /// </summary>
    string Text { get; set; }
    
    /// <summary>
    /// Text alignment within the component.
    /// </summary>
    TextAlignment TextAlignment { get; set; }
    
    /// <summary>
    /// Whether text should wrap to multiple lines.
    /// </summary>
    bool WordWrap { get; set; }
    
    /// <summary>
    /// Maximum number of lines to display.
    /// </summary>
    int MaxLines { get; set; }
}

/// <summary>
/// Interface for interactive components that can be clicked.
/// </summary>
public interface IClickableComponent : IUIComponent
{
    /// <summary>
    /// Whether the component is enabled for interaction.
    /// </summary>
    bool IsEnabled { get; set; }
    
    /// <summary>
    /// Whether the component responds to mouse clicks.
    /// </summary>
    bool AcceptsClicks { get; set; }
    
    /// <summary>
    /// Command to execute when clicked.
    /// </summary>
    string? Command { get; set; }
    
    /// <summary>
    /// Parameters for the command.
    /// </summary>
    object? CommandParameter { get; set; }
}

/// <summary>
/// Interface for components that accept text input.
/// </summary>
public interface IInputComponent : ITextComponent
{
    /// <summary>
    /// Current input value.
    /// </summary>
    string Value { get; set; }
    
    /// <summary>
    /// Placeholder text when input is empty.
    /// </summary>
    string? Placeholder { get; set; }
    
    /// <summary>
    /// Whether the input is read-only.
    /// </summary>
    bool IsReadOnly { get; set; }
    
    /// <summary>
    /// Maximum length of input.
    /// </summary>
    int MaxLength { get; set; }
    
    /// <summary>
    /// Whether to mask input characters (for passwords).
    /// </summary>
    bool IsMasked { get; set; }
    
    /// <summary>
    /// Event raised when the input value changes.
    /// </summary>
    event EventHandler<InputValueChangedEventArgs>? ValueChanged;
}

/// <summary>
/// Interface for selectable components (radio buttons, checkboxes).
/// </summary>
public interface ISelectableComponent : IUIComponent
{
    /// <summary>
    /// Whether the component is selected/checked.
    /// </summary>
    bool IsSelected { get; set; }
    
    /// <summary>
    /// Value associated with the component.
    /// </summary>
    object? Value { get; set; }
    
    /// <summary>
    /// Selection group (for radio buttons).
    /// </summary>
    string? GroupName { get; set; }
    
    /// <summary>
    /// Event raised when selection state changes.
    /// </summary>
    event EventHandler<SelectionChangedEventArgs>? SelectionChanged;
}

/// <summary>
/// Interface for dialog components.
/// </summary>
public interface IDialog : ILayoutContainer
{
    /// <summary>
    /// Dialog title.
    /// </summary>
    string Title { get; set; }
    
    /// <summary>
    /// Whether the dialog is modal.
    /// </summary>
    bool IsModal { get; set; }
    
    /// <summary>
    /// Whether the dialog can be resized.
    /// </summary>
    bool CanResize { get; set; }
    
    /// <summary>
    /// Dialog result when closed.
    /// </summary>
    DialogResult Result { get; set; }
    
    /// <summary>
    /// Closes the dialog with the specified result.
    /// </summary>
    /// <param name="result">Result to return.</param>
    void Close(DialogResult result);
    
    /// <summary>
    /// Event raised when the dialog is closing.
    /// </summary>
    event EventHandler<DialogClosingEventArgs>? Closing;
    
    /// <summary>
    /// Event raised when the dialog is closed.
    /// </summary>
    event EventHandler<DialogClosedEventArgs>? Closed;
}

/// <summary>
/// Interface for menu item components.
/// </summary>
public interface IMenuItem : IClickableComponent
{
    /// <summary>
    /// Display text for the menu item.
    /// </summary>
    string DisplayText { get; set; }
    
    /// <summary>
    /// Keyboard shortcut for the menu item.
    /// </summary>
    string? Shortcut { get; set; }
    
    /// <summary>
    /// Whether the menu item is a separator.
    /// </summary>
    bool IsSeparator { get; set; }
    
    /// <summary>
    /// Sub-menu items.
    /// </summary>
    IReadOnlyList<IMenuItem> SubItems { get; }
    
    /// <summary>
    /// Adds a sub-menu item.
    /// </summary>
    /// <param name="item">Item to add.</param>
    void AddSubItem(IMenuItem item);
}

/// <summary>
/// Interface for context menu components.
/// </summary>
public interface IContextMenu : IUIComponent
{
    /// <summary>
    /// Menu items in the context menu.
    /// </summary>
    IReadOnlyList<IMenuItem> Items { get; }
    
    /// <summary>
    /// Adds a menu item.
    /// </summary>
    /// <param name="item">Item to add.</param>
    void AddItem(IMenuItem item);
    
    /// <summary>
    /// Shows the menu at the specified position.
    /// </summary>
    /// <param name="position">Position to show at.</param>
    /// <returns>Selected item result.</returns>
    Task<MenuItemResult> ShowAsync(ConsolePosition position);
}

/// <summary>
/// Interface for menu bar components.
/// </summary>
public interface IMenuBar : ILayoutContainer
{
    /// <summary>
    /// Menu items in the menu bar.
    /// </summary>
    IReadOnlyList<IMenuItem> Items { get; }
    
    /// <summary>
    /// Adds a menu item to the bar.
    /// </summary>
    /// <param name="item">Item to add.</param>
    void AddItem(IMenuItem item);
    
    /// <summary>
    /// Currently selected menu item index.
    /// </summary>
    int SelectedIndex { get; set; }
}

/// <summary>
/// Event arguments for component click events.
/// </summary>
public class ComponentClickEventArgs : EventArgs
{
    public ConsolePosition Position { get; }
    public MouseButton Button { get; }
    public DateTime ClickTime { get; }
    
    public ComponentClickEventArgs(ConsolePosition position, MouseButton button = MouseButton.Left)
    {
        Position = position;
        Button = button;
        ClickTime = DateTime.UtcNow;
    }
}

/// <summary>
/// Event arguments for component key events.
/// </summary>
public class ComponentKeyEventArgs : EventArgs
{
    public ConsoleKeyInfo KeyInfo { get; }
    public bool Handled { get; set; }
    public DateTime KeyTime { get; }
    
    public ComponentKeyEventArgs(ConsoleKeyInfo keyInfo)
    {
        KeyInfo = keyInfo;
        KeyTime = DateTime.UtcNow;
    }
}

/// <summary>
/// Event arguments for input value changes.
/// </summary>
public class InputValueChangedEventArgs : EventArgs
{
    public string OldValue { get; }
    public string NewValue { get; }
    public DateTime ChangeTime { get; }
    
    public InputValueChangedEventArgs(string oldValue, string newValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
        ChangeTime = DateTime.UtcNow;
    }
}

/// <summary>
/// Event arguments for selection changes.
/// </summary>
public class SelectionChangedEventArgs : EventArgs
{
    public bool WasSelected { get; }
    public bool IsSelected { get; }
    public object? Value { get; }
    public DateTime ChangeTime { get; }
    
    public SelectionChangedEventArgs(bool wasSelected, bool isSelected, object? value = null)
    {
        WasSelected = wasSelected;
        IsSelected = isSelected;
        Value = value;
        ChangeTime = DateTime.UtcNow;
    }
}

/// <summary>
/// Event arguments for dialog closing events.
/// </summary>
public class DialogClosingEventArgs : EventArgs
{
    public DialogResult Result { get; }
    public bool Cancel { get; set; }
    public DateTime ClosingTime { get; }
    
    public DialogClosingEventArgs(DialogResult result)
    {
        Result = result;
        ClosingTime = DateTime.UtcNow;
    }
}

/// <summary>
/// Event arguments for dialog closed events.
/// </summary>
public class DialogClosedEventArgs : EventArgs
{
    public DialogResult Result { get; }
    public DateTime ClosedTime { get; }
    
    public DialogClosedEventArgs(DialogResult result)
    {
        Result = result;
        ClosedTime = DateTime.UtcNow;
    }
}

/// <summary>
/// Placeholder for mouse button enum (would reference Input.Core in real implementation).
/// </summary>
public enum MouseButton
{
    Left = 0,
    Right = 1,
    Middle = 2
}