using GameConsole.Core.Abstractions;
using GameConsole.UI.Core;
using Microsoft.Extensions.Logging;

namespace GameConsole.UI.Services;

/// <summary>
/// Base implementation for UI services providing common functionality.
/// </summary>
public abstract class BaseUIService : GameConsole.UI.Core.IService
{
    protected readonly ILogger _logger;
    private bool _isRunning;
    private bool _disposed;

    protected BaseUIService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region IService Implementation

    public bool IsRunning => _isRunning && !_disposed;

    public virtual async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BaseUIService));
        
        _logger.LogInformation("Initializing {ServiceType}", GetType().Name);
        await OnInitializeAsync(cancellationToken);
        _logger.LogInformation("Initialized {ServiceType}", GetType().Name);
    }

    public virtual async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BaseUIService));
        
        _logger.LogInformation("Starting {ServiceType}", GetType().Name);
        _isRunning = true;
        await OnStartAsync(cancellationToken);
        _logger.LogInformation("Started {ServiceType}", GetType().Name);
    }

    public virtual async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping {ServiceType}", GetType().Name);
        _isRunning = false;
        await OnStopAsync(cancellationToken);
        _logger.LogInformation("Stopped {ServiceType}", GetType().Name);
    }

    public virtual async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        
        if (_isRunning)
        {
            await StopAsync();
        }
        
        await OnDisposeAsync();
        _disposed = true;
    }

    #endregion

    #region IUIService Implementation

    public abstract Task RenderAsync(CancellationToken cancellationToken = default);
    public abstract Task ClearScreenAsync(CancellationToken cancellationToken = default);
    public abstract Task AddComponentAsync(IUIComponent component, CancellationToken cancellationToken = default);
    public abstract Task RemoveComponentAsync(IUIComponent component, CancellationToken cancellationToken = default);
    public abstract Task<Size> GetConsoleSize(CancellationToken cancellationToken = default);
    public abstract bool SupportsColors { get; }

    // Capability properties - can be overridden by derived classes
    public virtual IUIRenderer? Renderer => null;
    public virtual ITextComponentCapability? TextComponents => null;

    #endregion

    #region Protected Methods for Derived Classes

    protected virtual Task OnInitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    protected virtual Task OnStartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    protected virtual Task OnStopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    protected virtual ValueTask OnDisposeAsync() => ValueTask.CompletedTask;

    protected void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(GetType().Name);
    }

    protected void ThrowIfNotRunning()
    {
        ThrowIfDisposed();
        if (!_isRunning) throw new InvalidOperationException($"{GetType().Name} is not running");
    }

    #endregion
}