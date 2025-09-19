namespace GameConsole.UI.Configuration;

/// <summary>
/// Validates profile configurations for completeness and compatibility.
/// </summary>
public interface IConfigurationValidator
{
    /// <summary>
    /// Validates a profile configuration asynchronously.
    /// </summary>
    /// <param name="configuration">The configuration to validate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A validation result indicating success or failure with details.</returns>
    Task<ValidationResult> ValidateAsync(IProfileConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates configuration compatibility with a parent profile.
    /// </summary>
    /// <param name="childConfiguration">The child configuration to validate.</param>
    /// <param name="parentConfiguration">The parent configuration to validate against.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A validation result indicating compatibility.</returns>
    Task<ValidationResult> ValidateCompatibilityAsync(
        IProfileConfiguration childConfiguration, 
        IProfileConfiguration parentConfiguration, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates configuration schema against expected structure.
    /// </summary>
    /// <param name="configuration">The configuration to validate.</param>
    /// <param name="schemaVersion">The expected schema version.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A validation result indicating schema compliance.</returns>
    Task<ValidationResult> ValidateSchemaAsync(
        IProfileConfiguration configuration, 
        Version schemaVersion, 
        CancellationToken cancellationToken = default);
}