using GameConsole.AI.Services;
using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Local;

/// <summary>
/// Base implementation for local AI services providing common functionality.
/// </summary>
public abstract class BaseLocalAIService : GameConsole.AI.Services.IService
{
    protected readonly ILogger _logger;
    private bool _isRunning;
    private bool _disposed;
    
    protected BaseLocalAIService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region IService Implementation

    public bool IsRunning => _isRunning && !_disposed;

    public virtual async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BaseLocalAIService));
        
        _logger.LogInformation("Initializing {ServiceType}", GetType().Name);
        await OnInitializeAsync(cancellationToken);
        _logger.LogInformation("Initialized {ServiceType}", GetType().Name);
    }

    public virtual async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BaseLocalAIService));
        
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

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _logger.LogDebug("Disposing {ServiceType}", GetType().Name);
            
            if (_isRunning)
            {
                await StopAsync();
            }
            
            await OnDisposeAsync();
            _disposed = true;
            _logger.LogDebug("Disposed {ServiceType}", GetType().Name);
        }
        GC.SuppressFinalize(this);
    }

    #endregion

    #region Abstract AI Service Methods

    public abstract Task<AIModel> LoadModelAsync(string modelPath, AIFramework framework, ResourceConfiguration config, CancellationToken cancellationToken = default);
    public abstract Task UnloadModelAsync(string modelId, CancellationToken cancellationToken = default);
    public abstract Task<AIModel?> GetModelInfoAsync(string modelId, CancellationToken cancellationToken = default);
    public abstract Task<IEnumerable<AIModel>> ListModelsAsync(CancellationToken cancellationToken = default);
    public abstract Task<InferenceResult> InferAsync(InferenceRequest request, CancellationToken cancellationToken = default);
    public abstract Task<IEnumerable<InferenceResult>> InferBatchAsync(IEnumerable<InferenceRequest> requests, CancellationToken cancellationToken = default);
    public abstract Task<ResourceStats> GetResourceStatsAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Capabilities (to be overridden by implementations)

    public virtual IResourceManagerCapability? ResourceManager => null;
    public virtual IModelCacheCapability? ModelCache => null; 
    public virtual ILocalInferenceCapability? InferenceEngine => null;

    #endregion

    #region Protected Virtual Methods

    protected virtual Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnStartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnStopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnDisposeAsync()
    {
        return Task.CompletedTask;
    }

    #endregion
}