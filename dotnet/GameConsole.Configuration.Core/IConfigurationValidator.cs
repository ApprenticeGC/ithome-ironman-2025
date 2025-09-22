using Microsoft.Extensions.Configuration;
using GameConsole.Configuration.Core.Models;

namespace GameConsole.Configuration.Core;

/// <summary>
/// Validates configuration against schemas and business rules.
/// </summary>
public interface IConfigurationValidator
{
    /// <summary>
    /// Validates the provided configuration.
    /// </summary>
    /// <param name="configuration">The configuration to validate.</param>
    /// <param name="context">The configuration context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The validation result.</returns>
    Task<ValidationResult> ValidateAsync(IConfiguration configuration, ConfigurationContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a specific configuration section.
    /// </summary>
    /// <typeparam name="T">The type of the configuration section.</typeparam>
    /// <param name="section">The configuration section to validate.</param>
    /// <param name="sectionPath">The path of the configuration section.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The validation result.</returns>
    Task<ValidationResult> ValidateSectionAsync<T>(T section, string sectionPath, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Registers a custom validator for a specific configuration section type.
    /// </summary>
    /// <typeparam name="T">The configuration section type.</typeparam>
    /// <param name="validator">The validator function.</param>
    void RegisterValidator<T>(Func<T, string, Task<ValidationResult>> validator) where T : class;

    /// <summary>
    /// Registers a schema validator for JSON configuration sections.
    /// </summary>
    /// <param name="sectionPath">The configuration section path.</param>
    /// <param name="jsonSchema">The JSON schema for validation.</param>
    void RegisterJsonSchema(string sectionPath, string jsonSchema);

    /// <summary>
    /// Removes a registered validator for a specific configuration section type.
    /// </summary>
    /// <typeparam name="T">The configuration section type.</typeparam>
    void RemoveValidator<T>() where T : class;
}