# GameConsole AI Local Infrastructure

This project provides local AI deployment infrastructure with resource management and optimization for the GameConsole 4-tier architecture.

## Overview

GameConsole.AI.Local implements the Local AI Deployment Infrastructure as specified in Game-RFC-008-01, providing high-performance local AI inference capabilities with intelligent resource management.

## Architecture

The AI infrastructure follows the established GameConsole 4-tier pattern:

- **Tier 1 (Contracts)**: `GameConsole.AI.Core` - Pure interface definitions
- **Tier 3 (Services)**: `GameConsole.AI.Local` - Service implementations

### Core Components

#### LocalAIRuntime
Main service orchestrator that integrates all AI capabilities:
- Model lifecycle management (load, unload, query)
- Inference request routing and execution
- Resource allocation coordination
- Unified service interface implementation

#### AIResourceManager
GPU/CPU resource allocation and optimization:
- Device detection (CPU, CUDA, DirectML, etc.)
- Memory allocation with system limits
- Resource monitoring and statistics
- Automatic fallback to available devices

#### ModelCacheManager
Efficient local model storage and retrieval:
- LRU cache eviction with configurable size limits (2GB default)
- Persistent cache index with automatic recovery
- SHA-256 based cache key generation
- Hot-swap model streaming support

#### LocalInferenceEngine
ONNX Runtime integration for AI model execution:
- Multi-session inference management
- Dynamic batching and scheduling
- Cross-platform optimization (CPU/GPU)
- Performance monitoring and metrics

## Features

### Multi-Framework Support
- **Primary**: ONNX Runtime for cross-platform inference
- **Extensible**: Architecture supports PyTorch, TensorFlow Lite, custom engines
- **Optimization**: Multiple optimization levels (None, Basic, Aggressive, Maximum)

### Resource Management
- **Memory Management**: Configurable limits with automatic allocation
- **Device Selection**: Intelligent GPU/CPU selection with fallbacks  
- **Monitoring**: Real-time resource usage statistics
- **Constraints**: Respect system memory and processing limits

### Model Caching
- **Storage**: Efficient local model caching with persistence
- **Eviction**: LRU-based cache management
- **Performance**: Fast model loading from cache
- **Space Management**: Automatic cleanup when space is needed

### Inference Engine
- **Sessions**: Persistent inference sessions for model reuse
- **Batching**: Support for batch inference requests
- **Scheduling**: Intelligent request scheduling
- **Optimization**: Framework-specific performance tuning

## Usage

### Basic Service Usage

```csharp
// Register AI services
serviceProvider.RegisterFromAttributes(typeof(LocalAIRuntime).Assembly, "AI");

// Get the AI service
var aiService = serviceProvider.GetService<GameConsole.AI.Services.IService>();

// Initialize and start
await aiService.InitializeAsync();
await aiService.StartAsync();

// Load a model
var config = new ResourceConfiguration(
    PreferredDevice: ExecutionDevice.CUDA,
    MaxMemoryMB: 1024,
    MaxConcurrentInferences: 4,
    InferenceTimeoutMs: TimeSpan.FromSeconds(30),
    OptimizationLevel: OptimizationLevel.Aggressive
);

var model = await aiService.LoadModelAsync("path/to/model.onnx", AIFramework.OnnxRuntime, config);

// Run inference
var request = new InferenceRequest(
    RequestId: "inference-001",
    ModelId: model.Id,
    Inputs: new Dictionary<string, object> 
    {
        ["input_tensor"] = new float[] { 1.0f, 2.0f, 3.0f, 4.0f }
    }
);

var result = await aiService.InferAsync(request);
if (result.Success)
{
    Console.WriteLine($"Inference completed in {result.ExecutionTime.TotalMilliseconds}ms");
    // Process result.Outputs
}
```

### Resource Management

```csharp
// Access resource manager capability
var resourceManager = aiService.ResourceManager;

// Check available devices
var devices = await resourceManager.GetAvailableDevicesAsync();
Console.WriteLine($"Available devices: {string.Join(", ", devices)}");

// Get optimal device for configuration
var optimalDevice = await resourceManager.GetOptimalDeviceAsync(config);

// Monitor resource usage
var stats = await aiService.GetResourceStatsAsync();
Console.WriteLine($"Memory: {stats.MemoryUsedMB}MB used, {stats.MemoryAvailableMB}MB available");
```

### Model Caching

```csharp
// Access model cache capability
var modelCache = aiService.ModelCache;

// Pre-cache models for faster loading
var cacheKey = await modelCache.CacheModelAsync("path/to/large_model.onnx");

// Check cache status
var cacheStats = await modelCache.GetCacheStatsAsync();
Console.WriteLine($"Cache: {cacheStats.CachedModels} models, {cacheStats.UsedBytes / (1024*1024)}MB used");

// Clear cache when needed
await modelCache.ClearCacheAsync();
```

## Configuration

### Resource Configuration
- `PreferredDevice`: Target execution device (CPU, CUDA, DirectML, etc.)
- `MaxMemoryMB`: Maximum memory allocation for the model
- `MaxConcurrentInferences`: Concurrent inference limit
- `InferenceTimeoutMs`: Request timeout duration  
- `OptimizationLevel`: Model optimization level

### Cache Configuration
- `MaxCacheSizeMB`: Maximum cache size (default: 2048MB)
- `CacheDirectory`: Custom cache location (default: temp directory)

## Performance Considerations

### Memory Management
- Models cached with LRU eviction when space needed
- Automatic resource cleanup on service disposal
- Memory-mapped model loading for large models
- Resource monitoring with configurable limits

### Inference Optimization
- Session reuse for repeated inferences
- Dynamic batching for throughput optimization
- Framework-specific optimization settings
- CPU/GPU execution provider selection

### Device Selection
- Intelligent device selection based on availability
- Automatic fallback to CPU when GPU unavailable
- Performance-based device recommendations
- Resource constraint awareness

## Error Handling

The AI infrastructure provides robust error handling:

- **Resource Allocation Failures**: Graceful fallback to available resources
- **Model Loading Errors**: Detailed error messages with recovery suggestions
- **Inference Failures**: Request-level error isolation
- **Cache Errors**: Automatic cache recovery and rebuilding

## Testing

Comprehensive test coverage includes:

- **Unit Tests**: 31 tests covering all components
- **Integration Tests**: Service lifecycle and component interaction
- **Performance Tests**: Resource usage and inference performance
- **Error Scenarios**: Failure modes and recovery

Run tests with:
```bash
dotnet test ./dotnet/GameConsole.AI.Local.Tests
```

## Dependencies

- **Microsoft.ML.OnnxRuntime**: Cross-platform AI inference engine
- **System.Reactive**: Reactive patterns (future enhancements)
- **GameConsole.Core.Abstractions**: Base service interfaces
- **GameConsole.Core.Registry**: Service registration patterns

## Future Enhancements

- Model quantization for performance optimization
- Streaming inference for large models
- Multi-model ensemble support
- Distributed inference coordination
- Advanced batching strategies
- GPU memory optimization