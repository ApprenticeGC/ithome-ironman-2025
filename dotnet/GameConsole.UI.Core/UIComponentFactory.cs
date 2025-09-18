namespace GameConsole.UI.Core;

/// <summary>
/// Interface for framework-agnostic UI component factories.
/// Creates appropriate framework-specific components based on the UI context.
/// </summary>
public interface IUIComponentFactory
{
    /// <summary>
    /// Gets the UI framework type this factory creates components for.
    /// </summary>
    UIFrameworkType FrameworkType { get; }
    
    /// <summary>
    /// Gets the supported component types this factory can create.
    /// </summary>
    IReadOnlySet<string> SupportedComponentTypes { get; }
    
    /// <summary>
    /// Creates a UI component of the specified type.
    /// </summary>
    /// <param name="componentType">The type of component to create.</param>
    /// <param name="context">The UI context for component creation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the created component.</returns>
    Task<IUIComponent> CreateComponentAsync(string componentType, UIContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a UI component of the specified type with initial data.
    /// </summary>
    /// <param name="componentType">The type of component to create.</param>
    /// <param name="data">The initial data for the component.</param>
    /// <param name="context">The UI context for component creation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the created component.</returns>
    Task<IUIComponent> CreateComponentAsync(string componentType, object? data, UIContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a UI component of the specified type with initial properties.
    /// </summary>
    /// <param name="componentType">The type of component to create.</param>
    /// <param name="properties">The initial properties for the component.</param>
    /// <param name="context">The UI context for component creation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the created component.</returns>
    Task<IUIComponent> CreateComponentAsync(string componentType, Dictionary<string, object> properties, UIContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a UI component with full configuration.
    /// </summary>
    /// <param name="componentType">The type of component to create.</param>
    /// <param name="data">The initial data for the component.</param>
    /// <param name="properties">The initial properties for the component.</param>
    /// <param name="context">The UI context for component creation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the created component.</returns>
    Task<IUIComponent> CreateComponentAsync(string componentType, object? data, Dictionary<string, object> properties, UIContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if the factory can create a component of the specified type.
    /// </summary>
    /// <param name="componentType">The type of component to check.</param>
    /// <returns>True if the factory can create the component type; otherwise, false.</returns>
    bool CanCreateComponent(string componentType);
    
    /// <summary>
    /// Gets the default properties for a component type.
    /// </summary>
    /// <param name="componentType">The component type to get default properties for.</param>
    /// <returns>A dictionary of default properties for the component type.</returns>
    IReadOnlyDictionary<string, object> GetDefaultProperties(string componentType);
}

/// <summary>
/// Factory class for creating framework-agnostic UI components.
/// Implements the factory pattern to create appropriate components based on the UI framework type.
/// </summary>
public class UIComponentFactory : IUIComponentFactory
{
    private readonly Dictionary<string, Func<UIContext, Task<IUIComponent>>> _componentCreators;
    private readonly Dictionary<string, Dictionary<string, object>> _defaultProperties;
    
    /// <inheritdoc />
    public UIFrameworkType FrameworkType { get; }
    
    /// <inheritdoc />
    public IReadOnlySet<string> SupportedComponentTypes { get; }
    
    /// <summary>
    /// Initializes a new instance of the UIComponentFactory class.
    /// </summary>
    /// <param name="frameworkType">The UI framework type this factory creates components for.</param>
    public UIComponentFactory(UIFrameworkType frameworkType)
    {
        FrameworkType = frameworkType;
        _componentCreators = new Dictionary<string, Func<UIContext, Task<IUIComponent>>>();
        _defaultProperties = new Dictionary<string, Dictionary<string, object>>();
        
        RegisterDefaultComponentTypes();
        SupportedComponentTypes = _componentCreators.Keys.ToHashSet();
    }
    
    /// <inheritdoc />
    public async Task<IUIComponent> CreateComponentAsync(string componentType, UIContext context, CancellationToken cancellationToken = default)
    {
        if (!CanCreateComponent(componentType))
        {
            throw new ArgumentException($"Component type '{componentType}' is not supported by this factory.", nameof(componentType));
        }
        
        var creator = _componentCreators[componentType];
        return await creator(context);
    }
    
    /// <inheritdoc />
    public async Task<IUIComponent> CreateComponentAsync(string componentType, object? data, UIContext context, CancellationToken cancellationToken = default)
    {
        var component = await CreateComponentAsync(componentType, context, cancellationToken);
        await component.UpdateAsync(data, cancellationToken);
        return component;
    }
    
    /// <inheritdoc />
    public async Task<IUIComponent> CreateComponentAsync(string componentType, Dictionary<string, object> properties, UIContext context, CancellationToken cancellationToken = default)
    {
        var component = await CreateComponentAsync(componentType, context, cancellationToken);
        
        foreach (var property in properties)
        {
            await component.SetPropertyAsync(property.Key, property.Value, cancellationToken);
        }
        
        return component;
    }
    
    /// <inheritdoc />
    public async Task<IUIComponent> CreateComponentAsync(string componentType, object? data, Dictionary<string, object> properties, UIContext context, CancellationToken cancellationToken = default)
    {
        var component = await CreateComponentAsync(componentType, properties, context, cancellationToken);
        await component.UpdateAsync(data, cancellationToken);
        return component;
    }
    
    /// <inheritdoc />
    public bool CanCreateComponent(string componentType)
    {
        return _componentCreators.ContainsKey(componentType);
    }
    
    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> GetDefaultProperties(string componentType)
    {
        return _defaultProperties.TryGetValue(componentType, out var properties) 
            ? properties 
            : new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Registers a component creator for the specified component type.
    /// </summary>
    /// <param name="componentType">The component type to register.</param>
    /// <param name="creator">The component creator function.</param>
    /// <param name="defaultProperties">The default properties for the component type.</param>
    protected void RegisterComponentType(string componentType, Func<UIContext, Task<IUIComponent>> creator, Dictionary<string, object>? defaultProperties = null)
    {
        _componentCreators[componentType] = creator ?? throw new ArgumentNullException(nameof(creator));
        _defaultProperties[componentType] = defaultProperties ?? new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Registers the default component types supported by all frameworks.
    /// </summary>
    protected virtual void RegisterDefaultComponentTypes()
    {
        // Register basic component types that all frameworks should support
        RegisterComponentType("text", context => Task.FromResult<IUIComponent>(new BasicUIComponent("text", "text")));
        RegisterComponentType("button", context => Task.FromResult<IUIComponent>(new BasicUIComponent("button", "button")));
        RegisterComponentType("input", context => Task.FromResult<IUIComponent>(new BasicUIComponent("input", "input")));
        RegisterComponentType("container", context => Task.FromResult<IUIComponent>(new BasicUIComponent("container", "container")));
        RegisterComponentType("list", context => Task.FromResult<IUIComponent>(new BasicUIComponent("list", "list")));
        RegisterComponentType("table", context => Task.FromResult<IUIComponent>(new BasicUIComponent("table", "table")));
        RegisterComponentType("progress", context => Task.FromResult<IUIComponent>(new BasicUIComponent("progress", "progress")));
        RegisterComponentType("menu", context => Task.FromResult<IUIComponent>(new BasicUIComponent("menu", "menu")));
    }
}

/// <summary>
/// Basic implementation of IUIComponent for default component types.
/// Provides a minimal, framework-agnostic component implementation.
/// </summary>
public class BasicUIComponent : IUIComponent, IAsyncDisposable
{
    private readonly List<IUIComponent> _children = new();
    private readonly Dictionary<string, object> _properties = new();
    private object? _data;
    private bool _isVisible = true;
    private bool _isEnabled = true;
    private bool _disposed;
    
    /// <inheritdoc />
    public string Id { get; }
    
    /// <inheritdoc />
    public string ComponentType { get; }
    
    /// <inheritdoc />
    public object? Data 
    { 
        get => _data;
        set
        {
            var previousData = _data;
            _data = value;
            DataChanged?.Invoke(this, new UIDataChangedEventArgs(previousData, value));
        }
    }
    
    /// <inheritdoc />
    public IReadOnlyList<IUIComponent> Children => _children.AsReadOnly();
    
    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> Properties => _properties.AsReadOnly();
    
    /// <inheritdoc />
    public bool IsVisible => _isVisible;
    
    /// <inheritdoc />
    public bool IsEnabled => _isEnabled;
    
    /// <inheritdoc />
    public event EventHandler<UIComponentEventArgs>? ComponentEvent;
    
    /// <inheritdoc />
    public event EventHandler<UIDataChangedEventArgs>? DataChanged;
    
    /// <summary>
    /// Initializes a new instance of the BasicUIComponent class.
    /// </summary>
    /// <param name="id">The unique identifier for the component.</param>
    /// <param name="componentType">The type of the component.</param>
    public BasicUIComponent(string id, string componentType)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        ComponentType = componentType ?? throw new ArgumentNullException(nameof(componentType));
    }
    
    /// <inheritdoc />
    public Task InitializeAsync(UIContext context, CancellationToken cancellationToken = default)
    {
        ComponentEvent?.Invoke(this, new UIComponentEventArgs(this, "initialized", context));
        return Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public Task<object> RenderAsync(UIContext context, CancellationToken cancellationToken = default)
    {
        // Return a basic representation of the component for rendering
        var renderData = new
        {
            Id,
            ComponentType,
            Data,
            Properties = _properties,
            Children = _children.Select(c => c.Id).ToArray(),
            IsVisible,
            IsEnabled
        };
        
        ComponentEvent?.Invoke(this, new UIComponentEventArgs(this, "rendered", renderData));
        return Task.FromResult<object>(renderData);
    }
    
    /// <inheritdoc />
    public Task UpdateAsync(object? data, CancellationToken cancellationToken = default)
    {
        Data = data;
        ComponentEvent?.Invoke(this, new UIComponentEventArgs(this, "updated", data));
        return Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public Task SetPropertyAsync(string key, object value, CancellationToken cancellationToken = default)
    {
        _properties[key] = value;
        ComponentEvent?.Invoke(this, new UIComponentEventArgs(this, "property_changed", new { Key = key, Value = value }));
        return Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public T GetProperty<T>(string key, T defaultValue = default!)
    {
        if (_properties.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }
    
    /// <inheritdoc />
    public Task AddChildAsync(IUIComponent child, CancellationToken cancellationToken = default)
    {
        _children.Add(child);
        ComponentEvent?.Invoke(this, new UIComponentEventArgs(this, "child_added", child));
        return Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public Task<bool> RemoveChildAsync(IUIComponent child, CancellationToken cancellationToken = default)
    {
        var removed = _children.Remove(child);
        if (removed)
        {
            ComponentEvent?.Invoke(this, new UIComponentEventArgs(this, "child_removed", child));
        }
        return Task.FromResult(removed);
    }
    
    /// <inheritdoc />
    public Task SetVisibilityAsync(bool visible, CancellationToken cancellationToken = default)
    {
        _isVisible = visible;
        ComponentEvent?.Invoke(this, new UIComponentEventArgs(this, "visibility_changed", visible));
        return Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public Task SetEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        _isEnabled = enabled;
        ComponentEvent?.Invoke(this, new UIComponentEventArgs(this, "enabled_changed", enabled));
        return Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        
        // Dispose all child components
        foreach (var child in _children)
        {
            if (child is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
        }
        
        _children.Clear();
        _properties.Clear();
        _disposed = true;
        
        ComponentEvent?.Invoke(this, new UIComponentEventArgs(this, "disposed", null));
    }
}