using GameConsole.Core.Abstractions;

namespace GameConsole.ECS.Core;

/// <summary>
/// Interface for systems that process entities and components.
/// Systems contain the logic that operates on entity data.
/// </summary>
public interface ISystem : IService
{
    /// <summary>
    /// Updates the system for the current frame.
    /// </summary>
    /// <param name="world">The ECS world to operate on.</param>
    /// <param name="deltaTime">Time elapsed since last update in seconds.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async update operation.</returns>
    Task UpdateAsync(IECSWorld world, float deltaTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the execution priority of this system.
    /// Systems with lower priority values execute first.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Gets a value indicating whether this system should execute in parallel.
    /// Parallel systems can run concurrently with other parallel systems of the same priority.
    /// </summary>
    bool CanExecuteInParallel { get; }

    /// <summary>
    /// Gets the component types that this system processes.
    /// Used for dependency tracking and optimization.
    /// </summary>
    IReadOnlySet<Type> ComponentTypes { get; }
}