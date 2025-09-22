namespace GameConsole.ECS.Behaviors;

/// <summary>
/// Represents a reusable template for creating behavior instances.
/// Behavior templates define common entity archetypes like "Player", "Enemy", "Collectible", etc.
/// </summary>
public interface IBehaviorTemplate
{
    /// <summary>
    /// Gets the unique identifier for this template.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the name of this template.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of what this template creates.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the version of this template for compatibility tracking.
    /// </summary>
    Version Version { get; }

    /// <summary>
    /// Gets the component types required by entities created from this template.
    /// </summary>
    IReadOnlyCollection<Type> RequiredComponents { get; }

    /// <summary>
    /// Gets the behavior types that should be composed for entities created from this template.
    /// </summary>
    IReadOnlyCollection<Type> BehaviorTypes { get; }

    /// <summary>
    /// Gets the tags associated with this template for categorization.
    /// </summary>
    IReadOnlyCollection<string> Tags { get; }

    /// <summary>
    /// Creates a new behavior instance from this template.
    /// </summary>
    /// <param name="composer">The behavior composer to use for creating behaviors.</param>
    /// <param name="customComponents">Optional custom components to add to the template's components.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the created behavior.</returns>
    Task<IBehavior> CreateBehaviorAsync(IBehaviorComposer composer, IEnumerable<object>? customComponents = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that this template can create a valid behavior with the given custom components.
    /// </summary>
    /// <param name="composer">The behavior composer to use for validation.</param>
    /// <param name="customComponents">Optional custom components to validate with the template.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns validation results.</returns>
    Task<BehaviorTemplateValidationResult> ValidateAsync(IBehaviorComposer composer, IEnumerable<object>? customComponents = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a specialized version of this template with additional constraints or components.
    /// </summary>
    /// <param name="name">Name for the specialized template.</param>
    /// <param name="additionalComponents">Additional component types to require.</param>
    /// <param name="additionalBehaviors">Additional behavior types to include.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the specialized template.</returns>
    Task<IBehaviorTemplate> CreateSpecializedTemplateAsync(string name, IEnumerable<Type>? additionalComponents = null, IEnumerable<Type>? additionalBehaviors = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Results of behavior template validation.
/// </summary>
public class BehaviorTemplateValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the template validation was successful.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets validation errors if the template is not valid.
    /// </summary>
    public IReadOnlyCollection<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets validation warnings that don't prevent template usage.
    /// </summary>
    public IReadOnlyCollection<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets missing component types that are required but not provided.
    /// </summary>
    public IReadOnlyCollection<Type> MissingComponents { get; init; } = Array.Empty<Type>();

    /// <summary>
    /// Gets conflicting component types that cannot be used together.
    /// </summary>
    public IReadOnlyCollection<Type> ConflictingComponents { get; init; } = Array.Empty<Type>();

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static BehaviorTemplateValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with errors.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <param name="warnings">Optional validation warnings.</param>
    /// <param name="missingComponents">Missing component types.</param>
    /// <param name="conflictingComponents">Conflicting component types.</param>
    public static BehaviorTemplateValidationResult Failure(
        IEnumerable<string> errors,
        IEnumerable<string>? warnings = null,
        IEnumerable<Type>? missingComponents = null,
        IEnumerable<Type>? conflictingComponents = null) => new()
        {
            IsValid = false,
            Errors = errors?.ToList() ?? new List<string>(),
            Warnings = warnings?.ToList() ?? new List<string>(),
            MissingComponents = missingComponents?.ToList() ?? new List<Type>(),
            ConflictingComponents = conflictingComponents?.ToList() ?? new List<Type>()
        };
}