namespace GameConsole.UI.Core;

/// <summary>
/// Base interface for all UI components that work across different frameworks.
/// Provides a framework-agnostic way to define, render, and manage UI components.
/// </summary>
public interface IUIComponent : IAsyncDisposable
{
    /// <summary>
    /// Gets the unique identifier for this component instance.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the component descriptor with metadata about this component.
    /// </summary>
    UIComponentDescriptor Descriptor { get; }

    /// <summary>
    /// Gets the current state of the component.
    /// </summary>
    UIComponentState State { get; }

    /// <summary>
    /// Gets the parent component (if any).
    /// </summary>
    IUIComponent? Parent { get; }

    /// <summary>
    /// Gets the child components.
    /// </summary>
    IReadOnlyList<IUIComponent> Children { get; }

    /// <summary>
    /// Gets the component properties.
    /// </summary>
    Dictionary<string, object> Properties { get; }

    /// <summary>
    /// Gets the component style information.
    /// </summary>
    UIStyle Style { get; set; }

    /// <summary>
    /// Gets the component layout information.
    /// </summary>
    UILayout Layout { get; set; }

    /// <summary>
    /// Observable stream of events from this component.
    /// </summary>
    IObservable<UIEvent> Events { get; }

    /// <summary>
    /// Initializes the component with the specified context.
    /// </summary>
    /// <param name="context">UI context for initialization.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async initialization.</returns>
    Task InitializeAsync(UIContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders the component to a virtual node tree.
    /// </summary>
    /// <param name="context">UI context for rendering.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async render operation.</returns>
    Task<UIRenderResult> RenderAsync(UIContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the component with new properties or state.
    /// </summary>
    /// <param name="properties">Properties to update.</param>
    /// <param name="context">UI context for the update.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async update operation.</returns>
    Task UpdateAsync(Dictionary<string, object> properties, UIContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a child component to this component.
    /// </summary>
    /// <param name="child">The child component to add.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task AddChildAsync(IUIComponent child, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a child component from this component.
    /// </summary>
    /// <param name="child">The child component to remove.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RemoveChildAsync(IUIComponent child, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles an event that occurred on this component.
    /// </summary>
    /// <param name="uiEvent">The event to handle.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async event handling.</returns>
    Task HandleEventAsync(UIEvent uiEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the current component state and properties.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async validation operation.</returns>
    Task<UIValidationResult> ValidateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a property value by name.
    /// </summary>
    /// <typeparam name="T">The expected property type.</typeparam>
    /// <param name="propertyName">The property name.</param>
    /// <returns>The property value, or default if not found.</returns>
    T? GetProperty<T>(string propertyName);

    /// <summary>
    /// Sets a property value by name.
    /// </summary>
    /// <typeparam name="T">The property type.</typeparam>
    /// <param name="propertyName">The property name.</param>
    /// <param name="value">The property value.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async property update.</returns>
    Task SetPropertyAsync<T>(string propertyName, T value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a child component by ID.
    /// </summary>
    /// <param name="id">The component ID to search for.</param>
    /// <returns>The found component, or null if not found.</returns>
    IUIComponent? FindChildById(string id);

    /// <summary>
    /// Finds child components by type.
    /// </summary>
    /// <typeparam name="T">The component type to search for.</typeparam>
    /// <returns>A collection of matching components.</returns>
    IEnumerable<T> FindChildrenByType<T>() where T : class, IUIComponent;
}

/// <summary>
/// Represents the result of a UI validation operation.
/// </summary>
public record UIValidationResult
{
    /// <summary>
    /// Gets whether the validation passed.
    /// </summary>
    public bool IsValid { get; init; } = true;

    /// <summary>
    /// Gets the validation errors (if any).
    /// </summary>
    public List<UIValidationError> Errors { get; init; } = new();

    /// <summary>
    /// Gets validation warnings (non-critical issues).
    /// </summary>
    public List<UIValidationWarning> Warnings { get; init; } = new();
}

/// <summary>
/// Represents a validation error.
/// </summary>
public record UIValidationError
{
    /// <summary>
    /// Gets the property name that failed validation.
    /// </summary>
    public string PropertyName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets the validation rule that failed.
    /// </summary>
    public UIValidationRule Rule { get; init; } = new();

    /// <summary>
    /// Gets the invalid value.
    /// </summary>
    public object? Value { get; init; }
}

/// <summary>
/// Represents a validation warning.
/// </summary>
public record UIValidationWarning
{
    /// <summary>
    /// Gets the property name that generated the warning.
    /// </summary>
    public string PropertyName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the warning message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets the warning severity.
    /// </summary>
    public UIValidationSeverity Severity { get; init; } = UIValidationSeverity.Warning;
}

/// <summary>
/// Validation severity levels.
/// </summary>
public enum UIValidationSeverity
{
    /// <summary>
    /// Information message.
    /// </summary>
    Info,

    /// <summary>
    /// Warning message.
    /// </summary>
    Warning,

    /// <summary>
    /// Error message.
    /// </summary>
    Error
}