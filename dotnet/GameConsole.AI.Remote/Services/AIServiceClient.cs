using GameConsole.AI.Remote.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GameConsole.AI.Remote.Services;

/// <summary>
/// HTTP/gRPC client for communicating with remote AI service providers.
/// Implements resilience patterns with Polly for error handling and retries.
/// </summary>
public sealed class AIServiceClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AIServiceClient> _logger;
    private readonly AIEndpointConfig _endpointConfig;
    private readonly CachingConfig _cachingConfig;
    private readonly FailoverConfig _failoverConfig;
    private readonly SemaphoreSlim _concurrencySemaphore;

    private bool _disposed;
    private int _currentApiKeyIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIServiceClient"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client for making requests.</param>
    /// <param name="cache">Memory cache for response caching.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    /// <param name="endpointConfig">Configuration for the AI endpoint.</param>
    /// <param name="cachingConfig">Configuration for response caching.</param>
    /// <param name="failoverConfig">Configuration for failover behavior.</param>
    public AIServiceClient(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<AIServiceClient> logger,
        AIEndpointConfig endpointConfig,
        CachingConfig cachingConfig,
        FailoverConfig failoverConfig)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _endpointConfig = endpointConfig ?? throw new ArgumentNullException(nameof(endpointConfig));
        _cachingConfig = cachingConfig ?? throw new ArgumentNullException(nameof(cachingConfig));
        _failoverConfig = failoverConfig ?? throw new ArgumentNullException(nameof(failoverConfig));

        _concurrencySemaphore = new SemaphoreSlim(endpointConfig.MaxConcurrentRequests, endpointConfig.MaxConcurrentRequests);

        ConfigureHttpClient();
    }

    /// <summary>
    /// Gets the AI provider type for this client.
    /// </summary>
    public AIProvider Provider { get; init; }

    /// <summary>
    /// Gets a value indicating whether the client is healthy and can accept requests.
    /// </summary>
    public bool IsHealthy => !_disposed && _endpointConfig.IsEnabled;

    /// <summary>
    /// Sends a completion request to the remote AI service.
    /// </summary>
    /// <param name="request">The AI completion request.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The AI completion response.</returns>
    public async Task<AICompletionResponse> GetCompletionAsync(AICompletionRequest request, CancellationToken cancellationToken = default)
    {
        ServiceHelpers.ThrowIfDisposed(_disposed);
        ArgumentNullException.ThrowIfNull(request);

        var cacheKey = GenerateCacheKey(request);

        // Try to get from cache first
        if (_cachingConfig.IsEnabled && _cache.TryGetValue(cacheKey, out AICompletionResponse? cachedResponse))
        {
            _logger.LogDebug("Returning cached response for request {RequestId}", request.GetHashCode());
            return cachedResponse! with { FromCache = true };
        }

        await _concurrencySemaphore.WaitAsync(cancellationToken);
        
        try
        {
            var response = await ExecuteWithRetry(async () =>
            {
                using var httpRequest = CreateHttpRequest(request);
                using var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);

                await EnsureSuccessfulResponse(httpResponse);

                var responseContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                return ParseCompletionResponse(responseContent, request);
            }, cancellationToken);

            // Cache the response
            if (_cachingConfig.IsEnabled)
            {
                var expiration = _cachingConfig.ExpirationByPriority.GetValueOrDefault(request.Priority, _cachingConfig.DefaultExpiration);
                _cache.Set(cacheKey, response, expiration);
            }

            _logger.LogDebug("Successfully processed completion request for model {Model}", request.Model ?? _endpointConfig.DefaultModel);
            return response;
        }
        finally
        {
            _concurrencySemaphore.Release();
        }
    }

    /// <summary>
    /// Streams a completion response from the remote AI service.
    /// </summary>
    /// <param name="request">The AI completion request.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>An async enumerable of streaming response chunks.</returns>
    public async IAsyncEnumerable<AIStreamingChunk> GetStreamingCompletionAsync(AICompletionRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ServiceHelpers.ThrowIfDisposed(_disposed);
        ArgumentNullException.ThrowIfNull(request);

        await _concurrencySemaphore.WaitAsync(cancellationToken);

        try
        {
            using var httpRequest = CreateStreamingHttpRequest(request);
            using var httpResponse = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            await EnsureSuccessfulResponse(httpResponse);

            using var stream = await httpResponse.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            var requestId = Guid.NewGuid().ToString();
            string? line;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
                    continue;

                var data = line["data: ".Length..];

                if (data == "[DONE]")
                {
                    yield return new AIStreamingChunk
                    {
                        Content = "",
                        IsComplete = true,
                        Provider = Provider,
                        RequestId = requestId
                    };
                    break;
                }

                var chunk = ParseStreamingChunk(data, requestId);
                if (chunk != null)
                {
                    yield return chunk;
                }
            }
        }
        finally
        {
            _concurrencySemaphore.Release();
        }
    }

    /// <summary>
    /// Performs a health check on the remote AI service endpoint.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Health information for the endpoint.</returns>
    public async Task<EndpointHealth> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        ServiceHelpers.ThrowIfDisposed(_disposed);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Create a minimal test request
            var testRequest = new AICompletionRequest
            {
                Prompt = "Hello",
                MaxTokens = 1,
                Temperature = 0.0f
            };

            using var httpRequest = CreateHttpRequest(testRequest);
            using var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);

            stopwatch.Stop();

            if (httpResponse.IsSuccessStatusCode)
            {
                return new EndpointHealth
                {
                    Status = HealthStatus.Healthy,
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    LastSuccessfulRequest = DateTimeOffset.UtcNow
                };
            }

            return new EndpointHealth
            {
                Status = HealthStatus.Unhealthy,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                ErrorMessage = $"HTTP {(int)httpResponse.StatusCode}: {httpResponse.ReasonPhrase}"
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogWarning(ex, "Health check failed for {Provider} endpoint", Provider);

            return new EndpointHealth
            {
                Status = HealthStatus.Unhealthy,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Rotates to the next available API key if multiple keys are configured.
    /// </summary>
    public void RotateApiKey()
    {
        if (_endpointConfig.SecondaryApiKeys.Count > 0)
        {
            _currentApiKeyIndex = (_currentApiKeyIndex + 1) % (_endpointConfig.SecondaryApiKeys.Count + 1);
            _logger.LogInformation("Rotated to API key index {Index}", _currentApiKeyIndex);
        }
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_endpointConfig.BaseUrl);
        _httpClient.Timeout = _endpointConfig.RequestTimeout ?? TimeSpan.FromSeconds(30);

        // Add default headers
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "GameConsole.AI.Remote/1.0");

        // Add custom headers
        foreach (var header in _endpointConfig.AdditionalHeaders)
        {
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
        }
    }

    private async Task<T> ExecuteWithRetry<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
    {
        var maxRetries = _failoverConfig.MaxRetryAttempts;
        var exceptions = new List<Exception>();

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (ShouldRetry(ex, attempt))
            {
                exceptions.Add(ex);
                _logger.LogWarning(ex, "Request failed on attempt {Attempt}: {Message}", attempt + 1, ex.Message);

                if (attempt < maxRetries)
                {
                    var delay = CalculateRetryDelay(attempt);
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        throw new AggregateException("All retry attempts failed", exceptions);
    }

    private bool ShouldRetry(Exception exception, int attemptNumber)
    {
        if (attemptNumber >= _failoverConfig.MaxRetryAttempts)
            return false;

        return exception switch
        {
            HttpRequestException => true,
            TaskCanceledException when !exception.Message.Contains("timeout") => false,
            TaskCanceledException => true,
            System.Net.Sockets.SocketException => true,
            System.IO.IOException => true,
            _ => false
        };
    }

    private TimeSpan CalculateRetryDelay(int attemptNumber)
    {
        var baseDelay = _failoverConfig.BaseRetryDelay.TotalMilliseconds;
        var exponentialDelay = baseDelay * Math.Pow(_failoverConfig.BackoffMultiplier, attemptNumber);
        var finalDelay = Math.Min(exponentialDelay, _failoverConfig.MaxRetryDelay.TotalMilliseconds);
        return TimeSpan.FromMilliseconds(Math.Max(finalDelay, 0));
    }

    private HttpRequestMessage CreateHttpRequest(AICompletionRequest request)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, GetCompletionEndpoint());

        // Set authorization header
        var apiKey = GetCurrentApiKey();
        httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");

        // Create request body based on provider
        var requestBody = CreateRequestBody(request);
        httpRequest.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        return httpRequest;
    }

    private HttpRequestMessage CreateStreamingHttpRequest(AICompletionRequest request)
    {
        var httpRequest = CreateHttpRequest(request);
        httpRequest.Headers.Add("Accept", "text/event-stream");
        
        // Modify the request body to enable streaming
        var requestBody = CreateStreamingRequestBody(request);
        httpRequest.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        return httpRequest;
    }

    private string GetCompletionEndpoint()
    {
        return Provider switch
        {
            AIProvider.OpenAI => "/v1/chat/completions",
            AIProvider.Azure => "/openai/deployments/{deployment-id}/chat/completions?api-version=2024-02-15-preview",
            AIProvider.AWS => "/model/anthropic.claude-3-sonnet-20240229-v1:0/invoke",
            AIProvider.Anthropic => "/v1/messages",
            _ => "/v1/completions"
        };
    }

    private string CreateRequestBody(AICompletionRequest request)
    {
        var model = request.Model ?? _endpointConfig.DefaultModel;

        return Provider switch
        {
            AIProvider.OpenAI or AIProvider.Azure => JsonSerializer.Serialize(new
            {
                model,
                messages = new[] { new { role = "user", content = request.Prompt } },
                max_tokens = request.MaxTokens,
                temperature = request.Temperature,
                stream = false
            }),
            AIProvider.Anthropic => JsonSerializer.Serialize(new
            {
                model,
                max_tokens = request.MaxTokens,
                temperature = request.Temperature,
                messages = new[] { new { role = "user", content = request.Prompt } }
            }),
            AIProvider.AWS => JsonSerializer.Serialize(new
            {
                modelId = model,
                contentType = "application/json",
                accept = "application/json",
                body = JsonSerializer.Serialize(new
                {
                    anthropic_version = "bedrock-2023-05-31",
                    max_tokens = request.MaxTokens,
                    temperature = request.Temperature,
                    messages = new[] { new { role = "user", content = request.Prompt } }
                })
            }),
            _ => JsonSerializer.Serialize(new
            {
                model,
                prompt = request.Prompt,
                max_tokens = request.MaxTokens,
                temperature = request.Temperature
            })
        };
    }

    private string CreateStreamingRequestBody(AICompletionRequest request)
    {
        var model = request.Model ?? _endpointConfig.DefaultModel;

        return Provider switch
        {
            AIProvider.OpenAI or AIProvider.Azure => JsonSerializer.Serialize(new
            {
                model,
                messages = new[] { new { role = "user", content = request.Prompt } },
                max_tokens = request.MaxTokens,
                temperature = request.Temperature,
                stream = true
            }),
            AIProvider.Anthropic => JsonSerializer.Serialize(new
            {
                model,
                max_tokens = request.MaxTokens,
                temperature = request.Temperature,
                messages = new[] { new { role = "user", content = request.Prompt } },
                stream = true
            }),
            _ => CreateRequestBody(request) // Fallback to non-streaming for unsupported providers
        };
    }

    private AICompletionResponse ParseCompletionResponse(string responseContent, AICompletionRequest request)
    {
        using var jsonDoc = JsonDocument.Parse(responseContent);
        var root = jsonDoc.RootElement;

        var content = Provider switch
        {
            AIProvider.OpenAI or AIProvider.Azure => root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "",
            AIProvider.Anthropic => root.GetProperty("content")[0].GetProperty("text").GetString() ?? "",
            AIProvider.AWS => JsonSerializer.Deserialize<JsonElement>(root.GetProperty("body").GetString()!)
                .GetProperty("content")[0].GetProperty("text").GetString() ?? "",
            _ => root.GetProperty("choices")[0].GetProperty("text").GetString() ?? ""
        };

        var usage = ParseTokenUsage(root);
        var model = request.Model ?? _endpointConfig.DefaultModel;

        return new AICompletionResponse
        {
            Content = content,
            Model = model,
            Usage = usage,
            Provider = Provider,
            RequestId = Guid.NewGuid().ToString(),
            Timestamp = DateTimeOffset.UtcNow,
            FromCache = false
        };
    }

    private AIStreamingChunk? ParseStreamingChunk(string data, string requestId)
    {
        try
        {
            using var jsonDoc = JsonDocument.Parse(data);
            var root = jsonDoc.RootElement;

            var content = Provider switch
            {
                AIProvider.OpenAI or AIProvider.Azure => root.GetProperty("choices")[0].GetProperty("delta")
                    .TryGetProperty("content", out var contentProp) ? contentProp.GetString() : null,
                AIProvider.Anthropic => root.GetProperty("delta").TryGetProperty("text", out var textProp) ? textProp.GetString() : null,
                _ => null
            };

            if (content != null)
            {
                return new AIStreamingChunk
                {
                    Content = content,
                    IsComplete = false,
                    Provider = Provider,
                    RequestId = requestId
                };
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse streaming chunk: {Data}", data);
        }

        return null;
    }

    private TokenUsage ParseTokenUsage(JsonElement root)
    {
        if (!root.TryGetProperty("usage", out var usage))
        {
            return new TokenUsage { PromptTokens = 0, CompletionTokens = 0 };
        }

        return new TokenUsage
        {
            PromptTokens = usage.TryGetProperty("prompt_tokens", out var prompt) ? prompt.GetInt32() : 0,
            CompletionTokens = usage.TryGetProperty("completion_tokens", out var completion) ? completion.GetInt32() : 0
        };
    }

    private string GenerateCacheKey(AICompletionRequest request)
    {
        return _cachingConfig.KeyStrategy switch
        {
            CacheKeyStrategy.PromptAndModelHash => ComputeHash($"{request.Prompt}:{request.Model ?? _endpointConfig.DefaultModel}"),
            CacheKeyStrategy.FullRequestHash => ComputeHash(JsonSerializer.Serialize(request)),
            _ => ComputeHash(request.Prompt)
        };
    }

    private static string ComputeHash(string input)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash);
    }

    private string GetCurrentApiKey()
    {
        if (_currentApiKeyIndex == 0)
        {
            return _endpointConfig.ApiKey;
        }

        var secondaryIndex = _currentApiKeyIndex - 1;
        if (secondaryIndex < _endpointConfig.SecondaryApiKeys.Count)
        {
            return _endpointConfig.SecondaryApiKeys[secondaryIndex];
        }

        return _endpointConfig.ApiKey; // Fallback
    }

    private static async Task EnsureSuccessfulResponse(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Request failed with status {response.StatusCode}: {content}");
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="AIServiceClient"/>.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _concurrencySemaphore?.Dispose();
            _disposed = true;
        }
    }
}