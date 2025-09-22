namespace GameConsole.Core.Abstractions;

/// <summary>
/// Interface for AI-driven actors that extend the base actor functionality
/// with AI-specific processing capabilities and behavior types.
/// </summary>
public interface IAIAgent : IActor
{
    /// <summary>
    /// Gets the type of AI behavior this agent implements.
    /// This can be used for clustering agents with similar behaviors.
    /// </summary>
    string BehaviorType { get; }
    
    /// <summary>
    /// Gets or sets the processing priority for this AI agent.
    /// Higher values indicate higher priority. Used for resource allocation
    /// and update order within clusters.
    /// </summary>
    float ProcessingPriority { get; set; }
    
    /// <summary>
    /// Processes the AI logic for this agent asynchronously.
    /// This method is called to execute AI-specific behavior and decision making.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last AI processing, in seconds.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async AI processing operation.</returns>
    Task ProcessAIAsync(float deltaTime, CancellationToken cancellationToken = default);
}