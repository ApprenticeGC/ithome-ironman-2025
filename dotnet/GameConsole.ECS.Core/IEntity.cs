using GameConsole.Core.Abstractions;

namespace GameConsole.ECS.Core;

/// <summary>
/// Represents a game entity with a unique identifier.
/// Entities are containers for components and serve as the primary objects in the ECS system.
/// </summary>
public interface IEntity
{
    /// <summary>
    /// Unique identifier for this entity within its world.
    /// </summary>
    uint Id { get; }

    /// <summary>
    /// Indicates whether this entity is currently active in the world.
    /// </summary>
    bool IsValid { get; }
}