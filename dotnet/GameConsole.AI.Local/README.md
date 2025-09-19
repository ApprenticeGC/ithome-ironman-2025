# GameConsole.AI.Local

Local AI deployment infrastructure for GameConsole with resource management and optimization.

## Overview

This project implements RFC-008-01: Create Local AI Deployment Infrastructure, providing:

- **LocalAIRuntimeService**: Central service for managing local AI model execution
- **AIResourceManager**: GPU/CPU allocation and monitoring with resource optimization
- **ModelCacheManager**: Efficient local model storage and retrieval with LRU eviction
- **LocalInferenceEngine**: AI model execution with dynamic batching and scheduling

## Features

### 🚀 Performance Optimized
- **Dynamic Batching**: Automatically batches inference requests for improved throughput
- **Model Quantization**: Supports multiple quantization levels (Dynamic, Static, Aggressive)
- **Multi-Provider Support**: CPU, CUDA, DirectML, and automatic provider selection
- **Memory Management**: Intelligent caching with size limits and LRU eviction

### 🔧 Resource Management
- **GPU/CPU Allocation**: Smart resource allocation with constraint enforcement
- **Memory Monitoring**: Real-time memory usage tracking and optimization
- **Fallback Mechanisms**: Graceful degradation when resources are constrained
- **Performance Metrics**: Comprehensive monitoring of inference operations

### 💾 Model Caching
- **Hybrid Storage**: In-memory and disk-based caching for optimal performance
- **Compression Support**: Efficient storage with model compression
- **Metadata Tracking**: Rich metadata support for cached models
- **Cache Eviction**: LRU-based eviction when cache limits are reached

### 🔄 Execution Engine
- **ONNX Runtime**: Cross-platform AI model inference using ONNX Runtime
- **Execution Providers**: Support for CPU, CUDA, DirectML, and OpenVINO
- **Batch Processing**: Configurable batching for improved throughput
- **Session Management**: Efficient session lifecycle management

## Usage

### Basic Setup

```csharp
// Initialize the Local AI Runtime
var logger = serviceProvider.GetService<ILogger<LocalAIRuntimeService>>();
var runtime = new LocalAIRuntimeService(logger);

await runtime.InitializeAsync();
await runtime.StartAsync();
```

### Loading and Using Models

```csharp
// Load a model with quantization
var quantizationConfig = new QuantizationConfig
{
    Level = QuantizationLevel.Dynamic,
    UseGpuAcceleration = true
};

await runtime.LoadModelAsync("model.onnx", "my-model", quantizationConfig);

// Single inference
var input = new Dictionary<string, object>
{
    ["input"] = new float[] { 1.0f, 2.0f, 3.0f }
};
var result = await runtime.InferAsync("my-model", input);

// Batch inference
var batchInputs = new[]
{
    new Dictionary<string, object> { ["input"] = new float[] { 1.0f, 2.0f, 3.0f } },
    new Dictionary<string, object> { ["input"] = new float[] { 4.0f, 5.0f, 6.0f } }
};
var batchResults = await runtime.InferBatchAsync("my-model", batchInputs);
```

### Resource Constraints

```csharp
// Configure resource limits
var constraints = new ResourceConstraints
{
    MaxMemoryBytes = 2L * 1024 * 1024 * 1024, // 2GB
    MaxCpuUtilizationPercent = 80.0,
    MaxGpuUtilizationPercent = 90.0,
    MaxConcurrentOperations = 4,
    InferenceTimeout = TimeSpan.FromSeconds(30)
};

await runtime.SetResourceConstraintsAsync(constraints);
```

### Batch Configuration

```csharp
// Configure batching behavior
var batchConfig = new BatchConfiguration
{
    MaxBatchSize = 32,
    OptimalBatchSize = 8,
    BatchTimeout = TimeSpan.FromMilliseconds(100),
    EnableDynamicBatching = true
};

await runtime.InferenceEngine.ConfigureBatchingAsync(batchConfig);
```

## Architecture

The implementation follows the GameConsole 4-tier architecture:

### Tier 1: Interfaces
- `ILocalAIRuntime`: Main runtime interface
- `IAIResourceManager`: Resource management interface
- `IModelCacheManager`: Model caching interface
- `ILocalInferenceEngine`: Inference engine interface

### Tier 2: Service Layer
- `LocalAIRuntimeService`: Main orchestrator service
- Implements service lifecycle (Initialize, Start, Stop, Dispose)
- Integrates all components into a unified interface

### Tier 3: Business Logic
- `AIResourceManagerService`: Resource allocation and monitoring
- `ModelCacheManagerService`: Model caching with hybrid storage
- `LocalInferenceEngineService`: ONNX Runtime integration

### Tier 4: Providers
- ONNX Runtime integration
- Multiple execution providers (CPU, CUDA, DirectML)
- Platform-specific optimizations

## Dependencies

- **Microsoft.ML.OnnxRuntime**: Cross-platform AI inference
- **Microsoft.Extensions.Caching.Memory**: In-memory caching
- **Microsoft.Extensions.Logging.Abstractions**: Logging infrastructure
- **System.Reactive**: Event-driven processing
- **GameConsole.Core.Abstractions**: Base service interfaces

## Configuration

The system supports various configuration options:

### Execution Providers
- **CPU**: Default fallback provider
- **CUDA**: NVIDIA GPU acceleration
- **DirectML**: Windows DirectX-based acceleration
- **Auto**: Automatic provider selection

### Quantization Levels
- **None**: Full precision (no quantization)
- **Dynamic**: Runtime quantization
- **Static**: Pre-computed quantization
- **Aggressive**: Maximum compression

### Resource Limits
- Memory allocation limits
- CPU/GPU utilization thresholds
- Concurrent operation limits
- Operation timeout settings

## Performance Considerations

### Memory Management
- Hybrid caching strategy (memory + disk)
- LRU eviction for cache management
- Resource allocation tracking
- Automatic garbage collection triggers

### Batching Optimization
- Dynamic batch size adjustment
- Optimal batch size detection
- Timeout-based batch processing
- Concurrent batch execution

### Provider Selection
- Hardware capability detection
- Performance-based provider ranking
- Graceful fallback mechanisms
- Provider-specific optimizations

## Error Handling

The system includes comprehensive error handling:

- **Resource Exhaustion**: Graceful degradation when resources are limited
- **Model Loading Errors**: Detailed error reporting and recovery
- **Inference Failures**: Retry mechanisms and fallback strategies
- **Provider Failures**: Automatic provider switching

## Monitoring and Metrics

Real-time monitoring includes:

- **Resource Utilization**: CPU, GPU, and memory usage
- **Inference Metrics**: Latency, throughput, and error rates
- **Cache Performance**: Hit rates, eviction counts, and storage utilization
- **System Health**: Overall system status and performance indicators

## Testing

The implementation is designed to be testable with:

- Mockable interfaces for all components
- Dependency injection support
- Comprehensive logging for debugging
- Performance benchmarking capabilities

## Future Enhancements

Planned improvements include:

- **Model Streaming**: Support for very large models
- **Distributed Inference**: Multi-node execution
- **Advanced Quantization**: Custom quantization strategies
- **Hardware Optimization**: Platform-specific optimizations
- **Monitoring Dashboard**: Real-time performance visualization