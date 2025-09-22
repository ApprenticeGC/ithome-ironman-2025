using System.Collections.Concurrent;
using GameConsole.Core.Abstractions;
using GameConsole.UI.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GameConsole.UI.Services;

/// <summary>
/// Service implementation for managing UI profiles.
/// Provides coordinated profile switching across multiple subsystems.
/// </summary>
public class UIProfileService : IUIProfileService
{
    private readonly ILogger<UIProfileService> _logger;
    private readonly IConfiguration? _configuration;
    private readonly ConcurrentDictionary<string, UIProfile> _profiles;
    private UIProfile? _activeProfile;
    private bool _isInitialized;
    private bool _isRunning;

    /// <inheritdoc />
    public IUIProfile? ActiveProfile => _activeProfile;

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public event EventHandler<UIProfileChangedEventArgs>? ActiveProfileChanged;

    /// <summary>
    /// Initializes a new instance of the UIProfileService class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="configuration">Configuration provider (optional).</param>
    public UIProfileService(ILogger<UIProfileService> logger, IConfiguration? configuration = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration;
        _profiles = new ConcurrentDictionary<string, UIProfile>();
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
            return;

        _logger.LogInformation("Initializing UIProfileService");

        // Create default profiles
        await CreateDefaultProfilesAsync(cancellationToken);

        _isInitialized = true;
        _logger.LogInformation("UIProfileService initialized with {ProfileCount} profiles", _profiles.Count);
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Service must be initialized before starting");

        if (_isRunning)
            return Task.CompletedTask;

        _logger.LogInformation("Starting UIProfileService");
        _isRunning = true;
        _logger.LogInformation("UIProfileService started");

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
            return Task.CompletedTask;

        _logger.LogInformation("Stopping UIProfileService");
        _isRunning = false;
        _logger.LogInformation("UIProfileService stopped");

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IUIProfile> CreateProfileAsync(
        string id, 
        string name, 
        string description,
        string? inputProfileName = null,
        Dictionary<string, object>? graphicsSettings = null,
        Dictionary<string, object>? uiSettings = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Profile ID cannot be null or empty", nameof(id));
        
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Profile name cannot be null or empty", nameof(name));

        if (_profiles.ContainsKey(id))
            throw new ArgumentException($"Profile with ID '{id}' already exists", nameof(id));

        var profile = new UIProfile(id, name, description, inputProfileName, graphicsSettings, uiSettings);
        
        if (!_profiles.TryAdd(id, profile))
            throw new InvalidOperationException($"Failed to add profile '{id}' - concurrent modification detected");

        _logger.LogInformation("Created UI profile: {ProfileId} - {ProfileName}", id, name);

        return await Task.FromResult<IUIProfile>(profile);
    }

    /// <inheritdoc />
    public async Task<IUIProfile> UpdateProfileAsync(
        string id,
        string? name = null,
        string? description = null,
        string? inputProfileName = null,
        Dictionary<string, object>? graphicsSettings = null,
        Dictionary<string, object>? uiSettings = null,
        CancellationToken cancellationToken = default)
    {
        if (!_profiles.TryGetValue(id, out var profile))
            throw new ArgumentException($"Profile '{id}' not found", nameof(id));

        // Update basic properties
        profile.Update(name, description, inputProfileName);

        // Update graphics settings
        if (graphicsSettings != null)
        {
            foreach (var setting in graphicsSettings)
            {
                profile.SetGraphicsSetting(setting.Key, setting.Value);
            }
        }

        // Update UI settings
        if (uiSettings != null)
        {
            foreach (var setting in uiSettings)
            {
                profile.SetUISetting(setting.Key, setting.Value);
            }
        }

        _logger.LogInformation("Updated UI profile: {ProfileId}", id);

        return await Task.FromResult<IUIProfile>(profile);
    }

    /// <inheritdoc />
    public Task DeleteProfileAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Profile ID cannot be null or empty", nameof(id));

        if (_activeProfile?.Id == id)
        {
            // Deactivate the profile before deletion
            var previousProfile = _activeProfile;
            _activeProfile = null;
            ActiveProfileChanged?.Invoke(this, new UIProfileChangedEventArgs(previousProfile, null));
        }

        if (_profiles.TryRemove(id, out var removedProfile))
        {
            _logger.LogInformation("Deleted UI profile: {ProfileId} - {ProfileName}", 
                removedProfile.Id, removedProfile.Name);
        }
        else
        {
            _logger.LogWarning("Attempted to delete non-existent profile: {ProfileId}", id);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IUIProfile?> GetProfileAsync(string id, CancellationToken cancellationToken = default)
    {
        _profiles.TryGetValue(id, out var profile);
        return Task.FromResult<IUIProfile?>(profile);
    }

    /// <inheritdoc />
    public Task<IEnumerable<IUIProfile>> GetAllProfilesAsync(CancellationToken cancellationToken = default)
    {
        var profiles = _profiles.Values.Cast<IUIProfile>().ToList();
        _logger.LogDebug("Retrieved {ProfileCount} UI profiles", profiles.Count);
        return Task.FromResult<IEnumerable<IUIProfile>>(profiles);
    }

    /// <inheritdoc />
    public Task ActivateProfileAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!_profiles.TryGetValue(id, out var profile))
            throw new ArgumentException($"Profile '{id}' not found", nameof(id));

        var previousProfile = _activeProfile;
        _activeProfile = profile;

        _logger.LogInformation("Activated UI profile: {ProfileId} - {ProfileName}", profile.Id, profile.Name);

        // Notify listeners of the profile change
        ActiveProfileChanged?.Invoke(this, new UIProfileChangedEventArgs(previousProfile, _activeProfile));

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isRunning)
        {
            await StopAsync();
        }
    }

    /// <summary>
    /// Creates default UI profiles to get started.
    /// </summary>
    private async Task CreateDefaultProfilesAsync(CancellationToken cancellationToken)
    {
        // Create a default profile
        var defaultProfile = await CreateProfileAsync(
            "default",
            "Default",
            "Default UI profile for general use",
            inputProfileName: "default",
            graphicsSettings: new Dictionary<string, object>
            {
                { "WindowMode", "Windowed" },
                { "Resolution", "1280x720" },
                { "RefreshRate", 60 }
            },
            uiSettings: new Dictionary<string, object>
            {
                { "Theme", "Default" },
                { "Layout", "Standard" },
                { "ShowTooltips", true }
            },
            cancellationToken);

        // Create Unity-style profile
        var unityProfile = await CreateProfileAsync(
            "unity-style",
            "Unity Style",
            "UI profile mimicking Unity Editor behavior and shortcuts",
            inputProfileName: "unity",
            graphicsSettings: new Dictionary<string, object>
            {
                { "WindowMode", "Windowed" },
                { "Resolution", "1920x1080" },
                { "RefreshRate", 60 },
                { "GizmoStyle", "Unity" }
            },
            uiSettings: new Dictionary<string, object>
            {
                { "Theme", "Dark" },
                { "Layout", "Unity" },
                { "ShowInspector", true },
                { "ShowHierarchy", true },
                { "DockingEnabled", true }
            },
            cancellationToken);

        // Create Godot-style profile
        var godotProfile = await CreateProfileAsync(
            "godot-style",
            "Godot Style", 
            "UI profile mimicking Godot Editor behavior and shortcuts",
            inputProfileName: "godot",
            graphicsSettings: new Dictionary<string, object>
            {
                { "WindowMode", "Windowed" },
                { "Resolution", "1920x1080" },
                { "RefreshRate", 60 },
                { "GizmoStyle", "Godot" }
            },
            uiSettings: new Dictionary<string, object>
            {
                { "Theme", "Light" },
                { "Layout", "Godot" },
                { "ShowInspector", true },
                { "ShowFileSystem", true },
                { "TabsOnTop", false }
            },
            cancellationToken);

        // Activate the default profile by default
        await ActivateProfileAsync("default", cancellationToken);
    }
}