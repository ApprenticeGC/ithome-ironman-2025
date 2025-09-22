using GameConsole.AI.Remote.Configuration;
using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Remote.Services;

/// <summary>
/// Handles failover and retry logic for remote AI services.
/// Provides automatic fallback to alternative endpoints and local AI when remote services fail.
/// </summary>
public sealed class AIServiceFailover
{
    private readonly ILogger<AIServiceFailover> _logger;
    private readonly FailoverConfig _config;
    private readonly RemoteAILoadBalancer _loadBalancer;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIServiceFailover"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic messages.</param>
    /// <param name="config">Failover configuration.</param>
    /// <param name="loadBalancer">Load balancer for selecting endpoints.</param>
    public AIServiceFailover(
        ILogger<AIServiceFailover> logger,
        FailoverConfig config,
        RemoteAILoadBalancer loadBalancer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _loadBalancer = loadBalancer ?? throw new ArgumentNullException(nameof(loadBalancer));
    }

    /// <summary>
    /// Delegate for local AI fallback operations.
    /// </summary>
    /// <param name="request">The AI completion request.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The AI completion response from local service.</returns>
    public delegate Task<AICompletionResponse> LocalAIFallbackDelegate(AICompletionRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Executes a request with automatic failover and retry logic.
    /// </summary>
    /// <param name="request">The AI completion request.</param>
    /// <param name="localFallback">Optional local AI fallback function.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The AI completion response.</returns>
    /// <exception cref="AIServiceException">Thrown when all retry attempts and fallbacks fail.</exception>
    public async Task<AICompletionResponse> ExecuteWithFailoverAsync(
        AICompletionRequest request,
        LocalAIFallbackDelegate? localFallback = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_config.IsEnabled)
        {
            // If failover is disabled, try once with the load balancer
            var client = _loadBalancer.SelectEndpoint(request);
            if (client == null)
            {
                throw new AIServiceException("No healthy endpoints available and failover is disabled");
            }

            return await client.GetCompletionAsync(request, cancellationToken);
        }

        var exceptions = new List<Exception>();
        var startTime = DateTimeOffset.UtcNow;

        // Attempt with remote endpoints first
        for (int attempt = 1; attempt <= _config.MaxRetryAttempts + 1; attempt++)
        {
            try
            {
                var client = _loadBalancer.SelectEndpoint(request);
                if (client == null)
                {
                    _logger.LogWarning("No healthy remote endpoints available on attempt {Attempt}", attempt);
                    break; // Exit to try local fallback
                }

                var response = await client.GetCompletionAsync(request, cancellationToken);
                
                // Record successful request
                _loadBalancer.RecordRequestCompletion(response.Provider, 
                    (DateTimeOffset.UtcNow - startTime).TotalMilliseconds, true, response.Usage.TotalTokens);

                _logger.LogDebug("Request succeeded on attempt {Attempt} using {Provider}", attempt, response.Provider);
                return response;
            }
            catch (Exception ex) when (ShouldRetry(ex, attempt))
            {
                exceptions.Add(ex);
                
                _logger.LogWarning(ex, "Request failed on attempt {Attempt}: {Message}", attempt, ex.Message);

                // Record failed request if we can determine the provider
                if (ex.Data.Contains("Provider") && Enum.TryParse<AIProvider>(ex.Data["Provider"]?.ToString(), out var provider))
                {
                    _loadBalancer.RecordRequestCompletion(provider, 
                        (DateTimeOffset.UtcNow - startTime).TotalMilliseconds, false);
                }

                if (attempt <= _config.MaxRetryAttempts)
                {
                    var delay = CalculateRetryDelay(attempt);
                    _logger.LogDebug("Waiting {Delay}ms before retry attempt {NextAttempt}", delay.TotalMilliseconds, attempt + 1);
                    
                    try
                    {
                        await Task.Delay(delay, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("Request cancelled during retry delay");
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                _logger.LogError(ex, "Non-retryable error on attempt {Attempt}: {Message}", attempt, ex.Message);
                break; // Exit to try local fallback for non-retryable errors
            }
        }

        // Try local fallback if enabled and available
        if (_config.EnableLocalFallback && localFallback != null)
        {
            try
            {
                _logger.LogInformation("Attempting local AI fallback after remote failures");
                var response = await localFallback(request, cancellationToken);
                
                _logger.LogInformation("Local AI fallback succeeded");
                return response;
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                _logger.LogError(ex, "Local AI fallback also failed: {Message}", ex.Message);
            }
        }

        // All attempts failed
        var aggregateException = new AggregateException(exceptions);
        var totalTime = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
        
        _logger.LogError("All retry attempts and fallbacks failed after {TotalTime}ms. Attempts: {AttemptCount}, Exceptions: {ExceptionCount}",
            totalTime, _config.MaxRetryAttempts + 1, exceptions.Count);

        throw new AIServiceException(
            $"Request failed after {_config.MaxRetryAttempts + 1} attempts and fallback. See inner exception for details.",
            aggregateException);
    }

    /// <summary>
    /// Executes a streaming request with automatic failover and retry logic.
    /// </summary>
    /// <param name="request">The AI completion request.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>An async enumerable of streaming response chunks.</returns>
    /// <exception cref="AIServiceException">Thrown when all retry attempts fail.</exception>
    public async IAsyncEnumerable<AIStreamingChunk> ExecuteStreamingWithFailoverAsync(
        AICompletionRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var chunks = await ExecuteStreamingRequestWithRetry(request, cancellationToken);

        foreach (var chunk in chunks)
        {
            yield return chunk;
        }
    }

    private async Task<List<AIStreamingChunk>> ExecuteStreamingRequestWithRetry(
        AICompletionRequest request,
        CancellationToken cancellationToken)
    {
        var exceptions = new List<Exception>();
        var startTime = DateTimeOffset.UtcNow;

        for (int attempt = 1; attempt <= _config.MaxRetryAttempts + 1; attempt++)
        {
            try
            {
                var client = _loadBalancer.SelectEndpoint(request);
                if (client == null)
                {
                    throw new AIServiceException("No healthy endpoints available for streaming request");
                }

                var chunks = new List<AIStreamingChunk>();
                await foreach (var chunk in client.GetStreamingCompletionAsync(request, cancellationToken))
                {
                    chunks.Add(chunk);
                    
                    if (chunk.IsComplete)
                    {
                        // Record successful streaming request
                        _loadBalancer.RecordRequestCompletion(chunk.Provider,
                            (DateTimeOffset.UtcNow - startTime).TotalMilliseconds, true);
                        break;
                    }
                }

                if (chunks.Count == 0)
                {
                    throw new AIServiceException("Streaming request completed but no chunks were received");
                }

                return chunks;
            }
            catch (Exception ex) when (ShouldRetry(ex, attempt) && attempt <= _config.MaxRetryAttempts)
            {
                exceptions.Add(ex);
                _logger.LogWarning(ex, "Streaming request failed on attempt {Attempt}: {Message}", attempt, ex.Message);

                var delay = CalculateRetryDelay(attempt);
                _logger.LogDebug("Waiting {Delay}ms before streaming retry attempt {NextAttempt}", delay.TotalMilliseconds, attempt + 1);

                try
                {
                    await Task.Delay(delay, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Streaming request cancelled during retry delay");
                    throw;
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                
                if (ShouldRetry(ex, attempt) && attempt <= _config.MaxRetryAttempts)
                {
                    _logger.LogWarning(ex, "Streaming request failed on attempt {Attempt}, retrying: {Message}", attempt, ex.Message);
                    continue;
                }

                _logger.LogError(ex, "Non-retryable streaming error on attempt {Attempt}: {Message}", attempt, ex.Message);
                break;
            }
        }

        // All attempts failed
        var aggregateException = new AggregateException(exceptions);
        var totalTime = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;

        _logger.LogError("All streaming retry attempts failed after {TotalTime}ms. Attempts: {AttemptCount}, Exceptions: {ExceptionCount}",
            totalTime, _config.MaxRetryAttempts + 1, exceptions.Count);

        throw new AIServiceException(
            $"Streaming request failed after {_config.MaxRetryAttempts + 1} attempts. See inner exception for details.",
            aggregateException);
    }

    /// <summary>
    /// Checks if a request should be retried based on the exception and attempt number.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="attemptNumber">The current attempt number.</param>
    /// <returns>True if the request should be retried; otherwise, false.</returns>
    private bool ShouldRetry(Exception exception, int attemptNumber)
    {
        // Don't retry if we've exceeded the maximum attempts
        if (attemptNumber > _config.MaxRetryAttempts)
        {
            return false;
        }

        // Don't retry cancellation requests
        if (exception is OperationCanceledException)
        {
            return false;
        }

        // Check for HTTP request exceptions with retryable status codes
        if (exception is HttpRequestException httpEx)
        {
            // Try to extract status code from the message (this is a simplification)
            foreach (var statusCode in _config.FailoverHttpStatusCodes)
            {
                if (httpEx.Message.Contains(statusCode.ToString()))
                {
                    _logger.LogDebug("HTTP status {StatusCode} is retryable", statusCode);
                    return true;
                }
            }
        }

        // Check for specific exception types that should trigger retries
        var exceptionTypeName = exception.GetType().FullName ?? exception.GetType().Name;
        if (_config.FailoverExceptionTypes.Contains(exceptionTypeName))
        {
            _logger.LogDebug("Exception type {ExceptionType} is retryable", exceptionTypeName);
            return true;
        }

        // Check for common transient failures
        return exception switch
        {
            HttpRequestException => true,
            TaskCanceledException when !exception.Message.Contains("timeout") => false, // User cancellation
            TaskCanceledException => true, // Timeout
            System.Net.Sockets.SocketException => true,
            System.IO.IOException => true,
            _ => false
        };
    }

    /// <summary>
    /// Calculates the delay before the next retry attempt using exponential backoff with jitter.
    /// </summary>
    /// <param name="attemptNumber">The current attempt number (1-based).</param>
    /// <returns>The delay to wait before the next attempt.</returns>
    private TimeSpan CalculateRetryDelay(int attemptNumber)
    {
        // Exponential backoff with jitter
        var baseDelay = _config.BaseRetryDelay.TotalMilliseconds;
        var exponentialDelay = baseDelay * Math.Pow(_config.BackoffMultiplier, attemptNumber - 1);
        
        // Apply jitter (±25% randomization)
        var jitter = (Random.Shared.NextDouble() - 0.5) * 0.5; // -0.25 to +0.25
        var jitteredDelay = exponentialDelay * (1 + jitter);
        
        // Ensure we don't exceed the maximum delay
        var finalDelay = Math.Min(jitteredDelay, _config.MaxRetryDelay.TotalMilliseconds);
        
        return TimeSpan.FromMilliseconds(Math.Max(finalDelay, 0));
    }
}

/// <summary>
/// Exception thrown when AI service operations fail.
/// </summary>
public class AIServiceException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIServiceException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public AIServiceException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AIServiceException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public AIServiceException(string message, Exception innerException) : base(message, innerException) { }
}