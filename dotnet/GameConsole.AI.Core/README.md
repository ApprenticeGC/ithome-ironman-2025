# GameConsole AI Core

This project defines the Tier 1 service contracts for AI operations in the GameConsole architecture.

## Purpose

GameConsole.AI.Core provides pure interface definitions and common types for AI services, following the 4-tier GameConsole architecture pattern. This project contains no implementations, only contracts that define the AI service behavior.

## Components

### Service Interface

#### `IService` 
Main AI service interface extending `GameConsole.Core.Abstractions.IService`:
- **Model Management**: Load, unload, and query AI models
- **Inference Operations**: Single and batch inference execution
- **Resource Access**: Resource manager, model cache, and inference engine capabilities

### Capability Interfaces

#### `IResourceManagerCapability`
Resource allocation and management operations:
- Device detection and selection
- Memory allocation and monitoring
- Resource optimization

#### `IModelCacheCapability`
Model storage and caching operations:
- Model caching and retrieval
- Cache eviction and management
- Storage statistics

#### `ILocalInferenceCapability`
AI inference execution operations:
- Session management
- Inference execution
- Performance monitoring

### Types and Enums

#### `AIFramework`
Supported AI inference frameworks:
- `OnnxRuntime`: Cross-platform ONNX inference
- `PyTorch`: PyTorch inference engine  
- `TensorFlowLite`: TensorFlow Lite for edge deployment
- `Custom`: Custom inference implementations

#### `ExecutionDevice`
Available execution devices:
- `CPU`: CPU execution (fallback default)
- `CUDA`: NVIDIA GPU with CUDA
- `ROCm`: AMD GPU with ROCm
- `Metal`: Apple Silicon GPU
- `DirectML`: DirectX GPU acceleration

#### `OptimizationLevel`
Model optimization levels:
- `None`: No optimization
- `Basic`: Basic optimizations preserving accuracy
- `Aggressive`: Advanced optimizations with slight accuracy trade-off
- `Maximum`: Maximum optimization for best performance

### Data Types

#### `AIModel`
Represents a loaded AI model with metadata:
- Model identification and versioning
- Framework and file information
- Access tracking and metadata

#### `ResourceConfiguration`
Resource allocation configuration:
- Device preferences and limits
- Memory and concurrency settings
- Timeout and optimization preferences

#### `InferenceRequest`/`InferenceResult`
Request/response pair for inference operations:
- Request identification and inputs
- Execution results and timing
- Error handling and status

#### `ResourceStats`
System resource usage statistics:
- Memory and CPU utilization
- Active and queued inference counts
- Performance metrics

## Usage

This is a contracts-only project. Reference it to use AI service interfaces:

```csharp
using GameConsole.AI.Services;

// Use service interface
public class MyAIClient
{
    private readonly IService _aiService;
    
    public MyAIClient(IService aiService)
    {
        _aiService = aiService;
    }
    
    public async Task<InferenceResult> RunInferenceAsync(Dictionary<string, object> inputs)
    {
        var request = new InferenceRequest(
            RequestId: Guid.NewGuid().ToString(),
            ModelId: "my-model",
            Inputs: inputs
        );
        
        return await _aiService.InferAsync(request);
    }
}
```

## Dependencies

- **GameConsole.Core.Abstractions**: Base service interface definitions

## Architecture Compliance

This project follows GameConsole Tier 1 requirements:
- ✅ Pure .NET contracts with no external dependencies
- ✅ Async-first design with CancellationToken support
- ✅ Capability-based service discovery
- ✅ Consistent naming and documentation patterns
- ✅ Extensible design for future AI frameworks