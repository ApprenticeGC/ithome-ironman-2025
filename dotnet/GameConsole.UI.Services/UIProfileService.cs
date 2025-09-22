using GameConsole.Core.Abstractions;
using GameConsole.UI.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.UI.Services;

/// <summary>
/// Service for managing UI profiles and configuration.
/// Provides UI profile capabilities within the 4-tier architecture.
/// </summary>
[Service("UI Profile Service", "1.0.0", "Manages UI profiles for different UI behavior modes and engine simulation", 
    Categories = new[] { "UI", "Configuration", "Profile" }, Lifetime = ServiceLifetime.Singleton)]
public class UIProfileService : IService, IUIProfileProvider
{
    private readonly ILogger<UIProfileService> _logger;
    private readonly IConfiguration? _configuration;
    private readonly ConcurrentDictionary<string, UIProfile> _profiles;
    private UIProfile? _activeProfile;
    private bool _isRunning = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="UIProfileService"/> class.
    /// </summary>
    /// <param name="logger">The logger for this service.</param>
    /// <param name="configuration">The configuration provider (optional).</param>
    public UIProfileService(ILogger<UIProfileService> logger, IConfiguration? configuration = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration;
        _profiles = new ConcurrentDictionary<string, UIProfile>();
    }

    #region IService Implementation

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing UI Profile Service");
        
        // Create default profiles
        await CreateDefaultProfilesAsync(cancellationToken);
        
        // Load profiles from configuration if available
        if (_configuration != null)
        {
            await LoadProfilesFromConfigurationAsync(cancellationToken);
        }
        
        // Set default active profile if none is set
        if (_activeProfile == null && _profiles.Any())
        {
            var defaultProfile = _profiles.Values.FirstOrDefault(p => p.Id == "tui-default") 
                                ?? _profiles.Values.First();
            await ActivateProfileAsync(defaultProfile.Id, cancellationToken);
        }
        
        _logger.LogInformation("Initialized UI Profile Service with {ProfileCount} profiles", _profiles.Count);
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting UI Profile Service");
        _isRunning = true;
        _logger.LogInformation("Started UI Profile Service");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping UI Profile Service");
        _isRunning = false;
        _logger.LogInformation("Stopped UI Profile Service");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _logger.LogDebug("Disposing UI Profile Service");
        return ValueTask.CompletedTask;
    }

    #endregion

    #region IUIProfileProvider Implementation

    /// <inheritdoc />
    public Task<IEnumerable<IUIProfile>> GetProfilesAsync(CancellationToken cancellationToken = default)
    {
        var profiles = _profiles.Values.Cast<IUIProfile>().ToList();
        _logger.LogTrace("Retrieved {ProfileCount} UI profiles", profiles.Count);
        return Task.FromResult<IEnumerable<IUIProfile>>(profiles);
    }

    /// <inheritdoc />
    public Task<IUIProfile?> GetActiveProfileAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Retrieved active UI profile: {ProfileName}", _activeProfile?.Name ?? "None");
        return Task.FromResult<IUIProfile?>(_activeProfile);
    }

    /// <inheritdoc />
    public Task<bool> ActivateProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profileId))
        {
            _logger.LogWarning("Cannot activate UI profile with null or empty ID");
            return Task.FromResult(false);
        }

        if (!_profiles.TryGetValue(profileId, out var profile))
        {
            _logger.LogWarning("UI profile not found: {ProfileId}", profileId);
            return Task.FromResult(false);
        }

        // Deactivate current active profile
        if (_activeProfile != null)
        {
            _activeProfile.IsActive = false;
        }

        // Activate new profile
        profile.IsActive = true;
        _activeProfile = profile;

        _logger.LogInformation("Activated UI profile: {ProfileName} ({ProfileId})", profile.Name, profileId);
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<IUIProfile?> GetProfileByIdAsync(string profileId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profileId))
        {
            _logger.LogTrace("Cannot retrieve UI profile with null or empty ID");
            return Task.FromResult<IUIProfile?>(null);
        }

        _profiles.TryGetValue(profileId, out var profile);
        _logger.LogTrace("Retrieved UI profile by ID {ProfileId}: {ProfileName}", 
            profileId, profile?.Name ?? "Not found");
        
        return Task.FromResult<IUIProfile?>(profile);
    }

    #endregion

    #region ICapabilityProvider Implementation

    /// <inheritdoc />
    public Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var capabilities = new[] { typeof(IUIProfileProvider) };
        return Task.FromResult<IEnumerable<Type>>(capabilities);
    }

    /// <inheritdoc />
    public Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        var hasCapability = typeof(T) == typeof(IUIProfileProvider);
        return Task.FromResult(hasCapability);
    }

    /// <inheritdoc />
    public Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (typeof(T) == typeof(IUIProfileProvider))
        {
            return Task.FromResult(this as T);
        }
        
        return Task.FromResult<T?>(null);
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Creates the default UI profiles for the system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    private Task CreateDefaultProfilesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating default UI profiles");

        // TUI Default Profile
        var tuiDefaultSettings = new UIProfileSettings
        {
            RenderingMode = "TUI",
            InputMode = "Console",
            GraphicsBackend = "None",
            TuiMode = true,
            Priority = 100
        };
        var tuiDefaultProfile = new UIProfile("tui-default", "TUI Default", 
            "Default text-based user interface profile", tuiDefaultSettings);
        _profiles.TryAdd(tuiDefaultProfile.Id, tuiDefaultProfile);

        // Unity-style Profile
        var unitySettings = new UIProfileSettings
        {
            RenderingMode = "Unity",
            InputMode = "Unity",
            GraphicsBackend = "OpenGL",
            TuiMode = false,
            Priority = 50
        };
        unitySettings.CustomProperties["EngineMode"] = "Unity";
        var unityProfile = new UIProfile("unity-style", "Unity Style", 
            "UI profile that simulates Unity engine behavior", unitySettings);
        _profiles.TryAdd(unityProfile.Id, unityProfile);

        // Godot-style Profile
        var godotSettings = new UIProfileSettings
        {
            RenderingMode = "Godot",
            InputMode = "Godot",
            GraphicsBackend = "Vulkan",
            TuiMode = false,
            Priority = 50
        };
        godotSettings.CustomProperties["EngineMode"] = "Godot";
        var godotProfile = new UIProfile("godot-style", "Godot Style", 
            "UI profile that simulates Godot engine behavior", godotSettings);
        _profiles.TryAdd(godotProfile.Id, godotProfile);

        _logger.LogDebug("Created {ProfileCount} default UI profiles", _profiles.Count);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Loads UI profiles from configuration if available.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    private Task LoadProfilesFromConfigurationAsync(CancellationToken cancellationToken = default)
    {
        if (_configuration == null)
            return Task.CompletedTask;

        _logger.LogDebug("Loading UI profiles from configuration");
        
        // TODO: Implement configuration loading
        // This would read from configuration sections like "UIProfiles:CustomProfile1" etc.
        // For now, we just log that configuration loading is available
        
        _logger.LogDebug("Configuration-based profile loading is available but not yet implemented");
        return Task.CompletedTask;
    }

    #endregion
}