namespace GameConsole.Core.Abstractions;

/// <summary>
/// Unique identifier for an actor cluster.
/// </summary>
public readonly struct ClusterId : IEquatable<ClusterId>
{
    /// <summary>
    /// The unique identifier value for the cluster.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Initializes a new ClusterId with the specified GUID value.
    /// </summary>
    /// <param name="value">The unique identifier value.</param>
    public ClusterId(Guid value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new random ClusterId.
    /// </summary>
    /// <returns>A new ClusterId with a random GUID value.</returns>
    public static ClusterId NewId() => new(Guid.NewGuid());

    /// <summary>
    /// Creates a ClusterId from a string representation.
    /// </summary>
    /// <param name="value">String representation of the GUID.</param>
    /// <returns>ClusterId if parsing succeeds.</returns>
    /// <exception cref="FormatException">Thrown if value is not a valid GUID.</exception>
    public static ClusterId FromString(string value) => new(Guid.Parse(value));

    /// <inheritdoc/>
    public bool Equals(ClusterId other) => Value.Equals(other.Value);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ClusterId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode();

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();

    /// <summary>
    /// Equality operator for ClusterId comparison.
    /// </summary>
    public static bool operator ==(ClusterId left, ClusterId right) => left.Equals(right);

    /// <summary>
    /// Inequality operator for ClusterId comparison.
    /// </summary>
    public static bool operator !=(ClusterId left, ClusterId right) => !(left == right);

    /// <summary>
    /// Implicit conversion from Guid to ClusterId.
    /// </summary>
    public static implicit operator ClusterId(Guid value) => new(value);

    /// <summary>
    /// Implicit conversion from ClusterId to Guid.
    /// </summary>
    public static implicit operator Guid(ClusterId clusterId) => clusterId.Value;
}

/// <summary>
/// Information about an actor cluster membership change.
/// </summary>
public class ClusterMembershipEventArgs : EventArgs
{
    /// <summary>
    /// The cluster involved in the membership change.
    /// </summary>
    public ClusterId ClusterId { get; }

    /// <summary>
    /// The actor whose membership changed.
    /// </summary>
    public ActorId ActorId { get; }

    /// <summary>
    /// True if actor joined, false if actor left.
    /// </summary>
    public bool IsJoining { get; }

    /// <summary>
    /// Initializes new cluster membership event args.
    /// </summary>
    public ClusterMembershipEventArgs(ClusterId clusterId, ActorId actorId, bool isJoining)
    {
        ClusterId = clusterId;
        ActorId = actorId;
        IsJoining = isJoining;
    }
}

/// <summary>
/// Defines the interface for actor clusters in the GameConsole system.
/// Clusters manage groups of related actors and provide coordination services.
/// </summary>
public interface IActorCluster : IService
{
    /// <summary>
    /// Unique identifier for this cluster.
    /// </summary>
    ClusterId Id { get; }

    /// <summary>
    /// Gets the name of this cluster for identification.
    /// </summary>
    string ClusterName { get; }

    /// <summary>
    /// Gets the type of actors this cluster manages.
    /// </summary>
    string ActorType { get; }

    /// <summary>
    /// Gets the current number of actors in this cluster.
    /// </summary>
    int MemberCount { get; }

    /// <summary>
    /// Gets the IDs of all actors currently in this cluster.
    /// </summary>
    Task<IEnumerable<ActorId>> GetMemberIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers an actor with this cluster.
    /// </summary>
    /// <param name="actor">The actor to register.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task that completes when registration is successful.</returns>
    Task RegisterActorAsync(IActor actor, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters an actor from this cluster.
    /// </summary>
    /// <param name="actorId">ID of the actor to unregister.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task that completes when unregistration is successful.</returns>
    Task UnregisterActorAsync(ActorId actorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an actor is registered with this cluster.
    /// </summary>
    /// <param name="actorId">ID of the actor to check.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the actor is in this cluster.</returns>
    Task<bool> HasMemberAsync(ActorId actorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts a message to all actors in this cluster.
    /// </summary>
    /// <param name="message">The message to broadcast.</param>
    /// <param name="excludeActor">Optional actor ID to exclude from broadcast.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task that completes when all messages are queued.</returns>
    Task BroadcastMessageAsync(IActorMessage message, ActorId? excludeActor = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message to a specific actor in this cluster.
    /// </summary>
    /// <param name="actorId">Target actor ID.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task that completes when message is queued, or fails if actor not in cluster.</returns>
    Task SendMessageToActorAsync(ActorId actorId, IActorMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event fired when actors join or leave the cluster.
    /// </summary>
    event EventHandler<ClusterMembershipEventArgs>? MembershipChanged;
}