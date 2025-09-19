using System.Numerics;

namespace GameConsole.Graphics.Core;

/// <summary>
/// Common graphics types used across the graphics system.
/// </summary>

/// <summary>
/// Represents a 2D texture resource.
/// </summary>
public record struct Texture2D(
    string Id,
    int Width, 
    int Height,
    TextureFormat Format,
    bool IsLoaded = false);

/// <summary>
/// Represents a 3D mesh resource.
/// </summary>
public record struct Mesh(
    string Id,
    int VertexCount,
    int IndexCount,
    bool IsLoaded = false);

/// <summary>
/// Represents a compiled shader program.
/// </summary>
public record struct Shader(
    string Id,
    ShaderType Type,
    string SourcePath,
    bool IsCompiled = false);

/// <summary>
/// Represents camera configuration.
/// </summary>
public record struct Camera(
    Vector3 Position,
    Vector3 Target,
    Vector3 Up,
    float FieldOfView = 45.0f,
    float NearPlane = 0.1f,
    float FarPlane = 1000.0f);

/// <summary>
/// Supported texture formats.
/// </summary>
public enum TextureFormat
{
    RGBA8,
    RGB8, 
    BGRA8,
    DXT1,
    DXT5,
    BC7
}

/// <summary>
/// Shader types supported.
/// </summary>
public enum ShaderType
{
    Vertex,
    Fragment,
    Geometry,
    Compute
}

/// <summary>
/// Graphics API backends.
/// </summary>
public enum GraphicsBackend
{
    DirectX12,
    Vulkan,
    OpenGL,
    Metal
}

/// <summary>
/// Render target configuration.
/// </summary>
public record struct RenderTarget(
    int Width,
    int Height,
    TextureFormat Format,
    bool HasDepthBuffer = true);