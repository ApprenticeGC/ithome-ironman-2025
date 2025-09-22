using GameConsole.Core.Abstractions;

namespace GameConsole.UI.Core;

/// <summary>
/// Base interface for UI services in the GameConsole system.
/// Extends the core service interface with UI-specific capabilities.
/// </summary>
public interface IUIService : IService, ICapabilityProvider
{
    /// <summary>
    /// Gets the UI profile management capability.
    /// </summary>
    IUIProfileCapability? ProfileManager { get; }

    /// <summary>
    /// Applies a UI profile configuration to the service.
    /// </summary>
    /// <param name="profile">The UI profile to apply.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the profile was applied successfully.</returns>
    Task<bool> ApplyProfileAsync(UIProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current UI mode being used by this service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The current UI mode string.</returns>
    Task<string> GetCurrentModeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the supported UI modes by this service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A collection of supported UI mode strings.</returns>
    Task<IEnumerable<string>> GetSupportedModesAsync(CancellationToken cancellationToken = default);
}