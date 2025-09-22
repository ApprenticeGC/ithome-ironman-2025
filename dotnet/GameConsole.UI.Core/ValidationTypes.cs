using System;
using System.Collections.Generic;
using System.Linq;

namespace GameConsole.UI.Core
{
    /// <summary>
    /// Represents the result of profile validation operations.
    /// </summary>
    public class ProfileValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the profile is valid.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets the validation errors found in the profile.
    /// </summary>
    public IReadOnlyCollection<string> Errors { get; }

    /// <summary>
    /// Gets the validation warnings found in the profile.
    /// </summary>
    public IReadOnlyCollection<string> Warnings { get; }

    /// <summary>
    /// Initializes a new instance of the ProfileValidationResult class for a valid profile.
    /// </summary>
    public ProfileValidationResult()
    {
        IsValid = true;
        Errors = Array.Empty<string>();
        Warnings = Array.Empty<string>();
    }

    /// <summary>
    /// Initializes a new instance of the ProfileValidationResult class with validation results.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <param name="warnings">The validation warnings.</param>
    public ProfileValidationResult(IEnumerable<string> errors, IEnumerable<string> warnings = null)
    {
        Errors = errors?.ToArray() ?? Array.Empty<string>();
        Warnings = warnings?.ToArray() ?? Array.Empty<string>();
        IsValid = !Errors.Any();
    }
}

/// <summary>
/// Describes the schema for a configuration option.
/// </summary>
public class ConfigurationOptionSchema
{
    /// <summary>
    /// Gets the name of the configuration option.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the description of what this configuration option controls.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the type of value expected for this configuration option.
    /// </summary>
    public Type ValueType { get; }

    /// <summary>
    /// Gets the default value for this configuration option.
    /// </summary>
    public object DefaultValue { get; }

    /// <summary>
    /// Gets a value indicating whether this configuration option is required.
    /// </summary>
    public bool IsRequired { get; }

    /// <summary>
    /// Gets the possible values for this configuration option (if applicable).
    /// </summary>
    public IReadOnlyCollection<object> PossibleValues { get; }

    public ConfigurationOptionSchema(
        string name, 
        string description, 
        Type valueType, 
        object defaultValue = null, 
        bool isRequired = false,
        IEnumerable<object> possibleValues = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        ValueType = valueType ?? throw new ArgumentNullException(nameof(valueType));
        DefaultValue = defaultValue;
        IsRequired = isRequired;
        PossibleValues = possibleValues?.ToArray();
    }
    }
}