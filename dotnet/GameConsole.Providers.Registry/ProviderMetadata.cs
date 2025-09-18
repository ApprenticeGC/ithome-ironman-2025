namespace GameConsole.Providers.Registry;

/// <summary>
/// Contains metadata information about a provider including versioning, capabilities, and platform support.
/// </summary>
/// <param name="Name">The name of the provider.</param>
/// <param name="Priority">The priority of the provider for selection (higher values have higher priority).</param>
/// <param name="Capabilities">The set of capabilities supported by this provider.</param>
/// <param name="SupportedPlatforms">The platforms supported by this provider.</param>
/// <param name="Version">The semantic version of the provider.</param>
public record ProviderMetadata(
    string Name,
    int Priority,
    IReadOnlySet<string> Capabilities,
    Platform SupportedPlatforms,
    Version Version);