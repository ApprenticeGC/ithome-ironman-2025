namespace GameConsole.UI.Core;

/// <summary>
/// Context information for UI framework operations, providing framework-specific rendering capabilities.
/// </summary>
/// <param name="Args">Command line arguments or initialization parameters.</param>
/// <param name="State">Framework-specific state dictionary.</param>
/// <param name="FrameworkType">The current UI framework type being used.</param>
/// <param name="Capabilities">The capabilities supported by the current framework.</param>
/// <param name="Properties">Additional framework-specific properties.</param>
public record UIContext(
    string[] Args,
    Dictionary<string, object> State,
    UIFrameworkType FrameworkType,
    UICapabilities Capabilities,
    Dictionary<string, object> Properties)
{
    /// <summary>
    /// Creates a new UIContext with default values.
    /// </summary>
    /// <param name="frameworkType">The framework type to use.</param>
    /// <param name="capabilities">The capabilities to support.</param>
    /// <returns>A new UIContext instance with default values.</returns>
    public static UIContext Create(UIFrameworkType frameworkType, UICapabilities capabilities)
    {
        return new UIContext(
            Array.Empty<string>(),
            new Dictionary<string, object>(),
            frameworkType,
            capabilities,
            new Dictionary<string, object>()
        );
    }
    
    /// <summary>
    /// Creates a copy of this context with additional state.
    /// </summary>
    /// <param name="key">The state key to add or update.</param>
    /// <param name="value">The state value to add or update.</param>
    /// <returns>A new UIContext with the updated state.</returns>
    public UIContext WithState(string key, object value)
    {
        var newState = new Dictionary<string, object>(State)
        {
            [key] = value
        };
        
        return this with { State = newState };
    }
    
    /// <summary>
    /// Creates a copy of this context with additional properties.
    /// </summary>
    /// <param name="key">The property key to add or update.</param>
    /// <param name="value">The property value to add or update.</param>
    /// <returns>A new UIContext with the updated properties.</returns>
    public UIContext WithProperty(string key, object value)
    {
        var newProperties = new Dictionary<string, object>(Properties)
        {
            [key] = value
        };
        
        return this with { Properties = newProperties };
    }
    
    /// <summary>
    /// Checks if the current framework supports a specific capability.
    /// </summary>
    /// <param name="capability">The capability to check.</param>
    /// <returns>True if the capability is supported; otherwise, false.</returns>
    public bool SupportsCapability(UICapabilities capability)
    {
        return (Capabilities & capability) == capability;
    }
}