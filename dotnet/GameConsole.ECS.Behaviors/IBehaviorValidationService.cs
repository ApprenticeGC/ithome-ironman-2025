using GameConsole.Core.Abstractions;

namespace GameConsole.ECS.Behaviors;

/// <summary>
/// Service for validating behavior consistency and preventing invalid behavior combinations.
/// Ensures behaviors are properly composed and compatible with each other.
/// </summary>
public interface IBehaviorValidationService : IService
{
    /// <summary>
    /// Validates that a behavior is internally consistent and properly composed.
    /// </summary>
    /// <param name="behavior">The behavior to validate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns validation results.</returns>
    Task<BehaviorValidationResult> ValidateBehaviorAsync(IBehavior behavior, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a set of behaviors can coexist without conflicts.
    /// </summary>
    /// <param name="behaviors">The behaviors to validate together.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns validation results.</returns>
    Task<BehaviorSetValidationResult> ValidateBehaviorSetAsync(IEnumerable<IBehavior> behaviors, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a behavior modification would be valid.
    /// </summary>
    /// <param name="behavior">The behavior to modify.</param>
    /// <param name="modificationType">The type of modification.</param>
    /// <param name="component">The component being modified.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns validation results.</returns>
    Task<BehaviorModificationValidationResult> ValidateModificationAsync(IBehavior behavior, BehaviorModificationType modificationType, object component, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers validation rules for behaviors.
    /// </summary>
    /// <param name="rule">The validation rule to register.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RegisterValidationRuleAsync(IBehaviorValidationRule rule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters a validation rule.
    /// </summary>
    /// <param name="ruleId">The ID of the rule to unregister.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task UnregisterValidationRuleAsync(Guid ruleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all currently registered validation rules.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the validation rules.</returns>
    Task<IEnumerable<IBehaviorValidationRule>> GetValidationRulesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a validation rule for behaviors.
/// </summary>
public interface IBehaviorValidationRule
{
    /// <summary>
    /// Gets the unique identifier for this validation rule.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the name of this validation rule.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of what this rule validates.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the severity level of violations of this rule.
    /// </summary>
    ValidationSeverity Severity { get; }

    /// <summary>
    /// Gets the behavior types this rule applies to. Empty means applies to all behaviors.
    /// </summary>
    IReadOnlyCollection<Type> ApplicableBehaviorTypes { get; }

    /// <summary>
    /// Validates a behavior against this rule.
    /// </summary>
    /// <param name="behavior">The behavior to validate.</param>
    /// <param name="context">Additional validation context.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns validation results.</returns>
    Task<ValidationRuleResult> ValidateAsync(IBehavior behavior, BehaviorValidationContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Context information for behavior validation.
/// </summary>
public class BehaviorValidationContext
{
    /// <summary>
    /// Gets the other behaviors that coexist with the one being validated.
    /// </summary>
    public IReadOnlyCollection<IBehavior> CoexistingBehaviors { get; init; } = Array.Empty<IBehavior>();

    /// <summary>
    /// Gets the entity or context that owns the behavior.
    /// </summary>
    public object? Owner { get; init; }

    /// <summary>
    /// Gets additional validation parameters.
    /// </summary>
    public IReadOnlyDictionary<string, object> Parameters { get; init; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets the validation mode being used.
    /// </summary>
    public BehaviorValidationMode Mode { get; init; } = BehaviorValidationMode.Strict;
}

/// <summary>
/// Represents the mode of behavior validation.
/// </summary>
public enum BehaviorValidationMode
{
    /// <summary>
    /// Strict validation that fails on any rule violation.
    /// </summary>
    Strict,

    /// <summary>
    /// Lenient validation that allows warnings but fails on errors.
    /// </summary>
    Lenient,

    /// <summary>
    /// Permissive validation that only reports violations but doesn't fail.
    /// </summary>
    Permissive
}

/// <summary>
/// Results of behavior validation.
/// </summary>
public class BehaviorValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the behavior passed validation.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets validation errors that prevent the behavior from functioning correctly.
    /// </summary>
    public IReadOnlyCollection<BehaviorValidationIssue> Errors { get; init; } = Array.Empty<BehaviorValidationIssue>();

    /// <summary>
    /// Gets validation warnings that indicate potential issues but don't prevent functioning.
    /// </summary>
    public IReadOnlyCollection<BehaviorValidationIssue> Warnings { get; init; } = Array.Empty<BehaviorValidationIssue>();

    /// <summary>
    /// Gets validation information messages.
    /// </summary>
    public IReadOnlyCollection<BehaviorValidationIssue> Info { get; init; } = Array.Empty<BehaviorValidationIssue>();

    /// <summary>
    /// Gets the overall validation score (0-100, higher is better).
    /// </summary>
    public int ValidationScore { get; init; } = 100;

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static BehaviorValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with issues.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <param name="warnings">Optional validation warnings.</param>
    /// <param name="info">Optional validation info messages.</param>
    /// <param name="score">Optional validation score.</param>
    public static BehaviorValidationResult Failure(
        IEnumerable<BehaviorValidationIssue> errors,
        IEnumerable<BehaviorValidationIssue>? warnings = null,
        IEnumerable<BehaviorValidationIssue>? info = null,
        int score = 0) => new()
        {
            IsValid = false,
            Errors = errors?.ToList() ?? new List<BehaviorValidationIssue>(),
            Warnings = warnings?.ToList() ?? new List<BehaviorValidationIssue>(),
            Info = info?.ToList() ?? new List<BehaviorValidationIssue>(),
            ValidationScore = score
        };
}

/// <summary>
/// Results of behavior set validation.
/// </summary>
public class BehaviorSetValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the behavior set is valid.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets validation results for individual behaviors.
    /// </summary>
    public IReadOnlyDictionary<IBehavior, BehaviorValidationResult> IndividualResults { get; init; } = new Dictionary<IBehavior, BehaviorValidationResult>();

    /// <summary>
    /// Gets validation issues that apply to the behavior set as a whole.
    /// </summary>
    public IReadOnlyCollection<BehaviorValidationIssue> SetIssues { get; init; } = Array.Empty<BehaviorValidationIssue>();

    /// <summary>
    /// Gets conflicts detected between behaviors in the set.
    /// </summary>
    public IReadOnlyCollection<BehaviorConflict> Conflicts { get; init; } = Array.Empty<BehaviorConflict>();
}

/// <summary>
/// Results of behavior modification validation.
/// </summary>
public class BehaviorModificationValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the modification would be valid.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets validation issues with the proposed modification.
    /// </summary>
    public IReadOnlyCollection<BehaviorValidationIssue> Issues { get; init; } = Array.Empty<BehaviorValidationIssue>();

    /// <summary>
    /// Gets the predicted impact of the modification.
    /// </summary>
    public ModificationImpact Impact { get; init; } = ModificationImpact.Unknown;
}

/// <summary>
/// Represents a validation issue found in a behavior.
/// </summary>
public class BehaviorValidationIssue
{
    /// <summary>
    /// Gets the severity of this validation issue.
    /// </summary>
    public required ValidationSeverity Severity { get; init; }

    /// <summary>
    /// Gets the message describing the validation issue.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the validation rule that detected this issue.
    /// </summary>
    public required IBehaviorValidationRule Rule { get; init; }

    /// <summary>
    /// Gets the component or behavior aspect that has the issue.
    /// </summary>
    public object? Source { get; init; }

    /// <summary>
    /// Gets suggested fixes for this issue.
    /// </summary>
    public IReadOnlyCollection<string> SuggestedFixes { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Represents a conflict between behaviors.
/// </summary>
public class BehaviorConflict
{
    /// <summary>
    /// Gets the behaviors that are in conflict.
    /// </summary>
    public required IReadOnlyCollection<IBehavior> ConflictingBehaviors { get; init; }

    /// <summary>
    /// Gets the type of conflict.
    /// </summary>
    public required BehaviorConflictType ConflictType { get; init; }

    /// <summary>
    /// Gets the description of the conflict.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the severity of the conflict.
    /// </summary>
    public required ValidationSeverity Severity { get; init; }
}

/// <summary>
/// Results of a single validation rule execution.
/// </summary>
public class ValidationRuleResult
{
    /// <summary>
    /// Gets a value indicating whether the rule passed.
    /// </summary>
    public bool Passed { get; init; }

    /// <summary>
    /// Gets issues found by this rule.
    /// </summary>
    public IReadOnlyCollection<BehaviorValidationIssue> Issues { get; init; } = Array.Empty<BehaviorValidationIssue>();
}

/// <summary>
/// Represents the severity of a validation issue.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// Informational message, no action required.
    /// </summary>
    Info,

    /// <summary>
    /// Warning about potential issues.
    /// </summary>
    Warning,

    /// <summary>
    /// Error that prevents proper functioning.
    /// </summary>
    Error,

    /// <summary>
    /// Critical error that would cause system failure.
    /// </summary>
    Critical
}

/// <summary>
/// Represents the type of conflict between behaviors.
/// </summary>
public enum BehaviorConflictType
{
    /// <summary>
    /// Behaviors have mutually exclusive components.
    /// </summary>
    ComponentConflict,

    /// <summary>
    /// Behaviors have conflicting resource requirements.
    /// </summary>
    ResourceConflict,

    /// <summary>
    /// Behaviors have incompatible state requirements.
    /// </summary>
    StateConflict,

    /// <summary>
    /// Behaviors have conflicting update or execution priorities.
    /// </summary>
    ExecutionConflict
}

/// <summary>
/// Represents the predicted impact of a behavior modification.
/// </summary>
public enum ModificationImpact
{
    /// <summary>
    /// Impact is unknown or cannot be determined.
    /// </summary>
    Unknown,

    /// <summary>
    /// No significant impact expected.
    /// </summary>
    None,

    /// <summary>
    /// Minor impact, behavior will still function normally.
    /// </summary>
    Minor,

    /// <summary>
    /// Moderate impact, some behavior aspects may change.
    /// </summary>
    Moderate,

    /// <summary>
    /// Major impact, behavior functionality will significantly change.
    /// </summary>
    Major,

    /// <summary>
    /// Critical impact, behavior may become non-functional.
    /// </summary>
    Critical
}