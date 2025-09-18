namespace GameConsole.AI.Core;

/// <summary>
/// Marker interface for AI capabilities that can be provided by AI agents.
/// Implement this interface to define specific AI skills and abilities
/// that can be discovered and used by the capability provider system.
/// </summary>
public interface IAICapability
{
    /// <summary>
    /// Gets the unique identifier for this capability.
    /// Used for capability discovery and registration.
    /// </summary>
    string CapabilityId { get; }

    /// <summary>
    /// Gets the human-readable name of this capability.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a description of what this capability does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the version of this capability implementation.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Validates whether this capability can be used with the given context.
    /// </summary>
    /// <param name="context">The AI execution context to validate against.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that represents the async validation operation. Returns true if the capability is compatible.</returns>
    Task<bool> ValidateAsync(IAIContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the resource requirements for this capability.
    /// </summary>
    /// <returns>The resource requirements, or null if no specific requirements.</returns>
    AIResourceRequirements? GetResourceRequirements();
}