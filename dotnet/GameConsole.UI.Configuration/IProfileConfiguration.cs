using Microsoft.Extensions.Configuration;

namespace GameConsole.UI.Configuration;

/// <summary>
/// Defines the configuration settings for a UI profile with validation and inheritance support.
/// </summary>
public interface IProfileConfiguration
{
    /// <summary>
    /// Gets the unique identifier for this profile configuration.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the display name of the profile configuration.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of what this profile configuration provides.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the version of this configuration schema.
    /// </summary>
    Version Version { get; }

    /// <summary>
    /// Gets the environment this configuration is designed for (Development, Staging, Production).
    /// </summary>
    string Environment { get; }

    /// <summary>
    /// Gets the profile this configuration inherits from, if any.
    /// </summary>
    string? ParentProfileId { get; }

    /// <summary>
    /// Gets the configuration data as an IConfiguration instance.
    /// </summary>
    IConfiguration Configuration { get; }

    /// <summary>
    /// Gets configuration metadata including creation time, author, and tags.
    /// </summary>
    IReadOnlyDictionary<string, object> Metadata { get; }

    /// <summary>
    /// Validates this configuration against the profile requirements.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A validation result indicating success or failure with details.</returns>
    Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a strongly-typed configuration section.
    /// </summary>
    /// <typeparam name="T">The type to bind the configuration to.</typeparam>
    /// <param name="sectionPath">The path to the configuration section.</param>
    /// <returns>The bound configuration object.</returns>
    T GetSection<T>(string sectionPath) where T : class, new();
}