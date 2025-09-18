using GameConsole.Core.Abstractions;

namespace GameConsole.Engine.Core;

/// <summary>
/// Resource loading priority levels.
/// </summary>
public enum ResourceLoadPriority
{
    /// <summary>
    /// Low priority loading that won't block the main thread.
    /// </summary>
    Low,
    
    /// <summary>
    /// Normal priority loading.
    /// </summary>
    Normal,
    
    /// <summary>
    /// High priority loading that should be completed as quickly as possible.
    /// </summary>
    High,
    
    /// <summary>
    /// Critical priority loading that blocks until complete.
    /// </summary>
    Critical
}

/// <summary>
/// Resource loading modes.
/// </summary>
public enum ResourceLoadMode
{
    /// <summary>
    /// Load immediately and block until complete.
    /// </summary>
    Immediate,
    
    /// <summary>
    /// Load asynchronously in the background.
    /// </summary>
    Async,
    
    /// <summary>
    /// Stream the resource as needed.
    /// </summary>
    Streaming
}

/// <summary>
/// Arguments for resource-related events.
/// </summary>
public class ResourceEventArgs : EventArgs
{
    /// <summary>
    /// The identifier of the resource.
    /// </summary>
    public string ResourceId { get; }
    
    /// <summary>
    /// The type of the resource.
    /// </summary>
    public Type ResourceType { get; }
    
    /// <summary>
    /// Optional additional data about the resource event.
    /// </summary>
    public object? Data { get; }

    /// <summary>
    /// Initializes a new instance of the ResourceEventArgs class.
    /// </summary>
    /// <param name="resourceId">The identifier of the resource.</param>
    /// <param name="resourceType">The type of the resource.</param>
    /// <param name="data">Optional additional data about the resource event.</param>
    public ResourceEventArgs(string resourceId, Type resourceType, object? data = null)
    {
        ResourceId = resourceId ?? throw new ArgumentNullException(nameof(resourceId));
        ResourceType = resourceType ?? throw new ArgumentNullException(nameof(resourceType));
        Data = data;
    }
}

/// <summary>
/// Represents a resource dependency relationship.
/// </summary>
public class ResourceDependency
{
    /// <summary>
    /// The identifier of the dependent resource.
    /// </summary>
    public string ResourceId { get; }
    
    /// <summary>
    /// The type of the dependent resource.
    /// </summary>
    public Type ResourceType { get; }
    
    /// <summary>
    /// Whether this dependency is required or optional.
    /// </summary>
    public bool IsRequired { get; }

    /// <summary>
    /// Initializes a new instance of the ResourceDependency class.
    /// </summary>
    /// <param name="resourceId">The identifier of the dependent resource.</param>
    /// <param name="resourceType">The type of the dependent resource.</param>
    /// <param name="isRequired">Whether this dependency is required or optional.</param>
    public ResourceDependency(string resourceId, Type resourceType, bool isRequired = true)
    {
        ResourceId = resourceId ?? throw new ArgumentNullException(nameof(resourceId));
        ResourceType = resourceType ?? throw new ArgumentNullException(nameof(resourceType));
        IsRequired = isRequired;
    }
}

/// <summary>
/// Tier 2: Resource manager service interface for async asset loading with dependency resolution.
/// Handles resource lifecycle, caching, dependency graphs, and streaming to support
/// efficient asset management across different platforms.
/// </summary>
public interface IResourceManager : IService
{
    /// <summary>
    /// Event raised when a resource starts loading.
    /// </summary>
    event EventHandler<ResourceEventArgs>? ResourceLoadStarted;
    
    /// <summary>
    /// Event raised when a resource completes loading.
    /// </summary>
    event EventHandler<ResourceEventArgs>? ResourceLoadCompleted;
    
    /// <summary>
    /// Event raised when a resource fails to load.
    /// </summary>
    event EventHandler<ResourceEventArgs>? ResourceLoadFailed;
    
    /// <summary>
    /// Event raised when a resource is unloaded.
    /// </summary>
    event EventHandler<ResourceEventArgs>? ResourceUnloaded;

    /// <summary>
    /// Loads a resource asynchronously with the specified priority and mode.
    /// </summary>
    /// <typeparam name="T">The type of resource to load.</typeparam>
    /// <param name="resourceId">The identifier of the resource to load.</param>
    /// <param name="priority">The priority level for loading the resource.</param>
    /// <param name="mode">The loading mode for the resource.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async resource loading operation.</returns>
    Task<T?> LoadResourceAsync<T>(string resourceId, ResourceLoadPriority priority = ResourceLoadPriority.Normal, 
        ResourceLoadMode mode = ResourceLoadMode.Async, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Loads a resource with its dependencies using the dependency graph.
    /// </summary>
    /// <typeparam name="T">The type of resource to load.</typeparam>
    /// <param name="resourceId">The identifier of the resource to load.</param>
    /// <param name="dependencies">The dependency graph for the resource.</param>
    /// <param name="priority">The priority level for loading the resource and its dependencies.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async resource loading operation with dependencies.</returns>
    Task<T?> LoadWithDependenciesAsync<T>(string resourceId, IEnumerable<ResourceDependency> dependencies, 
        ResourceLoadPriority priority = ResourceLoadPriority.Normal, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Unloads a resource and optionally its dependencies.
    /// </summary>
    /// <param name="resourceId">The identifier of the resource to unload.</param>
    /// <param name="unloadDependencies">Whether to unload dependencies that are no longer referenced.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async resource unloading operation.</returns>
    Task UnloadResourceAsync(string resourceId, bool unloadDependencies = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Preloads a resource in the background without returning it.
    /// </summary>
    /// <typeparam name="T">The type of resource to preload.</typeparam>
    /// <param name="resourceId">The identifier of the resource to preload.</param>
    /// <param name="priority">The priority level for preloading the resource.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async resource preloading operation.</returns>
    Task PreloadResourceAsync<T>(string resourceId, ResourceLoadPriority priority = ResourceLoadPriority.Low, 
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Checks if a resource is currently loaded in memory.
    /// </summary>
    /// <param name="resourceId">The identifier of the resource to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns true if the resource is loaded.</returns>
    Task<bool> IsResourceLoadedAsync(string resourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the dependencies of a specified resource.
    /// </summary>
    /// <param name="resourceId">The identifier of the resource.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the resource dependencies.</returns>
    Task<IEnumerable<ResourceDependency>> GetResourceDependenciesAsync(string resourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all resources that depend on the specified resource.
    /// </summary>
    /// <param name="resourceId">The identifier of the resource.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns resources that depend on this resource.</returns>
    Task<IEnumerable<string>> GetDependentResourcesAsync(string resourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current memory usage of loaded resources in bytes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the memory usage in bytes.</returns>
    Task<long> GetMemoryUsageAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs garbage collection of unused resources based on reference counting.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async garbage collection operation.</returns>
    Task CollectUnusedResourcesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the maximum memory limit for cached resources.
    /// </summary>
    /// <param name="maxMemoryBytes">The maximum memory limit in bytes.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetMemoryLimitAsync(long maxMemoryBytes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all currently loaded resource identifiers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns loaded resource identifiers.</returns>
    Task<IEnumerable<string>> GetLoadedResourcesAsync(CancellationToken cancellationToken = default);
}