using Microsoft.Extensions.Logging;

namespace GameConsole.UI.Configuration;

/// <summary>
/// Validates UI profile configurations for completeness and compatibility.
/// Supports extensible validation rules and schema validation.
/// </summary>
public class ConfigurationValidator
{
    private readonly ILogger<ConfigurationValidator>? _logger;
    private readonly List<IValidationRule> _rules = new();

    public ConfigurationValidator(ILogger<ConfigurationValidator>? logger = null)
    {
        _logger = logger;
        RegisterDefaultRules();
    }

    /// <summary>
    /// Adds a custom validation rule.
    /// </summary>
    /// <param name="rule">The validation rule to add.</param>
    public void AddRule(IValidationRule rule)
    {
        _rules.Add(rule ?? throw new ArgumentNullException(nameof(rule)));
        _logger?.LogDebug("Added validation rule: {RuleName}", rule.GetType().Name);
    }

    /// <summary>
    /// Removes a validation rule of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of validation rule to remove.</typeparam>
    public void RemoveRule<T>() where T : IValidationRule
    {
        var removed = _rules.RemoveAll(r => r is T);
        if (removed > 0)
        {
            _logger?.LogDebug("Removed {Count} validation rules of type {RuleType}", removed, typeof(T).Name);
        }
    }

    /// <summary>
    /// Validates a profile configuration against all registered rules.
    /// </summary>
    /// <param name="configuration">The configuration to validate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Validation result with errors and warnings.</returns>
    public async Task<ValidationResult> ValidateAsync(IProfileConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        var errors = new List<ValidationError>();
        var warnings = new List<ValidationWarning>();

        _logger?.LogDebug("Validating configuration for profile {ProfileId}", configuration.ProfileId);

        foreach (var rule in _rules)
        {
            try
            {
                var result = await rule.ValidateAsync(configuration, cancellationToken);
                errors.AddRange(result.Errors);
                warnings.AddRange(result.Warnings);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error executing validation rule {RuleName}", rule.GetType().Name);
                errors.Add(new ValidationError("ValidationRule", $"Validation rule {rule.GetType().Name} failed", ex.Message));
            }
        }

        var isValid = errors.Count == 0;
        
        _logger?.LogDebug("Validation completed for profile {ProfileId}: {IsValid} ({ErrorCount} errors, {WarningCount} warnings)",
            configuration.ProfileId, isValid, errors.Count, warnings.Count);

        return new ValidationResult
        {
            IsValid = isValid,
            Errors = errors,
            Warnings = warnings
        };
    }

    /// <summary>
    /// Validates multiple configurations in batch.
    /// </summary>
    /// <param name="configurations">The configurations to validate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Dictionary of validation results keyed by profile ID.</returns>
    public async Task<Dictionary<string, ValidationResult>> ValidateBatchAsync(
        IEnumerable<IProfileConfiguration> configurations, 
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, ValidationResult>();

        var tasks = configurations.Select(async config =>
        {
            var result = await ValidateAsync(config, cancellationToken);
            return new { ProfileId = config.ProfileId, Result = result };
        });

        var completedTasks = await Task.WhenAll(tasks);

        foreach (var task in completedTasks)
        {
            results[task.ProfileId] = task.Result;
        }

        return results;
    }

    private void RegisterDefaultRules()
    {
        _rules.Add(new RequiredFieldsValidationRule());
        _rules.Add(new ProfileMetadataValidationRule());
        _rules.Add(new UISettingsValidationRule());
        _rules.Add(new VersionValidationRule());
        _rules.Add(new EnvironmentValidationRule());
    }
}

/// <summary>
/// Interface for profile configuration validation rules.
/// </summary>
public interface IValidationRule
{
    /// <summary>
    /// Validates a profile configuration.
    /// </summary>
    /// <param name="configuration">The configuration to validate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Validation result with errors and warnings.</returns>
    Task<ValidationResult> ValidateAsync(IProfileConfiguration configuration, CancellationToken cancellationToken = default);
}

/// <summary>
/// Validates that required fields are present in the configuration.
/// </summary>
internal class RequiredFieldsValidationRule : IValidationRule
{
    public Task<ValidationResult> ValidateAsync(IProfileConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(configuration.ProfileId))
            errors.Add(new ValidationError(nameof(configuration.ProfileId), "Profile ID is required"));

        if (string.IsNullOrWhiteSpace(configuration.Name))
            errors.Add(new ValidationError(nameof(configuration.Name), "Profile name is required"));

        if (string.IsNullOrWhiteSpace(configuration.Version))
            errors.Add(new ValidationError(nameof(configuration.Version), "Profile version is required"));

        return Task.FromResult(new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        });
    }
}

/// <summary>
/// Validates profile metadata for consistency and completeness.
/// </summary>
internal class ProfileMetadataValidationRule : IValidationRule
{
    public Task<ValidationResult> ValidateAsync(IProfileConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var warnings = new List<ValidationWarning>();

        if (string.IsNullOrWhiteSpace(configuration.Description))
            warnings.Add(new ValidationWarning(nameof(configuration.Description), "Profile description is recommended"));

        // Check for common metadata fields
        var recommendedMetadataFields = new[] { "Author", "Created", "Category", "Tags" };
        foreach (var field in recommendedMetadataFields)
        {
            if (!configuration.Metadata.ContainsKey(field))
                warnings.Add(new ValidationWarning($"Metadata.{field}", $"Metadata field '{field}' is recommended"));
        }

        return Task.FromResult(new ValidationResult
        {
            IsValid = true,
            Warnings = warnings
        });
    }
}

/// <summary>
/// Validates UI-specific configuration settings.
/// </summary>
internal class UISettingsValidationRule : IValidationRule
{
    public Task<ValidationResult> ValidateAsync(IProfileConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var errors = new List<ValidationError>();
        var warnings = new List<ValidationWarning>();

        // Validate window dimensions if present
        if (configuration.HasKey("UI:Window:Width"))
        {
            var width = configuration.GetValue<int>("UI:Window:Width");
            if (width <= 0)
                errors.Add(new ValidationError("UI:Window:Width", "Window width must be positive"));
            else if (width < 800)
                warnings.Add(new ValidationWarning("UI:Window:Width", "Window width less than 800px may cause usability issues"));
        }

        if (configuration.HasKey("UI:Window:Height"))
        {
            var height = configuration.GetValue<int>("UI:Window:Height");
            if (height <= 0)
                errors.Add(new ValidationError("UI:Window:Height", "Window height must be positive"));
            else if (height < 600)
                warnings.Add(new ValidationWarning("UI:Window:Height", "Window height less than 600px may cause usability issues"));
        }

        // Validate theme if present
        if (configuration.HasKey("UI:Theme"))
        {
            var theme = configuration.GetValue<string>("UI:Theme");
            if (string.IsNullOrWhiteSpace(theme))
                errors.Add(new ValidationError("UI:Theme", "UI theme cannot be empty if specified"));
        }

        return Task.FromResult(new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        });
    }
}

/// <summary>
/// Validates version format and semantics.
/// </summary>
internal class VersionValidationRule : IValidationRule
{
    public Task<ValidationResult> ValidateAsync(IProfileConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var errors = new List<ValidationError>();

        if (!Version.TryParse(configuration.Version, out var version))
        {
            errors.Add(new ValidationError(nameof(configuration.Version), 
                "Version must be in valid semantic version format (e.g., '1.0.0')"));
        }
        else if (version.Major == 0 && version.Minor == 0 && version.Build == 0)
        {
            errors.Add(new ValidationError(nameof(configuration.Version), "Version cannot be 0.0.0"));
        }

        return Task.FromResult(new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        });
    }
}

/// <summary>
/// Validates environment-specific settings.
/// </summary>
internal class EnvironmentValidationRule : IValidationRule
{
    private static readonly HashSet<string> ValidEnvironments = new(StringComparer.OrdinalIgnoreCase)
    {
        "Development", "Testing", "Staging", "Production", "Default"
    };

    public Task<ValidationResult> ValidateAsync(IProfileConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var warnings = new List<ValidationWarning>();

        if (!ValidEnvironments.Contains(configuration.Environment))
        {
            warnings.Add(new ValidationWarning(nameof(configuration.Environment), 
                $"Environment '{configuration.Environment}' is not a standard environment name"));
        }

        return Task.FromResult(new ValidationResult
        {
            IsValid = true,
            Warnings = warnings
        });
    }
}