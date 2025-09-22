namespace GameConsole.UI.Core;

/// <summary>
/// Factory interface for creating framework-agnostic UI components.
/// Implementations provide framework-specific component instances that implement the common IUIComponent interface.
/// </summary>
public interface IUIComponentFactory
{
    /// <summary>
    /// The framework type this factory creates components for.
    /// </summary>
    FrameworkType FrameworkType { get; }
    
    /// <summary>
    /// Component types supported by this factory.
    /// </summary>
    IReadOnlySet<ComponentType> SupportedComponents { get; }
    
    /// <summary>
    /// Creates a component of the specified type with the given configuration.
    /// </summary>
    Task<IUIComponent> CreateComponentAsync<T>(
        ComponentType componentType,
        string id,
        T? configuration = default,
        CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Creates a component using a configuration dictionary.
    /// </summary>
    Task<IUIComponent> CreateComponentAsync(
        ComponentType componentType,
        string id,
        Dictionary<string, object>? configuration = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a button component.
    /// </summary>
    Task<IUIButton> CreateButtonAsync(
        string id,
        string text,
        Dictionary<string, object>? configuration = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a text input component.
    /// </summary>
    Task<IUITextInput> CreateTextInputAsync(
        string id,
        string? placeholder = null,
        string? initialValue = null,
        Dictionary<string, object>? configuration = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a label component.
    /// </summary>
    Task<IUILabel> CreateLabelAsync(
        string id,
        string text,
        Dictionary<string, object>? configuration = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a panel container component.
    /// </summary>
    Task<IUIPanel> CreatePanelAsync(
        string id,
        Dictionary<string, object>? configuration = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a menu component.
    /// </summary>
    Task<IUIMenu> CreateMenuAsync(
        string id,
        IEnumerable<string>? menuItems = null,
        Dictionary<string, object>? configuration = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a list component.
    /// </summary>
    Task<IUIList> CreateListAsync<T>(
        string id,
        IEnumerable<T>? items = null,
        Dictionary<string, object>? configuration = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a table component.
    /// </summary>
    Task<IUITable> CreateTableAsync(
        string id,
        Dictionary<string, object>? configuration = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a progress bar component.
    /// </summary>
    Task<IUIProgressBar> CreateProgressBarAsync(
        string id,
        double initialValue = 0.0,
        Dictionary<string, object>? configuration = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a checkbox component.
    /// </summary>
    Task<IUICheckbox> CreateCheckboxAsync(
        string id,
        string label,
        bool initialChecked = false,
        Dictionary<string, object>? configuration = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a dropdown component.
    /// </summary>
    Task<IUIDropdown> CreateDropdownAsync<T>(
        string id,
        IEnumerable<T>? options = null,
        T? selectedOption = default,
        Dictionary<string, object>? configuration = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a dialog component.
    /// </summary>
    Task<IUIDialog> CreateDialogAsync(
        string id,
        string title,
        string? content = null,
        Dictionary<string, object>? configuration = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if the factory can create a component of the specified type.
    /// </summary>
    bool CanCreate(ComponentType componentType);
    
    /// <summary>
    /// Gets the default configuration for a component type.
    /// </summary>
    Dictionary<string, object> GetDefaultConfiguration(ComponentType componentType);
    
    /// <summary>
    /// Validates a configuration dictionary for the specified component type.
    /// </summary>
    Task<bool> ValidateConfigurationAsync(ComponentType componentType, Dictionary<string, object> configuration, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates multiple components from a batch configuration.
    /// </summary>
    Task<IReadOnlyList<IUIComponent>> CreateComponentBatchAsync(
        IEnumerable<(ComponentType Type, string Id, Dictionary<string, object>? Configuration)> components,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Event fired when a component is created.
    /// </summary>
    event Func<IUIComponent, Task>? ComponentCreated;
    
    /// <summary>
    /// Event fired when component creation fails.
    /// </summary>
    event Func<ComponentType, string, Exception, Task>? ComponentCreationFailed;
}

/// <summary>
/// Base abstract implementation of IUIComponentFactory providing common functionality.
/// Framework-specific factories should inherit from this class.
/// </summary>
public abstract class UIComponentFactoryBase : IUIComponentFactory
{
    private readonly HashSet<ComponentType> _supportedComponents;
    
    protected UIComponentFactoryBase(FrameworkType frameworkType, params ComponentType[] supportedComponents)
    {
        FrameworkType = frameworkType;
        _supportedComponents = new HashSet<ComponentType>(supportedComponents);
    }
    
    /// <inheritdoc />
    public FrameworkType FrameworkType { get; }
    
    /// <inheritdoc />
    public IReadOnlySet<ComponentType> SupportedComponents => _supportedComponents;
    
    /// <inheritdoc />
    public event Func<IUIComponent, Task>? ComponentCreated;
    
    /// <inheritdoc />
    public event Func<ComponentType, string, Exception, Task>? ComponentCreationFailed;
    
    /// <inheritdoc />
    public abstract Task<IUIComponent> CreateComponentAsync<T>(
        ComponentType componentType,
        string id,
        T? configuration = default,
        CancellationToken cancellationToken = default) where T : class;
    
    /// <inheritdoc />
    public abstract Task<IUIComponent> CreateComponentAsync(
        ComponentType componentType,
        string id,
        Dictionary<string, object>? configuration = null,
        CancellationToken cancellationToken = default);
    
    /// <inheritdoc />
    public abstract Task<IUIButton> CreateButtonAsync(string id, string text, Dictionary<string, object>? configuration = null, CancellationToken cancellationToken = default);
    
    /// <inheritdoc />
    public abstract Task<IUITextInput> CreateTextInputAsync(string id, string? placeholder = null, string? initialValue = null, Dictionary<string, object>? configuration = null, CancellationToken cancellationToken = default);
    
    /// <inheritdoc />
    public abstract Task<IUILabel> CreateLabelAsync(string id, string text, Dictionary<string, object>? configuration = null, CancellationToken cancellationToken = default);
    
    /// <inheritdoc />
    public abstract Task<IUIPanel> CreatePanelAsync(string id, Dictionary<string, object>? configuration = null, CancellationToken cancellationToken = default);
    
    /// <inheritdoc />
    public abstract Task<IUIMenu> CreateMenuAsync(string id, IEnumerable<string>? menuItems = null, Dictionary<string, object>? configuration = null, CancellationToken cancellationToken = default);
    
    /// <inheritdoc />
    public abstract Task<IUIList> CreateListAsync<T>(string id, IEnumerable<T>? items = null, Dictionary<string, object>? configuration = null, CancellationToken cancellationToken = default);
    
    /// <inheritdoc />
    public abstract Task<IUITable> CreateTableAsync(string id, Dictionary<string, object>? configuration = null, CancellationToken cancellationToken = default);
    
    /// <inheritdoc />
    public abstract Task<IUIProgressBar> CreateProgressBarAsync(string id, double initialValue = 0, Dictionary<string, object>? configuration = null, CancellationToken cancellationToken = default);
    
    /// <inheritdoc />
    public abstract Task<IUICheckbox> CreateCheckboxAsync(string id, string label, bool initialChecked = false, Dictionary<string, object>? configuration = null, CancellationToken cancellationToken = default);
    
    /// <inheritdoc />
    public abstract Task<IUIDropdown> CreateDropdownAsync<T>(string id, IEnumerable<T>? options = null, T? selectedOption = default, Dictionary<string, object>? configuration = null, CancellationToken cancellationToken = default);
    
    /// <inheritdoc />
    public abstract Task<IUIDialog> CreateDialogAsync(string id, string title, string? content = null, Dictionary<string, object>? configuration = null, CancellationToken cancellationToken = default);
    
    /// <inheritdoc />
    public virtual bool CanCreate(ComponentType componentType) => _supportedComponents.Contains(componentType);
    
    /// <inheritdoc />
    public abstract Dictionary<string, object> GetDefaultConfiguration(ComponentType componentType);
    
    /// <inheritdoc />
    public virtual Task<bool> ValidateConfigurationAsync(ComponentType componentType, Dictionary<string, object> configuration, CancellationToken cancellationToken = default)
    {
        // Basic validation - check if component type is supported
        return Task.FromResult(CanCreate(componentType));
    }
    
    /// <inheritdoc />
    public async Task<IReadOnlyList<IUIComponent>> CreateComponentBatchAsync(
        IEnumerable<(ComponentType Type, string Id, Dictionary<string, object>? Configuration)> components,
        CancellationToken cancellationToken = default)
    {
        var results = new List<IUIComponent>();
        
        foreach (var (type, id, configuration) in components)
        {
            try
            {
                var component = await CreateComponentAsync(type, id, configuration, cancellationToken);
                results.Add(component);
                
                if (ComponentCreated != null)
                {
                    await ComponentCreated(component);
                }
            }
            catch (Exception ex)
            {
                if (ComponentCreationFailed != null)
                {
                    await ComponentCreationFailed(type, id, ex);
                }
                throw; // Re-throw to maintain original behavior
            }
        }
        
        return results;
    }
    
    /// <summary>
    /// Helper method to raise ComponentCreated event.
    /// </summary>
    protected async Task OnComponentCreatedAsync(IUIComponent component)
    {
        if (ComponentCreated != null)
        {
            await ComponentCreated(component);
        }
    }
    
    /// <summary>
    /// Helper method to raise ComponentCreationFailed event.
    /// </summary>
    protected async Task OnComponentCreationFailedAsync(ComponentType componentType, string id, Exception exception)
    {
        if (ComponentCreationFailed != null)
        {
            await ComponentCreationFailed(componentType, id, exception);
        }
    }
}