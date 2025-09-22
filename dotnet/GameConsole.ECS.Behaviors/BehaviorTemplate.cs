using Microsoft.Extensions.Logging;

namespace GameConsole.ECS.Behaviors;

/// <summary>
/// Basic implementation of a behavior template.
/// Provides a simple way to create reusable behavior patterns.
/// </summary>
public class BehaviorTemplate : IBehaviorTemplate
{
    private readonly ILogger<BehaviorTemplate> _logger;
    private readonly Func<IBehaviorComposer, IEnumerable<object>, CancellationToken, Task<IBehavior>> _behaviorFactory;
    private readonly Func<IEnumerable<object>>? _defaultComponentsFactory;

    /// <inheritdoc />
    public Guid Id { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string Description { get; }

    /// <inheritdoc />
    public Version Version { get; }

    /// <inheritdoc />
    public IReadOnlyCollection<Type> RequiredComponents { get; }

    /// <inheritdoc />
    public IReadOnlyCollection<Type> BehaviorTypes { get; }

    /// <inheritdoc />
    public IReadOnlyCollection<string> Tags { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BehaviorTemplate"/> class.
    /// </summary>
    /// <param name="name">The name of the template.</param>
    /// <param name="description">The description of the template.</param>
    /// <param name="version">The version of the template.</param>
    /// <param name="requiredComponents">The component types required by this template.</param>
    /// <param name="behaviorTypes">The behavior types this template creates.</param>
    /// <param name="behaviorFactory">Factory function to create behaviors from components.</param>
    /// <param name="logger">Logger for this template.</param>
    /// <param name="defaultComponentsFactory">Optional factory for default components.</param>
    /// <param name="tags">Tags for categorizing this template.</param>
    public BehaviorTemplate(
        string name,
        string description,
        Version version,
        IEnumerable<Type> requiredComponents,
        IEnumerable<Type> behaviorTypes,
        Func<IBehaviorComposer, IEnumerable<object>, CancellationToken, Task<IBehavior>> behaviorFactory,
        ILogger<BehaviorTemplate> logger,
        Func<IEnumerable<object>>? defaultComponentsFactory = null,
        IEnumerable<string>? tags = null)
    {
        Id = Guid.NewGuid();
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Version = version ?? throw new ArgumentNullException(nameof(version));
        RequiredComponents = requiredComponents?.ToList() ?? throw new ArgumentNullException(nameof(requiredComponents));
        BehaviorTypes = behaviorTypes?.ToList() ?? throw new ArgumentNullException(nameof(behaviorTypes));
        _behaviorFactory = behaviorFactory ?? throw new ArgumentNullException(nameof(behaviorFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _defaultComponentsFactory = defaultComponentsFactory;
        Tags = tags?.ToList() ?? new List<string>();
    }

    /// <inheritdoc />
    public async Task<IBehavior> CreateBehaviorAsync(IBehaviorComposer composer, IEnumerable<object>? customComponents = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating behavior from template {TemplateName} ({TemplateId})", Name, Id);

        try
        {
            // Get default components if factory is provided
            var defaultComponents = _defaultComponentsFactory?.Invoke() ?? Enumerable.Empty<object>();
            
            // Combine default and custom components
            var allComponents = defaultComponents.Concat(customComponents ?? Enumerable.Empty<object>()).ToList();

            // Validate we have all required components
            var validation = await ValidateAsync(composer, allComponents, cancellationToken);
            if (!validation.IsValid)
            {
                var errorMessage = $"Template validation failed: {string.Join(", ", validation.Errors)}";
                _logger.LogError("Failed to create behavior from template {TemplateName}: {Error}", Name, errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            // Create the behavior using the factory
            var behavior = await _behaviorFactory(composer, allComponents, cancellationToken);
            
            _logger.LogInformation("Successfully created behavior {BehaviorName} from template {TemplateName}", behavior.Name, Name);
            return behavior;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating behavior from template {TemplateName} ({TemplateId})", Name, Id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<BehaviorTemplateValidationResult> ValidateAsync(IBehaviorComposer composer, IEnumerable<object>? customComponents = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Validating template {TemplateName} ({TemplateId})", Name, Id);

        try
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            var missingComponents = new List<Type>();

            // Get all components (default + custom)
            var defaultComponents = _defaultComponentsFactory?.Invoke() ?? Enumerable.Empty<object>();
            var allComponents = defaultComponents.Concat(customComponents ?? Enumerable.Empty<object>()).ToList();
            var componentTypes = allComponents.Select(c => c.GetType()).ToHashSet();

            // Check for required components
            foreach (var requiredType in RequiredComponents)
            {
                if (!componentTypes.Contains(requiredType) && !componentTypes.Any(t => requiredType.IsAssignableFrom(t)))
                {
                    errors.Add($"Missing required component: {requiredType.Name}");
                    missingComponents.Add(requiredType);
                }
            }

            // Validate behavior composition if we have all required components
            if (errors.Count == 0 && BehaviorTypes.Any())
            {
                foreach (var behaviorType in BehaviorTypes)
                {
                    var canCompose = await composer.ValidateCompositionAsync(behaviorType, allComponents, cancellationToken);
                    if (!canCompose)
                    {
                        errors.Add($"Cannot compose behavior type: {behaviorType.Name}");
                    }
                }
            }

            var isValid = errors.Count == 0;
            _logger.LogDebug("Template {TemplateName} validation result: {IsValid}", Name, isValid);

            return isValid
                ? BehaviorTemplateValidationResult.Success()
                : BehaviorTemplateValidationResult.Failure(errors, warnings, missingComponents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating template {TemplateName} ({TemplateId})", Name, Id);
            return BehaviorTemplateValidationResult.Failure(new[] { $"Validation error: {ex.Message}" });
        }
    }

    /// <inheritdoc />
    public Task<IBehaviorTemplate> CreateSpecializedTemplateAsync(string name, IEnumerable<Type>? additionalComponents = null, IEnumerable<Type>? additionalBehaviors = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating specialized template {SpecializedName} from {TemplateName}", name, Name);

        try
        {
            var newRequiredComponents = RequiredComponents.Concat(additionalComponents ?? Enumerable.Empty<Type>()).Distinct().ToList();
            var newBehaviorTypes = BehaviorTypes.Concat(additionalBehaviors ?? Enumerable.Empty<Type>()).Distinct().ToList();
            var newTags = Tags.Concat(new[] { $"specialized-from-{Name}" }).ToList();

            var specializedTemplate = new BehaviorTemplate(
                name,
                $"Specialized version of {Name}: {Description}",
                new Version(Version.Major, Version.Minor, Version.Build + 1),
                newRequiredComponents,
                newBehaviorTypes,
                _behaviorFactory,
                _logger,
                _defaultComponentsFactory,
                newTags);

            _logger.LogInformation("Created specialized template {SpecializedName} from {TemplateName}", name, Name);
            return Task.FromResult<IBehaviorTemplate>(specializedTemplate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating specialized template {SpecializedName} from {TemplateName}", name, Name);
            throw;
        }
    }
}

/// <summary>
/// Builder for creating behavior templates with fluent API.
/// </summary>
public class BehaviorTemplateBuilder
{
    private readonly ILogger<BehaviorTemplate> _logger;
    private string? _name;
    private string? _description;
    private Version _version = new(1, 0, 0);
    private readonly List<Type> _requiredComponents = new();
    private readonly List<Type> _behaviorTypes = new();
    private readonly List<string> _tags = new();
    private Func<IBehaviorComposer, IEnumerable<object>, CancellationToken, Task<IBehavior>>? _behaviorFactory;
    private Func<IEnumerable<object>>? _defaultComponentsFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="BehaviorTemplateBuilder"/> class.
    /// </summary>
    /// <param name="logger">Logger for the template.</param>
    public BehaviorTemplateBuilder(ILogger<BehaviorTemplate> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sets the name of the template.
    /// </summary>
    /// <param name="name">The template name.</param>
    /// <returns>The builder instance for chaining.</returns>
    public BehaviorTemplateBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>
    /// Sets the description of the template.
    /// </summary>
    /// <param name="description">The template description.</param>
    /// <returns>The builder instance for chaining.</returns>
    public BehaviorTemplateBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Sets the version of the template.
    /// </summary>
    /// <param name="version">The template version.</param>
    /// <returns>The builder instance for chaining.</returns>
    public BehaviorTemplateBuilder WithVersion(Version version)
    {
        _version = version;
        return this;
    }

    /// <summary>
    /// Adds required component types to the template.
    /// </summary>
    /// <param name="componentTypes">The component types to require.</param>
    /// <returns>The builder instance for chaining.</returns>
    public BehaviorTemplateBuilder RequiresComponents(params Type[] componentTypes)
    {
        _requiredComponents.AddRange(componentTypes);
        return this;
    }

    /// <summary>
    /// Adds behavior types that this template creates.
    /// </summary>
    /// <param name="behaviorTypes">The behavior types to create.</param>
    /// <returns>The builder instance for chaining.</returns>
    public BehaviorTemplateBuilder CreatesBehaviors(params Type[] behaviorTypes)
    {
        _behaviorTypes.AddRange(behaviorTypes);
        return this;
    }

    /// <summary>
    /// Adds tags to the template for categorization.
    /// </summary>
    /// <param name="tags">The tags to add.</param>
    /// <returns>The builder instance for chaining.</returns>
    public BehaviorTemplateBuilder WithTags(params string[] tags)
    {
        _tags.AddRange(tags);
        return this;
    }

    /// <summary>
    /// Sets the behavior factory function.
    /// </summary>
    /// <param name="factory">The factory function.</param>
    /// <returns>The builder instance for chaining.</returns>
    public BehaviorTemplateBuilder WithBehaviorFactory(Func<IBehaviorComposer, IEnumerable<object>, CancellationToken, Task<IBehavior>> factory)
    {
        _behaviorFactory = factory;
        return this;
    }

    /// <summary>
    /// Sets the default components factory.
    /// </summary>
    /// <param name="factory">The default components factory.</param>
    /// <returns>The builder instance for chaining.</returns>
    public BehaviorTemplateBuilder WithDefaultComponents(Func<IEnumerable<object>> factory)
    {
        _defaultComponentsFactory = factory;
        return this;
    }

    /// <summary>
    /// Builds the behavior template.
    /// </summary>
    /// <returns>The created behavior template.</returns>
    /// <exception cref="InvalidOperationException">Thrown if required properties are not set.</exception>
    public BehaviorTemplate Build()
    {
        if (string.IsNullOrEmpty(_name))
            throw new InvalidOperationException("Template name is required");
        if (string.IsNullOrEmpty(_description))
            throw new InvalidOperationException("Template description is required");
        if (_behaviorFactory == null)
            throw new InvalidOperationException("Behavior factory is required");

        return new BehaviorTemplate(
            _name,
            _description,
            _version,
            _requiredComponents,
            _behaviorTypes,
            _behaviorFactory,
            _logger,
            _defaultComponentsFactory,
            _tags);
    }
}