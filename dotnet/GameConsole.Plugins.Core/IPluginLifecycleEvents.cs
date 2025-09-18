namespace GameConsole.Plugins.Core;

/// <summary>
/// Defines events for monitoring plugin lifecycle operations.
/// Provides comprehensive event notifications for all phases of plugin management including
/// configuration, initialization, startup, shutdown, and unloading.
/// </summary>
public interface IPluginLifecycleEvents
{
    /// <summary>
    /// Occurs when a plugin is about to be configured with its runtime context.
    /// This event can be used to modify configuration or cancel the operation.
    /// </summary>
    event EventHandler<PluginLifecycleEventArgs> PluginConfiguring;

    /// <summary>
    /// Occurs after a plugin has been successfully configured with its runtime context.
    /// </summary>
    event EventHandler<PluginLifecycleEventArgs> PluginConfigured;

    /// <summary>
    /// Occurs when a plugin is about to be initialized.
    /// This event can be used to perform pre-initialization setup or cancel the operation.
    /// </summary>
    event EventHandler<PluginLifecycleEventArgs> PluginInitializing;

    /// <summary>
    /// Occurs after a plugin has been successfully initialized.
    /// </summary>
    event EventHandler<PluginLifecycleEventArgs> PluginInitialized;

    /// <summary>
    /// Occurs when a plugin is about to be started.
    /// This event can be used to perform pre-startup operations or cancel the operation.
    /// </summary>
    event EventHandler<PluginLifecycleEventArgs> PluginStarting;

    /// <summary>
    /// Occurs after a plugin has been successfully started and is now running.
    /// </summary>
    event EventHandler<PluginLifecycleEventArgs> PluginStarted;

    /// <summary>
    /// Occurs when a plugin is about to be stopped.
    /// This event can be used to perform pre-shutdown operations.
    /// </summary>
    event EventHandler<PluginLifecycleEventArgs> PluginStopping;

    /// <summary>
    /// Occurs after a plugin has been successfully stopped.
    /// </summary>
    event EventHandler<PluginLifecycleEventArgs> PluginStopped;

    /// <summary>
    /// Occurs when a plugin is about to be unloaded from memory.
    /// This event can be used to perform cleanup operations or cancel the operation.
    /// </summary>
    event EventHandler<PluginLifecycleEventArgs> PluginUnloading;

    /// <summary>
    /// Occurs after a plugin has been successfully unloaded from memory.
    /// </summary>
    event EventHandler<PluginLifecycleEventArgs> PluginUnloaded;

    /// <summary>
    /// Occurs when an error occurs during any plugin lifecycle operation.
    /// This provides a centralized way to handle plugin-related errors.
    /// </summary>
    event EventHandler<PluginLifecycleEventArgs> PluginError;
}