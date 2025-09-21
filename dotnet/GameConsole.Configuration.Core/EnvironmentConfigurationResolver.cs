using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GameConsole.Configuration.Core;

/// <summary>
/// Default implementation of IEnvironmentConfigurationResolver that provides
/// environment-specific configuration resolution with inheritance support.
/// </summary>
public class EnvironmentConfigurationResolver : IEnvironmentConfigurationResolver
{
    private readonly ILogger<EnvironmentConfigurationResolver> _logger;
    private readonly string _configurationBasePath;
    private static readonly string[] DefaultEnvironments = { "Development", "Staging", "Production" };

    /// <summary>
    /// Initializes a new instance of the EnvironmentConfigurationResolver class.
    /// </summary>
    public EnvironmentConfigurationResolver(
        ILogger<EnvironmentConfigurationResolver> logger,
        string configurationBasePath = "config")
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configurationBasePath = configurationBasePath ?? throw new ArgumentNullException(nameof(configurationBasePath));
    }

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedEnvironments => DefaultEnvironments;

    /// <inheritdoc />
    public async Task<IConfiguration> ResolveAsync(
        ConfigurationContext context, 
        IConfiguration baseConfiguration, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(baseConfiguration);

        _logger.LogDebug("Resolving configuration for environment '{Environment}' and mode '{Mode}'",
            context.Environment, context.Mode);

        // If no environment-specific overrides are needed, return base configuration
        if (string.IsNullOrEmpty(context.Environment))
        {
            return baseConfiguration;
        }

        // Create a new configuration builder starting with the base configuration
        var builder = new ConfigurationBuilder();
        
        // Add base configuration as the foundation
        builder.AddConfiguration(baseConfiguration);

        // Apply environment-specific overrides
        await ApplyEnvironmentOverridesAsync(builder, context, cancellationToken);

        var resolvedConfiguration = builder.Build();
        
        _logger.LogDebug("Configuration resolved successfully for environment '{Environment}'", context.Environment);
        
        return resolvedConfiguration;
    }

    /// <inheritdoc />
    public bool ShouldOverride(string key, string environment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(environment);

        // Define keys that should have environment-specific overrides
        var overridableKeys = new[]
        {
            "ConnectionStrings",
            "Logging",
            "Authentication",
            "ApiKeys",
            "External",
            "Security",
            "Performance"
        };

        return overridableKeys.Any(overridableKey => 
            key.StartsWith(overridableKey, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetConfigurationPathsAsync(ConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var paths = new List<string>();
        
        // Base configuration paths
        var baseJsonPath = Path.Combine(_configurationBasePath, "appsettings.json");
        var baseXmlPath = Path.Combine(_configurationBasePath, "appsettings.xml");
        
        paths.Add(baseJsonPath);
        paths.Add(baseXmlPath);

        // Environment-specific paths
        if (!string.IsNullOrEmpty(context.Environment))
        {
            var envJsonPath = Path.Combine(_configurationBasePath, $"appsettings.{context.Environment}.json");
            var envXmlPath = Path.Combine(_configurationBasePath, $"appsettings.{context.Environment}.xml");
            
            paths.Add(envJsonPath);
            paths.Add(envXmlPath);
        }

        // Mode-specific paths
        if (!string.IsNullOrEmpty(context.Mode))
        {
            var modeJsonPath = Path.Combine(_configurationBasePath, $"appsettings.{context.Mode}.json");
            var modeXmlPath = Path.Combine(_configurationBasePath, $"appsettings.{context.Mode}.xml");
            
            paths.Add(modeJsonPath);
            paths.Add(modeXmlPath);
        }

        // Environment + Mode specific paths
        if (!string.IsNullOrEmpty(context.Environment) && !string.IsNullOrEmpty(context.Mode))
        {
            var combinedJsonPath = Path.Combine(_configurationBasePath, 
                $"appsettings.{context.Environment}.{context.Mode}.json");
            var combinedXmlPath = Path.Combine(_configurationBasePath, 
                $"appsettings.{context.Environment}.{context.Mode}.xml");
            
            paths.Add(combinedJsonPath);
            paths.Add(combinedXmlPath);
        }

        return await Task.FromResult(paths.Where(File.Exists));
    }

    private async Task ApplyEnvironmentOverridesAsync(
        IConfigurationBuilder builder, 
        ConfigurationContext context, 
        CancellationToken cancellationToken)
    {
        var configPaths = await GetConfigurationPathsAsync(context);
        
        foreach (var configPath in configPaths)
        {
            _logger.LogDebug("Applying environment override from '{ConfigPath}'", configPath);
            
            var extension = Path.GetExtension(configPath).ToLowerInvariant();
            switch (extension)
            {
                case ".json":
                    builder.AddJsonFile(configPath, optional: true, reloadOnChange: true);
                    break;
                case ".xml":
                    builder.AddXmlFile(configPath, optional: true, reloadOnChange: true);
                    break;
                default:
                    _logger.LogWarning("Unsupported configuration file extension '{Extension}' for file '{ConfigPath}'", 
                        extension, configPath);
                    break;
            }
        }

        // Apply environment variables with environment-specific prefix
        var envPrefix = $"GameConsole_{context.Environment}_";
        builder.AddEnvironmentVariables(envPrefix);
        
        // Apply mode-specific environment variables
        if (!string.IsNullOrEmpty(context.Mode))
        {
            var modePrefix = $"GameConsole_{context.Mode}_";
            builder.AddEnvironmentVariables(modePrefix);
        }
    }
}