using System.Numerics;

namespace GameConsole.Graphics.Core;

/// <summary>
/// Contains current rendering state for graphics operations.
/// </summary>
public record struct RenderState
{
    /// <summary>
    /// Gets or sets the blend state configuration.
    /// </summary>
    public BlendState BlendState { get; set; }

    /// <summary>
    /// Gets or sets the depth/stencil state configuration.
    /// </summary>
    public DepthStencilState DepthStencilState { get; set; }

    /// <summary>
    /// Gets or sets the rasterizer state configuration.
    /// </summary>
    public RasterizerState RasterizerState { get; set; }

    /// <summary>
    /// Gets or sets the scissor test rectangle.
    /// </summary>
    public Rectangle? ScissorRectangle { get; set; }

    /// <summary>
    /// Gets or sets the current line width.
    /// </summary>
    public float LineWidth { get; set; }

    /// <summary>
    /// Gets or sets the current point size.
    /// </summary>
    public float PointSize { get; set; }

    /// <summary>
    /// Gets or sets the current clear color.
    /// </summary>
    public Vector4 ClearColor { get; set; }

    /// <summary>
    /// Gets or sets the current clear depth value.
    /// </summary>
    public float ClearDepth { get; set; }

    /// <summary>
    /// Gets or sets the current clear stencil value.
    /// </summary>
    public uint ClearStencil { get; set; }
}

/// <summary>
/// Configuration for color blending operations.
/// </summary>
public record struct BlendState
{
    /// <summary>
    /// Gets or sets a value indicating whether blending is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the source blend factor for RGB.
    /// </summary>
    public BlendFactor SourceRgb { get; set; }

    /// <summary>
    /// Gets or sets the destination blend factor for RGB.
    /// </summary>
    public BlendFactor DestinationRgb { get; set; }

    /// <summary>
    /// Gets or sets the source blend factor for alpha.
    /// </summary>
    public BlendFactor SourceAlpha { get; set; }

    /// <summary>
    /// Gets or sets the destination blend factor for alpha.
    /// </summary>
    public BlendFactor DestinationAlpha { get; set; }

    /// <summary>
    /// Gets or sets the blend operation for RGB.
    /// </summary>
    public BlendOperation OperationRgb { get; set; }

    /// <summary>
    /// Gets or sets the blend operation for alpha.
    /// </summary>
    public BlendOperation OperationAlpha { get; set; }

    /// <summary>
    /// Gets or sets the constant blend color.
    /// </summary>
    public Vector4 ConstantColor { get; set; }

    /// <summary>
    /// Gets or sets the color write mask.
    /// </summary>
    public ColorWriteMask WriteMask { get; set; }
}

/// <summary>
/// Configuration for depth and stencil testing.
/// </summary>
public record struct DepthStencilState
{
    /// <summary>
    /// Gets or sets a value indicating whether depth testing is enabled.
    /// </summary>
    public bool DepthTestEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether depth writing is enabled.
    /// </summary>
    public bool DepthWriteEnabled { get; set; }

    /// <summary>
    /// Gets or sets the depth comparison function.
    /// </summary>
    public ComparisonFunction DepthFunction { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether stencil testing is enabled.
    /// </summary>
    public bool StencilTestEnabled { get; set; }

    /// <summary>
    /// Gets or sets the stencil read mask.
    /// </summary>
    public uint StencilReadMask { get; set; }

    /// <summary>
    /// Gets or sets the stencil write mask.
    /// </summary>
    public uint StencilWriteMask { get; set; }

    /// <summary>
    /// Gets or sets the front face stencil operation.
    /// </summary>
    public StencilOperation FrontFace { get; set; }

    /// <summary>
    /// Gets or sets the back face stencil operation.
    /// </summary>
    public StencilOperation BackFace { get; set; }

    /// <summary>
    /// Gets or sets the stencil reference value.
    /// </summary>
    public uint StencilReference { get; set; }
}

/// <summary>
/// Configuration for rasterization operations.
/// </summary>
public record struct RasterizerState
{
    /// <summary>
    /// Gets or sets the polygon fill mode.
    /// </summary>
    public FillMode FillMode { get; set; }

    /// <summary>
    /// Gets or sets the polygon cull mode.
    /// </summary>
    public CullMode CullMode { get; set; }

    /// <summary>
    /// Gets or sets the front face winding order.
    /// </summary>
    public FrontFace FrontFace { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether scissor testing is enabled.
    /// </summary>
    public bool ScissorTestEnabled { get; set; }

    /// <summary>
    /// Gets or sets the depth bias constant factor.
    /// </summary>
    public float DepthBias { get; set; }

    /// <summary>
    /// Gets or sets the depth bias slope factor.
    /// </summary>
    public float DepthBiasSlope { get; set; }

    /// <summary>
    /// Gets or sets the depth bias clamp value.
    /// </summary>
    public float DepthBiasClamp { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether multisampling is enabled.
    /// </summary>
    public bool MultisampleEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether alpha to coverage is enabled.
    /// </summary>
    public bool AlphaToCoverageEnabled { get; set; }
}

/// <summary>
/// Configuration for stencil operations.
/// </summary>
public record struct StencilOperation
{
    /// <summary>
    /// Gets or sets the stencil comparison function.
    /// </summary>
    public ComparisonFunction Function { get; set; }

    /// <summary>
    /// Gets or sets the operation to perform when stencil test fails.
    /// </summary>
    public StencilOp StencilFail { get; set; }

    /// <summary>
    /// Gets or sets the operation to perform when depth test fails.
    /// </summary>
    public StencilOp DepthFail { get; set; }

    /// <summary>
    /// Gets or sets the operation to perform when both tests pass.
    /// </summary>
    public StencilOp Pass { get; set; }
}

/// <summary>
/// Contains performance statistics for a rendered frame.
/// </summary>
public record struct FrameStatistics
{
    /// <summary>
    /// Gets or sets the frame number.
    /// </summary>
    public ulong FrameNumber { get; set; }

    /// <summary>
    /// Gets or sets the frame time in milliseconds.
    /// </summary>
    public double FrameTime { get; set; }

    /// <summary>
    /// Gets or sets the frames per second.
    /// </summary>
    public double FramesPerSecond { get; set; }

    /// <summary>
    /// Gets or sets the number of draw calls.
    /// </summary>
    public uint DrawCalls { get; set; }

    /// <summary>
    /// Gets or sets the number of triangles rendered.
    /// </summary>
    public ulong TrianglesRendered { get; set; }

    /// <summary>
    /// Gets or sets the number of vertices processed.
    /// </summary>
    public ulong VerticesProcessed { get; set; }

    /// <summary>
    /// Gets or sets the number of pixels rendered.
    /// </summary>
    public ulong PixelsRendered { get; set; }

    /// <summary>
    /// Gets or sets the number of texture bindings.
    /// </summary>
    public uint TextureBindings { get; set; }

    /// <summary>
    /// Gets or sets the number of shader switches.
    /// </summary>
    public uint ShaderSwitches { get; set; }

    /// <summary>
    /// Gets or sets the number of render target switches.
    /// </summary>
    public uint RenderTargetSwitches { get; set; }

    /// <summary>
    /// Gets or sets the GPU memory used in bytes.
    /// </summary>
    public ulong GpuMemoryUsed { get; set; }

    /// <summary>
    /// Gets or sets the CPU time spent on graphics in milliseconds.
    /// </summary>
    public double CpuTime { get; set; }

    /// <summary>
    /// Gets or sets the GPU time spent on graphics in milliseconds.
    /// </summary>
    public double GpuTime { get; set; }
}

/// <summary>
/// Defines blend factors for color blending.
/// </summary>
public enum BlendFactor
{
    /// <summary>
    /// Zero (0, 0, 0, 0).
    /// </summary>
    Zero,

    /// <summary>
    /// One (1, 1, 1, 1).
    /// </summary>
    One,

    /// <summary>
    /// Source color.
    /// </summary>
    SourceColor,

    /// <summary>
    /// One minus source color.
    /// </summary>
    OneMinusSourceColor,

    /// <summary>
    /// Destination color.
    /// </summary>
    DestinationColor,

    /// <summary>
    /// One minus destination color.
    /// </summary>
    OneMinusDestinationColor,

    /// <summary>
    /// Source alpha.
    /// </summary>
    SourceAlpha,

    /// <summary>
    /// One minus source alpha.
    /// </summary>
    OneMinusSourceAlpha,

    /// <summary>
    /// Destination alpha.
    /// </summary>
    DestinationAlpha,

    /// <summary>
    /// One minus destination alpha.
    /// </summary>
    OneMinusDestinationAlpha,

    /// <summary>
    /// Constant color.
    /// </summary>
    ConstantColor,

    /// <summary>
    /// One minus constant color.
    /// </summary>
    OneMinusConstantColor,

    /// <summary>
    /// Constant alpha.
    /// </summary>
    ConstantAlpha,

    /// <summary>
    /// One minus constant alpha.
    /// </summary>
    OneMinusConstantAlpha,

    /// <summary>
    /// Source alpha saturated.
    /// </summary>
    SourceAlphaSaturate
}

/// <summary>
/// Defines blend operations.
/// </summary>
public enum BlendOperation
{
    /// <summary>
    /// Add source and destination.
    /// </summary>
    Add,

    /// <summary>
    /// Subtract destination from source.
    /// </summary>
    Subtract,

    /// <summary>
    /// Subtract source from destination.
    /// </summary>
    ReverseSubtract,

    /// <summary>
    /// Minimum of source and destination.
    /// </summary>
    Min,

    /// <summary>
    /// Maximum of source and destination.
    /// </summary>
    Max
}

/// <summary>
/// Defines color write masks.
/// </summary>
[Flags]
public enum ColorWriteMask
{
    /// <summary>
    /// No color channels.
    /// </summary>
    None = 0,

    /// <summary>
    /// Red channel.
    /// </summary>
    Red = 1 << 0,

    /// <summary>
    /// Green channel.
    /// </summary>
    Green = 1 << 1,

    /// <summary>
    /// Blue channel.
    /// </summary>
    Blue = 1 << 2,

    /// <summary>
    /// Alpha channel.
    /// </summary>
    Alpha = 1 << 3,

    /// <summary>
    /// All color channels.
    /// </summary>
    All = Red | Green | Blue | Alpha
}

/// <summary>
/// Defines comparison functions.
/// </summary>
public enum ComparisonFunction
{
    /// <summary>
    /// Never pass.
    /// </summary>
    Never,

    /// <summary>
    /// Pass if less than.
    /// </summary>
    Less,

    /// <summary>
    /// Pass if equal.
    /// </summary>
    Equal,

    /// <summary>
    /// Pass if less than or equal.
    /// </summary>
    LessEqual,

    /// <summary>
    /// Pass if greater than.
    /// </summary>
    Greater,

    /// <summary>
    /// Pass if not equal.
    /// </summary>
    NotEqual,

    /// <summary>
    /// Pass if greater than or equal.
    /// </summary>
    GreaterEqual,

    /// <summary>
    /// Always pass.
    /// </summary>
    Always
}

/// <summary>
/// Defines stencil operations.
/// </summary>
public enum StencilOp
{
    /// <summary>
    /// Keep the current value.
    /// </summary>
    Keep,

    /// <summary>
    /// Set to zero.
    /// </summary>
    Zero,

    /// <summary>
    /// Replace with reference value.
    /// </summary>
    Replace,

    /// <summary>
    /// Increment and clamp.
    /// </summary>
    Increment,

    /// <summary>
    /// Increment and wrap.
    /// </summary>
    IncrementWrap,

    /// <summary>
    /// Decrement and clamp.
    /// </summary>
    Decrement,

    /// <summary>
    /// Decrement and wrap.
    /// </summary>
    DecrementWrap,

    /// <summary>
    /// Bitwise invert.
    /// </summary>
    Invert
}

/// <summary>
/// Defines polygon fill modes.
/// </summary>
public enum FillMode
{
    /// <summary>
    /// Fill with points.
    /// </summary>
    Point,

    /// <summary>
    /// Fill with wireframe.
    /// </summary>
    Wireframe,

    /// <summary>
    /// Fill solid.
    /// </summary>
    Solid
}

/// <summary>
/// Defines polygon cull modes.
/// </summary>
public enum CullMode
{
    /// <summary>
    /// No culling.
    /// </summary>
    None,

    /// <summary>
    /// Cull front faces.
    /// </summary>
    Front,

    /// <summary>
    /// Cull back faces.
    /// </summary>
    Back,

    /// <summary>
    /// Cull front and back faces.
    /// </summary>
    FrontAndBack
}

/// <summary>
/// Defines front face winding orders.
/// </summary>
public enum FrontFace
{
    /// <summary>
    /// Clockwise winding.
    /// </summary>
    Clockwise,

    /// <summary>
    /// Counter-clockwise winding.
    /// </summary>
    CounterClockwise
}