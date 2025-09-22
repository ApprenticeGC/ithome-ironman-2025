using GameConsole.Core.Abstractions;
using GameConsole.Input.Services;
using Microsoft.Extensions.Logging;

namespace GameConsole.UI.Console;

/// <summary>
/// Main console UI service implementation.
/// </summary>
public class ConsoleUIService : IService, IAsyncDisposable
{
    private readonly ILogger<ConsoleUIService> _logger;
    private readonly GameConsole.Input.Services.IService _inputService;
    private bool _isRunning = false;
    private bool _disposed = false;
    
    private readonly IConsoleUIFramework _framework;
    private readonly IConsoleInputManager _inputManager;
    private readonly IConsoleLayoutManager _layoutManager;
    private readonly List<IUIComponent> _components = new();
    private readonly CancellationTokenSource _renderCancellation = new();
    
    public event EventHandler<ConsoleResizeEventArgs>? ScreenResize;
    
    public IConsoleUIFramework Framework => _framework;
    public IConsoleInputManager InputManager => _inputManager;
    public IConsoleLayoutManager LayoutManager => _layoutManager;
    public bool IsRunning => _isRunning;
    
    public ConsoleUIService(
        GameConsole.Input.Services.IService inputService,
        ILogger<ConsoleUIService> logger)
    {
        _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _framework = new ConsoleUIFramework();
        _inputManager = new ConsoleInputManager(_inputService, logger.CreateLogger<ConsoleInputManager>());
        _layoutManager = new ConsoleLayoutManager(_framework);
        
        // Hook up input events
        _inputManager.KeyPressed += OnInputManagerKeyPressed;
        
        // Monitor console resize events
        System.Console.CancelKeyPress += OnCancelKeyPress;
    }
    
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing {ServiceType}", GetType().Name);
        
        // Initialize input service if not already running
        if (!_inputService.IsRunning)
        {
            await _inputService.InitializeAsync(cancellationToken);
        }
        
        // Setup console
        _framework.CursorVisible = false;
        
        _logger.LogInformation("Initialized {ServiceType}", GetType().Name);
    }
    
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting {ServiceType}", GetType().Name);
        
        // Start input service if not already running
        if (!_inputService.IsRunning)
        {
            await _inputService.StartAsync(cancellationToken);
        }
        
        // Enable input capture
        _inputManager.SetInputCapture(true);
        
        // Start render loop
        _ = Task.Run(RenderLoopAsync, _renderCancellation.Token);
        
        _isRunning = true;
        _logger.LogInformation("Started {ServiceType}", GetType().Name);
    }
    
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping {ServiceType}", GetType().Name);
        
        _isRunning = false;
        
        // Stop render loop
        _renderCancellation.Cancel();
        
        // Disable input capture
        _inputManager.SetInputCapture(false);
        
        // Restore cursor
        _framework.CursorVisible = true;
        
        _logger.LogInformation("Stopped {ServiceType}", GetType().Name);
        await Task.CompletedTask;
    }
    
    public async Task RenderAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning || _disposed) return;
        
        try
        {
            // Calculate layout for all visible components
            var visibleComponents = _components.Where(c => c.IsVisible).ToList();
            _layoutManager.CalculateLayout(visibleComponents);
            
            // Render all components
            foreach (var component in visibleComponents)
            {
                cancellationToken.ThrowIfCancellationRequested();
                component.Render(_framework);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during UI rendering");
        }
        
        await Task.CompletedTask;
    }
    
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _framework.Clear();
        await Task.CompletedTask;
    }
    
    /// <summary>
    /// Adds a UI component to be managed by this service.
    /// </summary>
    /// <param name="component">Component to add.</param>
    public void AddComponent(IUIComponent component)
    {
        if (component == null) throw new ArgumentNullException(nameof(component));
        
        if (!_components.Contains(component))
        {
            _components.Add(component);
            _logger.LogDebug("Added UI component: {ComponentId}", component.Id);
        }
    }
    
    /// <summary>
    /// Removes a UI component from management.
    /// </summary>
    /// <param name="component">Component to remove.</param>
    /// <returns>True if removed; false if not found.</returns>
    public bool RemoveComponent(IUIComponent component)
    {
        if (component == null) return false;
        
        var removed = _components.Remove(component);
        if (removed)
        {
            _logger.LogDebug("Removed UI component: {ComponentId}", component.Id);
        }
        return removed;
    }
    
    /// <summary>
    /// Gets all components managed by this service.
    /// </summary>
    /// <returns>Read-only collection of components.</returns>
    public IReadOnlyList<IUIComponent> GetComponents()
    {
        return _components.AsReadOnly();
    }
    
    /// <summary>
    /// Finds a component by its ID.
    /// </summary>
    /// <param name="id">Component ID to find.</param>
    /// <returns>The component if found; null otherwise.</returns>
    public IUIComponent? FindComponent(string id)
    {
        return _components.FirstOrDefault(c => c.Id == id);
    }
    
    /// <summary>
    /// Finds a component by its type.
    /// </summary>
    /// <typeparam name="T">Type of component to find.</typeparam>
    /// <returns>The first component of the specified type; null if none found.</returns>
    public T? FindComponent<T>() where T : class, IUIComponent
    {
        return _components.OfType<T>().FirstOrDefault();
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        
        if (_isRunning)
        {
            await StopAsync();
        }
        
        _renderCancellation.Dispose();
        System.Console.CancelKeyPress -= OnCancelKeyPress;
        
        _disposed = true;
        GC.SuppressFinalize(this);
    }
    
    private async Task RenderLoopAsync()
    {
        const int targetFps = 30;
        const int frameTimeMs = 1000 / targetFps;
        
        while (_isRunning && !_renderCancellation.Token.IsCancellationRequested)
        {
            try
            {
                var frameStart = DateTime.UtcNow;
                
                await RenderAsync(_renderCancellation.Token);
                
                var frameTime = DateTime.UtcNow - frameStart;
                var remainingTime = TimeSpan.FromMilliseconds(frameTimeMs) - frameTime;
                
                if (remainingTime > TimeSpan.Zero)
                {
                    await Task.Delay(remainingTime, _renderCancellation.Token);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in render loop");
                await Task.Delay(100, _renderCancellation.Token); // Brief delay before retry
            }
        }
    }
    
    private void OnInputManagerKeyPressed(object? sender, UIKeyEventArgs e)
    {
        // Distribute input to focused component first, then to all components that can handle it
        var focusedComponent = _components.FirstOrDefault(c => c.HasFocus);
        
        if (focusedComponent?.HandleInput(e) == true)
        {
            return; // Input was handled by focused component
        }
        
        // Try other components
        foreach (var component in _components.Where(c => c.CanFocus && c != focusedComponent))
        {
            if (component.HandleInput(e))
            {
                break; // Input was handled
            }
        }
    }
    
    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        // Handle Ctrl+C gracefully
        e.Cancel = true; // Don't terminate the application immediately
        
        // Request shutdown
        _ = Task.Run(async () =>
        {
            if (_isRunning)
            {
                await StopAsync();
            }
        });
    }
}