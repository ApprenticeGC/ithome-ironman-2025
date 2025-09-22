using GameConsole.Core.Abstractions;

namespace GameConsole.UI.Console;

/// <summary>
/// Console UI framework service providing rich console UI components and text-based rendering.
/// Implements the main service interface for the console UI system.
/// </summary>
public class ConsoleUIFramework : IService
{
    private readonly TerminalInfo _terminalInfo;
    private readonly ConsoleLayoutManager _layoutManager;
    private readonly ConsoleInputManager _inputManager;
    private ConsoleBuffer? _currentBuffer;
    private ILayoutContainer? _rootContainer;
    private bool _isRunning;
    
    /// <summary>
    /// Initializes a new console UI framework.
    /// </summary>
    public ConsoleUIFramework()
    {
        _terminalInfo = TerminalInfo.Detect();
        _layoutManager = new ConsoleLayoutManager(_terminalInfo);
        _inputManager = new ConsoleInputManager(_terminalInfo);
        
        // Setup basic input handlers
        _inputManager.RegisterKeyHandler(GameConsole.Input.Core.KeyCode.LeftControl, 
            args => { if (args.Character == 'c') Environment.Exit(0); });
    }
    
    /// <summary>
    /// Gets the terminal information.
    /// </summary>
    public TerminalInfo TerminalInfo => _terminalInfo;
    
    /// <summary>
    /// Gets the layout manager.
    /// </summary>
    public ConsoleLayoutManager LayoutManager => _layoutManager;
    
    /// <summary>
    /// Gets the input manager.
    /// </summary>
    public ConsoleInputManager InputManager => _inputManager;
    
    /// <summary>
    /// Gets the current console buffer.
    /// </summary>
    public ConsoleBuffer? CurrentBuffer => _currentBuffer;
    
    /// <summary>
    /// Gets or sets the root layout container.
    /// </summary>
    public ILayoutContainer? RootContainer
    {
        get => _rootContainer;
        set => _rootContainer = value;
    }
    
    /// <inheritdoc />
    public bool IsRunning => _isRunning;
    
    /// <summary>
    /// Event raised when the terminal is resized.
    /// </summary>
    public event EventHandler<ConsoleSize>? TerminalResized;
    
    /// <summary>
    /// Event raised before rendering.
    /// </summary>
    public event EventHandler? PreRender;
    
    /// <summary>
    /// Event raised after rendering.
    /// </summary>
    public event EventHandler? PostRender;
    
    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        
        // Initialize console settings
        if (_terminalInfo.SupportsColor)
        {
            try
            {
                System.Console.OutputEncoding = System.Text.Encoding.UTF8;
            }
            catch
            {
                // Ignore encoding errors
            }
        }
        
        // Create initial buffer
        UpdateBuffer();
        
        // Setup terminal resize monitoring (if supported)
        SetupTerminalResizeMonitoring();
    }
    
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        
        if (_isRunning) return;
        
        _isRunning = true;
        
        // Start input capture
        _inputManager.StartCapture();
        
        // Clear screen and hide cursor
        ClearScreen();
        HideCursor();
    }
    
    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        
        if (!_isRunning) return;
        
        _isRunning = false;
        
        // Stop input capture
        _inputManager.StopCapture();
        
        // Show cursor and reset console
        ShowCursor();
        ResetConsole();
    }
    
    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isRunning)
        {
            await StopAsync();
        }
        
        GC.SuppressFinalize(this);
    }
    
    /// <summary>
    /// Creates a new menu component.
    /// </summary>
    /// <param name="title">Menu title.</param>
    /// <param name="selectedColor">Color for selected items.</param>
    /// <returns>A new console menu.</returns>
    public ConsoleMenu CreateMenu(string title = "", string selectedColor = ANSIEscapeSequences.FgBrightYellow)
    {
        return new ConsoleMenu(title, selectedColor: selectedColor);
    }
    
    /// <summary>
    /// Creates a new table component.
    /// </summary>
    /// <typeparam name="T">The data type for table rows.</typeparam>
    /// <param name="title">Table title.</param>
    /// <returns>A new console table.</returns>
    public ConsoleTable<T> CreateTable<T>(string title = "") where T : class
    {
        return new ConsoleTable<T>(title);
    }
    
    /// <summary>
    /// Creates a new progress bar component.
    /// </summary>
    /// <param name="label">Progress bar label.</param>
    /// <param name="style">Progress bar style.</param>
    /// <returns>A new console progress bar.</returns>
    public ConsoleProgressBar CreateProgressBar(string label = "", ProgressBarStyle style = ProgressBarStyle.Block)
    {
        return new ConsoleProgressBar(label, style);
    }
    
    /// <summary>
    /// Creates a new multi-progress bar component.
    /// </summary>
    /// <param name="title">Multi-progress bar title.</param>
    /// <returns>A new console multi-progress bar.</returns>
    public ConsoleMultiProgressBar CreateMultiProgressBar(string title = "")
    {
        return new ConsoleMultiProgressBar(title);
    }
    
    /// <summary>
    /// Updates the current buffer size to match terminal size.
    /// </summary>
    public void UpdateBuffer()
    {
        _terminalInfo.UpdateSize();
        var size = _terminalInfo.Size;
        
        if (_currentBuffer?.Size != size)
        {
            _currentBuffer = new ConsoleBuffer(Math.Max(1, size.Width), Math.Max(1, size.Height));
            TerminalResized?.Invoke(this, size);
        }
    }
    
    /// <summary>
    /// Renders the current layout to the console.
    /// </summary>
    public void Render()
    {
        if (!_isRunning || _currentBuffer == null) return;
        
        PreRender?.Invoke(this, EventArgs.Empty);
        
        try
        {
            // Clear the buffer
            _currentBuffer.Clear();
            
            // Render root container if available
            if (_rootContainer != null)
            {
                var bounds = new ConsoleRect(0, 0, _currentBuffer.Width, _currentBuffer.Height);
                _layoutManager.CalculateLayout(_rootContainer);
                _rootContainer.Render(_currentBuffer, bounds);
            }
            
            // Output the buffer to console
            var output = _currentBuffer.Render();
            
            // Move cursor to home and output
            System.Console.SetCursorPosition(0, 0);
            System.Console.Write(output);
        }
        catch (Exception)
        {
            // Handle rendering errors gracefully
        }
        
        PostRender?.Invoke(this, EventArgs.Empty);
    }
    
    /// <summary>
    /// Renders a specific layout element to the console.
    /// </summary>
    /// <param name="layout">The layout element to render.</param>
    /// <param name="clearScreen">Whether to clear the screen first.</param>
    public void Render(ILayout layout, bool clearScreen = true)
    {
        if (!_isRunning || _currentBuffer == null || layout == null) return;
        
        if (clearScreen)
        {
            _currentBuffer.Clear();
            ClearScreen();
        }
        
        var bounds = new ConsoleRect(0, 0, _currentBuffer.Width, _currentBuffer.Height);
        var arrangeRect = layout.Arrange(bounds);
        layout.Render(_currentBuffer, arrangeRect);
        
        var output = _currentBuffer.Render();
        System.Console.SetCursorPosition(0, 0);
        System.Console.Write(output);
    }
    
    /// <summary>
    /// Shows a message box with the specified text and optional title.
    /// </summary>
    /// <param name="text">The message text.</param>
    /// <param name="title">Optional title.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task ShowMessageBoxAsync(string text, string title = "Message", CancellationToken cancellationToken = default)
    {
        var textElement = _layoutManager.CreateTextElement(text, LayoutAlignment.Center);
        var borderElement = _layoutManager.CreateBorderElement(textElement, title);
        var container = _layoutManager.CreateVerticalContainer(padding: new LayoutSpacing(2));
        container.AddChild(borderElement);
        
        var centerRect = _layoutManager.CenterRect(container.GetDesiredSize(_terminalInfo.Size));
        
        // Store current buffer content
        var originalBuffer = new ConsoleBuffer(_currentBuffer!.Width, _currentBuffer!.Height);
        originalBuffer.CopyFrom(_currentBuffer, new ConsoleRect(0, 0, _currentBuffer.Width, _currentBuffer.Height), ConsolePoint.Origin);
        
        // Render message box
        container.Render(_currentBuffer, centerRect);
        var output = _currentBuffer.Render();
        
        System.Console.SetCursorPosition(0, 0);
        System.Console.Write(output);
        
        // Wait for key press
        await _inputManager.ReadKeyAsync("\nPress any key to continue...", cancellationToken);
        
        // Restore original buffer
        _currentBuffer.CopyFrom(originalBuffer, new ConsoleRect(0, 0, originalBuffer.Width, originalBuffer.Height), ConsolePoint.Origin);
        output = _currentBuffer.Render();
        System.Console.SetCursorPosition(0, 0);
        System.Console.Write(output);
    }
    
    /// <summary>
    /// Shows an input dialog with the specified prompt.
    /// </summary>
    /// <param name="prompt">The input prompt.</param>
    /// <param name="title">Optional title.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The input text, or null if cancelled.</returns>
    public async Task<string?> ShowInputDialogAsync(string prompt, string title = "Input", CancellationToken cancellationToken = default)
    {
        var textElement = _layoutManager.CreateTextElement(prompt);
        var borderElement = _layoutManager.CreateBorderElement(textElement, title);
        var container = _layoutManager.CreateVerticalContainer(padding: new LayoutSpacing(2));
        container.AddChild(borderElement);
        
        var centerRect = _layoutManager.CenterRect(container.GetDesiredSize(_terminalInfo.Size));
        
        // Render input dialog
        container.Render(_currentBuffer!, centerRect);
        var output = _currentBuffer!.Render();
        
        System.Console.SetCursorPosition(0, 0);
        System.Console.Write(output);
        
        // Position cursor for input
        System.Console.SetCursorPosition(centerRect.X + 2, centerRect.Bottom - 2);
        System.Console.Write("Input: ");
        
        return await _inputManager.ReadLineAsync("", cancellationToken);
    }
    
    /// <summary>
    /// Clears the console screen.
    /// </summary>
    public void ClearScreen()
    {
        if (_terminalInfo.SupportsColor)
        {
            System.Console.Write(ANSIEscapeSequences.ClearScreen + ANSIEscapeSequences.CursorHome);
        }
        else
        {
            System.Console.Clear();
        }
    }
    
    /// <summary>
    /// Hides the console cursor.
    /// </summary>
    public void HideCursor()
    {
        if (_terminalInfo.SupportsColor)
        {
            System.Console.Write(ANSIEscapeSequences.CursorHide);
        }
        else
        {
            try
            {
                System.Console.CursorVisible = false;
            }
            catch
            {
                // Ignore if not supported
            }
        }
    }
    
    /// <summary>
    /// Shows the console cursor.
    /// </summary>
    public void ShowCursor()
    {
        if (_terminalInfo.SupportsColor)
        {
            System.Console.Write(ANSIEscapeSequences.CursorShow);
        }
        else
        {
            try
            {
                System.Console.CursorVisible = true;
            }
            catch
            {
                // Ignore if not supported
            }
        }
    }
    
    /// <summary>
    /// Resets console formatting.
    /// </summary>
    public void ResetConsole()
    {
        if (_terminalInfo.SupportsColor)
        {
            System.Console.Write(ANSIEscapeSequences.Reset);
        }
        
        try
        {
            System.Console.ResetColor();
        }
        catch
        {
            // Ignore if not supported
        }
    }
    
    private void SetupTerminalResizeMonitoring()
    {
        // Monitor terminal size changes (basic implementation)
        Task.Run(async () =>
        {
            var lastSize = _terminalInfo.Size;
            
            while (_isRunning)
            {
                try
                {
                    await Task.Delay(1000); // Check every second
                    
                    _terminalInfo.UpdateSize();
                    var currentSize = _terminalInfo.Size;
                    
                    if (currentSize != lastSize)
                    {
                        UpdateBuffer();
                        lastSize = currentSize;
                    }
                }
                catch
                {
                    // Continue monitoring despite errors
                }
            }
        });
    }
}