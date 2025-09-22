namespace GameConsole.UI.Configuration;

/// <summary>
/// Represents a UI profile that defines the behavior and configuration
/// for different UI modes (Console, Unity-like, Godot-like, etc.).
/// </summary>
public interface IUIProfile
{
    /// <summary>
    /// Gets the unique identifier for this profile.
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Gets the human-readable name of this profile.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Gets the description of this profile.
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Gets the service configurations for this profile.
    /// Maps service interface types to their implementation types.
    /// </summary>
    IReadOnlyDictionary<Type, Type> ServiceConfigurations { get; }
    
    /// <summary>
    /// Gets the capability configurations for this profile.
    /// Specifies which capabilities should be enabled.
    /// </summary>
    IReadOnlySet<Type> EnabledCapabilities { get; }
    
    /// <summary>
    /// Gets the profile-specific settings as key-value pairs.
    /// </summary>
    IReadOnlyDictionary<string, object> Settings { get; }
    
    /// <summary>
    /// Gets a value indicating whether this profile is active.
    /// </summary>
    bool IsActive { get; }
}