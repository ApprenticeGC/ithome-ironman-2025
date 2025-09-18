namespace GameConsole.UI.Configuration;

/// <summary>
/// Validates profile configurations for completeness, compatibility, and correctness.
/// Provides comprehensive validation with extensible rule-based checking.
/// </summary>
public class ConfigurationValidator
{
    private readonly List<IValidationRule> _rules = [];
    private readonly Dictionary<string, object?> _validationContext = [];

    /// <summary>
    /// Initializes a new instance of the ConfigurationValidator with default rules.
    /// </summary>
    public ConfigurationValidator()
    {
        // Register default validation rules
        AddRule(new RequiredPropertiesRule());
        AddRule(new VersionValidationRule());
        AddRule(new SettingsTypeValidationRule());
        AddRule(new ScopeCompatibilityRule());
    }

    /// <summary>
    /// Adds a custom validation rule.
    /// </summary>
    /// <param name="rule">The validation rule to add.</param>
    public void AddRule(IValidationRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        _rules.Add(rule);
    }

    /// <summary>
    /// Removes a validation rule by type.
    /// </summary>
    /// <typeparam name="T">The type of rule to remove.</typeparam>
    /// <returns>True if a rule was removed, false otherwise.</returns>
    public bool RemoveRule<T>() where T : IValidationRule
    {
        var rule = _rules.OfType<T>().FirstOrDefault();
        return rule != null && _rules.Remove(rule);
    }

    /// <summary>
    /// Sets a context value for validation rules to use.
    /// </summary>
    /// <param name="key">The context key.</param>
    /// <param name="value">The context value.</param>
    public void SetContext(string key, object? value)
    {
        _validationContext[key] = value;
    }

    /// <summary>
    /// Validates a profile configuration against all registered rules.
    /// </summary>
    /// <param name="configuration">The configuration to validate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A comprehensive validation result.</returns>
    public async Task<ValidationResult> ValidateAsync(IProfileConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var errors = new List<ValidationError>();
        var warnings = new List<ValidationWarning>();

        var context = new ValidationContext
        {
            Configuration = configuration,
            GlobalContext = _validationContext.AsReadOnly()
        };

        foreach (var rule in _rules)
        {
            try
            {
                var result = await rule.ValidateAsync(context, cancellationToken);
                errors.AddRange(result.Errors);
                warnings.AddRange(result.Warnings);
            }
            catch (Exception ex)
            {
                errors.Add(new ValidationError
                {
                    Property = "ValidationRule",
                    Message = $"Validation rule '{rule.GetType().Name}' threw an exception: {ex.Message}",
                    ErrorCode = "RULE_EXCEPTION"
                });
            }
        }

        return errors.Count > 0
            ? ValidationResult.Failure(errors.ToArray())
            : warnings.Count > 0
                ? ValidationResult.SuccessWithWarnings(warnings.ToArray())
                : ValidationResult.Success();
    }

    /// <summary>
    /// Validates multiple configurations in batch.
    /// </summary>
    /// <param name="configurations">The configurations to validate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A dictionary mapping configuration IDs to validation results.</returns>
    public async Task<IReadOnlyDictionary<string, ValidationResult>> ValidateBatchAsync(
        IEnumerable<IProfileConfiguration> configurations,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, ValidationResult>();
        
        foreach (var config in configurations)
        {
            results[config.Id] = await ValidateAsync(config, cancellationToken);
        }

        return results;
    }

    /// <summary>
    /// Validates configuration compatibility between profiles.
    /// </summary>
    /// <param name="configurations">The configurations to check for compatibility.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A validation result indicating compatibility issues.</returns>
    public async Task<ValidationResult> ValidateCompatibilityAsync(
        IEnumerable<IProfileConfiguration> configurations,
        CancellationToken cancellationToken = default)
    {
        var configList = configurations.ToList();
        var errors = new List<ValidationError>();
        var warnings = new List<ValidationWarning>();

        // Check scope compatibility
        var scopeGroups = configList.GroupBy(c => c.Scope).ToList();
        if (scopeGroups.Count > 1)
        {
            warnings.Add(new ValidationWarning
            {
                Property = "Scope",
                Message = $"Multiple scopes detected: {string.Join(", ", scopeGroups.Select(g => g.Key))}",
                WarningCode = "MIXED_SCOPES"
            });
        }

        // Check environment compatibility
        var environments = configList.Select(c => c.Environment).Distinct().ToList();
        if (environments.Count > 1)
        {
            warnings.Add(new ValidationWarning
            {
                Property = "Environment",
                Message = $"Multiple environments detected: {string.Join(", ", environments)}",
                WarningCode = "MIXED_ENVIRONMENTS"
            });
        }

        // Check version compatibility
        var versions = configList.Select(c => c.Version).Distinct().ToList();
        if (versions.Count > 1)
        {
            var maxVersion = versions.Max();
            var minVersion = versions.Min();
            
            if (maxVersion?.Major != minVersion?.Major)
            {
                errors.Add(new ValidationError
                {
                    Property = "Version",
                    Message = $"Incompatible major versions: {minVersion} and {maxVersion}",
                    ErrorCode = "VERSION_INCOMPATIBILITY"
                });
            }
        }

        await Task.CompletedTask; // Placeholder for async operations

        return errors.Count > 0
            ? ValidationResult.Failure(errors.ToArray())
            : warnings.Count > 0
                ? ValidationResult.SuccessWithWarnings(warnings.ToArray())
                : ValidationResult.Success();
    }
}

/// <summary>
/// Interface for validation rules that can be applied to profile configurations.
/// </summary>
public interface IValidationRule
{
    /// <summary>
    /// Validates a configuration within the given context.
    /// </summary>
    /// <param name="context">The validation context.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A validation result.</returns>
    Task<ValidationResult> ValidateAsync(ValidationContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Context information for validation rules.
/// </summary>
public class ValidationContext
{
    /// <summary>
    /// Gets the configuration being validated.
    /// </summary>
    public required IProfileConfiguration Configuration { get; init; }

    /// <summary>
    /// Gets global context values set by the validator.
    /// </summary>
    public IReadOnlyDictionary<string, object?> GlobalContext { get; init; } = new Dictionary<string, object?>();
}

/// <summary>
/// Validates that required properties are present and valid.
/// </summary>
internal class RequiredPropertiesRule : IValidationRule
{
    public Task<ValidationResult> ValidateAsync(ValidationContext context, CancellationToken cancellationToken = default)
    {
        var config = context.Configuration;
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(config.Id))
            errors.Add(new ValidationError { Property = nameof(config.Id), Message = "Configuration ID is required." });

        if (string.IsNullOrWhiteSpace(config.Name))
            errors.Add(new ValidationError { Property = nameof(config.Name), Message = "Configuration name is required." });

        if (string.IsNullOrWhiteSpace(config.Environment))
            errors.Add(new ValidationError { Property = nameof(config.Environment), Message = "Environment is required." });

        return Task.FromResult(errors.Count > 0 ? ValidationResult.Failure(errors.ToArray()) : ValidationResult.Success());
    }
}

/// <summary>
/// Validates version information.
/// </summary>
internal class VersionValidationRule : IValidationRule
{
    public Task<ValidationResult> ValidateAsync(ValidationContext context, CancellationToken cancellationToken = default)
    {
        var config = context.Configuration;
        var warnings = new List<ValidationWarning>();

        if (config.Version.Major < 1)
            warnings.Add(new ValidationWarning { Property = nameof(config.Version), Message = "Major version should be at least 1." });

        if (config.Version.Minor < 0)
            warnings.Add(new ValidationWarning { Property = nameof(config.Version), Message = "Minor version should not be negative." });

        return Task.FromResult(warnings.Count > 0 ? ValidationResult.SuccessWithWarnings(warnings.ToArray()) : ValidationResult.Success());
    }
}

/// <summary>
/// Validates settings types and values.
/// </summary>
internal class SettingsTypeValidationRule : IValidationRule
{
    public Task<ValidationResult> ValidateAsync(ValidationContext context, CancellationToken cancellationToken = default)
    {
        var config = context.Configuration;
        var warnings = new List<ValidationWarning>();

        foreach (var (key, value) in config.Settings)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                warnings.Add(new ValidationWarning
                {
                    Property = "Settings",
                    Message = "Setting key should not be empty or whitespace.",
                    CurrentValue = key
                });
            }

            // Warn about potentially problematic types
            if (value?.GetType().IsAssignableTo(typeof(System.Collections.IEnumerable)) == true && value is not string)
            {
                warnings.Add(new ValidationWarning
                {
                    Property = key,
                    Message = "Complex collection types may not serialize properly.",
                    CurrentValue = value
                });
            }
        }

        return Task.FromResult(warnings.Count > 0 ? ValidationResult.SuccessWithWarnings(warnings.ToArray()) : ValidationResult.Success());
    }
}

/// <summary>
/// Validates scope compatibility and appropriateness.
/// </summary>
internal class ScopeCompatibilityRule : IValidationRule
{
    public Task<ValidationResult> ValidateAsync(ValidationContext context, CancellationToken cancellationToken = default)
    {
        var config = context.Configuration;
        var warnings = new List<ValidationWarning>();

        // Check if scope matches environment expectations
        if (config.Scope == ConfigurationScope.Global && config.Environment != "Default")
        {
            warnings.Add(new ValidationWarning
            {
                Property = nameof(config.Scope),
                Message = "Global scope configurations should typically use 'Default' environment.",
                CurrentValue = $"Scope: {config.Scope}, Environment: {config.Environment}"
            });
        }

        if (config.Scope == ConfigurationScope.Environment && config.Environment == "Default")
        {
            warnings.Add(new ValidationWarning
            {
                Property = nameof(config.Scope),
                Message = "Environment scope configurations should specify a specific environment.",
                CurrentValue = $"Scope: {config.Scope}, Environment: {config.Environment}"
            });
        }

        return Task.FromResult(warnings.Count > 0 ? ValidationResult.SuccessWithWarnings(warnings.ToArray()) : ValidationResult.Success());
    }
}