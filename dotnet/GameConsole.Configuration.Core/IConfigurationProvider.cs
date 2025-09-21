using Microsoft.Extensions.Configuration;

namespace GameConsole.Configuration.Core;

/// <summary>
/// Defines the priority levels for configuration providers to determine
/// the order in which they are applied when building configuration.
/// </summary>
public enum ConfigurationPriority
{
    /// <summary>
    /// Lowest priority - applied first and can be overridden by higher priorities.
    /// </summary>
    Default = 0,
    
    /// <summary>
    /// Base configuration files (appsettings.json).
    /// </summary>
    Base = 100,
    
    /// <summary>
    /// Environment-specific configuration files.
    /// </summary>
    Environment = 200,
    
    /// <summary>
    /// User-specific configuration.
    /// </summary>
    User = 300,
    
    /// <summary>
    /// Environment variables.
    /// </summary>
    EnvironmentVariables = 400,
    
    /// <summary>
    /// Command line arguments - highest priority.
    /// </summary>
    CommandLine = 500
}

/// <summary>
/// Interface for configuration providers that supply configuration data
/// from various sources (JSON, XML, Environment Variables, etc.).
/// </summary>
public interface IConfigurationProvider
{
    /// <summary>
    /// Gets the name of this configuration provider.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Gets the priority of this provider for ordering in the configuration chain.
    /// </summary>
    ConfigurationPriority Priority { get; }
    
    /// <summary>
    /// Gets a value indicating whether this provider can contribute to the configuration
    /// for the given context.
    /// </summary>
    /// <param name="context">The configuration context.</param>
    /// <returns>True if this provider is applicable for the context.</returns>
    Task<bool> CanApplyAsync(ConfigurationContext context);
    
    /// <summary>
    /// Builds configuration from this provider's source(s).
    /// </summary>
    /// <param name="builder">The configuration builder to add sources to.</param>
    /// <param name="context">The configuration context.</param>
    /// <returns>A task representing the configuration building operation.</returns>
    Task BuildConfigurationAsync(IConfigurationBuilder builder, ConfigurationContext context);
    
    /// <summary>
    /// Gets a value indicating whether this provider supports hot-reload of configuration changes.
    /// </summary>
    bool SupportsReload { get; }
    
    /// <summary>
    /// Event raised when this provider detects configuration changes (if SupportsReload is true).
    /// </summary>
    event EventHandler? ConfigurationChanged;
}