using GameConsole.Configuration.Core.Models;

namespace GameConsole.Configuration.Core;

/// <summary>
/// Resolves environment-specific configuration settings with support for inheritance and overrides.
/// </summary>
public interface IEnvironmentConfigurationResolver
{
    /// <summary>
    /// Gets the current environment name.
    /// </summary>
    string CurrentEnvironment { get; }

    /// <summary>
    /// Gets all supported environment names.
    /// </summary>
    IReadOnlyList<string> SupportedEnvironments { get; }

    /// <summary>
    /// Resolves the appropriate configuration file path for the given context.
    /// </summary>
    /// <param name="basePath">The base configuration file path.</param>
    /// <param name="context">The configuration context.</param>
    /// <returns>The resolved configuration file paths in priority order (highest first).</returns>
    IReadOnlyList<string> ResolveConfigurationPaths(string basePath, ConfigurationContext context);

    /// <summary>
    /// Determines if the given environment is valid.
    /// </summary>
    /// <param name="environment">The environment name to check.</param>
    /// <returns>True if the environment is valid.</returns>
    bool IsValidEnvironment(string environment);

    /// <summary>
    /// Gets environment-specific configuration overrides.
    /// </summary>
    /// <param name="context">The configuration context.</param>
    /// <returns>A dictionary of configuration overrides.</returns>
    Task<IReadOnlyDictionary<string, object>> GetEnvironmentOverridesAsync(ConfigurationContext context);

    /// <summary>
    /// Sets the current environment.
    /// </summary>
    /// <param name="environment">The environment to set.</param>
    void SetEnvironment(string environment);
}