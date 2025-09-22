using GameConsole.Core.Abstractions;

namespace GameConsole.UI.Core;

/// <summary>
/// Main interface for UI framework abstraction.
/// Provides lifecycle management and component rendering capabilities across Console, Web, and Desktop frameworks.
/// </summary>
public interface IUIFramework : IService
{
    /// <summary>
    /// The type of framework this implementation supports.
    /// </summary>
    FrameworkType FrameworkType { get; }
    
    /// <summary>
    /// Capabilities supported by this framework implementation.
    /// </summary>
    UICapabilities SupportedCapabilities { get; }
    
    /// <summary>
    /// Name and version information for this framework.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Version of this framework implementation.
    /// </summary>
    string Version { get; }
    
    /// <summary>
    /// Event dispatcher for this framework instance.
    /// </summary>
    IUIEventDispatcher EventDispatcher { get; }
    
    /// <summary>
    /// Renderer for this framework.
    /// </summary>
    IUIRenderer Renderer { get; }
    
    /// <summary>
    /// Component factory for creating framework-appropriate components.
    /// </summary>
    IUIComponentFactory ComponentFactory { get; }
    
    /// <summary>
    /// Creates a UI context for this framework with the specified parameters.
    /// </summary>
    UIContext CreateContext(
        string[] args,
        UIMode mode,
        UIPreferences? preferences = null,
        Dictionary<string, object>? initialState = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Activates the framework with the specified context.
    /// This prepares the framework for rendering and user interaction.
    /// </summary>
    Task<bool> ActivateAsync(UIContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deactivates the framework and cleans up resources.
    /// </summary>
    Task DeactivateAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Renders the root UI component and its children.
    /// </summary>
    Task<RenderResult> RenderRootAsync(IUIComponent rootComponent, UIContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates the display with current component state.
    /// </summary>
    Task RefreshAsync(UIContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Processes pending input events and routes them to appropriate components.
    /// </summary>
    Task ProcessInputAsync(UIContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Handles responsive design by adapting to context changes.
    /// </summary>
    Task HandleResponsiveChangeAsync(UIContext oldContext, UIContext newContext, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates that a component can be rendered by this framework.
    /// </summary>
    bool CanRender(IUIComponent component, UIContext context);
    
    /// <summary>
    /// Gets framework-specific configuration or metadata.
    /// </summary>
    T? GetConfiguration<T>(string key, T? defaultValue = default);
    
    /// <summary>
    /// Sets framework-specific configuration.
    /// </summary>
    Task SetConfigurationAsync<T>(string key, T value, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Current context being used by this framework instance.
    /// </summary>
    UIContext? CurrentContext { get; }
    
    /// <summary>
    /// Indicates if the framework is currently active and ready for rendering.
    /// </summary>
    bool IsActive { get; }
    
    /// <summary>
    /// Event fired when the framework context changes.
    /// </summary>
    event Func<UIContext?, UIContext?, Task>? ContextChanged;
    
    /// <summary>
    /// Event fired when the framework activation state changes.
    /// </summary>
    event Func<bool, Task>? ActivationChanged;
    
    /// <summary>
    /// Event fired when an error occurs in the framework.
    /// </summary>
    event Func<string, Exception?, Task>? ErrorOccurred;
}

/// <summary>
/// Base abstract implementation of IUIFramework providing common functionality.
/// Framework-specific implementations should inherit from this class.
/// </summary>
public abstract class UIFrameworkBase : IUIFramework
{
    private bool _isRunning;
    private bool _isActive;
    private UIContext? _currentContext;
    private readonly Dictionary<string, object> _configuration = new();
    
    protected UIFrameworkBase(string name, string version, FrameworkType frameworkType)
    {
        Name = name;
        Version = version;
        FrameworkType = frameworkType;
    }
    
    /// <inheritdoc />
    public FrameworkType FrameworkType { get; }
    
    /// <inheritdoc />
    public abstract UICapabilities SupportedCapabilities { get; }
    
    /// <inheritdoc />
    public string Name { get; }
    
    /// <inheritdoc />
    public string Version { get; }
    
    /// <inheritdoc />
    public abstract IUIEventDispatcher EventDispatcher { get; }
    
    /// <inheritdoc />
    public abstract IUIRenderer Renderer { get; }
    
    /// <inheritdoc />
    public abstract IUIComponentFactory ComponentFactory { get; }
    
    /// <inheritdoc />
    public bool IsRunning => _isRunning;
    
    /// <inheritdoc />
    public bool IsActive => _isActive;
    
    /// <inheritdoc />
    public UIContext? CurrentContext => _currentContext;
    
    /// <inheritdoc />
    public event Func<UIContext?, UIContext?, Task>? ContextChanged;
    
    /// <inheritdoc />
    public event Func<bool, Task>? ActivationChanged;
    
    /// <inheritdoc />
    public event Func<string, Exception?, Task>? ErrorOccurred;
    
    /// <inheritdoc />
    public virtual UIContext CreateContext(
        string[] args,
        UIMode mode,
        UIPreferences? preferences = null,
        Dictionary<string, object>? initialState = null,
        CancellationToken cancellationToken = default)
    {
        return new UIContext(
            Args: args,
            State: initialState ?? new Dictionary<string, object>(),
            CurrentMode: mode,
            Preferences: preferences ?? new UIPreferences(),
            SupportedCapabilities: SupportedCapabilities,
            FrameworkType: FrameworkType,
            Style: StyleContext.Empty,
            CancellationToken: cancellationToken
        )
        {
            EventDispatcher = EventDispatcher
        };
    }
    
    /// <inheritdoc />
    public virtual async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning) return;
        
        await OnInitializeAsync(cancellationToken);
        _isRunning = true;
    }
    
    /// <inheritdoc />
    public virtual async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
        {
            await InitializeAsync(cancellationToken);
        }
        
        await OnStartAsync(cancellationToken);
    }
    
    /// <inheritdoc />
    public virtual async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning) return;
        
        if (_isActive)
        {
            await DeactivateAsync(cancellationToken);
        }
        
        await OnStopAsync(cancellationToken);
        _isRunning = false;
    }
    
    /// <inheritdoc />
    public virtual async Task<bool> ActivateAsync(UIContext context, CancellationToken cancellationToken = default)
    {
        var oldContext = _currentContext;
        _currentContext = context;
        
        var success = await OnActivateAsync(context, cancellationToken);
        if (success)
        {
            _isActive = true;
            if (ContextChanged != null)
            {
                await ContextChanged(oldContext, context);
            }
            if (ActivationChanged != null)
            {
                await ActivationChanged(true);
            }
        }
        else
        {
            _currentContext = oldContext; // Revert on failure
        }
        
        return success;
    }
    
    /// <inheritdoc />
    public virtual async Task DeactivateAsync(CancellationToken cancellationToken = default)
    {
        if (!_isActive) return;
        
        var oldContext = _currentContext;
        await OnDeactivateAsync(cancellationToken);
        
        _isActive = false;
        _currentContext = null;
        
        if (ContextChanged != null)
        {
            await ContextChanged(oldContext, null);
        }
        if (ActivationChanged != null)
        {
            await ActivationChanged(false);
        }
    }
    
    /// <inheritdoc />
    public abstract Task<RenderResult> RenderRootAsync(IUIComponent rootComponent, UIContext context, CancellationToken cancellationToken = default);
    
    /// <inheritdoc />
    public abstract Task RefreshAsync(UIContext context, CancellationToken cancellationToken = default);
    
    /// <inheritdoc />
    public abstract Task ProcessInputAsync(UIContext context, CancellationToken cancellationToken = default);
    
    /// <inheritdoc />
    public virtual async Task HandleResponsiveChangeAsync(UIContext oldContext, UIContext newContext, CancellationToken cancellationToken = default)
    {
        // Default implementation - framework-specific implementations can override
        await Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public abstract bool CanRender(IUIComponent component, UIContext context);
    
    /// <inheritdoc />
    public T? GetConfiguration<T>(string key, T? defaultValue = default)
    {
        if (_configuration.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }
    
    /// <inheritdoc />
    public virtual Task SetConfigurationAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        _configuration[key] = value!;
        return Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public virtual async ValueTask DisposeAsync()
    {
        await StopAsync();
        
        // Clear event handlers
        ContextChanged = null;
        ActivationChanged = null;
        ErrorOccurred = null;
        
        await OnDisposeAsync();
        GC.SuppressFinalize(this);
    }
    
    /// <summary>
    /// Called during framework initialization. Override to provide framework-specific initialization logic.
    /// </summary>
    protected virtual Task OnInitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    
    /// <summary>
    /// Called during framework startup. Override to provide framework-specific startup logic.
    /// </summary>
    protected virtual Task OnStartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    
    /// <summary>
    /// Called during framework shutdown. Override to provide framework-specific shutdown logic.
    /// </summary>
    protected virtual Task OnStopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    
    /// <summary>
    /// Called during framework activation. Override to provide framework-specific activation logic.
    /// </summary>
    protected virtual Task<bool> OnActivateAsync(UIContext context, CancellationToken cancellationToken = default) => Task.FromResult(true);
    
    /// <summary>
    /// Called during framework deactivation. Override to provide framework-specific deactivation logic.
    /// </summary>
    protected virtual Task OnDeactivateAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    
    /// <summary>
    /// Called during disposal. Override to provide framework-specific cleanup logic.
    /// </summary>
    protected virtual Task OnDisposeAsync() => Task.CompletedTask;
    
    /// <summary>
    /// Helper method to raise error events.
    /// </summary>
    protected async Task RaiseErrorAsync(string message, Exception? exception = null)
    {
        if (ErrorOccurred != null)
        {
            await ErrorOccurred(message, exception);
        }
    }
}