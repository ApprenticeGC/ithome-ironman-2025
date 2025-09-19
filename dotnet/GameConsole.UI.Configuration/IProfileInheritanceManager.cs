namespace GameConsole.UI.Configuration;

/// <summary>
/// Manages configuration inheritance and override capabilities between profiles.
/// </summary>
public interface IProfileInheritanceManager
{
    /// <summary>
    /// Resolves the effective configuration by applying inheritance and overrides.
    /// </summary>
    /// <param name="profileConfiguration">The profile configuration to resolve.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The resolved configuration with inheritance applied.</returns>
    Task<IProfileConfiguration> ResolveConfigurationAsync(
        IProfileConfiguration profileConfiguration, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the inheritance chain for a given profile configuration.
    /// </summary>
    /// <param name="profileId">The profile ID to get the inheritance chain for.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>An ordered list of profile configurations from root to leaf.</returns>
    Task<IReadOnlyList<IProfileConfiguration>> GetInheritanceChainAsync(
        string profileId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a profile configuration can inherit from another.
    /// </summary>
    /// <param name="childProfileId">The child profile ID.</param>
    /// <param name="parentProfileId">The parent profile ID.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if inheritance is valid, false otherwise.</returns>
    Task<bool> CanInheritFromAsync(
        string childProfileId, 
        string parentProfileId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects circular inheritance dependencies.
    /// </summary>
    /// <param name="profileId">The profile ID to check for circular dependencies.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if circular dependency detected, false otherwise.</returns>
    Task<bool> HasCircularDependencyAsync(
        string profileId, 
        CancellationToken cancellationToken = default);
}