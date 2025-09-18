namespace GameConsole.Plugins.Lifecycle;

/// <summary>
/// Represents the current state of a plugin in its lifecycle.
/// </summary>
public enum PluginState
{
    /// <summary>
    /// Plugin is not loaded.
    /// </summary>
    NotLoaded,

    /// <summary>
    /// Plugin is loaded but not initialized.
    /// </summary>
    Loaded,

    /// <summary>
    /// Plugin is initialized but not started.
    /// </summary>
    Initialized,

    /// <summary>
    /// Plugin is running normally.
    /// </summary>
    Running,

    /// <summary>
    /// Plugin is stopping.
    /// </summary>
    Stopping,

    /// <summary>
    /// Plugin is stopped.
    /// </summary>
    Stopped,

    /// <summary>
    /// Plugin has failed and requires recovery.
    /// </summary>
    Failed,

    /// <summary>
    /// Plugin is in recovery mode.
    /// </summary>
    Recovering,

    /// <summary>
    /// Plugin is being updated.
    /// </summary>
    Updating,

    /// <summary>
    /// Plugin is being unloaded.
    /// </summary>
    Unloading
}

/// <summary>
/// Represents the health status of a plugin.
/// </summary>
public enum PluginHealth
{
    /// <summary>
    /// Plugin health is unknown.
    /// </summary>
    Unknown,

    /// <summary>
    /// Plugin is healthy and responding normally.
    /// </summary>
    Healthy,

    /// <summary>
    /// Plugin is showing degraded performance but still functional.
    /// </summary>
    Degraded,

    /// <summary>
    /// Plugin is unhealthy and may require intervention.
    /// </summary>
    Unhealthy,

    /// <summary>
    /// Plugin has failed and is not responding.
    /// </summary>
    Failed
}