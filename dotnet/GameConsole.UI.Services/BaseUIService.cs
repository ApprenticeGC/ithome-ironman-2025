using GameConsole.Core.Abstractions;
using GameConsole.UI.Core;
using Microsoft.Extensions.Logging;
using System.Reactive.Subjects;

namespace GameConsole.UI.Services;

/// <summary>
/// Base class for UI services providing common functionality.
/// </summary>
public abstract class BaseUIService : IService, IDisposable
{
    protected readonly ILogger Logger;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    protected readonly CancellationToken CancellationToken;
    private bool _isRunning = false;
    private bool _disposed = false;
    
    protected BaseUIService(ILogger logger)
    {
        Logger = logger;
        CancellationToken = _cancellationTokenSource.Token;
    }

    public bool IsRunning => _isRunning && !_disposed;

    public virtual async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(GetType().Name);

        Logger.LogInformation("Initializing {ServiceName}", GetType().Name);
        
        try
        {
            await OnInitializeAsync(cancellationToken);
            Logger.LogInformation("Initialized {ServiceName}", GetType().Name);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize {ServiceName}", GetType().Name);
            throw;
        }
    }

    public virtual async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(GetType().Name);

        Logger.LogInformation("Starting {ServiceName}", GetType().Name);
        
        try
        {
            _isRunning = true;
            await OnStartAsync(cancellationToken);
            Logger.LogInformation("Started {ServiceName}", GetType().Name);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to start {ServiceName}", GetType().Name);
            _isRunning = false;
            throw;
        }
    }

    public virtual async Task StopAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Stopping {ServiceName}", GetType().Name);
        
        try
        {
            _isRunning = false;
            await OnStopAsync(cancellationToken);
            Logger.LogInformation("Stopped {ServiceName}", GetType().Name);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to stop {ServiceName}", GetType().Name);
            throw;
        }
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

    protected virtual Task OnInitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    protected virtual Task OnStartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    protected virtual Task OnStopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    protected virtual ValueTask OnDisposeAsync() => ValueTask.CompletedTask;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _disposed = true;
        }
    }
}