using GameConsole.Audio.Services;
using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace GameConsole.Audio.Services.Implementation;

/// <summary>
/// Base implementation for audio services providing common functionality.
/// </summary>
public abstract class BaseAudioService : IService
{
    private readonly ILogger? _logger;
    private bool _isRunning;
    private bool _disposed;

    protected BaseAudioService(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets a value indicating whether the service is currently running.
    /// </summary>
    public bool IsRunning => _isRunning && !_disposed;

    /// <summary>
    /// Initializes the service asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async initialization operation.</returns>
    public virtual async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        _logger?.LogDebug("Initializing {ServiceType}", GetType().Name);
        
        await OnInitializeAsync(cancellationToken);
        
        _logger?.LogInformation("{ServiceType} initialized successfully", GetType().Name);
    }

    /// <summary>
    /// Starts the service asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async start operation.</returns>
    public virtual async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        _logger?.LogDebug("Starting {ServiceType}", GetType().Name);
        
        await OnStartAsync(cancellationToken);
        _isRunning = true;
        
        _logger?.LogInformation("{ServiceType} started successfully", GetType().Name);
    }

    /// <summary>
    /// Stops the service asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async stop operation.</returns>
    public virtual async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning) return;
        
        _logger?.LogDebug("Stopping {ServiceType}", GetType().Name);
        
        await OnStopAsync(cancellationToken);
        _isRunning = false;
        
        _logger?.LogInformation("{ServiceType} stopped successfully", GetType().Name);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        
        if (_isRunning)
        {
            await StopAsync();
        }
        
        await OnDisposeAsync();
        _disposed = true;
        
        _logger?.LogDebug("{ServiceType} disposed", GetType().Name);
    }

    /// <summary>
    /// Override to provide custom initialization logic.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    protected virtual Task OnInitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Override to provide custom start logic.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    protected virtual Task OnStartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Override to provide custom stop logic.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    protected virtual Task OnStopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Override to provide custom disposal logic.
    /// </summary>
    /// <returns>Task representing the async operation.</returns>
    protected virtual ValueTask OnDisposeAsync() => ValueTask.CompletedTask;

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger? Logger => _logger;

    /// <summary>
    /// Throws if the service has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the service is disposed.</exception>
    protected void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(GetType().Name);
    }
}