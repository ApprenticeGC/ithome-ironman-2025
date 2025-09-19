using GameConsole.Core.Abstractions;
using System.Numerics;

namespace GameConsole.Graphics.Core;

/// <summary>
/// Core graphics service interface for rendering abstraction.
/// Provides unified interface for 2D and 3D rendering operations.
/// </summary>
public interface IService : GameConsole.Core.Abstractions.IService
{
    #region Rendering Operations
    
    /// <summary>
    /// Begins a new render frame.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task BeginFrameAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Ends the current render frame and presents to screen.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task EndFrameAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Clears the current render target.
    /// </summary>
    /// <param name="color">Clear color.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ClearAsync(Vector4 color, CancellationToken cancellationToken = default);
    
    #endregion

    #region Resource Management
    
    /// <summary>
    /// Gets the texture manager capability.
    /// </summary>
    ITextureManagerCapability? TextureManager { get; }
    
    /// <summary>
    /// Gets the shader manager capability.
    /// </summary>
    IShaderCapability? ShaderManager { get; }
    
    /// <summary>
    /// Gets the mesh manager capability.
    /// </summary>
    IMeshCapability? MeshManager { get; }
    
    /// <summary>
    /// Gets the camera manager capability.
    /// </summary>
    ICameraCapability? CameraManager { get; }

    #endregion
    
    #region Graphics State
    
    /// <summary>
    /// Sets the current viewport.
    /// </summary>
    /// <param name="x">Viewport X offset.</param>
    /// <param name="y">Viewport Y offset.</param>
    /// <param name="width">Viewport width.</param>
    /// <param name="height">Viewport height.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetViewportAsync(int x, int y, int width, int height, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets current graphics backend in use.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The graphics backend being used.</returns>
    Task<Core.GraphicsBackend> GetBackendAsync(CancellationToken cancellationToken = default);
    
    #endregion
}

/// <summary>
/// Capability interface for texture management operations.
/// </summary>
public interface ITextureManagerCapability : ICapabilityProvider
{
    /// <summary>
    /// Loads a texture from file path.
    /// </summary>
    /// <param name="path">File path to texture.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Loaded texture resource.</returns>
    Task<Core.Texture2D> LoadTextureAsync(string path, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Unloads a texture resource.
    /// </summary>
    /// <param name="textureId">Texture identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task UnloadTextureAsync(string textureId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets cached texture by ID.
    /// </summary>
    /// <param name="textureId">Texture identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Cached texture or null if not found.</returns>
    Task<Core.Texture2D?> GetTextureAsync(string textureId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for shader management operations.
/// </summary>
public interface IShaderCapability : ICapabilityProvider
{
    /// <summary>
    /// Compiles a shader from source code.
    /// </summary>
    /// <param name="source">Shader source code.</param>
    /// <param name="type">Shader type.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Compiled shader resource.</returns>
    Task<Core.Shader> CompileShaderAsync(string source, Core.ShaderType type, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Loads and compiles a shader from file.
    /// </summary>
    /// <param name="path">Path to shader file.</param>
    /// <param name="type">Shader type.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Compiled shader resource.</returns>
    Task<Core.Shader> LoadShaderAsync(string path, Core.ShaderType type, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Binds a shader for rendering.
    /// </summary>
    /// <param name="shaderId">Shader identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task BindShaderAsync(string shaderId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for mesh management operations.
/// </summary>
public interface IMeshCapability : ICapabilityProvider
{
    /// <summary>
    /// Loads a 3D mesh from file.
    /// </summary>
    /// <param name="path">Path to mesh file.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Loaded mesh resource.</returns>
    Task<Core.Mesh> LoadMeshAsync(string path, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Renders a mesh with the current graphics state.
    /// </summary>
    /// <param name="meshId">Mesh identifier.</param>
    /// <param name="transform">World transform matrix.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RenderMeshAsync(string meshId, Matrix4x4 transform, CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for camera management operations.
/// </summary>
public interface ICameraCapability : ICapabilityProvider
{
    /// <summary>
    /// Sets the current camera configuration.
    /// </summary>
    /// <param name="camera">Camera configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetCameraAsync(Core.Camera camera, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the current camera configuration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Current camera configuration.</returns>
    Task<Core.Camera> GetCameraAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the view matrix for the current camera.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>View matrix.</returns>
    Task<Matrix4x4> GetViewMatrixAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the projection matrix for the current camera.
    /// </summary>
    /// <param name="aspectRatio">Screen aspect ratio.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Projection matrix.</returns>
    Task<Matrix4x4> GetProjectionMatrixAsync(float aspectRatio, CancellationToken cancellationToken = default);
}