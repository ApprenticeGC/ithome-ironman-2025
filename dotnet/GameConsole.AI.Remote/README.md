# GameConsole.AI.Remote

This project implements RFC-008-02: Remote AI Service Integration for the GameConsole framework.

## Overview

GameConsole.AI.Remote provides comprehensive remote AI service integration with load balancing, failover, and monitoring capabilities. It supports multiple AI providers including OpenAI, Azure OpenAI, AWS Bedrock, and Anthropic Claude.

## Features

### Core Components

- **IRemoteAIService**: Main service interface with completion and streaming support
- **AIServiceClient**: HTTP client with resilience patterns and retry logic
- **RemoteAILoadBalancer**: Multi-strategy load balancing across endpoints
- **AIServiceFailover**: Automatic failover with exponential backoff and local fallback
- **Comprehensive Configuration**: Full configuration support for all aspects

### Capabilities

- **IBatchProcessingCapability**: Process multiple AI requests in batches
- **IRateLimitingCapability**: Rate limiting and quota management

### Load Balancing Strategies

- Round Robin
- Weighted Round Robin
- Least Connections
- Fastest Response
- Lowest Cost

### Supported AI Providers

- **OpenAI**: GPT models via OpenAI API
- **Azure**: Azure OpenAI Service
- **AWS**: AWS Bedrock
- **Anthropic**: Claude models
- **Local**: Fallback to local AI services

### Key Features

- **Response Caching**: Configurable response caching with multiple strategies
- **Rate Limiting**: Global and per-endpoint rate limiting
- **Health Monitoring**: Continuous health checks and metrics collection
- **Cost Tracking**: Usage and cost monitoring with breakdown by provider
- **Streaming Support**: Real-time streaming responses
- **API Key Rotation**: Automatic API key rotation for security
- **Compression**: Request/response compression support
- **Circuit Breaker**: Circuit breaker pattern for resilience

## Architecture Compliance

This implementation follows the GameConsole 4-tier service architecture:

- **Tier 1 (Contracts)**: IRemoteAIService interface extends GameConsole.Core.Abstractions.IService
- **Tier 2 (Proxies)**: Auto-generated proxies (future enhancement)
- **Tier 3 (Adapters)**: RemoteAIService implements business logic and orchestration
- **Tier 4 (Providers)**: AIServiceClient handles provider-specific communication

## Configuration

The service is configured via `RemoteAIConfiguration` which includes:

- Endpoint configurations for each AI provider
- Load balancer settings
- Failover and retry policies
- Caching configuration
- Rate limiting settings

## Usage

```csharp
// Initialize the service
var service = new RemoteAIService(logger, configuration, httpClientFactory, cache, loggerFactory);

// Initialize and start
await service.InitializeAsync(cancellationToken);
await service.StartAsync(cancellationToken);

// Make completion requests
var request = new AICompletionRequest
{
    Prompt = "Hello, world!",
    MaxTokens = 100,
    Temperature = 0.7f
};

var response = await service.GetCompletionAsync(request, cancellationToken);

// Stream responses
await foreach (var chunk in service.GetStreamingCompletionAsync(request, cancellationToken))
{
    Console.Write(chunk.Content);
}

// Get health status
var health = await service.GetHealthStatusAsync(cancellationToken);

// Get usage metrics
var metrics = await service.GetUsageMetricsAsync(cancellationToken);
```

## Dependencies

- GameConsole.Core.Abstractions (base service contracts)
- Microsoft.Extensions.Http (HTTP client factory)
- Microsoft.Extensions.Caching.Memory (response caching)
- Microsoft.Extensions.Logging (logging)
- Microsoft.Extensions.Configuration (configuration)
- System.Text.Json (JSON serialization)

## Testing

The project includes comprehensive error handling and is designed to be testable. Unit tests should be added following the existing GameConsole test patterns.

## Future Enhancements

- Polly integration for advanced resilience patterns
- gRPC support for high-performance scenarios
- Enhanced metrics and observability
- Custom load balancing algorithms
- Advanced caching strategies