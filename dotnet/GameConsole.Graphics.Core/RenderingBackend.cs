using System.Numerics;

namespace GameConsole.Graphics.Core;

/// <summary>
/// Interface for rendering backend abstraction that hides platform-specific graphics APIs.
/// </summary>
public interface IRenderingBackend
{
    /// <summary>
    /// Gets the type of rendering backend (OpenGL, DirectX, Vulkan, etc.).
    /// </summary>
    RenderingBackendType Type { get; }

    /// <summary>
    /// Gets the version string of the rendering backend.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets the vendor information for the graphics hardware.
    /// </summary>
    string Vendor { get; }

    /// <summary>
    /// Gets the name of the graphics device.
    /// </summary>
    string DeviceName { get; }

    /// <summary>
    /// Gets the capabilities and features supported by this backend.
    /// </summary>
    BackendCapabilities Capabilities { get; }

    /// <summary>
    /// Gets the current driver information.
    /// </summary>
    DriverInfo DriverInfo { get; }

    /// <summary>
    /// Gets the available memory information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns memory information.</returns>
    Task<MemoryInfo> GetMemoryInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific feature is supported by this backend.
    /// </summary>
    /// <param name="feature">The feature to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns true if the feature is supported.</returns>
    Task<bool> IsFeatureSupportedAsync(RenderingFeature feature, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the optimal settings for the current hardware.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns optimal settings.</returns>
    Task<OptimalSettings> GetOptimalSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that the backend is properly initialized and functional.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns validation results.</returns>
    Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Contains information about graphics driver.
/// </summary>
public record struct DriverInfo
{
    /// <summary>
    /// Gets or sets the driver version.
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    /// Gets or sets the driver name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the driver date.
    /// </summary>
    public DateTimeOffset Date { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the driver supports the required features.
    /// </summary>
    public bool IsCompatible { get; set; }
}

/// <summary>
/// Contains information about graphics memory.
/// </summary>
public record struct MemoryInfo
{
    /// <summary>
    /// Gets or sets the total available graphics memory in bytes.
    /// </summary>
    public ulong TotalMemory { get; set; }

    /// <summary>
    /// Gets or sets the currently used graphics memory in bytes.
    /// </summary>
    public ulong UsedMemory { get; set; }

    /// <summary>
    /// Gets or sets the available graphics memory in bytes.
    /// </summary>
    public ulong AvailableMemory { get; set; }

    /// <summary>
    /// Gets or sets the peak memory usage in bytes.
    /// </summary>
    public ulong PeakUsage { get; set; }

    /// <summary>
    /// Gets or sets the number of memory allocations.
    /// </summary>
    public uint AllocationCount { get; set; }
}

/// <summary>
/// Contains backend capabilities and supported features.
/// </summary>
public record struct BackendCapabilities
{
    /// <summary>
    /// Gets or sets the maximum texture size supported.
    /// </summary>
    public uint MaxTextureSize { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of texture units.
    /// </summary>
    public uint MaxTextureUnits { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of render targets.
    /// </summary>
    public uint MaxRenderTargets { get; set; }

    /// <summary>
    /// Gets or sets the maximum vertex attributes.
    /// </summary>
    public uint MaxVertexAttributes { get; set; }

    /// <summary>
    /// Gets or sets the maximum uniform buffer size.
    /// </summary>
    public uint MaxUniformBufferSize { get; set; }

    /// <summary>
    /// Gets or sets the supported shader model version.
    /// </summary>
    public string ShaderModel { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether tessellation is supported.
    /// </summary>
    public bool SupportsTessellation { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether geometry shaders are supported.
    /// </summary>
    public bool SupportsGeometryShaders { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether compute shaders are supported.
    /// </summary>
    public bool SupportsComputeShaders { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether instanced rendering is supported.
    /// </summary>
    public bool SupportsInstancing { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether multisampling is supported.
    /// </summary>
    public bool SupportsMultisampling { get; set; }

    /// <summary>
    /// Gets or sets the supported sample counts for multisampling.
    /// </summary>
    public uint[] SupportedSampleCounts { get; set; }
}

/// <summary>
/// Contains optimal settings for the current hardware.
/// </summary>
public record struct OptimalSettings
{
    /// <summary>
    /// Gets or sets the recommended texture quality level.
    /// </summary>
    public QualityLevel TextureQuality { get; set; }

    /// <summary>
    /// Gets or sets the recommended shadow quality level.
    /// </summary>
    public QualityLevel ShadowQuality { get; set; }

    /// <summary>
    /// Gets or sets the recommended antialiasing level.
    /// </summary>
    public uint AntiAliasingLevel { get; set; }

    /// <summary>
    /// Gets or sets the recommended anisotropic filtering level.
    /// </summary>
    public uint AnisotropicFiltering { get; set; }

    /// <summary>
    /// Gets or sets the recommended VSync setting.
    /// </summary>
    public bool VSync { get; set; }

    /// <summary>
    /// Gets or sets the recommended maximum frame rate.
    /// </summary>
    public uint MaxFrameRate { get; set; }

    /// <summary>
    /// Gets or sets the recommended buffer count for triple buffering.
    /// </summary>
    public uint BufferCount { get; set; }
}

/// <summary>
/// Contains validation results for the rendering backend.
/// </summary>
public record struct ValidationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the validation passed.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets any validation errors.
    /// </summary>
    public string[] Errors { get; set; }

    /// <summary>
    /// Gets or sets any validation warnings.
    /// </summary>
    public string[] Warnings { get; set; }

    /// <summary>
    /// Gets or sets performance recommendations.
    /// </summary>
    public string[] Recommendations { get; set; }
}

/// <summary>
/// Defines the supported rendering backend types.
/// </summary>
public enum RenderingBackendType
{
    /// <summary>
    /// Unknown or unsupported backend.
    /// </summary>
    Unknown,

    /// <summary>
    /// OpenGL backend.
    /// </summary>
    OpenGL,

    /// <summary>
    /// OpenGL ES backend.
    /// </summary>
    OpenGLES,

    /// <summary>
    /// DirectX 11 backend.
    /// </summary>
    DirectX11,

    /// <summary>
    /// DirectX 12 backend.
    /// </summary>
    DirectX12,

    /// <summary>
    /// Vulkan backend.
    /// </summary>
    Vulkan,

    /// <summary>
    /// Metal backend.
    /// </summary>
    Metal,

    /// <summary>
    /// WebGL backend.
    /// </summary>
    WebGL,

    /// <summary>
    /// Software rasterizer backend.
    /// </summary>
    Software
}

/// <summary>
/// Defines rendering features that can be queried for support.
/// </summary>
public enum RenderingFeature
{
    /// <summary>
    /// Tessellation shaders.
    /// </summary>
    Tessellation,

    /// <summary>
    /// Geometry shaders.
    /// </summary>
    GeometryShaders,

    /// <summary>
    /// Compute shaders.
    /// </summary>
    ComputeShaders,

    /// <summary>
    /// Instanced rendering.
    /// </summary>
    Instancing,

    /// <summary>
    /// Multisampling anti-aliasing.
    /// </summary>
    Multisampling,

    /// <summary>
    /// Anisotropic filtering.
    /// </summary>
    AnisotropicFiltering,

    /// <summary>
    /// Texture compression.
    /// </summary>
    TextureCompression,

    /// <summary>
    /// Floating point render targets.
    /// </summary>
    FloatRenderTargets,

    /// <summary>
    /// Multiple render targets.
    /// </summary>
    MultipleRenderTargets,

    /// <summary>
    /// Occlusion queries.
    /// </summary>
    OcclusionQueries,

    /// <summary>
    /// Timer queries.
    /// </summary>
    TimerQueries,

    /// <summary>
    /// Transform feedback.
    /// </summary>
    TransformFeedback,

    /// <summary>
    /// Shader storage buffer objects.
    /// </summary>
    ShaderStorageBuffers,

    /// <summary>
    /// Atomic counters.
    /// </summary>
    AtomicCounters,

    /// <summary>
    /// Image load/store operations.
    /// </summary>
    ImageLoadStore,

    /// <summary>
    /// Bindless textures.
    /// </summary>
    BindlessTextures,

    /// <summary>
    /// Conservative rasterization.
    /// </summary>
    ConservativeRasterization,

    /// <summary>
    /// Variable rate shading.
    /// </summary>
    VariableRateShading,

    /// <summary>
    /// Mesh shaders.
    /// </summary>
    MeshShaders,

    /// <summary>
    /// Ray tracing.
    /// </summary>
    RayTracing
}

/// <summary>
/// Defines quality levels for graphics settings.
/// </summary>
public enum QualityLevel
{
    /// <summary>
    /// Low quality settings.
    /// </summary>
    Low,

    /// <summary>
    /// Medium quality settings.
    /// </summary>
    Medium,

    /// <summary>
    /// High quality settings.
    /// </summary>
    High,

    /// <summary>
    /// Ultra quality settings.
    /// </summary>
    Ultra
}