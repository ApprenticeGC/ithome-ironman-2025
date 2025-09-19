using GameConsole.Core.Abstractions;
using System.Reactive;
using System.Reactive.Subjects;
using System.Reactive.Linq;

namespace GameConsole.UI.Core;

/// <summary>
/// Represents a cross-framework UI component with lifecycle management and event handling.
/// </summary>
public interface IUIComponent : IAsyncDisposable
{
    /// <summary>
    /// Unique identifier for this component instance.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Component type name.
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Parent component (null for root components).
    /// </summary>
    IUIComponent? Parent { get; }

    /// <summary>
    /// Child components.
    /// </summary>
    IReadOnlyList<IUIComponent> Children { get; }

    /// <summary>
    /// Current visual style for this component.
    /// </summary>
    UIStyle? Style { get; set; }

    /// <summary>
    /// Current layout information for this component.
    /// </summary>
    UILayout? Layout { get; set; }

    /// <summary>
    /// Whether this component is currently enabled for interaction.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Whether this component currently has input focus.
    /// </summary>
    bool HasFocus { get; }

    /// <summary>
    /// Data bound to this component.
    /// </summary>
    object? DataContext { get; set; }

    /// <summary>
    /// Custom properties for framework-specific features.
    /// </summary>
    Dictionary<string, object>? Properties { get; set; }

    /// <summary>
    /// Observable stream of UI events from this component.
    /// </summary>
    IObservable<UIEvent> Events { get; }

    /// <summary>
    /// Renders the component using the specified UI context.
    /// </summary>
    /// <param name="context">Framework-specific rendering context.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the rendering operation.</returns>
    Task RenderAsync(UIContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the component's state and triggers re-rendering if necessary.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the update operation.</returns>
    Task UpdateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a child component to this component.
    /// </summary>
    /// <param name="child">Component to add as a child.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the add operation.</returns>
    Task AddChildAsync(IUIComponent child, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a child component from this component.
    /// </summary>
    /// <param name="child">Component to remove.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the remove operation.</returns>
    Task RemoveChildAsync(IUIComponent child, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gives input focus to this component.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the focus operation.</returns>
    Task FocusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Measures the desired size of this component for layout purposes.
    /// </summary>
    /// <param name="availableSize">Available space for the component.</param>
    /// <param name="context">UI context for measurement.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Desired size of the component.</returns>
    Task<(float Width, float Height)> MeasureAsync((float Width, float Height) availableSize, UIContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Arranges the component within the specified bounds.
    /// </summary>
    /// <param name="bounds">Final bounds for the component.</param>
    /// <param name="context">UI context for arrangement.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the arrange operation.</returns>
    Task ArrangeAsync(UILayout bounds, UIContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Base implementation of IUIComponent providing common functionality.
/// </summary>
public abstract class UIComponentBase : IUIComponent
{
    private readonly List<IUIComponent> _children = new();
    private readonly Subject<UIEvent> _eventSubject = new();

    /// <inheritdoc />
    public string Id { get; protected set; } = Guid.NewGuid().ToString();

    /// <inheritdoc />
    public abstract string Type { get; }

    /// <inheritdoc />
    public IUIComponent? Parent { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<IUIComponent> Children => _children.AsReadOnly();

    /// <inheritdoc />
    public UIStyle? Style { get; set; }

    /// <inheritdoc />
    public UILayout? Layout { get; set; }

    /// <inheritdoc />
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc />
    public bool HasFocus { get; private set; }

    /// <inheritdoc />
    public object? DataContext { get; set; }

    /// <inheritdoc />
    public Dictionary<string, object>? Properties { get; set; }

    /// <inheritdoc />
    public IObservable<UIEvent> Events => _eventSubject.AsObservable();

    /// <inheritdoc />
    public abstract Task RenderAsync(UIContext context, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public virtual Task UpdateAsync(CancellationToken cancellationToken = default)
    {
        OnLifecycleEvent(UILifecycleStage.Updating);
        OnLifecycleEvent(UILifecycleStage.Updated);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual async Task AddChildAsync(IUIComponent child, CancellationToken cancellationToken = default)
    {
        if (child is UIComponentBase childBase)
        {
            childBase.Parent = this;
        }
        _children.Add(child);
        await child.UpdateAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task RemoveChildAsync(IUIComponent child, CancellationToken cancellationToken = default)
    {
        if (_children.Remove(child))
        {
            if (child is UIComponentBase childBase)
            {
                childBase.Parent = null;
            }
            await child.DisposeAsync();
        }
    }

    /// <inheritdoc />
    public virtual Task FocusAsync(CancellationToken cancellationToken = default)
    {
        HasFocus = true;
        OnFocusEvent(true);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public abstract Task<(float Width, float Height)> MeasureAsync((float Width, float Height) availableSize, UIContext context, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task ArrangeAsync(UILayout bounds, UIContext context, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public virtual async ValueTask DisposeAsync()
    {
        OnLifecycleEvent(UILifecycleStage.Disposing);
        
        foreach (var child in _children.ToList())
        {
            await child.DisposeAsync();
        }
        _children.Clear();
        
        OnLifecycleEvent(UILifecycleStage.Disposed);
        _eventSubject.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Publishes a UI event to the event stream.
    /// </summary>
    protected void PublishEvent(UIEvent uiEvent)
    {
        _eventSubject.OnNext(uiEvent);
    }

    /// <summary>
    /// Helper method to publish lifecycle events.
    /// </summary>
    protected void OnLifecycleEvent(UILifecycleStage stage)
    {
        PublishEvent(new UILifecycleEvent { ComponentId = Id, Stage = stage });
    }

    /// <summary>
    /// Helper method to publish focus events.
    /// </summary>
    protected void OnFocusEvent(bool hasFocus)
    {
        PublishEvent(new UIFocusEvent { ComponentId = Id, HasFocus = hasFocus });
    }

    /// <summary>
    /// Helper method to publish click events.
    /// </summary>
    protected void OnClickEvent((float X, float Y)? position = null, int button = 0)
    {
        PublishEvent(new UIClickEvent { ComponentId = Id, Position = position, Button = button });
    }

    /// <summary>
    /// Helper method to publish value changed events.
    /// </summary>
    protected void OnValueChangedEvent(object? oldValue, object? newValue)
    {
        PublishEvent(new UIValueChangedEvent { ComponentId = Id, OldValue = oldValue, NewValue = newValue });
    }
}