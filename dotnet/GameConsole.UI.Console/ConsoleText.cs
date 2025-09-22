namespace GameConsole.UI.Console;

/// <summary>
/// Console text component implementation with formatting and alignment.
/// </summary>
public class ConsoleText : ConsoleComponentBase, IConsoleText
{
    private string _text;
    private ConsoleTextStyle _style;
    private bool _wordWrap;
    private ConsoleTextAlignment _alignment;

    /// <inheritdoc/>
    public string Text
    {
        get => _text;
        set
        {
            _text = value ?? string.Empty;
            if (Size.Equals(ConsoleSize.Empty))
            {
                UpdateAutoSize();
            }
        }
    }

    /// <inheritdoc/>
    public ConsoleTextStyle Style
    {
        get => _style;
        set => _style = value;
    }

    /// <inheritdoc/>
    public bool WordWrap
    {
        get => _wordWrap;
        set => _wordWrap = value;
    }

    /// <inheritdoc/>
    public ConsoleTextAlignment Alignment
    {
        get => _alignment;
        set => _alignment = value;
    }

    /// <summary>
    /// Initializes a new console text component.
    /// </summary>
    /// <param name="text">Text content.</param>
    /// <param name="style">Text style.</param>
    /// <param name="position">Text position.</param>
    /// <param name="size">Text size.</param>
    /// <param name="wordWrap">Whether to enable word wrapping.</param>
    /// <param name="alignment">Text alignment.</param>
    public ConsoleText(
        string text,
        ConsoleTextStyle style = default,
        ConsolePosition position = default,
        ConsoleSize size = default,
        bool wordWrap = false,
        ConsoleTextAlignment alignment = ConsoleTextAlignment.Left)
        : base(position: position, size: size)
    {
        _text = text ?? string.Empty;
        _style = style.Equals(default) ? ConsoleTextStyle.Default : style;
        _wordWrap = wordWrap;
        _alignment = alignment;

        // Text components typically don't receive focus unless they're interactive
        CanReceiveFocus = false;

        // Auto-size if not specified
        if (Size.Equals(ConsoleSize.Empty))
        {
            UpdateAutoSize();
        }
    }

    /// <inheritdoc/>
    public override async Task RenderAsync(IConsoleBuffer buffer, CancellationToken cancellationToken = default)
    {
        if (!IsVisible || Size.Width <= 0 || Size.Height <= 0 || string.IsNullOrEmpty(_text))
            return;

        await Task.Run(() =>
        {
            // Clear the text area
            buffer.FillArea(Position.X, Position.Y, Size.Width, Size.Height, ' ', ConsoleTextStyle.Default);

            // Prepare text lines
            var lines = PrepareTextLines();

            // Render each line
            for (int i = 0; i < lines.Count && i < Size.Height; i++)
            {
                var line = lines[i];
                var y = Position.Y + i;

                // Apply alignment
                var alignedLine = ApplyAlignment(line, Size.Width);
                
                // Write the line to the buffer
                buffer.WriteAt(Position.X, y, alignedLine, _style);
            }

        }, cancellationToken);
    }

    /// <summary>
    /// Prepares text lines based on wrapping and size constraints.
    /// </summary>
    /// <returns>List of text lines ready for rendering.</returns>
    private List<string> PrepareTextLines()
    {
        var lines = new List<string>();
        
        if (string.IsNullOrEmpty(_text))
            return lines;

        // Split text into paragraphs (by line breaks)
        var paragraphs = _text.Split(new[] { '\r', '\n' }, StringSplitOptions.None);

        foreach (var paragraph in paragraphs)
        {
            if (_wordWrap && Size.Width > 0)
            {
                // Word wrap the paragraph
                var wrappedLines = WrapText(paragraph, Size.Width);
                lines.AddRange(wrappedLines);
            }
            else
            {
                // No wrapping, just truncate if necessary
                var line = Size.Width > 0 && paragraph.Length > Size.Width 
                    ? paragraph.Substring(0, Size.Width)
                    : paragraph;
                lines.Add(line);
            }
        }

        return lines;
    }

    /// <summary>
    /// Applies text alignment to a line.
    /// </summary>
    /// <param name="line">Line of text to align.</param>
    /// <param name="width">Available width.</param>
    /// <returns>Aligned line of text.</returns>
    private string ApplyAlignment(string line, int width)
    {
        if (string.IsNullOrEmpty(line) || width <= 0)
            return string.Empty;

        // Truncate if too long
        if (line.Length > width)
        {
            line = line.Substring(0, width);
        }

        return _alignment switch
        {
            ConsoleTextAlignment.Center => CenterText(line, width),
            ConsoleTextAlignment.Right => RightAlignText(line, width),
            _ => line.PadRight(width) // Left alignment (default)
        };
    }

    /// <summary>
    /// Updates the auto-size based on text content.
    /// </summary>
    private void UpdateAutoSize()
    {
        if (Size.Equals(ConsoleSize.Empty) && !string.IsNullOrEmpty(_text))
        {
            var lines = _text.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            var maxLineLength = lines.Length > 0 ? lines.Max(line => line?.Length ?? 0) : 0;
            var lineCount = lines.Length;

            Size = new ConsoleSize(Math.Max(1, maxLineLength), Math.Max(1, lineCount));
        }
    }

    /// <inheritdoc/>
    protected override Task<bool> HandleKeyPressAsync(Input.Core.KeyCode key, ConsoleInputModifiers modifiers, CancellationToken cancellationToken = default)
    {
        // Text components don't typically handle input unless they're editable
        // This could be extended to support text selection or editing in the future
        return Task.FromResult(false);
    }
}

/// <summary>
/// Editable console text component with cursor support.
/// </summary>
public class EditableConsoleText : ConsoleText
{
    private int _cursorPosition;
    private bool _isEditing;
    private string _originalText;

    /// <summary>
    /// Gets or sets whether the text is currently being edited.
    /// </summary>
    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            if (_isEditing != value)
            {
                _isEditing = value;
                if (_isEditing)
                {
                    _originalText = Text;
                    CanReceiveFocus = true;
                }
                else
                {
                    CanReceiveFocus = false;
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets the cursor position within the text.
    /// </summary>
    public int CursorPosition
    {
        get => _cursorPosition;
        set => _cursorPosition = Math.Max(0, Math.Min(Text.Length, value));
    }

    /// <summary>
    /// Event raised when text editing is completed.
    /// </summary>
    public event EventHandler<EditingCompletedEventArgs>? EditingCompleted;

    /// <summary>
    /// Initializes a new editable console text component.
    /// </summary>
    public EditableConsoleText(
        string text,
        ConsoleTextStyle style = default,
        ConsolePosition position = default,
        ConsoleSize size = default,
        bool wordWrap = false,
        ConsoleTextAlignment alignment = ConsoleTextAlignment.Left)
        : base(text, style, position, size, wordWrap, alignment)
    {
        _cursorPosition = 0;
        _isEditing = false;
        _originalText = text;
    }

    /// <inheritdoc/>
    public override async Task RenderAsync(IConsoleBuffer buffer, CancellationToken cancellationToken = default)
    {
        await base.RenderAsync(buffer, cancellationToken);

        // Draw cursor if editing and has focus
        if (_isEditing && HasFocus && IsVisible)
        {
            // Calculate cursor position on screen
            // For simplicity, assume single line editing for now
            var cursorX = Position.X + Math.Min(_cursorPosition, Size.Width - 1);
            var cursorY = Position.Y;

            // Draw cursor as an inverted character
            var cursorChar = _cursorPosition < Text.Length ? Text[_cursorPosition] : ' ';
            var cursorStyle = new ConsoleTextStyle(
                Style.BackgroundColor,
                Style.ForegroundColor,
                Style.IsBold,
                Style.IsUnderlined,
                Style.IsItalic,
                true); // Blinking cursor

            buffer.WriteCharAt(cursorX, cursorY, cursorChar, cursorStyle);
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> HandleKeyPressAsync(Input.Core.KeyCode key, ConsoleInputModifiers modifiers, CancellationToken cancellationToken = default)
    {
        if (!_isEditing)
            return false;

        switch (key)
        {
            case Input.Core.KeyCode.Enter:
                // Complete editing
                CompleteEditing(false);
                return true;

            case Input.Core.KeyCode.Escape:
                // Cancel editing
                CompleteEditing(true);
                return true;

            case Input.Core.KeyCode.LeftArrow:
                if (_cursorPosition > 0)
                    _cursorPosition--;
                return true;

            case Input.Core.KeyCode.RightArrow:
                if (_cursorPosition < Text.Length)
                    _cursorPosition++;
                return true;

            case Input.Core.KeyCode.Home:
                _cursorPosition = 0;
                return true;

            case Input.Core.KeyCode.End:
                _cursorPosition = Text.Length;
                return true;

            case Input.Core.KeyCode.Backspace:
                if (_cursorPosition > 0)
                {
                    var newText = Text.Remove(_cursorPosition - 1, 1);
                    Text = newText;
                    _cursorPosition--;
                }
                return true;

            case Input.Core.KeyCode.Delete:
                if (_cursorPosition < Text.Length)
                {
                    var newText = Text.Remove(_cursorPosition, 1);
                    Text = newText;
                }
                return true;

            default:
                return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> HandleTextInputAsync(char character, ConsoleInputModifiers modifiers, CancellationToken cancellationToken = default)
    {
        if (!_isEditing)
            return false;

        // Insert character at cursor position
        if (char.IsControl(character))
            return false;

        var newText = Text.Insert(_cursorPosition, character.ToString());
        Text = newText;
        _cursorPosition++;
        return true;
    }

    private void CompleteEditing(bool cancel)
    {
        if (cancel)
        {
            Text = _originalText;
        }

        _isEditing = false;
        CanReceiveFocus = false;

        EditingCompleted?.Invoke(this, new EditingCompletedEventArgs(Text, cancel));
    }
}

/// <summary>
/// Event arguments for editing completed events.
/// </summary>
public class EditingCompletedEventArgs : EventArgs
{
    /// <summary>
    /// The final text value.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Whether editing was cancelled.
    /// </summary>
    public bool WasCancelled { get; }

    /// <summary>
    /// Initializes event arguments for editing completed.
    /// </summary>
    public EditingCompletedEventArgs(string text, bool wasCancelled)
    {
        Text = text;
        WasCancelled = wasCancelled;
    }
}