namespace GameConsole.UI.Console;

/// <summary>
/// Represents the style of a progress bar.
/// </summary>
public enum ProgressBarStyle
{
    /// <summary>Block characters (█).</summary>
    Block,
    /// <summary>Hash characters (#).</summary>
    Hash,
    /// <summary>Equal characters (=).</summary>
    Equal,
    /// <summary>Unicode box drawing characters (▓▒░).</summary>
    Shaded
}

/// <summary>
/// Console progress bar with customizable display options.
/// </summary>
public class ConsoleProgressBar : ILayout
{
    private double _value;
    private readonly string _label;
    private readonly ProgressBarStyle _style;
    private readonly string _fillColor;
    private readonly string _emptyColor;
    private readonly string _textColor;
    private readonly bool _showPercentage;
    private readonly bool _showValue;
    private readonly LayoutSpacing _padding;
    
    /// <summary>
    /// Initializes a new console progress bar.
    /// </summary>
    /// <param name="label">Optional label text.</param>
    /// <param name="style">Progress bar style.</param>
    /// <param name="fillColor">Color for filled portion.</param>
    /// <param name="emptyColor">Color for empty portion.</param>
    /// <param name="textColor">Color for text.</param>
    /// <param name="showPercentage">Whether to show percentage.</param>
    /// <param name="showValue">Whether to show numeric value.</param>
    /// <param name="padding">Progress bar padding.</param>
    public ConsoleProgressBar(string label = "", ProgressBarStyle style = ProgressBarStyle.Block,
                             string fillColor = ANSIEscapeSequences.FgBrightGreen,
                             string emptyColor = ANSIEscapeSequences.FgBrightBlack,
                             string textColor = "",
                             bool showPercentage = true, bool showValue = false,
                             LayoutSpacing padding = default)
    {
        _label = label ?? string.Empty;
        _style = style;
        _fillColor = fillColor;
        _emptyColor = emptyColor;
        _textColor = textColor;
        _showPercentage = showPercentage;
        _showValue = showValue;
        _padding = padding;
        _value = 0.0;
    }
    
    /// <summary>
    /// Gets or sets the progress value (0.0 to 1.0).
    /// </summary>
    public double Value
    {
        get => _value;
        set => _value = Math.Clamp(value, 0.0, 1.0);
    }
    
    /// <summary>
    /// Gets or sets the minimum value for the progress range.
    /// </summary>
    public double MinValue { get; set; } = 0.0;
    
    /// <summary>
    /// Gets or sets the maximum value for the progress range.
    /// </summary>
    public double MaxValue { get; set; } = 100.0;
    
    /// <summary>
    /// Gets or sets the current numeric value within the range.
    /// </summary>
    public double NumericValue
    {
        get => MinValue + (_value * (MaxValue - MinValue));
        set => Value = (value - MinValue) / (MaxValue - MinValue);
    }
    
    /// <summary>
    /// Gets the percentage value (0 to 100).
    /// </summary>
    public double Percentage => _value * 100.0;
    
    /// <summary>
    /// Sets the progress value as a percentage.
    /// </summary>
    /// <param name="percentage">The percentage value (0 to 100).</param>
    public void SetPercentage(double percentage)
    {
        Value = percentage / 100.0;
    }
    
    /// <summary>
    /// Increments the progress value.
    /// </summary>
    /// <param name="amount">The amount to increment (0.0 to 1.0).</param>
    public void Increment(double amount = 0.01)
    {
        Value += amount;
    }
    
    /// <summary>
    /// Resets the progress to zero.
    /// </summary>
    public void Reset()
    {
        Value = 0.0;
    }
    
    /// <summary>
    /// Completes the progress (sets to 100%).
    /// </summary>
    public void Complete()
    {
        Value = 1.0;
    }
    
    /// <inheritdoc />
    public ConsoleSize GetDesiredSize(ConsoleSize availableSize)
    {
        var height = 1;
        if (!string.IsNullOrEmpty(_label))
            height += 1;
        
        var width = Math.Min(40, availableSize.Width); // Default width
        if (_showPercentage || _showValue)
            width += 10; // Extra space for percentage/value display
        
        return new ConsoleSize(
            Math.Min(width + _padding.TotalHorizontal, availableSize.Width),
            Math.Min(height + _padding.TotalVertical, availableSize.Height));
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
        
        // Render label
        if (!string.IsNullOrEmpty(_label) && y < contentRect.Bottom)
        {
            buffer.SetText(contentRect.X, y, _label, _textColor);
            y++;
        }
        
        // Render progress bar
        if (y < contentRect.Bottom && contentRect.Width > 0)
        {
            RenderProgressBar(buffer, contentRect.X, y, contentRect.Width);
        }
    }
    
    private void RenderProgressBar(ConsoleBuffer buffer, int x, int y, int width)
    {
        // Reserve space for percentage/value display
        var displayText = GetDisplayText();
        var barWidth = width;
        if (!string.IsNullOrEmpty(displayText))
        {
            barWidth = Math.Max(5, width - displayText.Length - 1);
        }
        
        // Calculate filled width
        var filledWidth = (int)Math.Round(_value * barWidth);
        
        // Get style characters
        var (fillChar, emptyChar) = GetStyleCharacters();
        
        // Render filled portion
        for (var i = 0; i < filledWidth; i++)
        {
            buffer.SetText(x + i, y, fillChar.ToString(), _fillColor);
        }
        
        // Render empty portion
        for (var i = filledWidth; i < barWidth; i++)
        {
            buffer.SetText(x + i, y, emptyChar.ToString(), _emptyColor);
        }
        
        // Render display text
        if (!string.IsNullOrEmpty(displayText) && barWidth < width)
        {
            buffer.SetText(x + barWidth + 1, y, displayText, _textColor);
        }
    }
    
    private (char fillChar, char emptyChar) GetStyleCharacters()
    {
        return _style switch
        {
            ProgressBarStyle.Block => ('█', '░'),
            ProgressBarStyle.Hash => ('#', '-'),
            ProgressBarStyle.Equal => ('=', '-'),
            ProgressBarStyle.Shaded => ('▓', '░'),
            _ => ('█', '░')
        };
    }
    
    private string GetDisplayText()
    {
        var parts = new List<string>();
        
        if (_showPercentage)
            parts.Add($"{Percentage:F0}%");
        
        if (_showValue)
            parts.Add($"{NumericValue:F0}/{MaxValue:F0}");
        
        return string.Join(" ", parts);
    }
}

/// <summary>
/// Multi-progress bar for displaying multiple progress indicators.
/// </summary>
public class ConsoleMultiProgressBar : ILayout
{
    private readonly List<(string label, ConsoleProgressBar progressBar)> _bars = [];
    private readonly string _title;
    private readonly LayoutSpacing _padding;
    private readonly int _spacing;
    
    /// <summary>
    /// Initializes a new multi-progress bar.
    /// </summary>
    /// <param name="title">Optional title.</param>
    /// <param name="spacing">Spacing between progress bars.</param>
    /// <param name="padding">Overall padding.</param>
    public ConsoleMultiProgressBar(string title = "", int spacing = 0, LayoutSpacing padding = default)
    {
        _title = title ?? string.Empty;
        _spacing = Math.Max(0, spacing);
        _padding = padding;
    }
    
    /// <summary>
    /// Gets the progress bars.
    /// </summary>
    public IReadOnlyList<(string label, ConsoleProgressBar progressBar)> ProgressBars => _bars.AsReadOnly();
    
    /// <summary>
    /// Adds a progress bar.
    /// </summary>
    /// <param name="label">Label for the progress bar.</param>
    /// <param name="progressBar">The progress bar instance.</param>
    public void AddProgressBar(string label, ConsoleProgressBar progressBar)
    {
        if (!string.IsNullOrEmpty(label) && progressBar != null)
            _bars.Add((label, progressBar));
    }
    
    /// <summary>
    /// Creates and adds a new progress bar.
    /// </summary>
    /// <param name="label">Label for the progress bar.</param>
    /// <param name="style">Progress bar style.</param>
    /// <param name="fillColor">Fill color.</param>
    /// <param name="emptyColor">Empty color.</param>
    /// <param name="showPercentage">Whether to show percentage.</param>
    /// <returns>The created progress bar.</returns>
    public ConsoleProgressBar AddProgressBar(string label, ProgressBarStyle style = ProgressBarStyle.Block,
                                           string fillColor = ANSIEscapeSequences.FgBrightGreen,
                                           string emptyColor = ANSIEscapeSequences.FgBrightBlack,
                                           bool showPercentage = true)
    {
        var progressBar = new ConsoleProgressBar("", style, fillColor, emptyColor, "", showPercentage);
        AddProgressBar(label, progressBar);
        return progressBar;
    }
    
    /// <summary>
    /// Removes a progress bar by label.
    /// </summary>
    /// <param name="label">The label of the progress bar to remove.</param>
    /// <returns>True if removed; otherwise, false.</returns>
    public bool RemoveProgressBar(string label)
    {
        for (var i = 0; i < _bars.Count; i++)
        {
            if (_bars[i].label == label)
            {
                _bars.RemoveAt(i);
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Clears all progress bars.
    /// </summary>
    public void ClearProgressBars()
    {
        _bars.Clear();
    }
    
    /// <summary>
    /// Gets a progress bar by label.
    /// </summary>
    /// <param name="label">The label to find.</param>
    /// <returns>The progress bar if found; otherwise, null.</returns>
    public ConsoleProgressBar? GetProgressBar(string label)
    {
        return _bars.FirstOrDefault(b => b.label == label).progressBar;
    }
    
    /// <inheritdoc />
    public ConsoleSize GetDesiredSize(ConsoleSize availableSize)
    {
        var height = 0;
        var width = 0;
        
        if (!string.IsNullOrEmpty(_title))
            height += 2; // Title + separator
        
        foreach (var (label, progressBar) in _bars)
        {
            height += 1; // Progress bar
            if (_spacing > 0)
                height += _spacing;
            
            var labelWidth = label.Length + 2; // "Label: "
            var barSize = progressBar.GetDesiredSize(availableSize);
            width = Math.Max(width, labelWidth + barSize.Width);
        }
        
        if (_bars.Count > 0 && _spacing > 0)
            height -= _spacing; // Remove extra spacing after last bar
        
        return new ConsoleSize(
            Math.Min(width + _padding.TotalHorizontal, availableSize.Width),
            Math.Min(height + _padding.TotalVertical, availableSize.Height));
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
            buffer.SetText(contentRect.X, y, _title, ANSIEscapeSequences.FgBrightWhite);
            y++;
            
            if (y < contentRect.Bottom)
            {
                var separator = new string('─', Math.Min(_title.Length, contentRect.Width));
                buffer.SetText(contentRect.X, y, separator, ANSIEscapeSequences.FgBrightBlack);
                y++;
            }
        }
        
        // Render progress bars
        foreach (var (label, progressBar) in _bars)
        {
            if (y >= contentRect.Bottom) break;
            
            // Render label
            var labelText = $"{label}: ";
            buffer.SetText(contentRect.X, y, labelText);
            
            // Render progress bar
            var barX = contentRect.X + labelText.Length;
            var barWidth = Math.Max(1, contentRect.Width - labelText.Length);
            var barBounds = new ConsoleRect(barX, y, barWidth, 1);
            
            progressBar.Render(buffer, barBounds);
            
            y += 1 + _spacing;
        }
    }
}