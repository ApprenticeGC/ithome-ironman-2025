using System.Numerics;

namespace GameConsole.Graphics.Core;

/// <summary>
/// Interface for render command buffers that batch rendering operations.
/// </summary>
public interface IRenderCommandBuffer : IAsyncDisposable
{
    /// <summary>
    /// Gets a value indicating whether the command buffer is currently recording commands.
    /// </summary>
    bool IsRecording { get; }

    /// <summary>
    /// Gets a value indicating whether the command buffer has been submitted for execution.
    /// </summary>
    bool IsSubmitted { get; }

    /// <summary>
    /// Begins recording commands into the buffer.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task BeginRecordingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ends recording commands into the buffer.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task EndRecordingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a render pass with the specified render targets.
    /// </summary>
    /// <param name="renderPass">The render pass configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task BeginRenderPassAsync(IRenderPass renderPass, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ends the current render pass.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task EndRenderPassAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the viewport for subsequent rendering operations.
    /// </summary>
    /// <param name="viewport">The viewport configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetViewportAsync(IViewport viewport, CancellationToken cancellationToken = default);

    /// <summary>
    /// Binds a shader program for subsequent draw calls.
    /// </summary>
    /// <param name="shaderProgram">The shader program to bind.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task BindShaderAsync(IShaderProgram shaderProgram, CancellationToken cancellationToken = default);

    /// <summary>
    /// Binds textures to shader resource slots.
    /// </summary>
    /// <param name="textures">The textures to bind mapped to their binding slots.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task BindTexturesAsync(IReadOnlyDictionary<uint, ITexture> textures, CancellationToken cancellationToken = default);

    /// <summary>
    /// Draws a mesh with the current render state.
    /// </summary>
    /// <param name="mesh">The mesh to draw.</param>
    /// <param name="instanceCount">The number of instances to draw.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task DrawMeshAsync(IMesh mesh, uint instanceCount = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Draws primitives directly without a mesh.
    /// </summary>
    /// <param name="topology">The primitive topology.</param>
    /// <param name="vertexCount">The number of vertices to draw.</param>
    /// <param name="instanceCount">The number of instances to draw.</param>
    /// <param name="firstVertex">The index of the first vertex.</param>
    /// <param name="firstInstance">The index of the first instance.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task DrawPrimitivesAsync(PrimitiveTopology topology, uint vertexCount, uint instanceCount = 1, uint firstVertex = 0, uint firstInstance = 0, CancellationToken cancellationToken = default);

    /// <summary>
    /// Draws indexed primitives.
    /// </summary>
    /// <param name="topology">The primitive topology.</param>
    /// <param name="indexCount">The number of indices to draw.</param>
    /// <param name="instanceCount">The number of instances to draw.</param>
    /// <param name="firstIndex">The index of the first index.</param>
    /// <param name="vertexOffset">The vertex offset to add to each index.</param>
    /// <param name="firstInstance">The index of the first instance.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task DrawIndexedPrimitivesAsync(PrimitiveTopology topology, uint indexCount, uint instanceCount = 1, uint firstIndex = 0, int vertexOffset = 0, uint firstInstance = 0, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies data from one buffer to another.
    /// </summary>
    /// <param name="source">The source buffer.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="sourceOffset">The offset in the source buffer.</param>
    /// <param name="destinationOffset">The offset in the destination buffer.</param>
    /// <param name="size">The number of bytes to copy.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task CopyBufferAsync(IBuffer source, IBuffer destination, ulong sourceOffset = 0, ulong destinationOffset = 0, ulong size = 0, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies data from one texture to another.
    /// </summary>
    /// <param name="source">The source texture.</param>
    /// <param name="destination">The destination texture.</param>
    /// <param name="sourceRegion">The region in the source texture to copy from.</param>
    /// <param name="destinationOffset">The offset in the destination texture.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task CopyTextureAsync(ITexture source, ITexture destination, TextureRegion? sourceRegion = null, Vector3? destinationOffset = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a memory barrier to ensure proper ordering of memory operations.
    /// </summary>
    /// <param name="barriers">The memory barriers to set.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetMemoryBarrierAsync(MemoryBarrier barriers, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for render contexts that maintain current rendering state.
/// </summary>
public interface IRenderContext
{
    /// <summary>
    /// Gets the current viewport.
    /// </summary>
    IViewport? CurrentViewport { get; }

    /// <summary>
    /// Gets the currently bound shader program.
    /// </summary>
    IShaderProgram? CurrentShader { get; }

    /// <summary>
    /// Gets the currently bound textures.
    /// </summary>
    IReadOnlyDictionary<uint, ITexture> BoundTextures { get; }

    /// <summary>
    /// Gets the current render state.
    /// </summary>
    RenderState CurrentState { get; }

    /// <summary>
    /// Gets the current camera being used for rendering.
    /// </summary>
    ICamera? CurrentCamera { get; }

    /// <summary>
    /// Gets the performance statistics for the current frame.
    /// </summary>
    FrameStatistics FrameStats { get; }

    /// <summary>
    /// Resets the render context to default state.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ResetAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for render passes that define rendering targets and operations.
/// </summary>
public interface IRenderPass
{
    /// <summary>
    /// Gets the color attachments for this render pass.
    /// </summary>
    IReadOnlyList<RenderAttachment> ColorAttachments { get; }

    /// <summary>
    /// Gets the depth/stencil attachment for this render pass.
    /// </summary>
    RenderAttachment? DepthStencilAttachment { get; }

    /// <summary>
    /// Gets the render area for this pass.
    /// </summary>
    Rectangle RenderArea { get; }

    /// <summary>
    /// Gets the clear color for color attachments.
    /// </summary>
    Vector4 ClearColor { get; }

    /// <summary>
    /// Gets the clear depth value.
    /// </summary>
    float ClearDepth { get; }

    /// <summary>
    /// Gets the clear stencil value.
    /// </summary>
    uint ClearStencil { get; }
}

/// <summary>
/// Describes a render attachment (render target).
/// </summary>
public record struct RenderAttachment
{
    /// <summary>
    /// Gets or sets the texture to render to.
    /// </summary>
    public ITexture Texture { get; set; }

    /// <summary>
    /// Gets or sets the mip level of the texture to render to.
    /// </summary>
    public uint MipLevel { get; set; }

    /// <summary>
    /// Gets or sets the array layer of the texture to render to.
    /// </summary>
    public uint ArrayLayer { get; set; }

    /// <summary>
    /// Gets or sets the load operation for the attachment.
    /// </summary>
    public AttachmentLoadOp LoadOp { get; set; }

    /// <summary>
    /// Gets or sets the store operation for the attachment.
    /// </summary>
    public AttachmentStoreOp StoreOp { get; set; }
}

/// <summary>
/// Describes a region within a texture.
/// </summary>
public record struct TextureRegion
{
    /// <summary>
    /// Gets or sets the offset of the region.
    /// </summary>
    public Vector3 Offset { get; set; }

    /// <summary>
    /// Gets or sets the size of the region.
    /// </summary>
    public Vector3 Size { get; set; }

    /// <summary>
    /// Gets or sets the mip level of the region.
    /// </summary>
    public uint MipLevel { get; set; }

    /// <summary>
    /// Gets or sets the array layer of the region.
    /// </summary>
    public uint ArrayLayer { get; set; }
}

/// <summary>
/// Describes a rectangular area.
/// </summary>
public record struct Rectangle
{
    /// <summary>
    /// Gets or sets the x coordinate of the rectangle.
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// Gets or sets the y coordinate of the rectangle.
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// Gets or sets the width of the rectangle.
    /// </summary>
    public uint Width { get; set; }

    /// <summary>
    /// Gets or sets the height of the rectangle.
    /// </summary>
    public uint Height { get; set; }
}

/// <summary>
/// Defines attachment load operations.
/// </summary>
public enum AttachmentLoadOp
{
    /// <summary>
    /// Load the existing contents of the attachment.
    /// </summary>
    Load,

    /// <summary>
    /// Clear the attachment to a specific value.
    /// </summary>
    Clear,

    /// <summary>
    /// Don't care about the initial contents.
    /// </summary>
    DontCare
}

/// <summary>
/// Defines attachment store operations.
/// </summary>
public enum AttachmentStoreOp
{
    /// <summary>
    /// Store the contents of the attachment.
    /// </summary>
    Store,

    /// <summary>
    /// Don't care about storing the contents.
    /// </summary>
    DontCare
}

/// <summary>
/// Defines memory barrier types.
/// </summary>
[Flags]
public enum MemoryBarrier
{
    /// <summary>
    /// No barrier.
    /// </summary>
    None = 0,

    /// <summary>
    /// Vertex attribute array reads.
    /// </summary>
    VertexAttribArray = 1 << 0,

    /// <summary>
    /// Element array buffer reads.
    /// </summary>
    ElementArray = 1 << 1,

    /// <summary>
    /// Uniform buffer reads.
    /// </summary>
    Uniform = 1 << 2,

    /// <summary>
    /// Texture fetches.
    /// </summary>
    TextureFetch = 1 << 3,

    /// <summary>
    /// Shader image access.
    /// </summary>
    ShaderImageAccess = 1 << 4,

    /// <summary>
    /// Command buffer access.
    /// </summary>
    CommandBuffer = 1 << 5,

    /// <summary>
    /// Pixel buffer access.
    /// </summary>
    PixelBuffer = 1 << 6,

    /// <summary>
    /// Texture update access.
    /// </summary>
    TextureUpdate = 1 << 7,

    /// <summary>
    /// Buffer update access.
    /// </summary>
    BufferUpdate = 1 << 8,

    /// <summary>
    /// Framebuffer access.
    /// </summary>
    Framebuffer = 1 << 9,

    /// <summary>
    /// Transform feedback buffer access.
    /// </summary>
    TransformFeedback = 1 << 10,

    /// <summary>
    /// Atomic counter access.
    /// </summary>
    AtomicCounter = 1 << 11,

    /// <summary>
    /// Shader storage buffer access.
    /// </summary>
    ShaderStorageBuffer = 1 << 12,

    /// <summary>
    /// All barrier types.
    /// </summary>
    All = ~0
}