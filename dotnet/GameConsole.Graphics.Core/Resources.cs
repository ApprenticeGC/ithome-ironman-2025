namespace GameConsole.Graphics.Core;

/// <summary>
/// Base interface for all graphics resources.
/// </summary>
public interface IGraphicsResource : IAsyncDisposable
{
    /// <summary>
    /// Gets the unique identifier of the resource.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the size of the resource in bytes.
    /// </summary>
    ulong SizeInBytes { get; }

    /// <summary>
    /// Gets a value indicating whether the resource is valid and can be used.
    /// </summary>
    bool IsValid { get; }

    /// <summary>
    /// Gets the creation timestamp of the resource.
    /// </summary>
    DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// Gets the last access timestamp of the resource.
    /// </summary>
    DateTimeOffset LastAccessedAt { get; }
}

/// <summary>
/// Interface for texture resources.
/// </summary>
public interface ITexture : IGraphicsResource
{
    /// <summary>
    /// Gets the texture descriptor that defines the texture properties.
    /// </summary>
    TextureDescriptor Descriptor { get; }

    /// <summary>
    /// Gets the width of the texture in pixels.
    /// </summary>
    uint Width { get; }

    /// <summary>
    /// Gets the height of the texture in pixels.
    /// </summary>
    uint Height { get; }

    /// <summary>
    /// Gets the depth of the texture for 3D textures.
    /// </summary>
    uint Depth { get; }

    /// <summary>
    /// Gets the number of mip levels in the texture.
    /// </summary>
    uint MipLevels { get; }

    /// <summary>
    /// Gets the pixel format of the texture.
    /// </summary>
    PixelFormat Format { get; }

    /// <summary>
    /// Gets the texture type.
    /// </summary>
    TextureType Type { get; }

    /// <summary>
    /// Updates a region of the texture with new data.
    /// </summary>
    /// <param name="data">The new texture data.</param>
    /// <param name="x">The x offset of the region to update.</param>
    /// <param name="y">The y offset of the region to update.</param>
    /// <param name="width">The width of the region to update.</param>
    /// <param name="height">The height of the region to update.</param>
    /// <param name="mipLevel">The mip level to update.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async update operation.</returns>
    Task UpdateDataAsync(ReadOnlyMemory<byte> data, uint x = 0, uint y = 0, uint width = 0, uint height = 0, uint mipLevel = 0, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads texture data from the GPU to system memory.
    /// </summary>
    /// <param name="mipLevel">The mip level to read from.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the texture data.</returns>
    Task<byte[]> ReadDataAsync(uint mipLevel = 0, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for shader resources.
/// </summary>
public interface IShader : IGraphicsResource
{
    /// <summary>
    /// Gets the shader descriptor that defines the shader properties.
    /// </summary>
    ShaderDescriptor Descriptor { get; }

    /// <summary>
    /// Gets the type of shader.
    /// </summary>
    ShaderType Type { get; }

    /// <summary>
    /// Gets the shader language.
    /// </summary>
    ShaderLanguage Language { get; }

    /// <summary>
    /// Gets the entry point function name.
    /// </summary>
    string EntryPoint { get; }

    /// <summary>
    /// Gets the compilation status of the shader.
    /// </summary>
    bool IsCompiled { get; }

    /// <summary>
    /// Gets the compilation log containing any errors or warnings.
    /// </summary>
    string CompilationLog { get; }

    /// <summary>
    /// Gets the uniform parameters defined in the shader.
    /// </summary>
    IReadOnlyList<ShaderParameter> Parameters { get; }
}

/// <summary>
/// Interface for shader programs (collections of linked shaders).
/// </summary>
public interface IShaderProgram : IGraphicsResource
{
    /// <summary>
    /// Gets the shaders that make up this program.
    /// </summary>
    IReadOnlyList<IShader> Shaders { get; }

    /// <summary>
    /// Gets a value indicating whether the program is linked successfully.
    /// </summary>
    bool IsLinked { get; }

    /// <summary>
    /// Gets the linking log containing any errors or warnings.
    /// </summary>
    string LinkingLog { get; }

    /// <summary>
    /// Gets all uniform parameters in the program.
    /// </summary>
    IReadOnlyList<ShaderParameter> Parameters { get; }

    /// <summary>
    /// Sets the value of a uniform parameter.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="value">The value to set.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetParameterAsync(string name, object value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the value of a uniform parameter.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the parameter value.</returns>
    Task<T?> GetParameterAsync<T>(string name, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for mesh resources.
/// </summary>
public interface IMesh : IGraphicsResource
{
    /// <summary>
    /// Gets the mesh descriptor that defines the mesh properties.
    /// </summary>
    MeshDescriptor Descriptor { get; }

    /// <summary>
    /// Gets the vertex layout describing the vertex attributes.
    /// </summary>
    VertexLayout Layout { get; }

    /// <summary>
    /// Gets the primitive topology for rendering.
    /// </summary>
    PrimitiveTopology Topology { get; }

    /// <summary>
    /// Gets the number of vertices in the mesh.
    /// </summary>
    uint VertexCount { get; }

    /// <summary>
    /// Gets the number of indices in the mesh.
    /// </summary>
    uint IndexCount { get; }

    /// <summary>
    /// Gets a value indicating whether the mesh has an index buffer.
    /// </summary>
    bool HasIndices { get; }

    /// <summary>
    /// Gets the vertex buffer for this mesh.
    /// </summary>
    IBuffer VertexBuffer { get; }

    /// <summary>
    /// Gets the index buffer for this mesh, if it exists.
    /// </summary>
    IBuffer? IndexBuffer { get; }
}

/// <summary>
/// Interface for buffer resources.
/// </summary>
public interface IBuffer : IGraphicsResource
{
    /// <summary>
    /// Gets the buffer descriptor that defines the buffer properties.
    /// </summary>
    BufferDescriptor Descriptor { get; }

    /// <summary>
    /// Gets the usage flags for the buffer.
    /// </summary>
    BufferUsage Usage { get; }

    /// <summary>
    /// Gets the memory access pattern for the buffer.
    /// </summary>
    MemoryAccess Access { get; }

    /// <summary>
    /// Updates the buffer data.
    /// </summary>
    /// <param name="data">The new buffer data.</param>
    /// <param name="offset">The offset in bytes where to start updating.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async update operation.</returns>
    Task UpdateDataAsync(ReadOnlyMemory<byte> data, ulong offset = 0, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads buffer data from the GPU to system memory.
    /// </summary>
    /// <param name="offset">The offset in bytes where to start reading.</param>
    /// <param name="size">The number of bytes to read (0 for entire buffer).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the buffer data.</returns>
    Task<byte[]> ReadDataAsync(ulong offset = 0, ulong size = 0, CancellationToken cancellationToken = default);
}

/// <summary>
/// Describes a shader parameter (uniform variable).
/// </summary>
public record struct ShaderParameter
{
    /// <summary>
    /// Gets or sets the name of the parameter.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the data type of the parameter.
    /// </summary>
    public Type DataType { get; set; }

    /// <summary>
    /// Gets or sets the size of the parameter in bytes.
    /// </summary>
    public uint Size { get; set; }

    /// <summary>
    /// Gets or sets the offset of the parameter in the uniform buffer.
    /// </summary>
    public uint Offset { get; set; }

    /// <summary>
    /// Gets or sets the array size (1 for non-arrays).
    /// </summary>
    public uint ArraySize { get; set; }
}