using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.UI.Profiles;

/// <summary>
/// Default implementation of the UI Profile Manager service.
/// Provides profile detection, loading, and dynamic switching capabilities
/// following the GameConsole 4-tier architecture patterns.
/// </summary>
[Service("UI Profile Manager", "1.0.0", "Manages UI profiles for mode-based interface configurations")]
public sealed class UIProfileManager : IUIProfileManager, IServiceMetadata
{
    private readonly ILogger<UIProfileManager> _logger;
    private readonly IConfiguration _configuration;
    private readonly ConcurrentDictionary<string, UIProfile> _profiles;
    private readonly object _switchLock = new object();
    
    private UIProfile? _currentProfile;
    private ConsoleMode _currentMode;
    private bool _isRunning;

    /// <summary>
    /// Initializes a new instance of the UIProfileManager class.
    /// </summary>
    /// <param name="logger">Logger for the service.</param>
    /// <param name="configuration">Configuration provider.</param>
    public UIProfileManager(ILogger<UIProfileManager> logger, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _profiles = new ConcurrentDictionary<string, UIProfile>();
        _currentMode = ConsoleMode.Game; // Default to Game mode
    }

    /// <inheritdoc />
    public UIProfile? CurrentProfile => _currentProfile;

    /// <inheritdoc />
    public ConsoleMode CurrentMode => _currentMode;

    /// <inheritdoc />
    public event EventHandler<ProfileChangedEventArgs>? ProfileChanged;

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    #region IServiceMetadata Implementation
    /// <inheritdoc />
    public string Name => "UI Profile Manager";

    /// <inheritdoc />
    public string Version => "1.0.0";

    /// <inheritdoc />
    public string Description => "Manages UI profiles for mode-based interface configurations";

    /// <inheritdoc />
    public IEnumerable<string> Categories => new[] { "UI", "Profiles", "Configuration" };

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> Properties => new Dictionary<string, object>
    {
        ["SupportedModes"] = Enum.GetNames<ConsoleMode>(),
        ["ProfileCount"] = _profiles.Count,
        ["CurrentMode"] = _currentMode.ToString(),
        ["HasActiveProfile"] = _currentProfile != null
    };
    #endregion

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing UI Profile Manager");

        try
        {
            await LoadBuiltInProfilesAsync(cancellationToken);
            await LoadConfigurationProfilesAsync(cancellationToken);

            _logger.LogInformation("UI Profile Manager initialized with {ProfileCount} profiles", _profiles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize UI Profile Manager");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting UI Profile Manager");

        try
        {
            // Switch to default profile for current mode
            await SwitchToModeAsync(_currentMode, cancellationToken);
            
            _isRunning = true;
            _logger.LogInformation("UI Profile Manager started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start UI Profile Manager");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping UI Profile Manager");

        try
        {
            if (_currentProfile != null)
            {
                var context = new UIProfileDeactivationContext
                {
                    Reason = "Service Shutdown"
                };
                await _currentProfile.OnDeactivateAsync(context, cancellationToken);
            }

            _isRunning = false;
            _logger.LogInformation("UI Profile Manager stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during UI Profile Manager shutdown");
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isRunning)
        {
            await StopAsync();
        }
    }

    /// <inheritdoc />
    public Task<IEnumerable<UIProfile>> GetAvailableProfilesAsync(CancellationToken cancellationToken = default)
    {
        var profiles = _profiles.Values.AsEnumerable();
        return Task.FromResult(profiles);
    }

    /// <inheritdoc />
    public Task<IEnumerable<UIProfile>> GetProfilesForModeAsync(ConsoleMode mode, CancellationToken cancellationToken = default)
    {
        var profiles = _profiles.Values.Where(p => p.TargetMode == mode);
        return Task.FromResult(profiles);
    }

    /// <inheritdoc />
    public Task<UIProfile?> GetProfileByIdAsync(string profileId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profileId))
            return Task.FromResult<UIProfile?>(null);

        _profiles.TryGetValue(profileId, out var profile);
        return Task.FromResult(profile);
    }

    /// <inheritdoc />
    public async Task<bool> SwitchToProfileAsync(string profileId, string reason = "Manual", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profileId))
        {
            _logger.LogWarning("Cannot switch to profile with null or empty ID");
            return false;
        }

        var targetProfile = await GetProfileByIdAsync(profileId, cancellationToken);
        if (targetProfile == null)
        {
            _logger.LogWarning("Profile with ID '{ProfileId}' not found", profileId);
            return false;
        }

        return await SwitchToProfileInternalAsync(targetProfile, reason, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> SwitchToModeAsync(ConsoleMode mode, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Switching to console mode: {Mode}", mode);

        var availableProfiles = await GetProfilesForModeAsync(mode, cancellationToken);
        var targetProfile = availableProfiles
            .OrderByDescending(p => p.Metadata.Priority)
            .FirstOrDefault();

        if (targetProfile == null)
        {
            _logger.LogWarning("No profiles available for console mode: {Mode}", mode);
            return false;
        }

        _currentMode = mode;
        return await SwitchToProfileInternalAsync(targetProfile, $"Mode Switch to {mode}", cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> RegisterProfileAsync(UIProfile profile, CancellationToken cancellationToken = default)
    {
        if (profile == null)
        {
            _logger.LogWarning("Cannot register null profile");
            return Task.FromResult(false);
        }

        var validationErrors = profile.Validate().ToList();
        if (validationErrors.Count > 0)
        {
            _logger.LogWarning("Cannot register profile '{ProfileId}' due to validation errors: {Errors}",
                profile.Id, string.Join(", ", validationErrors));
            return Task.FromResult(false);
        }

        var wasAdded = _profiles.TryAdd(profile.Id, profile);
        if (wasAdded)
        {
            _logger.LogInformation("Registered UI profile: {ProfileId} ({ProfileName})", profile.Id, profile.Name);
        }
        else
        {
            _logger.LogWarning("Profile with ID '{ProfileId}' already exists", profile.Id);
        }

        return Task.FromResult(wasAdded);
    }

    /// <inheritdoc />
    public Task<bool> UnregisterProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profileId))
        {
            _logger.LogWarning("Cannot unregister profile with null or empty ID");
            return Task.FromResult(false);
        }

        var wasRemoved = _profiles.TryRemove(profileId, out var profile);
        if (wasRemoved)
        {
            _logger.LogInformation("Unregistered UI profile: {ProfileId}", profileId);
            
            // If this was the current profile, switch to a different one
            if (_currentProfile?.Id == profileId)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await SwitchToModeAsync(_currentMode, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to switch profiles after unregistering current profile");
                    }
                });
            }
        }
        else
        {
            _logger.LogWarning("Profile with ID '{ProfileId}' not found for unregistration", profileId);
        }

        return Task.FromResult(wasRemoved);
    }

    /// <inheritdoc />
    public async Task ReloadProfilesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Reloading UI profiles from configuration");

        try
        {
            _profiles.Clear();
            await LoadBuiltInProfilesAsync(cancellationToken);
            await LoadConfigurationProfilesAsync(cancellationToken);

            _logger.LogInformation("Reloaded {ProfileCount} UI profiles", _profiles.Count);

            // Try to maintain current profile if it still exists, otherwise switch to mode default
            if (_currentProfile != null && !_profiles.ContainsKey(_currentProfile.Id))
            {
                await SwitchToModeAsync(_currentMode, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload UI profiles");
            throw;
        }
    }

    private async Task<bool> SwitchToProfileInternalAsync(UIProfile targetProfile, string reason, CancellationToken cancellationToken)
    {
        lock (_switchLock)
        {
            if (_currentProfile?.Id == targetProfile.Id)
            {
                _logger.LogDebug("Already using profile '{ProfileId}'", targetProfile.Id);
                return true;
            }
        }

        try
        {
            var previousProfile = _currentProfile;

            // Deactivate current profile
            if (previousProfile != null)
            {
                var deactivationContext = new UIProfileDeactivationContext
                {
                    NextProfile = targetProfile,
                    Reason = reason
                };
                await previousProfile.OnDeactivateAsync(deactivationContext, cancellationToken);
            }

            // Activate new profile
            var activationContext = new UIProfileActivationContext
            {
                PreviousProfile = previousProfile,
                Reason = reason
            };
            await targetProfile.OnActivateAsync(activationContext, cancellationToken);

            lock (_switchLock)
            {
                _currentProfile = targetProfile;
                _currentMode = targetProfile.TargetMode;
            }

            _logger.LogInformation("Switched to UI profile: {ProfileId} ({ProfileName}) for mode {Mode}",
                targetProfile.Id, targetProfile.Name, targetProfile.TargetMode);

            // Fire profile changed event
            ProfileChanged?.Invoke(this, new ProfileChangedEventArgs(previousProfile, targetProfile, reason));

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to switch to profile '{ProfileId}'", targetProfile.Id);
            return false;
        }
    }

    private async Task LoadBuiltInProfilesAsync(CancellationToken cancellationToken)
    {
        // Register built-in profiles
        var gameProfile = new DefaultGameProfile();
        var editorProfile = new DefaultEditorProfile();

        await RegisterProfileAsync(gameProfile, cancellationToken);
        await RegisterProfileAsync(editorProfile, cancellationToken);
    }

    private Task LoadConfigurationProfilesAsync(CancellationToken cancellationToken)
    {
        // TODO: Implement loading profiles from configuration files
        // This would be part of a future enhancement
        _logger.LogDebug("Configuration-based profile loading not yet implemented");
        return Task.CompletedTask;
    }
}