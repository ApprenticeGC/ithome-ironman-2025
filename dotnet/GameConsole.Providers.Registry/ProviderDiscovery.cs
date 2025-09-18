using System.Reflection;
using GameConsole.Core.Abstractions;

namespace GameConsole.Providers.Registry;

/// <summary>
/// Provides functionality for automatic discovery of provider implementations through assembly scanning.
/// </summary>
public static class ProviderDiscovery
{
    /// <summary>
    /// Discovers and registers providers from the specified assembly.
    /// </summary>
    /// <typeparam name="TContract">The provider contract type.</typeparam>
    /// <param name="registry">The provider registry to register discovered providers.</param>
    /// <param name="assembly">The assembly to scan for providers.</param>
    /// <param name="serviceProvider">Optional service provider for dependency injection when creating provider instances.</param>
    /// <returns>The number of providers discovered and registered.</returns>
    public static int DiscoverAndRegisterProviders<TContract>(
        IProviderRegistry<TContract> registry,
        Assembly assembly,
        IServiceProvider? serviceProvider = null)
        where TContract : class
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(assembly);

        var discovered = DiscoverProviders<TContract>(assembly);
        var registered = 0;

        foreach (var (type, metadata) in discovered)
        {
            try
            {
                var provider = CreateProviderInstance<TContract>(type, serviceProvider);
                if (provider != null)
                {
                    registry.RegisterProvider(provider, metadata);
                    registered++;
                }
            }
            catch (Exception ex)
            {
                // Log the error but continue with other providers
                // In a real implementation, this would use proper logging
                Console.WriteLine($"Failed to create provider instance for type {type.FullName}: {ex.Message}");
            }
        }

        return registered;
    }

    /// <summary>
    /// Discovers provider types and their metadata from the specified assembly.
    /// </summary>
    /// <typeparam name="TContract">The provider contract type.</typeparam>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>A collection of discovered provider types and their metadata.</returns>
    public static IReadOnlyList<(Type Type, ProviderMetadata Metadata)> DiscoverProviders<TContract>(Assembly assembly)
        where TContract : class
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var contractType = typeof(TContract);
        var discovered = new List<(Type, ProviderMetadata)>();

        try
        {
            var types = assembly.GetTypes();

            foreach (var type in types)
            {
                // Skip abstract classes and interfaces
                if (type.IsAbstract || type.IsInterface)
                    continue;

                // Check if type implements the contract
                if (!contractType.IsAssignableFrom(type))
                    continue;

                // Try to extract metadata
                var metadata = ExtractProviderMetadata(type);
                if (metadata != null)
                {
                    discovered.Add((type, metadata));
                }
            }
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Handle cases where some types can't be loaded
            var loadableTypes = ex.Types.Where(t => t != null).Cast<Type>();
            foreach (var type in loadableTypes)
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;

                if (!contractType.IsAssignableFrom(type))
                    continue;

                var metadata = ExtractProviderMetadata(type);
                if (metadata != null)
                {
                    discovered.Add((type, metadata));
                }
            }
        }

        return discovered;
    }

    /// <summary>
    /// Discovers providers from all assemblies in the current application domain.
    /// </summary>
    /// <typeparam name="TContract">The provider contract type.</typeparam>
    /// <param name="registry">The provider registry to register discovered providers.</param>
    /// <param name="serviceProvider">Optional service provider for dependency injection.</param>
    /// <returns>The number of providers discovered and registered.</returns>
    public static int DiscoverAndRegisterProvidersFromAllAssemblies<TContract>(
        IProviderRegistry<TContract> registry,
        IServiceProvider? serviceProvider = null)
        where TContract : class
    {
        ArgumentNullException.ThrowIfNull(registry);

        var totalRegistered = 0;
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            try
            {
                totalRegistered += DiscoverAndRegisterProviders(registry, assembly, serviceProvider);
            }
            catch (Exception ex)
            {
                // Log the error but continue with other assemblies
                Console.WriteLine($"Failed to discover providers from assembly {assembly.FullName}: {ex.Message}");
            }
        }

        return totalRegistered;
    }

    /// <summary>
    /// Extracts provider metadata from a type using reflection and attributes.
    /// </summary>
    /// <param name="type">The type to extract metadata from.</param>
    /// <returns>The extracted metadata, or null if metadata cannot be determined.</returns>
    private static ProviderMetadata? ExtractProviderMetadata(Type type)
    {
        // Try to get metadata from ServiceAttribute first
        var serviceAttribute = type.GetCustomAttribute<ServiceAttribute>();
        if (serviceAttribute != null)
        {
            var name = serviceAttribute.Name ?? type.Name;
            var version = Version.TryParse(serviceAttribute.Version, out var parsedVersion) ? parsedVersion : ExtractVersion(type);
            var capabilities = ExtractCapabilities(type);
            var platforms = ExtractSupportedPlatforms(type);
            const int defaultPriority = 0; // Default priority since ServiceAttribute doesn't have Priority

            return new ProviderMetadata(name, defaultPriority, capabilities, platforms, version);
        }

        // Try to get metadata from IServiceMetadata interface
        if (typeof(IServiceMetadata).IsAssignableFrom(type))
        {
            try
            {
                var instance = Activator.CreateInstance(type) as IServiceMetadata;
                if (instance != null)
                {
                    var version = Version.TryParse(instance.Version, out var parsedVersion) ? parsedVersion : new Version(1, 0, 0);
                    var capabilities = new HashSet<string>(instance.Categories);
                    var platforms = Platform.All; // Default to all platforms
                    const int defaultPriority = 0;

                    return new ProviderMetadata(instance.Name, defaultPriority, capabilities, platforms, version);
                }
            }
            catch
            {
                // Fall through to default metadata
            }
        }

        // Generate default metadata
        return new ProviderMetadata(
            type.Name,
            0,
            new HashSet<string>(),
            Platform.All,
            ExtractVersion(type));
    }

    /// <summary>
    /// Extracts version information from a type.
    /// </summary>
    /// <param name="type">The type to extract version from.</param>
    /// <returns>The extracted version or a default version.</returns>
    private static Version ExtractVersion(Type type)
    {
        // Try to get version from assembly
        var assembly = type.Assembly;
        var assemblyVersion = assembly.GetName().Version;
        return assemblyVersion ?? new Version(1, 0, 0);
    }

    /// <summary>
    /// Extracts capabilities from a type by checking if it implements ICapabilityProvider.
    /// </summary>
    /// <param name="type">The type to extract capabilities from.</param>
    /// <returns>A set of capabilities.</returns>
    private static IReadOnlySet<string> ExtractCapabilities(Type type)
    {
        var capabilities = new HashSet<string>();

        // Check if type implements ICapabilityProvider
        if (typeof(ICapabilityProvider).IsAssignableFrom(type))
        {
            // For now, just add a generic capability indicator
            capabilities.Add("CapabilityProvider");
        }

        // Could be extended to analyze implemented interfaces or attributes
        return capabilities;
    }

    /// <summary>
    /// Extracts supported platforms from a type (currently defaults to all platforms).
    /// </summary>
    /// <param name="type">The type to extract platform support from.</param>
    /// <returns>The supported platforms.</returns>
    private static Platform ExtractSupportedPlatforms(Type type)
    {
        // For now, default to all platforms
        // Could be extended to check for platform-specific attributes or conditional compilation symbols
        return Platform.All;
    }

    /// <summary>
    /// Creates an instance of a provider type.
    /// </summary>
    /// <typeparam name="TContract">The provider contract type.</typeparam>
    /// <param name="type">The concrete type to instantiate.</param>
    /// <param name="serviceProvider">Optional service provider for dependency injection.</param>
    /// <returns>The created provider instance, or null if creation failed.</returns>
    private static TContract? CreateProviderInstance<TContract>(Type type, IServiceProvider? serviceProvider)
        where TContract : class
    {
        try
        {
            // Try using service provider for dependency injection
            if (serviceProvider != null)
            {
                var instance = ActivatorUtilities.CreateInstance(serviceProvider, type);
                return instance as TContract;
            }

            // Fall back to parameterless constructor
            return Activator.CreateInstance(type) as TContract;
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Helper class for creating instances with dependency injection support.
/// This is a simplified version - in practice you'd use Microsoft.Extensions.DependencyInjection.
/// </summary>
internal static class ActivatorUtilities
{
    public static object CreateInstance(IServiceProvider serviceProvider, Type type)
    {
        // Simplified implementation - try parameterless constructor first
        try
        {
            return Activator.CreateInstance(type) ?? throw new InvalidOperationException($"Cannot create instance of {type}");
        }
        catch
        {
            // In a real implementation, this would analyze constructors and inject dependencies
            throw new InvalidOperationException($"Cannot create instance of {type} with available services");
        }
    }
}