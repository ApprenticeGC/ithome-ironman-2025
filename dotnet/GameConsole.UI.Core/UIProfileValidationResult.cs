namespace GameConsole.UI.Core;

/// <summary>
/// Represents the result of UI profile validation.
/// </summary>
public class UIProfileValidationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the validation was successful.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the validation errors if any.
    /// </summary>
    public List<string> Errors { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the validation warnings if any.
    /// </summary>
    public List<string> Warnings { get; set; } = new List<string>();

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A validation result indicating success.</returns>
    public static UIProfileValidationResult Success()
    {
        return new UIProfileValidationResult { IsValid = true };
    }

    /// <summary>
    /// Creates a failed validation result with errors.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <returns>A validation result indicating failure.</returns>
    public static UIProfileValidationResult Failure(params string[] errors)
    {
        return new UIProfileValidationResult 
        { 
            IsValid = false, 
            Errors = errors.ToList() 
        };
    }

    /// <summary>
    /// Creates a successful validation result with warnings.
    /// </summary>
    /// <param name="warnings">The validation warnings.</param>
    /// <returns>A validation result indicating success with warnings.</returns>
    public static UIProfileValidationResult SuccessWithWarnings(params string[] warnings)
    {
        return new UIProfileValidationResult 
        { 
            IsValid = true, 
            Warnings = warnings.ToList() 
        };
    }
}