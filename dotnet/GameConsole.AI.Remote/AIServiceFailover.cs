using GameConsole.AI.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace GameConsole.AI.Remote;

/// <summary>
/// Provides failover and redundancy capabilities for remote AI services.
/// Handles automatic fallback to alternative providers or local AI when services fail.
/// </summary>
public class AIServiceFailover : IDisposable
{
    private readonly ILogger<AIServiceFailover> _logger;
    private readonly FailoverConfiguration _config;
    private readonly RemoteAILoadBalancer _loadBalancer;
    private readonly ConcurrentDictionary<string, FailoverState> _providerStates;
    private readonly ConcurrentQueue<FailedRequest> _retryQueue;
    private readonly Timer? _retryTimer;
    private readonly Timer? _healthRecoveryTimer;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the AIServiceFailover.
    /// </summary>
    /// <param name="config">Failover configuration.</param>
    /// <param name="loadBalancer">Load balancer for provider selection.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    public AIServiceFailover(
        IOptions<FailoverConfiguration> config,
        RemoteAILoadBalancer loadBalancer,
        ILogger<AIServiceFailover> logger)
    {
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _loadBalancer = loadBalancer ?? throw new ArgumentNullException(nameof(loadBalancer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _providerStates = new ConcurrentDictionary<string, FailoverState>();
        _retryQueue = new ConcurrentQueue<FailedRequest>();
        
        if (_config.Enabled)
        {
            _retryTimer = new Timer(ProcessRetryQueue, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
            _healthRecoveryTimer = new Timer(CheckProviderRecovery, null, 
                _config.HealthCheckInterval, _config.HealthCheckInterval);
            
            _logger.LogInformation("Initialized AIServiceFailover with strategy {Strategy}", _config.Strategy);
        }
        else
        {
            _retryTimer = null;
            _healthRecoveryTimer = null;
            _logger.LogInformation("AIServiceFailover is disabled");
        }
    }

    /// <summary>
    /// Executes an AI request with failover protection.
    /// Automatically retries with alternative providers if the primary fails.
    /// </summary>
    /// <param name="request">The AI request to execute.</param>
    /// <param name="executeFunction">Function to execute the request against a provider.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the async operation that returns the AI response.</returns>
    public async Task<AIResponse> ExecuteWithFailoverAsync<T>(
        T request,
        Func<AIProvider, T, CancellationToken, Task<AIResponse>> executeFunction,
        CancellationToken cancellationToken = default) where T : class
    {
        if (_disposed) throw new ObjectDisposedException(nameof(AIServiceFailover));
        
        if (!_config.Enabled)
        {
            var provider = _loadBalancer.SelectProvider();
            if (provider == null)
                throw new AIServiceException("No providers available", "none");
            
            return await executeFunction(provider, request, cancellationToken);
        }

        var attemptedProviders = new HashSet<string>();
        var lastException = default(Exception);

        for (int attempt = 0; attempt < _config.MaxRetryAttempts; attempt++)
        {
            var provider = _loadBalancer.SelectProvider(attemptedProviders);
            if (provider == null)
            {
                _logger.LogWarning("No more providers available after {AttemptCount} attempts", attempt);
                break;
            }

            attemptedProviders.Add(provider.Id);
            
            try
            {
                _logger.LogDebug("Attempting request with provider {ProviderId} (attempt {AttemptNumber})", 
                    provider.Id, attempt + 1);
                
                var trackingContext = _loadBalancer.StartRequest(provider.Id);
                
                try
                {
                    var response = await executeFunction(provider, request, cancellationToken);
                    _loadBalancer.CompleteRequest(trackingContext, success: true);
                    
                    // Reset failure state on successful request
                    ResetProviderFailureState(provider.Id);
                    
                    _logger.LogDebug("Request succeeded with provider {ProviderId}", provider.Id);
                    return response;
                }
                catch (Exception ex)
                {
                    _loadBalancer.CompleteRequest(trackingContext, success: false);
                    lastException = ex;
                    
                    await HandleProviderFailure(provider.Id, ex);
                    
                    _logger.LogWarning(ex, "Request failed with provider {ProviderId}, attempting failover", provider.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error with provider {ProviderId}", provider.Id);
                lastException = ex;
            }
        }

        // All providers failed, try fallback strategies
        return await HandleCompleteFallback(request, executeFunction, lastException!, cancellationToken);
    }

    /// <summary>
    /// Handles a provider failure and updates failover state.
    /// </summary>
    /// <param name="providerId">The ID of the failed provider.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    public Task HandleProviderFailure(string providerId, Exception exception)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(AIServiceFailover));
        
        var state = _providerStates.GetOrAdd(providerId, _ => new FailoverState
        {
            ProviderId = providerId,
            FailureCount = 0,
            LastFailure = DateTimeOffset.UtcNow,
            IsCircuitBreakerOpen = false
        });

        state.FailureCount++;
        state.LastFailure = DateTimeOffset.UtcNow;
        state.LastError = exception.Message;

        // Open circuit breaker if failure threshold exceeded
        if (state.FailureCount >= _config.MaxRetryAttempts && !state.IsCircuitBreakerOpen)
        {
            state.IsCircuitBreakerOpen = true;
            state.CircuitBreakerOpenTime = DateTimeOffset.UtcNow;
            
            _logger.LogWarning("Circuit breaker opened for provider {ProviderId} after {FailureCount} failures", 
                providerId, state.FailureCount);
        }

        _logger.LogDebug("Recorded failure for provider {ProviderId}: {FailureCount} total failures", 
            providerId, state.FailureCount);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Resets the failure state for a provider after successful requests.
    /// </summary>
    /// <param name="providerId">The provider ID to reset.</param>
    public void ResetProviderFailureState(string providerId)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(AIServiceFailover));
        
        if (_providerStates.TryGetValue(providerId, out var state))
        {
            state.FailureCount = 0;
            state.LastError = null;
            
            if (state.IsCircuitBreakerOpen)
            {
                state.IsCircuitBreakerOpen = false;
                state.CircuitBreakerOpenTime = null;
                
                _logger.LogInformation("Circuit breaker closed for provider {ProviderId} after successful recovery", providerId);
            }
        }
    }

    /// <summary>
    /// Gets the current failover status for all providers.
    /// </summary>
    /// <returns>Dictionary of provider failover status.</returns>
    public Dictionary<string, ProviderFailoverStatus> GetFailoverStatus()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(AIServiceFailover));
        
        return _providerStates.Values.ToDictionary(
            state => state.ProviderId,
            state => new ProviderFailoverStatus
            {
                ProviderId = state.ProviderId,
                FailureCount = state.FailureCount,
                LastFailure = state.LastFailure,
                IsCircuitBreakerOpen = state.IsCircuitBreakerOpen,
                LastError = state.LastError
            });
    }

    private async Task<AIResponse> HandleCompleteFallback<T>(
        T request,
        Func<AIProvider, T, CancellationToken, Task<AIResponse>> executeFunction,
        Exception lastException,
        CancellationToken cancellationToken) where T : class
    {
        _logger.LogWarning("All remote providers failed, applying fallback strategy {Strategy}", _config.Strategy);

        switch (_config.Strategy)
        {
            case FailoverStrategy.FailFast:
                throw new AIServiceException("All providers failed and failfast is enabled", "all", lastException);

            case FailoverStrategy.FallbackToLocal:
                return await FallbackToLocalAI(request, cancellationToken);

            case FailoverStrategy.QueueAndRetry:
                return await QueueForRetry(request, executeFunction, cancellationToken);

            case FailoverStrategy.FallbackToRemote:
                // Already tried all remote providers, fall through to exception
                break;
        }

        throw new AIServiceException("All failover strategies exhausted", "all", lastException);
    }

    private async Task<AIResponse> FallbackToLocalAI<T>(T request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Falling back to local AI service");
        
        // This would integrate with RFC-008-01 local AI service
        // For now, return a fallback response
        await Task.Delay(100, cancellationToken); // Simulate local processing
        
        return new AIResponse
        {
            Content = "Local AI fallback response - remote services unavailable",
            ModelId = "local-fallback",
            TokenUsage = new AITokenUsage(10, 10, 20, 0.001m),
            Metadata = new Dictionary<string, object> 
            { 
                ["fallback_reason"] = "remote_services_unavailable",
                ["fallback_type"] = "local_ai"
            }
        };
    }

    private Task<AIResponse> QueueForRetry<T>(
        T request,
        Func<AIProvider, T, CancellationToken, Task<AIResponse>> executeFunction,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Queuing request for retry when providers recover");
        
        var failedRequest = new FailedRequest
        {
            Id = Guid.NewGuid().ToString(),
            Request = request!,
            ExecuteFunction = (provider, req, ct) => executeFunction(provider, (T)req, ct),
            SubmittedAt = DateTimeOffset.UtcNow,
            CancellationToken = cancellationToken
        };
        
        _retryQueue.Enqueue(failedRequest);
        
        // Return a queued response for now - in practice might use TaskCompletionSource for async completion
        return Task.FromResult(new AIResponse
        {
            Content = "Request queued for retry when services recover",
            ModelId = "queued",
            Metadata = new Dictionary<string, object>
            {
                ["queue_position"] = _retryQueue.Count,
                ["request_id"] = failedRequest.Id
            }
        });
    }

    private async void ProcessRetryQueue(object? state)
    {
        if (_disposed || _retryQueue.IsEmpty) return;

        _logger.LogDebug("Processing retry queue with {QueueSize} requests", _retryQueue.Count);

        var processedCount = 0;
        const int maxProcessPerCycle = 10;

        while (processedCount < maxProcessPerCycle && _retryQueue.TryDequeue(out var failedRequest))
        {
            try
            {
                var provider = _loadBalancer.SelectProvider();
                if (provider != null && !failedRequest.CancellationToken.IsCancellationRequested)
                {
                    var response = await failedRequest.ExecuteFunction(provider, failedRequest.Request, failedRequest.CancellationToken);
                    _logger.LogInformation("Successfully retried queued request {RequestId}", failedRequest.Id);
                }
                else
                {
                    // Re-queue if no providers available yet
                    _retryQueue.Enqueue(failedRequest);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retry queued request {RequestId}", failedRequest.Id);
                // Could implement max retry attempts for queued items
            }

            processedCount++;
        }
    }

    private async void CheckProviderRecovery(object? state)
    {
        if (_disposed) return;

        var recoveryTasks = _providerStates.Values
            .Where(state => state.IsCircuitBreakerOpen)
            .Where(state => DateTimeOffset.UtcNow - state.CircuitBreakerOpenTime > TimeSpan.FromMinutes(5)) // Attempt recovery after 5 minutes
            .Select(async state =>
            {
                try
                {
                    // Simulate health check for recovery
                    await Task.Delay(100);
                    
                    if (Random.Shared.NextDouble() > 0.3) // 70% chance of recovery
                    {
                        ResetProviderFailureState(state.ProviderId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Provider {ProviderId} not yet recovered", state.ProviderId);
                }
            });

        await Task.WhenAll(recoveryTasks);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _retryTimer?.Dispose();
            _healthRecoveryTimer?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Represents the failover state for a provider.
/// </summary>
internal class FailoverState
{
    public required string ProviderId { get; init; }
    public volatile int FailureCount;
    public DateTimeOffset LastFailure;
    public volatile bool IsCircuitBreakerOpen;
    public DateTimeOffset? CircuitBreakerOpenTime;
    public string? LastError;
}

/// <summary>
/// Represents a request that failed and is queued for retry.
/// </summary>
internal record FailedRequest
{
    public required string Id { get; init; }
    public required object Request { get; init; }
    public required Func<AIProvider, object, CancellationToken, Task<AIResponse>> ExecuteFunction { get; init; }
    public DateTimeOffset SubmittedAt { get; init; }
    public CancellationToken CancellationToken { get; init; }
}

/// <summary>
/// Represents failover status for a specific provider.
/// </summary>
public record ProviderFailoverStatus
{
    /// <summary>Gets or sets the provider ID.</summary>
    public required string ProviderId { get; init; }
    
    /// <summary>Gets or sets the current failure count.</summary>
    public int FailureCount { get; init; }
    
    /// <summary>Gets or sets the timestamp of the last failure.</summary>
    public DateTimeOffset? LastFailure { get; init; }
    
    /// <summary>Gets or sets whether the circuit breaker is open.</summary>
    public bool IsCircuitBreakerOpen { get; init; }
    
    /// <summary>Gets or sets the last error message.</summary>
    public string? LastError { get; init; }
}