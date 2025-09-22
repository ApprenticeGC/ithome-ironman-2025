using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using GameConsole.Configuration.Core.Models;

namespace GameConsole.Configuration.Core;

/// <summary>
/// Validates configuration against schemas and business rules.
/// </summary>
public sealed class ConfigurationValidator : IConfigurationValidator
{
    private readonly ILogger<ConfigurationValidator> _logger;
    private readonly Dictionary<Type, Func<object, string, Task<ValidationResult>>> _validators;
    private readonly Dictionary<string, string> _jsonSchemas;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationValidator"/> class.
    /// </summary>
    public ConfigurationValidator(ILogger<ConfigurationValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _validators = new Dictionary<Type, Func<object, string, Task<ValidationResult>>>();
        _jsonSchemas = new Dictionary<string, string>();
    }

    /// <inheritdoc />
    public async Task<ValidationResult> ValidateAsync(IConfiguration configuration, ConfigurationContext context, CancellationToken cancellationToken = default)
    {
        var errors = new List<ValidationError>();
        var warnings = new List<ValidationWarning>();

        _logger.LogDebug("Validating configuration for environment: {Environment}", context.Environment);

        try
        {
            // Basic structure validation
            await ValidateBasicStructureAsync(configuration, errors, warnings, cancellationToken);

            // Environment-specific validation
            await ValidateEnvironmentSpecificAsync(configuration, context, errors, warnings, cancellationToken);

            // Custom validators
            await RunCustomValidatorsAsync(configuration, errors, warnings, cancellationToken);

            _logger.LogDebug("Configuration validation completed with {ErrorCount} errors and {WarningCount} warnings", 
                errors.Count, warnings.Count);

            return errors.Count == 0 
                ? ValidationResult.Success(warnings)
                : ValidationResult.Failure(errors, warnings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during configuration validation");
            errors.Add(new ValidationError("", $"Validation failed with exception: {ex.Message}", ValidationSeverity.Critical));
            return ValidationResult.Failure(errors, warnings);
        }
    }

    /// <inheritdoc />
    public async Task<ValidationResult> ValidateSectionAsync<T>(T section, string sectionPath, CancellationToken cancellationToken = default) where T : class
    {
        var errors = new List<ValidationError>();
        var warnings = new List<ValidationWarning>();

        if (section == null)
        {
            errors.Add(new ValidationError(sectionPath, "Configuration section is null", ValidationSeverity.Error));
            return ValidationResult.Failure(errors);
        }

        // Check if there's a custom validator for this type
        if (_validators.TryGetValue(typeof(T), out var validator))
        {
            var result = await validator(section, sectionPath);
            return result;
        }

        // Basic property validation
        await ValidateObjectPropertiesAsync(section, sectionPath, errors, warnings, cancellationToken);

        return errors.Count == 0 
            ? ValidationResult.Success(warnings)
            : ValidationResult.Failure(errors, warnings);
    }

    /// <inheritdoc />
    public void RegisterValidator<T>(Func<T, string, Task<ValidationResult>> validator) where T : class
    {
        ArgumentNullException.ThrowIfNull(validator);
        
        _validators[typeof(T)] = async (obj, path) => await validator((T)obj, path);
        _logger.LogDebug("Registered custom validator for type: {Type}", typeof(T).Name);
    }

    /// <inheritdoc />
    public void RegisterJsonSchema(string sectionPath, string jsonSchema)
    {
        ArgumentNullException.ThrowIfNull(sectionPath);
        ArgumentNullException.ThrowIfNull(jsonSchema);
        
        _jsonSchemas[sectionPath] = jsonSchema;
        _logger.LogDebug("Registered JSON schema for section: {SectionPath}", sectionPath);
    }

    /// <inheritdoc />
    public void RemoveValidator<T>() where T : class
    {
        if (_validators.Remove(typeof(T)))
        {
            _logger.LogDebug("Removed validator for type: {Type}", typeof(T).Name);
        }
    }

    private async Task ValidateBasicStructureAsync(IConfiguration configuration, List<ValidationError> errors, List<ValidationWarning> warnings, CancellationToken cancellationToken)
    {
        // Check if configuration root exists
        if (configuration == null)
        {
            errors.Add(new ValidationError("", "Configuration root is null", ValidationSeverity.Critical));
            return;
        }

        // Validate required sections exist
        var requiredSections = new[] { "Logging" };
        foreach (var section in requiredSections)
        {
            if (!configuration.GetSection(section).Exists())
            {
                warnings.Add(new ValidationWarning(section, $"Recommended section '{section}' is missing"));
            }
        }

        await Task.CompletedTask; // Placeholder for async validation logic
    }

    private async Task ValidateEnvironmentSpecificAsync(IConfiguration configuration, ConfigurationContext context, List<ValidationError> errors, List<ValidationWarning> warnings, CancellationToken cancellationToken)
    {
        switch (context.Environment?.ToLowerInvariant())
        {
            case "production":
                // Production-specific validation
                ValidateProductionConfiguration(configuration, errors, warnings);
                break;
                
            case "development":
                // Development-specific validation
                ValidateDevelopmentConfiguration(configuration, errors, warnings);
                break;
        }

        await Task.CompletedTask;
    }

    private void ValidateProductionConfiguration(IConfiguration configuration, List<ValidationError> errors, List<ValidationWarning> warnings)
    {
        // Ensure sensitive debug features are disabled in production
        if (configuration.GetValue<bool>("DetailedErrors", false))
        {
            warnings.Add(new ValidationWarning("DetailedErrors", "Detailed errors should be disabled in production"));
        }

        // Validate logging configuration
        var logLevel = configuration.GetValue<string>("Logging:LogLevel:Default", "Information");
        if (!string.IsNullOrEmpty(logLevel) && logLevel.Equals("Debug", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add(new ValidationWarning("Logging:LogLevel:Default", "Debug logging should be avoided in production"));
        }
    }

    private void ValidateDevelopmentConfiguration(IConfiguration configuration, List<ValidationError> errors, List<ValidationWarning> warnings)
    {
        // Development-specific validations can be added here
        // For now, just log that we're validating development configuration
        _logger.LogDebug("Performing development environment validation");
    }

    private async Task RunCustomValidatorsAsync(IConfiguration configuration, List<ValidationError> errors, List<ValidationWarning> warnings, CancellationToken cancellationToken)
    {
        // Run custom validators - placeholder for future implementation
        await Task.CompletedTask;
    }

    private async Task ValidateObjectPropertiesAsync(object obj, string path, List<ValidationError> errors, List<ValidationWarning> warnings, CancellationToken cancellationToken)
    {
        var properties = obj.GetType().GetProperties();
        
        foreach (var property in properties)
        {
            var value = property.GetValue(obj);
            var propertyPath = $"{path}:{property.Name}";
            
            // Basic null checks for reference types
            if (property.PropertyType.IsClass && property.PropertyType != typeof(string) && value == null)
            {
                warnings.Add(new ValidationWarning(propertyPath, $"Property '{property.Name}' is null"));
            }
        }

        await Task.CompletedTask;
    }
}