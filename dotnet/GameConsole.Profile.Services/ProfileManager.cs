using GameConsole.Profile.Core;
using Microsoft.Extensions.Logging;

namespace GameConsole.Profile.Services;

/// <summary>
/// Default implementation of the profile manager service.
/// </summary>
public class ProfileManager : BaseProfileManager
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileManager"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="profileProvider">The profile provider for storage operations.</param>
    public ProfileManager(ILogger<ProfileManager> logger, IProfileProvider profileProvider) 
        : base(logger, profileProvider)
    {
    }

    /// <inheritdoc />
    protected override async Task ApplyDefaultConfigurationsAsync(Core.Profile profile, CancellationToken cancellationToken = default)
    {
        // Apply default configurations based on profile type
        switch (profile.Type)
        {
            case ProfileType.Unity:
                await ApplyUnityDefaultsAsync(profile, cancellationToken);
                break;
            case ProfileType.Godot:
                await ApplyGodotDefaultsAsync(profile, cancellationToken);
                break;
            case ProfileType.Minimal:
                await ApplyMinimalDefaultsAsync(profile, cancellationToken);
                break;
            case ProfileType.Development:
                await ApplyDevelopmentDefaultsAsync(profile, cancellationToken);
                break;
            case ProfileType.Default:
                await ApplySystemDefaultsAsync(profile, cancellationToken);
                break;
            case ProfileType.Custom:
                // Custom profiles start empty
                break;
        }
    }

    /// <inheritdoc />
    protected override async Task<bool> OnValidateProfileAsync(IProfile profile, CancellationToken cancellationToken = default)
    {
        // Validate service configurations
        foreach (var config in profile.ServiceConfigurations)
        {
            if (string.IsNullOrWhiteSpace(config.Value.Implementation))
            {
                _logger.LogWarning("Profile {ProfileId} has service {ServiceName} with empty implementation", 
                    profile.Id, config.Key);
                return false;
            }

            // Check for problematic settings (avoid null key check that causes exception)
            if (config.Value.Settings?.Any(s => string.IsNullOrEmpty(s.Key)) == true)
            {
                _logger.LogWarning("Profile {ProfileId} has service {ServiceName} with null or empty setting key", 
                    profile.Id, config.Key);
                return false;
            }
        }

        return await base.OnValidateProfileAsync(profile, cancellationToken);
    }

    private async Task ApplyUnityDefaultsAsync(Core.Profile profile, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Applying Unity-style defaults to profile {ProfileId}", profile.Id);

        profile.SetServiceConfiguration("IInputService", new ServiceConfiguration
        {
            Implementation = "UnityStyleInputService",
            Capabilities = new List<string> { "KeyboardInput", "MouseInput", "GamepadInput", "TouchInput" },
            Settings = new Dictionary<string, object>
            {
                { "inputSensitivity", 1.0 },
                { "enableRawInput", true },
                { "unityCompat", true },
                { "componentBased", true }
            },
            Lifetime = "Singleton",
            Enabled = true
        });

        profile.SetServiceConfiguration("IGraphicsService", new ServiceConfiguration
        {
            Implementation = "UnityStyleGraphicsService",
            Capabilities = new List<string> { "Rendering", "PostProcessing", "ShaderGraph", "MaterialEditor" },
            Settings = new Dictionary<string, object>
            {
                { "vsync", true },
                { "targetFrameRate", 60 },
                { "renderPipeline", "URP" },
                { "hdrp", false },
                { "builtinRP", false }
            },
            Lifetime = "Singleton",
            Enabled = true
        });

        profile.SetServiceConfiguration("IPhysicsService", new ServiceConfiguration
        {
            Implementation = "UnityStylePhysicsService",
            Capabilities = new List<string> { "RigidBodyPhysics", "CollisionDetection", "PhysX" },
            Settings = new Dictionary<string, object>
            {
                { "gravity", -9.81f },
                { "timeStep", 0.02f },
                { "physxEnabled", true }
            },
            Lifetime = "Singleton",
            Enabled = true
        });

        await Task.CompletedTask;
    }

    private async Task ApplyGodotDefaultsAsync(Core.Profile profile, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Applying Godot-style defaults to profile {ProfileId}", profile.Id);

        profile.SetServiceConfiguration("IInputService", new ServiceConfiguration
        {
            Implementation = "GodotStyleInputService",
            Capabilities = new List<string> { "ActionMapInput", "InputEvents", "NodeBasedInput" },
            Settings = new Dictionary<string, object>
            {
                { "actionMapEnabled", true },
                { "nodeInputProcessing", true },
                { "godotCompat", true },
                { "sceneTree", true }
            },
            Lifetime = "Singleton",
            Enabled = true
        });

        profile.SetServiceConfiguration("IGraphicsService", new ServiceConfiguration
        {
            Implementation = "GodotStyleGraphicsService",
            Capabilities = new List<string> { "CanvasRendering", "SpatialRendering", "GDScript", "VisualScript" },
            Settings = new Dictionary<string, object>
            {
                { "canvasLayers", true },
                { "spatialEditor", true },
                { "renderMethod", "Forward+" },
                { "vulkanEnabled", true }
            },
            Lifetime = "Singleton",
            Enabled = true
        });

        profile.SetServiceConfiguration("IPhysicsService", new ServiceConfiguration
        {
            Implementation = "GodotStylePhysicsService",
            Capabilities = new List<string> { "RigidBodyPhysics", "StaticBodyPhysics", "KinematicBodyPhysics" },
            Settings = new Dictionary<string, object>
            {
                { "gravity", -9.8f },
                { "physics2D", true },
                { "physics3D", true }
            },
            Lifetime = "Singleton",
            Enabled = true
        });

        await Task.CompletedTask;
    }

    private async Task ApplyMinimalDefaultsAsync(Core.Profile profile, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Applying minimal defaults to profile {ProfileId}", profile.Id);

        profile.SetServiceConfiguration("IInputService", new ServiceConfiguration
        {
            Implementation = "BasicInputService",
            Capabilities = new List<string> { "KeyboardInput" },
            Settings = new Dictionary<string, object>
            {
                { "minimal", true },
                { "pollingRate", 60 }
            },
            Lifetime = "Singleton",
            Enabled = true
        });

        // Minimal graphics - just basic rendering
        profile.SetServiceConfiguration("IGraphicsService", new ServiceConfiguration
        {
            Implementation = "BasicGraphicsService",
            Capabilities = new List<string> { "BasicRendering" },
            Settings = new Dictionary<string, object>
            {
                { "minimal", true },
                { "softwareRenderer", true }
            },
            Lifetime = "Singleton",
            Enabled = true
        });

        await Task.CompletedTask;
    }

    private async Task ApplyDevelopmentDefaultsAsync(Core.Profile profile, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Applying development defaults to profile {ProfileId}", profile.Id);

        // Development profile has enhanced logging and debugging
        profile.SetServiceConfiguration("IInputService", new ServiceConfiguration
        {
            Implementation = "DebugInputService",
            Capabilities = new List<string> { "KeyboardInput", "MouseInput", "GamepadInput", "InputDebugging" },
            Settings = new Dictionary<string, object>
            {
                { "debugMode", true },
                { "inputLogging", true },
                { "hotReload", true }
            },
            Lifetime = "Singleton",
            Enabled = true
        });

        profile.SetServiceConfiguration("IGraphicsService", new ServiceConfiguration
        {
            Implementation = "DebugGraphicsService",
            Capabilities = new List<string> { "Rendering", "DebugRendering", "Wireframe", "Profiling" },
            Settings = new Dictionary<string, object>
            {
                { "debugMode", true },
                { "wireframeMode", false },
                { "showFPS", true },
                { "profileGPU", true }
            },
            Lifetime = "Singleton",
            Enabled = true
        });

        profile.SetServiceConfiguration("ILoggingService", new ServiceConfiguration
        {
            Implementation = "VerboseLoggingService",
            Capabilities = new List<string> { "FileLogging", "ConsoleLogging", "RemoteLogging" },
            Settings = new Dictionary<string, object>
            {
                { "logLevel", "Debug" },
                { "fileOutput", true },
                { "remoteLogging", true }
            },
            Lifetime = "Singleton",
            Enabled = true
        });

        await Task.CompletedTask;
    }

    private async Task ApplySystemDefaultsAsync(Core.Profile profile, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Applying system defaults to profile {ProfileId}", profile.Id);

        // Default system configuration - balanced settings
        profile.SetServiceConfiguration("IInputService", new ServiceConfiguration
        {
            Implementation = "StandardInputService",
            Capabilities = new List<string> { "KeyboardInput", "MouseInput", "GamepadInput" },
            Settings = new Dictionary<string, object>
            {
                { "inputSensitivity", 1.0 },
                { "enableRawInput", false },
                { "pollingRate", 60 }
            },
            Lifetime = "Singleton",
            Enabled = true
        });

        profile.SetServiceConfiguration("IGraphicsService", new ServiceConfiguration
        {
            Implementation = "StandardGraphicsService",
            Capabilities = new List<string> { "Rendering", "BasicPostProcessing" },
            Settings = new Dictionary<string, object>
            {
                { "vsync", false },
                { "targetFrameRate", 0 }, // Unlimited
                { "adaptiveSync", true }
            },
            Lifetime = "Singleton",
            Enabled = true
        });

        await Task.CompletedTask;
    }
}