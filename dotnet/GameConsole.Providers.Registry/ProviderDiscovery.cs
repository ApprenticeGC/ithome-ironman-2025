using System.Reflection;
using Microsoft.Extensions.Logging;

namespace GameConsole.Providers.Registry;

/// <summary>
/// Provides automatic provider discovery functionality through assembly scanning and reflection.
/// </summary>
public class ProviderDiscovery
{
    private readonly ILogger<ProviderDiscovery>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderDiscovery"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for discovery operations.</param>
    public ProviderDiscovery(ILogger<ProviderDiscovery>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Discovers providers of a specific contract type from the specified assemblies.
    /// </summary>
    /// <typeparam name="TContract">The contract type to discover providers for.</typeparam>
    /// <param name="assemblies">Assemblies to scan for providers.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns a collection of discovered provider descriptors.</returns>
    public async Task<IReadOnlyList<ProviderDescriptor<TContract>>> DiscoverProvidersAsync<TContract>(
        IEnumerable<Assembly> assemblies,
        CancellationToken cancellationToken = default) where TContract : class
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        var providers = new List<ProviderDescriptor<TContract>>();
        var contractType = typeof(TContract);

        _logger?.LogDebug("Starting provider discovery for contract type: {ContractType}", contractType.Name);

        foreach (var assembly in assemblies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var assemblyProviders = await DiscoverProvidersFromAssemblyAsync<TContract>(assembly, cancellationToken);
                providers.AddRange(assemblyProviders);

                _logger?.LogDebug("Found {Count} providers in assembly {AssemblyName}", 
                    assemblyProviders.Count, assembly.GetName().Name);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to discover providers from assembly {AssemblyName}", 
                    assembly.GetName().Name);
            }
        }

        _logger?.LogInformation("Provider discovery completed. Found {TotalCount} providers for contract {ContractType}", 
            providers.Count, contractType.Name);

        return providers.AsReadOnly();
    }

    /// <summary>
    /// Discovers providers from a single assembly.
    /// </summary>
    /// <typeparam name="TContract">The contract type to discover providers for.</typeparam>
    /// <param name="assembly">Assembly to scan for providers.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns a collection of discovered provider descriptors.</returns>
    public async Task<IReadOnlyList<ProviderDescriptor<TContract>>> DiscoverProvidersFromAssemblyAsync<TContract>(
        Assembly assembly,
        CancellationToken cancellationToken = default) where TContract : class
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var providers = new List<ProviderDescriptor<TContract>>();
        var contractType = typeof(TContract);

        try
        {
            var types = assembly.GetTypes();
            
            foreach (var type in types)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!IsValidProviderType<TContract>(type))
                    continue;

                try
                {
                    var descriptor = await CreateProviderDescriptorAsync<TContract>(type, cancellationToken);
                    if (descriptor != null)
                    {
                        providers.Add(descriptor);
                        _logger?.LogDebug("Discovered provider: {ProviderName} ({TypeName})", 
                            descriptor.Metadata.Name, type.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to create provider descriptor for type {TypeName}", type.Name);
                }
            }
        }
        catch (ReflectionTypeLoadException ex)
        {
            _logger?.LogWarning(ex, "Failed to load types from assembly {AssemblyName}", assembly.GetName().Name);
            
            // Process successfully loaded types
            foreach (var type in ex.Types.Where(t => t != null))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!IsValidProviderType<TContract>(type!))
                    continue;

                try
                {
                    var descriptor = await CreateProviderDescriptorAsync<TContract>(type!, cancellationToken);
                    if (descriptor != null)
                    {
                        providers.Add(descriptor);
                    }
                }
                catch (Exception descriptorEx)
                {
                    _logger?.LogWarning(descriptorEx, "Failed to create provider descriptor for type {TypeName}", type!.Name);
                }
            }
        }

        return providers.AsReadOnly();
    }

    /// <summary>
    /// Discovers providers from all assemblies in the specified directory.
    /// </summary>
    /// <typeparam name="TContract">The contract type to discover providers for.</typeparam>
    /// <param name="directoryPath">Directory path to scan for assemblies.</param>
    /// <param name="searchPattern">Search pattern for assembly files. Defaults to "*.dll".</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns a collection of discovered provider descriptors.</returns>
    public async Task<IReadOnlyList<ProviderDescriptor<TContract>>> DiscoverProvidersFromDirectoryAsync<TContract>(
        string directoryPath,
        string searchPattern = "*.dll",
        CancellationToken cancellationToken = default) where TContract : class
    {
        ArgumentException.ThrowIfNullOrEmpty(directoryPath);

        if (!Directory.Exists(directoryPath))
        {
            _logger?.LogWarning("Directory does not exist: {DirectoryPath}", directoryPath);
            return Array.Empty<ProviderDescriptor<TContract>>();
        }

        var assemblyFiles = Directory.GetFiles(directoryPath, searchPattern, SearchOption.TopDirectoryOnly);
        var assemblies = new List<Assembly>();

        foreach (var assemblyFile in assemblyFiles)
        {
            try
            {
                var assembly = Assembly.LoadFrom(assemblyFile);
                assemblies.Add(assembly);
                _logger?.LogDebug("Loaded assembly: {AssemblyPath}", assemblyFile);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to load assembly from {AssemblyPath}", assemblyFile);
            }
        }

        return await DiscoverProvidersAsync<TContract>(assemblies, cancellationToken);
    }

    /// <summary>
    /// Checks if a type is a valid provider implementation for the specified contract.
    /// </summary>
    /// <typeparam name="TContract">The contract type.</typeparam>
    /// <param name="type">The type to validate.</param>
    /// <returns>True if the type is a valid provider; otherwise, false.</returns>
    private static bool IsValidProviderType<TContract>(Type type) where TContract : class
    {
        var contractType = typeof(TContract);

        return type.IsClass &&
               !type.IsAbstract &&
               !type.IsInterface &&
               contractType.IsAssignableFrom(type) &&
               HasParameterlessConstructor(type);
    }

    /// <summary>
    /// Checks if a type has a parameterless constructor or a constructor that can be resolved by dependency injection.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type has a suitable constructor; otherwise, false.</returns>
    private static bool HasParameterlessConstructor(Type type)
    {
        return type.GetConstructors().Any(c => c.GetParameters().Length == 0) ||
               type.GetConstructors().Any(c => c.GetParameters().All(p => p.HasDefaultValue));
    }

    /// <summary>
    /// Creates a provider descriptor for the specified type.
    /// </summary>
    /// <typeparam name="TContract">The contract type.</typeparam>
    /// <param name="type">The provider implementation type.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns a provider descriptor, or null if creation failed.</returns>
    private Task<ProviderDescriptor<TContract>?> CreateProviderDescriptorAsync<TContract>(
        Type type,
        CancellationToken cancellationToken = default) where TContract : class
    {
        try
        {
            // Look for ProviderAttribute to get metadata
            var providerAttribute = type.GetCustomAttribute<ProviderAttribute>();
            if (providerAttribute == null)
            {
                _logger?.LogDebug("Type {TypeName} does not have ProviderAttribute, skipping", type.Name);
                return Task.FromResult<ProviderDescriptor<TContract>?>(null);
            }

            var metadata = new ProviderMetadata(
                Name: providerAttribute.Name ?? type.Name,
                Version: Version.Parse(providerAttribute.Version ?? "1.0.0"),
                Priority: providerAttribute.Priority,
                Capabilities: providerAttribute.Capabilities?.ToHashSet() ?? new HashSet<string>(),
                SupportedPlatforms: providerAttribute.SupportedPlatforms,
                Description: providerAttribute.Description,
                Author: providerAttribute.Author,
                Dependencies: providerAttribute.Dependencies?.ToDictionary(
                    kvp => kvp.Key,
                    kvp => Version.Parse(kvp.Value)) ?? new Dictionary<string, Version>());

            return Task.FromResult<ProviderDescriptor<TContract>?>(new ProviderDescriptor<TContract>(type, metadata));
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to create provider descriptor for type {TypeName}", type.Name);
            return Task.FromResult<ProviderDescriptor<TContract>?>(null);
        }
    }
}

/// <summary>
/// Descriptor for a discovered provider containing its implementation type and metadata.
/// </summary>
/// <typeparam name="TContract">The contract type the provider implements.</typeparam>
/// <param name="ImplementationType">The concrete implementation type of the provider.</param>
/// <param name="Metadata">Metadata describing the provider's capabilities and requirements.</param>
public record ProviderDescriptor<TContract>(Type ImplementationType, ProviderMetadata Metadata) where TContract : class;

/// <summary>
/// Attribute to mark a class as a provider and specify its metadata.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ProviderAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the friendly name of the provider.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the version of the provider.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the priority of the provider for selection.
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Gets or sets the capabilities supported by the provider.
    /// </summary>
    public string[]? Capabilities { get; set; }

    /// <summary>
    /// Gets or sets the platforms supported by the provider.
    /// </summary>
    public Platform SupportedPlatforms { get; set; } = Platform.All;

    /// <summary>
    /// Gets or sets the description of the provider.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the author of the provider.
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Gets or sets the dependencies of the provider as name-version pairs.
    /// </summary>
    public Dictionary<string, string>? Dependencies { get; set; }
}