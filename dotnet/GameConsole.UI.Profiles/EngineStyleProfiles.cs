using Microsoft.Extensions.Logging;

namespace GameConsole.UI.Profiles;

/// <summary>
/// Unity-style UI profile that simulates Unity's component-based UI behavior.
/// </summary>
public class UnityStyleProfile : BaseUIProfile
{
    public const string DefaultId = "unity-style";
    public const string DefaultName = "Unity Style UI";
    public const string DefaultDescription = "Unity-style component-based UI simulation with GameObject-like behavior.";

    public UnityStyleProfile(ILogger logger) 
        : base(DefaultId, DefaultName, DefaultDescription, UIMode.UnityStyle, logger)
    {
        InitializeUnityProperties();
    }

    public UnityStyleProfile(string id, string name, string description, ILogger logger)
        : base(id, name, description, UIMode.UnityStyle, logger)
    {
        InitializeUnityProperties();
    }

    protected override Task OnActivateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Activating Unity-style UI profile with component-based rendering");
        
        // Configure Unity-style specific settings
        SetProperty("renderer", "component-based");
        SetProperty("hierarchy", "gameobject-tree");
        SetProperty("inputMethod", "unity-input-system");
        SetProperty("canvasMode", "screen-space-overlay");
        SetProperty("eventSystem", "unity-ui-events");
        
        return Task.CompletedTask;
    }

    protected override Task OnDeactivateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deactivating Unity-style UI profile");
        return Task.CompletedTask;
    }

    private void InitializeUnityProperties()
    {
        SetProperty("framework", "Unity-Style");
        SetProperty("uiType", "Component-Based");
        SetProperty("supportsGameObjects", true);
        SetProperty("supportsComponents", true);
        SetProperty("defaultCanvas", "Canvas");
        SetProperty("eventSystemRequired", true);
        SetProperty("layoutMode", "rect-transform");
        SetProperty("anchorMode", "flexible");
        SetProperty("scalingMode", "scale-with-screen-size");
    }
}

/// <summary>
/// Godot-style UI profile that simulates Godot's scene-based UI behavior.
/// </summary>
public class GodotStyleProfile : BaseUIProfile
{
    public const string DefaultId = "godot-style";
    public const string DefaultName = "Godot Style UI";
    public const string DefaultDescription = "Godot-style scene-based UI simulation with Node-tree behavior.";

    public GodotStyleProfile(ILogger logger) 
        : base(DefaultId, DefaultName, DefaultDescription, UIMode.GodotStyle, logger)
    {
        InitializeGodotProperties();
    }

    public GodotStyleProfile(string id, string name, string description, ILogger logger)
        : base(id, name, description, UIMode.GodotStyle, logger)
    {
        InitializeGodotProperties();
    }

    protected override Task OnActivateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Activating Godot-style UI profile with scene-based rendering");
        
        // Configure Godot-style specific settings
        SetProperty("renderer", "scene-based");
        SetProperty("hierarchy", "node-tree");
        SetProperty("inputMethod", "godot-input");
        SetProperty("sceneMode", "2d-scene");
        SetProperty("signalSystem", "godot-signals");
        
        return Task.CompletedTask;
    }

    protected override Task OnDeactivateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deactivating Godot-style UI profile");
        return Task.CompletedTask;
    }

    private void InitializeGodotProperties()
    {
        SetProperty("framework", "Godot-Style");
        SetProperty("uiType", "Scene-Based");
        SetProperty("supportsNodes", true);
        SetProperty("supportsScenes", true);
        SetProperty("defaultScene", "Main");
        SetProperty("signalSystemEnabled", true);
        SetProperty("layoutMode", "container-based");
        SetProperty("anchorMode", "anchor-points");
        SetProperty("scalingMode", "viewport-scaling");
    }
}