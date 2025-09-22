using GameConsole.Core.Abstractions;

namespace GameConsole.Core.Abstractions;

/// <summary>
/// Unique identifier for an actor in the system.
/// Provides strongly-typed identity with equality comparison.
/// </summary>
public readonly struct ActorId : IEquatable<ActorId>
{
    /// <summary>
    /// The unique identifier value for the actor.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Initializes a new ActorId with the specified GUID value.
    /// </summary>
    /// <param name="value">The unique identifier value.</param>
    public ActorId(Guid value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new random ActorId.
    /// </summary>
    /// <returns>A new ActorId with a random GUID value.</returns>
    public static ActorId NewId() => new(Guid.NewGuid());

    /// <summary>
    /// Creates an ActorId from a string representation.
    /// </summary>
    /// <param name="value">String representation of the GUID.</param>
    /// <returns>ActorId if parsing succeeds.</returns>
    /// <exception cref="FormatException">Thrown if value is not a valid GUID.</exception>
    public static ActorId FromString(string value) => new(Guid.Parse(value));

    /// <inheritdoc/>
    public bool Equals(ActorId other) => Value.Equals(other.Value);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ActorId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode();

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();

    /// <summary>
    /// Equality operator for ActorId comparison.
    /// </summary>
    public static bool operator ==(ActorId left, ActorId right) => left.Equals(right);

    /// <summary>
    /// Inequality operator for ActorId comparison.
    /// </summary>
    public static bool operator !=(ActorId left, ActorId right) => !(left == right);

    /// <summary>
    /// Implicit conversion from Guid to ActorId.
    /// </summary>
    public static implicit operator ActorId(Guid value) => new(value);

    /// <summary>
    /// Implicit conversion from ActorId to Guid.
    /// </summary>
    public static implicit operator Guid(ActorId actorId) => actorId.Value;
}