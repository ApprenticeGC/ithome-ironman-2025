namespace GameConsole.UI.Profiles;

/// <summary>
/// Defines the contract for UI profiles that provide mode-specific interface configurations.
/// Each profile optimizes the UI experience for specific workflows and user interactions.
/// </summary>
public interface IUIProfile
{
    /// <summary>
    /// Gets the unique name identifier for this profile.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the console mode this profile is designed for.
    /// </summary>
    ConsoleMode TargetMode { get; }

    /// <summary>
    /// Gets metadata about this profile including versioning and compatibility information.
    /// </summary>
    UIProfileMetadata Metadata { get; }

    /// <summary>
    /// Gets the command set available in this profile.
    /// Commands define the operations and tools available to users.
    /// </summary>
    /// <returns>The command set for this profile.</returns>
    CommandSet GetCommandSet();

    /// <summary>
    /// Gets the layout configuration for this profile.
    /// Layout defines how UI elements are arranged and displayed.
    /// </summary>
    /// <returns>The layout configuration for this profile.</returns>
    LayoutConfiguration GetLayoutConfiguration();

    /// <summary>
    /// Gets the keyboard shortcuts configuration for this profile.
    /// </summary>
    /// <returns>The key bindings for this profile.</returns>
    KeyBindingSet GetKeyBindings();

    /// <summary>
    /// Validates that this profile can be activated in the current environment.
    /// </summary>
    /// <param name="context">The current UI context.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>True if the profile can be activated; otherwise, false.</returns>
    Task<bool> CanActivateAsync(IUIContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates this profile, setting up the UI according to its configuration.
    /// </summary>
    /// <param name="context">The UI context to activate in.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task representing the async activation operation.</returns>
    Task ActivateAsync(IUIContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates this profile, cleaning up any resources and UI state.
    /// </summary>
    /// <param name="context">The UI context to deactivate from.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task representing the async deactivation operation.</returns>
    Task DeactivateAsync(IUIContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when the profile configuration needs to be persisted.
    /// This enables custom and user-defined profiles to save their state.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task representing the async persistence operation.</returns>
    Task SaveConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when the profile needs to reload its configuration.
    /// This supports profile hot-reloading during development.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task representing the async reload operation.</returns>
    Task ReloadConfigurationAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the UI context in which profiles operate.
/// Provides access to the current UI state and services.
/// </summary>
public interface IUIContext
{
    /// <summary>
    /// Gets the current console mode.
    /// </summary>
    ConsoleMode CurrentMode { get; }

    /// <summary>
    /// Gets the service provider for dependency resolution.
    /// </summary>
    IServiceProvider Services { get; }

    /// <summary>
    /// Gets properties and context data for the current UI session.
    /// </summary>
    IReadOnlyDictionary<string, object> Properties { get; }

    /// <summary>
    /// Gets a cancellation token that is signaled when the UI context is being shut down.
    /// </summary>
    CancellationToken ShutdownToken { get; }
}