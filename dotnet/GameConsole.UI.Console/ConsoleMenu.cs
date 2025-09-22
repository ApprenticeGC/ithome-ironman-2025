using GameConsole.Input.Core;

namespace GameConsole.UI.Console;

/// <summary>
/// Interactive menu component with keyboard navigation.
/// </summary>
public class ConsoleMenu : BaseUIComponent
{
    private readonly List<MenuItem> _items = new();
    private int _selectedIndex = 0;
    private bool _showBorder = true;
    private string _title = string.Empty;
    
    public ConsoleMenu(string id) : base(id)
    {
        CanFocus = true;
    }
    
    /// <summary>
    /// Gets or sets the menu title.
    /// </summary>
    public string Title 
    { 
        get => _title; 
        set => _title = value ?? string.Empty; 
    }
    
    /// <summary>
    /// Gets or sets whether to show a border around the menu.
    /// </summary>
    public bool ShowBorder 
    { 
        get => _showBorder; 
        set => _showBorder = value; 
    }
    
    /// <summary>
    /// Gets the currently selected menu item index.
    /// </summary>
    public int SelectedIndex => _selectedIndex;
    
    /// <summary>
    /// Gets the currently selected menu item.
    /// </summary>
    public MenuItem? SelectedItem => _selectedIndex >= 0 && _selectedIndex < _items.Count ? _items[_selectedIndex] : null;
    
    /// <summary>
    /// Gets the collection of menu items.
    /// </summary>
    public IReadOnlyList<MenuItem> Items => _items.AsReadOnly();
    
    /// <summary>
    /// Event fired when a menu item is selected.
    /// </summary>
    public event EventHandler<MenuSelectionEventArgs>? ItemSelected;
    
    /// <summary>
    /// Adds a menu item.
    /// </summary>
    /// <param name="text">Display text for the item.</param>
    /// <param name="action">Action to execute when selected.</param>
    /// <param name="enabled">Whether the item is enabled.</param>
    /// <returns>The created menu item.</returns>
    public MenuItem AddItem(string text, Action? action = null, bool enabled = true)
    {
        var item = new MenuItem(text, action, enabled);
        _items.Add(item);
        return item;
    }
    
    /// <summary>
    /// Adds a separator line to the menu.
    /// </summary>
    public void AddSeparator()
    {
        _items.Add(new MenuItem("───", null, false) { IsSeparator = true });
    }
    
    /// <summary>
    /// Removes a menu item.
    /// </summary>
    /// <param name="item">Item to remove.</param>
    /// <returns>True if removed; false otherwise.</returns>
    public bool RemoveItem(MenuItem item)
    {
        var removed = _items.Remove(item);
        if (removed && _selectedIndex >= _items.Count)
        {
            _selectedIndex = Math.Max(0, _items.Count - 1);
        }
        return removed;
    }
    
    /// <summary>
    /// Clears all menu items.
    /// </summary>
    public void ClearItems()
    {
        _items.Clear();
        _selectedIndex = 0;
    }
    
    /// <summary>
    /// Sets the selected item index.
    /// </summary>
    /// <param name="index">Index to select.</param>
    public void SetSelectedIndex(int index)
    {
        if (index >= 0 && index < _items.Count)
        {
            _selectedIndex = index;
        }
    }
    
    public override void Render(IConsoleUIFramework framework)
    {
        if (!IsVisible || ActualBounds.IsEmpty) return;
        
        ClearArea(framework);
        
        int contentX = ActualBounds.X;
        int contentY = ActualBounds.Y;
        int contentWidth = ActualBounds.Width;
        int contentHeight = ActualBounds.Height;
        
        // Draw border if enabled
        if (_showBorder)
        {
            framework.DrawBox(ActualBounds.X, ActualBounds.Y, ActualBounds.Width, ActualBounds.Height, 
                BoxStyle.Single, HasFocus ? ConsoleColor.Cyan : ConsoleColor.Gray);
            contentX += 1;
            contentY += 1;
            contentWidth -= 2;
            contentHeight -= 2;
        }
        
        // Draw title if present
        int itemStartY = contentY;
        if (!string.IsNullOrEmpty(_title))
        {
            var titleColor = HasFocus ? ConsoleColor.White : ConsoleColor.Gray;
            framework.WriteAt(contentX + 1, contentY, _title, titleColor, null, TextStyle.Bold);
            itemStartY += 2;
            contentHeight -= 2;
        }
        
        // Draw menu items
        int visibleItems = Math.Min(_items.Count, contentHeight);
        int startIndex = 0;
        
        // Scroll if selected item is out of view
        if (_selectedIndex >= visibleItems)
        {
            startIndex = _selectedIndex - visibleItems + 1;
        }
        
        for (int i = 0; i < visibleItems && startIndex + i < _items.Count; i++)
        {
            var item = _items[startIndex + i];
            bool isSelected = (startIndex + i) == _selectedIndex;
            
            var foreground = item.Enabled 
                ? (isSelected ? ConsoleColor.Black : ConsoleColor.White)
                : ConsoleColor.DarkGray;
            var background = isSelected ? ConsoleColor.Gray : (ConsoleColor?)null;
            var style = isSelected ? TextStyle.Bold : TextStyle.None;
            
            if (item.IsSeparator)
            {
                var separatorLine = new string('─', Math.Max(1, contentWidth - 2));
                framework.WriteAt(contentX + 1, itemStartY + i, separatorLine, ConsoleColor.DarkGray);
            }
            else
            {
                var prefix = isSelected ? "► " : "  ";
                var displayText = prefix + item.Text;
                
                framework.WriteAt(contentX, itemStartY + i, displayText, foreground, background, style);
            }
        }
        
        // Show scroll indicators if needed
        if (startIndex > 0)
        {
            framework.WriteAt(contentX + contentWidth - 1, itemStartY, "▲", ConsoleColor.Yellow);
        }
        if (startIndex + visibleItems < _items.Count)
        {
            framework.WriteAt(contentX + contentWidth - 1, itemStartY + visibleItems - 1, "▼", ConsoleColor.Yellow);
        }
    }
    
    protected override bool OnHandleInput(UIKeyEventArgs input)
    {
        if (_items.Count == 0) return false;
        
        switch (input.Key)
        {
            case KeyCode.UpArrow:
                MoveToPrevious();
                return true;
                
            case KeyCode.DownArrow:
                MoveToNext();
                return true;
                
            case KeyCode.Return:
            case KeyCode.Space:
                SelectCurrentItem();
                return true;
                
            case KeyCode.Home:
                _selectedIndex = 0;
                return true;
                
            case KeyCode.End:
                _selectedIndex = _items.Count - 1;
                return true;
                
            default:
                return false;
        }
    }
    
    private void MoveToPrevious()
    {
        do
        {
            _selectedIndex = _selectedIndex > 0 ? _selectedIndex - 1 : _items.Count - 1;
        } 
        while (_items.Count > 0 && !_items[_selectedIndex].Enabled && !HasLoopedThroughAll());
    }
    
    private void MoveToNext()
    {
        do
        {
            _selectedIndex = _selectedIndex < _items.Count - 1 ? _selectedIndex + 1 : 0;
        } 
        while (_items.Count > 0 && !_items[_selectedIndex].Enabled && !HasLoopedThroughAll());
    }
    
    private bool HasLoopedThroughAll()
    {
        // Simple check to prevent infinite loops when all items are disabled
        return _items.All(i => !i.Enabled);
    }
    
    private void SelectCurrentItem()
    {
        var selectedItem = SelectedItem;
        if (selectedItem?.Enabled == true && !selectedItem.IsSeparator)
        {
            var eventArgs = new MenuSelectionEventArgs(selectedItem, _selectedIndex);
            ItemSelected?.Invoke(this, eventArgs);
            
            selectedItem.Action?.Invoke();
        }
    }
}

/// <summary>
/// Represents a menu item.
/// </summary>
public class MenuItem
{
    public string Text { get; set; }
    public Action? Action { get; set; }
    public bool Enabled { get; set; }
    public bool IsSeparator { get; set; }
    public object? Tag { get; set; }
    
    public MenuItem(string text, Action? action = null, bool enabled = true)
    {
        Text = text ?? string.Empty;
        Action = action;
        Enabled = enabled;
    }
}

/// <summary>
/// Event arguments for menu selection events.
/// </summary>
public class MenuSelectionEventArgs : EventArgs
{
    public MenuItem Item { get; }
    public int Index { get; }
    
    public MenuSelectionEventArgs(MenuItem item, int index)
    {
        Item = item;
        Index = index;
    }
}