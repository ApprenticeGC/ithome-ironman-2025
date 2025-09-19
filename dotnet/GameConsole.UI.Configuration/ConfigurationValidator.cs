namespace GameConsole.UI.Configuration;

/// <summary>
/// Default implementation of IConfigurationValidator for profile validation.
/// </summary>
public sealed class ConfigurationValidator : IConfigurationValidator
{
    /// <inheritdoc />
    public async Task<ValidationResult> ValidateAsync(IProfileConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (configuration == null)
            return ValidationResult.Failure("Configuration cannot be null.");

        var errors = new List<string>();
        var warnings = new List<string>();

        // Validate basic properties
        if (string.IsNullOrWhiteSpace(configuration.Id))
            errors.Add("Configuration ID is required.");

        if (string.IsNullOrWhiteSpace(configuration.Name))
            errors.Add("Configuration name is required.");

        if (string.IsNullOrWhiteSpace(configuration.Description))
            errors.Add("Configuration description is required.");

        if (configuration.Version == null)
            errors.Add("Configuration version is required.");

        // Validate environment
        var validEnvironments = new[] { "Development", "Staging", "Production", "Test" };
        if (!string.IsNullOrWhiteSpace(configuration.Environment) && 
            !validEnvironments.Contains(configuration.Environment))
        {
            warnings.Add($"Environment '{configuration.Environment}' is not standard. Consider using: {string.Join(", ", validEnvironments)}");
        }

        // Validate configuration structure
        if (configuration.Configuration == null)
        {
            errors.Add("Configuration data cannot be null.");
        }
        else
        {
            // Check for required sections (can be customized based on requirements)
            var requiredSections = new[] { "UI", "Commands", "Layout" };
            foreach (var section in requiredSections)
            {
                var configSection = configuration.Configuration.GetSection(section);
                if (!SectionExists(configSection))
                {
                    warnings.Add($"Recommended section '{section}' is missing from configuration.");
                }
            }
        }

        // Validate metadata
        if (configuration.Metadata == null)
        {
            warnings.Add("Configuration metadata is empty. Consider adding creation time, author, or version information.");
        }
        else if (!configuration.Metadata.ContainsKey("CreatedAt"))
        {
            warnings.Add("Configuration metadata should include 'CreatedAt' timestamp.");
        }

        // Run the configuration's own validation
        var selfValidation = await configuration.ValidateAsync(cancellationToken);
        if (!selfValidation.IsValid)
        {
            errors.AddRange(selfValidation.Errors);
        }
        warnings.AddRange(selfValidation.Warnings);

        return errors.Count > 0
            ? ValidationResult.Failure(errors.ToArray(), warnings.ToArray())
            : warnings.Count > 0
                ? ValidationResult.SuccessWithWarnings(warnings.ToArray())
                : ValidationResult.Success();
    }

    /// <inheritdoc />
    public async Task<ValidationResult> ValidateCompatibilityAsync(
        IProfileConfiguration childConfiguration, 
        IProfileConfiguration parentConfiguration, 
        CancellationToken cancellationToken = default)
    {
        if (childConfiguration == null)
            return ValidationResult.Failure("Child configuration cannot be null.");
        
        if (parentConfiguration == null)
            return ValidationResult.Failure("Parent configuration cannot be null.");

        var errors = new List<string>();
        var warnings = new List<string>();

        // Version compatibility check
        if (childConfiguration.Version < parentConfiguration.Version)
        {
            warnings.Add($"Child configuration version ({childConfiguration.Version}) is older than parent version ({parentConfiguration.Version}). This may cause compatibility issues.");
        }

        // Environment compatibility check
        if (childConfiguration.Environment != parentConfiguration.Environment)
        {
            warnings.Add($"Child environment ({childConfiguration.Environment}) differs from parent environment ({parentConfiguration.Environment}). Ensure this is intentional.");
        }

        // Check for conflicting settings
        var parentKeys = GetConfigurationKeys(parentConfiguration.Configuration);
        var childKeys = GetConfigurationKeys(childConfiguration.Configuration);
        
        var conflictingKeys = parentKeys.Intersect(childKeys).ToList();
        if (conflictingKeys.Count > 0)
        {
            warnings.Add($"Configuration keys will be overridden by child: {string.Join(", ", conflictingKeys.Take(5))}{(conflictingKeys.Count > 5 ? "..." : "")}");
        }

        // Simulate async work
        await Task.Delay(1, cancellationToken);

        return errors.Count > 0
            ? ValidationResult.Failure(errors.ToArray(), warnings.ToArray())
            : warnings.Count > 0
                ? ValidationResult.SuccessWithWarnings(warnings.ToArray())
                : ValidationResult.Success();
    }

    /// <inheritdoc />
    public async Task<ValidationResult> ValidateSchemaAsync(
        IProfileConfiguration configuration, 
        Version schemaVersion, 
        CancellationToken cancellationToken = default)
    {
        if (configuration == null)
            return ValidationResult.Failure("Configuration cannot be null.");
        
        if (schemaVersion == null)
            return ValidationResult.Failure("Schema version cannot be null.");

        var errors = new List<string>();
        var warnings = new List<string>();

        // Version compatibility
        if (configuration.Version.Major != schemaVersion.Major)
        {
            errors.Add($"Configuration version ({configuration.Version}) is incompatible with schema version ({schemaVersion}). Major versions must match.");
        }
        else if (configuration.Version < schemaVersion)
        {
            warnings.Add($"Configuration version ({configuration.Version}) is older than current schema version ({schemaVersion}). Consider upgrading.");
        }

        // Schema-specific validation could be added here
        // For now, we'll validate basic structure expectations
        var expectedSections = new Dictionary<string, bool>
        {
            { "UI", false },           // Optional but recommended
            { "Commands", false },     // Optional but recommended
            { "Layout", false }        // Optional but recommended
        };

        foreach (var (sectionName, isRequired) in expectedSections)
        {
            var section = configuration.Configuration.GetSection(sectionName);
            if (!SectionExists(section))
            {
                if (isRequired)
                    errors.Add($"Required section '{sectionName}' is missing from configuration.");
                else
                    warnings.Add($"Recommended section '{sectionName}' is missing from configuration.");
            }
        }

        // Simulate async work
        await Task.Delay(1, cancellationToken);

        return errors.Count > 0
            ? ValidationResult.Failure(errors.ToArray(), warnings.ToArray())
            : warnings.Count > 0
                ? ValidationResult.SuccessWithWarnings(warnings.ToArray())
                : ValidationResult.Success();
    }

    private static IEnumerable<string> GetConfigurationKeys(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        var keys = new List<string>();
        foreach (var child in configuration.GetChildren())
        {
            keys.Add(child.Key);
            var childKeys = GetConfigurationKeys(child).Select(k => $"{child.Key}:{k}");
            keys.AddRange(childKeys);
        }
        return keys;
    }

    private static bool SectionExists(Microsoft.Extensions.Configuration.IConfigurationSection section)
    {
        return section.GetChildren().Any() || !string.IsNullOrEmpty(section.Value);
    }
}