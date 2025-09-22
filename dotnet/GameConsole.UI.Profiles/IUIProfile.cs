namespace GameConsole.UI.Profiles;

/// <summary>
/// Represents a UI profile that defines how the user interface should behave and appear.
/// Profiles can simulate different engine behaviors (Unity/Godot) by configuring different providers and systems.
/// </summary>
public interface IUIProfile
{
    /// <summary>
    /// Gets the unique identifier for this UI profile.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the human-readable name of this UI profile.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of what this UI profile provides.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the UI mode type this profile represents.
    /// </summary>
    UIMode Mode { get; }

    /// <summary>
    /// Gets the configuration properties for this profile.
    /// </summary>
    IReadOnlyDictionary<string, object> Properties { get; }

    /// <summary>
    /// Gets a value indicating whether this profile is currently active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Activates this UI profile asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async activation operation.</returns>
    Task ActivateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates this UI profile asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async deactivation operation.</returns>
    Task DeactivateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a configuration value for the specified key.
    /// </summary>
    /// <typeparam name="T">The type to convert the value to.</typeparam>
    /// <param name="key">The configuration key.</param>
    /// <param name="defaultValue">The default value if the key is not found.</param>
    /// <returns>The configuration value or the default value.</returns>
    T GetProperty<T>(string key, T defaultValue = default!);
}