using GameConsole.Core.Abstractions;
using GameConsole.UI.Core;
using Microsoft.Extensions.Logging;

namespace GameConsole.UI.Services;

/// <summary>
/// Main UI service that provides access to console UI capabilities.
/// </summary>
[Service("Console UI", "1.0.0", "Main console UI service providing comprehensive UI capabilities",
    Categories = new[] { "UI", "Console" }, Lifetime = ServiceLifetime.Singleton)]
public class ConsoleUIService : BaseUIService, IUIService
{
    private ConsoleRendererService? _consoleRenderer;
    private ConsoleWindowManagerService? _windowManager;
    private readonly ILoggerFactory _loggerFactory;

    public ConsoleUIService(ILogger<ConsoleUIService> logger, ILoggerFactory? loggerFactory = null) : base(logger)
    {
        _loggerFactory = loggerFactory ?? new LoggerFactoryStub();
    }

    public IConsoleRenderer? ConsoleRenderer => _consoleRenderer;
    public IWindowManager? WindowManager => _windowManager;
    public ILayoutManager? LayoutManager => null; // TODO: Implement layout manager

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Initializing console UI service components");

        // Create and initialize console renderer
        var rendererLogger = _loggerFactory.CreateLogger<ConsoleRendererService>();
        _consoleRenderer = new ConsoleRendererService(rendererLogger);
        await _consoleRenderer.InitializeAsync(cancellationToken);

        // Create and initialize window manager
        var windowManagerLogger = _loggerFactory.CreateLogger<ConsoleWindowManagerService>();
        _windowManager = new ConsoleWindowManagerService(windowManagerLogger);
        await _windowManager.InitializeAsync(cancellationToken);

        Logger.LogDebug("Console UI service components initialized");
    }

    protected override async Task OnStartAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Starting console UI service components");

        if (_consoleRenderer != null)
        {
            await _consoleRenderer.StartAsync(cancellationToken);
        }

        if (_windowManager != null)
        {
            await _windowManager.StartAsync(cancellationToken);
        }

        Logger.LogDebug("Console UI service components started");
    }

    protected override async Task OnStopAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Stopping console UI service components");

        if (_windowManager != null)
        {
            await _windowManager.StopAsync(cancellationToken);
        }

        if (_consoleRenderer != null)
        {
            await _consoleRenderer.StopAsync(cancellationToken);
        }

        Logger.LogDebug("Console UI service components stopped");
    }

    protected override async ValueTask OnDisposeAsync()
    {
        if (_windowManager != null)
        {
            await _windowManager.DisposeAsync();
        }

        if (_consoleRenderer != null)
        {
            await _consoleRenderer.DisposeAsync();
        }

        await base.OnDisposeAsync();
    }

    /// <summary>
    /// Create a simple demo window to test the UI system.
    /// </summary>
    public async Task<IWindow> CreateDemoWindowAsync(CancellationToken cancellationToken = default)
    {
        if (_windowManager == null || _consoleRenderer == null)
        {
            throw new InvalidOperationException("UI service must be initialized before creating windows");
        }

        var window = await _windowManager.CreateWindowAsync("Demo Window", 
            new Rectangle(5, 2, 50, 15), cancellationToken);

        // Add some demo components
        var titleLabel = new ConsoleLabel("title", "Console UI Demo")
        {
            Bounds = new Rectangle(2, 1, 20, 1),
            ForegroundColor = ConsoleColor.Yellow,
            TextAlignment = HorizontalAlignment.Center
        };

        var button1 = new ConsoleButton("btn1", "Click Me!")
        {
            Bounds = new Rectangle(2, 3, 12, 3),
            ForegroundColor = ConsoleColor.White,
            BackgroundColor = ConsoleColor.DarkGreen
        };

        var button2 = new ConsoleButton("btn2", "Exit")
        {
            Bounds = new Rectangle(16, 3, 10, 3),
            ForegroundColor = ConsoleColor.White,
            BackgroundColor = ConsoleColor.DarkRed
        };

        var infoLabel = new ConsoleLabel("info", 
            "Use TAB to navigate, ENTER to click buttons, ESC to close")
        {
            Bounds = new Rectangle(2, 7, 44, 2),
            WordWrap = true,
            ForegroundColor = ConsoleColor.Cyan
        };

        // Add event handlers
        button1.Clicked.Subscribe(_ => 
        {
            Logger.LogInformation("Demo button clicked!");
        });

        button2.Clicked.Subscribe(async _ => 
        {
            await window.CloseAsync();
        });

        // Add components to window
        await window.AddComponentAsync(titleLabel, cancellationToken);
        await window.AddComponentAsync(button1, cancellationToken);
        await window.AddComponentAsync(button2, cancellationToken);
        await window.AddComponentAsync(infoLabel, cancellationToken);

        return window;
    }

    /// <summary>
    /// Run a simple UI demo loop.
    /// </summary>
    public async Task RunDemoAsync(CancellationToken cancellationToken = default)
    {
        if (_windowManager == null || _consoleRenderer == null)
        {
            throw new InvalidOperationException("UI service must be initialized and started before running demo");
        }

        Logger.LogInformation("Starting Console UI Demo");

        // Clear screen and create demo window
        await _consoleRenderer.ClearAsync(cancellationToken);
        var demoWindow = await CreateDemoWindowAsync(cancellationToken);

        // Set focus to first button
        var components = demoWindow.GetComponents();
        var firstButton = components.FirstOrDefault(c => c.CanFocus);
        if (firstButton is BaseUIComponent baseComponent)
        {
            baseComponent.SetFocus(true);
        }

        Console.WriteLine("Console UI Demo Started");
        Console.WriteLine("Use TAB to navigate, ENTER/SPACE to click buttons, ESC to close window");
        Console.WriteLine("Press Ctrl+C to exit demo");
        Console.WriteLine();

        try
        {
            while (!cancellationToken.IsCancellationRequested && _windowManager.GetWindows().Count > 0)
            {
                // Render all windows
                await _windowManager.RenderAllWindowsAsync(_consoleRenderer, cancellationToken);

                // Handle keyboard input
                if (Console.KeyAvailable)
                {
                    var keyInfo = Console.ReadKey(true);
                    var keyEvent = new KeyEvent(keyInfo.Key, keyInfo.Modifiers, keyInfo.KeyChar);
                    
                    await _windowManager.HandleGlobalEventAsync(keyEvent, cancellationToken);
                }

                // Small delay to prevent high CPU usage
                await Task.Delay(50, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelled
        }

        Logger.LogInformation("Console UI Demo ended");
    }
}

/// <summary>
/// Simple stub logger factory for when no factory is provided.
/// </summary>
internal class LoggerFactoryStub : ILoggerFactory
{
    public void AddProvider(ILoggerProvider provider) { }
    
    public ILogger CreateLogger(string categoryName)
    {
        return new LoggerStub();
    }
    
    public void Dispose() { }
}

/// <summary>
/// Simple stub logger that does nothing.
/// </summary>
internal class LoggerStub : ILogger
{
    public IDisposable BeginScope<TState>(TState state) where TState : notnull => new DisposableStub();
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}

/// <summary>
/// Simple stub disposable.
/// </summary>
internal class DisposableStub : IDisposable
{
    public void Dispose() { }
}