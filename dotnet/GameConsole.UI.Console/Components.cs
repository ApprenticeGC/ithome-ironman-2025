using GameConsole.UI.Core;

namespace GameConsole.UI.Console.Components;

/// <summary>
/// A simple label component for displaying text.
/// </summary>
public class Label : UIComponentBase
{
    /// <summary>
    /// Initializes a new instance of the Label class.
    /// </summary>
    /// <param name="id">The unique identifier for the label.</param>
    /// <param name="text">The text to display.</param>
    /// <param name="position">The position of the label.</param>
    /// <param name="style">Optional text style for the label.</param>
    public Label(string id, string text, Position position, TextStyle? style = null) 
        : base(id, position, new Size(text?.Length ?? 0, 1))
    {
        Text = text ?? string.Empty;
        Style = style;
    }

    /// <summary>
    /// Gets or sets the text to display.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Gets or sets the text style for the label.
    /// </summary>
    public TextStyle? Style { get; set; }

    /// <inheritdoc />
    public override async Task RenderAsync(IService uiService, CancellationToken cancellationToken = default)
    {
        if (!IsVisible || string.IsNullOrEmpty(Text)) return;

        await uiService.RenderTextAsync(Text, Position, Style, cancellationToken);
    }

    /// <summary>
    /// Updates the label text and adjusts size accordingly.
    /// </summary>
    /// <param name="text">The new text.</param>
    public void UpdateText(string text)
    {
        Text = text ?? string.Empty;
        Size = new Size(Text.Length, 1);
    }
}

/// <summary>
/// A simple panel component for grouping other components with a border.
/// </summary>
public class Panel : UIComponentBase
{
    private readonly List<IUIComponent> _children = new();

    /// <summary>
    /// Initializes a new instance of the Panel class.
    /// </summary>
    /// <param name="id">The unique identifier for the panel.</param>
    /// <param name="position">The position of the panel.</param>
    /// <param name="size">The size of the panel.</param>
    /// <param name="title">Optional title for the panel border.</param>
    /// <param name="borderStyle">The border style.</param>
    public Panel(string id, Position position, Size size, string? title = null, BorderStyle borderStyle = BorderStyle.Single) 
        : base(id, position, size)
    {
        Title = title;
        BorderStyle = borderStyle;
    }

    /// <summary>
    /// Gets or sets the panel title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the border style.
    /// </summary>
    public BorderStyle BorderStyle { get; set; }

    /// <summary>
    /// Gets the child components.
    /// </summary>
    public IReadOnlyList<IUIComponent> Children => _children.AsReadOnly();

    /// <summary>
    /// Adds a child component to the panel.
    /// </summary>
    /// <param name="component">The component to add.</param>
    public void AddChild(IUIComponent component)
    {
        if (component == null) throw new ArgumentNullException(nameof(component));
        _children.Add(component);
    }

    /// <summary>
    /// Removes a child component from the panel.
    /// </summary>
    /// <param name="componentId">The ID of the component to remove.</param>
    /// <returns>True if the component was removed, false if not found.</returns>
    public bool RemoveChild(string componentId)
    {
        var component = _children.FirstOrDefault(c => c.Id == componentId);
        if (component != null)
        {
            _children.Remove(component);
            return true;
        }
        return false;
    }

    /// <inheritdoc />
    public override async Task RenderAsync(IService uiService, CancellationToken cancellationToken = default)
    {
        if (!IsVisible) return;

        // Render border
        await RenderBorderAsync(uiService, cancellationToken);

        // Render children
        foreach (var child in _children)
        {
            if (child.IsVisible)
            {
                await child.RenderAsync(uiService, cancellationToken);
            }
        }
    }

    private async Task RenderBorderAsync(IService uiService, CancellationToken cancellationToken)
    {
        var chars = GetBorderChars(BorderStyle);
        
        // Top border
        var topBorder = chars.TopLeft + new string(chars.Horizontal, Size.Width - 2) + chars.TopRight;
        await uiService.RenderTextAsync(topBorder, Position, null, cancellationToken);

        // Side borders
        for (int y = 1; y < Size.Height - 1; y++)
        {
            await uiService.RenderTextAsync(chars.Vertical.ToString(), 
                Position.Add(new Position(0, y)), null, cancellationToken);
            await uiService.RenderTextAsync(chars.Vertical.ToString(), 
                Position.Add(new Position(Size.Width - 1, y)), null, cancellationToken);
        }

        // Bottom border
        var bottomBorder = chars.BottomLeft + new string(chars.Horizontal, Size.Width - 2) + chars.BottomRight;
        await uiService.RenderTextAsync(bottomBorder, 
            Position.Add(new Position(0, Size.Height - 1)), null, cancellationToken);

        // Title
        if (!string.IsNullOrEmpty(Title) && Title.Length + 2 < Size.Width)
        {
            var titleText = $" {Title} ";
            var titlePos = Position.Add(new Position((Size.Width - titleText.Length) / 2, 0));
            await uiService.RenderTextAsync(titleText, titlePos, null, cancellationToken);
        }
    }

    private static BorderChars GetBorderChars(BorderStyle style)
    {
        return style switch
        {
            BorderStyle.Double => new BorderChars('╔', '╗', '╚', '╝', '═', '║'),
            BorderStyle.Thick => new BorderChars('┏', '┓', '┗', '┛', '━', '┃'),
            _ => new BorderChars('┌', '┐', '└', '┘', '─', '│')
        };
    }

    private record BorderChars(char TopLeft, char TopRight, char BottomLeft, char BottomRight, char Horizontal, char Vertical);
}

/// <summary>
/// Border style options for panels and other bordered components.
/// </summary>
public enum BorderStyle
{
    /// <summary>
    /// Single line border (default).
    /// </summary>
    Single,
    
    /// <summary>
    /// Double line border.
    /// </summary>
    Double,
    
    /// <summary>
    /// Thick line border.
    /// </summary>
    Thick
}

/// <summary>
/// A simple button component for interactive elements.
/// </summary>
public class Button : UIComponentBase
{
    /// <summary>
    /// Initializes a new instance of the Button class.
    /// </summary>
    /// <param name="id">The unique identifier for the button.</param>
    /// <param name="text">The button text.</param>
    /// <param name="position">The position of the button.</param>
    /// <param name="style">Optional text style for the button.</param>
    public Button(string id, string text, Position position, TextStyle? style = null) 
        : base(id, position, new Size(text?.Length + 4 ?? 4, 3)) // Add padding and height for button
    {
        Text = text ?? string.Empty;
        Style = style ?? new TextStyle(ConsoleColor.Black, ConsoleColor.Gray);
        IsSelected = false;
    }

    /// <summary>
    /// Gets or sets the button text.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Gets or sets the text style for the button.
    /// </summary>
    public TextStyle Style { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the button is currently selected/focused.
    /// </summary>
    public bool IsSelected { get; set; }

    /// <inheritdoc />
    public override async Task RenderAsync(IService uiService, CancellationToken cancellationToken = default)
    {
        if (!IsVisible) return;

        var buttonStyle = IsSelected ? 
            new TextStyle(Style.BackgroundColor, Style.ForegroundColor, TextAttributes.Reverse) : 
            Style;

        // Top border
        var topBorder = "┌" + new string('─', Size.Width - 2) + "┐";
        await uiService.RenderTextAsync(topBorder, Position, buttonStyle, cancellationToken);

        // Middle with text
        var padding = (Size.Width - 2 - Text.Length) / 2;
        var middleText = "│" + new string(' ', padding) + Text + 
                        new string(' ', Size.Width - 2 - padding - Text.Length) + "│";
        await uiService.RenderTextAsync(middleText, Position.Add(new Position(0, 1)), 
            buttonStyle, cancellationToken);

        // Bottom border
        var bottomBorder = "└" + new string('─', Size.Width - 2) + "┘";
        await uiService.RenderTextAsync(bottomBorder, Position.Add(new Position(0, 2)), 
            buttonStyle, cancellationToken);
    }
}