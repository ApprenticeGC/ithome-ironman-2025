using GameConsole.Core.Abstractions;
using GameConsole.Plugins.Core;
using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Core;

/// <summary>
/// Base implementation of IAIAgent that provides common functionality.
/// Inherit from this class to create custom AI agents with minimal boilerplate.
/// </summary>
public abstract class BaseAIAgent : IAIAgent
{
    private readonly ILogger<BaseAIAgent> _logger;
    private volatile bool _isInitialized;
    private volatile bool _isStarted;
    private volatile bool _isDisposed;
    private volatile bool _isActive;
    private volatile bool _isLearning;
    private long _decisionCount;
    private DateTime? _lastDecisionTime;
    private readonly object _lock = new object();

    /// <inheritdoc />
    public abstract IPluginMetadata Metadata { get; }

    /// <inheritdoc />
    public IPluginContext? Context { get; set; }

    /// <inheritdoc />
    public abstract IAIAgentCapabilities Capabilities { get; }

    /// <inheritdoc />
    public IAIAgentState State => new AIAgentState
    {
        IsActive = _isActive,
        IsLearning = _isLearning,
        DecisionCount = _decisionCount,
        LastDecisionTime = _lastDecisionTime,
        Metrics = GetCurrentMetrics(),
        Configuration = GetCurrentConfiguration()
    };

    /// <inheritdoc />
    public bool IsRunning => _isStarted && !_isDisposed;

    /// <summary>
    /// Initializes a new instance of the BaseAIAgent class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic information.</param>
    protected BaseAIAgent(ILogger<BaseAIAgent> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public virtual Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        if (_isInitialized)
            return Task.CompletedTask;

        lock (_lock)
        {
            if (_isInitialized)
                return Task.CompletedTask;

            _logger.LogInformation("Initializing AI agent: {AgentName}", Metadata.Name);
            OnInitialize();
            _isInitialized = true;
            _logger.LogDebug("AI agent initialized: {AgentName}", Metadata.Name);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual Task StartAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfNotInitialized();

        if (_isStarted)
            return Task.CompletedTask;

        lock (_lock)
        {
            if (_isStarted)
                return Task.CompletedTask;

            _logger.LogInformation("Starting AI agent: {AgentName}", Metadata.Name);
            OnStart();
            _isStarted = true;
            _isActive = true;
            _logger.LogDebug("AI agent started: {AgentName}", Metadata.Name);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isStarted || _isDisposed)
            return Task.CompletedTask;

        lock (_lock)
        {
            if (!_isStarted || _isDisposed)
                return Task.CompletedTask;

            _logger.LogInformation("Stopping AI agent: {AgentName}", Metadata.Name);
            _isActive = false;
            OnStop();
            _isStarted = false;
            _logger.LogDebug("AI agent stopped: {AgentName}", Metadata.Name);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual async ValueTask DisposeAsync()
    {
        if (_isDisposed)
            return;

        await StopAsync();

        lock (_lock)
        {
            if (_isDisposed)
                return;

            _logger.LogInformation("Disposing AI agent: {AgentName}", Metadata.Name);
            OnDispose();
            _isDisposed = true;
            _logger.LogDebug("AI agent disposed: {AgentName}", Metadata.Name);
        }
    }

    /// <inheritdoc />
    public virtual Task ConfigureAsync(IPluginContext context, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        Context = context ?? throw new ArgumentNullException(nameof(context));
        
        _logger.LogDebug("Configuring AI agent: {AgentName}", Metadata.Name);
        OnConfigure(context);
        
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual Task<bool> CanUnloadAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        // Default implementation: can unload if not active or can be safely stopped
        var canUnload = !_isActive || CanSafelyStop();
        
        _logger.LogDebug("AI agent {AgentName} can unload: {CanUnload}", Metadata.Name, canUnload);
        return Task.FromResult(canUnload);
    }

    /// <inheritdoc />
    public virtual Task PrepareUnloadAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        _logger.LogDebug("Preparing AI agent {AgentName} for unload", Metadata.Name);
        OnPrepareUnload();
        
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IAIAgentResponse> ProcessAsync(IAIAgentInput input, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfNotActive();

        if (input == null)
            throw new ArgumentNullException(nameof(input));

        var startTime = DateTime.UtcNow;
        
        try
        {
            _logger.LogDebug("Processing input of type {InputType} for agent {AgentName}", 
                input.InputType, Metadata.Name);

            var response = await ProcessInputAsync(input, cancellationToken);
            
            // Update metrics
            Interlocked.Increment(ref _decisionCount);
            _lastDecisionTime = DateTime.UtcNow;
            
            var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
            OnDecisionMade(input, response, processingTime);

            _logger.LogDebug("Successfully processed input for agent {AgentName} in {ProcessingTime}ms", 
                Metadata.Name, processingTime);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing input for agent {AgentName}", Metadata.Name);
            
            return new AIAgentResponse
            {
                Success = false,
                ResponseType = "error",
                Data = new object(),
                Confidence = 0.0,
                Error = ex.Message,
                Metadata = new Dictionary<string, object>
                {
                    ["exception"] = ex.GetType().Name,
                    ["processingTime"] = (DateTime.UtcNow - startTime).TotalMilliseconds
                }
            };
        }
    }

    /// <inheritdoc />
    public async Task UpdateAsync(IAIAgentFeedback feedback, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        if (feedback == null)
            throw new ArgumentNullException(nameof(feedback));

        if (!Capabilities.SupportsLearning)
        {
            _logger.LogWarning("AI agent {AgentName} does not support learning but received feedback", Metadata.Name);
            return;
        }

        try
        {
            _logger.LogDebug("Processing feedback for agent {AgentName}: {FeedbackType} with score {Score}", 
                Metadata.Name, feedback.FeedbackType, feedback.Score);

            _isLearning = true;
            await ProcessFeedbackAsync(feedback, cancellationToken);
            OnFeedbackProcessed(feedback);

            _logger.LogDebug("Successfully processed feedback for agent {AgentName}", Metadata.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing feedback for agent {AgentName}", Metadata.Name);
            throw;
        }
        finally
        {
            _isLearning = false;
        }
    }

    /// <inheritdoc />
    public async Task ResetAsync(bool preserveConfiguration = true, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _logger.LogInformation("Resetting AI agent {AgentName} (preserveConfiguration: {PreserveConfiguration})", 
            Metadata.Name, preserveConfiguration);

        try
        {
            await OnResetAsync(preserveConfiguration, cancellationToken);
            
            // Reset metrics
            Interlocked.Exchange(ref _decisionCount, 0);
            _lastDecisionTime = null;
            
            _logger.LogDebug("Successfully reset AI agent {AgentName}", Metadata.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting AI agent {AgentName}", Metadata.Name);
            throw;
        }
    }

    /// <summary>
    /// Called during initialization. Override to perform custom initialization logic.
    /// </summary>
    protected virtual void OnInitialize() { }

    /// <summary>
    /// Called during startup. Override to perform custom startup logic.
    /// </summary>
    protected virtual void OnStart() { }

    /// <summary>
    /// Called during shutdown. Override to perform custom shutdown logic.
    /// </summary>
    protected virtual void OnStop() { }

    /// <summary>
    /// Called during disposal. Override to perform custom cleanup logic.
    /// </summary>
    protected virtual void OnDispose() { }

    /// <summary>
    /// Called during configuration. Override to handle plugin context setup.
    /// </summary>
    /// <param name="context">The plugin context.</param>
    protected virtual void OnConfigure(IPluginContext context) { }

    /// <summary>
    /// Called to prepare for unload. Override to perform custom unload preparation.
    /// </summary>
    protected virtual void OnPrepareUnload() { }

    /// <summary>
    /// Called after a decision is made. Override to update custom metrics or logging.
    /// </summary>
    /// <param name="input">The input that was processed.</param>
    /// <param name="response">The response that was generated.</param>
    /// <param name="processingTimeMs">The processing time in milliseconds.</param>
    protected virtual void OnDecisionMade(IAIAgentInput input, IAIAgentResponse response, double processingTimeMs) { }

    /// <summary>
    /// Called after feedback is processed. Override to handle post-feedback logic.
    /// </summary>
    /// <param name="feedback">The feedback that was processed.</param>
    protected virtual void OnFeedbackProcessed(IAIAgentFeedback feedback) { }

    /// <summary>
    /// Override this method to implement the core AI processing logic.
    /// </summary>
    /// <param name="input">The input to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The AI agent's response.</returns>
    protected abstract Task<IAIAgentResponse> ProcessInputAsync(IAIAgentInput input, CancellationToken cancellationToken);

    /// <summary>
    /// Override this method to implement learning from feedback (if supported).
    /// </summary>
    /// <param name="feedback">The feedback to learn from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the learning process.</returns>
    protected virtual Task ProcessFeedbackAsync(IAIAgentFeedback feedback, CancellationToken cancellationToken)
    {
        // Default implementation does nothing
        return Task.CompletedTask;
    }

    /// <summary>
    /// Override this method to implement reset logic.
    /// </summary>
    /// <param name="preserveConfiguration">Whether to preserve configuration during reset.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the reset process.</returns>
    protected virtual Task OnResetAsync(bool preserveConfiguration, CancellationToken cancellationToken)
    {
        // Default implementation does nothing
        return Task.CompletedTask;
    }

    /// <summary>
    /// Override this method to provide current metrics.
    /// </summary>
    /// <returns>Current agent metrics.</returns>
    protected virtual IAIAgentMetrics GetCurrentMetrics()
    {
        return new AIAgentMetrics
        {
            AverageProcessingTimeMs = 0.0, // Subclasses should track this
            SuccessRate = 1.0, // Subclasses should track this
            MemoryUsageBytes = GC.GetTotalMemory(false), // Basic memory usage
            AdditionalMetrics = new Dictionary<string, object>()
        };
    }

    /// <summary>
    /// Override this method to provide current configuration.
    /// </summary>
    /// <returns>Current agent configuration.</returns>
    protected virtual IReadOnlyDictionary<string, object> GetCurrentConfiguration()
    {
        return new Dictionary<string, object>();
    }

    /// <summary>
    /// Override this method to determine if the agent can be safely stopped.
    /// </summary>
    /// <returns>True if the agent can be safely stopped.</returns>
    protected virtual bool CanSafelyStop()
    {
        return true; // Default implementation assumes it's always safe
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(GetType().Name);
    }

    private void ThrowIfNotInitialized()
    {
        if (!_isInitialized)
            throw new InvalidOperationException("AI agent must be initialized before use");
    }

    private void ThrowIfNotActive()
    {
        if (!_isActive)
            throw new InvalidOperationException("AI agent is not active");
    }
}