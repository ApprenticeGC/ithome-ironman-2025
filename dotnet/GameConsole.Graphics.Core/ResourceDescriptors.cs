using System.Numerics;

namespace GameConsole.Graphics.Core;

/// <summary>
/// Describes a texture resource and its properties.
/// </summary>
public record struct TextureDescriptor
{
    /// <summary>
    /// Gets or sets the width of the texture in pixels.
    /// </summary>
    public uint Width { get; set; }

    /// <summary>
    /// Gets or sets the height of the texture in pixels.
    /// </summary>
    public uint Height { get; set; }

    /// <summary>
    /// Gets or sets the depth of the texture for 3D textures.
    /// </summary>
    public uint Depth { get; set; }

    /// <summary>
    /// Gets or sets the number of mip levels in the texture.
    /// </summary>
    public uint MipLevels { get; set; }

    /// <summary>
    /// Gets or sets the pixel format of the texture.
    /// </summary>
    public PixelFormat Format { get; set; }

    /// <summary>
    /// Gets or sets the texture type.
    /// </summary>
    public TextureType Type { get; set; }

    /// <summary>
    /// Gets or sets the usage flags for the texture.
    /// </summary>
    public TextureUsage Usage { get; set; }

    /// <summary>
    /// Gets or sets the sample count for multisampled textures.
    /// </summary>
    public uint SampleCount { get; set; }
}

/// <summary>
/// Describes a shader resource and its properties.
/// </summary>
public record struct ShaderDescriptor
{
    /// <summary>
    /// Gets or sets the type of shader (vertex, fragment, etc.).
    /// </summary>
    public ShaderType Type { get; set; }

    /// <summary>
    /// Gets or sets the source code or bytecode of the shader.
    /// </summary>
    public ReadOnlyMemory<byte> Source { get; set; }

    /// <summary>
    /// Gets or sets the shader language (HLSL, GLSL, SPIR-V, etc.).
    /// </summary>
    public ShaderLanguage Language { get; set; }

    /// <summary>
    /// Gets or sets the entry point function name for the shader.
    /// </summary>
    public string EntryPoint { get; set; }

    /// <summary>
    /// Gets or sets the target platform for the shader.
    /// </summary>
    public string Target { get; set; }
}

/// <summary>
/// Describes a mesh resource and its properties.
/// </summary>
public record struct MeshDescriptor
{
    /// <summary>
    /// Gets or sets the vertex buffer descriptor.
    /// </summary>
    public BufferDescriptor VertexBuffer { get; set; }

    /// <summary>
    /// Gets or sets the index buffer descriptor.
    /// </summary>
    public BufferDescriptor? IndexBuffer { get; set; }

    /// <summary>
    /// Gets or sets the vertex layout describing the vertex attributes.
    /// </summary>
    public VertexLayout Layout { get; set; }

    /// <summary>
    /// Gets or sets the primitive topology for rendering.
    /// </summary>
    public PrimitiveTopology Topology { get; set; }

    /// <summary>
    /// Gets or sets the number of vertices in the mesh.
    /// </summary>
    public uint VertexCount { get; set; }

    /// <summary>
    /// Gets or sets the number of indices in the mesh.
    /// </summary>
    public uint IndexCount { get; set; }
}

/// <summary>
/// Describes a buffer resource and its properties.
/// </summary>
public record struct BufferDescriptor
{
    /// <summary>
    /// Gets or sets the size of the buffer in bytes.
    /// </summary>
    public ulong Size { get; set; }

    /// <summary>
    /// Gets or sets the usage flags for the buffer.
    /// </summary>
    public BufferUsage Usage { get; set; }

    /// <summary>
    /// Gets or sets the memory access pattern for the buffer.
    /// </summary>
    public MemoryAccess Access { get; set; }
}

/// <summary>
/// Describes the layout of vertex attributes in a vertex buffer.
/// </summary>
public record struct VertexLayout
{
    /// <summary>
    /// Gets or sets the vertex attributes.
    /// </summary>
    public VertexAttribute[] Attributes { get; set; }

    /// <summary>
    /// Gets or sets the stride (size in bytes) of each vertex.
    /// </summary>
    public uint Stride { get; set; }
}

/// <summary>
/// Describes a single vertex attribute.
/// </summary>
public record struct VertexAttribute
{
    /// <summary>
    /// Gets or sets the semantic name of the attribute (e.g., "POSITION", "TEXCOORD").
    /// </summary>
    public string Semantic { get; set; }

    /// <summary>
    /// Gets or sets the data format of the attribute.
    /// </summary>
    public AttributeFormat Format { get; set; }

    /// <summary>
    /// Gets or sets the offset in bytes from the start of the vertex.
    /// </summary>
    public uint Offset { get; set; }

    /// <summary>
    /// Gets or sets the binding index for the attribute.
    /// </summary>
    public uint Binding { get; set; }
}