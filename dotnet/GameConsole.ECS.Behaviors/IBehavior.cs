namespace GameConsole.ECS.Behaviors;

/// <summary>
/// Base interface for all behaviors in the ECS behavior composition system.
/// Behaviors are high-level abstractions composed of multiple ECS components.
/// </summary>
public interface IBehavior
{
    /// <summary>
    /// Gets the unique identifier for this behavior instance.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the name of this behavior.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the current state of the behavior.
    /// </summary>
    BehaviorState State { get; }

    /// <summary>
    /// Gets all components that make up this behavior.
    /// </summary>
    IReadOnlyCollection<object> Components { get; }

    /// <summary>
    /// Gets metadata about the behavior including dependencies and capabilities.
    /// </summary>
    IBehaviorMetadata Metadata { get; }

    /// <summary>
    /// Activates the behavior, making it start processing.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ActivateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates the behavior, stopping its processing.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task DeactivateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the behavior state. Called by the behavior system each frame/tick.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task UpdateAsync(TimeSpan deltaTime, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the current state of a behavior.
/// </summary>
public enum BehaviorState
{
    /// <summary>
    /// The behavior is inactive and not processing.
    /// </summary>
    Inactive,

    /// <summary>
    /// The behavior is active and processing normally.
    /// </summary>
    Active,

    /// <summary>
    /// The behavior is paused but can be resumed.
    /// </summary>
    Paused,

    /// <summary>
    /// The behavior has encountered an error and is in a faulted state.
    /// </summary>
    Faulted,

    /// <summary>
    /// The behavior is in the process of being disposed.
    /// </summary>
    Disposing
}

/// <summary>
/// Metadata about a behavior's dependencies and capabilities.
/// </summary>
public interface IBehaviorMetadata
{
    /// <summary>
    /// Gets the component types required by this behavior.
    /// </summary>
    IReadOnlyCollection<Type> RequiredComponents { get; }

    /// <summary>
    /// Gets the component types that are optional for this behavior.
    /// </summary>
    IReadOnlyCollection<Type> OptionalComponents { get; }

    /// <summary>
    /// Gets the component types that conflict with this behavior.
    /// </summary>
    IReadOnlyCollection<Type> ConflictingComponents { get; }

    /// <summary>
    /// Gets the behaviors that this behavior depends on.
    /// </summary>
    IReadOnlyCollection<Type> Dependencies { get; }

    /// <summary>
    /// Gets the tags associated with this behavior for categorization.
    /// </summary>
    IReadOnlyCollection<string> Tags { get; }

    /// <summary>
    /// Gets custom properties for this behavior.
    /// </summary>
    IReadOnlyDictionary<string, object> Properties { get; }
}