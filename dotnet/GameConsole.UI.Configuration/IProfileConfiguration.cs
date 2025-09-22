using Microsoft.Extensions.Configuration;

namespace GameConsole.UI.Configuration;

/// <summary>
/// Defines the interface for UI profile configuration settings.
/// Provides access to configuration data, validation, and inheritance capabilities.
/// </summary>
public interface IProfileConfiguration
{
    /// <summary>
    /// Gets the unique identifier for this configuration profile.
    /// </summary>
    string ProfileId { get; }

    /// <summary>
    /// Gets the display name for this configuration profile.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of this configuration profile.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the version of this configuration profile.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets the configuration scope defining where this profile applies.
    /// </summary>
    ProfileScope Scope { get; }

    /// <summary>
    /// Gets the environment this configuration is intended for.
    /// </summary>
    string Environment { get; }

    /// <summary>
    /// Gets the identifier of the parent profile this configuration inherits from.
    /// </summary>
    string? ParentProfileId { get; }

    /// <summary>
    /// Gets the underlying configuration object containing all settings.
    /// </summary>
    IConfiguration Configuration { get; }

    /// <summary>
    /// Gets additional metadata associated with this profile.
    /// </summary>
    IReadOnlyDictionary<string, object> Metadata { get; }

    /// <summary>
    /// Gets a configuration value by key with optional type conversion.
    /// </summary>
    /// <typeparam name="T">The type to convert the value to.</typeparam>
    /// <param name="key">The configuration key.</param>
    /// <param name="defaultValue">The default value if the key is not found.</param>
    /// <returns>The configuration value or default value.</returns>
    T GetValue<T>(string key, T defaultValue = default!);

    /// <summary>
    /// Checks if a configuration key exists in this profile.
    /// </summary>
    /// <param name="key">The configuration key to check.</param>
    /// <returns>True if the key exists, otherwise false.</returns>
    bool HasKey(string key);

    /// <summary>
    /// Validates this configuration profile.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Validation results indicating whether the configuration is valid.</returns>
    Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the scope where a profile configuration applies.
/// </summary>
public enum ProfileScope
{
    /// <summary>
    /// Profile applies globally across the entire application.
    /// </summary>
    Global,

    /// <summary>
    /// Profile applies to a specific mode (e.g., Game or Editor).
    /// </summary>
    Mode,

    /// <summary>
    /// Profile applies to a specific user context.
    /// </summary>
    User,

    /// <summary>
    /// Profile applies to a specific environment (e.g., Development, Production).
    /// </summary>
    Environment,

    /// <summary>
    /// Profile applies to a specific plugin or component.
    /// </summary>
    Component
}

/// <summary>
/// Represents the result of configuration validation.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the validation was successful.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets the collection of validation errors.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; init; } = new List<ValidationError>();

    /// <summary>
    /// Gets the collection of validation warnings.
    /// </summary>
    public IReadOnlyList<ValidationWarning> Warnings { get; init; } = new List<ValidationWarning>();

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with errors.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    public static ValidationResult Failure(params ValidationError[] errors) => new()
    {
        IsValid = false,
        Errors = errors
    };
}

/// <summary>
/// Represents a validation error in configuration.
/// </summary>
public record ValidationError(string Key, string Message, string? Details = null);

/// <summary>
/// Represents a validation warning in configuration.
/// </summary>
public record ValidationWarning(string Key, string Message, string? Details = null);