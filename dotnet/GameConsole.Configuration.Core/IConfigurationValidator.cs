using System.ComponentModel.DataAnnotations;

namespace GameConsole.Configuration.Core;

/// <summary>
/// Interface for validating configuration sections against schemas
/// and business rules with comprehensive error reporting.
/// </summary>
public interface IConfigurationValidator
{
    /// <summary>
    /// Gets the supported validation types by this validator.
    /// </summary>
    IReadOnlyList<Type> SupportedTypes { get; }
    
    /// <summary>
    /// Validates a configuration object against its schema and business rules.
    /// </summary>
    /// <typeparam name="T">The type of configuration object to validate.</typeparam>
    /// <param name="configurationObject">The configuration object to validate.</param>
    /// <param name="sectionKey">The configuration section key for context.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The validation result containing errors and warnings.</returns>
    Task<ConfigurationValidationResult> ValidateAsync<T>(
        T configurationObject, 
        string sectionKey, 
        CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Validates a configuration section by its key.
    /// </summary>
    /// <param name="configuration">The configuration root.</param>
    /// <param name="sectionKey">The section key to validate.</param>
    /// <param name="expectedType">The expected type for the section.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The validation result containing errors and warnings.</returns>
    Task<ConfigurationValidationResult> ValidateSectionAsync(
        Microsoft.Extensions.Configuration.IConfiguration configuration,
        string sectionKey,
        Type expectedType,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Registers a custom validation rule for a specific configuration type.
    /// </summary>
    /// <typeparam name="T">The configuration type.</typeparam>
    /// <param name="validationRule">The validation rule function.</param>
    void RegisterValidationRule<T>(Func<T, ValidationResult?> validationRule) where T : class;
    
    /// <summary>
    /// Registers a schema for a configuration type using JSON Schema.
    /// </summary>
    /// <typeparam name="T">The configuration type.</typeparam>
    /// <param name="jsonSchema">The JSON schema string.</param>
    void RegisterSchema<T>(string jsonSchema) where T : class;
}