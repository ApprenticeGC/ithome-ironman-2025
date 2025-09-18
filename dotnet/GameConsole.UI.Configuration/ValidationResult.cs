namespace GameConsole.UI.Configuration;

/// <summary>
/// Represents the result of a configuration validation operation.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the validation was successful.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets the collection of validation errors, if any.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; init; } = [];

    /// <summary>
    /// Gets the collection of validation warnings, if any.
    /// </summary>
    public IReadOnlyList<ValidationWarning> Warnings { get; init; } = [];

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A validation result indicating success.</returns>
    public static ValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with errors.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <returns>A validation result indicating failure.</returns>
    public static ValidationResult Failure(params ValidationError[] errors) => 
        new() { IsValid = false, Errors = errors };

    /// <summary>
    /// Creates a validation result with warnings but success.
    /// </summary>
    /// <param name="warnings">The validation warnings.</param>
    /// <returns>A validation result indicating success with warnings.</returns>
    public static ValidationResult SuccessWithWarnings(params ValidationWarning[] warnings) => 
        new() { IsValid = true, Warnings = warnings };
}

/// <summary>
/// Represents a validation error with details about what failed.
/// </summary>
public record ValidationError
{
    /// <summary>
    /// Gets the property or configuration key that failed validation.
    /// </summary>
    public required string Property { get; init; }

    /// <summary>
    /// Gets the error message describing what failed.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the error code for programmatic handling.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Gets the current value that failed validation.
    /// </summary>
    public object? CurrentValue { get; init; }

    /// <summary>
    /// Gets suggested values or actions to fix the error.
    /// </summary>
    public string? Suggestion { get; init; }
}

/// <summary>
/// Represents a validation warning with details about potential issues.
/// </summary>
public record ValidationWarning
{
    /// <summary>
    /// Gets the property or configuration key that generated the warning.
    /// </summary>
    public required string Property { get; init; }

    /// <summary>
    /// Gets the warning message describing the potential issue.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the warning code for programmatic handling.
    /// </summary>
    public string? WarningCode { get; init; }

    /// <summary>
    /// Gets the current value that generated the warning.
    /// </summary>
    public object? CurrentValue { get; init; }
}