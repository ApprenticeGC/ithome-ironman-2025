using GameConsole.Core.Abstractions;
using GameConsole.UI.Core;
using Microsoft.Extensions.Logging;

namespace GameConsole.UI.Services;

/// <summary>
/// Console-based UI service implementation providing TUI (Text-based User Interface) functionality.
/// </summary>
public class ConsoleUIService : IService
{
    private readonly ILogger<ConsoleUIService> _logger;
    private readonly Dictionary<string, UIElement> _elements = new();
    private readonly object _lockObject = new();
    private string? _focusedElementId;
    private bool _isRunning;
    private ConsoleColor _originalForeground;
    private ConsoleColor _originalBackground;

    /// <summary>
    /// Initializes a new instance of the ConsoleUIService.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public ConsoleUIService(ILogger<ConsoleUIService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _originalForeground = Console.ForegroundColor;
        _originalBackground = Console.BackgroundColor;
    }

    #region IService Implementation

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing ConsoleUIService");
        
        try
        {
            // Initialize console settings
            Console.CursorVisible = false;
            Console.Clear();
            
            _logger.LogInformation("ConsoleUIService initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize ConsoleUIService");
            throw;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting ConsoleUIService");
        _isRunning = true;
        _logger.LogInformation("ConsoleUIService started successfully");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping ConsoleUIService");
        _isRunning = false;
        
        // Restore original console colors
        Console.ForegroundColor = _originalForeground;
        Console.BackgroundColor = _originalBackground;
        Console.CursorVisible = true;
        
        _logger.LogInformation("ConsoleUIService stopped successfully");
        return Task.CompletedTask;
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

    #endregion

    #region UI Service Implementation

    /// <summary>
    /// Begins a new UI render frame.
    /// </summary>
    public Task BeginRenderAsync(CancellationToken cancellationToken = default)
    {
        // For console rendering, we don't need frame-based rendering
        return Task.CompletedTask;
    }

    /// <summary>
    /// Ends the current UI render frame and presents to console.
    /// </summary>
    public Task EndRenderAsync(CancellationToken cancellationToken = default)
    {
        // Flush console output
        Console.Out.Flush();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears the console screen.
    /// </summary>
    public Task ClearScreenAsync(CancellationToken cancellationToken = default)
    {
        Console.Clear();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates a new UI element and adds it to the UI hierarchy.
    /// </summary>
    public Task CreateElementAsync<T>(T element, CancellationToken cancellationToken = default) where T : UIElement
    {
        if (element == null) throw new ArgumentNullException(nameof(element));

        lock (_lockObject)
        {
            _elements[element.Id] = element;
            _logger.LogDebug("Created UI element: {ElementId} of type {ElementType}", element.Id, typeof(T).Name);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Updates an existing UI element.
    /// </summary>
    public Task UpdateElementAsync(string elementId, UIElement element, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(elementId)) throw new ArgumentException("Element ID cannot be null or empty", nameof(elementId));
        if (element == null) throw new ArgumentNullException(nameof(element));

        lock (_lockObject)
        {
            if (_elements.ContainsKey(elementId))
            {
                _elements[elementId] = element;
                _logger.LogDebug("Updated UI element: {ElementId}", elementId);
            }
            else
            {
                _logger.LogWarning("Attempted to update non-existent element: {ElementId}", elementId);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes a UI element from the hierarchy.
    /// </summary>
    public Task RemoveElementAsync(string elementId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(elementId)) throw new ArgumentException("Element ID cannot be null or empty", nameof(elementId));

        lock (_lockObject)
        {
            if (_elements.Remove(elementId))
            {
                _logger.LogDebug("Removed UI element: {ElementId}", elementId);
                
                // Clear focus if this element was focused
                if (_focusedElementId == elementId)
                {
                    _focusedElementId = null;
                }
            }
            else
            {
                _logger.LogWarning("Attempted to remove non-existent element: {ElementId}", elementId);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets a UI element by its ID.
    /// </summary>
    public Task<UIElement?> GetElementAsync(string elementId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(elementId)) return Task.FromResult<UIElement?>(null);

        lock (_lockObject)
        {
            _elements.TryGetValue(elementId, out var element);
            return Task.FromResult(element);
        }
    }

    /// <summary>
    /// Sets focus to a specific UI element.
    /// </summary>
    public Task SetFocusAsync(string elementId, CancellationToken cancellationToken = default)
    {
        lock (_lockObject)
        {
            var previousFocusedId = _focusedElementId;
            
            if (_elements.ContainsKey(elementId) && _elements[elementId].CanFocus)
            {
                _focusedElementId = elementId;
                _logger.LogDebug("Focus set to element: {ElementId}", elementId);

                // Raise focus changed event
                OnFocusChanged(new UIFocusEvent
                {
                    ElementId = elementId,
                    PreviousElementId = previousFocusedId,
                    Timestamp = DateTime.UtcNow
                });
            }
            else
            {
                _logger.LogWarning("Attempted to focus non-existent or non-focusable element: {ElementId}", elementId);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the currently focused element ID.
    /// </summary>
    public Task<string?> GetFocusedElementAsync(CancellationToken cancellationToken = default)
    {
        lock (_lockObject)
        {
            return Task.FromResult(_focusedElementId);
        }
    }

    /// <summary>
    /// Processes input for UI interactions.
    /// </summary>
    public Task ProcessInputAsync(UIInputData inputData, CancellationToken cancellationToken = default)
    {
        if (inputData == null) return Task.CompletedTask;

        _logger.LogDebug("Processing UI input: {InputType}", inputData.InputType);

        // Handle basic navigation and interaction
        switch (inputData.InputType)
        {
            case UIInputType.KeyPress:
                ProcessKeyInput(inputData);
                break;
            case UIInputType.Character:
                ProcessCharacterInput(inputData);
                break;
            case UIInputType.MouseClick:
                ProcessMouseInput(inputData);
                break;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the current console size.
    /// </summary>
    public Task<UISize> GetConsoleSizeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var size = new UISize(Console.WindowWidth, Console.WindowHeight);
            return Task.FromResult(size);
        }
        catch (IOException)
        {
            // Return default size if console is not available
            return Task.FromResult(new UISize(80, 25));
        }
    }

    /// <summary>
    /// Sets console colors for rendering.
    /// </summary>
    public Task SetConsoleColorsAsync(UIColor foreground, UIColor background, CancellationToken cancellationToken = default)
    {
        Console.ForegroundColor = ConvertToConsoleColor(foreground);
        Console.BackgroundColor = ConvertToConsoleColor(background);
        return Task.CompletedTask;
    }

    #endregion

    #region Events

    public event EventHandler<UIFocusEvent>? FocusChanged;
    public event EventHandler<UIActivationEvent>? ElementActivated;
    public event EventHandler<UILayoutEvent>? LayoutChanged;
    public event EventHandler<UITextInputEvent>? TextInput;
    public event EventHandler<UIStateChangeEvent>? StateChanged;

    #endregion

    #region Capability Properties

    public IThemeManagerCapability? ThemeManager => null; // Not implemented in basic version
    public ILayoutManagerCapability? LayoutManager => null; // Not implemented in basic version
    public IAnimationCapability? AnimationManager => null; // Not implemented in basic version

    #endregion

    #region Private Helper Methods

    private void ProcessKeyInput(UIInputData inputData)
    {
        if (inputData.Key == null) return;

        // Handle special keys for navigation
        switch (inputData.Key.Value)
        {
            case ConsoleKey.Tab:
                NavigateToNextFocusableElement();
                break;
            case ConsoleKey.Enter:
                ActivateFocusedElement();
                break;
            case ConsoleKey.Escape:
                // Handle escape key
                break;
        }
    }

    private void ProcessCharacterInput(UIInputData inputData)
    {
        if (inputData.Character == null || _focusedElementId == null) return;

        // Send text input to focused element
        OnTextInput(new UITextInputEvent
        {
            Text = inputData.Character?.ToString() ?? string.Empty,
            ElementId = _focusedElementId ?? string.Empty,
            Timestamp = DateTime.UtcNow
        });
    }

    private void ProcessMouseInput(UIInputData inputData)
    {
        if (inputData.MousePosition == null) return;

        // Find element at mouse position and activate it
        var elementAtPosition = FindElementAtPosition(inputData.MousePosition.Value);
        if (elementAtPosition != null)
        {
            OnElementActivated(new UIActivationEvent
            {
                ElementId = elementAtPosition.Id,
                Method = UIActivationMethod.Mouse,
                Position = inputData.MousePosition,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    private void NavigateToNextFocusableElement()
    {
        lock (_lockObject)
        {
            var focusableElements = _elements.Values.Where(e => e.CanFocus).ToList();
            if (focusableElements.Count == 0) return;

            var currentIndex = _focusedElementId != null 
                ? focusableElements.FindIndex(e => e.Id == _focusedElementId) 
                : -1;
            
            var nextIndex = (currentIndex + 1) % focusableElements.Count;
            _ = SetFocusAsync(focusableElements[nextIndex].Id);
        }
    }

    private void ActivateFocusedElement()
    {
        if (_focusedElementId == null) return;

        OnElementActivated(new UIActivationEvent
        {
            ElementId = _focusedElementId,
            Method = UIActivationMethod.Keyboard,
            Timestamp = DateTime.UtcNow
        });
    }

    private UIElement? FindElementAtPosition(UIPosition position)
    {
        lock (_lockObject)
        {
            // Simple implementation: return the first visible element that contains the position
            return _elements.Values
                .Where(e => e.IsVisible)
                .FirstOrDefault(e => IsPositionInElement(position, e));
        }
    }

    private static bool IsPositionInElement(UIPosition position, UIElement element)
    {
        var rect = new UIRect(element.Position, element.Size);
        return position.X >= rect.Left && position.X < rect.Right &&
               position.Y >= rect.Top && position.Y < rect.Bottom;
    }

    private static ConsoleColor ConvertToConsoleColor(UIColor color)
    {
        return color switch
        {
            UIColor.Black => ConsoleColor.Black,
            UIColor.DarkBlue => ConsoleColor.DarkBlue,
            UIColor.DarkGreen => ConsoleColor.DarkGreen,
            UIColor.DarkCyan => ConsoleColor.DarkCyan,
            UIColor.DarkRed => ConsoleColor.DarkRed,
            UIColor.DarkMagenta => ConsoleColor.DarkMagenta,
            UIColor.DarkYellow => ConsoleColor.DarkYellow,
            UIColor.Gray => ConsoleColor.Gray,
            UIColor.DarkGray => ConsoleColor.DarkGray,
            UIColor.Blue => ConsoleColor.Blue,
            UIColor.Green => ConsoleColor.Green,
            UIColor.Cyan => ConsoleColor.Cyan,
            UIColor.Red => ConsoleColor.Red,
            UIColor.Magenta => ConsoleColor.Magenta,
            UIColor.Yellow => ConsoleColor.Yellow,
            UIColor.White => ConsoleColor.White,
            _ => ConsoleColor.White
        };
    }

    #endregion

    #region Event Triggers

    protected virtual void OnFocusChanged(UIFocusEvent e) => FocusChanged?.Invoke(this, e);
    protected virtual void OnElementActivated(UIActivationEvent e) => ElementActivated?.Invoke(this, e);
    protected virtual void OnLayoutChanged(UILayoutEvent e) => LayoutChanged?.Invoke(this, e);
    protected virtual void OnTextInput(UITextInputEvent e) => TextInput?.Invoke(this, e);
    protected virtual void OnStateChanged(UIStateChangeEvent e) => StateChanged?.Invoke(this, e);

    #endregion
}