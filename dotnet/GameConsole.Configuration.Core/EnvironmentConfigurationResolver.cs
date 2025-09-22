using Microsoft.Extensions.Logging;
using GameConsole.Configuration.Core.Models;

namespace GameConsole.Configuration.Core;

/// <summary>
/// Resolves environment-specific configuration settings with support for inheritance and overrides.
/// </summary>
public sealed class EnvironmentConfigurationResolver : IEnvironmentConfigurationResolver
{
    private readonly ILogger<EnvironmentConfigurationResolver> _logger;
    private string _currentEnvironment;
    
    private static readonly string[] DefaultSupportedEnvironments = 
    {
        "Development",
        "Staging", 
        "Production",
        "Testing"
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentConfigurationResolver"/> class.
    /// </summary>
    public EnvironmentConfigurationResolver(ILogger<EnvironmentConfigurationResolver> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _currentEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") 
                             ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                             ?? Environment.GetEnvironmentVariable("GAMECONSOLE_ENVIRONMENT")
                             ?? "Development";
        
        _logger.LogInformation("Environment resolved to: {Environment}", _currentEnvironment);
    }

    /// <inheritdoc />
    public string CurrentEnvironment => _currentEnvironment;

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedEnvironments => DefaultSupportedEnvironments;

    /// <inheritdoc />
    public IReadOnlyList<string> ResolveConfigurationPaths(string basePath, ConfigurationContext context)
    {
        var paths = new List<string>();
        var baseFileName = Path.GetFileNameWithoutExtension(basePath);
        var directory = Path.GetDirectoryName(basePath) ?? "";
        var extension = Path.GetExtension(basePath);

        // Base configuration file (lowest priority)
        paths.Add(basePath);

        // Environment-specific file
        if (!string.IsNullOrEmpty(context.Environment))
        {
            var envFile = Path.Combine(directory, $"{baseFileName}.{context.Environment}{extension}");
            paths.Add(envFile);
        }

        // Mode-specific file
        if (!string.IsNullOrEmpty(context.Mode))
        {
            var modeFile = Path.Combine(directory, $"{baseFileName}.{context.Mode}{extension}");
            paths.Add(modeFile);
        }

        // User-specific file (highest priority)
        if (!string.IsNullOrEmpty(context.UserId))
        {
            var userFile = Path.Combine(directory, $"{baseFileName}.User.{context.UserId}{extension}");
            paths.Add(userFile);
        }

        _logger.LogDebug("Resolved configuration paths for {BasePath}: {Paths}", basePath, paths);
        return paths;
    }

    /// <inheritdoc />
    public bool IsValidEnvironment(string environment)
    {
        return !string.IsNullOrWhiteSpace(environment) && 
               SupportedEnvironments.Contains(environment, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, object>> GetEnvironmentOverridesAsync(ConfigurationContext context)
    {
        var overrides = new Dictionary<string, object>();

        // Add environment-specific overrides based on context
        switch (context.Environment?.ToLowerInvariant())
        {
            case "development":
                overrides["Logging:LogLevel:Default"] = "Debug";
                overrides["DetailedErrors"] = true;
                break;
                
            case "production":
                overrides["Logging:LogLevel:Default"] = "Warning";
                overrides["DetailedErrors"] = false;
                break;
                
            case "testing":
                overrides["Logging:LogLevel:Default"] = "Information";
                overrides["DisableBackgroundServices"] = true;
                break;
        }

        // Add mode-specific overrides
        if (!string.IsNullOrEmpty(context.Mode))
        {
            switch (context.Mode.ToLowerInvariant())
            {
                case "game":
                    overrides["Performance:OptimizeForGame"] = true;
                    break;
                    
                case "editor":
                    overrides["Performance:OptimizeForEditor"] = true;
                    overrides["EnableDevelopmentFeatures"] = true;
                    break;
            }
        }

        _logger.LogDebug("Generated {Count} environment overrides for context: {Context}", 
            overrides.Count, $"{context.Environment}/{context.Mode}");

        return Task.FromResult<IReadOnlyDictionary<string, object>>(overrides);
    }

    /// <inheritdoc />
    public void SetEnvironment(string environment)
    {
        if (!IsValidEnvironment(environment))
        {
            throw new ArgumentException($"Invalid environment: {environment}. Supported environments: {string.Join(", ", SupportedEnvironments)}", nameof(environment));
        }

        var previousEnvironment = _currentEnvironment;
        _currentEnvironment = environment;
        
        _logger.LogInformation("Environment changed from {Previous} to {Current}", previousEnvironment, _currentEnvironment);
    }
}