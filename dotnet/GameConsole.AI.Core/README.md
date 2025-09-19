# GameConsole.AI.Core

This package provides the foundational AI agent interface framework for the GameConsole platform, enabling seamless integration of AI capabilities into game development workflows.

## Overview

The GameConsole.AI.Core package defines the core contracts and abstractions for AI agents, following the established 4-tier architecture pattern of the GameConsole framework.

## Key Components

### IAIAgent Interface

The primary interface for AI agents that combines service lifecycle management with capability discovery:

```csharp
public interface IAIAgent : IService, ICapabilityProvider
{
    AIAgentMetadata Metadata { get; }
    IAIContext? Context { get; }
    
    Task ConfigureAsync(IAIContext context, CancellationToken cancellationToken = default);
    Task<string> InvokeAsync(string input, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> StreamAsync(string input, CancellationToken cancellationToken = default);
    Task<AIPerformanceMetrics> GetPerformanceMetricsAsync(CancellationToken cancellationToken = default);
}
```

### IAICapability Interface

Defines individual AI capabilities that can be provided by agents:

```csharp
public interface IAICapability
{
    string Id { get; }
    string Name { get; }
    string Description { get; }
    Version Version { get; }
    
    Task<bool> ValidateInputAsync(object input, CancellationToken cancellationToken = default);
    Task<object> ExecuteAsync(object input, CancellationToken cancellationToken = default);
    Task<AIPerformanceEstimate> EstimatePerformanceAsync(object input, CancellationToken cancellationToken = default);
}
```

### AIAgentMetadata

Provides comprehensive metadata about AI agents including model information, versioning, and requirements:

```csharp
public class AIAgentMetadata
{
    public string Id { get; }
    public string Name { get; }
    public Version Version { get; }
    public AIModelInfo ModelInfo { get; }
    public AIResourceRequirements ResourceRequirements { get; }
    public IReadOnlyList<AIFrameworkType> SupportedFrameworks { get; }
    // ... additional properties
}
```

### IAIContext Interface

Provides a secure execution environment for AI agents:

```csharp
public interface IAIContext : IDisposable
{
    string Id { get; }
    AIResourceAllocation ResourceAllocation { get; }
    AISecuritySettings SecuritySettings { get; }
    AIPerformanceMetrics PerformanceMetrics { get; }
    
    Task InitializeAsync(AIContextSettings settings, CancellationToken cancellationToken = default);
    Task<bool> AllocateResourcesAsync(AIResourceRequirements requirements, CancellationToken cancellationToken = default);
    Task<bool> ValidateSecurityAsync(string operation, CancellationToken cancellationToken = default);
}
```

## Supported AI Frameworks

The framework supports multiple AI runtime environments through the `AIFrameworkType` enumeration:

- **ONNX** - Open Neural Network Exchange format
- **TensorFlow** - Google's machine learning framework
- **PyTorch** - Facebook's deep learning framework
- **OpenVINO** - Intel's AI inference toolkit
- **DirectML** - Microsoft's DirectX-based ML framework
- **CoreML** - Apple's machine learning framework
- **Custom** - For proprietary or specialized frameworks

## Security Features

The framework provides comprehensive security controls:

- **Sandboxing** - Isolated execution environments for AI operations
- **Resource Limits** - CPU, GPU, and memory allocation controls
- **Operation Whitelisting** - Fine-grained permission system
- **Network/File Access Controls** - Configurable access policies

## Performance Monitoring

Built-in performance monitoring capabilities include:

- Execution time tracking
- Resource usage metrics (CPU, GPU, memory)
- Throughput measurements
- Error rate monitoring
- Custom metric support

## Architecture Compliance

This package follows the GameConsole 4-tier architecture:

- **Tier 1**: Core interfaces (`IAIAgent`, `IAICapability`, `IAIContext`)
- **Tier 2**: Proxy implementations (to be implemented in service packages)
- **Tier 3**: Business logic and orchestration (to be implemented in service packages)
- **Tier 4**: Provider implementations for specific AI frameworks

## Dependencies

- `GameConsole.Core.Abstractions` - Base service interfaces (`IService`, `ICapabilityProvider`)

## Usage Example

```csharp
// Agent metadata
var modelInfo = new AIModelInfo("GPT-4", new Version(1, 0), AIFrameworkType.Custom);
var metadata = new AIAgentMetadata("text-generator", "Text Generation Agent", 
    new Version(1, 0, 0), "Generates text using AI", "GameConsole", modelInfo);

// Security settings
var securitySettings = new AISecuritySettings
{
    EnableSandboxing = true,
    MaxExecutionTime = TimeSpan.FromMinutes(5),
    EnableNetworkAccess = false
};

// Context settings
var contextSettings = new AIContextSettings
{
    SecuritySettings = securitySettings,
    EnablePerformanceMonitoring = true
};

// Usage with an agent implementation
// var agent = new MyAIAgent(metadata);
// await agent.InitializeAsync();
// await agent.ConfigureAsync(context);
// await agent.StartAsync();
// var response = await agent.InvokeAsync("Generate a story about...");
```

## Testing

The package includes comprehensive unit tests covering all major components. Run tests with:

```bash
dotnet test GameConsole.AI.Core.Tests
```

## License

This package is part of the GameConsole framework. Please refer to the main repository for licensing information.