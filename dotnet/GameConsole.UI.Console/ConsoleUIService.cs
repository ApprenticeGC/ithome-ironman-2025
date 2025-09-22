using GameConsole.UI.Core;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.UI.Console;

/// <summary>
/// Console-based implementation of the UI service for text-based user interfaces.
/// Provides TUI capabilities using System.Console for rendering and input handling.
/// </summary>
public class ConsoleUIService : IService, IAdvancedTextRenderingCapability, IComponentManagementCapability
{
    private readonly ILogger<ConsoleUIService> _logger;
    private readonly ConcurrentDictionary<string, IUIComponent> _components;
    private readonly object _consoleLock = new object();
    private Size _currentSize;
    private bool _isRunning;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the ConsoleUIService class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ConsoleUIService(ILogger<ConsoleUIService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _components = new ConcurrentDictionary<string, IUIComponent>();
        _currentSize = new Size(System.Console.WindowWidth, System.Console.WindowHeight);
    }

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public event EventHandler<SizeChangedEventArgs>? SizeChanged;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing ConsoleUIService");
        
        lock (_consoleLock)
        {
            // Initialize console settings
            System.Console.CursorVisible = false;
            System.Console.Clear();
            _currentSize = new Size(System.Console.WindowWidth, System.Console.WindowHeight);
        }

        _logger.LogInformation("Initialized ConsoleUIService with size {Width}x{Height}", _currentSize.Width, _currentSize.Height);
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting ConsoleUIService");
        _isRunning = true;
        
        // Start monitoring for console size changes
        _ = Task.Run(async () => await MonitorSizeChangesAsync(cancellationToken), cancellationToken);
        
        _logger.LogInformation("Started ConsoleUIService");
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping ConsoleUIService");
        _isRunning = false;
        
        lock (_consoleLock)
        {
            System.Console.CursorVisible = true;
            System.Console.ResetColor();
        }
        
        _logger.LogInformation("Stopped ConsoleUIService");
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task RenderTextAsync(string text, Position position, TextStyle? style = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(text)) return;
        
        lock (_consoleLock)
        {
            if (!IsValidPosition(position)) return;
            
            try
            {
                System.Console.SetCursorPosition(position.X, position.Y);
                
                if (style != null)
                {
                    if (style.ForegroundColor.HasValue)
                        System.Console.ForegroundColor = style.ForegroundColor.Value;
                    if (style.BackgroundColor.HasValue)
                        System.Console.BackgroundColor = style.BackgroundColor.Value;
                }
                
                System.Console.Write(text);
                
                if (style != null)
                {
                    System.Console.ResetColor();
                }
            }
            catch (ArgumentOutOfRangeException ex)
            {
                _logger.LogWarning(ex, "Failed to render text at position {X}, {Y}", position.X, position.Y);
            }
        }
        
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        lock (_consoleLock)
        {
            System.Console.Clear();
        }
        
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task ClearAreaAsync(Rectangle bounds, CancellationToken cancellationToken = default)
    {
        lock (_consoleLock)
        {
            var clearString = new string(' ', bounds.Size.Width);
            
            for (int y = bounds.Top; y <= bounds.Bottom && y < _currentSize.Height; y++)
            {
                if (bounds.Left >= 0 && bounds.Left < _currentSize.Width)
                {
                    try
                    {
                        System.Console.SetCursorPosition(bounds.Left, y);
                        System.Console.Write(clearString);
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        _logger.LogWarning(ex, "Failed to clear area at {Left}, {Y}", bounds.Left, y);
                        break;
                    }
                }
            }
        }
        
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<Size> GetSurfaceSizeAsync(CancellationToken cancellationToken = default)
    {
        lock (_consoleLock)
        {
            _currentSize = new Size(System.Console.WindowWidth, System.Console.WindowHeight);
            return Task.FromResult(_currentSize);
        }
    }

    /// <inheritdoc />
    public async Task SetCursorPositionAsync(Position position, CancellationToken cancellationToken = default)
    {
        lock (_consoleLock)
        {
            if (!IsValidPosition(position)) return;
            
            try
            {
                System.Console.SetCursorPosition(position.X, position.Y);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                _logger.LogWarning(ex, "Failed to set cursor position to {X}, {Y}", position.X, position.Y);
            }
        }
        
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task RenderColoredTextAsync(string text, Position position, ConsoleColor? foregroundColor = null, 
        ConsoleColor? backgroundColor = null, TextAttributes attributes = TextAttributes.None, 
        CancellationToken cancellationToken = default)
    {
        var style = new TextStyle(foregroundColor, backgroundColor, attributes);
        await RenderTextAsync(text, position, style, cancellationToken);
    }

    /// <inheritdoc />
    public Task AddComponentAsync(IUIComponent component, CancellationToken cancellationToken = default)
    {
        if (component == null) throw new ArgumentNullException(nameof(component));
        
        _components.AddOrUpdate(component.Id, component, (key, existing) => component);
        _logger.LogDebug("Added UI component {ComponentId}", component.Id);
        
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveComponentAsync(string componentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(componentId)) return Task.CompletedTask;
        
        _components.TryRemove(componentId, out _);
        _logger.LogDebug("Removed UI component {ComponentId}", componentId);
        
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task RenderAllComponentsAsync(CancellationToken cancellationToken = default)
    {
        foreach (var component in _components.Values)
        {
            if (component.IsVisible)
            {
                await component.RenderAsync(this, cancellationToken);
            }
        }
    }

    /// <inheritdoc />
    public Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Type>>(new[] { typeof(IAdvancedTextRenderingCapability), typeof(IComponentManagementCapability) });
    }

    /// <inheritdoc />
    public Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        var hasCapability = typeof(T) == typeof(IAdvancedTextRenderingCapability) || 
                           typeof(T) == typeof(IComponentManagementCapability);
        return Task.FromResult(hasCapability);
    }

    /// <inheritdoc />
    public Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (typeof(T) == typeof(IAdvancedTextRenderingCapability) || 
            typeof(T) == typeof(IComponentManagementCapability))
        {
            return Task.FromResult(this as T);
        }
        return Task.FromResult<T?>(null);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        
        await StopAsync();
        _isDisposed = true;
        
        GC.SuppressFinalize(this);
    }

    private bool IsValidPosition(Position position)
    {
        return position.X >= 0 && position.X < _currentSize.Width && 
               position.Y >= 0 && position.Y < _currentSize.Height;
    }

    private async Task MonitorSizeChangesAsync(CancellationToken cancellationToken)
    {
        var lastSize = _currentSize;
        
        while (_isRunning && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(500, cancellationToken); // Check every 500ms
                
                Size newSize;
                lock (_consoleLock)
                {
                    newSize = new Size(System.Console.WindowWidth, System.Console.WindowHeight);
                }
                
                if (!newSize.Equals(lastSize))
                {
                    _currentSize = newSize;
                    SizeChanged?.Invoke(this, new SizeChangedEventArgs(lastSize, newSize));
                    _logger.LogDebug("Console size changed from {OldWidth}x{OldHeight} to {NewWidth}x{NewHeight}", 
                        lastSize.Width, lastSize.Height, newSize.Width, newSize.Height);
                    lastSize = newSize;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring console size changes");
                await Task.Delay(1000, cancellationToken); // Wait longer on error
            }
        }
    }
}