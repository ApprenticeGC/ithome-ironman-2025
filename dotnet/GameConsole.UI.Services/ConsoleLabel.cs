using GameConsole.UI.Core;
using System.Reactive.Subjects;

namespace GameConsole.UI.Services;

/// <summary>
/// Console implementation of a label component for displaying text.
/// </summary>
public class ConsoleLabel : BaseUIComponent, ILabel
{
    private string _text = string.Empty;
    private ConsoleColor? _foregroundColor;
    private ConsoleColor? _backgroundColor;
    private HorizontalAlignment _textAlignment = HorizontalAlignment.Left;
    private bool _wordWrap = false;

    public ConsoleLabel(string id, string text = "") : base(id)
    {
        _text = text;
    }

    public string Text
    {
        get => _text;
        set => _text = value ?? string.Empty;
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

    public HorizontalAlignment TextAlignment
    {
        get => _textAlignment;
        set => _textAlignment = value;
    }

    public bool WordWrap
    {
        get => _wordWrap;
        set => _wordWrap = value;
    }

    public override async Task RenderAsync(IConsoleRenderer renderer, CancellationToken cancellationToken = default)
    {
        if (!Visible || Bounds.IsEmpty || string.IsNullOrEmpty(_text))
            return;

        var lines = GetTextLines();
        int startY = Bounds.Top;

        for (int i = 0; i < lines.Count && startY + i < Bounds.Bottom + 1; i++)
        {
            var line = lines[i];
            var x = GetLineStartPosition(line, Bounds.Width);
            
            // Truncate line if it's too long and word wrap is disabled
            if (!_wordWrap && line.Length > Bounds.Width)
            {
                line = line.Substring(0, Bounds.Width);
            }

            await renderer.WriteTextAtAsync(
                Bounds.Left + x, 
                startY + i, 
                line, 
                _foregroundColor, 
                _backgroundColor, 
                cancellationToken);
        }
    }

    private List<string> GetTextLines()
    {
        if (string.IsNullOrEmpty(_text))
            return new List<string> { string.Empty };

        if (!_wordWrap)
            return _text.Split('\n').ToList();

        var lines = new List<string>();
        var paragraphs = _text.Split('\n');

        foreach (var paragraph in paragraphs)
        {
            if (paragraph.Length <= Bounds.Width)
            {
                lines.Add(paragraph);
            }
            else
            {
                var words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var currentLine = string.Empty;

                foreach (var word in words)
                {
                    if (string.IsNullOrEmpty(currentLine))
                    {
                        currentLine = word;
                    }
                    else if (currentLine.Length + word.Length + 1 <= Bounds.Width)
                    {
                        currentLine += " " + word;
                    }
                    else
                    {
                        lines.Add(currentLine);
                        currentLine = word;
                    }
                }

                if (!string.IsNullOrEmpty(currentLine))
                {
                    lines.Add(currentLine);
                }
            }
        }

        return lines;
    }

    private int GetLineStartPosition(string line, int availableWidth)
    {
        return _textAlignment switch
        {
            HorizontalAlignment.Center => Math.Max(0, (availableWidth - line.Length) / 2),
            HorizontalAlignment.Right => Math.Max(0, availableWidth - line.Length),
            _ => 0 // Left alignment
        };
    }
}