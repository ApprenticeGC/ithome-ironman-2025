namespace GameConsole.Profile.Core;

/// <summary>
/// Unity-like profile configuration.
/// Simulates Unity engine behaviors and interface patterns for developers familiar with Unity workflows.
/// </summary>
public class UnityProfile : IProfileConfiguration
{
    /// <inheritdoc />
    public string ProfileId => "unity";

    /// <inheritdoc />
    public string DisplayName => "Unity-like Interface";

    /// <inheritdoc />
    public string Description => "Interface and behaviors that simulate Unity engine development environment";

    /// <inheritdoc />
    public int Priority => 50; // Medium priority

    /// <inheritdoc />
    public Task<bool> IsSupported(CancellationToken cancellationToken = default)
    {
        // Unity-like profile could check for specific capabilities or environment
        // For now, assume it's always available but with lower priority than TUI
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<IEnumerable<Type>> GetCapabilityProviders(CancellationToken cancellationToken = default)
    {
        // Unity-like providers would implement Unity-familiar patterns
        var providers = new List<Type>
        {
            // These would be actual types when Unity providers are implemented
            // typeof(UnitySceneProvider),
            // typeof(UnityComponentProvider),
            // typeof(UnityInspectorProvider)
        };

        return Task.FromResult<IEnumerable<Type>>(providers);
    }

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, object>> GetConfigurationSettings(CancellationToken cancellationToken = default)
    {
        var settings = new Dictionary<string, object>
        {
            ["ui.mode"] = "unity",
            ["graphics.mode"] = "gameobject",
            ["input.method"] = "unity_input",
            ["scene.management"] = true,
            ["component.system"] = "unity",
            ["inspector.enabled"] = true,
            ["hierarchy.enabled"] = true
        };

        return Task.FromResult<IReadOnlyDictionary<string, object>>(settings);
    }
}