# RFC-008: Local vs Remote AI Deployment Strategy

- **Start Date**: 2025-01-15
- **RFC Author**: Claude
- **Status**: Draft
- **Depends On**: RFC-007

## Summary

This RFC defines deployment strategies for AI agents in GameConsole, supporting both local (Ollama, local models) and remote (OpenAI, Azure OpenAI) AI backends with intelligent switching, cost optimization, and offline capability. The system provides unified API access while optimizing for performance, cost, and availability.

## Motivation

GameConsole AI integration needs flexible deployment options to support:

1. **Local Development**: Offline AI capabilities using local models
2. **Cost Management**: Balance between local compute and API costs
3. **Privacy Requirements**: Keep sensitive data local when required
4. **Performance Optimization**: Minimize latency for real-time features
5. **Reliability**: Fallback strategies when remote services unavailable
6. **Resource Constraints**: Adapt to different deployment environments

## Detailed Design

### AI Deployment Architecture

```
Local Deployment              Hybrid Deployment           Remote Deployment
┌─────────────────────────┐    ┌──────────────────────────┐    ┌─────────────────────────┐
│ GameConsole Instance    │    │ GameConsole Instance     │    │ GameConsole Instance    │
│ ├── Ollama Service      │    │ ├── Local Models (Cache) │    │ ├── Remote API Client   │
│ ├── Local Models        │    │ ├── Remote Fallback      │    │ ├── Request Queue       │
│ ├── Model Cache         │    │ ├── Intelligent Routing  │    │ ├── Response Cache      │
│ └── Embedding DB        │    │ └── Cost Optimizer       │    │ └── Rate Limiter        │
└─────────────────────────┘    └──────────────────────────┘    └─────────────────────────┘
```

### Deployment Configuration

```csharp
// GameConsole.AI.Configuration/src/AIDeploymentConfig.cs
public class AIDeploymentConfig
{
    /// <summary>
    /// Primary deployment strategy
    /// </summary>
    public DeploymentStrategy Strategy { get; set; } = DeploymentStrategy.Hybrid;

    /// <summary>
    /// Local model configuration
    /// </summary>
    public LocalModelConfig LocalModels { get; set; } = new();

    /// <summary>
    /// Remote service configuration
    /// </summary>
    public RemoteServiceConfig RemoteServices { get; set; } = new();

    /// <summary>
    /// Cost and performance preferences
    /// </summary>
    public OptimizationConfig Optimization { get; set; } = new();

    /// <summary>
    /// Fallback and retry policies
    /// </summary>
    public ResilienceConfig Resilience { get; set; } = new();
}

public enum DeploymentStrategy
{
    LocalOnly,      // Only use local models
    RemoteOnly,     // Only use remote APIs
    Hybrid,         // Intelligent routing between local/remote
    LocalFirst,     // Prefer local, fallback to remote
    RemoteFirst     // Prefer remote, fallback to local
}

public class LocalModelConfig
{
    public string OllamaEndpoint { get; set; } = "http://localhost:11434";
    public Dictionary<string, LocalModel> AvailableModels { get; set; } = new();
    public string ModelCachePath { get; set; } = "./models";
    public long MaxCacheSize { get; set; } = 10_000_000_000; // 10GB
    public bool AutoDownloadModels { get; set; } = true;
}

public record LocalModel(
    string Name,
    string ModelFile,
    long SizeBytes,
    AICapability[] Capabilities,
    PerformanceProfile Performance);

public class RemoteServiceConfig
{
    public Dictionary<string, RemoteProvider> Providers { get; set; } = new();
    public string DefaultProvider { get; set; } = "OpenAI";
    public int RequestTimeoutSeconds { get; set; } = 30;
    public int MaxConcurrentRequests { get; set; } = 10;
}

public record RemoteProvider(
    string Name,
    string ApiEndpoint,
    string ApiKeyConfigPath,
    Dictionary<string, RemoteModel> Models,
    RateLimitConfig RateLimit,
    CostConfig Pricing);
```

### AI Backend Abstraction

```csharp
// GameConsole.AI.Abstraction/src/IAIBackend.cs
public interface IAIBackend
{
    string Name { get; }
    DeploymentType Type { get; }
    bool IsAvailable { get; }
    IReadOnlySet<AICapability> SupportedCapabilities { get; }

    Task<bool> InitializeAsync(CancellationToken cancellationToken = default);
    Task<AIResponse> ProcessAsync(AIRequest request, CancellationToken cancellationToken = default);
    Task<Stream> ProcessStreamAsync(AIRequest request, CancellationToken cancellationToken = default);
    Task<HealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default);
}

public enum DeploymentType
{
    Local,
    Remote,
    Hybrid
}

public record AIRequest(
    string Prompt,
    AICapability RequiredCapability,
    Dictionary<string, object> Parameters,
    QualityOfService QoS);

public record QualityOfService(
    int MaxTokens,
    TimeSpan MaxLatency,
    decimal MaxCost,
    bool RequireOffline);
```

### Local AI Backend (Ollama)

```csharp
// GameConsole.AI.Local/src/OllamaBackend.cs
[ProviderFor(typeof(IAIBackend))]
[Capability("text-generation")]
[Capability("offline")]
[Priority(70)] // High priority for local-first scenarios
public class OllamaBackend : IAIBackend
{
    private readonly HttpClient _httpClient;
    private readonly LocalModelConfig _config;
    private readonly ILogger<OllamaBackend> _logger;

    public string Name => "Ollama";
    public DeploymentType Type => DeploymentType.Local;
    public bool IsAvailable => _isInitialized && _ollamaRunning;

    private bool _isInitialized;
    private bool _ollamaRunning;

    public async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if Ollama is running
            var response = await _httpClient.GetAsync($"{_config.OllamaEndpoint}/api/tags", cancellationToken);
            _ollamaRunning = response.IsSuccessStatusCode;

            if (!_ollamaRunning)
            {
                _logger.LogWarning("Ollama service not available at {Endpoint}", _config.OllamaEndpoint);
                return false;
            }

            // Verify required models are available
            await EnsureModelsAvailableAsync(cancellationToken);

            _isInitialized = true;
            _logger.LogInformation("Ollama backend initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Ollama backend");
            return false;
        }
    }

    public async Task<AIResponse> ProcessAsync(AIRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
            throw new AIBackendUnavailableException("Ollama backend is not available");

        var model = SelectBestModel(request.RequiredCapability, request.QoS);

        var ollamaRequest = new
        {
            model = model.Name,
            prompt = request.Prompt,
            options = BuildOptions(request.Parameters, request.QoS),
            stream = false
        };

        var requestJson = JsonSerializer.Serialize(ollamaRequest);
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        var stopwatch = Stopwatch.StartNew();
        var response = await _httpClient.PostAsync($"{_config.OllamaEndpoint}/api/generate", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseJson);

        return new AIResponse(
            Text: ollamaResponse.Response,
            Tokens: ollamaResponse.TokenCount ?? 0,
            Latency: stopwatch.Elapsed,
            Cost: 0, // No cost for local processing
            Metadata: new Dictionary<string, object>
            {
                ["model"] = model.Name,
                ["backend"] = Name,
                ["type"] = Type.ToString()
            });
    }

    private LocalModel SelectBestModel(AICapability capability, QualityOfService qos)
    {
        return _config.AvailableModels.Values
            .Where(m => m.Capabilities.Contains(capability))
            .Where(m => m.Performance.AverageLatency <= qos.MaxLatency)
            .OrderByDescending(m => m.Performance.Quality)
            .First();
    }
}
```

### Remote AI Backend (OpenAI/Azure)

```csharp
// GameConsole.AI.Remote/src/OpenAIBackend.cs
[ProviderFor(typeof(IAIBackend))]
[Capability("text-generation")]
[Capability("image-generation")]
[Capability("embeddings")]
[Priority(80)] // High priority for remote-first scenarios
public class OpenAIBackend : IAIBackend
{
    private readonly OpenAIClient _client;
    private readonly RemoteServiceConfig _config;
    private readonly IRateLimiter _rateLimiter;
    private readonly ICostTracker _costTracker;
    private readonly ILogger<OpenAIBackend> _logger;

    public string Name => "OpenAI";
    public DeploymentType Type => DeploymentType.Remote;
    public bool IsAvailable => _isInitialized && !_rateLimited;

    public async Task<AIResponse> ProcessAsync(AIRequest request, CancellationToken cancellationToken = default)
    {
        // Check rate limits and cost constraints
        await _rateLimiter.WaitAsync(cancellationToken);

        var estimatedCost = EstimateCost(request);
        if (estimatedCost > request.QoS.MaxCost)
        {
            throw new CostLimitExceededException($"Estimated cost {estimatedCost:C} exceeds limit {request.QoS.MaxCost:C}");
        }

        if (request.QoS.RequireOffline)
        {
            throw new OfflineRequiredException("Request requires offline processing but backend is remote");
        }

        var model = SelectBestRemoteModel(request.RequiredCapability, request.QoS);

        try
        {
            var stopwatch = Stopwatch.StartNew();
            var completion = await _client.GetCompletionsAsync(new CompletionsOptions
            {
                DeploymentName = model.DeploymentName,
                Prompts = { request.Prompt },
                MaxTokens = request.QoS.MaxTokens,
                Temperature = GetParameter<float>(request.Parameters, "temperature", 0.7f)
            }, cancellationToken);

            stopwatch.Stop();

            var response = completion.Value.Choices[0];
            var actualCost = CalculateActualCost(response.LogProbabilities?.TokenLogProbabilities?.Count ?? 0, model);

            await _costTracker.RecordCostAsync(actualCost, cancellationToken);

            return new AIResponse(
                Text: response.Text,
                Tokens: response.LogProbabilities?.TokenLogProbabilities?.Count ?? 0,
                Latency: stopwatch.Elapsed,
                Cost: actualCost,
                Metadata: new Dictionary<string, object>
                {
                    ["model"] = model.Name,
                    ["backend"] = Name,
                    ["type"] = Type.ToString(),
                    ["finish_reason"] = response.FinishReason?.ToString() ?? "unknown"
                });
        }
        catch (RequestFailedException ex) when (ex.Status == 429)
        {
            _logger.LogWarning("Rate limit exceeded, backing off");
            await _rateLimiter.BackoffAsync(cancellationToken);
            throw new RateLimitExceededException("OpenAI rate limit exceeded", ex);
        }
    }
}
```

### Intelligent Backend Routing

```csharp
// GameConsole.AI.Routing/src/IntelligentRouter.cs
public class IntelligentRouter : IAIBackend
{
    private readonly IServiceRegistry<IAIBackend> _backendRegistry;
    private readonly AIDeploymentConfig _config;
    private readonly IMetricsCollector _metrics;
    private readonly ILogger<IntelligentRouter> _logger;

    public string Name => "IntelligentRouter";
    public DeploymentType Type => DeploymentType.Hybrid;
    public bool IsAvailable => _backendRegistry.GetProviders().Any(b => b.IsAvailable);

    public async Task<AIResponse> ProcessAsync(AIRequest request, CancellationToken cancellationToken = default)
    {
        var routingDecision = await MakeRoutingDecisionAsync(request, cancellationToken);
        var selectedBackend = routingDecision.Backend;

        _logger.LogInformation("Routing request to {Backend} (reason: {Reason})",
            selectedBackend.Name, routingDecision.Reason);

        try
        {
            var response = await selectedBackend.ProcessAsync(request, cancellationToken);

            // Record successful routing decision
            await _metrics.RecordRoutingSuccessAsync(selectedBackend.Name, response.Latency, response.Cost);

            return response;
        }
        catch (Exception ex) when (ShouldRetryWithFallback(ex))
        {
            _logger.LogWarning(ex, "Backend {Backend} failed, trying fallback", selectedBackend.Name);

            var fallbackBackend = await SelectFallbackBackendAsync(request, selectedBackend, cancellationToken);
            if (fallbackBackend != null)
            {
                return await fallbackBackend.ProcessAsync(request, cancellationToken);
            }

            throw;
        }
    }

    private async Task<RoutingDecision> MakeRoutingDecisionAsync(AIRequest request, CancellationToken cancellationToken)
    {
        var availableBackends = _backendRegistry.GetProviders()
            .Where(b => b.IsAvailable && b.SupportedCapabilities.Contains(request.RequiredCapability))
            .ToList();

        if (!availableBackends.Any())
            throw new NoSuitableBackendException($"No backend available for capability {request.RequiredCapability}");

        return _config.Strategy switch
        {
            DeploymentStrategy.LocalOnly => SelectLocal(availableBackends, request),
            DeploymentStrategy.RemoteOnly => SelectRemote(availableBackends, request),
            DeploymentStrategy.LocalFirst => SelectLocalFirst(availableBackends, request),
            DeploymentStrategy.RemoteFirst => SelectRemoteFirst(availableBackends, request),
            DeploymentStrategy.Hybrid => await SelectOptimalAsync(availableBackends, request, cancellationToken),
            _ => throw new ArgumentException($"Unknown strategy: {_config.Strategy}")
        };
    }

    private async Task<RoutingDecision> SelectOptimalAsync(
        List<IAIBackend> backends,
        AIRequest request,
        CancellationToken cancellationToken)
    {
        // Collect metrics for decision making
        var decisions = new List<(IAIBackend Backend, double Score, string Reason)>();

        foreach (var backend in backends)
        {
            var metrics = await _metrics.GetBackendMetricsAsync(backend.Name, cancellationToken);
            var score = CalculateBackendScore(backend, request, metrics);
            var reason = BuildScoreReason(backend, score, metrics);

            decisions.Add((backend, score, reason));
        }

        var best = decisions.OrderByDescending(d => d.Score).First();
        return new RoutingDecision(best.Backend, best.Reason);
    }

    private double CalculateBackendScore(IAIBackend backend, AIRequest request, BackendMetrics metrics)
    {
        double score = 100.0;

        // Latency preference (higher weight for real-time scenarios)
        if (request.QoS.MaxLatency < TimeSpan.FromSeconds(2))
        {
            score += backend.Type == DeploymentType.Local ? 50 : -50;
        }

        // Cost preference
        if (request.QoS.MaxCost < 0.01m) // Very low cost requirement
        {
            score += backend.Type == DeploymentType.Local ? 40 : -40;
        }

        // Offline requirement
        if (request.QoS.RequireOffline)
        {
            score += backend.Type == DeploymentType.Local ? 100 : -1000;
        }

        // Historical performance
        score += (1.0 - metrics.ErrorRate) * 30; // Reliability bonus
        score -= Math.Min(metrics.AverageLatency.TotalSeconds, 10) * 5; // Latency penalty

        // Resource utilization
        if (backend.Type == DeploymentType.Local)
        {
            score -= metrics.CpuUtilization * 0.3; // Penalize high CPU usage
            score -= metrics.MemoryUtilization * 0.2; // Penalize high memory usage
        }

        return score;
    }
}

public record RoutingDecision(IAIBackend Backend, string Reason);
```

### Cost Management

```csharp
// GameConsole.AI.Cost/src/CostTracker.cs
public class CostTracker : ICostTracker
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<CostTracker> _logger;
    private decimal _currentMonthSpend = 0;
    private readonly object _lock = new();

    public decimal CurrentMonthSpend
    {
        get { lock (_lock) return _currentMonthSpend; }
    }

    public decimal MonthlyBudget => _configuration.GetValue<decimal>("AI:MonthlyBudget", 100);

    public async Task<bool> CanAffordAsync(decimal estimatedCost)
    {
        lock (_lock)
        {
            return _currentMonthSpend + estimatedCost <= MonthlyBudget;
        }
    }

    public async Task RecordCostAsync(decimal actualCost, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _currentMonthSpend += actualCost;
        }

        _logger.LogInformation("AI cost recorded: {Cost:C}, Monthly total: {Total:C}/{Budget:C}",
            actualCost, _currentMonthSpend, MonthlyBudget);

        if (_currentMonthSpend > MonthlyBudget * 0.8m)
        {
            _logger.LogWarning("AI spending approaching monthly budget: {Percentage:P1}",
                _currentMonthSpend / MonthlyBudget);
        }

        // Persist to storage
        await PersistCostDataAsync(actualCost, cancellationToken);
    }
}
```

### Configuration Examples

```yaml
# appsettings.Development.yml - Local development
AI:
  Deployment:
    Strategy: LocalFirst
    LocalModels:
      OllamaEndpoint: "http://localhost:11434"
      AvailableModels:
        llama2:
          Name: "llama2:7b"
          Capabilities: ["text-generation"]
          Performance:
            AverageLatency: "00:00:02"
            Quality: 0.8
    RemoteServices:
      Providers:
        OpenAI:
          ApiEndpoint: "https://api.openai.com/v1"
          ApiKeyConfigPath: "AI:OpenAI:ApiKey"
    Resilience:
      MaxRetries: 3
      BackoffDelay: "00:00:05"

# appsettings.Production.yml - Production deployment
AI:
  Deployment:
    Strategy: RemoteFirst
    RemoteServices:
      Providers:
        AzureOpenAI:
          ApiEndpoint: "https://mycompany.openai.azure.com/"
          ApiKeyConfigPath: "AI:Azure:ApiKey"
          Models:
            gpt-35-turbo:
              DeploymentName: "gpt-35-turbo-prod"
              MaxTokens: 4096
              Pricing:
                InputTokenCost: 0.0015
                OutputTokenCost: 0.002
    Optimization:
      MonthlyBudget: 500
      PreferLowCost: true
```

## Benefits

### Flexibility
- Support for local, remote, and hybrid deployments
- Runtime switching between backends based on requirements
- Configuration-driven deployment strategies

### Cost Optimization
- Intelligent routing to minimize API costs
- Local processing for cost-sensitive operations
- Real-time cost tracking and budget management

### Performance
- Local processing for low-latency requirements
- Optimal backend selection based on current conditions
- Efficient fallback strategies

### Reliability
- Multiple backend options for redundancy
- Graceful degradation when services unavailable
- Comprehensive error handling and retry logic

## Drawbacks

### Complexity
- Complex routing and decision-making logic
- Multiple backend implementations to maintain
- Configuration complexity for different scenarios

### Resource Usage
- Local models require significant storage and compute
- Multiple backends consume more memory
- Background health checking overhead

### Operational Overhead
- Model management for local deployments
- Cost monitoring and budget management
- Performance tuning across multiple backends

## Alternatives Considered

### Single Backend Approach
- Simpler but lacks flexibility
- **Rejected**: Can't adapt to different deployment scenarios

### Manual Backend Selection
- More predictable but requires user configuration
- **Rejected**: Poor user experience, doesn't optimize automatically

### Always-Remote Strategy
- Simpler deployment but requires internet connectivity
- **Rejected**: Doesn't support offline scenarios

## Migration Strategy

### Phase 1: Backend Abstraction
- Implement IAIBackend interface
- Create basic local and remote backends
- Add simple routing logic

### Phase 2: Intelligent Routing
- Implement metrics collection
- Add intelligent routing algorithm
- Create cost tracking system

### Phase 3: Advanced Features
- Add model management for local backends
- Implement advanced fallback strategies
- Add performance monitoring and optimization

### Phase 4: Production Hardening
- Add comprehensive error handling
- Implement advanced cost controls
- Add operational monitoring and alerting

## Success Metrics

- **Backend Availability**: 99.9% uptime for at least one backend
- **Cost Optimization**: Stay within budget 95% of the time
- **Performance**: Route requests optimally based on QoS requirements
- **Reliability**: Successful fallback in 99% of backend failures

## Future Possibilities

- **Machine Learning Routing**: Use ML to predict optimal backend selection
- **Dynamic Model Loading**: Load local models on-demand
- **Multi-Region Deployment**: Route to geographically optimal backends
- **Custom Model Training**: Support for fine-tuned local models