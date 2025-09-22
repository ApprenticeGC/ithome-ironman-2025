namespace GameConsole.ECS.Core;

/// <summary>
/// Lightweight entity implementation with unique identifier.
/// Entities are immutable value types for optimal performance.
/// </summary>
public readonly struct Entity : IEntity, IEquatable<Entity>
{
    private static uint _nextId = 1;
    private static readonly object _idLock = new();

    /// <summary>
    /// Unique identifier for this entity within its world.
    /// </summary>
    public uint Id { get; }

    /// <summary>
    /// Indicates whether this entity has a valid identifier.
    /// </summary>
    public bool IsValid => Id != 0;

    /// <summary>
    /// Creates a new entity with the specified identifier.
    /// </summary>
    /// <param name="id">The unique identifier for this entity.</param>
    public Entity(uint id)
    {
        Id = id;
    }

    /// <summary>
    /// Creates a new entity with a globally unique identifier.
    /// </summary>
    /// <returns>A new entity with a unique identifier.</returns>
    public static Entity Create()
    {
        lock (_idLock)
        {
            return new Entity(_nextId++);
        }
    }

    /// <summary>
    /// Returns an invalid entity with ID 0.
    /// </summary>
    public static Entity None => new(0);

    public bool Equals(Entity other) => Id == other.Id;

    public override bool Equals(object? obj) => obj is Entity entity && Equals(entity);

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity left, Entity right) => left.Equals(right);

    public static bool operator !=(Entity left, Entity right) => !(left == right);

    public override string ToString() => $"Entity({Id})";
}