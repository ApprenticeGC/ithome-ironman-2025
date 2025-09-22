using System;
using System.Threading.Tasks;

namespace GameConsole.UI.Core;

/// <summary>
/// Abstract base for mode-specific UI profiles
/// </summary>
public abstract class UIProfile
{
    public string Name { get; protected set; } = string.Empty;
    public ConsoleMode TargetMode { get; protected set; }
    public UICapabilities SupportedCapabilities { get; protected set; }
    public UIProfileMetadata Metadata { get; protected set; } = new();

    /// <summary>
    /// Get the command set for this profile
    /// </summary>
    public abstract CommandSet GetCommandSet();

    /// <summary>
    /// Get the layout configuration for this profile
    /// </summary>
    public abstract LayoutConfiguration GetLayoutConfiguration();

    /// <summary>
    /// Handle a command in this profile's context
    /// </summary>
    public abstract Task<UICommandResult> HandleCommandAsync(string command, UIContext context);

    /// <summary>
    /// Configure this profile with given settings
    /// </summary>
    public abstract void Configure(UIConfiguration config);

    /// <summary>
    /// Initialize the profile asynchronously
    /// </summary>
    public virtual Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clean up profile resources
    /// </summary>
    public virtual Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Validate if this profile supports the given mode
    /// </summary>
    public virtual bool SupportsMode(ConsoleMode mode)
    {
        return (TargetMode & mode) != 0;
    }

    /// <summary>
    /// Check if a capability is supported
    /// </summary>
    public virtual bool HasCapability(UICapabilities capability)
    {
        return (SupportedCapabilities & capability) != 0;
    }
}