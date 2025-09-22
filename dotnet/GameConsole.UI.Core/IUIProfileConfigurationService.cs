using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GameConsole.Core.Abstractions;

namespace GameConsole.UI.Core
{
    /// <summary>
    /// The main service interface for UI Profile Configuration System.
    /// This service provides the high-level operations for managing UI profiles and their configurations.
    /// </summary>
    public interface IUIProfileConfigurationService : IService
{
    /// <summary>
    /// Gets the profile manager for this service.
    /// </summary>
    IUIProfileManager ProfileManager { get; }

    /// <summary>
    /// Loads profiles from persistent storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async load operation.</returns>
    Task LoadProfilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all profiles to persistent storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async save operation.</returns>
    Task SaveProfilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports profiles from a configuration source.
    /// </summary>
    /// <param name="source">The source to import profiles from.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The number of profiles imported.</returns>
    Task<int> ImportProfilesAsync(string source, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports profiles to a configuration destination.
    /// </summary>
    /// <param name="destination">The destination to export profiles to.</param>
    /// <param name="profileIds">Optional list of specific profile IDs to export. If null, exports all profiles.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The number of profiles exported.</returns>
    Task<int> ExportProfilesAsync(
        string destination, 
        IEnumerable<string> profileIds = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the configuration to default profiles.
    /// This will create the standard TUI, Unity-like, and Godot-like profiles.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async reset operation.</returns>
    Task ResetToDefaultProfilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a profile configuration for correctness and completeness.
    /// </summary>
    /// <param name="profile">The profile to validate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A validation result indicating any issues found.</returns>
    Task<ProfileValidationResult> ValidateProfileAsync(IUIProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the configuration schema for a specific profile type.
    /// This helps understand what configuration options are available for each profile type.
    /// </summary>
    /// <param name="profileType">The profile type to get the schema for.</param>
    /// <returns>A dictionary describing the configuration schema.</returns>
    IReadOnlyDictionary<string, ConfigurationOptionSchema> GetConfigurationSchema(ProfileType profileType);
    }
}