using GameConsole.Core.Abstractions;

namespace GameConsole.UI.Profiles;

/// <summary>
/// Defines a UI profile for different interaction modes (Console, Web, Desktop).
/// Profiles determine the UI behavior, available commands, and layout configurations.
/// </summary>
public interface IUIProfile
{
    /// <summary>
    /// Unique identifier for the profile.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Human-readable name of the profile.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Target UI mode this profile is designed for.
    /// </summary>
    UIMode TargetMode { get; }

    /// <summary>
    /// Profile metadata including version, author, and capabilities.
    /// </summary>
    UIProfileMetadata Metadata { get; }

    /// <summary>
    /// Determines if this profile can be activated in the current context.
    /// </summary>
    /// <param name="context">The current UI context.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the profile can be activated, false otherwise.</returns>
    Task<bool> CanActivateAsync(UIContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates this profile, setting up the UI configuration and capabilities.
    /// </summary>
    /// <param name="context">The UI context to activate in.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    Task ActivateAsync(UIContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates this profile, cleaning up resources and state.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    Task DeactivateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the command set available in this profile.
    /// </summary>
    /// <returns>The command set for this profile.</returns>
    CommandSet GetCommandSet();

    /// <summary>
    /// Gets the layout configuration for this profile.
    /// </summary>
    /// <returns>The layout configuration for this profile.</returns>
    LayoutConfiguration GetLayoutConfiguration();

    /// <summary>
    /// Gets the UI capabilities supported by this profile.
    /// </summary>
    /// <returns>Flags indicating supported UI capabilities.</returns>
    UICapabilities GetSupportedCapabilities();
}

/// <summary>
/// UI interaction modes supported by the system.
/// </summary>
public enum UIMode
{
    /// <summary>
    /// Console/Terminal-based interface.
    /// </summary>
    Console,

    /// <summary>
    /// Web-based interface.
    /// </summary>
    Web,

    /// <summary>
    /// Desktop application interface.
    /// </summary>
    Desktop,

    /// <summary>
    /// Game runtime interface.
    /// </summary>
    Game,

    /// <summary>
    /// Editor/development interface.
    /// </summary>
    Editor
}

/// <summary>
/// UI capabilities that profiles can support.
/// </summary>
[Flags]
public enum UICapabilities
{
    None = 0,
    TextInput = 1 << 0,
    FileSelection = 1 << 1,
    ProgressDisplay = 1 << 2,
    InteractiveNavigation = 1 << 3,
    RealTimeUpdates = 1 << 4,
    GraphicalElements = 1 << 5,
    AudioOutput = 1 << 6,
    VideoPlayback = 1 << 7,
    NetworkAccess = 1 << 8,
    ClipboardAccess = 1 << 9,
    FileSystemAccess = 1 << 10,
    HotKeySupport = 1 << 11,
    Notifications = 1 << 12,
    Theming = 1 << 13,
    Plugins = 1 << 14,
    Scripting = 1 << 15
}