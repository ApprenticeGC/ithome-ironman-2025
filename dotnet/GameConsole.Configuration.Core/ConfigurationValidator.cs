using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace GameConsole.Configuration.Core;

/// <summary>
/// Default implementation of IConfigurationValidator providing comprehensive
/// validation using data annotations and custom validation rules.
/// </summary>
public class ConfigurationValidator : IConfigurationValidator
{
    private readonly ILogger<ConfigurationValidator> _logger;
    private readonly Dictionary<Type, List<Func<object, ValidationResult?>>> _customValidationRules;
    private readonly Dictionary<Type, string> _jsonSchemas;
    private readonly HashSet<Type> _supportedTypes;

    /// <summary>
    /// Initializes a new instance of the ConfigurationValidator class.
    /// </summary>
    public ConfigurationValidator(ILogger<ConfigurationValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _customValidationRules = new Dictionary<Type, List<Func<object, ValidationResult?>>>();
        _jsonSchemas = new Dictionary<Type, string>();
        _supportedTypes = new HashSet<Type>();
        
        // Register built-in configuration types
        RegisterBuiltInTypes();
    }

    /// <inheritdoc />
    public IReadOnlyList<Type> SupportedTypes => _supportedTypes.ToList();

    /// <inheritdoc />
    public async Task<ConfigurationValidationResult> ValidateAsync<T>(
        T configurationObject, 
        string sectionKey, 
        CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(configurationObject);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionKey);

        _logger.LogDebug("Validating configuration object of type '{Type}' for section '{SectionKey}'", 
            typeof(T).Name, sectionKey);

        var errors = new List<string>();
        var warnings = new List<string>();

        try
        {
            // Data Annotations validation
            var dataAnnotationResults = await ValidateDataAnnotationsAsync(configurationObject);
            errors.AddRange(dataAnnotationResults);

            // Custom validation rules
            var customValidationResults = await ValidateCustomRulesAsync(configurationObject);
            errors.AddRange(customValidationResults.errors);
            warnings.AddRange(customValidationResults.warnings);

            // JSON Schema validation if available
            if (_jsonSchemas.TryGetValue(typeof(T), out var schema))
            {
                var schemaValidationResults = await ValidateJsonSchemaAsync(configurationObject, schema);
                errors.AddRange(schemaValidationResults);
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate configuration object of type '{Type}' for section '{SectionKey}'", 
                typeof(T).Name, sectionKey);
            errors.Add($"Validation failed with exception: {ex.Message}");
        }

        return new ConfigurationValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }

    /// <inheritdoc />
    public async Task<ConfigurationValidationResult> ValidateSectionAsync(
        IConfiguration configuration,
        string sectionKey,
        Type expectedType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionKey);
        ArgumentNullException.ThrowIfNull(expectedType);

        _logger.LogDebug("Validating configuration section '{SectionKey}' against type '{ExpectedType}'", 
            sectionKey, expectedType.Name);

        var section = configuration.GetSection(sectionKey);
        if (!section.Exists())
        {
            return new ConfigurationValidationResult
            {
                IsValid = false,
                Errors = new[] { $"Configuration section '{sectionKey}' does not exist" }
            };
        }

        try
        {
            // Bind section to expected type
            var configurationObject = section.Get(expectedType);
            if (configurationObject == null)
            {
                return new ConfigurationValidationResult
                {
                    IsValid = false,
                    Errors = new[] { $"Failed to bind configuration section '{sectionKey}' to type '{expectedType.Name}'" }
                };
            }

            // Use reflection to call ValidateAsync<T> with the correct generic type
            var validateMethod = typeof(ConfigurationValidator)
                .GetMethod(nameof(ValidateAsync), BindingFlags.Public | BindingFlags.Instance)
                ?.MakeGenericMethod(expectedType);

            if (validateMethod == null)
            {
                return new ConfigurationValidationResult
                {
                    IsValid = false,
                    Errors = new[] { $"Could not find validation method for type '{expectedType.Name}'" }
                };
            }

            var result = await (Task<ConfigurationValidationResult>)validateMethod.Invoke(
                this, new object[] { configurationObject, sectionKey, cancellationToken })!;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate configuration section '{SectionKey}' against type '{ExpectedType}'", 
                sectionKey, expectedType.Name);
            
            return new ConfigurationValidationResult
            {
                IsValid = false,
                Errors = new[] { $"Validation failed with exception: {ex.Message}" }
            };
        }
    }

    /// <inheritdoc />
    public void RegisterValidationRule<T>(Func<T, ValidationResult?> validationRule) where T : class
    {
        ArgumentNullException.ThrowIfNull(validationRule);

        var type = typeof(T);
        _supportedTypes.Add(type);

        if (!_customValidationRules.TryGetValue(type, out var rules))
        {
            rules = new List<Func<object, ValidationResult?>>();
            _customValidationRules[type] = rules;
        }

        // Wrap the typed validation rule in an untyped one
        rules.Add(obj => obj is T typedObj ? validationRule(typedObj) : null);

        _logger.LogDebug("Registered custom validation rule for type '{Type}'", type.Name);
    }

    /// <inheritdoc />
    public void RegisterSchema<T>(string jsonSchema) where T : class
    {
        ArgumentNullException.ThrowIfNull(jsonSchema);

        var type = typeof(T);
        _supportedTypes.Add(type);
        _jsonSchemas[type] = jsonSchema;

        _logger.LogDebug("Registered JSON schema for type '{Type}'", type.Name);
    }

    private async Task<List<string>> ValidateDataAnnotationsAsync<T>(T configurationObject) where T : class
    {
        var errors = new List<string>();
        var validationContext = new ValidationContext(configurationObject);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(configurationObject, validationContext, validationResults, true);
        
        if (!isValid)
        {
            foreach (var validationResult in validationResults)
            {
                if (validationResult != null && !string.IsNullOrEmpty(validationResult.ErrorMessage))
                {
                    var memberNames = validationResult.MemberNames.Any() 
                        ? string.Join(", ", validationResult.MemberNames)
                        : "Unknown";
                    errors.Add($"{memberNames}: {validationResult.ErrorMessage}");
                }
            }
        }

        return await Task.FromResult(errors);
    }

    private async Task<(List<string> errors, List<string> warnings)> ValidateCustomRulesAsync<T>(T configurationObject) where T : class
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        var type = typeof(T);
        if (_customValidationRules.TryGetValue(type, out var rules))
        {
            foreach (var rule in rules)
            {
                try
                {
                    var result = rule(configurationObject);
                    if (result != null && !string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        // Determine if it's an error or warning based on the validation result
                        if (result.ErrorMessage.StartsWith("Warning:", StringComparison.OrdinalIgnoreCase))
                        {
                            warnings.Add(result.ErrorMessage);
                        }
                        else
                        {
                            errors.Add(result.ErrorMessage);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Custom validation rule failed for type '{Type}'", type.Name);
                    errors.Add($"Custom validation rule failed: {ex.Message}");
                }
            }
        }

        return await Task.FromResult((errors, warnings));
    }

    private async Task<List<string>> ValidateJsonSchemaAsync<T>(T configurationObject, string jsonSchema) where T : class
    {
        var errors = new List<string>();
        
        try
        {
            // Basic JSON schema validation would be implemented here
            // For now, we'll just log that schema validation is not fully implemented
            _logger.LogWarning("JSON Schema validation is not fully implemented yet for type '{Type}'", typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JSON Schema validation failed for type '{Type}'", typeof(T).Name);
            errors.Add($"Schema validation failed: {ex.Message}");
        }

        return await Task.FromResult(errors);
    }

    private void RegisterBuiltInTypes()
    {
        // Register common configuration types that might be used
        // These would typically be defined in the actual application
        _logger.LogDebug("Registering built-in configuration types");
        
        // Note: In a real implementation, you would register actual configuration classes here
        // For this implementation, we'll keep it minimal to avoid dependencies on specific types
    }
}