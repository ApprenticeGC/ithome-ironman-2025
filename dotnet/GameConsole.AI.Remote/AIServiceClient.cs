using GameConsole.AI.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace GameConsole.AI.Remote;

/// <summary>
/// HTTP/gRPC client for communicating with remote AI services.
/// Handles communication with various AI providers with resilience patterns.
/// </summary>
public class AIServiceClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AIServiceClient> _logger;
    private readonly AIServiceClientOptions _options;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the AIServiceClient.
    /// </summary>
    /// <param name="httpClient">The HTTP client for making requests.</param>
    /// <param name="options">Configuration options for the client.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    public AIServiceClient(HttpClient httpClient, IOptions<AIServiceClientOptions> options, ILogger<AIServiceClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false,
            PropertyNameCaseInsensitive = true
        };

        _resiliencePipeline = CreateResiliencePipeline();
    }

    /// <summary>
    /// Sends a completion request to a remote AI provider.
    /// </summary>
    /// <param name="provider">The AI provider to send the request to.</param>
    /// <param name="request">The AI request to send.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the async operation that returns the AI response.</returns>
    public async Task<AIResponse> SendCompletionAsync(AIProvider provider, AIRequest request, CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(AIServiceClient));
        
        _logger.LogDebug("Sending completion request to provider {ProviderId} with model {ModelId}", provider.Id, request.ModelId);

        return await _resiliencePipeline.ExecuteAsync(async (ct) =>
        {
            var providerRequest = CreateProviderRequest(provider, request);
            var endpoint = GetCompletionEndpoint(provider);
            
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(providerRequest, options: _jsonOptions)
            };
            
            AddAuthenticationHeaders(httpRequestMessage, provider);
            
            using var response = await _httpClient.SendAsync(httpRequestMessage, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                throw new AIServiceException($"Request failed with status {response.StatusCode}: {errorContent}", provider.Id);
            }
            
            var responseContent = await response.Content.ReadAsStringAsync(ct);
            var providerResponse = JsonSerializer.Deserialize<ProviderResponse>(responseContent, _jsonOptions);
            
            var aiResponse = ConvertToAIResponse(providerResponse, request.ModelId);
            
            _logger.LogDebug("Received completion response from provider {ProviderId}: {TokenCount} tokens", 
                provider.Id, aiResponse.TokenUsage?.TotalTokens ?? 0);
            
            return aiResponse;
            
        }, cancellationToken);
    }

    /// <summary>
    /// Sends a streaming completion request to a remote AI provider.
    /// </summary>
    /// <param name="provider">The AI provider to send the request to.</param>
    /// <param name="request">The AI request to send.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>An async enumerable of AI response chunks.</returns>
    public async IAsyncEnumerable<AIResponseChunk> SendStreamingCompletionAsync(
        AIProvider provider, 
        AIRequest request, 
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(AIServiceClient));
        
        _logger.LogDebug("Sending streaming completion request to provider {ProviderId} with model {ModelId}", provider.Id, request.ModelId);

        var providerRequest = CreateProviderRequest(provider, request, streaming: true);
        var endpoint = GetCompletionEndpoint(provider);
        
        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(providerRequest, options: _jsonOptions)
        };
        
        AddAuthenticationHeaders(httpRequestMessage, provider);
        
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new AIServiceException($"Failed to send streaming request: {ex.Message}", provider.Id, ex);
        }
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            response.Dispose();
            throw new AIServiceException($"Streaming request failed with status {response.StatusCode}: {errorContent}", provider.Id);
        }
        
        using (response)
        {
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);
            
            var chunkIndex = 0;
            string? line;
            
            while ((line = await reader.ReadLineAsync()) != null && !cancellationToken.IsCancellationRequested)
            {
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
                    continue;
                    
                var jsonData = line.Substring(6); // Remove "data: " prefix
                
                if (jsonData.Trim() == "[DONE]")
                {
                    yield return new AIResponseChunk
                    {
                        Content = string.Empty,
                        IsFinal = true,
                        Index = chunkIndex++
                    };
                    break;
                }
                
                ProviderStreamChunk? chunkData = null;
                try
                {
                    chunkData = JsonSerializer.Deserialize<ProviderStreamChunk>(jsonData, _jsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse streaming chunk: {JsonData}", jsonData);
                    continue;
                }
                
                if (chunkData?.Content != null)
                {
                    yield return new AIResponseChunk
                    {
                        Content = chunkData.Content,
                        IsFinal = false,
                        Index = chunkIndex++
                    };
                }
            }
        }
    }

    /// <summary>
    /// Checks the health of a remote AI provider.
    /// </summary>
    /// <param name="provider">The AI provider to check.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the async operation that returns health status.</returns>
    public async Task<AIHealthStatus> CheckHealthAsync(AIProvider provider, CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(AIServiceClient));
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            var endpoint = GetHealthEndpoint(provider);
            using var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            
            stopwatch.Stop();
            
            var isHealthy = response.IsSuccessStatusCode;
            var status = isHealthy ? "Healthy" : $"Unhealthy: {response.StatusCode}";
            
            return new AIHealthStatus
            {
                IsHealthy = isHealthy,
                Status = status,
                ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds,
                Error = isHealthy ? null : await response.Content.ReadAsStringAsync(cancellationToken)
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogWarning(ex, "Health check failed for provider {ProviderId}", provider.Id);
            
            return new AIHealthStatus
            {
                IsHealthy = false,
                Status = "Error",
                ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds,
                Error = ex.Message
            };
        }
    }

    private ResiliencePipeline CreateResiliencePipeline()
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new()
            {
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>().Handle<TaskCanceledException>(),
                MaxRetryAttempts = _options.MaxRetryAttempts,
                Delay = TimeSpan.FromMilliseconds(1000),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true
            })
            .AddTimeout(_options.RequestTimeout)
            .AddCircuitBreaker(new()
            {
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>().Handle<TaskCanceledException>(),
                FailureRatio = 0.5,
                MinimumThroughput = 10,
                BreakDuration = TimeSpan.FromSeconds(30)
            })
            .Build();
    }

    private object CreateProviderRequest(AIProvider provider, AIRequest request, bool streaming = false)
    {
        return provider.Type switch
        {
            AIProviderType.OpenAI => new
            {
                model = request.ModelId,
                messages = new[] { new { role = "user", content = request.Prompt } },
                max_tokens = request.MaxTokens,
                temperature = request.Temperature,
                stream = streaming
            },
            AIProviderType.Azure => new
            {
                model = request.ModelId,
                messages = new[] { new { role = "user", content = request.Prompt } },
                max_tokens = request.MaxTokens,
                temperature = request.Temperature,
                stream = streaming
            },
            AIProviderType.AWS => new
            {
                modelId = request.ModelId,
                contentType = "application/json",
                accept = "application/json",
                body = JsonSerializer.Serialize(new
                {
                    inputText = request.Prompt,
                    textGenerationConfig = new
                    {
                        maxTokenCount = request.MaxTokens,
                        temperature = request.Temperature
                    }
                })
            },
            _ => throw new NotSupportedException($"Provider type {provider.Type} is not supported")
        };
    }

    private string GetCompletionEndpoint(AIProvider provider)
    {
        return provider.Type switch
        {
            AIProviderType.OpenAI => $"{provider.BaseUrl}/v1/chat/completions",
            AIProviderType.Azure => $"{provider.BaseUrl}/openai/deployments/{GetDeploymentName(provider)}/chat/completions?api-version=2024-02-15-preview",
            AIProviderType.AWS => $"{provider.BaseUrl}/model/invoke",
            _ => throw new NotSupportedException($"Provider type {provider.Type} is not supported")
        };
    }

    private string GetHealthEndpoint(AIProvider provider)
    {
        return provider.Type switch
        {
            AIProviderType.OpenAI => $"{provider.BaseUrl}/v1/models",
            AIProviderType.Azure => $"{provider.BaseUrl}/openai/deployments?api-version=2024-02-15-preview",
            AIProviderType.AWS => $"{provider.BaseUrl}/",
            _ => provider.BaseUrl
        };
    }

    private void AddAuthenticationHeaders(HttpRequestMessage request, AIProvider provider)
    {
        var apiKey = _options.GetApiKey(provider.Id);
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new AIServiceException($"API key not configured for provider {provider.Id}", provider.Id);
        }

        switch (provider.Type)
        {
            case AIProviderType.OpenAI:
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                break;
            case AIProviderType.Azure:
                request.Headers.Add("api-key", apiKey);
                break;
            case AIProviderType.AWS:
                // AWS requires signature v4 - simplified for example
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("AWS4-HMAC-SHA256", apiKey);
                break;
        }
    }

    private string GetDeploymentName(AIProvider provider)
    {
        return _options.GetDeploymentName(provider.Id) ?? "gpt-4";
    }

    private AIResponse ConvertToAIResponse(ProviderResponse? providerResponse, string modelId)
    {
        if (providerResponse == null)
            throw new AIServiceException("Empty response from provider", "unknown");

        return new AIResponse
        {
            Content = providerResponse.Content ?? string.Empty,
            ModelId = modelId,
            TokenUsage = providerResponse.Usage != null ? new AITokenUsage(
                providerResponse.Usage.PromptTokens,
                providerResponse.Usage.CompletionTokens,
                providerResponse.Usage.TotalTokens) : null
        };
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}

/// <summary>
/// Configuration options for the AI service client.
/// </summary>
public class AIServiceClientOptions
{
    /// <summary>Gets or sets the request timeout.</summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromMinutes(2);
    
    /// <summary>Gets or sets the maximum retry attempts.</summary>
    public int MaxRetryAttempts { get; set; } = 3;
    
    /// <summary>Gets or sets the API keys for each provider.</summary>
    public Dictionary<string, string> ApiKeys { get; set; } = new();
    
    /// <summary>Gets or sets the deployment names for Azure providers.</summary>
    public Dictionary<string, string> DeploymentNames { get; set; } = new();

    /// <summary>Gets the API key for a specific provider.</summary>
    public string? GetApiKey(string providerId) => ApiKeys.TryGetValue(providerId, out var key) ? key : null;
    
    /// <summary>Gets the deployment name for a specific provider.</summary>
    public string? GetDeploymentName(string providerId) => DeploymentNames.TryGetValue(providerId, out var name) ? name : null;
}

/// <summary>
/// Represents a provider-specific response structure.
/// </summary>
internal record ProviderResponse(string? Content, ProviderUsage? Usage);

/// <summary>
/// Represents provider-specific usage information.
/// </summary>
internal record ProviderUsage(int PromptTokens, int CompletionTokens, int TotalTokens);

/// <summary>
/// Represents a provider-specific streaming chunk.
/// </summary>
internal record ProviderStreamChunk(string? Content);

/// <summary>
/// Exception thrown when AI service operations fail.
/// </summary>
public class AIServiceException : Exception
{
    /// <summary>Gets the provider ID associated with the error.</summary>
    public string ProviderId { get; }

    /// <summary>
    /// Initializes a new instance of the AIServiceException.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="providerId">The provider ID.</param>
    public AIServiceException(string message, string providerId) : base(message)
    {
        ProviderId = providerId;
    }

    /// <summary>
    /// Initializes a new instance of the AIServiceException.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="providerId">The provider ID.</param>
    /// <param name="innerException">The inner exception.</param>
    public AIServiceException(string message, string providerId, Exception innerException) : base(message, innerException)
    {
        ProviderId = providerId;
    }
}