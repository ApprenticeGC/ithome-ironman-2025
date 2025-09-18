namespace GameConsole.Providers.Registry;

/// <summary>
/// Defines criteria for selecting providers from the registry.
/// </summary>
/// <param name="RequiredCapabilities">Capabilities that must be supported by the provider.</param>
/// <param name="PreferredCapabilities">Capabilities that are preferred but not required.</param>
/// <param name="TargetPlatform">The target platform the provider should support.</param>
/// <param name="MinimumPriority">The minimum priority level required.</param>
/// <param name="MinimumVersion">The minimum version required.</param>
/// <param name="MaximumVersion">The maximum version allowed.</param>
public record ProviderSelectionCriteria(
    IReadOnlySet<string>? RequiredCapabilities = null,
    IReadOnlySet<string>? PreferredCapabilities = null,
    Platform? TargetPlatform = null,
    int? MinimumPriority = null,
    Version? MinimumVersion = null,
    Version? MaximumVersion = null);