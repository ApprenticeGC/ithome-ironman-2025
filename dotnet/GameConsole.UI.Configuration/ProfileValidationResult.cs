namespace GameConsole.UI.Configuration;

/// <summary>
/// Represents the result of a UI profile validation.
/// </summary>
public class ProfileValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the profile is valid.
    /// </summary>
    public bool IsValid { get; }
    
    /// <summary>
    /// Gets the validation errors, if any.
    /// </summary>
    public IReadOnlyList<string> Errors { get; }
    
    /// <summary>
    /// Gets the validation warnings, if any.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; }
    
    public ProfileValidationResult(bool isValid, IEnumerable<string> errors, IEnumerable<string> warnings)
    {
        IsValid = isValid;
        Errors = errors.ToList().AsReadOnly();
        Warnings = warnings.ToList().AsReadOnly();
    }
    
    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <param name="warnings">Optional warnings.</param>
    /// <returns>A valid result.</returns>
    public static ProfileValidationResult Success(IEnumerable<string>? warnings = null)
    {
        return new ProfileValidationResult(true, Enumerable.Empty<string>(), warnings ?? Enumerable.Empty<string>());
    }
    
    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <param name="warnings">Optional warnings.</param>
    /// <returns>An invalid result.</returns>
    public static ProfileValidationResult Failure(IEnumerable<string> errors, IEnumerable<string>? warnings = null)
    {
        return new ProfileValidationResult(false, errors, warnings ?? Enumerable.Empty<string>());
    }
}