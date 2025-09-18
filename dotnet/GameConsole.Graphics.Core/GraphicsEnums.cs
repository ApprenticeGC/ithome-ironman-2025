namespace GameConsole.Graphics.Core;

/// <summary>
/// Defines the supported pixel formats for textures.
/// </summary>
public enum PixelFormat
{
    /// <summary>
    /// Unknown or unsupported format.
    /// </summary>
    Unknown,

    /// <summary>
    /// 8-bit red channel.
    /// </summary>
    R8,

    /// <summary>
    /// 8-bit red and green channels.
    /// </summary>
    RG8,

    /// <summary>
    /// 8-bit RGB channels.
    /// </summary>
    RGB8,

    /// <summary>
    /// 8-bit RGBA channels.
    /// </summary>
    RGBA8,

    /// <summary>
    /// 16-bit floating point red channel.
    /// </summary>
    R16F,

    /// <summary>
    /// 16-bit floating point red and green channels.
    /// </summary>
    RG16F,

    /// <summary>
    /// 16-bit floating point RGB channels.
    /// </summary>
    RGB16F,

    /// <summary>
    /// 16-bit floating point RGBA channels.
    /// </summary>
    RGBA16F,

    /// <summary>
    /// 32-bit floating point red channel.
    /// </summary>
    R32F,

    /// <summary>
    /// 32-bit floating point red and green channels.
    /// </summary>
    RG32F,

    /// <summary>
    /// 32-bit floating point RGB channels.
    /// </summary>
    RGB32F,

    /// <summary>
    /// 32-bit floating point RGBA channels.
    /// </summary>
    RGBA32F,

    /// <summary>
    /// 24-bit depth buffer.
    /// </summary>
    Depth24,

    /// <summary>
    /// 32-bit depth buffer.
    /// </summary>
    Depth32F,

    /// <summary>
    /// 24-bit depth with 8-bit stencil.
    /// </summary>
    Depth24Stencil8,

    /// <summary>
    /// 32-bit depth with 8-bit stencil.
    /// </summary>
    Depth32FStencil8
}

/// <summary>
/// Defines the supported texture types.
/// </summary>
public enum TextureType
{
    /// <summary>
    /// 1D texture.
    /// </summary>
    Texture1D,

    /// <summary>
    /// 2D texture.
    /// </summary>
    Texture2D,

    /// <summary>
    /// 3D texture.
    /// </summary>
    Texture3D,

    /// <summary>
    /// Cube map texture.
    /// </summary>
    TextureCube,

    /// <summary>
    /// 1D texture array.
    /// </summary>
    Texture1DArray,

    /// <summary>
    /// 2D texture array.
    /// </summary>
    Texture2DArray,

    /// <summary>
    /// Cube map array texture.
    /// </summary>
    TextureCubeArray
}

/// <summary>
/// Defines texture usage flags.
/// </summary>
[Flags]
public enum TextureUsage
{
    /// <summary>
    /// No specific usage.
    /// </summary>
    None = 0,

    /// <summary>
    /// Texture can be sampled in shaders.
    /// </summary>
    Sampled = 1 << 0,

    /// <summary>
    /// Texture can be used as a render target.
    /// </summary>
    RenderTarget = 1 << 1,

    /// <summary>
    /// Texture can be used for storage operations.
    /// </summary>
    Storage = 1 << 2,

    /// <summary>
    /// Texture can be used as a depth/stencil buffer.
    /// </summary>
    DepthStencil = 1 << 3,

    /// <summary>
    /// Texture data can be transferred to/from.
    /// </summary>
    Transfer = 1 << 4
}

/// <summary>
/// Defines the supported shader types.
/// </summary>
public enum ShaderType
{
    /// <summary>
    /// Vertex shader.
    /// </summary>
    Vertex,

    /// <summary>
    /// Fragment/pixel shader.
    /// </summary>
    Fragment,

    /// <summary>
    /// Geometry shader.
    /// </summary>
    Geometry,

    /// <summary>
    /// Tessellation control shader.
    /// </summary>
    TessellationControl,

    /// <summary>
    /// Tessellation evaluation shader.
    /// </summary>
    TessellationEvaluation,

    /// <summary>
    /// Compute shader.
    /// </summary>
    Compute
}

/// <summary>
/// Defines the supported shader languages.
/// </summary>
public enum ShaderLanguage
{
    /// <summary>
    /// GLSL (OpenGL Shading Language).
    /// </summary>
    GLSL,

    /// <summary>
    /// HLSL (High Level Shading Language).
    /// </summary>
    HLSL,

    /// <summary>
    /// SPIR-V bytecode.
    /// </summary>
    SPIRV,

    /// <summary>
    /// Metal Shading Language.
    /// </summary>
    MSL
}

/// <summary>
/// Defines buffer usage flags.
/// </summary>
[Flags]
public enum BufferUsage
{
    /// <summary>
    /// No specific usage.
    /// </summary>
    None = 0,

    /// <summary>
    /// Buffer contains vertex data.
    /// </summary>
    Vertex = 1 << 0,

    /// <summary>
    /// Buffer contains index data.
    /// </summary>
    Index = 1 << 1,

    /// <summary>
    /// Buffer contains uniform/constant data.
    /// </summary>
    Uniform = 1 << 2,

    /// <summary>
    /// Buffer can be used for storage operations.
    /// </summary>
    Storage = 1 << 3,

    /// <summary>
    /// Buffer data can be transferred to/from.
    /// </summary>
    Transfer = 1 << 4,

    /// <summary>
    /// Buffer can be used as indirect draw arguments.
    /// </summary>
    Indirect = 1 << 5
}

/// <summary>
/// Defines memory access patterns for buffers.
/// </summary>
public enum MemoryAccess
{
    /// <summary>
    /// GPU read/write access only.
    /// </summary>
    DeviceLocal,

    /// <summary>
    /// CPU write, GPU read access.
    /// </summary>
    HostVisible,

    /// <summary>
    /// CPU read/write, GPU read access.
    /// </summary>
    HostCoherent,

    /// <summary>
    /// CPU write once, GPU read frequently.
    /// </summary>
    HostCached
}

/// <summary>
/// Defines primitive topology for rendering.
/// </summary>
public enum PrimitiveTopology
{
    /// <summary>
    /// Point list.
    /// </summary>
    PointList,

    /// <summary>
    /// Line list.
    /// </summary>
    LineList,

    /// <summary>
    /// Line strip.
    /// </summary>
    LineStrip,

    /// <summary>
    /// Triangle list.
    /// </summary>
    TriangleList,

    /// <summary>
    /// Triangle strip.
    /// </summary>
    TriangleStrip,

    /// <summary>
    /// Triangle fan.
    /// </summary>
    TriangleFan,

    /// <summary>
    /// Line list with adjacency.
    /// </summary>
    LineListAdjacency,

    /// <summary>
    /// Line strip with adjacency.
    /// </summary>
    LineStripAdjacency,

    /// <summary>
    /// Triangle list with adjacency.
    /// </summary>
    TriangleListAdjacency,

    /// <summary>
    /// Triangle strip with adjacency.
    /// </summary>
    TriangleStripAdjacency,

    /// <summary>
    /// Patch list for tessellation.
    /// </summary>
    PatchList
}

/// <summary>
/// Defines vertex attribute formats.
/// </summary>
public enum AttributeFormat
{
    /// <summary>
    /// Single 32-bit float.
    /// </summary>
    Float,

    /// <summary>
    /// Two 32-bit floats.
    /// </summary>
    Float2,

    /// <summary>
    /// Three 32-bit floats.
    /// </summary>
    Float3,

    /// <summary>
    /// Four 32-bit floats.
    /// </summary>
    Float4,

    /// <summary>
    /// Single 32-bit signed integer.
    /// </summary>
    Int,

    /// <summary>
    /// Two 32-bit signed integers.
    /// </summary>
    Int2,

    /// <summary>
    /// Three 32-bit signed integers.
    /// </summary>
    Int3,

    /// <summary>
    /// Four 32-bit signed integers.
    /// </summary>
    Int4,

    /// <summary>
    /// Single 32-bit unsigned integer.
    /// </summary>
    UInt,

    /// <summary>
    /// Two 32-bit unsigned integers.
    /// </summary>
    UInt2,

    /// <summary>
    /// Three 32-bit unsigned integers.
    /// </summary>
    UInt3,

    /// <summary>
    /// Four 32-bit unsigned integers.
    /// </summary>
    UInt4,

    /// <summary>
    /// Single 8-bit unsigned byte.
    /// </summary>
    Byte,

    /// <summary>
    /// Two 8-bit unsigned bytes.
    /// </summary>
    Byte2,

    /// <summary>
    /// Three 8-bit unsigned bytes.
    /// </summary>
    Byte3,

    /// <summary>
    /// Four 8-bit unsigned bytes.
    /// </summary>
    Byte4
}