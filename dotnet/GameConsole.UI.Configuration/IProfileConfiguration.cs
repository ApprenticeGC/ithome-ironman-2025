namespace GameConsole.UI.Configuration;

/// <summary>
/// Interface for UI profile configuration settings with validation and inheritance support.
/// Provides comprehensive profile configuration with type safety and fluent API capabilities.
/// </summary>
public interface IProfileConfiguration
{
    /// <summary>
    /// Gets the unique identifier for this profile configuration.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the human-readable name of the profile configuration.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of what this profile configuration provides.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the version of this profile configuration for migration support.
    /// </summary>
    Version Version { get; }

    /// <summary>
    /// Gets the configuration scope (Global, Mode, Plugin, User, Environment).
    /// </summary>
    ConfigurationScope Scope { get; }

    /// <summary>
    /// Gets the environment this configuration targets (Development, Staging, Production, etc.).
    /// </summary>
    string Environment { get; }

    /// <summary>
    /// Gets the profile this configuration inherits from, if any.
    /// </summary>
    string? InheritsFrom { get; }

    /// <summary>
    /// Gets all configuration settings as a read-only dictionary.
    /// </summary>
    IReadOnlyDictionary<string, object?> Settings { get; }

    /// <summary>
    /// Gets a configuration value with type safety.
    /// </summary>
    /// <typeparam name="T">The expected type of the configuration value.</typeparam>
    /// <param name="key">The configuration key.</param>
    /// <returns>The configuration value or default if not found.</returns>
    T? GetValue<T>(string key);

    /// <summary>
    /// Gets a configuration value with a fallback default.
    /// </summary>
    /// <typeparam name="T">The expected type of the configuration value.</typeparam>
    /// <param name="key">The configuration key.</param>
    /// <param name="defaultValue">The default value to return if the key is not found.</param>
    /// <returns>The configuration value or the provided default.</returns>
    T GetValue<T>(string key, T defaultValue);

    /// <summary>
    /// Checks if a configuration key exists.
    /// </summary>
    /// <param name="key">The configuration key to check.</param>
    /// <returns>True if the key exists, false otherwise.</returns>
    bool HasValue(string key);

    /// <summary>
    /// Validates the configuration against its schema and inheritance chain.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the validation operation with results.</returns>
    Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a copy of this configuration with specified overrides.
    /// </summary>
    /// <param name="overrides">Configuration values to override.</param>
    /// <returns>A new configuration instance with the overrides applied.</returns>
    IProfileConfiguration WithOverrides(IReadOnlyDictionary<string, object?> overrides);

    /// <summary>
    /// Serializes this configuration to JSON format.
    /// </summary>
    /// <returns>JSON representation of the configuration.</returns>
    string ToJson();

    /// <summary>
    /// Serializes this configuration to XML format.
    /// </summary>
    /// <returns>XML representation of the configuration.</returns>
    string ToXml();
}

/// <summary>
/// Defines the scope of a configuration profile.
/// </summary>
public enum ConfigurationScope
{
    /// <summary>
    /// Global configuration that applies to the entire application.
    /// </summary>
    Global,

    /// <summary>
    /// Mode-specific configuration (Game mode vs Editor mode).
    /// </summary>
    Mode,

    /// <summary>
    /// Plugin-specific configuration.
    /// </summary>
    Plugin,

    /// <summary>
    /// User-specific preferences and customizations.
    /// </summary>
    User,

    /// <summary>
    /// Environment-specific overrides (Development, Production, etc.).
    /// </summary>
    Environment
}