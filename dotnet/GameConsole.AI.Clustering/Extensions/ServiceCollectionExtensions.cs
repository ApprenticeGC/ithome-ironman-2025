using GameConsole.AI.Clustering.Configuration;
using GameConsole.AI.Clustering.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GameConsole.AI.Clustering.Extensions;

/// <summary>
/// Extension methods for registering AI clustering services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add AI clustering services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configureOptions">Configuration action</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddAIClusteringServices(
        this IServiceCollection services,
        Action<AIClusterConfig>? configureOptions = null)
    {
        // Configure the cluster configuration
        var config = new AIClusterConfig();
        configureOptions?.Invoke(config);
        
        services.AddSingleton(config);
        services.AddSingleton<AIClusterService>();
        services.AddHostedService<AIClusterService>(provider => provider.GetRequiredService<AIClusterService>());
        
        return services;
    }

    /// <summary>
    /// Add AI clustering services with specific configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="config">Pre-configured cluster configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddAIClusteringServices(
        this IServiceCollection services,
        AIClusterConfig config)
    {
        services.AddSingleton(config);
        services.AddSingleton<AIClusterService>();
        services.AddHostedService<AIClusterService>(provider => provider.GetRequiredService<AIClusterService>());
        
        return services;
    }

    /// <summary>
    /// Add AI clustering with default configuration for development
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="nodeCapabilities">Capabilities for this node</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddAIClusteringForDevelopment(
        this IServiceCollection services,
        params string[] nodeCapabilities)
    {
        var config = new AIClusterConfig
        {
            ClusterPort = 8080,
            SeedNodes = new List<string> { "localhost:8080" },
            NodeCapabilities = nodeCapabilities.ToList(),
            HealthCheckIntervalSeconds = 10,
            MinClusterSize = 1,
            MaxClusterSize = 5,
            DiscoveryMethod = "static"
        };

        return AddAIClusteringServices(services, config);
    }

    /// <summary>
    /// Add AI clustering with production configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="seedNodes">Seed nodes for cluster discovery</param>
    /// <param name="nodeCapabilities">Capabilities for this node</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddAIClusteringForProduction(
        this IServiceCollection services,
        IEnumerable<string> seedNodes,
        params string[] nodeCapabilities)
    {
        var config = new AIClusterConfig
        {
            ClusterPort = 8080,
            SeedNodes = seedNodes.ToList(),
            NodeCapabilities = nodeCapabilities.ToList(),
            HealthCheckIntervalSeconds = 30,
            MinClusterSize = 3,
            MaxClusterSize = 20,
            DiscoveryMethod = "static",
            LoadBalancingStrategy = LoadBalancingStrategies.WeightedRoundRobin
        };

        return AddAIClusteringServices(services, config);
    }
}