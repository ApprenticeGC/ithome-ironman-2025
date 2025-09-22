namespace GameConsole.UI.Configuration;

/// <summary>
/// Default implementation of IUIProfile representing a UI profile configuration.
/// </summary>
public class UIProfile : IUIProfile
{
    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public IReadOnlyDictionary<Type, Type> ServiceConfigurations { get; }
    public IReadOnlySet<Type> EnabledCapabilities { get; }
    public IReadOnlyDictionary<string, object> Settings { get; }
    public bool IsActive { get; private set; }

    public UIProfile(
        string id,
        string name, 
        string description,
        IReadOnlyDictionary<Type, Type> serviceConfigurations,
        IReadOnlySet<Type> enabledCapabilities,
        IReadOnlyDictionary<string, object> settings)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Profile ID cannot be null or empty", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Profile name cannot be null or empty", nameof(name));
            
        Id = id;
        Name = name;
        Description = description ?? string.Empty;
        ServiceConfigurations = serviceConfigurations ?? new Dictionary<Type, Type>();
        EnabledCapabilities = enabledCapabilities ?? new HashSet<Type>();
        Settings = settings ?? new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Marks this profile as active.
    /// This should only be called by the ProfileConfigurationService.
    /// </summary>
    internal void SetActive(bool active)
    {
        IsActive = active;
    }
}