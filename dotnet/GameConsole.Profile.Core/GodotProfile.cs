namespace GameConsole.Profile.Core;

/// <summary>
/// Godot-like profile configuration.
/// Simulates Godot engine behaviors and interface patterns for developers familiar with Godot workflows.
/// </summary>
public class GodotProfile : IProfileConfiguration
{
    /// <inheritdoc />
    public string ProfileId => "godot";

    /// <inheritdoc />
    public string DisplayName => "Godot-like Interface";

    /// <inheritdoc />
    public string Description => "Interface and behaviors that simulate Godot engine development environment";

    /// <inheritdoc />
    public int Priority => 40; // Lower priority than Unity

    /// <inheritdoc />
    public Task<bool> IsSupported(CancellationToken cancellationToken = default)
    {
        // Godot-like profile could check for specific capabilities or environment
        // For now, assume it's always available but with lower priority
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<IEnumerable<Type>> GetCapabilityProviders(CancellationToken cancellationToken = default)
    {
        // Godot-like providers would implement Godot-familiar patterns
        var providers = new List<Type>
        {
            // These would be actual types when Godot providers are implemented
            // typeof(GodotNodeProvider),
            // typeof(GodotScriptProvider),
            // typeof(GodotSignalProvider)
        };

        return Task.FromResult<IEnumerable<Type>>(providers);
    }

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, object>> GetConfigurationSettings(CancellationToken cancellationToken = default)
    {
        var settings = new Dictionary<string, object>
        {
            ["ui.mode"] = "godot",
            ["graphics.mode"] = "node2d",
            ["input.method"] = "godot_input",
            ["scene.tree"] = true,
            ["node.system"] = "godot",
            ["signals.enabled"] = true,
            ["scripting.language"] = "gdscript"
        };

        return Task.FromResult<IReadOnlyDictionary<string, object>>(settings);
    }
}