using GameConsole.Core.Abstractions;
using GameConsole.UI.Core;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.UI.Services;

/// <summary>
/// Console rendering service for outputting UI components to the console.
/// Provides ANSI escape code support and efficient console drawing operations.
/// </summary>
[Service("Console Rendering Service", "1.0.0", "Console-based UI rendering with ANSI escape code support",
    Categories = new[] { "UI", "Console", "Rendering" })]
public sealed class ConsoleRenderingService : BaseUIService, IUIRenderer
{
    private readonly List<IUIComponent> _rootComponents = new();
    private readonly object _componentsLock = new();
    
    public ConsoleRenderingService(ILogger<ConsoleRenderingService> logger)
        : base(logger) { }

    #region BaseUIService Implementation

    public override IUIRenderer Renderer => this;

    public override bool SupportsColors => 
        Environment.GetEnvironmentVariable("NO_COLOR") == null && 
        !Console.IsOutputRedirected;

    protected override Task OnInitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Initializing console rendering");
        
        // Enable ANSI escape sequences on Windows
        if (OperatingSystem.IsWindows())
        {
            try
            {
                // Enable virtual terminal processing on Windows 10+
                var handle = GetStdHandle(-11); // STD_OUTPUT_HANDLE
                GetConsoleMode(handle, out uint mode);
                SetConsoleMode(handle, mode | 0x0004); // ENABLE_VIRTUAL_TERMINAL_PROCESSING
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not enable ANSI support on Windows");
            }
        }
        
        return Task.CompletedTask;
    }

    protected override Task OnStartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting console rendering");
        return Task.CompletedTask;
    }

    protected override Task OnStopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Stopping console rendering");
        return Task.CompletedTask;
    }

    public override Task RenderAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        
        lock (_componentsLock)
        {
            foreach (var component in _rootComponents)
            {
                if (component.Visibility == Visibility.Visible)
                {
                    RenderComponentRecursive(component);
                }
            }
        }
        
        return Task.CompletedTask;
    }

    public override Task ClearScreenAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        
        Console.Clear();
        return Task.CompletedTask;
    }

    public override Task AddComponentAsync(IUIComponent component, CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        
        lock (_componentsLock)
        {
            _rootComponents.Add(component);
        }
        
        return Task.CompletedTask;
    }

    public override Task RemoveComponentAsync(IUIComponent component, CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        
        lock (_componentsLock)
        {
            _rootComponents.Remove(component);
        }
        
        return Task.CompletedTask;
    }

    public override Task<Size> GetConsoleSize(CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        
        try
        {
            return Task.FromResult(new Size(Console.WindowWidth, Console.WindowHeight));
        }
        catch (IOException)
        {
            // Fallback if console dimensions aren't available
            return Task.FromResult(new Size(80, 25));
        }
    }

    #endregion

    #region IUIRenderer Implementation

    public Task RenderComponentAsync(IUIComponent component, CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        
        RenderComponentRecursive(component);
        return Task.CompletedTask;
    }

    public Task ClearAreaAsync(UIBounds bounds, CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        
        for (int y = bounds.Top; y <= bounds.Bottom; y++)
        {
            Console.SetCursorPosition(bounds.Left, y);
            Console.Write(new string(' ', bounds.Size.Width));
        }
        
        return Task.CompletedTask;
    }

    public Task SetCursorPositionAsync(Position position, CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        
        try
        {
            Console.SetCursorPosition(position.X, position.Y);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            _logger.LogWarning(ex, "Invalid cursor position: {Position}", position);
        }
        
        return Task.CompletedTask;
    }

    public Task RenderTextAsync(Position position, string text, ConsoleColor colors, CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        
        try
        {
            Console.SetCursorPosition(position.X, position.Y);
            
            if (SupportsColors)
            {
                Console.ForegroundColor = colors.Foreground;
                Console.BackgroundColor = colors.Background;
            }
            
            Console.Write(text);
            
            if (SupportsColors)
            {
                Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error rendering text at {Position}: {Text}", position, text);
        }
        
        return Task.CompletedTask;
    }

    #endregion

    #region Private Methods

    private void RenderComponentRecursive(IUIComponent component)
    {
        if (component.Visibility != Visibility.Visible) return;

        // Render the component itself
        if (component is ITextComponent textComponent)
        {
            RenderTextComponent(textComponent);
        }

        // Render child components
        foreach (var child in component.Children)
        {
            RenderComponentRecursive(child);
        }
    }

    private void RenderTextComponent(ITextComponent textComponent)
    {
        if (string.IsNullOrEmpty(textComponent.Text)) return;

        var position = CalculateTextPosition(textComponent);
        
        try
        {
            Console.SetCursorPosition(position.X, position.Y);
            
            if (SupportsColors)
            {
                Console.ForegroundColor = textComponent.Colors.Foreground;
                Console.BackgroundColor = textComponent.Colors.Background;
            }
            
            Console.Write(textComponent.Text);
            
            if (SupportsColors)
            {
                Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error rendering text component {ComponentId}", textComponent.Id);
        }
    }

    private Position CalculateTextPosition(ITextComponent textComponent)
    {
        var bounds = textComponent.Bounds;
        var textLength = textComponent.Text.Length;
        
        int x = textComponent.TextAlignment switch
        {
            TextAlignment.Left => bounds.Left,
            TextAlignment.Center => bounds.Left + (bounds.Size.Width - textLength) / 2,
            TextAlignment.Right => bounds.Right - textLength + 1,
            _ => bounds.Left
        };
        
        int y = textComponent.VerticalAlignment switch
        {
            VerticalAlignment.Top => bounds.Top,
            VerticalAlignment.Center => bounds.Top + bounds.Size.Height / 2,
            VerticalAlignment.Bottom => bounds.Bottom,
            _ => bounds.Top
        };
        
        return new Position(Math.Max(0, x), Math.Max(0, y));
    }

    #endregion

    #region Windows Console API (for ANSI support)

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    #endregion
}