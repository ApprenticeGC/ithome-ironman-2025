namespace GameConsole.UI.Console;

/// <summary>
/// ANSI escape sequence codes for console formatting and colors.
/// </summary>
public static class ANSIEscapeSequences
{
    // Reset
    public const string Reset = "\u001b[0m";
    
    // Text styles
    public const string Bold = "\u001b[1m";
    public const string Dim = "\u001b[2m";
    public const string Italic = "\u001b[3m";
    public const string Underline = "\u001b[4m";
    public const string Blink = "\u001b[5m";
    public const string Reverse = "\u001b[7m";
    public const string Hidden = "\u001b[8m";
    public const string Strikethrough = "\u001b[9m";
    
    // Foreground colors (standard)
    public const string FgBlack = "\u001b[30m";
    public const string FgRed = "\u001b[31m";
    public const string FgGreen = "\u001b[32m";
    public const string FgYellow = "\u001b[33m";
    public const string FgBlue = "\u001b[34m";
    public const string FgMagenta = "\u001b[35m";
    public const string FgCyan = "\u001b[36m";
    public const string FgWhite = "\u001b[37m";
    
    // Foreground colors (bright)
    public const string FgBrightBlack = "\u001b[90m";
    public const string FgBrightRed = "\u001b[91m";
    public const string FgBrightGreen = "\u001b[92m";
    public const string FgBrightYellow = "\u001b[93m";
    public const string FgBrightBlue = "\u001b[94m";
    public const string FgBrightMagenta = "\u001b[95m";
    public const string FgBrightCyan = "\u001b[96m";
    public const string FgBrightWhite = "\u001b[97m";
    
    // Background colors (standard)
    public const string BgBlack = "\u001b[40m";
    public const string BgRed = "\u001b[41m";
    public const string BgGreen = "\u001b[42m";
    public const string BgYellow = "\u001b[43m";
    public const string BgBlue = "\u001b[44m";
    public const string BgMagenta = "\u001b[45m";
    public const string BgCyan = "\u001b[46m";
    public const string BgWhite = "\u001b[47m";
    
    // Background colors (bright)
    public const string BgBrightBlack = "\u001b[100m";
    public const string BgBrightRed = "\u001b[101m";
    public const string BgBrightGreen = "\u001b[102m";
    public const string BgBrightYellow = "\u001b[103m";
    public const string BgBrightBlue = "\u001b[104m";
    public const string BgBrightMagenta = "\u001b[105m";
    public const string BgBrightCyan = "\u001b[106m";
    public const string BgBrightWhite = "\u001b[107m";
    
    // Cursor control
    public const string CursorUp = "\u001b[1A";
    public const string CursorDown = "\u001b[1B";
    public const string CursorRight = "\u001b[1C";
    public const string CursorLeft = "\u001b[1D";
    public const string CursorHome = "\u001b[H";
    public const string CursorSave = "\u001b[s";
    public const string CursorRestore = "\u001b[u";
    public const string CursorHide = "\u001b[?25l";
    public const string CursorShow = "\u001b[?25h";
    
    // Screen control
    public const string ClearScreen = "\u001b[2J";
    public const string ClearLine = "\u001b[2K";
    public const string ClearToEndOfScreen = "\u001b[0J";
    public const string ClearToEndOfLine = "\u001b[0K";
    
    // Scrolling
    public const string ScrollUp = "\u001b[1S";
    public const string ScrollDown = "\u001b[1T";
    
    /// <summary>
    /// Moves cursor to specific position.
    /// </summary>
    /// <param name="row">Row position (1-based).</param>
    /// <param name="col">Column position (1-based).</param>
    /// <returns>ANSI sequence to move cursor.</returns>
    public static string MoveCursor(int row, int col) => $"\u001b[{row};{col}H";
    
    /// <summary>
    /// Moves cursor up by specified lines.
    /// </summary>
    /// <param name="lines">Number of lines to move up.</param>
    /// <returns>ANSI sequence to move cursor up.</returns>
    public static string MoveCursorUp(int lines) => $"\u001b[{lines}A";
    
    /// <summary>
    /// Moves cursor down by specified lines.
    /// </summary>
    /// <param name="lines">Number of lines to move down.</param>
    /// <returns>ANSI sequence to move cursor down.</returns>
    public static string MoveCursorDown(int lines) => $"\u001b[{lines}B";
    
    /// <summary>
    /// Moves cursor right by specified columns.
    /// </summary>
    /// <param name="cols">Number of columns to move right.</param>
    /// <returns>ANSI sequence to move cursor right.</returns>
    public static string MoveCursorRight(int cols) => $"\u001b[{cols}C";
    
    /// <summary>
    /// Moves cursor left by specified columns.
    /// </summary>
    /// <param name="cols">Number of columns to move left.</param>
    /// <returns>ANSI sequence to move cursor left.</returns>
    public static string MoveCursorLeft(int cols) => $"\u001b[{cols}D";
    
    /// <summary>
    /// Sets foreground color using RGB values.
    /// </summary>
    /// <param name="r">Red component (0-255).</param>
    /// <param name="g">Green component (0-255).</param>
    /// <param name="b">Blue component (0-255).</param>
    /// <returns>ANSI sequence to set foreground RGB color.</returns>
    public static string FgRgb(int r, int g, int b) => $"\u001b[38;2;{r};{g};{b}m";
    
    /// <summary>
    /// Sets background color using RGB values.
    /// </summary>
    /// <param name="r">Red component (0-255).</param>
    /// <param name="g">Green component (0-255).</param>
    /// <param name="b">Blue component (0-255).</param>
    /// <returns>ANSI sequence to set background RGB color.</returns>
    public static string BgRgb(int r, int g, int b) => $"\u001b[48;2;{r};{g};{b}m";
    
    /// <summary>
    /// Sets foreground color using 256-color palette.
    /// </summary>
    /// <param name="color">Color index (0-255).</param>
    /// <returns>ANSI sequence to set foreground color.</returns>
    public static string Fg256(int color) => $"\u001b[38;5;{color}m";
    
    /// <summary>
    /// Sets background color using 256-color palette.
    /// </summary>
    /// <param name="color">Color index (0-255).</param>
    /// <returns>ANSI sequence to set background color.</returns>
    public static string Bg256(int color) => $"\u001b[48;5;{color}m";
}