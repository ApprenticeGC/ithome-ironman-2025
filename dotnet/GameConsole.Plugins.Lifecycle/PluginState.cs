namespace GameConsole.Plugins.Lifecycle;

/// <summary>
/// Represents the various states a plugin can be in during its lifecycle.
/// These states form a state machine that governs plugin operations and transitions.
/// </summary>
public enum PluginState
{
    /// <summary>
    /// Initial state when plugin is discovered but not yet loaded.
    /// </summary>
    Discovered = 0,

    /// <summary>
    /// Plugin assembly has been loaded into memory but not yet configured.
    /// </summary>
    Loaded = 1,

    /// <summary>
    /// Plugin is being configured with its runtime context.
    /// </summary>
    Configuring = 2,

    /// <summary>
    /// Plugin has been configured and context is available.
    /// </summary>
    Configured = 3,

    /// <summary>
    /// Plugin is being initialized.
    /// </summary>
    Initializing = 4,

    /// <summary>
    /// Plugin has been initialized and is ready to start.
    /// </summary>
    Initialized = 5,

    /// <summary>
    /// Plugin is being started.
    /// </summary>
    Starting = 6,

    /// <summary>
    /// Plugin is running and operational.
    /// </summary>
    Running = 7,

    /// <summary>
    /// Plugin is being stopped gracefully.
    /// </summary>
    Stopping = 8,

    /// <summary>
    /// Plugin has been stopped and is no longer operational.
    /// </summary>
    Stopped = 9,

    /// <summary>
    /// Plugin is being prepared for unloading.
    /// </summary>
    Unloading = 10,

    /// <summary>
    /// Plugin has been unloaded from memory.
    /// </summary>
    Unloaded = 11,

    /// <summary>
    /// Plugin has encountered an error and is in a failed state.
    /// </summary>
    Failed = 12,

    /// <summary>
    /// Plugin is undergoing recovery after a failure.
    /// </summary>
    Recovering = 13,

    /// <summary>
    /// Plugin is being updated to a new version.
    /// </summary>
    Updating = 14
}