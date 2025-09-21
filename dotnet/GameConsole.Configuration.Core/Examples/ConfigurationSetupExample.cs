using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using GameConsole.Configuration.Core.Providers;

namespace GameConsole.Configuration.Core.Examples;

/// <summary>
/// Example showing how to set up the configuration management system.
/// </summary>
public static class ConfigurationSetupExample
{
    /// <summary>
    /// Example of configuring the configuration management system with DI.
    /// </summary>
    public static IServiceCollection AddGameConsoleConfiguration(
        this IServiceCollection services,
        string environment = "Development",
        string mode = "Game")
    {
        // Register configuration context
        services.AddSingleton(new ConfigurationContext
        {
            Environment = environment,
            Mode = mode,
            Properties = new Dictionary<string, object?>
            {
                ["ApplicationName"] = "GameConsole",
                ["Version"] = "1.0.0"
            }
        });

        // Register configuration providers
        services.AddSingleton<IConfigurationProvider>(provider =>
            new JsonConfigurationProvider(
                provider.GetRequiredService<ILogger<JsonConfigurationProvider>>(),
                "config",
                reloadOnChange: true));

        services.AddSingleton<IConfigurationProvider>(provider =>
            new XmlConfigurationProvider(
                provider.GetRequiredService<ILogger<XmlConfigurationProvider>>(),
                "config",
                reloadOnChange: true));

        services.AddSingleton<IConfigurationProvider>(provider =>
            new EnvironmentVariablesConfigurationProvider(
                provider.GetRequiredService<ILogger<EnvironmentVariablesConfigurationProvider>>(),
                "GameConsole_"));

        // Register core configuration services
        services.AddSingleton<IEnvironmentConfigurationResolver, EnvironmentConfigurationResolver>();
        services.AddSingleton<IConfigurationValidator, ConfigurationValidator>();
        services.AddSingleton<IConfigurationManager, ConfigurationManager>();

        return services;
    }

    /// <summary>
    /// Example configuration classes that could be used with the system.
    /// </summary>
    public class GameConsoleConfiguration
    {
        public EngineConfiguration Engine { get; set; } = new();
        public AudioConfiguration Audio { get; set; } = new();
        public InputConfiguration Input { get; set; } = new();
        public ConfigurationManagementConfiguration Configuration { get; set; } = new();
    }

    public class EngineConfiguration
    {
        public string Type { get; set; } = "Unity";
        public int TargetFrameRate { get; set; } = 60;
    }

    public class AudioConfiguration
    {
        public string Provider { get; set; } = "OpenAL";
        public double Volume { get; set; } = 0.8;
        public bool EnableSpatial { get; set; } = true;
    }

    public class InputConfiguration
    {
        public string Provider { get; set; } = "Direct";
        public bool EnableGamepad { get; set; } = true;
        public string KeyboardLayout { get; set; } = "QWERTY";
    }

    public class ConfigurationManagementConfiguration
    {
        public bool HotReloadEnabled { get; set; } = true;
        public string ValidationLevel { get; set; } = "Strict";
        public bool CacheEnabled { get; set; } = true;
    }
}