using GameConsole.Core.Abstractions;
using GameConsole.Graphics.Core;
using GameConsole.Graphics.Services;
using Microsoft.Extensions.Logging;
using System.Numerics;

namespace GameConsole.Graphics.Services;

/// <summary>
/// Base implementation for graphics services providing common functionality.
/// </summary>
public abstract class BaseGraphicsService : GameConsole.Graphics.Services.IService
{
    protected readonly ILogger _logger;
    private bool _isRunning;
    private bool _disposed;

    protected BaseGraphicsService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region IService Implementation

    public bool IsRunning => _isRunning && !_disposed;

    public virtual async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BaseGraphicsService));
        
        _logger.LogInformation("Initializing {ServiceType}", GetType().Name);
        await OnInitializeAsync(cancellationToken);
        _logger.LogInformation("Initialized {ServiceType}", GetType().Name);
    }

    public virtual async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BaseGraphicsService));
        
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

    #region IGraphicsService Implementation

    public abstract Task BeginFrameAsync(CancellationToken cancellationToken = default);
    public abstract Task EndFrameAsync(CancellationToken cancellationToken = default);
    public abstract Task ClearAsync(Vector4 color, CancellationToken cancellationToken = default);
    public abstract Task SetViewportAsync(int x, int y, int width, int height, CancellationToken cancellationToken = default);
    public abstract Task<GraphicsBackend> GetBackendAsync(CancellationToken cancellationToken = default);

    // Capability properties - can be overridden by derived classes
    public virtual ITextureManagerCapability? TextureManager => null;
    public virtual IShaderCapability? ShaderManager => null; 
    public virtual IMeshCapability? MeshManager => null;
    public virtual ICameraCapability? CameraManager => null;

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