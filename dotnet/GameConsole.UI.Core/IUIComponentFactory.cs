using GameConsole.Core.Abstractions;

namespace GameConsole.UI.Core;

/// <summary>
/// Provides framework-agnostic component creation and management.
/// </summary>
public interface IUIComponentFactory : ICapabilityProvider
{
    /// <summary>
    /// Creates a new UI component of the specified type.
    /// </summary>
    /// <param name="componentType">Type of component to create.</param>
    /// <param name="properties">Initial properties for the component.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The created component.</returns>
    Task<IUIComponent> CreateComponentAsync(string componentType, Dictionary<string, object>? properties = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new UI component of the specified generic type.
    /// </summary>
    /// <typeparam name="T">Type of component to create.</typeparam>
    /// <param name="properties">Initial properties for the component.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The created component.</returns>
    Task<T> CreateComponentAsync<T>(Dictionary<string, object>? properties = null, CancellationToken cancellationToken = default) where T : class, IUIComponent;

    /// <summary>
    /// Gets the supported component types for the current framework.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Collection of supported component type names.</returns>
    Task<IEnumerable<string>> GetSupportedComponentTypesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a component type is supported by the current framework.
    /// </summary>
    /// <param name="componentType">Component type name to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the component type is supported.</returns>
    Task<bool> IsComponentTypeSupportedAsync(string componentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a custom component type with the factory.
    /// </summary>
    /// <param name="componentType">Component type name.</param>
    /// <param name="factory">Factory function to create instances.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the registration operation.</returns>
    Task RegisterComponentTypeAsync(string componentType, Func<Dictionary<string, object>?, CancellationToken, Task<IUIComponent>> factory, CancellationToken cancellationToken = default);
}

/// <summary>
/// Base implementation of IUIComponentFactory providing common functionality.
/// </summary>
public abstract class UIComponentFactoryBase : IUIComponentFactory
{
    private readonly Dictionary<string, Func<Dictionary<string, object>?, CancellationToken, Task<IUIComponent>>> _componentFactories = new();

    /// <inheritdoc />
    public virtual Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        // Component factories provide the IUIComponentFactory capability
        return Task.FromResult<IEnumerable<Type>>(new[] { typeof(IUIComponentFactory) });
    }

    /// <inheritdoc />
    public virtual Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(this is T);
    }

    /// <inheritdoc />
    public virtual Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        return Task.FromResult(this as T);
    }

    /// <inheritdoc />
    public abstract Task<IUIComponent> CreateComponentAsync(string componentType, Dictionary<string, object>? properties = null, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public virtual async Task<T> CreateComponentAsync<T>(Dictionary<string, object>? properties = null, CancellationToken cancellationToken = default) where T : class, IUIComponent
    {
        var componentType = typeof(T).Name;
        var component = await CreateComponentAsync(componentType, properties, cancellationToken);
        
        if (component is not T typedComponent)
        {
            throw new InvalidOperationException($"Component of type {componentType} could not be cast to {typeof(T).Name}");
        }
        
        return typedComponent;
    }

    /// <inheritdoc />
    public virtual Task<IEnumerable<string>> GetSupportedComponentTypesAsync(CancellationToken cancellationToken = default)
    {
        var builtInTypes = GetBuiltInComponentTypes();
        var customTypes = _componentFactories.Keys;
        
        return Task.FromResult(builtInTypes.Concat(customTypes));
    }

    /// <inheritdoc />
    public virtual Task<bool> IsComponentTypeSupportedAsync(string componentType, CancellationToken cancellationToken = default)
    {
        var builtInTypes = GetBuiltInComponentTypes();
        var isSupported = builtInTypes.Contains(componentType) || _componentFactories.ContainsKey(componentType);
        
        return Task.FromResult(isSupported);
    }

    /// <inheritdoc />
    public virtual Task RegisterComponentTypeAsync(string componentType, Func<Dictionary<string, object>?, CancellationToken, Task<IUIComponent>> factory, CancellationToken cancellationToken = default)
    {
        _componentFactories[componentType] = factory;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the factory function for a custom component type.
    /// </summary>
    protected Func<Dictionary<string, object>?, CancellationToken, Task<IUIComponent>>? GetCustomFactory(string componentType)
    {
        return _componentFactories.TryGetValue(componentType, out var factory) ? factory : null;
    }

    /// <summary>
    /// Gets the built-in component types supported by this factory.
    /// </summary>
    protected abstract IEnumerable<string> GetBuiltInComponentTypes();
}