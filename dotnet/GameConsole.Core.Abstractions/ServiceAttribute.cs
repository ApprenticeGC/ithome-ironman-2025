namespace GameConsole.Core.Abstractions;

/// <summary>
/// Attribute used to decorate service implementations with metadata.
/// This attribute enables declarative service registration and provides
/// metadata for service discovery and documentation purposes.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class ServiceAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceAttribute"/> class.
    /// </summary>
    /// <param name="name">The human-readable name of the service.</param>
    /// <param name="version">The version of the service implementation.</param>
    /// <param name="description">A brief description of what the service does.</param>
    public ServiceAttribute(string name, string version = "1.0.0", string description = "")
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Version = version ?? throw new ArgumentNullException(nameof(version));
        Description = description ?? string.Empty;
    }

    /// <summary>
    /// Gets the human-readable name of the service.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the version of the service implementation.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets a brief description of what the service does.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets or sets the categories or tags associated with this service.
    /// Used for grouping and filtering services in service discovery.
    /// </summary>
    public string[] Categories { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the service lifetime for dependency injection.
    /// </summary>
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Scoped;
}

/// <summary>
/// Defines the lifetime of a service in the dependency injection container.
/// </summary>
public enum ServiceLifetime
{
    /// <summary>
    /// A new instance is created for each request.
    /// </summary>
    Transient,

    /// <summary>
    /// A single instance is created and shared within a scope.
    /// </summary>
    Scoped,

    /// <summary>
    /// A single instance is created and shared throughout the application lifetime.
    /// </summary>
    Singleton
}
