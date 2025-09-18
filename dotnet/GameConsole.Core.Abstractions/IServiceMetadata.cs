namespace GameConsole.Core.Abstractions;

/// <summary>
/// Interface for providing metadata information about a service.
/// This allows services to expose descriptive information for documentation,
/// logging, and service discovery purposes.
/// </summary>
public interface IServiceMetadata
{
    /// <summary>
    /// Gets the human-readable name of the service.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the version of the service implementation.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets a brief description of what the service does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the categories or tags associated with this service.
    /// Used for grouping and filtering services in service discovery.
    /// </summary>
    IEnumerable<string> Categories { get; }

    /// <summary>
    /// Gets additional metadata properties as key-value pairs.
    /// Can be used to store service-specific configuration or information.
    /// </summary>
    IReadOnlyDictionary<string, object> Properties { get; }
}
