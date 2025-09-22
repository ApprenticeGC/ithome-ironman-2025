using GameConsole.Input.Core;

namespace GameConsole.UI.Console;

/// <summary>
/// Console table component implementation with sorting and filtering.
/// </summary>
public class ConsoleTable : ConsoleComponentBase, IConsoleTable
{
    private readonly List<string> _headers;
    private readonly List<List<string>> _allRows;
    private readonly List<List<string>> _filteredRows;
    private int _selectedRowIndex;
    private ConsoleTableStyle _style;
    private int _sortColumnIndex = -1;
    private bool _sortAscending = true;
    private Func<IReadOnlyList<string>, bool>? _currentFilter;

    /// <inheritdoc/>
    public IReadOnlyList<string> Headers => _headers.AsReadOnly();

    /// <inheritdoc/>
    public IReadOnlyList<IReadOnlyList<string>> Rows => _filteredRows.Select(row => (IReadOnlyList<string>)row.AsReadOnly()).ToList().AsReadOnly();

    /// <inheritdoc/>
    public ConsoleTableStyle Style
    {
        get => _style;
        set => _style = value;
    }

    /// <inheritdoc/>
    public int SelectedRowIndex
    {
        get => _selectedRowIndex;
        set => _selectedRowIndex = Math.Max(0, Math.Min(_filteredRows.Count - 1, value));
    }

    /// <inheritdoc/>
    public bool IsSortingEnabled { get; set; } = true;

    /// <inheritdoc/>
    public bool IsFilteringEnabled { get; set; } = true;

    /// <inheritdoc/>
    public event EventHandler<ConsoleTableRowSelectedEventArgs>? RowSelected;

    /// <summary>
    /// Initializes a new console table.
    /// </summary>
    /// <param name="headers">Table column headers.</param>
    /// <param name="position">Table position.</param>
    /// <param name="size">Table size.</param>
    /// <param name="style">Table style.</param>
    public ConsoleTable(
        IEnumerable<string> headers,
        ConsolePosition position = default,
        ConsoleSize size = default,
        ConsoleTableStyle style = default)
        : base(position: position, size: size)
    {
        _headers = headers?.ToList() ?? new List<string>();
        _allRows = new List<List<string>>();
        _filteredRows = new List<List<string>>();
        _selectedRowIndex = 0;
        _style = style.Equals(default) ? ConsoleTableStyle.Default : style;

        // Auto-size if not specified
        if (Size.Equals(ConsoleSize.Empty))
        {
            UpdateAutoSize();
        }
    }

    /// <inheritdoc/>
    public void AddRow(params string[] values)
    {
        var row = new List<string>();
        
        // Ensure row has same number of columns as headers
        for (int i = 0; i < _headers.Count; i++)
        {
            row.Add(i < values.Length ? values[i] ?? string.Empty : string.Empty);
        }

        _allRows.Add(row);
        ApplyCurrentFilter();
        UpdateAutoSize();
    }

    /// <inheritdoc/>
    public bool RemoveRow(int index)
    {
        if (index >= 0 && index < _allRows.Count)
        {
            _allRows.RemoveAt(index);
            ApplyCurrentFilter();
            
            // Adjust selected index if needed
            if (_selectedRowIndex >= _filteredRows.Count)
            {
                _selectedRowIndex = Math.Max(0, _filteredRows.Count - 1);
            }
            
            UpdateAutoSize();
            return true;
        }
        return false;
    }

    /// <inheritdoc/>
    public void ClearRows()
    {
        _allRows.Clear();
        _filteredRows.Clear();
        _selectedRowIndex = 0;
        UpdateAutoSize();
    }

    /// <inheritdoc/>
    public void SortByColumn(int columnIndex, bool ascending = true)
    {
        if (!IsSortingEnabled || columnIndex < 0 || columnIndex >= _headers.Count)
            return;

        _sortColumnIndex = columnIndex;
        _sortAscending = ascending;

        _allRows.Sort((row1, row2) =>
        {
            var value1 = columnIndex < row1.Count ? row1[columnIndex] : string.Empty;
            var value2 = columnIndex < row2.Count ? row2[columnIndex] : string.Empty;

            var comparison = string.Compare(value1, value2, StringComparison.OrdinalIgnoreCase);
            return ascending ? comparison : -comparison;
        });

        ApplyCurrentFilter();
    }

    /// <inheritdoc/>
    public void FilterRows(Func<IReadOnlyList<string>, bool> filter)
    {
        if (!IsFilteringEnabled)
            return;

        _currentFilter = filter;
        ApplyCurrentFilter();
    }

    /// <inheritdoc/>
    public void ClearFilters()
    {
        _currentFilter = null;
        ApplyCurrentFilter();
    }

    /// <inheritdoc/>
    public override async Task RenderAsync(IConsoleBuffer buffer, CancellationToken cancellationToken = default)
    {
        if (!IsVisible || Size.Width <= 0 || Size.Height <= 0)
            return;

        await Task.Run(() =>
        {
            // Clear the table area
            buffer.FillArea(Position.X, Position.Y, Size.Width, Size.Height, ' ', ConsoleTextStyle.Default);

            var contentX = Position.X;
            var contentY = Position.Y;
            var contentWidth = Size.Width;
            var contentHeight = Size.Height;

            // Draw border if enabled
            if (Style.ShowBorders && Size.Width >= 3 && Size.Height >= 3)
            {
                buffer.DrawBorder(Position.X, Position.Y, Size.Width, Size.Height, Style.BorderStyle, Style.RowStyle);
                contentX += 1;
                contentY += 1;
                contentWidth -= 2;
                contentHeight -= 2;
            }

            if (contentWidth <= 0 || contentHeight <= 0)
                return;

            // Calculate column widths
            var columnWidths = CalculateColumnWidths(contentWidth);
            var currentY = contentY;

            // Draw headers if there's space
            if (_headers.Count > 0 && contentHeight > 0)
            {
                DrawHeaderRow(buffer, contentX, currentY, columnWidths);
                currentY += 1;
                contentHeight -= 1;

                // Draw header separator if there's space and borders are enabled
                if (contentHeight > 0 && Style.ShowBorders)
                {
                    DrawSeparatorRow(buffer, contentX, currentY, columnWidths);
                    currentY += 1;
                    contentHeight -= 1;
                }
            }

            // Draw data rows
            var startRow = Math.Max(0, _selectedRowIndex - (contentHeight - 1) / 2);
            for (int i = startRow; i < _filteredRows.Count && contentHeight > 0; i++)
            {
                var isSelected = i == _selectedRowIndex && HasFocus;
                DrawDataRow(buffer, contentX, currentY, columnWidths, _filteredRows[i], isSelected, i % 2 == 1 && Style.AlternateRows);
                currentY += 1;
                contentHeight -= 1;
            }

        }, cancellationToken);
    }

    /// <inheritdoc/>
    protected override async Task<bool> HandleKeyPressAsync(KeyCode key, ConsoleInputModifiers modifiers, CancellationToken cancellationToken = default)
    {
        if (_filteredRows.Count == 0)
            return false;

        switch (key)
        {
            case KeyCode.UpArrow:
                if (_selectedRowIndex > 0)
                {
                    _selectedRowIndex--;
                }
                return true;

            case KeyCode.DownArrow:
                if (_selectedRowIndex < _filteredRows.Count - 1)
                {
                    _selectedRowIndex++;
                }
                return true;

            case KeyCode.Home:
                _selectedRowIndex = 0;
                return true;

            case KeyCode.End:
                _selectedRowIndex = _filteredRows.Count - 1;
                return true;

            case KeyCode.PageUp:
                _selectedRowIndex = Math.Max(0, _selectedRowIndex - 10);
                return true;

            case KeyCode.PageDown:
                _selectedRowIndex = Math.Min(_filteredRows.Count - 1, _selectedRowIndex + 10);
                return true;

            case KeyCode.Enter:
                await ExecuteRowSelectionAsync();
                return true;

            case KeyCode.Space:
                await ExecuteRowSelectionAsync();
                return true;

            default:
                // Handle column sorting shortcuts
                if (IsSortingEnabled && key >= KeyCode.F1 && key <= KeyCode.F12)
                {
                    var columnIndex = key - KeyCode.F1;
                    if (columnIndex < _headers.Count)
                    {
                        var ascending = _sortColumnIndex != columnIndex || !_sortAscending;
                        SortByColumn(columnIndex, ascending);
                        return true;
                    }
                }
                break;
        }

        return false;
    }

    private void ApplyCurrentFilter()
    {
        _filteredRows.Clear();

        foreach (var row in _allRows)
        {
            if (_currentFilter == null || _currentFilter(row.AsReadOnly()))
            {
                _filteredRows.Add(row);
            }
        }

        // Adjust selected index if needed
        if (_selectedRowIndex >= _filteredRows.Count)
        {
            _selectedRowIndex = Math.Max(0, _filteredRows.Count - 1);
        }
    }

    private List<int> CalculateColumnWidths(int availableWidth)
    {
        var widths = new List<int>();
        if (_headers.Count == 0 || availableWidth <= 0)
            return widths;

        // Calculate minimum required width for each column
        var minWidths = new List<int>();
        var totalMinWidth = 0;

        for (int col = 0; col < _headers.Count; col++)
        {
            var minWidth = Math.Max(3, _headers[col].Length); // Minimum 3 characters
            
            // Check data rows for longer content
            foreach (var row in _filteredRows.Take(100)) // Sample first 100 rows for performance
            {
                if (col < row.Count)
                {
                    minWidth = Math.Max(minWidth, row[col].Length);
                }
            }

            minWidths.Add(minWidth);
            totalMinWidth += minWidth;
        }

        // Add space for column separators
        var separatorSpace = Style.ShowBorders ? (_headers.Count - 1) : 0;
        totalMinWidth += separatorSpace;

        if (totalMinWidth <= availableWidth)
        {
            // Distribute extra space proportionally
            var extraSpace = availableWidth - totalMinWidth;
            var spacePerColumn = extraSpace / _headers.Count;
            var remainingSpace = extraSpace % _headers.Count;

            for (int i = 0; i < _headers.Count; i++)
            {
                var width = minWidths[i] + spacePerColumn;
                if (i < remainingSpace)
                    width += 1;
                widths.Add(width);
            }
        }
        else
        {
            // Not enough space, use proportional reduction
            var reductionFactor = (double)availableWidth / totalMinWidth;
            var usedWidth = 0;

            for (int i = 0; i < _headers.Count - 1; i++)
            {
                var width = Math.Max(1, (int)(minWidths[i] * reductionFactor));
                widths.Add(width);
                usedWidth += width;
            }

            // Give remaining space to last column
            widths.Add(Math.Max(1, availableWidth - usedWidth - separatorSpace));
        }

        return widths;
    }

    private void DrawHeaderRow(IConsoleBuffer buffer, int x, int y, List<int> columnWidths)
    {
        var currentX = x;

        for (int col = 0; col < _headers.Count && col < columnWidths.Count; col++)
        {
            var header = _headers[col];
            var width = columnWidths[col];

            // Add sort indicator if this column is sorted
            if (_sortColumnIndex == col)
            {
                header += _sortAscending ? " ↑" : " ↓";
            }

            // Truncate if too long
            if (header.Length > width)
            {
                header = header.Substring(0, width - 3) + "...";
            }

            buffer.WriteAt(currentX, y, header.PadRight(width), Style.HeaderStyle);
            currentX += width;

            // Draw column separator
            if (col < _headers.Count - 1 && Style.ShowBorders)
            {
                buffer.WriteCharAt(currentX, y, '│', Style.HeaderStyle);
                currentX += 1;
            }
        }
    }

    private void DrawSeparatorRow(IConsoleBuffer buffer, int x, int y, List<int> columnWidths)
    {
        var currentX = x;
        var separatorChar = Style.BorderStyle == ConsoleBorderStyle.Double ? '═' : '─';
        var junctionChar = Style.BorderStyle == ConsoleBorderStyle.Double ? '╬' : '┼';

        for (int col = 0; col < columnWidths.Count; col++)
        {
            var width = columnWidths[col];
            buffer.WriteAt(currentX, y, new string(separatorChar, width), Style.RowStyle);
            currentX += width;

            if (col < columnWidths.Count - 1)
            {
                buffer.WriteCharAt(currentX, y, junctionChar, Style.RowStyle);
                currentX += 1;
            }
        }
    }

    private void DrawDataRow(IConsoleBuffer buffer, int x, int y, List<int> columnWidths, List<string> row, bool isSelected, bool isAlternate)
    {
        var currentX = x;
        var style = isSelected ? Style.SelectedRowStyle : 
                    (isAlternate ? new ConsoleTextStyle(Style.RowStyle.ForegroundColor, ConsoleColorType.DarkGray) : Style.RowStyle);

        for (int col = 0; col < columnWidths.Count && col < _headers.Count; col++)
        {
            var cellValue = col < row.Count ? row[col] : string.Empty;
            var width = columnWidths[col];

            // Truncate if too long
            if (cellValue.Length > width)
            {
                cellValue = cellValue.Substring(0, width - 3) + "...";
            }

            var displayValue = cellValue.PadRight(width);
            buffer.WriteAt(currentX, y, displayValue, style);
            currentX += width;

            // Draw column separator
            if (col < columnWidths.Count - 1 && Style.ShowBorders)
            {
                buffer.WriteCharAt(currentX, y, '│', style);
                currentX += 1;
            }
        }
    }

    private async Task ExecuteRowSelectionAsync()
    {
        if (_selectedRowIndex >= 0 && _selectedRowIndex < _filteredRows.Count)
        {
            var selectedRow = _filteredRows[_selectedRowIndex];
            var eventArgs = new ConsoleTableRowSelectedEventArgs(_selectedRowIndex, selectedRow.AsReadOnly());
            RowSelected?.Invoke(this, eventArgs);
        }

        await Task.CompletedTask;
    }

    private void UpdateAutoSize()
    {
        if (Size.Equals(ConsoleSize.Empty))
        {
            var maxWidth = _headers.Count > 0 ? _headers.Sum(h => h.Length + 2) : 10;
            var totalHeight = _filteredRows.Count + (_headers.Count > 0 ? 2 : 0) + (Style.ShowBorders ? 2 : 0);

            Size = new ConsoleSize(Math.Max(maxWidth, 20), Math.Max(totalHeight, 3));
        }
    }
}