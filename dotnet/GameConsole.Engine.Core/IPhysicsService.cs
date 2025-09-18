using GameConsole.Core.Abstractions;

namespace GameConsole.Engine.Core;

/// <summary>
/// Physics simulation step modes.
/// </summary>
public enum PhysicsStepMode
{
    /// <summary>
    /// Automatic stepping based on the frame rate.
    /// </summary>
    Automatic,
    
    /// <summary>
    /// Manual stepping controlled by the application.
    /// </summary>
    Manual,
    
    /// <summary>
    /// Fixed timestep stepping independent of frame rate.
    /// </summary>
    FixedTimestep
}

/// <summary>
/// Physics body types.
/// </summary>
public enum PhysicsBodyType
{
    /// <summary>
    /// Static body that doesn't move.
    /// </summary>
    Static,
    
    /// <summary>
    /// Kinematic body that moves but isn't affected by forces.
    /// </summary>
    Kinematic,
    
    /// <summary>
    /// Dynamic body that is fully simulated with forces.
    /// </summary>
    Dynamic
}

/// <summary>
/// Represents a 3D vector for physics calculations.
/// </summary>
public struct Vector3
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public Vector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static Vector3 Zero => new(0, 0, 0);
    public static Vector3 One => new(1, 1, 1);
    public static Vector3 Up => new(0, 1, 0);
}

/// <summary>
/// Represents a raycast hit result.
/// </summary>
public class RaycastHit
{
    /// <summary>
    /// The point where the ray hit the collider.
    /// </summary>
    public Vector3 Point { get; set; }
    
    /// <summary>
    /// The normal vector at the hit point.
    /// </summary>
    public Vector3 Normal { get; set; }
    
    /// <summary>
    /// The distance from the ray origin to the hit point.
    /// </summary>
    public float Distance { get; set; }
    
    /// <summary>
    /// The identifier of the collider that was hit.
    /// </summary>
    public string ColliderId { get; set; } = string.Empty;
    
    /// <summary>
    /// The identifier of the physics body that was hit.
    /// </summary>
    public string BodyId { get; set; } = string.Empty;
}

/// <summary>
/// Represents a collision event between two physics bodies.
/// </summary>
public class CollisionEventArgs : EventArgs
{
    /// <summary>
    /// The identifier of the first body in the collision.
    /// </summary>
    public string BodyA { get; }
    
    /// <summary>
    /// The identifier of the second body in the collision.
    /// </summary>
    public string BodyB { get; }
    
    /// <summary>
    /// The collision point.
    /// </summary>
    public Vector3 ContactPoint { get; }
    
    /// <summary>
    /// The collision normal.
    /// </summary>
    public Vector3 ContactNormal { get; }
    
    /// <summary>
    /// The impulse magnitude of the collision.
    /// </summary>
    public float Impulse { get; }

    /// <summary>
    /// Initializes a new instance of the CollisionEventArgs class.
    /// </summary>
    public CollisionEventArgs(string bodyA, string bodyB, Vector3 contactPoint, Vector3 contactNormal, float impulse)
    {
        BodyA = bodyA ?? throw new ArgumentNullException(nameof(bodyA));
        BodyB = bodyB ?? throw new ArgumentNullException(nameof(bodyB));
        ContactPoint = contactPoint;
        ContactNormal = contactNormal;
        Impulse = impulse;
    }
}

/// <summary>
/// Physics simulation statistics.
/// </summary>
public class PhysicsStatistics
{
    /// <summary>
    /// The number of active physics bodies.
    /// </summary>
    public int ActiveBodies { get; set; }
    
    /// <summary>
    /// The number of static physics bodies.
    /// </summary>
    public int StaticBodies { get; set; }
    
    /// <summary>
    /// The number of collision pairs processed in the last step.
    /// </summary>
    public int CollisionPairs { get; set; }
    
    /// <summary>
    /// The time taken for the last physics step in milliseconds.
    /// </summary>
    public float LastStepTime { get; set; }
    
    /// <summary>
    /// The average physics step time in milliseconds.
    /// </summary>
    public float AverageStepTime { get; set; }
    
    /// <summary>
    /// The total number of physics steps processed.
    /// </summary>
    public long TotalSteps { get; set; }
}

/// <summary>
/// Tier 2: Physics service interface for abstracted physics simulation.
/// Provides physics engine abstraction that can work with different physics engines
/// while maintaining consistent API and independent stepping capabilities.
/// </summary>
public interface IPhysicsService : IService
{
    /// <summary>
    /// Event raised when a collision begins between two bodies.
    /// </summary>
    event EventHandler<CollisionEventArgs>? CollisionEnter;
    
    /// <summary>
    /// Event raised when a collision continues between two bodies.
    /// </summary>
    event EventHandler<CollisionEventArgs>? CollisionStay;
    
    /// <summary>
    /// Event raised when a collision ends between two bodies.
    /// </summary>
    event EventHandler<CollisionEventArgs>? CollisionExit;
    
    /// <summary>
    /// Event raised before each physics step.
    /// </summary>
    event EventHandler<EventArgs>? BeforePhysicsStep;
    
    /// <summary>
    /// Event raised after each physics step.
    /// </summary>
    event EventHandler<EventArgs>? AfterPhysicsStep;

    /// <summary>
    /// Gets the current physics step mode.
    /// </summary>
    PhysicsStepMode StepMode { get; }
    
    /// <summary>
    /// Gets the fixed timestep interval for physics simulation in seconds.
    /// </summary>
    float FixedTimestep { get; }
    
    /// <summary>
    /// Gets the gravity vector for the physics world.
    /// </summary>
    Vector3 Gravity { get; }
    
    /// <summary>
    /// Gets whether the physics simulation is currently running.
    /// </summary>
    bool IsSimulationRunning { get; }

    /// <summary>
    /// Sets the physics step mode.
    /// </summary>
    /// <param name="stepMode">The step mode to use for physics simulation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetStepModeAsync(PhysicsStepMode stepMode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the fixed timestep for physics simulation.
    /// </summary>
    /// <param name="timestep">The fixed timestep in seconds.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetFixedTimestepAsync(float timestep, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the gravity for the physics world.
    /// </summary>
    /// <param name="gravity">The gravity vector.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetGravityAsync(Vector3 gravity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually steps the physics simulation by the specified time.
    /// </summary>
    /// <param name="deltaTime">The time step in seconds, or null to use the fixed timestep.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task StepSimulationAsync(float? deltaTime = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts the physics simulation.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task StartSimulationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses the physics simulation.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task PauseSimulationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes the physics simulation after a pause.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ResumeSimulationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a raycast from the specified origin in the given direction.
    /// </summary>
    /// <param name="origin">The starting point of the ray.</param>
    /// <param name="direction">The direction of the ray (should be normalized).</param>
    /// <param name="maxDistance">The maximum distance to cast the ray.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the first hit, or null if no hit.</returns>
    Task<RaycastHit?> RaycastAsync(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a raycast and returns all hits along the ray.
    /// </summary>
    /// <param name="origin">The starting point of the ray.</param>
    /// <param name="direction">The direction of the ray (should be normalized).</param>
    /// <param name="maxDistance">The maximum distance to cast the ray.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns all hits along the ray.</returns>
    Task<IEnumerable<RaycastHit>> RaycastAllAsync(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a physics body with the specified properties.
    /// </summary>
    /// <param name="bodyId">The unique identifier for the physics body.</param>
    /// <param name="bodyType">The type of physics body to create.</param>
    /// <param name="position">The initial position of the body.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task CreateBodyAsync(string bodyId, PhysicsBodyType bodyType, Vector3 position, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a physics body from the simulation.
    /// </summary>
    /// <param name="bodyId">The identifier of the physics body to remove.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RemoveBodyAsync(string bodyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the position of a physics body.
    /// </summary>
    /// <param name="bodyId">The identifier of the physics body.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the body position.</returns>
    Task<Vector3> GetBodyPositionAsync(string bodyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the position of a physics body.
    /// </summary>
    /// <param name="bodyId">The identifier of the physics body.</param>
    /// <param name="position">The new position for the body.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetBodyPositionAsync(string bodyId, Vector3 position, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a force to a physics body.
    /// </summary>
    /// <param name="bodyId">The identifier of the physics body.</param>
    /// <param name="force">The force vector to apply.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ApplyForceAsync(string bodyId, Vector3 force, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies an impulse to a physics body.
    /// </summary>
    /// <param name="bodyId">The identifier of the physics body.</param>
    /// <param name="impulse">The impulse vector to apply.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ApplyImpulseAsync(string bodyId, Vector3 impulse, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets physics simulation statistics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns physics statistics.</returns>
    Task<PhysicsStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all physics body identifiers currently in the simulation.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns physics body identifiers.</returns>
    Task<IEnumerable<string>> GetAllBodiesAsync(CancellationToken cancellationToken = default);
}