namespace GameConsole.Graphics.Core;

/// <summary>
/// Generic interface for managing graphics resources.
/// Provides resource creation, caching, and lifecycle management.
/// </summary>
/// <typeparam name="TResource">The type of resource being managed.</typeparam>
/// <typeparam name="TDescriptor">The type of descriptor used to create the resource.</typeparam>
public interface IResourceManager<TResource, TDescriptor>
    where TResource : class, IGraphicsResource
    where TDescriptor : struct
{
    /// <summary>
    /// Creates a new resource asynchronously from the given descriptor.
    /// </summary>
    /// <param name="descriptor">The descriptor defining the resource properties.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the created resource.</returns>
    Task<TResource> CreateResourceAsync(TDescriptor descriptor, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new resource asynchronously from the given descriptor with initial data.
    /// </summary>
    /// <param name="descriptor">The descriptor defining the resource properties.</param>
    /// <param name="initialData">Initial data to populate the resource with.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the created resource.</returns>
    Task<TResource> CreateResourceAsync(TDescriptor descriptor, ReadOnlyMemory<byte> initialData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an existing resource by its unique identifier.
    /// </summary>
    /// <param name="resourceId">The unique identifier of the resource.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the resource, or null if not found.</returns>
    Task<TResource?> GetResourceAsync(Guid resourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a resource and frees its GPU memory.
    /// </summary>
    /// <param name="resource">The resource to release.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async release operation.</returns>
    Task ReleaseResourceAsync(TResource resource, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current memory usage statistics for this resource type.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns memory usage information.</returns>
    Task<ResourceMemoryInfo> GetMemoryUsageAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs garbage collection to free unused resources.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async garbage collection operation.</returns>
    Task CollectGarbageAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Specialized interface for managing texture resources.
/// </summary>
public interface ITextureManager : IResourceManager<ITexture, TextureDescriptor>
{
    /// <summary>
    /// Loads a texture from file asynchronously.
    /// </summary>
    /// <param name="filePath">The path to the texture file.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the loaded texture.</returns>
    Task<ITexture> LoadFromFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a texture from a stream asynchronously.
    /// </summary>
    /// <param name="stream">The stream containing texture data.</param>
    /// <param name="format">The format of the texture data in the stream.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the loaded texture.</returns>
    Task<ITexture> LoadFromStreamAsync(Stream stream, string format, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates mipmaps for the specified texture.
    /// </summary>
    /// <param name="texture">The texture to generate mipmaps for.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async mipmap generation operation.</returns>
    Task GenerateMipmapsAsync(ITexture texture, CancellationToken cancellationToken = default);
}

/// <summary>
/// Specialized interface for managing shader resources.
/// </summary>
public interface IShaderManager : IResourceManager<IShader, ShaderDescriptor>
{
    /// <summary>
    /// Compiles a shader from source code asynchronously.
    /// </summary>
    /// <param name="sourceCode">The shader source code.</param>
    /// <param name="type">The type of shader to compile.</param>
    /// <param name="language">The shader language being used.</param>
    /// <param name="entryPoint">The entry point function name.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the compiled shader.</returns>
    Task<IShader> CompileFromSourceAsync(string sourceCode, ShaderType type, ShaderLanguage language, string entryPoint = "main", CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads and compiles a shader from file asynchronously.
    /// </summary>
    /// <param name="filePath">The path to the shader file.</param>
    /// <param name="type">The type of shader to compile.</param>
    /// <param name="language">The shader language being used.</param>
    /// <param name="entryPoint">The entry point function name.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the compiled shader.</returns>
    Task<IShader> LoadFromFileAsync(string filePath, ShaderType type, ShaderLanguage language, string entryPoint = "main", CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a shader program from multiple shader stages.
    /// </summary>
    /// <param name="shaders">The shaders to link into a program.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the shader program.</returns>
    Task<IShaderProgram> CreateProgramAsync(IEnumerable<IShader> shaders, CancellationToken cancellationToken = default);
}

/// <summary>
/// Specialized interface for managing mesh resources.
/// </summary>
public interface IMeshManager : IResourceManager<IMesh, MeshDescriptor>
{
    /// <summary>
    /// Creates a mesh from vertex and index data.
    /// </summary>
    /// <param name="vertices">The vertex data.</param>
    /// <param name="indices">The index data (optional).</param>
    /// <param name="layout">The vertex layout describing the vertex attributes.</param>
    /// <param name="topology">The primitive topology for rendering.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the created mesh.</returns>
    Task<IMesh> CreateMeshAsync<T>(ReadOnlyMemory<T> vertices, ReadOnlyMemory<uint>? indices, VertexLayout layout, PrimitiveTopology topology, CancellationToken cancellationToken = default) where T : struct;

    /// <summary>
    /// Loads a mesh from file asynchronously.
    /// </summary>
    /// <param name="filePath">The path to the mesh file.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the loaded mesh.</returns>
    Task<IMesh> LoadFromFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the vertex data of an existing mesh.
    /// </summary>
    /// <param name="mesh">The mesh to update.</param>
    /// <param name="vertices">The new vertex data.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async update operation.</returns>
    Task UpdateVerticesAsync<T>(IMesh mesh, ReadOnlyMemory<T> vertices, CancellationToken cancellationToken = default) where T : struct;

    /// <summary>
    /// Updates the index data of an existing mesh.
    /// </summary>
    /// <param name="mesh">The mesh to update.</param>
    /// <param name="indices">The new index data.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async update operation.</returns>
    Task UpdateIndicesAsync(IMesh mesh, ReadOnlyMemory<uint> indices, CancellationToken cancellationToken = default);
}

/// <summary>
/// Contains memory usage information for graphics resources.
/// </summary>
public record struct ResourceMemoryInfo
{
    /// <summary>
    /// Gets or sets the total allocated memory in bytes.
    /// </summary>
    public ulong AllocatedBytes { get; set; }

    /// <summary>
    /// Gets or sets the currently used memory in bytes.
    /// </summary>
    public ulong UsedBytes { get; set; }

    /// <summary>
    /// Gets or sets the number of allocated resources.
    /// </summary>
    public uint ResourceCount { get; set; }

    /// <summary>
    /// Gets or sets the peak memory usage in bytes.
    /// </summary>
    public ulong PeakUsageBytes { get; set; }
}