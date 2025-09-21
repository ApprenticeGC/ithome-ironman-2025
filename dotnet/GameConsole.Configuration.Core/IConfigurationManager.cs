using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Configuration;

namespace GameConsole.Configuration.Core;

/// <summary>
/// Central configuration management interface providing unified access to
/// all configuration sources with environment-aware resolution and validation.
/// </summary>
public interface IConfigurationManager : IService
{
    /// <summary>
    /// Gets the current configuration root.
    /// </summary>
    IConfiguration Configuration { get; }
    
    /// <summary>
    /// Gets the current configuration context.
    /// </summary>
    ConfigurationContext Context { get; }
    
    /// <summary>
    /// Event raised when configuration changes are detected.
    /// </summary>
    event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;
    
    /// <summary>
    /// Reloads the configuration from all sources asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the reload operation.</returns>
    Task ReloadAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a strongly-typed configuration section.
    /// </summary>
    /// <typeparam name="T">The type to bind the configuration to.</typeparam>
    /// <param name="sectionKey">The configuration section key.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The bound configuration object.</returns>
    Task<T?> GetSectionAsync<T>(string sectionKey, CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Validates the current configuration against its schema.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The validation result.</returns>
    Task<ConfigurationValidationResult> ValidateAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates a specific configuration section.
    /// </summary>
    /// <param name="sectionKey">The section key to validate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The validation result.</returns>
    Task<ConfigurationValidationResult> ValidateSectionAsync(string sectionKey, CancellationToken cancellationToken = default);
}