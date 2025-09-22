using System.Numerics;

namespace GameConsole.Core.Abstractions;

/// <summary>
/// Base interface for all actors in the GameConsole system.
/// Actors are entities that can be positioned, updated, and managed within the game world.
/// </summary>
public interface IActor
{
    /// <summary>
    /// Gets the unique identifier for this actor.
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Gets or sets the position of the actor in world space.
    /// </summary>
    Vector3 Position { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether this actor is currently active.
    /// Inactive actors may be skipped during processing.
    /// </summary>
    bool IsActive { get; set; }
    
    /// <summary>
    /// Updates the actor state asynchronously.
    /// This method is called every frame to update the actor's logic and state.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update, in seconds.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async update operation.</returns>
    Task UpdateAsync(float deltaTime, CancellationToken cancellationToken = default);
}