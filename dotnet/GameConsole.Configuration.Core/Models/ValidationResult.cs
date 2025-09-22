namespace GameConsole.Configuration.Core.Models;

/// <summary>
/// Represents the result of configuration validation.
/// </summary>
public sealed class ValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the validation was successful.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets the list of validation errors, if any.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; }

    /// <summary>
    /// Gets the list of validation warnings, if any.
    /// </summary>
    public IReadOnlyList<ValidationWarning> Warnings { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationResult"/> class.
    /// </summary>
    /// <param name="isValid">Whether the validation was successful.</param>
    /// <param name="errors">The validation errors.</param>
    /// <param name="warnings">The validation warnings.</param>
    public ValidationResult(bool isValid, IReadOnlyList<ValidationError>? errors = null, IReadOnlyList<ValidationWarning>? warnings = null)
    {
        IsValid = isValid;
        Errors = errors ?? Array.Empty<ValidationError>();
        Warnings = warnings ?? Array.Empty<ValidationWarning>();
    }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ValidationResult Success(IReadOnlyList<ValidationWarning>? warnings = null) =>
        new(true, null, warnings);

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    public static ValidationResult Failure(IReadOnlyList<ValidationError> errors, IReadOnlyList<ValidationWarning>? warnings = null) =>
        new(false, errors, warnings);
}

/// <summary>
/// Represents a configuration validation error.
/// </summary>
public sealed class ValidationError
{
    /// <summary>
    /// Gets the configuration path where the error occurred.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the severity of the error.
    /// </summary>
    public ValidationSeverity Severity { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationError"/> class.
    /// </summary>
    public ValidationError(string path, string message, ValidationSeverity severity = ValidationSeverity.Error)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Severity = severity;
    }
}

/// <summary>
/// Represents a configuration validation warning.
/// </summary>
public sealed class ValidationWarning
{
    /// <summary>
    /// Gets the configuration path where the warning occurred.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the warning message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationWarning"/> class.
    /// </summary>
    public ValidationWarning(string path, string message)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Message = message ?? throw new ArgumentNullException(nameof(message));
    }
}

/// <summary>
/// Defines the severity level of validation issues.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// Informational message.
    /// </summary>
    Info,

    /// <summary>
    /// Warning that doesn't prevent operation.
    /// </summary>
    Warning,

    /// <summary>
    /// Error that prevents proper operation.
    /// </summary>
    Error,

    /// <summary>
    /// Critical error that requires immediate attention.
    /// </summary>
    Critical
}