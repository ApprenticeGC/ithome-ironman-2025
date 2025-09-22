using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace GameConsole.ECS.Behaviors;

/// <summary>
/// Service implementation for validating behavior consistency and preventing invalid behavior combinations.
/// </summary>
public class BehaviorValidationService : IBehaviorValidationService
{
    private readonly ILogger<BehaviorValidationService> _logger;
    private readonly IComponentDependencyResolver _dependencyResolver;
    private readonly Dictionary<Guid, IBehaviorValidationRule> _validationRules = new();
    private bool _isRunning;

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Initializes a new instance of the <see cref="BehaviorValidationService"/> class.
    /// </summary>
    /// <param name="logger">Logger for the service.</param>
    /// <param name="dependencyResolver">Dependency resolver for component validation.</param>
    public BehaviorValidationService(ILogger<BehaviorValidationService> logger, IComponentDependencyResolver dependencyResolver)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dependencyResolver = dependencyResolver ?? throw new ArgumentNullException(nameof(dependencyResolver));
    }

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing BehaviorValidationService");
        
        // Register default validation rules
        RegisterDefaultRules();
        
        _logger.LogInformation("Initialized BehaviorValidationService with {RuleCount} validation rules", _validationRules.Count);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting BehaviorValidationService");
        _isRunning = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping BehaviorValidationService");
        _isRunning = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isRunning)
        {
            await StopAsync();
        }
        _logger.LogInformation("Disposed BehaviorValidationService");
    }

    /// <inheritdoc />
    public async Task<BehaviorValidationResult> ValidateBehaviorAsync(IBehavior behavior, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Validating behavior {BehaviorName} ({BehaviorId})", behavior.Name, behavior.Id);

        var context = new BehaviorValidationContext();
        var errors = new List<BehaviorValidationIssue>();
        var warnings = new List<BehaviorValidationIssue>();
        var info = new List<BehaviorValidationIssue>();

        try
        {
            // Run all applicable validation rules
            foreach (var rule in _validationRules.Values)
            {
                if (IsRuleApplicable(rule, behavior))
                {
                    var ruleResult = await rule.ValidateAsync(behavior, context, cancellationToken);
                    
                    foreach (var issue in ruleResult.Issues)
                    {
                        switch (issue.Severity)
                        {
                            case ValidationSeverity.Error:
                            case ValidationSeverity.Critical:
                                errors.Add(issue);
                                break;
                            case ValidationSeverity.Warning:
                                warnings.Add(issue);
                                break;
                            case ValidationSeverity.Info:
                                info.Add(issue);
                                break;
                        }
                    }
                }
            }

            // Calculate validation score
            var score = CalculateValidationScore(errors, warnings, info);
            var isValid = errors.Count == 0;

            _logger.LogDebug("Behavior {BehaviorName} validation result: {IsValid} (Score: {Score}, Errors: {ErrorCount}, Warnings: {WarningCount})",
                behavior.Name, isValid, score, errors.Count, warnings.Count);

            return isValid
                ? new BehaviorValidationResult { IsValid = true, ValidationScore = score, Warnings = warnings, Info = info }
                : BehaviorValidationResult.Failure(errors, warnings, info, score);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating behavior {BehaviorName} ({BehaviorId})", behavior.Name, behavior.Id);
            var errorIssue = new BehaviorValidationIssue
            {
                Severity = ValidationSeverity.Critical,
                Message = $"Validation error: {ex.Message}",
                Rule = new ErrorValidationRule(),
                Source = behavior
            };
            return BehaviorValidationResult.Failure(new[] { errorIssue });
        }
    }

    /// <inheritdoc />
    public async Task<BehaviorSetValidationResult> ValidateBehaviorSetAsync(IEnumerable<IBehavior> behaviors, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Validating behavior set with {BehaviorCount} behaviors", behaviors.Count());

        var behaviorList = behaviors.ToList();
        var individualResults = new Dictionary<IBehavior, BehaviorValidationResult>();
        var setIssues = new List<BehaviorValidationIssue>();
        var conflicts = new List<BehaviorConflict>();

        try
        {
            // Validate each behavior individually
            foreach (var behavior in behaviorList)
            {
                var context = new BehaviorValidationContext
                {
                    CoexistingBehaviors = behaviorList.Where(b => b != behavior).ToList()
                };

                var result = await ValidateBehaviorAsync(behavior, cancellationToken);
                individualResults[behavior] = result;
            }

            // Check for conflicts between behaviors
            conflicts.AddRange(await DetectBehaviorConflictsAsync(behaviorList, cancellationToken));

            // Overall validation result
            var isValid = individualResults.Values.All(r => r.IsValid) && conflicts.Count == 0;

            return new BehaviorSetValidationResult
            {
                IsValid = isValid,
                IndividualResults = individualResults,
                SetIssues = setIssues,
                Conflicts = conflicts
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating behavior set");
            var errorIssue = new BehaviorValidationIssue
            {
                Severity = ValidationSeverity.Critical,
                Message = $"Set validation error: {ex.Message}",
                Rule = new ErrorValidationRule()
            };
            setIssues.Add(errorIssue);

            return new BehaviorSetValidationResult
            {
                IsValid = false,
                IndividualResults = individualResults,
                SetIssues = setIssues,
                Conflicts = conflicts
            };
        }
    }

    /// <inheritdoc />
    public async Task<BehaviorModificationValidationResult> ValidateModificationAsync(IBehavior behavior, BehaviorModificationType modificationType, object component, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Validating modification {ModificationType} of component {ComponentType} on behavior {BehaviorName}",
            modificationType, component.GetType().Name, behavior.Name);

        try
        {
            var issues = new List<BehaviorValidationIssue>();
            var impact = ModificationImpact.Minor;

            // Simulate the modification to validate the result
            var currentComponents = behavior.Components.ToList();
            var newComponents = new List<object>(currentComponents);

            switch (modificationType)
            {
                case BehaviorModificationType.AddComponent:
                    newComponents.Add(component);
                    break;

                case BehaviorModificationType.RemoveComponent:
                    var componentType = component.GetType();
                    var removedCount = newComponents.RemoveAll(c => c.GetType() == componentType);
                    if (removedCount == 0)
                    {
                        issues.Add(new BehaviorValidationIssue
                        {
                            Severity = ValidationSeverity.Warning,
                            Message = $"Component {componentType.Name} not found for removal",
                            Rule = new ModificationValidationRule(),
                            Source = component
                        });
                    }
                    else
                    {
                        impact = ModificationImpact.Moderate;
                    }
                    break;

                case BehaviorModificationType.UpdateComponent:
                case BehaviorModificationType.ReplaceComponent:
                    // Find existing component to replace
                    var replaceType = component.GetType();
                    var foundIndex = -1;
                    for (int i = 0; i < newComponents.Count; i++)
                    {
                        if (newComponents[i].GetType() == replaceType || replaceType.IsAssignableFrom(newComponents[i].GetType()))
                        {
                            foundIndex = i;
                            break;
                        }
                    }

                    if (foundIndex >= 0)
                    {
                        newComponents[foundIndex] = component;
                        impact = ModificationImpact.Moderate;
                    }
                    else
                    {
                        issues.Add(new BehaviorValidationIssue
                        {
                            Severity = ValidationSeverity.Warning,
                            Message = $"No component of type {replaceType.Name} found for replacement",
                            Rule = new ModificationValidationRule(),
                            Source = component
                        });
                    }
                    break;
            }

            // Validate the new component composition
            var componentTypes = newComponents.Select(c => c.GetType()).ToList();
            var compatibilityResult = await _dependencyResolver.ValidateCompatibilityAsync(componentTypes, cancellationToken);

            if (!compatibilityResult.IsCompatible)
            {
                impact = ModificationImpact.Critical;
                issues.AddRange(compatibilityResult.Errors.Select(error => new BehaviorValidationIssue
                {
                    Severity = ValidationSeverity.Error,
                    Message = error,
                    Rule = new ComponentCompatibilityValidationRule(),
                    Source = component
                }));
            }

            var isValid = issues.Count(i => i.Severity >= ValidationSeverity.Error) == 0;

            return new BehaviorModificationValidationResult
            {
                IsValid = isValid,
                Issues = issues,
                Impact = impact
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating modification for behavior {BehaviorName}", behavior.Name);
            return new BehaviorModificationValidationResult
            {
                IsValid = false,
                Issues = new[]
                {
                    new BehaviorValidationIssue
                    {
                        Severity = ValidationSeverity.Critical,
                        Message = $"Modification validation error: {ex.Message}",
                        Rule = new ErrorValidationRule(),
                        Source = component
                    }
                },
                Impact = ModificationImpact.Unknown
            };
        }
    }

    /// <inheritdoc />
    public Task RegisterValidationRuleAsync(IBehaviorValidationRule rule, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Registering validation rule {RuleName} ({RuleId})", rule.Name, rule.Id);
        _validationRules[rule.Id] = rule;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UnregisterValidationRuleAsync(Guid ruleId, CancellationToken cancellationToken = default)
    {
        if (_validationRules.Remove(ruleId))
        {
            _logger.LogDebug("Unregistered validation rule {RuleId}", ruleId);
        }
        else
        {
            _logger.LogWarning("Attempted to unregister unknown validation rule {RuleId}", ruleId);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IEnumerable<IBehaviorValidationRule>> GetValidationRulesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<IBehaviorValidationRule>>(_validationRules.Values.ToList());
    }

    /// <summary>
    /// Registers default validation rules.
    /// </summary>
    private void RegisterDefaultRules()
    {
        var defaultRules = new IBehaviorValidationRule[]
        {
            new ComponentCompatibilityValidationRule(),
            new MetadataConsistencyValidationRule(),
            new ModificationValidationRule()
        };

        foreach (var rule in defaultRules)
        {
            _validationRules[rule.Id] = rule;
        }
    }

    /// <summary>
    /// Checks if a validation rule is applicable to a behavior.
    /// </summary>
    /// <param name="rule">The validation rule.</param>
    /// <param name="behavior">The behavior to check.</param>
    /// <returns>True if the rule is applicable.</returns>
    private static bool IsRuleApplicable(IBehaviorValidationRule rule, IBehavior behavior)
    {
        if (rule.ApplicableBehaviorTypes.Count == 0)
        {
            return true; // Rule applies to all behaviors
        }

        var behaviorType = behavior.GetType();
        return rule.ApplicableBehaviorTypes.Any(t => t.IsAssignableFrom(behaviorType));
    }

    /// <summary>
    /// Calculates a validation score based on issues found.
    /// </summary>
    /// <param name="errors">Validation errors.</param>
    /// <param name="warnings">Validation warnings.</param>
    /// <param name="info">Validation info messages.</param>
    /// <returns>A score from 0-100.</returns>
    private static int CalculateValidationScore(List<BehaviorValidationIssue> errors, List<BehaviorValidationIssue> warnings, List<BehaviorValidationIssue> info)
    {
        if (errors.Count > 0)
        {
            return Math.Max(0, 50 - (errors.Count * 10)); // Errors heavily impact score
        }

        if (warnings.Count > 0)
        {
            return Math.Max(70, 100 - (warnings.Count * 5)); // Warnings moderately impact score
        }

        return 100; // Perfect score with no errors or warnings
    }

    /// <summary>
    /// Detects conflicts between behaviors in a set.
    /// </summary>
    /// <param name="behaviors">The behaviors to check for conflicts.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of detected conflicts.</returns>
    private Task<List<BehaviorConflict>> DetectBehaviorConflictsAsync(List<IBehavior> behaviors, CancellationToken cancellationToken)
    {
        var conflicts = new List<BehaviorConflict>();

        // Check for component conflicts between behaviors
        for (int i = 0; i < behaviors.Count; i++)
        {
            for (int j = i + 1; j < behaviors.Count; j++)
            {
                var behavior1 = behaviors[i];
                var behavior2 = behaviors[j];

                // Check if behaviors have conflicting component requirements
                var conflictingTypes = behavior1.Metadata.ConflictingComponents
                    .Intersect(behavior2.Components.Select(c => c.GetType()))
                    .ToList();

                if (conflictingTypes.Any())
                {
                    conflicts.Add(new BehaviorConflict
                    {
                        ConflictingBehaviors = new[] { behavior1, behavior2 },
                        ConflictType = BehaviorConflictType.ComponentConflict,
                        Description = $"Behaviors have conflicting component requirements: {string.Join(", ", conflictingTypes.Select(t => t.Name))}",
                        Severity = ValidationSeverity.Error
                    });
                }
            }
        }

        return Task.FromResult(conflicts);
    }
}

// Default validation rule implementations

/// <summary>
/// Validation rule for component compatibility.
/// </summary>
public class ComponentCompatibilityValidationRule : IBehaviorValidationRule
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "Component Compatibility";
    public string Description => "Validates that behavior components are compatible with each other";
    public ValidationSeverity Severity => ValidationSeverity.Error;
    public IReadOnlyCollection<Type> ApplicableBehaviorTypes => Array.Empty<Type>();

    public Task<ValidationRuleResult> ValidateAsync(IBehavior behavior, BehaviorValidationContext context, CancellationToken cancellationToken = default)
    {
        var issues = new List<BehaviorValidationIssue>();

        // Basic validation - ensure all required components are present
        var componentTypes = behavior.Components.Select(c => c.GetType()).ToHashSet();
        var requiredComponents = behavior.Metadata.RequiredComponents;

        foreach (var required in requiredComponents)
        {
            if (!componentTypes.Contains(required) && !componentTypes.Any(t => required.IsAssignableFrom(t)))
            {
                issues.Add(new BehaviorValidationIssue
                {
                    Severity = ValidationSeverity.Error,
                    Message = $"Missing required component: {required.Name}",
                    Rule = this,
                    Source = behavior
                });
            }
        }

        // Check for conflicting components
        var conflictingComponents = behavior.Metadata.ConflictingComponents;
        foreach (var conflict in conflictingComponents)
        {
            if (componentTypes.Contains(conflict) || componentTypes.Any(t => conflict.IsAssignableFrom(t)))
            {
                issues.Add(new BehaviorValidationIssue
                {
                    Severity = ValidationSeverity.Error,
                    Message = $"Behavior contains conflicting component: {conflict.Name}",
                    Rule = this,
                    Source = behavior
                });
            }
        }

        return Task.FromResult(new ValidationRuleResult
        {
            Passed = issues.Count == 0,
            Issues = issues
        });
    }
}

/// <summary>
/// Validation rule for metadata consistency.
/// </summary>
public class MetadataConsistencyValidationRule : IBehaviorValidationRule
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "Metadata Consistency";
    public string Description => "Validates that behavior metadata is consistent with its components";
    public ValidationSeverity Severity => ValidationSeverity.Warning;
    public IReadOnlyCollection<Type> ApplicableBehaviorTypes => Array.Empty<Type>();

    public Task<ValidationRuleResult> ValidateAsync(IBehavior behavior, BehaviorValidationContext context, CancellationToken cancellationToken = default)
    {
        var issues = new List<BehaviorValidationIssue>();

        // Validate that metadata accurately reflects the components
        var actualComponents = behavior.Components.Select(c => c.GetType()).ToHashSet();
        var metadataComponents = behavior.Metadata.RequiredComponents.ToHashSet();

        // Warn if components are not reflected in metadata
        foreach (var component in actualComponents)
        {
            if (!metadataComponents.Contains(component) && !metadataComponents.Any(m => component.IsAssignableFrom(m)))
            {
                issues.Add(new BehaviorValidationIssue
                {
                    Severity = ValidationSeverity.Info,
                    Message = $"Component {component.Name} is not declared in metadata",
                    Rule = this,
                    Source = behavior
                });
            }
        }

        return Task.FromResult(new ValidationRuleResult
        {
            Passed = true, // This rule never fails, just provides info
            Issues = issues
        });
    }
}

/// <summary>
/// Validation rule for behavior modifications.
/// </summary>
public class ModificationValidationRule : IBehaviorValidationRule
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "Modification Validation";
    public string Description => "Validates behavior modifications";
    public ValidationSeverity Severity => ValidationSeverity.Warning;
    public IReadOnlyCollection<Type> ApplicableBehaviorTypes => Array.Empty<Type>();

    public Task<ValidationRuleResult> ValidateAsync(IBehavior behavior, BehaviorValidationContext context, CancellationToken cancellationToken = default)
    {
        // This rule is primarily used in modification validation
        return Task.FromResult(new ValidationRuleResult { Passed = true, Issues = Array.Empty<BehaviorValidationIssue>() });
    }
}

/// <summary>
/// Error validation rule for system errors.
/// </summary>
public class ErrorValidationRule : IBehaviorValidationRule
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "System Error";
    public string Description => "Handles system validation errors";
    public ValidationSeverity Severity => ValidationSeverity.Critical;
    public IReadOnlyCollection<Type> ApplicableBehaviorTypes => Array.Empty<Type>();

    public Task<ValidationRuleResult> ValidateAsync(IBehavior behavior, BehaviorValidationContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ValidationRuleResult { Passed = true, Issues = Array.Empty<BehaviorValidationIssue>() });
    }
}