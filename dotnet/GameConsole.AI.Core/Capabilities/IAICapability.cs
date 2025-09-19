using GameConsole.AI.Models;

namespace GameConsole.AI.Capabilities;

/// <summary>
/// Defines a capability that can be provided by an AI agent.
/// Represents a specific skill or functionality that the agent can perform.
/// </summary>
public interface IAICapability
{
    /// <summary>
    /// Gets the unique identifier for this capability.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the human-readable name of this capability.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a description of what this capability does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the version of this capability.
    /// </summary>
    Version Version { get; }

    /// <summary>
    /// Gets the input types that this capability can process.
    /// </summary>
    IReadOnlyList<Type> SupportedInputTypes { get; }

    /// <summary>
    /// Gets the output types that this capability can produce.
    /// </summary>
    IReadOnlyList<Type> SupportedOutputTypes { get; }

    /// <summary>
    /// Gets the resource requirements for this capability.
    /// </summary>
    AIResourceRequirements ResourceRequirements { get; }

    /// <summary>
    /// Validates whether the given input is supported by this capability.
    /// </summary>
    /// <param name="input">The input to validate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async validation operation that returns true if the input is supported.</returns>
    Task<bool> ValidateInputAsync(object input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes this capability with the given input.
    /// </summary>
    /// <param name="input">The input data for the capability.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async execution operation that returns the capability result.</returns>
    Task<object> ExecuteAsync(object input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the estimated performance metrics for executing this capability with the given input.
    /// </summary>
    /// <param name="input">The input data to estimate performance for.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns estimated performance metrics.</returns>
    Task<AIPerformanceEstimate> EstimatePerformanceAsync(object input, CancellationToken cancellationToken = default);
}