using GameConsole.Core.Abstractions;
using GameConsole.UI.Core;
using Microsoft.Extensions.Logging;
using System.Reactive.Subjects;
using System.Reactive.Linq;

namespace GameConsole.UI.Services;

/// <summary>
/// Console-based renderer service for drawing UI components to the terminal.
/// </summary>
[Service("Console Renderer", "1.0.0", "Provides console-based rendering for UI components",
    Categories = new[] { "UI", "Console", "Rendering" }, Lifetime = ServiceLifetime.Singleton)]
public class ConsoleRendererService : BaseUIService, IConsoleRenderer
{
    private readonly Subject<Size> _consoleSizeChangedSubject = new();
    private Size _lastConsoleSize = Size.Empty;

    public ConsoleRendererService(ILogger<ConsoleRendererService> logger) : base(logger) { }

    public IObservable<Size> ConsoleSizeChanged => _consoleSizeChangedSubject.AsObservable();

    protected override Task OnInitializeAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Initializing console renderer");
        
        // Enable virtual terminal sequences if supported
        try
        {
            if (OperatingSystem.IsWindows())
            {
                // Enable ANSI escape sequences on Windows
                EnableWindowsAnsiSequences();
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to enable ANSI sequences, console colors may not work properly");
        }

        // Initialize console size tracking
        _lastConsoleSize = GetConsoleSize();
        
        Logger.LogDebug("Console renderer initialized");
        return Task.CompletedTask;
    }

    protected override Task OnStartAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Starting console renderer");
        
        // Start monitoring console size changes
        _ = Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var currentSize = GetConsoleSize();
                    if (currentSize != _lastConsoleSize)
                    {
                        _lastConsoleSize = currentSize;
                        _consoleSizeChangedSubject.OnNext(currentSize);
                        Logger.LogDebug("Console size changed to {Width}x{Height}", currentSize.Width, currentSize.Height);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Error checking console size");
                }
                
                await Task.Delay(500, cancellationToken); // Check every 500ms
            }
        }, cancellationToken);
        
        Logger.LogDebug("Console renderer started");
        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            try
            {
                Console.Clear();
                Logger.LogDebug("Console cleared");
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to clear console");
            }
        }, cancellationToken);
    }

    public Task SetCursorPositionAsync(int left, int top, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            try
            {
                if (left >= 0 && top >= 0 && left < Console.BufferWidth && top < Console.BufferHeight)
                {
                    Console.SetCursorPosition(left, top);
                    Logger.LogTrace("Cursor position set to ({Left}, {Top})", left, top);
                }
                else
                {
                    Logger.LogWarning("Invalid cursor position ({Left}, {Top}), console size is {Width}x{Height}", 
                        left, top, Console.BufferWidth, Console.BufferHeight);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to set cursor position to ({Left}, {Top})", left, top);
            }
        }, cancellationToken);
    }

    public Task WriteTextAsync(string text, ConsoleColor? foreground = null, ConsoleColor? background = null, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            try
            {
                var oldForeground = Console.ForegroundColor;
                var oldBackground = Console.BackgroundColor;
                
                if (foreground.HasValue) Console.ForegroundColor = foreground.Value;
                if (background.HasValue) Console.BackgroundColor = background.Value;
                
                Console.Write(text);
                
                if (foreground.HasValue || background.HasValue)
                {
                    Console.ForegroundColor = oldForeground;
                    Console.BackgroundColor = oldBackground;
                }
                
                Logger.LogTrace("Wrote text: '{Text}' with colors F:{Foreground} B:{Background}", 
                    text, foreground, background);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to write text: '{Text}'", text);
            }
        }, cancellationToken);
    }

    public async Task WriteTextAtAsync(int left, int top, string text, ConsoleColor? foreground = null, ConsoleColor? background = null, CancellationToken cancellationToken = default)
    {
        await SetCursorPositionAsync(left, top, cancellationToken);
        await WriteTextAsync(text, foreground, background, cancellationToken);
    }

    public async Task DrawBorderAsync(Rectangle bounds, BorderStyle style = BorderStyle.Single, ConsoleColor? foreground = null, CancellationToken cancellationToken = default)
    {
        if (bounds.IsEmpty || style == BorderStyle.None) return;
        
        var (topLeft, topRight, bottomLeft, bottomRight, horizontal, vertical) = GetBorderChars(style);
        
        // Draw top border
        await WriteTextAtAsync(bounds.Left, bounds.Top, topLeft.ToString(), foreground, null, cancellationToken);
        if (bounds.Width > 2)
        {
            await WriteTextAsync(new string(horizontal, bounds.Width - 2), foreground, null, cancellationToken);
        }
        if (bounds.Width > 1)
        {
            await WriteTextAsync(topRight.ToString(), foreground, null, cancellationToken);
        }
        
        // Draw sides
        for (int y = bounds.Top + 1; y < bounds.Bottom; y++)
        {
            await WriteTextAtAsync(bounds.Left, y, vertical.ToString(), foreground, null, cancellationToken);
            if (bounds.Width > 1)
            {
                await WriteTextAtAsync(bounds.Right, y, vertical.ToString(), foreground, null, cancellationToken);
            }
        }
        
        // Draw bottom border
        if (bounds.Height > 1)
        {
            await WriteTextAtAsync(bounds.Left, bounds.Bottom, bottomLeft.ToString(), foreground, null, cancellationToken);
            if (bounds.Width > 2)
            {
                await WriteTextAsync(new string(horizontal, bounds.Width - 2), foreground, null, cancellationToken);
            }
            if (bounds.Width > 1)
            {
                await WriteTextAsync(bottomRight.ToString(), foreground, null, cancellationToken);
            }
        }
        
        Logger.LogTrace("Drew {Style} border at {Bounds}", style, bounds);
    }

    public Task FillRectangleAsync(Rectangle bounds, char fillChar = ' ', ConsoleColor? foreground = null, ConsoleColor? background = null, CancellationToken cancellationToken = default)
    {
        return Task.Run(async () =>
        {
            if (bounds.IsEmpty) return;
            
            var fillString = new string(fillChar, bounds.Width);
            
            for (int y = bounds.Top; y < bounds.Top + bounds.Height; y++)
            {
                await WriteTextAtAsync(bounds.Left, y, fillString, foreground, background, cancellationToken);
            }
            
            Logger.LogTrace("Filled rectangle {Bounds} with '{FillChar}'", bounds, fillChar);
        }, cancellationToken);
    }

    public Size GetConsoleSize()
    {
        try
        {
            return new Size(Console.BufferWidth, Console.BufferHeight);
        }
        catch
        {
            // Fallback size if console is not available
            return new Size(80, 25);
        }
    }

    private static (char topLeft, char topRight, char bottomLeft, char bottomRight, char horizontal, char vertical) GetBorderChars(BorderStyle style)
    {
        return style switch
        {
            BorderStyle.Single => ('┌', '┐', '└', '┘', '─', '│'),
            BorderStyle.Double => ('╔', '╗', '╚', '╝', '═', '║'),
            BorderStyle.Rounded => ('╭', '╮', '╰', '╯', '─', '│'),
            _ => ('┌', '┐', '└', '┘', '─', '│')
        };
    }

    private static void EnableWindowsAnsiSequences()
    {
        if (!OperatingSystem.IsWindows()) return;
        
        // This is a simplified approach - in a real implementation,
        // you might want to use Windows API calls to enable virtual terminal processing
        try
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
        }
        catch
        {
            // Ignore if we can't set encoding
        }
    }

    protected override async ValueTask OnDisposeAsync()
    {
        _consoleSizeChangedSubject.OnCompleted();
        _consoleSizeChangedSubject.Dispose();
        await base.OnDisposeAsync();
    }
}