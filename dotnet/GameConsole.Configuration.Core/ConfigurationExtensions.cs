using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using GameConsole.Configuration.Core.Models;
using GameConsole.Configuration.Core.Providers;

namespace GameConsole.Configuration.Core;

/// <summary>
/// Extension methods for configuring the GameConsole Configuration Management System.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Adds the GameConsole Configuration Management System to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGameConsoleConfiguration(
        this IServiceCollection services,
        Action<GameConsoleConfigurationOptions>? configureOptions = null)
    {
        var options = new GameConsoleConfigurationOptions();
        configureOptions?.Invoke(options);

        // Register core services
        services.AddSingleton<IEnvironmentConfigurationResolver, EnvironmentConfigurationResolver>();
        services.AddSingleton<IConfigurationValidator, ConfigurationValidator>();
        services.AddSingleton<IConfigurationManager, ConfigurationManager>();

        // Register default providers if enabled
        if (options.EnableJsonProvider)
        {
            services.AddSingleton<IConfigurationProvider>(provider =>
                new JsonConfigurationProvider(
                    provider.GetRequiredService<ILogger<JsonConfigurationProvider>>(),
                    options.JsonConfigPath,
                    options.JsonOptional,
                    options.JsonPriority));
        }

        if (options.EnableXmlProvider)
        {
            services.AddSingleton<IConfigurationProvider>(provider =>
                new XmlConfigurationProvider(
                    provider.GetRequiredService<ILogger<XmlConfigurationProvider>>(),
                    options.XmlConfigPath,
                    options.XmlOptional,
                    options.XmlPriority));
        }

        if (options.EnableEnvironmentVariables)
        {
            services.AddSingleton<IConfigurationProvider>(provider =>
                new EnvironmentVariablesConfigurationProvider(
                    provider.GetRequiredService<ILogger<EnvironmentVariablesConfigurationProvider>>(),
                    options.EnvironmentVariablePrefix,
                    options.EnvironmentVariablePriority));
        }

        return services;
    }

    /// <summary>
    /// Builds a configuration root using the GameConsole Configuration Management System.
    /// </summary>
    /// <param name="context">The configuration context.</param>
    /// <param name="providers">The configuration providers to use.</param>
    /// <returns>The built configuration root.</returns>
    public static async Task<IConfigurationRoot> BuildGameConsoleConfigurationAsync(
        ConfigurationContext context,
        params IConfigurationProvider[] providers)
    {
        var builder = new ConfigurationBuilder();
        
        // Sort providers by priority (higher first)
        var sortedProviders = providers.OrderByDescending(p => p.Priority).ToArray();
        
        foreach (var provider in sortedProviders.Where(p => p.CanLoad(context)))
        {
            await provider.LoadAsync(builder, context);
        }
        
        return builder.Build();
    }
}

/// <summary>
/// Configuration options for the GameConsole Configuration Management System.
/// </summary>
public sealed class GameConsoleConfigurationOptions
{
    /// <summary>
    /// Gets or sets whether to enable the JSON configuration provider.
    /// </summary>
    public bool EnableJsonProvider { get; set; } = true;

    /// <summary>
    /// Gets or sets the path to the JSON configuration file.
    /// </summary>
    public string JsonConfigPath { get; set; } = "appsettings.json";

    /// <summary>
    /// Gets or sets whether the JSON configuration file is optional.
    /// </summary>
    public bool JsonOptional { get; set; } = true;

    /// <summary>
    /// Gets or sets the priority of the JSON configuration provider.
    /// </summary>
    public int JsonPriority { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether to enable the XML configuration provider.
    /// </summary>
    public bool EnableXmlProvider { get; set; } = false;

    /// <summary>
    /// Gets or sets the path to the XML configuration file.
    /// </summary>
    public string XmlConfigPath { get; set; } = "config.xml";

    /// <summary>
    /// Gets or sets whether the XML configuration file is optional.
    /// </summary>
    public bool XmlOptional { get; set; } = true;

    /// <summary>
    /// Gets or sets the priority of the XML configuration provider.
    /// </summary>
    public int XmlPriority { get; set; } = 90;

    /// <summary>
    /// Gets or sets whether to enable environment variables configuration provider.
    /// </summary>
    public bool EnableEnvironmentVariables { get; set; } = true;

    /// <summary>
    /// Gets or sets the prefix for environment variables.
    /// </summary>
    public string EnvironmentVariablePrefix { get; set; } = "GAMECONSOLE_";

    /// <summary>
    /// Gets or sets the priority of the environment variables configuration provider.
    /// </summary>
    public int EnvironmentVariablePriority { get; set; } = 200;
}