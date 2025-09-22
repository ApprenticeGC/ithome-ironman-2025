using GameConsole.Profile.Core;

namespace GameConsole.Profile.Services;

/// <summary>
/// In-memory profile provider for testing and development scenarios.
/// </summary>
public class MemoryProfileProvider : IProfileProvider
{
    private readonly Dictionary<string, IProfile> _profiles = new Dictionary<string, IProfile>();
    private string? _activeProfileId;
    private readonly object _lock = new object();

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryProfileProvider"/> class.
    /// </summary>
    public MemoryProfileProvider()
    {
        // Initialize with default profiles
        InitializeDefaultProfiles();
    }

    /// <inheritdoc />
    public Task<IEnumerable<IProfile>> LoadProfilesAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_profiles.Values.AsEnumerable());
        }
    }

    /// <inheritdoc />
    public Task<IProfile?> LoadProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profileId))
            return Task.FromResult<IProfile?>(null);

        lock (_lock)
        {
            _profiles.TryGetValue(profileId, out var profile);
            return Task.FromResult(profile);
        }
    }

    /// <inheritdoc />
    public Task SaveProfileAsync(IProfile profile, CancellationToken cancellationToken = default)
    {
        if (profile == null)
            throw new ArgumentNullException(nameof(profile));

        lock (_lock)
        {
            _profiles[profile.Id] = profile;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> DeleteProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profileId))
            return Task.FromResult(false);

        lock (_lock)
        {
            var result = _profiles.Remove(profileId);
            
            // If we're deleting the active profile, clear it
            if (_activeProfileId == profileId)
            {
                _activeProfileId = null;
            }
            
            return Task.FromResult(result);
        }
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string profileId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profileId))
            return Task.FromResult(false);

        lock (_lock)
        {
            return Task.FromResult(_profiles.ContainsKey(profileId));
        }
    }

    /// <inheritdoc />
    public Task<string?> GetActiveProfileIdAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_activeProfileId);
        }
    }

    /// <inheritdoc />
    public Task SetActiveProfileIdAsync(string profileId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _activeProfileId = profileId;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears all profiles from memory.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _profiles.Clear();
            _activeProfileId = null;
        }
    }

    /// <summary>
    /// Gets the number of profiles currently stored.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _profiles.Count;
            }
        }
    }

    private void InitializeDefaultProfiles()
    {
        // Create default profile
        var defaultProfile = new Core.Profile(
            id: "default",
            name: "Default Profile",
            type: ProfileType.Default,
            description: "Standard system profile with default configurations",
            isReadOnly: false // Create as mutable first
        );

        // Create Unity-style profile
        var unityProfile = new Core.Profile(
            id: "unity-style",
            name: "Unity-Style",
            type: ProfileType.Unity,
            description: "Profile configured to mimic Unity engine behavior patterns",
            isReadOnly: false // Create as mutable first
        );

        // Add basic Unity-style configurations
        unityProfile.SetServiceConfiguration("IInputService", new ServiceConfiguration
        {
            Implementation = "UnityStyleInputService",
            Capabilities = new List<string> { "KeyboardInput", "MouseInput", "GamepadInput" },
            Settings = new Dictionary<string, object>
            {
                { "inputSensitivity", 1.0 },
                { "enableRawInput", true },
                { "unityCompat", true }
            },
            Enabled = true
        });

        unityProfile.SetServiceConfiguration("IGraphicsService", new ServiceConfiguration
        {
            Implementation = "UnityStyleGraphicsService",
            Capabilities = new List<string> { "Rendering", "PostProcessing", "ShaderGraph" },
            Settings = new Dictionary<string, object>
            {
                { "vsync", true },
                { "targetFrameRate", 60 },
                { "renderPipeline", "URP" }
            },
            Enabled = true
        });

        // Create Godot-style profile
        var godotProfile = new Core.Profile(
            id: "godot-style",
            name: "Godot-Style",
            type: ProfileType.Godot,
            description: "Profile configured to mimic Godot engine behavior patterns",
            isReadOnly: false // Create as mutable first
        );

        // Add basic Godot-style configurations
        godotProfile.SetServiceConfiguration("IInputService", new ServiceConfiguration
        {
            Implementation = "GodotStyleInputService",
            Capabilities = new List<string> { "ActionMapInput", "InputEvents", "NodeBasedInput" },
            Settings = new Dictionary<string, object>
            {
                { "actionMapEnabled", true },
                { "nodeInputProcessing", true },
                { "godotCompat", true }
            },
            Enabled = true
        });

        godotProfile.SetServiceConfiguration("IGraphicsService", new ServiceConfiguration
        {
            Implementation = "GodotStyleGraphicsService",
            Capabilities = new List<string> { "CanvasRendering", "SpatialRendering", "GDScript" },
            Settings = new Dictionary<string, object>
            {
                { "canvasLayers", true },
                { "spatialEditor", true },
                { "renderMethod", "Forward+" }
            },
            Enabled = true
        });

        // Create minimal profile
        var minimalProfile = new Core.Profile(
            id: "minimal",
            name: "Minimal",
            type: ProfileType.Minimal,
            description: "Lightweight profile with only essential services",
            isReadOnly: false // Create as mutable first
        );

        minimalProfile.SetServiceConfiguration("IInputService", new ServiceConfiguration
        {
            Implementation = "BasicInputService",
            Capabilities = new List<string> { "KeyboardInput" },
            Settings = new Dictionary<string, object> { { "minimal", true } },
            Enabled = true
        });

        // Now create read-only versions for storage
        var readOnlyDefault = new Core.Profile(
            defaultProfile.Id,
            defaultProfile.Name,
            defaultProfile.Type,
            defaultProfile.Description,
            new Dictionary<string, ServiceConfiguration>(defaultProfile.ServiceConfigurations),
            defaultProfile.Created,
            defaultProfile.LastModified,
            isReadOnly: true,
            defaultProfile.Version
        );

        var readOnlyUnity = new Core.Profile(
            unityProfile.Id,
            unityProfile.Name,
            unityProfile.Type,
            unityProfile.Description,
            new Dictionary<string, ServiceConfiguration>(unityProfile.ServiceConfigurations),
            unityProfile.Created,
            unityProfile.LastModified,
            isReadOnly: true,
            unityProfile.Version
        );

        var readOnlyGodot = new Core.Profile(
            godotProfile.Id,
            godotProfile.Name,
            godotProfile.Type,
            godotProfile.Description,
            new Dictionary<string, ServiceConfiguration>(godotProfile.ServiceConfigurations),
            godotProfile.Created,
            godotProfile.LastModified,
            isReadOnly: true,
            godotProfile.Version
        );

        var readOnlyMinimal = new Core.Profile(
            minimalProfile.Id,
            minimalProfile.Name,
            minimalProfile.Type,
            minimalProfile.Description,
            new Dictionary<string, ServiceConfiguration>(minimalProfile.ServiceConfigurations),
            minimalProfile.Created,
            minimalProfile.LastModified,
            isReadOnly: true,
            minimalProfile.Version
        );

        // Store read-only profiles
        _profiles[readOnlyDefault.Id] = readOnlyDefault;
        _profiles[readOnlyUnity.Id] = readOnlyUnity;
        _profiles[readOnlyGodot.Id] = readOnlyGodot;
        _profiles[readOnlyMinimal.Id] = readOnlyMinimal;

        // Set default as active
        _activeProfileId = readOnlyDefault.Id;
    }
}