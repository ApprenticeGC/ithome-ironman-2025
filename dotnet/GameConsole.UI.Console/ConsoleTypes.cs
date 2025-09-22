namespace GameConsole.UI.Console;

/// <summary>
/// Represents a position on the console screen.
/// </summary>
public readonly struct ConsolePosition
{
    /// <summary>
    /// X coordinate (column).
    /// </summary>
    public int X { get; }

    /// <summary>
    /// Y coordinate (row).
    /// </summary>
    public int Y { get; }

    /// <summary>
    /// Initializes a new console position.
    /// </summary>
    /// <param name="x">X coordinate (column).</param>
    /// <param name="y">Y coordinate (row).</param>
    public ConsolePosition(int x, int y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Zero position constant.
    /// </summary>
    public static readonly ConsolePosition Zero = new(0, 0);
}

/// <summary>
/// Represents a size on the console screen.
/// </summary>
public readonly struct ConsoleSize
{
    /// <summary>
    /// Width in characters.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Height in characters.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Initializes a new console size.
    /// </summary>
    /// <param name="width">Width in characters.</param>
    /// <param name="height">Height in characters.</param>
    public ConsoleSize(int width, int height)
    {
        Width = Math.Max(0, width);
        Height = Math.Max(0, height);
    }

    /// <summary>
    /// Empty size constant.
    /// </summary>
    public static readonly ConsoleSize Empty = new(0, 0);
}

/// <summary>
/// Console color enumeration.
/// </summary>
public enum ConsoleColorType
{
    Default,
    Black,
    DarkRed,
    DarkGreen,
    DarkYellow,
    DarkBlue,
    DarkMagenta,
    DarkCyan,
    Gray,
    DarkGray,
    Red,
    Green,
    Yellow,
    Blue,
    Magenta,
    Cyan,
    White
}

/// <summary>
/// Text style options for console text.
/// </summary>
public readonly struct ConsoleTextStyle
{
    /// <summary>
    /// Foreground color.
    /// </summary>
    public ConsoleColorType ForegroundColor { get; }

    /// <summary>
    /// Background color.
    /// </summary>
    public ConsoleColorType BackgroundColor { get; }

    /// <summary>
    /// Whether text is bold.
    /// </summary>
    public bool IsBold { get; }

    /// <summary>
    /// Whether text is underlined.
    /// </summary>
    public bool IsUnderlined { get; }

    /// <summary>
    /// Whether text is italic.
    /// </summary>
    public bool IsItalic { get; }

    /// <summary>
    /// Whether text is blinking.
    /// </summary>
    public bool IsBlinking { get; }

    /// <summary>
    /// Initializes a new console text style.
    /// </summary>
    public ConsoleTextStyle(
        ConsoleColorType foregroundColor = ConsoleColorType.Default,
        ConsoleColorType backgroundColor = ConsoleColorType.Default,
        bool isBold = false,
        bool isUnderlined = false,
        bool isItalic = false,
        bool isBlinking = false)
    {
        ForegroundColor = foregroundColor;
        BackgroundColor = backgroundColor;
        IsBold = isBold;
        IsUnderlined = isUnderlined;
        IsItalic = isItalic;
        IsBlinking = isBlinking;
    }

    /// <summary>
    /// Default text style.
    /// </summary>
    public static readonly ConsoleTextStyle Default = new();

    /// <summary>
    /// Bold text style.
    /// </summary>
    public static readonly ConsoleTextStyle Bold = new(isBold: true);

    /// <summary>
    /// Error text style (red).
    /// </summary>
    public static readonly ConsoleTextStyle Error = new(ConsoleColorType.Red, isBold: true);

    /// <summary>
    /// Warning text style (yellow).
    /// </summary>
    public static readonly ConsoleTextStyle Warning = new(ConsoleColorType.Yellow);

    /// <summary>
    /// Success text style (green).
    /// </summary>
    public static readonly ConsoleTextStyle Success = new(ConsoleColorType.Green);

    /// <summary>
    /// Info text style (cyan).
    /// </summary>
    public static readonly ConsoleTextStyle Info = new(ConsoleColorType.Cyan);
}

/// <summary>
/// Text alignment options for console text.
/// </summary>
public enum ConsoleTextAlignment
{
    Left,
    Center,
    Right
}

/// <summary>
/// Console menu style options.
/// </summary>
public readonly struct ConsoleMenuStyle
{
    /// <summary>
    /// Style for the menu title.
    /// </summary>
    public ConsoleTextStyle TitleStyle { get; }

    /// <summary>
    /// Style for normal menu items.
    /// </summary>
    public ConsoleTextStyle ItemStyle { get; }

    /// <summary>
    /// Style for the selected menu item.
    /// </summary>
    public ConsoleTextStyle SelectedItemStyle { get; }

    /// <summary>
    /// Character used to indicate selection.
    /// </summary>
    public string SelectionIndicator { get; }

    /// <summary>
    /// Whether to show border around the menu.
    /// </summary>
    public bool ShowBorder { get; }

    /// <summary>
    /// Border style.
    /// </summary>
    public ConsoleBorderStyle BorderStyle { get; }

    /// <summary>
    /// Initializes a new console menu style.
    /// </summary>
    public ConsoleMenuStyle(
        ConsoleTextStyle titleStyle = default,
        ConsoleTextStyle itemStyle = default,
        ConsoleTextStyle selectedItemStyle = default,
        string selectionIndicator = "►",
        bool showBorder = true,
        ConsoleBorderStyle borderStyle = ConsoleBorderStyle.Single)
    {
        TitleStyle = titleStyle.Equals(default) ? ConsoleTextStyle.Bold : titleStyle;
        ItemStyle = itemStyle.Equals(default) ? ConsoleTextStyle.Default : itemStyle;
        SelectedItemStyle = selectedItemStyle.Equals(default) ? 
            new ConsoleTextStyle(ConsoleColorType.Yellow, isBold: true) : selectedItemStyle;
        SelectionIndicator = selectionIndicator;
        ShowBorder = showBorder;
        BorderStyle = borderStyle;
    }

    /// <summary>
    /// Default menu style.
    /// </summary>
    public static readonly ConsoleMenuStyle Default = new();
}

/// <summary>
/// Console table style options.
/// </summary>
public readonly struct ConsoleTableStyle
{
    /// <summary>
    /// Style for table headers.
    /// </summary>
    public ConsoleTextStyle HeaderStyle { get; }

    /// <summary>
    /// Style for table rows.
    /// </summary>
    public ConsoleTextStyle RowStyle { get; }

    /// <summary>
    /// Style for selected table row.
    /// </summary>
    public ConsoleTextStyle SelectedRowStyle { get; }

    /// <summary>
    /// Whether to show borders.
    /// </summary>
    public bool ShowBorders { get; }

    /// <summary>
    /// Border style.
    /// </summary>
    public ConsoleBorderStyle BorderStyle { get; }

    /// <summary>
    /// Whether to alternate row colors.
    /// </summary>
    public bool AlternateRows { get; }

    /// <summary>
    /// Initializes a new console table style.
    /// </summary>
    public ConsoleTableStyle(
        ConsoleTextStyle headerStyle = default,
        ConsoleTextStyle rowStyle = default,
        ConsoleTextStyle selectedRowStyle = default,
        bool showBorders = true,
        ConsoleBorderStyle borderStyle = ConsoleBorderStyle.Single,
        bool alternateRows = false)
    {
        HeaderStyle = headerStyle.Equals(default) ? ConsoleTextStyle.Bold : headerStyle;
        RowStyle = rowStyle.Equals(default) ? ConsoleTextStyle.Default : rowStyle;
        SelectedRowStyle = selectedRowStyle.Equals(default) ?
            new ConsoleTextStyle(ConsoleColorType.Yellow, isBold: true) : selectedRowStyle;
        ShowBorders = showBorders;
        BorderStyle = borderStyle;
        AlternateRows = alternateRows;
    }

    /// <summary>
    /// Default table style.
    /// </summary>
    public static readonly ConsoleTableStyle Default = new();
}

/// <summary>
/// Console progress bar style options.
/// </summary>
public readonly struct ConsoleProgressBarStyle
{
    /// <summary>
    /// Character used for completed progress.
    /// </summary>
    public char CompletedChar { get; }

    /// <summary>
    /// Character used for remaining progress.
    /// </summary>
    public char RemainingChar { get; }

    /// <summary>
    /// Style for completed portion.
    /// </summary>
    public ConsoleTextStyle CompletedStyle { get; }

    /// <summary>
    /// Style for remaining portion.
    /// </summary>
    public ConsoleTextStyle RemainingStyle { get; }

    /// <summary>
    /// Style for label text.
    /// </summary>
    public ConsoleTextStyle LabelStyle { get; }

    /// <summary>
    /// Whether to show border around progress bar.
    /// </summary>
    public bool ShowBorder { get; }

    /// <summary>
    /// Initializes a new progress bar style.
    /// </summary>
    public ConsoleProgressBarStyle(
        char completedChar = '█',
        char remainingChar = '░',
        ConsoleTextStyle completedStyle = default,
        ConsoleTextStyle remainingStyle = default,
        ConsoleTextStyle labelStyle = default,
        bool showBorder = true)
    {
        CompletedChar = completedChar;
        RemainingChar = remainingChar;
        CompletedStyle = completedStyle.Equals(default) ? 
            new ConsoleTextStyle(ConsoleColorType.Green) : completedStyle;
        RemainingStyle = remainingStyle.Equals(default) ? 
            new ConsoleTextStyle(ConsoleColorType.DarkGray) : remainingStyle;
        LabelStyle = labelStyle.Equals(default) ? ConsoleTextStyle.Default : labelStyle;
        ShowBorder = showBorder;
    }

    /// <summary>
    /// Default progress bar style.
    /// </summary>
    public static readonly ConsoleProgressBarStyle Default = new();
}

/// <summary>
/// Border style options for console components.
/// </summary>
public enum ConsoleBorderStyle
{
    None,
    Single,
    Double,
    Rounded,
    Thick
}