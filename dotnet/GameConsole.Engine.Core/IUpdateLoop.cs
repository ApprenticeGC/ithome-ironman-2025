using GameConsole.Core.Abstractions;

namespace GameConsole.Engine.Core;

/// <summary>
/// Update execution modes that control timing behavior.
/// </summary>
public enum UpdateMode
{
    /// <summary>
    /// Variable timestep update that adapts to frame rate.
    /// </summary>
    Variable,
    
    /// <summary>
    /// Fixed timestep update that maintains consistent timing.
    /// </summary>
    Fixed,
    
    /// <summary>
    /// Hybrid mode that combines variable and fixed timesteps.
    /// </summary>
    Hybrid
}

/// <summary>
/// Priority levels for update callbacks.
/// </summary>
public enum UpdatePriority
{
    /// <summary>
    /// Lowest priority updates executed last.
    /// </summary>
    Lowest = 0,
    
    /// <summary>
    /// Low priority updates.
    /// </summary>
    Low = 25,
    
    /// <summary>
    /// Normal priority updates.
    /// </summary>
    Normal = 50,
    
    /// <summary>
    /// High priority updates executed early.
    /// </summary>
    High = 75,
    
    /// <summary>
    /// Highest priority updates executed first.
    /// </summary>
    Highest = 100
}

/// <summary>
/// Arguments for update-related events.
/// </summary>
public class UpdateEventArgs : EventArgs
{
    /// <summary>
    /// The delta time for this update in seconds.
    /// </summary>
    public float DeltaTime { get; }
    
    /// <summary>
    /// The total elapsed time since the update loop started.
    /// </summary>
    public TimeSpan TotalTime { get; }
    
    /// <summary>
    /// The current frame number.
    /// </summary>
    public long FrameNumber { get; }

    /// <summary>
    /// Initializes a new instance of the UpdateEventArgs class.
    /// </summary>
    /// <param name="deltaTime">The delta time for this update in seconds.</param>
    /// <param name="totalTime">The total elapsed time since the update loop started.</param>
    /// <param name="frameNumber">The current frame number.</param>
    public UpdateEventArgs(float deltaTime, TimeSpan totalTime, long frameNumber)
    {
        DeltaTime = deltaTime;
        TotalTime = totalTime;
        FrameNumber = frameNumber;
    }
}

/// <summary>
/// Represents an update callback registration.
/// </summary>
public class UpdateRegistration
{
    /// <summary>
    /// Unique identifier for this update registration.
    /// </summary>
    public Guid Id { get; }
    
    /// <summary>
    /// The callback action to execute during updates.
    /// </summary>
    public Action<UpdateEventArgs> Callback { get; }
    
    /// <summary>
    /// The priority level of this update callback.
    /// </summary>
    public UpdatePriority Priority { get; }
    
    /// <summary>
    /// Whether this update callback is currently enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Initializes a new instance of the UpdateRegistration class.
    /// </summary>
    /// <param name="callback">The callback action to execute during updates.</param>
    /// <param name="priority">The priority level of this update callback.</param>
    public UpdateRegistration(Action<UpdateEventArgs> callback, UpdatePriority priority = UpdatePriority.Normal)
    {
        Id = Guid.NewGuid();
        Callback = callback ?? throw new ArgumentNullException(nameof(callback));
        Priority = priority;
        IsEnabled = true;
    }
}

/// <summary>
/// Timing statistics for the update loop.
/// </summary>
public class UpdateLoopStatistics
{
    /// <summary>
    /// The current frames per second.
    /// </summary>
    public float FramesPerSecond { get; set; }
    
    /// <summary>
    /// The average frame time in milliseconds.
    /// </summary>
    public float AverageFrameTime { get; set; }
    
    /// <summary>
    /// The minimum frame time in milliseconds.
    /// </summary>
    public float MinFrameTime { get; set; }
    
    /// <summary>
    /// The maximum frame time in milliseconds.
    /// </summary>
    public float MaxFrameTime { get; set; }
    
    /// <summary>
    /// The total number of frames processed.
    /// </summary>
    public long TotalFrames { get; set; }
    
    /// <summary>
    /// The number of variable timestep updates in the last second.
    /// </summary>
    public int VariableUpdatesPerSecond { get; set; }
    
    /// <summary>
    /// The number of fixed timestep updates in the last second.
    /// </summary>
    public int FixedUpdatesPerSecond { get; set; }
}

/// <summary>
/// Tier 2: Update loop service interface for game loop management with variable and fixed timesteps.
/// Handles consistent timing across platforms, frame rate management, and update callback scheduling
/// to maintain smooth gameplay and rendering.
/// </summary>
public interface IUpdateLoop : IService
{
    /// <summary>
    /// Event raised before each variable timestep update cycle.
    /// </summary>
    event EventHandler<UpdateEventArgs>? BeforeUpdate;
    
    /// <summary>
    /// Event raised after each variable timestep update cycle.
    /// </summary>
    event EventHandler<UpdateEventArgs>? AfterUpdate;
    
    /// <summary>
    /// Event raised before each fixed timestep update cycle.
    /// </summary>
    event EventHandler<UpdateEventArgs>? BeforeFixedUpdate;
    
    /// <summary>
    /// Event raised after each fixed timestep update cycle.
    /// </summary>
    event EventHandler<UpdateEventArgs>? AfterFixedUpdate;

    /// <summary>
    /// Gets the current update mode being used.
    /// </summary>
    UpdateMode CurrentMode { get; }
    
    /// <summary>
    /// Gets the current delta time for variable timestep updates.
    /// </summary>
    float DeltaTime { get; }
    
    /// <summary>
    /// Gets the fixed timestep interval in seconds.
    /// </summary>
    float FixedDeltaTime { get; }
    
    /// <summary>
    /// Gets the total elapsed time since the update loop started.
    /// </summary>
    TimeSpan TotalTime { get; }
    
    /// <summary>
    /// Gets the current frame number.
    /// </summary>
    long FrameNumber { get; }
    
    /// <summary>
    /// Gets whether the update loop is currently running.
    /// </summary>
    bool IsUpdateLoopRunning { get; }

    /// <summary>
    /// Sets the update mode for the game loop.
    /// </summary>
    /// <param name="mode">The update mode to use.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetUpdateModeAsync(UpdateMode mode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the fixed timestep interval for fixed timestep updates.
    /// </summary>
    /// <param name="fixedDeltaTime">The fixed timestep interval in seconds.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetFixedDeltaTimeAsync(float fixedDeltaTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a callback for variable timestep updates.
    /// </summary>
    /// <param name="callback">The callback to execute during variable updates.</param>
    /// <param name="priority">The priority level for the callback execution order.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the update registration.</returns>
    Task<UpdateRegistration> RegisterUpdateAsync(Action<UpdateEventArgs> callback, 
        UpdatePriority priority = UpdatePriority.Normal, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a callback for fixed timestep updates.
    /// </summary>
    /// <param name="callback">The callback to execute during fixed updates.</param>
    /// <param name="priority">The priority level for the callback execution order.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the update registration.</returns>
    Task<UpdateRegistration> RegisterFixedUpdateAsync(Action<UpdateEventArgs> callback, 
        UpdatePriority priority = UpdatePriority.Normal, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters a previously registered update callback.
    /// </summary>
    /// <param name="registration">The update registration to remove.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task UnregisterUpdateAsync(UpdateRegistration registration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables or disables a registered update callback without removing it.
    /// </summary>
    /// <param name="registration">The update registration to enable/disable.</param>
    /// <param name="enabled">Whether the callback should be enabled.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetUpdateEnabledAsync(UpdateRegistration registration, bool enabled, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts the update loop execution.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task StartUpdateLoopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses the update loop execution.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task PauseUpdateLoopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes the update loop execution after a pause.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ResumeUpdateLoopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets timing statistics for the update loop performance.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns timing statistics.</returns>
    Task<UpdateLoopStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a single frame update manually for debugging or testing purposes.
    /// </summary>
    /// <param name="deltaTime">The delta time to use for this frame, or null to use calculated time.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ExecuteSingleFrameAsync(float? deltaTime = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the maximum allowed frame time to prevent spiral of death scenarios.
    /// </summary>
    /// <param name="maxFrameTime">The maximum frame time in seconds.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetMaxFrameTimeAsync(float maxFrameTime, CancellationToken cancellationToken = default);
}