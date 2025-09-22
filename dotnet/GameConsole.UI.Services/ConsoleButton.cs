using GameConsole.UI.Core;
using System.Reactive.Subjects;
using System.Reactive.Linq;

namespace GameConsole.UI.Services;

/// <summary>
/// Console implementation of a button component.
/// </summary>
public class ConsoleButton : BaseUIComponent, IButton
{
    private readonly Subject<IButton> _clickedSubject = new();
    private string _text = string.Empty;
    private bool _isPressed = false;
    private ConsoleColor? _foregroundColor;
    private ConsoleColor? _backgroundColor;

    public ConsoleButton(string id, string text = "") : base(id)
    {
        _text = text;
        CanFocus = true;
    }

    public string Text
    {
        get => _text;
        set => _text = value ?? string.Empty;
    }

    public bool IsPressed
    {
        get => _isPressed;
        private set => _isPressed = value;
    }

    public ConsoleColor? ForegroundColor
    {
        get => _foregroundColor;
        set => _foregroundColor = value;
    }

    public ConsoleColor? BackgroundColor
    {
        get => _backgroundColor;
        set => _backgroundColor = value;
    }

    public IObservable<IButton> Clicked => _clickedSubject.AsObservable();

    public override async Task RenderAsync(IConsoleRenderer renderer, CancellationToken cancellationToken = default)
    {
        if (!Visible || Bounds.IsEmpty)
            return;

        // Determine button colors based on focus and press state
        var (foreground, background) = GetRenderColors();

        // Fill button background
        await renderer.FillRectangleAsync(Bounds, ' ', foreground, background, cancellationToken);

        // Draw border around button
        var borderStyle = HasFocus ? BorderStyle.Double : BorderStyle.Single;
        await renderer.DrawBorderAsync(Bounds, borderStyle, foreground, cancellationToken);

        // Draw button text centered
        if (!string.IsNullOrEmpty(_text) && Bounds.Width > 2 && Bounds.Height > 0)
        {
            var textBounds = new Rectangle(Bounds.Left + 1, Bounds.Top, Bounds.Width - 2, Bounds.Height);
            var displayText = GetDisplayText(textBounds.Width);
            
            if (!string.IsNullOrEmpty(displayText))
            {
                var x = textBounds.Left + Math.Max(0, (textBounds.Width - displayText.Length) / 2);
                var y = textBounds.Top + Math.Max(0, textBounds.Height / 2);
                
                await renderer.WriteTextAtAsync(x, y, displayText, foreground, background, cancellationToken);
            }
        }

        // Show focus indicator if focused
        if (HasFocus && Bounds.Width > 0 && Bounds.Height > 0)
        {
            // Draw focus corner indicators
            await renderer.WriteTextAtAsync(Bounds.Left, Bounds.Top, "◆", ConsoleColor.Yellow, null, cancellationToken);
            await renderer.WriteTextAtAsync(Bounds.Right, Bounds.Bottom, "◆", ConsoleColor.Yellow, null, cancellationToken);
        }
    }

    public override async Task<bool> HandleEventAsync(UIEvent uiEvent, CancellationToken cancellationToken = default)
    {
        if (!HasFocus || !Visible)
            return false;

        switch (uiEvent)
        {
            case KeyEvent keyEvent when keyEvent.Key == ConsoleKey.Enter || keyEvent.Key == ConsoleKey.Spacebar:
                await SimulateClickAsync(cancellationToken);
                return true;

            case ClickEvent clickEvent when Bounds.Contains(clickEvent.Position):
                await SimulateClickAsync(cancellationToken);
                return true;
        }

        return false;
    }

    private async Task SimulateClickAsync(CancellationToken cancellationToken)
    {
        _isPressed = true;

        // Visual feedback - show pressed state briefly
        await Task.Delay(100, cancellationToken);
        
        _isPressed = false;
        _clickedSubject.OnNext(this);
    }

    private (ConsoleColor? foreground, ConsoleColor? background) GetRenderColors()
    {
        var foreground = _foregroundColor ?? ConsoleColor.White;
        var background = _backgroundColor ?? ConsoleColor.DarkGray;

        if (HasFocus)
        {
            foreground = ConsoleColor.Yellow;
            background = ConsoleColor.DarkBlue;
        }

        if (_isPressed)
        {
            foreground = ConsoleColor.Black;
            background = ConsoleColor.Gray;
        }

        return (foreground, background);
    }

    private string GetDisplayText(int maxWidth)
    {
        if (string.IsNullOrEmpty(_text) || maxWidth <= 0)
            return string.Empty;

        return _text.Length <= maxWidth ? _text : _text.Substring(0, maxWidth);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _clickedSubject.OnCompleted();
            _clickedSubject.Dispose();
        }
        base.Dispose(disposing);
    }
}