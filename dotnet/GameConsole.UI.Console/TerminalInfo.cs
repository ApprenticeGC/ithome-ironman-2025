namespace GameConsole.UI.Console;

/// <summary>
/// Provides information about terminal capabilities and configuration.
/// </summary>
public class TerminalInfo
{
    /// <summary>
    /// Gets whether the terminal supports color output.
    /// </summary>
    public bool SupportsColor { get; init; } = true;
    
    /// <summary>
    /// Gets whether the terminal supports 256-color output.
    /// </summary>
    public bool Supports256Colors { get; init; } = true;
    
    /// <summary>
    /// Gets whether the terminal supports RGB (true color) output.
    /// </summary>
    public bool SupportsRgbColors { get; init; } = true;
    
    /// <summary>
    /// Gets whether the terminal supports Unicode characters.
    /// </summary>
    public bool SupportsUnicode { get; init; } = true;
    
    /// <summary>
    /// Gets whether the terminal supports mouse input.
    /// </summary>
    public bool SupportsMouse { get; init; } = false;
    
    /// <summary>
    /// Gets the current terminal width in characters.
    /// </summary>
    public int Width { get; private set; } = 80;
    
    /// <summary>
    /// Gets the current terminal height in rows.
    /// </summary>
    public int Height { get; private set; } = 25;
    
    /// <summary>
    /// Gets the current terminal size.
    /// </summary>
    public ConsoleSize Size => new(Width, Height);
    
    /// <summary>
    /// Detects current terminal capabilities and size.
    /// </summary>
    /// <returns>Terminal information.</returns>
    public static TerminalInfo Detect()
    {
        var info = new TerminalInfo();
        info.UpdateSize();
        return info;
    }
    
    /// <summary>
    /// Updates the terminal size information.
    /// </summary>
    public void UpdateSize()
    {
        try
        {
            Width = System.Console.WindowWidth;
            Height = System.Console.WindowHeight;
        }
        catch
        {
            // Fallback to default values if console is not available
            Width = 80;
            Height = 25;
        }
    }
    
    /// <summary>
    /// Gets whether the terminal is likely running on Windows.
    /// </summary>
    public bool IsWindows => OperatingSystem.IsWindows();
    
    /// <summary>
    /// Gets whether the terminal is likely running on Linux/Unix.
    /// </summary>
    public bool IsUnix => OperatingSystem.IsLinux() || OperatingSystem.IsMacOS();
    
    /// <summary>
    /// Gets the terminal environment variable (if available).
    /// </summary>
    public string? TerminalType => Environment.GetEnvironmentVariable("TERM");
    
    /// <summary>
    /// Gets whether this is likely a modern terminal with extended capabilities.
    /// </summary>
    public bool IsModernTerminal => 
        TerminalType?.Contains("xterm") == true ||
        TerminalType?.Contains("screen") == true ||
        TerminalType?.Contains("tmux") == true ||
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("COLORTERM"));
}