using Microsoft.Extensions.Configuration;

namespace GameConsole.Configuration.Core;

/// <summary>
/// Interface for resolving environment-specific configuration settings
/// with support for configuration inheritance and overrides.
/// </summary>
public interface IEnvironmentConfigurationResolver
{
    /// <summary>
    /// Gets the supported environments by this resolver.
    /// </summary>
    IReadOnlyList<string> SupportedEnvironments { get; }
    
    /// <summary>
    /// Resolves environment-specific configuration for the given context.
    /// </summary>
    /// <param name="context">The configuration context containing environment information.</param>
    /// <param name="baseConfiguration">The base configuration to apply environment overrides to.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The environment-resolved configuration.</returns>
    Task<IConfiguration> ResolveAsync(
        ConfigurationContext context, 
        IConfiguration baseConfiguration, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Determines if a configuration key should be overridden for the given environment.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="environment">The environment name.</param>
    /// <returns>True if the key should be overridden for this environment.</returns>
    bool ShouldOverride(string key, string environment);
    
    /// <summary>
    /// Gets environment-specific configuration file paths for the given context.
    /// </summary>
    /// <param name="context">The configuration context.</param>
    /// <returns>The ordered list of configuration file paths to load.</returns>
    Task<IEnumerable<string>> GetConfigurationPathsAsync(ConfigurationContext context);
}