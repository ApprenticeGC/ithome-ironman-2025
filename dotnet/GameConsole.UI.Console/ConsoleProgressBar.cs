namespace GameConsole.UI.Console;

/// <summary>
/// Console progress bar component implementation.
/// </summary>
public class ConsoleProgressBar : ConsoleComponentBase, IConsoleProgressBar
{
    private string _label;
    private int _value;
    private int _maxValue;
    private ConsoleProgressBarStyle _style;
    private bool _showPercentage;
    private bool _isIndeterminate;
    private int _indeterminatePosition;

    /// <inheritdoc/>
    public string Label
    {
        get => _label;
        set => _label = value ?? string.Empty;
    }

    /// <inheritdoc/>
    public int Value
    {
        get => _value;
        set
        {
            var oldValue = _value;
            _value = Math.Max(0, Math.Min(_maxValue, value));
            
            if (oldValue != _value)
            {
                ProgressChanged?.Invoke(this, new ConsoleProgressChangedEventArgs(oldValue, _value, _maxValue));
            }
        }
    }

    /// <inheritdoc/>
    public int MaxValue
    {
        get => _maxValue;
        set => _maxValue = Math.Max(1, value);
    }

    /// <inheritdoc/>
    public double PercentComplete => _maxValue > 0 ? (double)_value / _maxValue : 0.0;

    /// <inheritdoc/>
    public ConsoleProgressBarStyle Style
    {
        get => _style;
        set => _style = value;
    }

    /// <inheritdoc/>
    public bool ShowPercentage
    {
        get => _showPercentage;
        set => _showPercentage = value;
    }

    /// <inheritdoc/>
    public bool IsIndeterminate
    {
        get => _isIndeterminate;
        set => _isIndeterminate = value;
    }

    /// <inheritdoc/>
    public event EventHandler<ConsoleProgressChangedEventArgs>? ProgressChanged;

    /// <summary>
    /// Initializes a new console progress bar.
    /// </summary>
    /// <param name="label">Progress bar label.</param>
    /// <param name="maxValue">Maximum progress value.</param>
    /// <param name="position">Progress bar position.</param>
    /// <param name="size">Progress bar size.</param>
    /// <param name="style">Progress bar style.</param>
    public ConsoleProgressBar(
        string label,
        int maxValue = 100,
        ConsolePosition position = default,
        ConsoleSize size = default,
        ConsoleProgressBarStyle style = default)
        : base(position: position, size: size)
    {
        _label = label ?? string.Empty;
        _value = 0;
        _maxValue = Math.Max(1, maxValue);
        _style = style.Equals(default) ? ConsoleProgressBarStyle.Default : style;
        _showPercentage = true;
        _isIndeterminate = false;
        _indeterminatePosition = 0;

        // Progress bars typically don't receive focus
        CanReceiveFocus = false;

        // Auto-size if not specified
        if (Size.Equals(ConsoleSize.Empty))
        {
            UpdateAutoSize();
        }
    }

    /// <inheritdoc/>
    public void Increment(int increment = 1)
    {
        Value += increment;
    }

    /// <inheritdoc/>
    public void Reset()
    {
        Value = 0;
        _indeterminatePosition = 0;
    }

    /// <inheritdoc/>
    public override async Task RenderAsync(IConsoleBuffer buffer, CancellationToken cancellationToken = default)
    {
        if (!IsVisible || Size.Width <= 0 || Size.Height <= 0)
            return;

        await Task.Run(() =>
        {
            // Clear the progress bar area
            buffer.FillArea(Position.X, Position.Y, Size.Width, Size.Height, ' ', ConsoleTextStyle.Default);

            var contentX = Position.X;
            var contentY = Position.Y;
            var contentWidth = Size.Width;
            var contentHeight = Size.Height;

            // Draw border if enabled
            if (Style.ShowBorder && Size.Width >= 3 && Size.Height >= 3)
            {
                buffer.DrawBorder(Position.X, Position.Y, Size.Width, Size.Height, ConsoleBorderStyle.Single, Style.LabelStyle);
                contentX += 1;
                contentY += 1;
                contentWidth -= 2;
                contentHeight -= 2;
            }

            if (contentWidth <= 0 || contentHeight <= 0)
                return;

            var currentY = contentY;

            // Draw label if there's space and a label exists
            if (!string.IsNullOrEmpty(_label) && contentHeight > 0)
            {
                var labelText = _label.Length > contentWidth ? _label.Substring(0, contentWidth) : _label;
                buffer.WriteAt(contentX, currentY, labelText, Style.LabelStyle);
                currentY += 1;
                contentHeight -= 1;
            }

            // Draw progress bar if there's space
            if (contentHeight > 0 && contentWidth > 0)
            {
                if (_isIndeterminate)
                {
                    DrawIndeterminateProgress(buffer, contentX, currentY, contentWidth);
                }
                else
                {
                    DrawDeterminateProgress(buffer, contentX, currentY, contentWidth);
                }
                currentY += 1;
                contentHeight -= 1;
            }

            // Draw percentage or status text if enabled and there's space
            if (_showPercentage && contentHeight > 0)
            {
                var statusText = _isIndeterminate ? "Working..." : $"{PercentComplete:P0}";
                var centeredText = CenterText(statusText, contentWidth);
                buffer.WriteAt(contentX, currentY, centeredText, Style.LabelStyle);
            }

        }, cancellationToken);
    }

    private void DrawDeterminateProgress(IConsoleBuffer buffer, int x, int y, int width)
    {
        var progressWidth = (int)(width * PercentComplete);
        progressWidth = Math.Max(0, Math.Min(width, progressWidth));

        // Draw completed portion
        for (int i = 0; i < progressWidth; i++)
        {
            buffer.WriteCharAt(x + i, y, Style.CompletedChar, Style.CompletedStyle);
        }

        // Draw remaining portion
        for (int i = progressWidth; i < width; i++)
        {
            buffer.WriteCharAt(x + i, y, Style.RemainingChar, Style.RemainingStyle);
        }
    }

    private void DrawIndeterminateProgress(IConsoleBuffer buffer, int x, int y, int width)
    {
        // Create a moving pattern for indeterminate progress
        var blockSize = Math.Max(1, width / 6);
        
        // Update indeterminate position (this should be called by a timer in real implementation)
        _indeterminatePosition = (_indeterminatePosition + 1) % (width + blockSize);

        // Fill with remaining characters first
        for (int i = 0; i < width; i++)
        {
            buffer.WriteCharAt(x + i, y, Style.RemainingChar, Style.RemainingStyle);
        }

        // Draw the moving block
        var startPos = _indeterminatePosition - blockSize;
        var endPos = _indeterminatePosition;

        for (int i = Math.Max(0, startPos); i < Math.Min(width, endPos); i++)
        {
            buffer.WriteCharAt(x + i, y, Style.CompletedChar, Style.CompletedStyle);
        }
    }

    /// <summary>
    /// Updates the indeterminate position for animation. 
    /// This should be called periodically by the UI framework for smooth animation.
    /// </summary>
    public void UpdateIndeterminateAnimation()
    {
        if (_isIndeterminate)
        {
            var width = Size.Width - (Style.ShowBorder ? 2 : 0);
            var blockSize = Math.Max(1, width / 6);
            _indeterminatePosition = (_indeterminatePosition + 1) % (width + blockSize);
        }
    }

    /// <inheritdoc/>
    protected override Task<bool> HandleKeyPressAsync(Input.Core.KeyCode key, ConsoleInputModifiers modifiers, CancellationToken cancellationToken = default)
    {
        // Progress bars typically don't handle input, but we can provide some basic functionality
        switch (key)
        {
            case Input.Core.KeyCode.R when modifiers.HasFlag(ConsoleInputModifiers.Control):
                // Ctrl+R to reset progress
                Reset();
                return Task.FromResult(true);

            case Input.Core.KeyCode.Space:
                // Space to toggle between determinate and indeterminate
                IsIndeterminate = !IsIndeterminate;
                return Task.FromResult(true);

            default:
                return Task.FromResult(false);
        }
    }

    private void UpdateAutoSize()
    {
        if (Size.Equals(ConsoleSize.Empty))
        {
            var labelWidth = string.IsNullOrEmpty(_label) ? 0 : _label.Length;
            var percentageWidth = _showPercentage ? 8 : 0; // "100.0%" = 6 chars + padding
            var minProgressWidth = 20; // Minimum progress bar width

            var totalWidth = Math.Max(labelWidth, Math.Max(percentageWidth, minProgressWidth));
            totalWidth += Style.ShowBorder ? 4 : 2; // Add padding

            var totalHeight = 1; // Progress bar itself
            if (!string.IsNullOrEmpty(_label)) totalHeight += 1; // Label
            if (_showPercentage) totalHeight += 1; // Percentage text
            totalHeight += Style.ShowBorder ? 2 : 0; // Border

            Size = new ConsoleSize(totalWidth, totalHeight);
        }
    }
}