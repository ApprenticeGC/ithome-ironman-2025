# GameConsole Graphics Services

This directory contains the graphics service implementations for the GameConsole 4-tier architecture.

## Services

### RenderingService
- **Purpose**: Core rendering service handling 2D and 3D drawing operations
- **Categories**: Graphics, Rendering
- **Features**: 
  - DirectX 12/Vulkan abstraction
  - Efficient draw call batching
  - Frame management with BeginFrame/EndFrame
  - Viewport and clear operations

### TextureManagerService
- **Purpose**: Texture loading, caching, and GPU resource management
- **Categories**: Graphics, Resources, Textures
- **Features**:
  - Efficient texture streaming and memory optimization
  - LRU caching with memory limits (512MB)
  - Support for multiple texture formats (RGBA8, DXT1, BC7, etc.)
  - Hot-reload support

### ShaderService
- **Purpose**: Shader compilation and management supporting HLSL/GLSL at runtime
- **Categories**: Graphics, Shaders, Compilation
- **Features**:
  - Cross-platform shader compilation
  - Built-in shader library
  - Hot-reload and caching
  - Support for vertex, fragment, geometry, and compute shaders

### MeshService
- **Purpose**: 3D mesh loading and rendering service
- **Categories**: Graphics, 3D, Meshes
- **Features**:
  - Support for common formats (OBJ, FBX, glTF, DAE)
  - Efficient mesh batching and GPU buffer management
  - Built-in primitive meshes (cube, sphere, plane, cylinder)
  - Memory management with LRU eviction

### CameraService
- **Purpose**: Camera and viewport management
- **Categories**: Graphics, Camera, Viewport
- **Features**:
  - Multiple camera types (perspective, orthographic)
  - Camera controls (move, rotate around target)
  - View and projection matrix calculations
  - Camera validation and constraints

## Architecture

The graphics services follow the 4-tier GameConsole architecture:

- **Tier 1**: `GameConsole.Graphics.Core` - Interface definitions and common types
- **Tier 3**: `GameConsole.Graphics.Services` - Service implementations (this project)

All services:
- Inherit from `BaseGraphicsService` which provides common functionality
- Use the `ServiceAttribute` for category-based registration
- Follow async/await patterns with `CancellationToken` support
- Implement proper resource management and cleanup
- Support capability-based service discovery through `ICapabilityProvider`

## Usage

Services are designed to be registered with the `ServiceProvider` using category-based discovery:

```csharp
// Register all graphics services
serviceProvider.RegisterFromAttributes(typeof(RenderingService).Assembly, "Graphics");

// Get the main rendering service
var renderingService = serviceProvider.GetService<IService>();

// Access capabilities
var textureManager = renderingService.TextureManager;
var shaderManager = renderingService.ShaderManager;
```

## Performance Considerations

- GPU resource management with memory limits and LRU eviction
- Efficient draw call batching in RenderingService
- Async resource loading to avoid blocking main thread
- Command buffering for graphics state changes
- Built-in debugging and profiling hooks