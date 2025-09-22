using Microsoft.Extensions.Configuration;
using GameConsole.Configuration.Core.Models;

namespace GameConsole.Configuration.Core;

/// <summary>
/// Provides centralized configuration management for the GameConsole system.
/// Supports multiple configuration sources, environment-specific resolution, and change notifications.
/// </summary>
public interface IConfigurationManager
{
    /// <summary>
    /// Gets the current configuration root.
    /// </summary>
    IConfigurationRoot Configuration { get; }

    /// <summary>
    /// Gets a strongly-typed configuration section.
    /// </summary>
    /// <typeparam name="T">The type to bind the configuration to.</typeparam>
    /// <param name="key">The configuration section key.</param>
    /// <returns>The bound configuration object.</returns>
    T GetSection<T>(string key) where T : class, new();

    /// <summary>
    /// Gets a configuration value.
    /// </summary>
    /// <typeparam name="T">The type of the configuration value.</typeparam>
    /// <param name="key">The configuration key.</param>
    /// <param name="defaultValue">The default value if not found.</param>
    /// <returns>The configuration value or default.</returns>
    T GetValue<T>(string key, T defaultValue = default!);

    /// <summary>
    /// Sets a configuration value at runtime.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The value to set.</param>
    Task SetValueAsync(string key, object value);

    /// <summary>
    /// Reloads configuration from all providers.
    /// </summary>
    Task ReloadAsync();

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <param name="context">The configuration context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The validation result.</returns>
    Task<ValidationResult> ValidateAsync(ConfigurationContext? context = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Occurs when configuration changes are detected.
    /// </summary>
    event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;
}

/// <summary>
/// Event arguments for configuration change notifications.
/// </summary>
public sealed class ConfigurationChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the configuration keys that changed.
    /// </summary>
    public IReadOnlyList<string> ChangedKeys { get; }

    /// <summary>
    /// Gets the context in which the change occurred.
    /// </summary>
    public ConfigurationContext Context { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationChangedEventArgs"/> class.
    /// </summary>
    public ConfigurationChangedEventArgs(IReadOnlyList<string> changedKeys, ConfigurationContext context)
    {
        ChangedKeys = changedKeys ?? throw new ArgumentNullException(nameof(changedKeys));
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }
}