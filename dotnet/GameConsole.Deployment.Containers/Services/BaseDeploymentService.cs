using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace GameConsole.Deployment.Containers.Services;

/// <summary>
/// Base implementation for deployment services providing common lifecycle functionality.
/// Follows the established patterns from GameConsole service architecture.
/// </summary>
public abstract class BaseDeploymentService : IService
{
    protected readonly ILogger _logger;
    private bool _isRunning = false;
    private bool _disposed = false;

    protected BaseDeploymentService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region IService Implementation

    public bool IsRunning => _isRunning;

    public virtual async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        _logger.LogInformation("Initializing {ServiceType}", GetType().Name);
        
        try
        {
            await OnInitializeAsync(cancellationToken);
            _logger.LogInformation("Initialized {ServiceType}", GetType().Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize {ServiceType}", GetType().Name);
            throw;
        }
    }

    public virtual async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        _logger.LogInformation("Starting {ServiceType}", GetType().Name);
        
        try
        {
            await OnStartAsync(cancellationToken);
            _isRunning = true;
            _logger.LogInformation("Started {ServiceType}", GetType().Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start {ServiceType}", GetType().Name);
            throw;
        }
    }

    public virtual async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
            return;

        _logger.LogInformation("Stopping {ServiceType}", GetType().Name);
        
        try
        {
            _isRunning = false;
            await OnStopAsync(cancellationToken);
            _logger.LogInformation("Stopped {ServiceType}", GetType().Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop {ServiceType}", GetType().Name);
            throw;
        }
    }

    public virtual async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        try
        {
            if (_isRunning)
            {
                await StopAsync();
            }
            
            await OnDisposeAsync();
            _disposed = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during disposal of {ServiceType}", GetType().Name);
            throw;
        }
    }

    #endregion

    #region Protected Methods for Derived Classes

    /// <summary>
    /// Override this method to provide service-specific initialization logic.
    /// </summary>
    protected virtual Task OnInitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>
    /// Override this method to provide service-specific start logic.
    /// </summary>
    protected virtual Task OnStartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>
    /// Override this method to provide service-specific stop logic.
    /// </summary>
    protected virtual Task OnStopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>
    /// Override this method to provide service-specific disposal logic.
    /// </summary>
    protected virtual ValueTask OnDisposeAsync() => ValueTask.CompletedTask;

    /// <summary>
    /// Throws ObjectDisposedException if the service has been disposed.
    /// </summary>
    protected void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(GetType().Name);
    }

    /// <summary>
    /// Helper method to handle exceptions and return appropriate results.
    /// </summary>
    protected T HandleException<T>(Exception ex, string operation, Func<T> fallbackResult) where T : class
    {
        _logger.LogError(ex, "Exception during {Operation} in {ServiceType}", operation, GetType().Name);
        return fallbackResult();
    }

    /// <summary>
    /// Helper method to handle async exceptions and return appropriate results.
    /// </summary>
    protected async Task<T> HandleExceptionAsync<T>(Exception ex, string operation, Func<Task<T>> fallbackResult) where T : class
    {
        _logger.LogError(ex, "Exception during {Operation} in {ServiceType}", operation, GetType().Name);
        return await fallbackResult();
    }

    #endregion
}