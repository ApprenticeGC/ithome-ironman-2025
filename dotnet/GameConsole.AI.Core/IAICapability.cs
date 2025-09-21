namespace GameConsole.AI.Services;

/// <summary>
/// Defines a specific capability or skill that an AI agent can provide.
/// Enables skill enumeration, validation, and metadata access for capability discovery.
/// </summary>
public interface IAICapability
{
    /// <summary>
    /// Gets the unique name of this capability.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a human-readable description of what this capability does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the version of this capability implementation.
    /// </summary>
    Version Version { get; }

    /// <summary>
    /// Gets the list of prerequisite skills or capabilities required for this capability to function.
    /// </summary>
    IReadOnlyList<string> RequiredSkills { get; }

    /// <summary>
    /// Gets the list of optional skills that can enhance this capability's functionality.
    /// </summary>
    IReadOnlyList<string> OptionalSkills { get; }

    /// <summary>
    /// Gets the performance characteristics and resource requirements for this capability.
    /// </summary>
    IReadOnlyList<GameConsole.AI.Core.ResourceRequirement> ResourceRequirements { get; }

    /// <summary>
    /// Gets the supported input types that this capability can process.
    /// </summary>
    IReadOnlyList<string> SupportedInputTypes { get; }

    /// <summary>
    /// Gets the output types that this capability can generate.
    /// </summary>
    IReadOnlyList<string> SupportedOutputTypes { get; }

    /// <summary>
    /// Gets additional metadata and configuration parameters for this capability.
    /// </summary>
    IReadOnlyDictionary<string, object> Metadata { get; }

    /// <summary>
    /// Determines whether this capability can execute a specific skill or operation.
    /// </summary>
    /// <param name="skill">The name of the skill or operation to validate.</param>
    /// <param name="context">The execution context for validation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns true if the skill can be executed.</returns>
    Task<bool> CanExecuteAsync(string skill, IAIContext? context = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available skills that this capability can perform.
    /// </summary>
    /// <param name="context">The execution context for skill enumeration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the list of available skill names.</returns>
    Task<IReadOnlyList<string>> GetAvailableSkillsAsync(IAIContext? context = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates whether the current environment and dependencies support this capability.
    /// </summary>
    /// <param name="context">The execution context for validation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns validation results with any issues found.</returns>
    Task<CapabilityValidationResult> ValidateAsync(IAIContext? context = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Estimates the resource consumption and performance characteristics for a specific operation.
    /// </summary>
    /// <param name="skill">The skill or operation to estimate.</param>
    /// <param name="inputSize">The estimated size or complexity of the input.</param>
    /// <param name="context">The execution context for estimation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns performance and resource estimates.</returns>
    Task<PerformanceEstimate> EstimatePerformanceAsync(string skill, long inputSize = 0, IAIContext? context = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the result of capability validation.
/// </summary>
public readonly record struct CapabilityValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the capability is valid and ready to use.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets any error messages or issues found during validation.
    /// </summary>
    public IReadOnlyList<string> Issues { get; init; }

    /// <summary>
    /// Gets any warning messages about potential issues or limitations.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; }

    /// <summary>
    /// Gets missing dependencies or requirements that prevent the capability from functioning.
    /// </summary>
    public IReadOnlyList<string> MissingDependencies { get; init; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A validation result indicating success.</returns>
    public static CapabilityValidationResult Success() => new()
    {
        IsValid = true,
        Issues = Array.Empty<string>(),
        Warnings = Array.Empty<string>(),
        MissingDependencies = Array.Empty<string>()
    };

    /// <summary>
    /// Creates a failed validation result with the specified issues.
    /// </summary>
    /// <param name="issues">The issues that caused validation to fail.</param>
    /// <param name="missingDependencies">Missing dependencies, if any.</param>
    /// <param name="warnings">Warning messages, if any.</param>
    /// <returns>A validation result indicating failure.</returns>
    public static CapabilityValidationResult Failure(
        IReadOnlyList<string> issues,
        IReadOnlyList<string>? missingDependencies = null,
        IReadOnlyList<string>? warnings = null) => new()
    {
        IsValid = false,
        Issues = issues,
        Warnings = warnings ?? Array.Empty<string>(),
        MissingDependencies = missingDependencies ?? Array.Empty<string>()
    };
}

/// <summary>
/// Represents performance and resource estimates for a capability operation.
/// </summary>
public readonly record struct PerformanceEstimate
{
    /// <summary>
    /// Gets the estimated execution time for the operation.
    /// </summary>
    public TimeSpan EstimatedExecutionTime { get; init; }

    /// <summary>
    /// Gets the estimated resource consumption for the operation.
    /// </summary>
    public IReadOnlyList<GameConsole.AI.Core.ResourceRequirement> EstimatedResourceUsage { get; init; }

    /// <summary>
    /// Gets the confidence level of the estimate (0.0 to 1.0).
    /// </summary>
    public double Confidence { get; init; }

    /// <summary>
    /// Gets additional performance metrics and estimates.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metrics { get; init; }
}