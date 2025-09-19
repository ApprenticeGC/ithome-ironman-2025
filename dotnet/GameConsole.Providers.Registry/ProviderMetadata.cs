namespace GameConsole.Providers.Registry;

/// <summary>
/// Metadata information for a provider including versioning, capabilities, and platform support.
/// </summary>
/// <param name="Name">The friendly name of the provider.</param>
/// <param name="Version">The semantic version of the provider.</param>
/// <param name="Priority">Priority for provider selection (higher values have higher priority).</param>
/// <param name="Capabilities">Set of capabilities supported by this provider.</param>
/// <param name="SupportedPlatforms">Platforms supported by this provider.</param>
/// <param name="Description">Optional description of the provider.</param>
/// <param name="Author">Optional author information.</param>
/// <param name="Dependencies">Optional provider dependencies.</param>
public record ProviderMetadata(
    string Name,
    Version Version,
    int Priority,
    IReadOnlySet<string> Capabilities,
    Platform SupportedPlatforms,
    string? Description = null,
    string? Author = null,
    IReadOnlyDictionary<string, Version>? Dependencies = null);

/// <summary>
/// Platform flags for provider platform support.
/// </summary>
[Flags]
public enum Platform
{
    None = 0,
    Windows = 1 << 0,
    Linux = 1 << 1,
    MacOS = 1 << 2,
    Android = 1 << 3,
    iOS = 1 << 4,
    WebGL = 1 << 5,
    All = Windows | Linux | MacOS | Android | iOS | WebGL
}

/// <summary>
/// Selection criteria for choosing providers from the registry.
/// </summary>
/// <param name="RequiredCapabilities">Capabilities that must be supported by the selected provider.</param>
/// <param name="PreferredCapabilities">Capabilities that are preferred but not required.</param>
/// <param name="TargetPlatform">Target platform for the provider.</param>
/// <param name="MinimumPriority">Minimum priority level for provider selection.</param>
/// <param name="MinimumVersion">Minimum version requirement.</param>
/// <param name="MaximumVersion">Maximum version requirement.</param>
public record ProviderSelectionCriteria(
    IReadOnlySet<string>? RequiredCapabilities = null,
    IReadOnlySet<string>? PreferredCapabilities = null,
    Platform? TargetPlatform = null,
    int? MinimumPriority = null,
    Version? MinimumVersion = null,
    Version? MaximumVersion = null);

/// <summary>
/// Event arguments for provider registry changes.
/// </summary>
/// <typeparam name="TContract">The contract type of the provider.</typeparam>
/// <param name="Provider">The provider that was changed.</param>
/// <param name="Metadata">The metadata of the changed provider.</param>
/// <param name="ChangeType">The type of change that occurred.</param>
public record ProviderChangedEventArgs<TContract>(
    TContract Provider,
    ProviderMetadata Metadata,
    ProviderChangeType ChangeType) where TContract : class;

/// <summary>
/// Types of provider registry changes.
/// </summary>
public enum ProviderChangeType
{
    /// <summary>A provider was registered.</summary>
    Registered,
    
    /// <summary>A provider was unregistered.</summary>
    Unregistered,
    
    /// <summary>A provider was updated.</summary>
    Updated
}