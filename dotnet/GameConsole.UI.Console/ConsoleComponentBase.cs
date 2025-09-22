namespace GameConsole.UI.Console;

/// <summary>
/// Base implementation for console UI components.
/// </summary>
public abstract class ConsoleComponentBase : IConsoleComponent
{
    private bool _hasFocus;
    private ConsolePosition _position;
    private ConsoleSize _size;

    /// <inheritdoc/>
    public string Id { get; }

    /// <inheritdoc/>
    public ConsolePosition Position
    {
        get => _position;
        set => _position = value;
    }

    /// <inheritdoc/>
    public ConsoleSize Size
    {
        get => _size;
        set => _size = value;
    }

    /// <inheritdoc/>
    public bool IsVisible { get; set; } = true;

    /// <inheritdoc/>
    public bool CanReceiveFocus { get; set; } = true;

    /// <inheritdoc/>
    public bool HasFocus
    {
        get => _hasFocus;
        set
        {
            if (_hasFocus != value)
            {
                var wasFocused = _hasFocus;
                _hasFocus = value;

                if (_hasFocus)
                {
                    OnFocusGained();
                    FocusGained?.Invoke(this, EventArgs.Empty);
                }
                else if (wasFocused)
                {
                    OnFocusLost();
                    FocusLost?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }

    /// <inheritdoc/>
    public event EventHandler<EventArgs>? FocusRequested;

    /// <inheritdoc/>
    public event EventHandler<EventArgs>? FocusGained;

    /// <inheritdoc/>
    public event EventHandler<EventArgs>? FocusLost;

    /// <summary>
    /// Initializes a new console component.
    /// </summary>
    /// <param name="id">Unique identifier for the component.</param>
    /// <param name="position">Initial position.</param>
    /// <param name="size">Initial size.</param>
    protected ConsoleComponentBase(string? id = null, ConsolePosition position = default, ConsoleSize size = default)
    {
        Id = id ?? Guid.NewGuid().ToString();
        _position = position;
        _size = size;
    }

    /// <inheritdoc/>
    public abstract Task RenderAsync(IConsoleBuffer buffer, CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public virtual async Task<bool> HandleInputAsync(ConsoleInputEvent inputEvent, CancellationToken cancellationToken = default)
    {
        // Handle common input events
        switch (inputEvent.Type)
        {
            case ConsoleInputEventType.KeyPress when inputEvent.Key.HasValue:
                return await HandleKeyPressAsync(inputEvent.Key.Value, inputEvent.Modifiers, cancellationToken);

            case ConsoleInputEventType.TextInput when inputEvent.Character.HasValue:
                return await HandleTextInputAsync(inputEvent.Character.Value, inputEvent.Modifiers, cancellationToken);

            case ConsoleInputEventType.MouseClick when inputEvent.MouseButton.HasValue && inputEvent.MousePosition.HasValue:
                return await HandleMouseClickAsync(inputEvent.MouseButton.Value, inputEvent.MousePosition.Value, inputEvent.Modifiers, cancellationToken);

            case ConsoleInputEventType.MouseMove when inputEvent.MousePosition.HasValue:
                return await HandleMouseMoveAsync(inputEvent.MousePosition.Value, cancellationToken);

            default:
                return false;
        }
    }

    /// <summary>
    /// Handles key press events. Override to provide component-specific key handling.
    /// </summary>
    /// <param name="key">The pressed key.</param>
    /// <param name="modifiers">Input modifiers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the key was handled, false otherwise.</returns>
    protected virtual Task<bool> HandleKeyPressAsync(Input.Core.KeyCode key, ConsoleInputModifiers modifiers, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    /// <summary>
    /// Handles text input events. Override to provide component-specific text handling.
    /// </summary>
    /// <param name="character">The input character.</param>
    /// <param name="modifiers">Input modifiers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the input was handled, false otherwise.</returns>
    protected virtual Task<bool> HandleTextInputAsync(char character, ConsoleInputModifiers modifiers, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    /// <summary>
    /// Handles mouse click events. Override to provide component-specific mouse handling.
    /// </summary>
    /// <param name="button">The clicked mouse button.</param>
    /// <param name="position">Mouse position in console coordinates.</param>
    /// <param name="modifiers">Input modifiers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the click was handled, false otherwise.</returns>
    protected virtual Task<bool> HandleMouseClickAsync(Input.Core.MouseButton button, ConsolePosition position, ConsoleInputModifiers modifiers, CancellationToken cancellationToken = default)
    {
        // Check if click is within component bounds
        if (IsPositionWithinBounds(position))
        {
            RequestFocus();
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    /// <summary>
    /// Handles mouse move events. Override to provide component-specific mouse move handling.
    /// </summary>
    /// <param name="position">Mouse position in console coordinates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the move was handled, false otherwise.</returns>
    protected virtual Task<bool> HandleMouseMoveAsync(ConsolePosition position, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    /// <summary>
    /// Called when the component gains focus. Override to provide component-specific focus behavior.
    /// </summary>
    protected virtual void OnFocusGained()
    {
        // Override in derived classes
    }

    /// <summary>
    /// Called when the component loses focus. Override to provide component-specific focus behavior.
    /// </summary>
    protected virtual void OnFocusLost()
    {
        // Override in derived classes
    }

    /// <summary>
    /// Requests focus for this component.
    /// </summary>
    protected void RequestFocus()
    {
        if (CanReceiveFocus && !HasFocus)
        {
            FocusRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Checks if a position is within the component's bounds.
    /// </summary>
    /// <param name="position">Position to check.</param>
    /// <returns>True if position is within bounds, false otherwise.</returns>
    protected bool IsPositionWithinBounds(ConsolePosition position)
    {
        return position.X >= Position.X && position.X < Position.X + Size.Width &&
               position.Y >= Position.Y && position.Y < Position.Y + Size.Height;
    }

    /// <summary>
    /// Gets the relative position within the component bounds.
    /// </summary>
    /// <param name="absolutePosition">Absolute console position.</param>
    /// <returns>Position relative to the component's top-left corner.</returns>
    protected ConsolePosition GetRelativePosition(ConsolePosition absolutePosition)
    {
        return new ConsolePosition(
            absolutePosition.X - Position.X,
            absolutePosition.Y - Position.Y);
    }

    /// <summary>
    /// Draws text with automatic wrapping within the component bounds.
    /// </summary>
    /// <param name="buffer">Console buffer to draw to.</param>
    /// <param name="x">X position relative to component.</param>
    /// <param name="y">Y position relative to component.</param>
    /// <param name="text">Text to draw.</param>
    /// <param name="style">Text style.</param>
    /// <param name="maxWidth">Maximum width for text wrapping.</param>
    /// <returns>Number of lines used.</returns>
    protected int DrawWrappedText(IConsoleBuffer buffer, int x, int y, string text, ConsoleTextStyle style, int maxWidth)
    {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0)
            return 0;

        var lines = WrapText(text, maxWidth);
        var absoluteX = Position.X + x;
        var absoluteY = Position.Y + y;

        for (int i = 0; i < lines.Count && absoluteY + i < Position.Y + Size.Height; i++)
        {
            if (absoluteY + i >= 0)
            {
                buffer.WriteAt(absoluteX, absoluteY + i, lines[i], style);
            }
        }

        return lines.Count;
    }

    /// <summary>
    /// Wraps text to fit within the specified width.
    /// </summary>
    /// <param name="text">Text to wrap.</param>
    /// <param name="width">Maximum line width.</param>
    /// <returns>List of wrapped lines.</returns>
    protected static List<string> WrapText(string text, int width)
    {
        var lines = new List<string>();
        if (string.IsNullOrEmpty(text) || width <= 0)
            return lines;

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var currentLine = new System.Text.StringBuilder();

        foreach (var word in words)
        {
            // If adding this word would exceed the width, start a new line
            if (currentLine.Length > 0 && currentLine.Length + 1 + word.Length > width)
            {
                lines.Add(currentLine.ToString());
                currentLine.Clear();
            }

            // Add word to current line
            if (currentLine.Length > 0)
                currentLine.Append(' ');
            currentLine.Append(word);
        }

        // Add the final line if it has content
        if (currentLine.Length > 0)
        {
            lines.Add(currentLine.ToString());
        }

        return lines;
    }

    /// <summary>
    /// Centers text within the specified width.
    /// </summary>
    /// <param name="text">Text to center.</param>
    /// <param name="width">Available width.</param>
    /// <returns>Centered text with padding.</returns>
    protected static string CenterText(string text, int width)
    {
        if (string.IsNullOrEmpty(text) || width <= 0)
            return string.Empty;

        if (text.Length >= width)
            return text.Substring(0, width);

        var padding = (width - text.Length) / 2;
        return text.PadLeft(padding + text.Length).PadRight(width);
    }

    /// <summary>
    /// Right-aligns text within the specified width.
    /// </summary>
    /// <param name="text">Text to align.</param>
    /// <param name="width">Available width.</param>
    /// <returns>Right-aligned text with padding.</returns>
    protected static string RightAlignText(string text, int width)
    {
        if (string.IsNullOrEmpty(text) || width <= 0)
            return string.Empty;

        if (text.Length >= width)
            return text.Substring(0, width);

        return text.PadLeft(width);
    }
}