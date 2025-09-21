namespace GameConsole.ECS.Core;

/// <summary>
/// Represents a game entity with a unique identifier and lifecycle state.
/// Entities are containers for components and are managed by the ECS world.
/// </summary>
public interface IEntity
{
    /// <summary>
    /// Unique identifier for this entity.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets a value indicating whether this entity is alive and active in the world.
    /// </summary>
    bool IsAlive { get; }

    /// <summary>
    /// Gets the generation number of this entity.
    /// Used to detect when an entity ID has been reused after destruction.
    /// </summary>
    uint Generation { get; }

    /// <summary>
    /// Gets the world that owns this entity.
    /// </summary>
    IECSWorld World { get; }
}