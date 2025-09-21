using System.Text.Json.Serialization;

namespace GameConsole.UI.Profiles;

/// <summary>
/// Metadata information about a UI profile.
/// </summary>
public record UIProfileMetadata
{
    /// <summary>
    /// Version of the profile.
    /// </summary>
    public string Version { get; init; } = "1.0.0";

    /// <summary>
    /// Author or creator of the profile.
    /// </summary>
    public string Author { get; init; } = "System";

    /// <summary>
    /// Description of what this profile provides.
    /// </summary>
    public string Description { get; init; } = "";

    /// <summary>
    /// Tags associated with this profile for categorization.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Whether this profile supports hot-reloading during development.
    /// </summary>
    public bool SupportsHotReload { get; init; } = false;

    /// <summary>
    /// Priority level for profile selection (higher = preferred).
    /// </summary>
    public int Priority { get; init; } = 0;

    /// <summary>
    /// Profiles that this profile depends on or inherits from.
    /// </summary>
    public IReadOnlyList<string> Dependencies { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Current UI context information used for profile activation decisions.
/// </summary>
public record UIContext
{
    /// <summary>
    /// Current operating system.
    /// </summary>
    public string Platform { get; init; } = Environment.OSVersion.Platform.ToString();

    /// <summary>
    /// Available display capabilities.
    /// </summary>
    public DisplayCapabilities Display { get; init; } = new();

    /// <summary>
    /// Current user preferences.
    /// </summary>
    public UserPreferences User { get; init; } = new();

    /// <summary>
    /// Runtime environment information.
    /// </summary>
    public RuntimeEnvironment Runtime { get; init; } = new();

    /// <summary>
    /// Additional context properties.
    /// </summary>
    public IReadOnlyDictionary<string, object> Properties { get; init; } = new Dictionary<string, object>();
}

/// <summary>
/// Display capabilities of the current system.
/// </summary>
public record DisplayCapabilities
{
    /// <summary>
    /// Whether a graphical display is available.
    /// </summary>
    public bool HasGraphicalDisplay { get; init; } = true;

    /// <summary>
    /// Screen resolution width.
    /// </summary>
    public int Width { get; init; } = 1920;

    /// <summary>
    /// Screen resolution height.
    /// </summary>
    public int Height { get; init; } = 1080;

    /// <summary>
    /// Color depth in bits.
    /// </summary>
    public int ColorDepth { get; init; } = 32;

    /// <summary>
    /// Whether touch input is supported.
    /// </summary>
    public bool TouchSupported { get; init; } = false;
}

/// <summary>
/// User preferences that influence profile selection.
/// </summary>
public record UserPreferences
{
    /// <summary>
    /// Preferred UI theme (light, dark, etc.).
    /// </summary>
    public string Theme { get; init; } = "dark";

    /// <summary>
    /// Preferred language/locale.
    /// </summary>
    public string Language { get; init; } = "en-US";

    /// <summary>
    /// Accessibility requirements.
    /// </summary>
    public AccessibilityOptions Accessibility { get; init; } = new();

    /// <summary>
    /// Performance preferences.
    /// </summary>
    public PerformancePreferences Performance { get; init; } = new();
}

/// <summary>
/// Accessibility options for UI profiles.
/// </summary>
public record AccessibilityOptions
{
    /// <summary>
    /// Whether high contrast mode is enabled.
    /// </summary>
    public bool HighContrast { get; init; } = false;

    /// <summary>
    /// Text scaling factor.
    /// </summary>
    public double TextScale { get; init; } = 1.0;

    /// <summary>
    /// Whether screen reader support is needed.
    /// </summary>
    public bool ScreenReader { get; init; } = false;

    /// <summary>
    /// Whether keyboard-only navigation is preferred.
    /// </summary>
    public bool KeyboardOnlyNavigation { get; init; } = false;
}

/// <summary>
/// Performance preferences for UI profiles.
/// </summary>
public record PerformancePreferences
{
    /// <summary>
    /// Whether to prefer low resource usage.
    /// </summary>
    public bool LowResourceUsage { get; init; } = false;

    /// <summary>
    /// Whether animations should be reduced or disabled.
    /// </summary>
    public bool ReducedAnimations { get; init; } = false;

    /// <summary>
    /// Maximum memory usage preference in MB.
    /// </summary>
    public int MaxMemoryUsageMB { get; init; } = 512;
}

/// <summary>
/// Runtime environment information.
/// </summary>
public record RuntimeEnvironment
{
    /// <summary>
    /// Whether running in development mode.
    /// </summary>
    public bool IsDevelopment { get; init; } = false;

    /// <summary>
    /// Whether running in a container.
    /// </summary>
    public bool IsContainer { get; init; } = false;

    /// <summary>
    /// Whether network access is available.
    /// </summary>
    public bool HasNetworkAccess { get; init; } = true;

    /// <summary>
    /// Available system resources.
    /// </summary>
    public SystemResources Resources { get; init; } = new();
}

/// <summary>
/// Available system resources.
/// </summary>
public record SystemResources
{
    /// <summary>
    /// Available memory in MB.
    /// </summary>
    public long AvailableMemoryMB { get; init; } = 4096;

    /// <summary>
    /// Number of CPU cores.
    /// </summary>
    public int CpuCores { get; init; } = Environment.ProcessorCount;

    /// <summary>
    /// Whether GPU acceleration is available.
    /// </summary>
    public bool GpuAcceleration { get; init; } = true;
}

/// <summary>
/// Command set configuration for UI profiles.
/// </summary>
public record CommandSet
{
    /// <summary>
    /// Available commands in this profile.
    /// </summary>
    public IReadOnlyList<CommandInfo> Commands { get; init; } = Array.Empty<CommandInfo>();

    /// <summary>
    /// Command categories and their priorities.
    /// </summary>
    public IReadOnlyDictionary<string, int> Categories { get; init; } = new Dictionary<string, int>();

    /// <summary>
    /// Keyboard shortcuts for commands.
    /// </summary>
    public IReadOnlyDictionary<string, string> Shortcuts { get; init; } = new Dictionary<string, string>();
}

/// <summary>
/// Information about a command.
/// </summary>
public record CommandInfo
{
    /// <summary>
    /// Command identifier.
    /// </summary>
    public string Id { get; init; } = "";

    /// <summary>
    /// Display name for the command.
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// Command description.
    /// </summary>
    public string Description { get; init; } = "";

    /// <summary>
    /// Command category.
    /// </summary>
    public string Category { get; init; } = "General";

    /// <summary>
    /// Whether the command is enabled in this profile.
    /// </summary>
    public bool Enabled { get; init; } = true;
}

/// <summary>
/// Layout configuration for UI profiles.
/// </summary>
public record LayoutConfiguration
{
    /// <summary>
    /// Primary layout type (console, web, desktop).
    /// </summary>
    public string LayoutType { get; init; } = "console";

    /// <summary>
    /// Panel configurations.
    /// </summary>
    public IReadOnlyList<PanelConfig> Panels { get; init; } = Array.Empty<PanelConfig>();

    /// <summary>
    /// Layout-specific properties.
    /// </summary>
    public IReadOnlyDictionary<string, object> Properties { get; init; } = new Dictionary<string, object>();
}

/// <summary>
/// Configuration for a UI panel.
/// </summary>
public record PanelConfig
{
    /// <summary>
    /// Panel identifier.
    /// </summary>
    public string Id { get; init; } = "";

    /// <summary>
    /// Panel type.
    /// </summary>
    public string Type { get; init; } = "";

    /// <summary>
    /// Whether the panel is visible.
    /// </summary>
    public bool Visible { get; init; } = true;

    /// <summary>
    /// Panel position and size.
    /// </summary>
    public PanelBounds Bounds { get; init; } = new();

    /// <summary>
    /// Panel-specific properties.
    /// </summary>
    public IReadOnlyDictionary<string, object> Properties { get; init; } = new Dictionary<string, object>();
}

/// <summary>
/// Panel position and size information.
/// </summary>
public record PanelBounds
{
    /// <summary>
    /// X position.
    /// </summary>
    public double X { get; init; } = 0;

    /// <summary>
    /// Y position.
    /// </summary>
    public double Y { get; init; } = 0;

    /// <summary>
    /// Panel width.
    /// </summary>
    public double Width { get; init; } = 100;

    /// <summary>
    /// Panel height.
    /// </summary>
    public double Height { get; init; } = 100;
}