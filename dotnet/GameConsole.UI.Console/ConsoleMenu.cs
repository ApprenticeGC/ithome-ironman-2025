using GameConsole.Input.Core;

namespace GameConsole.UI.Console;

/// <summary>
/// Represents a menu item in a console menu.
/// </summary>
public class ConsoleMenuItem
{
    /// <summary>
    /// Initializes a new menu item.
    /// </summary>
    /// <param name="text">The display text.</param>
    /// <param name="action">The action to execute when selected.</param>
    /// <param name="isEnabled">Whether the item is enabled.</param>
    /// <param name="hotkey">Optional hotkey character.</param>
    public ConsoleMenuItem(string text, Action? action = null, bool isEnabled = true, char? hotkey = null)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Action = action;
        IsEnabled = isEnabled;
        Hotkey = hotkey;
    }
    
    /// <summary>
    /// Gets the display text.
    /// </summary>
    public string Text { get; }
    
    /// <summary>
    /// Gets the action to execute when selected.
    /// </summary>
    public Action? Action { get; }
    
    /// <summary>
    /// Gets whether the item is enabled.
    /// </summary>
    public bool IsEnabled { get; }
    
    /// <summary>
    /// Gets the optional hotkey character.
    /// </summary>
    public char? Hotkey { get; }
    
    /// <summary>
    /// Creates a separator menu item.
    /// </summary>
    /// <param name="text">Optional separator text.</param>
    /// <returns>A separator menu item.</returns>
    public static ConsoleMenuItem CreateSeparator(string text = "") => new(text, null, false);
    
    /// <summary>
    /// Gets whether this item is a separator.
    /// </summary>
    public bool IsSeparator => Action == null && !IsEnabled;
}

/// <summary>
/// Interactive console menu with keyboard navigation.
/// </summary>
public class ConsoleMenu : ILayout
{
    private readonly List<ConsoleMenuItem> _items = [];
    private readonly string _title;
    private readonly string _foregroundColor;
    private readonly string _backgroundColor;
    private readonly string _selectedColor;
    private readonly string _disabledColor;
    private readonly LayoutSpacing _padding;
    private int _selectedIndex = 0;
    
    /// <summary>
    /// Initializes a new console menu.
    /// </summary>
    /// <param name="title">Optional menu title.</param>
    /// <param name="foregroundColor">Default foreground color.</param>
    /// <param name="backgroundColor">Default background color.</param>
    /// <param name="selectedColor">Color for selected item.</param>
    /// <param name="disabledColor">Color for disabled items.</param>
    /// <param name="padding">Menu padding.</param>
    public ConsoleMenu(string title = "", string foregroundColor = "", string backgroundColor = "",
                       string selectedColor = ANSIEscapeSequences.FgBrightYellow, 
                       string disabledColor = ANSIEscapeSequences.FgBrightBlack,
                       LayoutSpacing padding = default)
    {
        _title = title ?? string.Empty;
        _foregroundColor = foregroundColor;
        _backgroundColor = backgroundColor;
        _selectedColor = selectedColor;
        _disabledColor = disabledColor;
        _padding = padding;
    }
    
    /// <summary>
    /// Gets the menu items.
    /// </summary>
    public IReadOnlyList<ConsoleMenuItem> Items => _items.AsReadOnly();
    
    /// <summary>
    /// Gets or sets the selected item index.
    /// </summary>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set => _selectedIndex = Math.Max(0, Math.Min(value, _items.Count - 1));
    }
    
    /// <summary>
    /// Gets the currently selected item, or null if none.
    /// </summary>
    public ConsoleMenuItem? SelectedItem => _selectedIndex < _items.Count ? _items[_selectedIndex] : null;
    
    /// <summary>
    /// Event raised when an item is selected.
    /// </summary>
    public event EventHandler<ConsoleMenuItem>? ItemSelected;
    
    /// <summary>
    /// Adds a menu item.
    /// </summary>
    /// <param name="item">The item to add.</param>
    public void AddItem(ConsoleMenuItem item)
    {
        if (item != null)
            _items.Add(item);
    }
    
    /// <summary>
    /// Adds a menu item with text and action.
    /// </summary>
    /// <param name="text">The display text.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="hotkey">Optional hotkey character.</param>
    /// <returns>The created menu item.</returns>
    public ConsoleMenuItem AddItem(string text, Action? action = null, char? hotkey = null)
    {
        var item = new ConsoleMenuItem(text, action, true, hotkey);
        AddItem(item);
        return item;
    }
    
    /// <summary>
    /// Adds a separator.
    /// </summary>
    /// <param name="text">Optional separator text.</param>
    public void AddSeparator(string text = "")
    {
        AddItem(ConsoleMenuItem.CreateSeparator(text));
    }
    
    /// <summary>
    /// Removes a menu item.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    /// <returns>True if the item was found and removed; otherwise, false.</returns>
    public bool RemoveItem(ConsoleMenuItem item)
    {
        var removed = _items.Remove(item);
        if (removed && _selectedIndex >= _items.Count)
            _selectedIndex = Math.Max(0, _items.Count - 1);
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
    /// Handles keyboard input for menu navigation.
    /// </summary>
    /// <param name="key">The pressed key.</param>
    /// <returns>True if the key was handled; otherwise, false.</returns>
    public bool HandleKeyInput(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.UpArrow:
                MoveToPrevious();
                return true;
                
            case KeyCode.DownArrow:
                MoveToNext();
                return true;
                
            case KeyCode.Home:
                MoveToFirst();
                return true;
                
            case KeyCode.End:
                MoveToLast();
                return true;
                
            case KeyCode.Enter:
                SelectCurrentItem();
                return true;
                
            default:
                // Check for hotkey matches
                var keyChar = KeyCodeToChar(key);
                if (keyChar.HasValue && HandleHotkey(keyChar.Value))
                    return true;
                break;
        }
        
        return false;
    }
    
    /// <summary>
    /// Moves selection to the previous enabled item.
    /// </summary>
    public void MoveToPrevious()
    {
        if (_items.Count == 0) return;
        
        var current = _selectedIndex;
        do
        {
            current = current > 0 ? current - 1 : _items.Count - 1;
        } while (_items[current].IsSeparator && current != _selectedIndex);
        
        _selectedIndex = current;
    }
    
    /// <summary>
    /// Moves selection to the next enabled item.
    /// </summary>
    public void MoveToNext()
    {
        if (_items.Count == 0) return;
        
        var current = _selectedIndex;
        do
        {
            current = current < _items.Count - 1 ? current + 1 : 0;
        } while (_items[current].IsSeparator && current != _selectedIndex);
        
        _selectedIndex = current;
    }
    
    /// <summary>
    /// Moves selection to the first enabled item.
    /// </summary>
    public void MoveToFirst()
    {
        for (var i = 0; i < _items.Count; i++)
        {
            if (!_items[i].IsSeparator)
            {
                _selectedIndex = i;
                break;
            }
        }
    }
    
    /// <summary>
    /// Moves selection to the last enabled item.
    /// </summary>
    public void MoveToLast()
    {
        for (var i = _items.Count - 1; i >= 0; i--)
        {
            if (!_items[i].IsSeparator)
            {
                _selectedIndex = i;
                break;
            }
        }
    }
    
    /// <summary>
    /// Selects the current item and executes its action.
    /// </summary>
    public void SelectCurrentItem()
    {
        var item = SelectedItem;
        if (item?.IsEnabled == true && item.Action != null)
        {
            ItemSelected?.Invoke(this, item);
            item.Action();
        }
    }
    
    /// <inheritdoc />
    public ConsoleSize GetDesiredSize(ConsoleSize availableSize)
    {
        var contentWidth = 0;
        var contentHeight = 0;
        
        if (!string.IsNullOrEmpty(_title))
            contentHeight += 2; // Title + separator line
        
        foreach (var item in _items)
        {
            var itemText = GetItemDisplayText(item);
            contentWidth = Math.Max(contentWidth, itemText.Length);
            contentHeight++;
        }
        
        return new ConsoleSize(
            Math.Min(contentWidth + _padding.TotalHorizontal, availableSize.Width),
            Math.Min(contentHeight + _padding.TotalVertical, availableSize.Height));
    }
    
    /// <inheritdoc />
    public ConsoleRect Arrange(ConsoleRect arrangeRect)
    {
        var desiredSize = GetDesiredSize(arrangeRect.Size);
        return new ConsoleRect(arrangeRect.X, arrangeRect.Y,
                              Math.Min(desiredSize.Width, arrangeRect.Width),
                              Math.Min(desiredSize.Height, arrangeRect.Height));
    }
    
    /// <inheritdoc />
    public void Render(ConsoleBuffer buffer, ConsoleRect bounds)
    {
        var contentRect = new ConsoleRect(
            bounds.X + _padding.Left,
            bounds.Y + _padding.Top,
            Math.Max(0, bounds.Width - _padding.TotalHorizontal),
            Math.Max(0, bounds.Height - _padding.TotalVertical));
        
        var y = contentRect.Y;
        
        // Render title
        if (!string.IsNullOrEmpty(_title) && y < contentRect.Bottom)
        {
            buffer.SetText(contentRect.X, y, _title, _foregroundColor, _backgroundColor);
            y++;
            
            if (y < contentRect.Bottom)
            {
                var separator = new string('─', Math.Min(_title.Length, contentRect.Width));
                buffer.SetText(contentRect.X, y, separator, _foregroundColor, _backgroundColor);
                y++;
            }
        }
        
        // Render menu items
        for (var i = 0; i < _items.Count && y < contentRect.Bottom; i++)
        {
            var item = _items[i];
            var isSelected = i == _selectedIndex;
            var itemText = GetItemDisplayText(item);
            
            if (contentRect.Width > 0)
            {
                // Truncate text if needed
                if (itemText.Length > contentRect.Width)
                    itemText = itemText[..contentRect.Width];
                
                // Determine colors
                var foreground = _foregroundColor;
                var background = _backgroundColor;
                
                if (item.IsSeparator)
                {
                    // Render separator
                    var sepText = string.IsNullOrEmpty(item.Text) 
                        ? new string('─', contentRect.Width)
                        : item.Text.PadRight(contentRect.Width, '─');
                    buffer.SetText(contentRect.X, y, sepText, _disabledColor, background);
                }
                else
                {
                    if (isSelected && item.IsEnabled)
                    {
                        foreground = _selectedColor;
                        // Highlight selected item background
                        buffer.FillRect(new ConsoleRect(contentRect.X, y, contentRect.Width, 1), 
                                       ' ', foreground, background);
                    }
                    else if (!item.IsEnabled)
                    {
                        foreground = _disabledColor;
                    }
                    
                    // Add selection indicator
                    var displayText = isSelected && item.IsEnabled ? $"► {itemText}" : $"  {itemText}";
                    if (displayText.Length > contentRect.Width)
                        displayText = displayText[..contentRect.Width];
                    
                    buffer.SetText(contentRect.X, y, displayText, foreground, background);
                }
            }
            
            y++;
        }
    }
    
    private string GetItemDisplayText(ConsoleMenuItem item)
    {
        if (item.Hotkey.HasValue)
            return $"[{item.Hotkey}] {item.Text}";
        return item.Text;
    }
    
    private bool HandleHotkey(char key)
    {
        for (var i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            if (item.Hotkey.HasValue && 
                char.ToUpperInvariant(item.Hotkey.Value) == char.ToUpperInvariant(key) &&
                item.IsEnabled && !item.IsSeparator)
            {
                _selectedIndex = i;
                SelectCurrentItem();
                return true;
            }
        }
        
        return false;
    }
    
    private static char? KeyCodeToChar(KeyCode keyCode)
    {
        return keyCode switch
        {
            KeyCode.A => 'A',
            KeyCode.B => 'B',
            KeyCode.C => 'C',
            KeyCode.D => 'D',
            KeyCode.E => 'E',
            KeyCode.F => 'F',
            KeyCode.G => 'G',
            KeyCode.H => 'H',
            KeyCode.I => 'I',
            KeyCode.J => 'J',
            KeyCode.K => 'K',
            KeyCode.L => 'L',
            KeyCode.M => 'M',
            KeyCode.N => 'N',
            KeyCode.O => 'O',
            KeyCode.P => 'P',
            KeyCode.Q => 'Q',
            KeyCode.R => 'R',
            KeyCode.S => 'S',
            KeyCode.T => 'T',
            KeyCode.U => 'U',
            KeyCode.V => 'V',
            KeyCode.W => 'W',
            KeyCode.X => 'X',
            KeyCode.Y => 'Y',
            KeyCode.Z => 'Z',
            KeyCode.Alpha0 => '0',
            KeyCode.Alpha1 => '1',
            KeyCode.Alpha2 => '2',
            KeyCode.Alpha3 => '3',
            KeyCode.Alpha4 => '4',
            KeyCode.Alpha5 => '5',
            KeyCode.Alpha6 => '6',
            KeyCode.Alpha7 => '7',
            KeyCode.Alpha8 => '8',
            KeyCode.Alpha9 => '9',
            _ => null
        };
    }
}