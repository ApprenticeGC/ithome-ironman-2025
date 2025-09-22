using GameConsole.Core.Abstractions;

namespace GameConsole.Configuration.Security;

/// <summary>
/// Capability interface for UI profile configuration management.
/// Provides functionality to create, manage, and switch between different UI profiles.
/// </summary>
public interface IUIProfileCapability : ICapabilityProvider
{
    /// <summary>
    /// Gets the current UI profile configuration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Current UI profile configuration.</returns>
    Task<UIProfileConfiguration> GetUIProfileConfigurationAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Saves the UI profile configuration.
    /// </summary>
    /// <param name="configuration">Configuration to save.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the save operation.</returns>
    Task SaveUIProfileConfigurationAsync(UIProfileConfiguration configuration, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new UI profile.
    /// </summary>
    /// <param name="profileName">Name of the new profile.</param>
    /// <param name="theme">UI theme for the profile.</param>
    /// <param name="layoutMode">UI layout mode for the profile.</param>
    /// <param name="basedOnProfile">Optional profile to copy settings from.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The newly created UI profile configuration.</returns>
    Task<UIProfileConfiguration> CreateUIProfileAsync(string profileName, UITheme theme = UITheme.Default, 
        UILayoutMode layoutMode = UILayoutMode.Auto, string? basedOnProfile = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Switches to a different UI profile.
    /// </summary>
    /// <param name="profileName">Name of the profile to switch to.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the switch operation.</returns>
    Task SwitchUIProfileAsync(string profileName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all available UI profiles.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Collection of available UI profile names.</returns>
    Task<IEnumerable<string>> GetAvailableUIProfilesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes a UI profile.
    /// </summary>
    /// <param name="profileName">Name of the profile to delete.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the delete operation.</returns>
    Task DeleteUIProfileAsync(string profileName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates a specific UI setting in the current profile.
    /// </summary>
    /// <param name="key">Setting key.</param>
    /// <param name="value">Setting value.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the update operation.</returns>
    Task UpdateUISettingAsync(string key, string value, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a specific UI setting from the current profile.
    /// </summary>
    /// <param name="key">Setting key.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The setting value, or null if not found.</returns>
    Task<string?> GetUISettingAsync(string key, CancellationToken cancellationToken = default);
}