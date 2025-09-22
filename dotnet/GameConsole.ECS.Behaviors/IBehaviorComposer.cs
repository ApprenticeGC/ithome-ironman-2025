namespace GameConsole.ECS.Behaviors;

/// <summary>
/// Interface for composing multiple components into cohesive behaviors.
/// Enables combining individual ECS components into higher-level behavior abstractions.
/// </summary>
public interface IBehaviorComposer
{
    /// <summary>
    /// Creates a behavior by composing multiple components together.
    /// </summary>
    /// <typeparam name="T">The type of behavior to create.</typeparam>
    /// <param name="components">The components to compose into the behavior.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the composed behavior.</returns>
    Task<T> ComposeBehaviorAsync<T>(IEnumerable<object> components, CancellationToken cancellationToken = default) 
        where T : class, IBehavior;

    /// <summary>
    /// Validates that the given components can be composed into a valid behavior.
    /// </summary>
    /// <param name="behaviorType">The type of behavior to validate for.</param>
    /// <param name="components">The components to validate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns true if the composition is valid.</returns>
    Task<bool> ValidateCompositionAsync(Type behaviorType, IEnumerable<object> components, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decomposes a behavior back into its constituent components.
    /// </summary>
    /// <param name="behavior">The behavior to decompose.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the components.</returns>
    Task<IEnumerable<object>> DecomposeBehaviorAsync(IBehavior behavior, CancellationToken cancellationToken = default);

    /// <summary>
    /// Modifies an existing behavior by adding, removing, or updating components at runtime.
    /// </summary>
    /// <param name="behavior">The behavior to modify.</param>
    /// <param name="modificationType">The type of modification to perform.</param>
    /// <param name="component">The component to add, remove, or update.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the modified behavior.</returns>
    Task<IBehavior> ModifyBehaviorAsync(IBehavior behavior, BehaviorModificationType modificationType, object component, CancellationToken cancellationToken = default);
}