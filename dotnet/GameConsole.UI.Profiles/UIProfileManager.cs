using Microsoft.Extensions.Logging;

namespace GameConsole.UI.Profiles;

/// <summary>
/// Default implementation of the UI profile manager.
/// Manages profile registration, activation, and mode switching.
/// </summary>
public class UIProfileManager : IUIProfileManager
{
    private readonly ILogger<UIProfileManager> _logger;
    private readonly Dictionary<string, UIProfile> _profiles = new();
    private readonly object _lock = new();
    private UIProfile? _currentProfile;
    private ConsoleMode _currentMode = ConsoleMode.Game;
    private bool _isRunning;

    /// <summary>
    /// Initializes a new instance of the UIProfileManager class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public UIProfileManager(ILogger<UIProfileManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public UIProfile? CurrentProfile
    {
        get
        {
            lock (_lock)
            {
                return _currentProfile;
            }
        }
    }

    /// <inheritdoc />
    public ConsoleMode CurrentMode
    {
        get
        {
            lock (_lock)
            {
                return _currentMode;
            }
        }
    }

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public event EventHandler<ProfileChangedEventArgs>? ProfileChanged;

    /// <inheritdoc />
    public event EventHandler<ModeChangedEventArgs>? ModeChanged;

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing UIProfileManager");
        
        // Initialization logic can be added here
        // For now, we just log that initialization is complete
        
        _logger.LogInformation("UIProfileManager initialized successfully");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting UIProfileManager");
        
        lock (_lock)
        {
            _isRunning = true;
        }
        
        _logger.LogInformation("UIProfileManager started successfully");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping UIProfileManager");
        
        lock (_lock)
        {
            if (_currentProfile != null)
            {
                _currentProfile.OnDeactivating();
                _currentProfile = null;
            }
            
            _isRunning = false;
        }
        
        _logger.LogInformation("UIProfileManager stopped");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        return new ValueTask(StopAsync());
    }

    /// <inheritdoc />
    public void RegisterProfile(UIProfile profile)
    {
        if (profile == null)
            throw new ArgumentNullException(nameof(profile));

        lock (_lock)
        {
            if (_profiles.ContainsKey(profile.Name))
            {
                _logger.LogWarning("Profile '{ProfileName}' is already registered. Replacing existing profile.", profile.Name);
            }

            _profiles[profile.Name] = profile;
            _logger.LogDebug("Registered UI profile '{ProfileName}' for mode {Mode}", profile.Name, profile.TargetMode);
        }
    }

    /// <inheritdoc />
    public bool UnregisterProfile(string profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName))
            throw new ArgumentException("Profile name cannot be null or whitespace.", nameof(profileName));

        lock (_lock)
        {
            if (!_profiles.ContainsKey(profileName))
            {
                _logger.LogWarning("Attempted to unregister non-existent profile '{ProfileName}'", profileName);
                return false;
            }

            // If this is the currently active profile, deactivate it
            if (_currentProfile?.Name == profileName)
            {
                _currentProfile.OnDeactivating();
                var previousProfile = _currentProfile;
                _currentProfile = null;
                
                // Raise the profile changed event
                ProfileChanged?.Invoke(this, new ProfileChangedEventArgs(previousProfile, null));
            }

            _profiles.Remove(profileName);
            _logger.LogDebug("Unregistered UI profile '{ProfileName}'", profileName);
            return true;
        }
    }

    /// <inheritdoc />
    public IEnumerable<UIProfile> GetAllProfiles()
    {
        lock (_lock)
        {
            return _profiles.Values.ToList();
        }
    }

    /// <inheritdoc />
    public IEnumerable<UIProfile> GetProfilesForMode(ConsoleMode mode)
    {
        lock (_lock)
        {
            return _profiles.Values.Where(p => p.TargetMode == mode).ToList();
        }
    }

    /// <inheritdoc />
    public UIProfile? GetProfile(string profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName))
            return null;

        lock (_lock)
        {
            return _profiles.TryGetValue(profileName, out var profile) ? profile : null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ActivateProfileAsync(string profileName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profileName))
        {
            _logger.LogWarning("Cannot activate profile: profile name is null or whitespace");
            return false;
        }

        UIProfile? profileToActivate;
        UIProfile? previousProfile;

        lock (_lock)
        {
            if (!_profiles.TryGetValue(profileName, out profileToActivate))
            {
                _logger.LogWarning("Cannot activate profile '{ProfileName}': profile not found", profileName);
                return false;
            }

            if (!profileToActivate.CanActivate())
            {
                _logger.LogWarning("Cannot activate profile '{ProfileName}': profile cannot be activated in current context", profileName);
                return false;
            }

            if (_currentProfile?.Name == profileName)
            {
                _logger.LogDebug("Profile '{ProfileName}' is already active", profileName);
                return true;
            }

            previousProfile = _currentProfile;

            // Deactivate current profile if one is active
            if (_currentProfile != null)
            {
                _logger.LogDebug("Deactivating current profile '{CurrentProfileName}'", _currentProfile.Name);
                _currentProfile.OnDeactivating();
            }

            // Activate new profile
            _logger.LogInformation("Activating UI profile '{ProfileName}' for mode {Mode}", profileName, profileToActivate.TargetMode);
            profileToActivate.OnActivating();
            _currentProfile = profileToActivate;

            // Update current mode to match the profile's target mode
            var previousMode = _currentMode;
            _currentMode = profileToActivate.TargetMode;

            // Raise events outside the lock
            if (previousMode != _currentMode)
            {
                ModeChanged?.Invoke(this, new ModeChangedEventArgs(previousMode, _currentMode));
            }
        }

        // Raise the profile changed event outside the lock
        ProfileChanged?.Invoke(this, new ProfileChangedEventArgs(previousProfile, profileToActivate));

        _logger.LogInformation("Successfully activated UI profile '{ProfileName}'", profileName);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> SwitchModeAsync(ConsoleMode mode, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Switching to console mode {Mode}", mode);

        // Find the best profile for the requested mode
        var availableProfiles = GetProfilesForMode(mode)
            .Where(p => p.CanActivate())
            .OrderByDescending(p => p.Metadata.Priority)
            .ToList();

        if (!availableProfiles.Any())
        {
            _logger.LogWarning("No suitable profiles found for mode {Mode}", mode);
            
            // Update mode even if no profile is available
            lock (_lock)
            {
                var previousMode = _currentMode;
                _currentMode = mode;
                
                if (previousMode != mode)
                {
                    ModeChanged?.Invoke(this, new ModeChangedEventArgs(previousMode, mode));
                }
            }
            
            return false;
        }

        // Activate the highest priority profile
        var bestProfile = availableProfiles.First();
        return await ActivateProfileAsync(bestProfile.Name, cancellationToken);
    }
}