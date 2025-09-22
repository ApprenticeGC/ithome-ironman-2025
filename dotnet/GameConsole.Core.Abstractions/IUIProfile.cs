namespace GameConsole.Core.Abstractions;

/// <summary>
/// Supported UI profile types that define different interaction modes and behaviors.
/// </summary>
public enum UIProfileType
{
    /// <summary>
    /// Text-based User Interface optimized for console/terminal environments.
    /// </summary>
    TUI,
    
    /// <summary>
    /// Unity-style game engine interface with component-based architecture.
    /// </summary>
    Unity,
    
    /// <summary>
    /// Godot-style game engine interface with scene-node architecture.
    /// </summary>
    Godot,
    
    /// <summary>
    /// Custom profile type for specialized use cases.
    /// </summary>
    Custom
}

/// <summary>
/// Represents a UI profile that defines the visual and interaction behavior of the system.
/// Profiles control which providers and systems are used to simulate different engine behaviors.
/// </summary>
public interface IUIProfile
{
    /// <summary>
    /// Unique identifier for this profile.
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Human-readable name for this profile.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Description of what this profile provides.
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// The type of UI profile this represents.
    /// </summary>
    UIProfileType ProfileType { get; }
    
    /// <summary>
    /// Version of this profile for compatibility tracking.
    /// </summary>
    string Version { get; }
    
    /// <summary>
    /// Whether this profile is currently active.
    /// </summary>
    bool IsActive { get; }
    
    /// <summary>
    /// Configuration settings specific to this profile.
    /// </summary>
    IReadOnlyDictionary<string, object> Configuration { get; }
    
    /// <summary>
    /// Service provider types that should be used with this profile.
    /// Maps service interfaces to their preferred implementation types.
    /// </summary>
    IReadOnlyDictionary<Type, Type> PreferredProviders { get; }
    
    /// <summary>
    /// Activates this profile, applying its configuration and provider preferences.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async activation operation.</returns>
    Task ActivateAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deactivates this profile, cleaning up its configuration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async deactivation operation.</returns>
    Task DeactivateAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a specific configuration value for this profile.
    /// </summary>
    /// <typeparam name="T">The type of the configuration value.</typeparam>
    /// <param name="key">The configuration key.</param>
    /// <param name="defaultValue">Default value if key is not found.</param>
    /// <returns>The configuration value or default value if not found.</returns>
    T GetConfiguration<T>(string key, T defaultValue = default!);
    
    /// <summary>
    /// Sets a configuration value for this profile.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The configuration value.</param>
    void SetConfiguration(string key, object value);
}