using GameConsole.Core.Abstractions;
using GameConsole.UI.Core;
using Microsoft.Extensions.Logging;

namespace GameConsole.UI.Services;

/// <summary>
/// TUI-based UI service implementation for console interfaces.
/// Provides component management, focus handling, and rendering for text-based UI.
/// </summary>
public class UIService : Core.IService
{
    private readonly ILogger<UIService> _logger;
    private readonly List<IUIComponent> _components = new();
    private readonly Dictionary<string, IUIComponent> _componentMap = new();
    private IUIComponent? _focusedComponent;
    private bool _isRunning;
    
    /// <summary>
    /// Initializes a new instance of the UIService.
    /// </summary>
    public UIService(ILogger<UIService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    #region IService Implementation
    
    /// <inheritdoc />
    public bool IsRunning => _isRunning;
    
    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing UIService");
        await Task.CompletedTask;
        _logger.LogInformation("Initialized UIService");
    }
    
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting UIService");
        _isRunning = true;
        await Task.CompletedTask;
        _logger.LogInformation("Started UIService");
    }
    
    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping UIService");
        _isRunning = false;
        await Task.CompletedTask;
        _logger.LogInformation("Stopped UIService");
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
    
    #region Component Management
    
    /// <inheritdoc />
    public async Task<IWindow> CreateWindowAsync(string title, UIRect bounds, CancellationToken cancellationToken = default)
    {
        var window = new Window(title, bounds);
        await RegisterComponentAsync(window, cancellationToken);
        return window;
    }
    
    /// <inheritdoc />
    public async Task<ILabel> CreateLabelAsync(string text, UIRect bounds, CancellationToken cancellationToken = default)
    {
        var label = new Label(text, bounds);
        await RegisterComponentAsync(label, cancellationToken);
        return label;
    }
    
    /// <inheritdoc />
    public async Task<IButton> CreateButtonAsync(string text, UIRect bounds, CancellationToken cancellationToken = default)
    {
        var button = new Button(text, bounds);
        await RegisterComponentAsync(button, cancellationToken);
        return button;
    }
    
    /// <inheritdoc />
    public async Task<ITextInput> CreateTextInputAsync(string placeholder, UIRect bounds, CancellationToken cancellationToken = default)
    {
        var textInput = new TextInput(placeholder, bounds);
        await RegisterComponentAsync(textInput, cancellationToken);
        return textInput;
    }
    
    private async Task RegisterComponentAsync(IUIComponent component, CancellationToken cancellationToken = default)
    {
        _components.Add(component);
        _componentMap[component.Id] = component;
        
        // Wire up events
        component.Clicked += OnComponentClicked;
        component.FocusChanged += OnComponentFocusChanged;
        
        _logger.LogDebug("Registered component {ComponentId} of type {ComponentType}", 
            component.Id, component.GetType().Name);
        
        await Task.CompletedTask;
    }
    
    #endregion
    
    #region Focus Management
    
    /// <inheritdoc />
    public async Task SetFocusAsync(IUIComponent component, CancellationToken cancellationToken = default)
    {
        if (_focusedComponent == component) return;
        
        var previousFocused = _focusedComponent;
        _focusedComponent = component;
        
        var focusEvent = new UIFocusEvent
        {
            Source = component,
            PreviousFocused = previousFocused,
            NewFocused = component,
            IsFocusGained = true
        };
        
        FocusChanged?.Invoke(this, focusEvent);
        await Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public async Task<IUIComponent?> GetFocusedComponentAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        return _focusedComponent;
    }
    
    /// <inheritdoc />
    public async Task FocusNextAsync(CancellationToken cancellationToken = default)
    {
        var focusableComponents = _components.Where(c => c.CanFocus && c.IsEnabled).ToList();
        if (!focusableComponents.Any()) return;
        
        var currentIndex = _focusedComponent != null ? focusableComponents.IndexOf(_focusedComponent) : -1;
        var nextIndex = (currentIndex + 1) % focusableComponents.Count;
        
        await SetFocusAsync(focusableComponents[nextIndex], cancellationToken);
    }
    
    /// <inheritdoc />
    public async Task FocusPreviousAsync(CancellationToken cancellationToken = default)
    {
        var focusableComponents = _components.Where(c => c.CanFocus && c.IsEnabled).ToList();
        if (!focusableComponents.Any()) return;
        
        var currentIndex = _focusedComponent != null ? focusableComponents.IndexOf(_focusedComponent) : -1;
        var prevIndex = currentIndex <= 0 ? focusableComponents.Count - 1 : currentIndex - 1;
        
        await SetFocusAsync(focusableComponents[prevIndex], cancellationToken);
    }
    
    #endregion
    
    #region Rendering
    
    /// <inheritdoc />
    public async Task RenderAsync(CancellationToken cancellationToken = default)
    {
        var context = new UIRenderContext(
            new UIRect(0, 0, Console.WindowWidth, Console.WindowHeight),
            UITheme.Default
        );
        
        // Simple console rendering for demonstration
        Console.Clear();
        
        foreach (var component in _components.Where(c => c.IsVisible))
        {
            await component.RenderAsync(context, cancellationToken);
        }
    }
    
    /// <inheritdoc />
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await RenderAsync(cancellationToken);
    }
    
    #endregion
    
    #region Events
    
    /// <inheritdoc />
    public event EventHandler<UIClickEvent>? ComponentClicked;
    
    /// <inheritdoc />
    public event EventHandler<UIFocusEvent>? FocusChanged;
    
    /// <inheritdoc />
    public event EventHandler<UIValueChangedEvent>? ValueChanged;
    
    private void OnComponentClicked(object? sender, UIClickEvent e)
    {
        ComponentClicked?.Invoke(sender, e);
    }
    
    private void OnComponentFocusChanged(object? sender, UIFocusEvent e)
    {
        FocusChanged?.Invoke(sender, e);
    }
    
    #endregion
    
    #region Capabilities
    
    /// <inheritdoc />
    public ILayoutCapability? LayoutManager => new LayoutService();
    
    /// <inheritdoc />
    public IThemeCapability? ThemeManager => new ThemeService();
    
    #endregion
}

/// <summary>
/// Layout management capability implementation.
/// </summary>
public class LayoutService : ILayoutCapability
{
    /// <inheritdoc />
    public string Name => "LayoutService";
    
    /// <inheritdoc />
    public Task ArrangeComponentsAsync(IUIComponent container, LayoutStrategy strategy, CancellationToken cancellationToken = default)
    {
        // Simple layout implementation
        switch (strategy)
        {
            case LayoutStrategy.Vertical:
                ArrangeVertically(container);
                break;
            case LayoutStrategy.Horizontal:
                ArrangeHorizontally(container);
                break;
            default:
                break;
        }
        
        return Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public Task<UISize> GetPreferredSizeAsync(IUIComponent component, UISize availableSpace, CancellationToken cancellationToken = default)
    {
        // Basic preferred size calculation
        return Task.FromResult(new UISize(Math.Min(availableSpace.Width, 20), Math.Min(availableSpace.Height, 3)));
    }
    
    private void ArrangeVertically(IUIComponent container)
    {
        var y = container.Bounds.Y;
        foreach (var child in container.Children)
        {
            child.Bounds = new UIRect(container.Bounds.X, y, child.Bounds.Width, child.Bounds.Height);
            y += child.Bounds.Height + 1; // 1 pixel spacing
        }
    }
    
    private void ArrangeHorizontally(IUIComponent container)
    {
        var x = container.Bounds.X;
        foreach (var child in container.Children)
        {
            child.Bounds = new UIRect(x, container.Bounds.Y, child.Bounds.Width, child.Bounds.Height);
            x += child.Bounds.Width + 1; // 1 pixel spacing
        }
    }
}

/// <summary>
/// Theme management capability implementation.
/// </summary>
public class ThemeService : IThemeCapability
{
    private UITheme _currentTheme = UITheme.Default;
    
    /// <inheritdoc />
    public string Name => "ThemeService";
    
    /// <inheritdoc />
    public Task ApplyThemeAsync(UITheme theme, CancellationToken cancellationToken = default)
    {
        _currentTheme = theme;
        return Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public Task<UITheme> GetCurrentThemeAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_currentTheme);
    }
    
    /// <inheritdoc />
    public Task SetComponentStyleAsync(IUIComponent component, UIStyle style, CancellationToken cancellationToken = default)
    {
        // Store component-specific style in theme
        _currentTheme.ComponentStyles[component.Id] = style;
        return Task.CompletedTask;
    }
}