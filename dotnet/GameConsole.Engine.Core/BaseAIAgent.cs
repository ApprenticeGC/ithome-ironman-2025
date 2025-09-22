using System.Numerics;
using GameConsole.Core.Abstractions;

namespace GameConsole.Engine.Core;

/// <summary>
/// Base implementation of an AI agent that extends BaseActor with AI-specific functionality.
/// This class provides common AI behavior patterns and can be extended for specific AI implementations.
/// </summary>
public abstract class BaseAIAgent : BaseActor, IAIAgent
{
    /// <summary>
    /// Gets the type of AI behavior this agent implements.
    /// </summary>
    public string BehaviorType { get; }
    
    /// <summary>
    /// Gets or sets the processing priority for this AI agent.
    /// Higher values indicate higher priority.
    /// </summary>
    public float ProcessingPriority { get; set; } = 1.0f;

    /// <summary>
    /// Initializes a new instance of the BaseAIAgent class.
    /// </summary>
    /// <param name="id">The unique identifier for the AI agent.</param>
    /// <param name="behaviorType">The type of AI behavior this agent implements.</param>
    /// <param name="initialPosition">The initial position of the agent.</param>
    /// <param name="processingPriority">The initial processing priority.</param>
    protected BaseAIAgent(string id, string behaviorType, System.Numerics.Vector3 initialPosition = default, float processingPriority = 1.0f)
        : base(id, initialPosition)
    {
        BehaviorType = behaviorType ?? throw new ArgumentNullException(nameof(behaviorType));
        ProcessingPriority = processingPriority;
    }

    /// <summary>
    /// Updates the actor state asynchronously.
    /// This implementation calls both the base update and AI processing.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update, in seconds.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async update operation.</returns>
    public override async Task UpdateAsync(float deltaTime, CancellationToken cancellationToken = default)
    {
        await base.UpdateAsync(deltaTime, cancellationToken);
        
        if (IsActive)
        {
            await ProcessAIAsync(deltaTime, cancellationToken);
        }
    }

    /// <summary>
    /// Processes the AI logic for this agent asynchronously.
    /// Derived classes must implement this to define specific AI behavior.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last AI processing, in seconds.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async AI processing operation.</returns>
    public abstract Task ProcessAIAsync(float deltaTime, CancellationToken cancellationToken = default);
}