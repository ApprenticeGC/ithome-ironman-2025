using System.Numerics;
using GameConsole.Core.Abstractions;

namespace GameConsole.Engine.Core;

/// <summary>
/// Base implementation of an actor that provides common functionality.
/// This class can be used as a base for game entities that need basic actor behavior.
/// </summary>
public abstract class BaseActor : IActor
{
    /// <summary>
    /// Gets the unique identifier for this actor.
    /// </summary>
    public string Id { get; }
    
    /// <summary>
    /// Gets or sets the position of the actor in world space.
    /// </summary>
    public System.Numerics.Vector3 Position { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether this actor is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the BaseActor class.
    /// </summary>
    /// <param name="id">The unique identifier for the actor.</param>
    /// <param name="initialPosition">The initial position of the actor.</param>
    protected BaseActor(string id, System.Numerics.Vector3 initialPosition = default)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Position = initialPosition;
    }

    /// <summary>
    /// Updates the actor state asynchronously.
    /// Derived classes should override this to implement specific update logic.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update, in seconds.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async update operation.</returns>
    public virtual Task UpdateAsync(float deltaTime, CancellationToken cancellationToken = default)
    {
        // Base implementation does nothing - derived classes should override
        return Task.CompletedTask;
    }
}