using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace GameConsole.Configuration.Security;

/// <summary>
/// Service for managing UI profiles in the GameConsole system.
/// Provides functionality to switch between different UI modes and persist profile configurations.
/// </summary>
[Service("UI Profile Service", "1.0.0", "Manages UI profile configurations and mode switching", 
    Categories = new[] { "Configuration", "UI", "Security" }, Lifetime = ServiceLifetime.Singleton)]
public class UIProfileService : IUIProfileService
{
    private readonly ILogger<UIProfileService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ConcurrentDictionary<string, IUIProfileConfiguration> _profiles;
    private IUIProfileConfiguration? _activeProfile;
    private bool _isRunning;

    private const string DefaultConfigurationPath = "ui-profiles.json";
    private const string DefaultTuiProfileId = "tui-default";
    private const string UnitySimulationProfileId = "unity-simulation";
    private const string GodotSimulationProfileId = "godot-simulation";

    /// <summary>
    /// Initializes a new instance of the UIProfileService class.
    /// </summary>
    /// <param name="logger">Logger for service operations.</param>
    /// <param name="configuration">Configuration service.</param>
    public UIProfileService(ILogger<UIProfileService> logger, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _profiles = new ConcurrentDictionary<string, IUIProfileConfiguration>();
    }

    /// <inheritdoc />
    public IUIProfileConfiguration? ActiveProfile => _activeProfile;

    /// <inheritdoc />
    public IReadOnlyCollection<IUIProfileConfiguration> AvailableProfiles => _profiles.Values.ToList();

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public event EventHandler<UIProfileChangedEventArgs>? ProfileChanged;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing UI Profile Service");
        
        try
        {
            // Register default profiles
            RegisterDefaultProfiles();
            
            // Load persisted configuration
            await LoadConfigurationAsync(cancellationToken);
            
            // Set default profile if none is active
            if (_activeProfile == null)
            {
                var defaultProfile = _profiles.Values.FirstOrDefault(p => p.ProfileId == DefaultTuiProfileId);
                if (defaultProfile != null)
                {
                    _activeProfile = defaultProfile;
                    _logger.LogInformation("Set default TUI profile as active");
                }
            }
            
            _logger.LogInformation("UI Profile Service initialized with {ProfileCount} profiles", _profiles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize UI Profile Service");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting UI Profile Service");
        _isRunning = true;
        await Task.CompletedTask;
        _logger.LogInformation("UI Profile Service started");
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping UI Profile Service");
        
        try
        {
            // Save current configuration before stopping
            await SaveConfigurationAsync(cancellationToken);
            _isRunning = false;
            
            _logger.LogInformation("UI Profile Service stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while stopping UI Profile Service");
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isRunning)
        {
            await StopAsync();
        }
        
        _profiles.Clear();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public async Task SwitchToProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profileId))
            throw new ArgumentException("Profile ID cannot be null or empty", nameof(profileId));

        if (!_profiles.TryGetValue(profileId, out var newProfile))
            throw new ArgumentException($"Profile with ID '{profileId}' not found", nameof(profileId));

        if (_activeProfile?.ProfileId == profileId)
        {
            _logger.LogDebug("Profile '{ProfileId}' is already active", profileId);
            return;
        }

        var previousProfile = _activeProfile;
        
        try
        {
            _logger.LogInformation("Switching from profile '{PreviousProfile}' to '{NewProfile}'", 
                previousProfile?.ProfileId ?? "none", profileId);

            // TODO: In a full implementation, this would coordinate with provider swapping
            // For now, we just update the active profile
            _activeProfile = newProfile;
            
            // Raise profile changed event
            ProfileChanged?.Invoke(this, new UIProfileChangedEventArgs(previousProfile, newProfile));
            
            _logger.LogInformation("Successfully switched to profile '{ProfileId}' ({ProfileName})", 
                newProfile.ProfileId, newProfile.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to switch to profile '{ProfileId}'", profileId);
            throw new InvalidOperationException($"Failed to switch to profile '{profileId}'", ex);
        }
        
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public IUIProfileConfiguration? GetProfile(string profileId)
    {
        if (string.IsNullOrWhiteSpace(profileId))
            return null;

        return _profiles.TryGetValue(profileId, out var profile) ? profile : null;
    }

    /// <inheritdoc />
    public void RegisterProfile(IUIProfileConfiguration profile)
    {
        if (profile == null)
            throw new ArgumentNullException(nameof(profile));

        if (!_profiles.TryAdd(profile.ProfileId, profile))
            throw new ArgumentException($"Profile with ID '{profile.ProfileId}' already exists", nameof(profile));

        _logger.LogInformation("Registered UI profile '{ProfileId}' ({ProfileName})", 
            profile.ProfileId, profile.Name);
    }

    /// <inheritdoc />
    public bool UnregisterProfile(string profileId)
    {
        if (string.IsNullOrWhiteSpace(profileId))
            return false;

        if (_profiles.TryRemove(profileId, out var removedProfile))
        {
            // If this was the active profile, clear the active profile
            if (_activeProfile?.ProfileId == profileId)
            {
                var previousProfile = _activeProfile;
                _activeProfile = null;
                ProfileChanged?.Invoke(this, new UIProfileChangedEventArgs(previousProfile, null));
            }
            
            _logger.LogInformation("Unregistered UI profile '{ProfileId}' ({ProfileName})", 
                removedProfile.ProfileId, removedProfile.Name);
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public async Task SaveConfigurationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var configPath = GetConfigurationPath();
            var config = new UIProfilesConfiguration
            {
                ActiveProfileId = _activeProfile?.ProfileId,
                Profiles = _profiles.Values.Select(SerializeProfile).ToList()
            };

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(configPath, json, cancellationToken);
            _logger.LogDebug("Saved UI profile configuration to {ConfigPath}", configPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save UI profile configuration");
            // Don't rethrow - this shouldn't stop the service
        }
    }

    /// <inheritdoc />
    public async Task LoadConfigurationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var configPath = GetConfigurationPath();
            if (!File.Exists(configPath))
            {
                _logger.LogDebug("No UI profile configuration file found at {ConfigPath}", configPath);
                return;
            }

            var json = await File.ReadAllTextAsync(configPath, cancellationToken);
            var config = JsonSerializer.Deserialize<UIProfilesConfiguration>(json);

            if (config?.Profiles != null)
            {
                foreach (var profileData in config.Profiles)
                {
                    try
                    {
                        var profile = DeserializeProfile(profileData);
                        if (profile != null && !_profiles.ContainsKey(profile.ProfileId))
                        {
                            _profiles.TryAdd(profile.ProfileId, profile);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize profile data");
                    }
                }
            }

            // Set active profile if specified
            if (!string.IsNullOrEmpty(config?.ActiveProfileId) && 
                _profiles.TryGetValue(config.ActiveProfileId, out var activeProfile))
            {
                _activeProfile = activeProfile;
            }

            _logger.LogDebug("Loaded UI profile configuration from {ConfigPath}", configPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load UI profile configuration");
            // Don't rethrow - this shouldn't prevent service startup
        }
    }

    private void RegisterDefaultProfiles()
    {
        // Default TUI Profile (TUI-first approach)
        var tuiProfile = new UIProfileConfiguration(
            DefaultTuiProfileId,
            "Default TUI",
            "Text-based user interface (TUI-first approach)",
            UIMode.TUI);
        tuiProfile.SetSetting("ConsoleWidth", 120);
        tuiProfile.SetSetting("ConsoleHeight", 40);
        tuiProfile.SetSetting("ColorTheme", "Dark");
        _profiles.TryAdd(tuiProfile.ProfileId, tuiProfile);

        // Unity Simulation Profile
        var unityProfile = new UIProfileConfiguration(
            UnitySimulationProfileId,
            "Unity Simulation",
            "Simulates Unity engine-like behavior and interfaces",
            UIMode.Unity);
        unityProfile.SetSetting("WindowWidth", 1920);
        unityProfile.SetSetting("WindowHeight", 1080);
        unityProfile.SetSetting("TargetFrameRate", 60);
        unityProfile.SetSetting("VSync", true);
        _profiles.TryAdd(unityProfile.ProfileId, unityProfile);

        // Godot Simulation Profile
        var godotProfile = new UIProfileConfiguration(
            GodotSimulationProfileId,
            "Godot Simulation", 
            "Simulates Godot engine-like behavior and interfaces",
            UIMode.Godot);
        godotProfile.SetSetting("WindowWidth", 1280);
        godotProfile.SetSetting("WindowHeight", 720);
        godotProfile.SetSetting("TargetFrameRate", 60);
        godotProfile.SetSetting("Fullscreen", false);
        _profiles.TryAdd(godotProfile.ProfileId, godotProfile);

        _logger.LogDebug("Registered {ProfileCount} default UI profiles", 3);
    }

    private string GetConfigurationPath()
    {
        var basePath = _configuration["UIProfiles:ConfigurationPath"] ?? DefaultConfigurationPath;
        return Path.GetFullPath(basePath);
    }

    private static SerializableProfile SerializeProfile(IUIProfileConfiguration profile)
    {
        return new SerializableProfile
        {
            ProfileId = profile.ProfileId,
            Name = profile.Name,
            Description = profile.Description,
            Mode = profile.Mode,
            Settings = profile.Settings.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };
    }

    private static UIProfileConfiguration? DeserializeProfile(SerializableProfile data)
    {
        if (data == null) return null;

        var profile = new UIProfileConfiguration(data.ProfileId!, data.Name!, data.Description!, data.Mode);
        
        if (data.Settings != null)
        {
            foreach (var kvp in data.Settings)
            {
                profile.SetSetting(kvp.Key, kvp.Value);
            }
        }

        return profile;
    }
}

/// <summary>
/// Configuration data for UI profiles persistence.
/// </summary>
internal class UIProfilesConfiguration
{
    public string? ActiveProfileId { get; set; }
    public List<SerializableProfile>? Profiles { get; set; }
}

/// <summary>
/// Serializable representation of a UI profile.
/// </summary>
internal class SerializableProfile
{
    public string? ProfileId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public UIMode Mode { get; set; }
    public Dictionary<string, object>? Settings { get; set; }
}