using GameConsole.Core.Abstractions;
using GameConsole.UI.Profiles.BuiltIn;
using Microsoft.Extensions.Logging;

namespace GameConsole.UI.Profiles;

/// <summary>
/// Manages UI profiles including registration, discovery, switching, and persistence.
/// Provides centralized management of all available profiles and their lifecycle.
/// </summary>
[Service("UI Profile Manager", "1.0.0", "Manages UI profiles for dynamic interface switching",
    Categories = new[] { "UI", "Profiles", "Configuration" }, 
    Lifetime = ServiceLifetime.Singleton)]
public class UIProfileManager : IService
{
    private readonly Dictionary<string, IUIProfile> _profiles = new();
    private readonly Dictionary<ConsoleMode, List<IUIProfile>> _profilesByMode = new();
    private readonly ILogger _logger;
    
    private IUIProfile? _activeProfile;
    private readonly object _lock = new();

    /// <summary>
    /// Currently active profile.
    /// </summary>
    public IUIProfile? ActiveProfile
    {
        get
        {
            lock (_lock)
            {
                return _activeProfile;
            }
        }
    }

    /// <summary>
    /// All registered profiles.
    /// </summary>
    public IReadOnlyDictionary<string, IUIProfile> Profiles
    {
        get
        {
            lock (_lock)
            {
                return _profiles.AsReadOnly();
            }
        }
    }

    /// <summary>
    /// Event raised when the active profile changes.
    /// </summary>
    public event EventHandler<ProfileSwitchedEventArgs>? ProfileSwitched;

    public bool IsRunning { get; private set; }

    public UIProfileManager(ILogger<UIProfileManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        InitializeProfilesByMode();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing UI Profile Manager");

        // Register built-in profiles
        await RegisterBuiltInProfilesAsync(cancellationToken);

        // Load any custom profiles from configuration/persistence
        await LoadCustomProfilesAsync(cancellationToken);

        _logger.LogInformation("UI Profile Manager initialized with {ProfileCount} profiles", _profiles.Count);
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting UI Profile Manager");
        IsRunning = true;
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping UI Profile Manager");
        
        if (_activeProfile != null)
        {
            await _activeProfile.OnDeactivatedAsync(null, cancellationToken);
        }
        
        IsRunning = false;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Registers a new UI profile.
    /// </summary>
    /// <param name="profile">Profile to register.</param>
    /// <exception cref="ArgumentException">Thrown if profile name conflicts.</exception>
    public void RegisterProfile(IUIProfile profile)
    {
        if (profile == null)
            throw new ArgumentNullException(nameof(profile));

        lock (_lock)
        {
            if (_profiles.ContainsKey(profile.Name))
                throw new ArgumentException($"Profile with name '{profile.Name}' already exists", nameof(profile));

            _profiles[profile.Name] = profile;
            
            if (!_profilesByMode.ContainsKey(profile.TargetMode))
                _profilesByMode[profile.TargetMode] = new List<IUIProfile>();
            
            _profilesByMode[profile.TargetMode].Add(profile);

            _logger.LogDebug("Registered UI profile: {ProfileName} (mode: {TargetMode})", 
                profile.Name, profile.TargetMode);
        }
    }

    /// <summary>
    /// Unregisters a UI profile.
    /// </summary>
    /// <param name="profileName">Name of profile to unregister.</param>
    /// <returns>True if profile was removed, false if not found.</returns>
    public bool UnregisterProfile(string profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName))
            return false;

        lock (_lock)
        {
            if (!_profiles.TryGetValue(profileName, out var profile))
                return false;

            // Cannot unregister active profile
            if (_activeProfile == profile)
                throw new InvalidOperationException("Cannot unregister the currently active profile");

            _profiles.Remove(profileName);
            _profilesByMode[profile.TargetMode].Remove(profile);

            _logger.LogDebug("Unregistered UI profile: {ProfileName}", profileName);
            return true;
        }
    }

    /// <summary>
    /// Switches to a different UI profile.
    /// </summary>
    /// <param name="profileName">Name of profile to switch to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SwitchProfileAsync(string profileName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profileName))
            throw new ArgumentException("Profile name cannot be empty", nameof(profileName));

        IUIProfile? newProfile;
        IUIProfile? previousProfile;

        lock (_lock)
        {
            if (!_profiles.TryGetValue(profileName, out newProfile))
                throw new ArgumentException($"Profile '{profileName}' not found", nameof(profileName));

            if (_activeProfile == newProfile)
            {
                _logger.LogDebug("Profile '{ProfileName}' is already active", profileName);
                return;
            }

            previousProfile = _activeProfile;
        }

        _logger.LogInformation("Switching UI profile from '{PreviousProfile}' to '{NewProfile}'", 
            previousProfile?.Name ?? "none", newProfile.Name);

        try
        {
            // Deactivate previous profile
            if (previousProfile != null)
            {
                await previousProfile.OnDeactivatedAsync(newProfile, cancellationToken);
            }

            // Activate new profile
            await newProfile.OnActivatedAsync(previousProfile, cancellationToken);

            lock (_lock)
            {
                _activeProfile = newProfile;
            }

            // Raise event
            ProfileSwitched?.Invoke(this, new ProfileSwitchedEventArgs(previousProfile, newProfile));

            _logger.LogInformation("Successfully switched to UI profile: {ProfileName}", newProfile.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to switch to UI profile: {ProfileName}", profileName);
            throw;
        }
    }

    /// <summary>
    /// Gets profiles compatible with a specific console mode.
    /// </summary>
    /// <param name="mode">Console mode to filter by.</param>
    /// <returns>Profiles compatible with the specified mode.</returns>
    public IEnumerable<IUIProfile> GetProfilesByMode(ConsoleMode mode)
    {
        lock (_lock)
        {
            return _profilesByMode.TryGetValue(mode, out var profiles) 
                ? profiles.ToArray() 
                : Array.Empty<IUIProfile>();
        }
    }

    /// <summary>
    /// Finds the best profile for a given console mode based on priority and compatibility.
    /// </summary>
    /// <param name="mode">Target console mode.</param>
    /// <returns>Best matching profile, or null if none found.</returns>
    public IUIProfile? FindBestProfileForMode(ConsoleMode mode)
    {
        var compatibleProfiles = GetProfilesByMode(mode);
        
        return compatibleProfiles
            .OrderByDescending(p => p.Metadata.Priority)
            .ThenBy(p => p.Metadata.IsBuiltIn ? 0 : 1) // Prefer built-in profiles
            .FirstOrDefault();
    }

    /// <summary>
    /// Validates all registered profiles.
    /// </summary>
    /// <returns>Dictionary of validation results by profile name.</returns>
    public Dictionary<string, ProfileValidationResult> ValidateAllProfiles()
    {
        var results = new Dictionary<string, ProfileValidationResult>();

        IUIProfile[] profiles;
        lock (_lock)
        {
            profiles = _profiles.Values.ToArray();
        }

        foreach (var profile in profiles)
        {
            try
            {
                var result = profile.Validate();
                results[profile.Name] = result;

                if (!result.IsValid)
                {
                    _logger.LogWarning("Profile '{ProfileName}' failed validation: {Errors}", 
                        profile.Name, string.Join(", ", result.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating profile '{ProfileName}'", profile.Name);
                results[profile.Name] = ProfileValidationResult.Failed(new[] { $"Validation threw exception: {ex.Message}" });
            }
        }

        return results;
    }

    /// <summary>
    /// Creates a new profile based on an existing profile with modifications.
    /// </summary>
    /// <param name="basProfileName">Base profile to copy from.</param>
    /// <param name="newProfileName">Name for the new profile.</param>
    /// <param name="modifications">Modifications to apply.</param>
    /// <returns>The newly created profile.</returns>
    public IUIProfile CreateProfileVariant(string basProfileName, string newProfileName, ProfileModifications modifications)
    {
        if (string.IsNullOrWhiteSpace(basProfileName))
            throw new ArgumentException("Base profile name cannot be empty", nameof(basProfileName));
        if (string.IsNullOrWhiteSpace(newProfileName))
            throw new ArgumentException("New profile name cannot be empty", nameof(newProfileName));

        lock (_lock)
        {
            if (!_profiles.TryGetValue(basProfileName, out var baseProfile))
                throw new ArgumentException($"Base profile '{basProfileName}' not found", nameof(basProfileName));

            if (_profiles.ContainsKey(newProfileName))
                throw new ArgumentException($"Profile '{newProfileName}' already exists", nameof(newProfileName));

            var newProfile = baseProfile.CreateVariant(newProfileName, modifications);
            RegisterProfile(newProfile);

            _logger.LogInformation("Created profile variant '{NewProfileName}' based on '{BaseProfileName}'", 
                newProfileName, basProfileName);

            return newProfile;
        }
    }

    private void InitializeProfilesByMode()
    {
        foreach (ConsoleMode mode in Enum.GetValues<ConsoleMode>())
        {
            _profilesByMode[mode] = new List<IUIProfile>();
        }
    }

    private async Task RegisterBuiltInProfilesAsync(CancellationToken cancellationToken)
    {
        var builtInProfiles = new IUIProfile[]
        {
            new ConsoleProfile(_logger),
            new GameProfile(_logger),
            new EditorProfile(_logger),
            new WebProfile(_logger),
            new DesktopProfile(_logger)
        };

        foreach (var profile in builtInProfiles)
        {
            RegisterProfile(profile);
        }

        _logger.LogDebug("Registered {Count} built-in profiles", builtInProfiles.Length);
        await Task.CompletedTask;
    }

    private async Task LoadCustomProfilesAsync(CancellationToken cancellationToken)
    {
        // TODO: Load custom profiles from persistence layer
        // This would typically load from configuration files, database, etc.
        _logger.LogDebug("Loading custom profiles from persistence layer (not implemented yet)");
        await Task.CompletedTask;
    }
}

/// <summary>
/// Event arguments for profile switch events.
/// </summary>
public class ProfileSwitchedEventArgs : EventArgs
{
    /// <summary>
    /// Profile that was previously active (can be null).
    /// </summary>
    public IUIProfile? PreviousProfile { get; }

    /// <summary>
    /// Profile that is now active.
    /// </summary>
    public IUIProfile NewProfile { get; }

    public ProfileSwitchedEventArgs(IUIProfile? previousProfile, IUIProfile newProfile)
    {
        PreviousProfile = previousProfile;
        NewProfile = newProfile ?? throw new ArgumentNullException(nameof(newProfile));
    }
}