using GameConsole.Input.Core;
using System.Text.Json;

namespace GameConsole.UI.Console;

/// <summary>
/// Data table component with sorting and filtering capabilities.
/// </summary>
public class ConsoleTable : BaseUIComponent
{
    private readonly List<TableColumn> _columns = new();
    private readonly List<object[]> _rows = new();
    private readonly List<int> _filteredRows = new();
    private int _sortColumnIndex = -1;
    private bool _sortAscending = true;
    private int _scrollOffset = 0;
    private string _filterText = string.Empty;
    private bool _showHeaders = true;
    private bool _showBorder = true;
    
    public ConsoleTable(string id) : base(id)
    {
        CanFocus = true;
    }
    
    /// <summary>
    /// Gets or sets whether to show column headers.
    /// </summary>
    public bool ShowHeaders 
    { 
        get => _showHeaders; 
        set => _showHeaders = value; 
    }
    
    /// <summary>
    /// Gets or sets whether to show a border around the table.
    /// </summary>
    public bool ShowBorder 
    { 
        get => _showBorder; 
        set => _showBorder = value; 
    }
    
    /// <summary>
    /// Gets the current filter text.
    /// </summary>
    public string FilterText => _filterText;
    
    /// <summary>
    /// Gets the columns in the table.
    /// </summary>
    public IReadOnlyList<TableColumn> Columns => _columns.AsReadOnly();
    
    /// <summary>
    /// Gets the total number of rows in the table.
    /// </summary>
    public int RowCount => _rows.Count;
    
    /// <summary>
    /// Gets the number of filtered/visible rows.
    /// </summary>
    public int FilteredRowCount => _filteredRows.Count;
    
    /// <summary>
    /// Adds a column to the table.
    /// </summary>
    /// <param name="header">Column header text.</param>
    /// <param name="width">Column width (-1 for auto-size).</param>
    /// <param name="alignment">Text alignment.</param>
    /// <returns>The created column.</returns>
    public TableColumn AddColumn(string header, int width = -1, TextAlignment alignment = TextAlignment.Left)
    {
        var column = new TableColumn(header, width, alignment);
        _columns.Add(column);
        return column;
    }
    
    /// <summary>
    /// Adds a row to the table.
    /// </summary>
    /// <param name="values">Column values for the row.</param>
    public void AddRow(params object[] values)
    {
        if (values.Length != _columns.Count)
            throw new ArgumentException($"Row must have {_columns.Count} values to match column count");
            
        _rows.Add(values);
        UpdateFilter();
    }
    
    /// <summary>
    /// Clears all rows from the table.
    /// </summary>
    public void ClearRows()
    {
        _rows.Clear();
        _filteredRows.Clear();
        _scrollOffset = 0;
    }
    
    /// <summary>
    /// Sets a filter for the table data.
    /// </summary>
    /// <param name="filterText">Text to filter by (searches all columns).</param>
    public void SetFilter(string filterText)
    {
        _filterText = filterText ?? string.Empty;
        UpdateFilter();
        _scrollOffset = 0;
    }
    
    /// <summary>
    /// Sorts the table by the specified column.
    /// </summary>
    /// <param name="columnIndex">Index of the column to sort by.</param>
    /// <param name="ascending">True for ascending sort; false for descending.</param>
    public void SortByColumn(int columnIndex, bool ascending = true)
    {
        if (columnIndex < 0 || columnIndex >= _columns.Count) return;
        
        _sortColumnIndex = columnIndex;
        _sortAscending = ascending;
        UpdateFilter();
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
        
        if (_columns.Count == 0 || contentHeight < 1) return;
        
        // Calculate column widths
        var columnWidths = CalculateColumnWidths(contentWidth);
        int currentY = contentY;
        
        // Draw headers
        if (_showHeaders && currentY < contentY + contentHeight)
        {
            DrawHeaders(framework, contentX, currentY, columnWidths);
            currentY += 2; // Header + separator
            contentHeight -= 2;
        }
        
        // Draw data rows
        int visibleRows = Math.Max(0, contentHeight);
        int maxScroll = Math.Max(0, _filteredRows.Count - visibleRows);
        _scrollOffset = Math.Min(_scrollOffset, maxScroll);
        
        for (int i = 0; i < visibleRows && i + _scrollOffset < _filteredRows.Count; i++)
        {
            if (currentY + i >= contentY + contentHeight) break;
            
            var rowIndex = _filteredRows[i + _scrollOffset];
            var row = _rows[rowIndex];
            
            DrawRow(framework, contentX, currentY + i, columnWidths, row);
        }
        
        // Draw scroll indicators
        if (_scrollOffset > 0)
        {
            framework.WriteAt(contentX + contentWidth - 1, contentY, "▲", ConsoleColor.Yellow);
        }
        if (_scrollOffset < maxScroll)
        {
            framework.WriteAt(contentX + contentWidth - 1, contentY + contentHeight - 1, "▼", ConsoleColor.Yellow);
        }
        
        // Show status information
        if (!string.IsNullOrEmpty(_filterText))
        {
            var statusText = $"Filter: {_filterText} ({_filteredRows.Count}/{_rows.Count})";
            if (statusText.Length <= contentWidth)
            {
                framework.WriteAt(contentX, contentY + contentHeight - 1, statusText, ConsoleColor.Yellow);
            }
        }
    }
    
    protected override bool OnHandleInput(UIKeyEventArgs input)
    {
        switch (input.Key)
        {
            case KeyCode.UpArrow:
                _scrollOffset = Math.Max(0, _scrollOffset - 1);
                return true;
                
            case KeyCode.DownArrow:
                var maxScroll = Math.Max(0, _filteredRows.Count - GetVisibleRowCount());
                _scrollOffset = Math.Min(_scrollOffset + 1, maxScroll);
                return true;
                
            case KeyCode.PageUp:
                _scrollOffset = Math.Max(0, _scrollOffset - GetVisibleRowCount());
                return true;
                
            case KeyCode.PageDown:
                var maxScrollPg = Math.Max(0, _filteredRows.Count - GetVisibleRowCount());
                _scrollOffset = Math.Min(_scrollOffset + GetVisibleRowCount(), maxScrollPg);
                return true;
                
            case KeyCode.Home:
                _scrollOffset = 0;
                return true;
                
            case KeyCode.End:
                _scrollOffset = Math.Max(0, _filteredRows.Count - GetVisibleRowCount());
                return true;
                
            default:
                return false;
        }
    }
    
    private int[] CalculateColumnWidths(int totalWidth)
    {
        if (_columns.Count == 0) return Array.Empty<int>();
        
        var widths = new int[_columns.Count];
        int usedWidth = 0;
        int autoSizeColumns = 0;
        
        // First pass: assign fixed widths
        for (int i = 0; i < _columns.Count; i++)
        {
            if (_columns[i].Width > 0)
            {
                widths[i] = Math.Min(_columns[i].Width, totalWidth);
                usedWidth += widths[i];
            }
            else
            {
                autoSizeColumns++;
            }
        }
        
        // Second pass: distribute remaining width to auto-size columns
        if (autoSizeColumns > 0)
        {
            int remainingWidth = Math.Max(0, totalWidth - usedWidth);
            int widthPerColumn = Math.Max(1, remainingWidth / autoSizeColumns);
            
            for (int i = 0; i < _columns.Count; i++)
            {
                if (_columns[i].Width <= 0)
                {
                    widths[i] = widthPerColumn;
                }
            }
        }
        
        return widths;
    }
    
    private void DrawHeaders(IConsoleUIFramework framework, int x, int y, int[] columnWidths)
    {
        int currentX = x;
        
        for (int i = 0; i < _columns.Count && i < columnWidths.Length; i++)
        {
            var column = _columns[i];
            var sortIndicator = "";
            
            if (_sortColumnIndex == i)
            {
                sortIndicator = _sortAscending ? " ▲" : " ▼";
            }
            
            var headerText = column.Header + sortIndicator;
            var displayText = TruncateText(headerText, columnWidths[i]);
            
            framework.WriteAt(currentX, y, displayText, ConsoleColor.White, null, TextStyle.Bold);
            currentX += columnWidths[i];
        }
        
        // Draw separator line
        var separatorLine = new string('─', Math.Min(totalWidth: x + columnWidths.Sum() - x, currentX - x));
        framework.WriteAt(x, y + 1, separatorLine, ConsoleColor.Gray);
    }
    
    private void DrawRow(IConsoleUIFramework framework, int x, int y, int[] columnWidths, object[] row)
    {
        int currentX = x;
        
        for (int i = 0; i < _columns.Count && i < columnWidths.Length && i < row.Length; i++)
        {
            var value = row[i]?.ToString() ?? "";
            var displayText = AlignText(TruncateText(value, columnWidths[i]), columnWidths[i], _columns[i].Alignment);
            
            framework.WriteAt(currentX, y, displayText, ConsoleColor.White);
            currentX += columnWidths[i];
        }
    }
    
    private void UpdateFilter()
    {
        _filteredRows.Clear();
        
        for (int i = 0; i < _rows.Count; i++)
        {
            if (MatchesFilter(_rows[i]))
            {
                _filteredRows.Add(i);
            }
        }
        
        // Sort filtered results
        if (_sortColumnIndex >= 0 && _sortColumnIndex < _columns.Count)
        {
            _filteredRows.Sort((a, b) =>
            {
                var valueA = _rows[a][_sortColumnIndex];
                var valueB = _rows[b][_sortColumnIndex];
                
                int comparison = Comparer.Default.Compare(valueA, valueB);
                return _sortAscending ? comparison : -comparison;
            });
        }
    }
    
    private bool MatchesFilter(object[] row)
    {
        if (string.IsNullOrEmpty(_filterText)) return true;
        
        return row.Any(value => value?.ToString()?.Contains(_filterText, StringComparison.OrdinalIgnoreCase) == true);
    }
    
    private int GetVisibleRowCount()
    {
        int contentHeight = ActualBounds.Height;
        if (_showBorder) contentHeight -= 2;
        if (_showHeaders) contentHeight -= 2;
        return Math.Max(1, contentHeight);
    }
    
    private static string TruncateText(string text, int maxLength)
    {
        if (text.Length <= maxLength) return text;
        return maxLength > 3 ? text[..(maxLength - 3)] + "..." : text[..maxLength];
    }
    
    private static string AlignText(string text, int width, TextAlignment alignment)
    {
        if (text.Length >= width) return text;
        
        return alignment switch
        {
            TextAlignment.Center => text.PadLeft((width + text.Length) / 2).PadRight(width),
            TextAlignment.Right => text.PadLeft(width),
            _ => text.PadRight(width)
        };
    }
}

/// <summary>
/// Represents a table column.
/// </summary>
public class TableColumn
{
    public string Header { get; set; }
    public int Width { get; set; }
    public TextAlignment Alignment { get; set; }
    
    public TableColumn(string header, int width = -1, TextAlignment alignment = TextAlignment.Left)
    {
        Header = header ?? string.Empty;
        Width = width;
        Alignment = alignment;
    }
}

/// <summary>
/// Text alignment options.
/// </summary>
public enum TextAlignment
{
    Left,
    Center,
    Right
}