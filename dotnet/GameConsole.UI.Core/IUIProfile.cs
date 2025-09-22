namespace GameConsole.UI.Core;

/// <summary>
/// Defines the contract for UI profiles in the GameConsole system.
/// UI profiles encapsulate different UI behavior patterns and configurations.
/// </summary>
public interface IUIProfile
{
    /// <summary>
    /// Gets the configuration associated with this UI profile.
    /// </summary>
    UIProfileConfiguration Configuration { get; }

    /// <summary>
    /// Gets the unique identifier of the UI profile.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the display name of the UI profile.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the type of UI profile.
    /// </summary>
    UIProfileType ProfileType { get; }

    /// <summary>
    /// Gets a value indicating whether this profile is currently enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Activates the UI profile, applying its configuration to the UI system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async activation operation.</returns>
    Task ActivateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates the UI profile, removing its configuration from the UI system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async deactivation operation.</returns>
    Task DeactivateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the UI profile configuration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async validation operation that returns validation results.</returns>
    Task<UIProfileValidationResult> ValidateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a configuration value by key.
    /// </summary>
    /// <typeparam name="T">The type of the configuration value.</typeparam>
    /// <param name="key">The configuration key.</param>
    /// <param name="defaultValue">The default value to return if the key is not found.</param>
    /// <returns>The configuration value or the default value.</returns>
    T GetConfigurationValue<T>(string key, T defaultValue = default!);
}