namespace GameConsole.UI.Configuration;

/// <summary>
/// Represents the result of a configuration validation operation.
/// </summary>
public sealed class ValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the validation was successful.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets the collection of validation errors, if any.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the collection of validation warnings, if any.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets additional validation context or metadata.
    /// </summary>
    public IReadOnlyDictionary<string, object> Context { get; init; } = new Dictionary<string, object>();

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A validation result indicating success.</returns>
    public static ValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a successful validation result with warnings.
    /// </summary>
    /// <param name="warnings">The warnings to include.</param>
    /// <returns>A validation result indicating success with warnings.</returns>
    public static ValidationResult SuccessWithWarnings(params string[] warnings) =>
        new() { IsValid = true, Warnings = warnings.ToList().AsReadOnly() };

    /// <summary>
    /// Creates a failed validation result with errors.
    /// </summary>
    /// <param name="errors">The errors that caused validation to fail.</param>
    /// <returns>A validation result indicating failure.</returns>
    public static ValidationResult Failure(params string[] errors) =>
        new() { IsValid = false, Errors = errors.ToList().AsReadOnly() };

    /// <summary>
    /// Creates a failed validation result with errors and warnings.
    /// </summary>
    /// <param name="errors">The errors that caused validation to fail.</param>
    /// <param name="warnings">Additional warnings to include.</param>
    /// <returns>A validation result indicating failure.</returns>
    public static ValidationResult Failure(string[] errors, string[] warnings) =>
        new() 
        { 
            IsValid = false, 
            Errors = errors.ToList().AsReadOnly(),
            Warnings = warnings.ToList().AsReadOnly()
        };
}