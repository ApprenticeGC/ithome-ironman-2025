namespace GameConsole.UI.Console;

/// <summary>
/// Represents a column in a console table.
/// </summary>
public class ConsoleTableColumn
{
    /// <summary>
    /// Initializes a new table column.
    /// </summary>
    /// <param name="header">Column header text.</param>
    /// <param name="propertyName">Property name for data binding.</param>
    /// <param name="width">Column width (0 for auto-size).</param>
    /// <param name="alignment">Column alignment.</param>
    public ConsoleTableColumn(string header, string propertyName, int width = 0, 
                             LayoutAlignment alignment = LayoutAlignment.Start)
    {
        Header = header ?? throw new ArgumentNullException(nameof(header));
        PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
        Width = Math.Max(0, width);
        Alignment = alignment;
        IsSortable = true;
    }
    
    /// <summary>
    /// Gets the column header text.
    /// </summary>
    public string Header { get; }
    
    /// <summary>
    /// Gets the property name for data binding.
    /// </summary>
    public string PropertyName { get; }
    
    /// <summary>
    /// Gets the column width (0 for auto-size).
    /// </summary>
    public int Width { get; set; }
    
    /// <summary>
    /// Gets the column alignment.
    /// </summary>
    public LayoutAlignment Alignment { get; }
    
    /// <summary>
    /// Gets or sets whether this column is sortable.
    /// </summary>
    public bool IsSortable { get; set; }
    
    /// <summary>
    /// Gets or sets the custom formatter function for this column.
    /// </summary>
    public Func<object?, string>? Formatter { get; set; }
}

/// <summary>
/// Represents sorting information for a table.
/// </summary>
/// <param name="ColumnName">The column to sort by.</param>
/// <param name="Ascending">True for ascending, false for descending.</param>
public readonly record struct TableSortInfo(string ColumnName, bool Ascending);

/// <summary>
/// Console table with sorting and filtering capabilities.
/// </summary>
/// <typeparam name="T">The data type for table rows.</typeparam>
public class ConsoleTable<T> : ILayout where T : class
{
    private readonly List<ConsoleTableColumn> _columns = [];
    private readonly List<T> _data = [];
    private readonly List<T> _filteredData = [];
    private readonly string _title;
    private readonly string _headerColor;
    private readonly string _dataColor;
    private readonly string _borderColor;
    private readonly LayoutSpacing _padding;
    private TableSortInfo? _currentSort;
    private Func<T, bool>? _filter;
    private bool _showBorders = true;
    
    /// <summary>
    /// Initializes a new console table.
    /// </summary>
    /// <param name="title">Optional table title.</param>
    /// <param name="headerColor">Header row color.</param>
    /// <param name="dataColor">Data rows color.</param>
    /// <param name="borderColor">Border color.</param>
    /// <param name="padding">Table padding.</param>
    public ConsoleTable(string title = "", string headerColor = ANSIEscapeSequences.FgBrightWhite,
                        string dataColor = "", string borderColor = ANSIEscapeSequences.FgBrightBlack,
                        LayoutSpacing padding = default)
    {
        _title = title ?? string.Empty;
        _headerColor = headerColor;
        _dataColor = dataColor;
        _borderColor = borderColor;
        _padding = padding;
        UpdateFilteredData();
    }
    
    /// <summary>
    /// Gets the table columns.
    /// </summary>
    public IReadOnlyList<ConsoleTableColumn> Columns => _columns.AsReadOnly();
    
    /// <summary>
    /// Gets the table data.
    /// </summary>
    public IReadOnlyList<T> Data => _data.AsReadOnly();
    
    /// <summary>
    /// Gets the filtered data.
    /// </summary>
    public IReadOnlyList<T> FilteredData => _filteredData.AsReadOnly();
    
    /// <summary>
    /// Gets or sets whether to show table borders.
    /// </summary>
    public bool ShowBorders
    {
        get => _showBorders;
        set => _showBorders = value;
    }
    
    /// <summary>
    /// Gets the current sort information.
    /// </summary>
    public TableSortInfo? CurrentSort => _currentSort;
    
    /// <summary>
    /// Adds a column to the table.
    /// </summary>
    /// <param name="column">The column to add.</param>
    public void AddColumn(ConsoleTableColumn column)
    {
        if (column != null)
            _columns.Add(column);
    }
    
    /// <summary>
    /// Adds a column with the specified parameters.
    /// </summary>
    /// <param name="header">Column header text.</param>
    /// <param name="propertyName">Property name for data binding.</param>
    /// <param name="width">Column width (0 for auto-size).</param>
    /// <param name="alignment">Column alignment.</param>
    /// <returns>The created column.</returns>
    public ConsoleTableColumn AddColumn(string header, string propertyName, int width = 0, 
                                       LayoutAlignment alignment = LayoutAlignment.Start)
    {
        var column = new ConsoleTableColumn(header, propertyName, width, alignment);
        AddColumn(column);
        return column;
    }
    
    /// <summary>
    /// Sets the table data.
    /// </summary>
    /// <param name="data">The data to display.</param>
    public void SetData(IEnumerable<T> data)
    {
        _data.Clear();
        if (data != null)
            _data.AddRange(data);
        UpdateFilteredData();
    }
    
    /// <summary>
    /// Adds data to the table.
    /// </summary>
    /// <param name="items">The items to add.</param>
    public void AddData(params T[] items)
    {
        if (items != null)
        {
            _data.AddRange(items);
            UpdateFilteredData();
        }
    }
    
    /// <summary>
    /// Clears all table data.
    /// </summary>
    public void ClearData()
    {
        _data.Clear();
        UpdateFilteredData();
    }
    
    /// <summary>
    /// Sets the filter function.
    /// </summary>
    /// <param name="filter">The filter function, or null to remove filtering.</param>
    public void SetFilter(Func<T, bool>? filter)
    {
        _filter = filter;
        UpdateFilteredData();
    }
    
    /// <summary>
    /// Sorts the table by the specified column.
    /// </summary>
    /// <param name="columnName">The column name to sort by.</param>
    /// <param name="ascending">True for ascending, false for descending.</param>
    public void SortBy(string columnName, bool ascending = true)
    {
        var column = _columns.FirstOrDefault(c => c.PropertyName == columnName);
        if (column?.IsSortable == true)
        {
            _currentSort = new TableSortInfo(columnName, ascending);
            UpdateFilteredData();
        }
    }
    
    /// <summary>
    /// Toggles the sort order for the specified column.
    /// </summary>
    /// <param name="columnName">The column name to sort by.</param>
    public void ToggleSort(string columnName)
    {
        var ascending = _currentSort?.ColumnName != columnName || !_currentSort.Value.Ascending;
        SortBy(columnName, ascending);
    }
    
    /// <summary>
    /// Clears the current sort.
    /// </summary>
    public void ClearSort()
    {
        _currentSort = null;
        UpdateFilteredData();
    }
    
    /// <inheritdoc />
    public ConsoleSize GetDesiredSize(ConsoleSize availableSize)
    {
        var contentWidth = CalculateTableWidth(availableSize.Width);
        var contentHeight = CalculateTableHeight();
        
        if (!string.IsNullOrEmpty(_title))
            contentHeight += 2; // Title + separator
        
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
        if (_columns.Count == 0) return;
        
        var contentRect = new ConsoleRect(
            bounds.X + _padding.Left,
            bounds.Y + _padding.Top,
            Math.Max(0, bounds.Width - _padding.TotalHorizontal),
            Math.Max(0, bounds.Height - _padding.TotalVertical));
        
        var y = contentRect.Y;
        
        // Render title
        if (!string.IsNullOrEmpty(_title) && y < contentRect.Bottom)
        {
            buffer.SetText(contentRect.X, y, _title, _headerColor);
            y++;
            
            if (y < contentRect.Bottom)
            {
                var separator = new string('═', Math.Min(_title.Length, contentRect.Width));
                buffer.SetText(contentRect.X, y, separator, _borderColor);
                y++;
            }
        }
        
        // Calculate column widths
        var columnWidths = CalculateColumnWidths(contentRect.Width);
        if (columnWidths.Sum() == 0) return;
        
        // Render table border (top)
        if (_showBorders && y < contentRect.Bottom)
        {
            RenderHorizontalBorder(buffer, contentRect.X, y, columnWidths, true);
            y++;
        }
        
        // Render header
        if (y < contentRect.Bottom)
        {
            RenderRow(buffer, contentRect.X, y, columnWidths, 
                     _columns.Select(c => GetHeaderDisplayText(c)).ToArray(), _headerColor);
            y++;
        }
        
        // Render header separator
        if (_showBorders && y < contentRect.Bottom)
        {
            RenderHorizontalBorder(buffer, contentRect.X, y, columnWidths, false);
            y++;
        }
        
        // Render data rows
        var visibleRows = Math.Min(_filteredData.Count, contentRect.Bottom - y - (_showBorders ? 1 : 0));
        for (var i = 0; i < visibleRows; i++)
        {
            var rowData = _filteredData[i];
            var cellValues = _columns.Select(c => GetCellValue(rowData, c)).ToArray();
            
            RenderRow(buffer, contentRect.X, y, columnWidths, cellValues, _dataColor);
            y++;
        }
        
        // Render table border (bottom)
        if (_showBorders && y < contentRect.Bottom)
        {
            RenderHorizontalBorder(buffer, contentRect.X, y, columnWidths, true);
        }
    }
    
    private int CalculateTableWidth(int availableWidth)
    {
        var fixedWidth = _columns.Where(c => c.Width > 0).Sum(c => c.Width);
        var autoColumns = _columns.Count(c => c.Width == 0);
        var borderWidth = _showBorders ? _columns.Count + 1 : 0;
        
        if (autoColumns > 0)
        {
            var remainingWidth = Math.Max(0, availableWidth - fixedWidth - borderWidth);
            var autoColumnWidth = remainingWidth / autoColumns;
            return fixedWidth + autoColumns * autoColumnWidth + borderWidth;
        }
        
        return fixedWidth + borderWidth;
    }
    
    private int CalculateTableHeight()
    {
        var height = 1; // Header row
        height += _filteredData.Count; // Data rows
        
        if (_showBorders)
            height += 2; // Top and bottom borders + header separator
        
        return height;
    }
    
    private int[] CalculateColumnWidths(int availableWidth)
    {
        if (_columns.Count == 0) return [];
        
        var widths = new int[_columns.Count];
        var borderWidth = _showBorders ? _columns.Count + 1 : 0;
        var contentWidth = Math.Max(0, availableWidth - borderWidth);
        
        // Set fixed widths
        var remainingWidth = contentWidth;
        var autoColumns = new List<int>();
        
        for (var i = 0; i < _columns.Count; i++)
        {
            if (_columns[i].Width > 0)
            {
                widths[i] = Math.Min(_columns[i].Width, remainingWidth);
                remainingWidth -= widths[i];
            }
            else
            {
                autoColumns.Add(i);
            }
        }
        
        // Distribute remaining width among auto-sized columns
        if (autoColumns.Count > 0 && remainingWidth > 0)
        {
            var autoWidth = remainingWidth / autoColumns.Count;
            foreach (var colIndex in autoColumns)
            {
                widths[colIndex] = autoWidth;
            }
        }
        
        return widths;
    }
    
    private void RenderRow(ConsoleBuffer buffer, int x, int y, int[] columnWidths, string[] values, string color)
    {
        var currentX = x;
        
        if (_showBorders)
        {
            buffer.SetText(currentX, y, "│", _borderColor);
            currentX++;
        }
        
        for (var i = 0; i < Math.Min(_columns.Count, values.Length); i++)
        {
            var width = columnWidths[i];
            if (width > 0)
            {
                var value = values[i] ?? "";
                var alignment = _columns[i].Alignment;
                
                // Truncate and align text
                if (value.Length > width)
                    value = value[..width];
                else if (alignment == LayoutAlignment.Center)
                    value = value.PadLeft((width + value.Length) / 2).PadRight(width);
                else if (alignment == LayoutAlignment.End)
                    value = value.PadLeft(width);
                else
                    value = value.PadRight(width);
                
                buffer.SetText(currentX, y, value, color);
                currentX += width;
                
                if (_showBorders && i < _columns.Count - 1)
                {
                    buffer.SetText(currentX, y, "│", _borderColor);
                    currentX++;
                }
            }
        }
        
        if (_showBorders)
        {
            buffer.SetText(currentX, y, "│", _borderColor);
        }
    }
    
    private void RenderHorizontalBorder(ConsoleBuffer buffer, int x, int y, int[] columnWidths, bool isMainBorder)
    {
        var currentX = x;
        var lineChar = isMainBorder ? '─' : '─';
        var cornerChar = isMainBorder ? '┼' : '┼';
        var leftChar = isMainBorder ? '├' : '├';
        var rightChar = isMainBorder ? '┤' : '┤';
        
        if (y == 0 || !_showBorders) // Top border
        {
            leftChar = '┌';
            rightChar = '┐';
            cornerChar = '┬';
        }
        
        buffer.SetText(currentX, y, leftChar.ToString(), _borderColor);
        currentX++;
        
        for (var i = 0; i < columnWidths.Length; i++)
        {
            var width = columnWidths[i];
            var line = new string(lineChar, width);
            buffer.SetText(currentX, y, line, _borderColor);
            currentX += width;
            
            if (i < columnWidths.Length - 1)
            {
                buffer.SetText(currentX, y, cornerChar.ToString(), _borderColor);
                currentX++;
            }
        }
        
        buffer.SetText(currentX, y, rightChar.ToString(), _borderColor);
    }
    
    private string GetHeaderDisplayText(ConsoleTableColumn column)
    {
        var header = column.Header;
        if (_currentSort?.ColumnName == column.PropertyName)
        {
            header += _currentSort.Value.Ascending ? " ↑" : " ↓";
        }
        return header;
    }
    
    private string GetCellValue(T item, ConsoleTableColumn column)
    {
        try
        {
            var property = typeof(T).GetProperty(column.PropertyName);
            var value = property?.GetValue(item);
            
            if (column.Formatter != null)
                return column.Formatter(value);
            
            return value?.ToString() ?? "";
        }
        catch
        {
            return "";
        }
    }
    
    private void UpdateFilteredData()
    {
        _filteredData.Clear();
        var data = _filter == null ? _data : _data.Where(_filter);
        
        if (_currentSort.HasValue)
        {
            var sortInfo = _currentSort.Value;
            var property = typeof(T).GetProperty(sortInfo.ColumnName);
            if (property != null)
            {
                data = sortInfo.Ascending
                    ? data.OrderBy(x => property.GetValue(x))
                    : data.OrderByDescending(x => property.GetValue(x));
            }
        }
        
        _filteredData.AddRange(data);
    }
}