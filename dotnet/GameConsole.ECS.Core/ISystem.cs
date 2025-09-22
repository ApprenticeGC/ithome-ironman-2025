using GameConsole.Engine.Core;

namespace GameConsole.ECS.Core;

/// <summary>
/// Represents a system that processes entities and their components.
/// Systems contain the logic for operating on component data.
/// </summary>
public interface ISystem
{
    /// <summary>
    /// Update priority for system execution order.
    /// Systems with higher priority execute before systems with lower priority.
    /// </summary>
    UpdatePriority Priority { get; }

    /// <summary>
    /// Indicates whether this system is currently enabled for execution.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Process entities and components within the given world.
    /// </summary>
    /// <param name="world">The ECS world to process.</param>
    /// <param name="deltaTime">Time elapsed since the last update in seconds.</param>
    void Update(IECSWorld world, float deltaTime);
}