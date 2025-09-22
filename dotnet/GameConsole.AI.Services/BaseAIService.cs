using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Services;

/// <summary>
/// Base implementation for AI services providing common functionality.
/// </summary>
public abstract class BaseAIService : IService
{
    protected readonly ILogger _logger;
    private bool _isRunning = false;

    protected BaseAIService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region IService Implementation

    public bool IsRunning => _isRunning;

    public virtual async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing {ServiceType}", GetType().Name);
        await OnInitializeAsync(cancellationToken);
        _logger.LogInformation("Initialized {ServiceType}", GetType().Name);
    }

    public virtual async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting {ServiceType}", GetType().Name);
        await OnStartAsync(cancellationToken);
        _isRunning = true;
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
        await OnDisposeAsync();
        GC.SuppressFinalize(this);
    }

    #endregion

    #region Protected Virtual Methods

    /// <summary>
    /// Called during service initialization. Override to provide custom initialization logic.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async initialization operation.</returns>
    protected virtual Task OnInitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Called during service start. Override to provide custom start logic.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async start operation.</returns>
    protected virtual Task OnStartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Called during service stop. Override to provide custom stop logic.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async stop operation.</returns>
    protected virtual Task OnStopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Called during service disposal. Override to provide custom disposal logic.
    /// </summary>
    /// <returns>A task representing the async disposal operation.</returns>
    protected virtual ValueTask OnDisposeAsync() => ValueTask.CompletedTask;

    #endregion
}