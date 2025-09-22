using GameConsole.Input.Core;

namespace GameConsole.UI.Console;

/// <summary>
/// Console menu component implementation with keyboard navigation.
/// </summary>
public class ConsoleMenu : ConsoleComponentBase, IConsoleMenu
{
    private readonly List<ConsoleMenuItem> _items;
    private int _selectedIndex;
    private ConsoleMenuStyle _style;

    /// <inheritdoc/>
    public string Title { get; set; }

    /// <inheritdoc/>
    public IReadOnlyList<ConsoleMenuItem> Items => _items.AsReadOnly();

    /// <inheritdoc/>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            var newIndex = Math.Max(0, Math.Min(_items.Count - 1, value));
            if (_selectedIndex != newIndex)
            {
                _selectedIndex = newIndex;
                // Find the next enabled item if current is disabled
                EnsureSelectedItemIsEnabled();
            }
        }
    }

    /// <inheritdoc/>
    public ConsoleMenuStyle Style
    {
        get => _style;
        set => _style = value;
    }

    /// <inheritdoc/>
    public event EventHandler<ConsoleMenuItemSelectedEventArgs>? ItemSelected;

    /// <summary>
    /// Initializes a new console menu.
    /// </summary>
    /// <param name="title">Menu title.</param>
    /// <param name="items">Initial menu items.</param>
    /// <param name="position">Menu position.</param>
    /// <param name="size">Menu size.</param>
    /// <param name="style">Menu style.</param>
    public ConsoleMenu(
        string title, 
        IEnumerable<string>? items = null, 
        ConsolePosition position = default, 
        ConsoleSize size = default,
        ConsoleMenuStyle style = default)
        : base(position: position, size: size)
    {
        Title = title ?? string.Empty;
        _items = new List<ConsoleMenuItem>();
        _selectedIndex = 0;
        _style = style.Equals(default) ? ConsoleMenuStyle.Default : style;

        if (items != null)
        {
            foreach (var item in items)
            {
                AddItem(item);
            }
        }

        // Auto-size if not specified
        if (Size.Equals(ConsoleSize.Empty))
        {
            UpdateAutoSize();
        }
    }

    /// <inheritdoc/>
    public void AddItem(string text, Func<Task>? action = null)
    {
        _items.Add(new ConsoleMenuItem(text, action));
        UpdateAutoSize();
        EnsureSelectedItemIsEnabled();
    }

    /// <inheritdoc/>
    public bool RemoveItem(int index)
    {
        if (index >= 0 && index < _items.Count)
        {
            _items.RemoveAt(index);
            
            // Adjust selected index if needed
            if (_selectedIndex >= _items.Count)
            {
                _selectedIndex = Math.Max(0, _items.Count - 1);
            }
            
            UpdateAutoSize();
            EnsureSelectedItemIsEnabled();
            return true;
        }
        return false;
    }

    /// <inheritdoc/>
    public void ClearItems()
    {
        _items.Clear();
        _selectedIndex = 0;
        UpdateAutoSize();
    }

    /// <inheritdoc/>
    public override async Task RenderAsync(IConsoleBuffer buffer, CancellationToken cancellationToken = default)
    {
        if (!IsVisible || Size.Width <= 0 || Size.Height <= 0)
            return;

        await Task.Run(() =>
        {
            // Clear the menu area
            buffer.FillArea(Position.X, Position.Y, Size.Width, Size.Height, ' ', ConsoleTextStyle.Default);

            // Draw border if enabled
            var contentX = Position.X;
            var contentY = Position.Y;
            var contentWidth = Size.Width;
            var contentHeight = Size.Height;

            if (Style.ShowBorder && Size.Width >= 3 && Size.Height >= 3)
            {
                buffer.DrawBorder(Position.X, Position.Y, Size.Width, Size.Height, Style.BorderStyle, Style.ItemStyle);
                contentX += 1;
                contentY += 1;
                contentWidth -= 2;
                contentHeight -= 2;
            }

            var currentY = contentY;

            // Draw title if there's space
            if (!string.IsNullOrEmpty(Title) && contentHeight > 0)
            {
                var titleText = Title.Length > contentWidth ? Title.Substring(0, contentWidth) : Title;
                var centeredTitle = CenterText(titleText, contentWidth);
                buffer.WriteAt(contentX, currentY, centeredTitle, Style.TitleStyle);
                currentY += 1;
                contentHeight -= 1;

                // Draw separator line if there's space
                if (contentHeight > 0)
                {
                    var separatorChar = Style.BorderStyle == ConsoleBorderStyle.Double ? '═' : '─';
                    buffer.WriteAt(contentX, currentY, new string(separatorChar, contentWidth), Style.ItemStyle);
                    currentY += 1;
                    contentHeight -= 1;
                }
            }

            // Draw menu items
            for (int i = 0; i < _items.Count && contentHeight > 0; i++)
            {
                var item = _items[i];
                var isSelected = i == _selectedIndex && HasFocus;
                var style = isSelected ? Style.SelectedItemStyle : Style.ItemStyle;

                var prefix = isSelected ? Style.SelectionIndicator + " " : "  ";
                var itemText = prefix + item.Text;

                // Truncate if too long
                if (itemText.Length > contentWidth)
                {
                    itemText = itemText.Substring(0, contentWidth - 3) + "...";
                }

                // Pad to fill width for selected items (highlighting)
                if (isSelected)
                {
                    itemText = itemText.PadRight(contentWidth);
                }

                buffer.WriteAt(contentX, currentY, itemText, style);
                currentY += 1;
                contentHeight -= 1;
            }
        }, cancellationToken);
    }

    /// <inheritdoc/>
    protected override async Task<bool> HandleKeyPressAsync(KeyCode key, ConsoleInputModifiers modifiers, CancellationToken cancellationToken = default)
    {
        if (_items.Count == 0)
            return false;

        switch (key)
        {
            case KeyCode.UpArrow:
                SelectPreviousItem();
                return true;

            case KeyCode.DownArrow:
                SelectNextItem();
                return true;

            case KeyCode.Home:
                SelectedIndex = 0;
                return true;

            case KeyCode.End:
                SelectedIndex = _items.Count - 1;
                return true;

            case KeyCode.Enter:
                await ExecuteSelectedItemAsync();
                return true;

            case KeyCode.Space:
                await ExecuteSelectedItemAsync();
                return true;

            default:
                // Handle alphanumeric shortcuts
                if (key >= KeyCode.A && key <= KeyCode.Z)
                {
                    var targetChar = char.ToLower((char)((int)'A' + (key - KeyCode.A)));
                    for (int i = 0; i < _items.Count; i++)
                    {
                        if (_items[i].IsEnabled && 
                            !string.IsNullOrEmpty(_items[i].Text) &&
                            char.ToLower(_items[i].Text[0]) == targetChar)
                        {
                            SelectedIndex = i;
                            await ExecuteSelectedItemAsync();
                            return true;
                        }
                    }
                }
                break;
        }

        return false;
    }

    private void SelectNextItem()
    {
        if (_items.Count <= 1)
            return;

        var startIndex = _selectedIndex;
        do
        {
            _selectedIndex = (_selectedIndex + 1) % _items.Count;
        } while (!_items[_selectedIndex].IsEnabled && _selectedIndex != startIndex);
    }

    private void SelectPreviousItem()
    {
        if (_items.Count <= 1)
            return;

        var startIndex = _selectedIndex;
        do
        {
            _selectedIndex = _selectedIndex <= 0 ? _items.Count - 1 : _selectedIndex - 1;
        } while (!_items[_selectedIndex].IsEnabled && _selectedIndex != startIndex);
    }

    private async Task ExecuteSelectedItemAsync()
    {
        if (_selectedIndex >= 0 && _selectedIndex < _items.Count)
        {
            var selectedItem = _items[_selectedIndex];
            if (selectedItem.IsEnabled)
            {
                // Raise the selection event
                ItemSelected?.Invoke(this, new ConsoleMenuItemSelectedEventArgs(_selectedIndex, selectedItem));

                // Execute the action if available
                if (selectedItem.Action != null)
                {
                    await selectedItem.Action();
                }
            }
        }
    }

    private void EnsureSelectedItemIsEnabled()
    {
        if (_items.Count == 0)
        {
            _selectedIndex = 0;
            return;
        }

        // If current selection is enabled, we're good
        if (_selectedIndex >= 0 && _selectedIndex < _items.Count && _items[_selectedIndex].IsEnabled)
            return;

        // Find the first enabled item
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i].IsEnabled)
            {
                _selectedIndex = i;
                return;
            }
        }

        // If no enabled items, stay at current position
    }

    private void UpdateAutoSize()
    {
        if (Size.Equals(ConsoleSize.Empty))
        {
            var maxWidth = Title.Length;
            foreach (var item in _items)
            {
                var itemWidth = Style.SelectionIndicator.Length + 1 + item.Text.Length;
                maxWidth = Math.Max(maxWidth, itemWidth);
            }

            // Add padding for border and margins
            var totalWidth = maxWidth + (Style.ShowBorder ? 4 : 2);
            var totalHeight = _items.Count + (string.IsNullOrEmpty(Title) ? 0 : 2) + (Style.ShowBorder ? 2 : 0);

            Size = new ConsoleSize(totalWidth, Math.Max(totalHeight, 1));
        }
    }
}