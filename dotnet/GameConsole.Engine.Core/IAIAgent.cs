using GameConsole.Core.Abstractions;
using GameConsole.Plugins.Core;

namespace GameConsole.Engine.Core;

/// <summary>
/// Specialized interface for AI agents that extends the plugin architecture.
/// AI agents are actors that can be managed as plugins with additional game-specific behavior.
/// </summary>
public interface IAIAgent : IActor, IPlugin
{
    /// <summary>
    /// Gets the behavior type of this AI agent (e.g., "Guard", "Merchant", "Enemy").
    /// Used for clustering agents with similar behavior patterns.
    /// </summary>
    string BehaviorType { get; }

    /// <summary>
    /// Gets the current state of the AI agent's behavior.
    /// </summary>
    string CurrentState { get; }

    /// <summary>
    /// Updates the AI agent's behavior state based on game events.
    /// This is called periodically by the game engine.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last update in milliseconds.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task representing the async update operation.</returns>
    Task UpdateBehaviorAsync(double deltaTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event fired when the agent's behavior state changes.
    /// Can be used for monitoring, debugging, and coordination with other agents.
    /// </summary>
    event EventHandler<BehaviorStateChangedEventArgs>? BehaviorStateChanged;
}

/// <summary>
/// Event arguments for AI agent behavior state changes.
/// </summary>
public class BehaviorStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// The previous behavior state.
    /// </summary>
    public string PreviousState { get; }

    /// <summary>
    /// The new behavior state.
    /// </summary>
    public string NewState { get; }

    /// <summary>
    /// The reason for the state change (optional).
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Timestamp when the state change occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Initializes new behavior state change event args.
    /// </summary>
    public BehaviorStateChangedEventArgs(string previousState, string newState, string? reason = null)
    {
        PreviousState = previousState;
        NewState = newState;
        Reason = reason;
        Timestamp = DateTimeOffset.UtcNow;
    }
}