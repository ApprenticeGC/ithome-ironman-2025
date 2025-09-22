using GameConsole.Core.Abstractions;

namespace GameConsole.UI.Profiles;

/// <summary>
/// Implementation of the UI profile manager.
/// </summary>
[Service("UI Profile Manager", "1.0.0", "Manages UI profiles and console mode switching")]
public class UIProfileManager : IUIProfileManager
{
    private readonly Dictionary<string, UIProfile> _profiles = new();
    private readonly Dictionary<ConsoleMode, List<UIProfile>> _profilesByMode = new();
    
    private ConsoleMode _currentMode = ConsoleMode.Game;
    private UIProfile? _activeProfile;

    /// <inheritdoc />
    public ConsoleMode CurrentMode => _currentMode;

    /// <inheritdoc />
    public UIProfile? ActiveProfile => _activeProfile;

    /// <inheritdoc />
    public IReadOnlyList<UIProfile> AvailableProfiles => _profiles.Values.ToList();

    /// <inheritdoc />
    public bool IsRunning { get; private set; }

    /// <inheritdoc />
    public event EventHandler<ModeChangedEventArgs>? ModeChanged;

    /// <inheritdoc />
    public event EventHandler<ProfileChangedEventArgs>? ProfileChanged;

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Initialize profile collections for each mode
        _profilesByMode[ConsoleMode.Game] = new List<UIProfile>();
        _profilesByMode[ConsoleMode.Editor] = new List<UIProfile>();
        
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        IsRunning = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        IsRunning = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<bool> SwitchModeAsync(ConsoleMode mode, CancellationToken cancellationToken = default)
    {
        if (_currentMode == mode)
            return true;

        var previousMode = _currentMode;
        
        // Deactivate current profile
        if (_activeProfile != null)
        {
            await _activeProfile.DeactivateAsync(cancellationToken);
        }

        // Switch mode
        _currentMode = mode;
        
        // Try to activate a profile for the new mode
        var profilesForMode = _profilesByMode.GetValueOrDefault(mode, new List<UIProfile>());
        var targetProfile = profilesForMode.FirstOrDefault();
        
        var previousProfile = _activeProfile;
        _activeProfile = null;
        
        if (targetProfile != null)
        {
            if (await targetProfile.CanActivateAsync(cancellationToken))
            {
                await targetProfile.ActivateAsync(cancellationToken);
                _activeProfile = targetProfile;
            }
        }

        // Fire events
        ModeChanged?.Invoke(this, new ModeChangedEventArgs(previousMode, _currentMode));
        if (previousProfile != _activeProfile)
        {
            ProfileChanged?.Invoke(this, new ProfileChangedEventArgs(previousProfile, _activeProfile));
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ActivateProfileAsync(string profileName, CancellationToken cancellationToken = default)
    {
        if (!_profiles.TryGetValue(profileName, out var profile))
            return false;

        if (_activeProfile == profile)
            return true;

        if (!await profile.CanActivateAsync(cancellationToken))
            return false;

        var previousProfile = _activeProfile;
        
        // Deactivate current profile
        if (_activeProfile != null)
        {
            await _activeProfile.DeactivateAsync(cancellationToken);
        }

        // Activate new profile
        await profile.ActivateAsync(cancellationToken);
        _activeProfile = profile;
        
        // Update current mode if needed
        var previousMode = _currentMode;
        if (_currentMode != profile.TargetMode)
        {
            _currentMode = profile.TargetMode;
            ModeChanged?.Invoke(this, new ModeChangedEventArgs(previousMode, _currentMode));
        }

        ProfileChanged?.Invoke(this, new ProfileChangedEventArgs(previousProfile, _activeProfile));

        return true;
    }

    /// <inheritdoc />
    public void RegisterProfile(UIProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        
        _profiles[profile.Name] = profile;
        
        if (_profilesByMode.TryGetValue(profile.TargetMode, out var profilesForMode))
        {
            profilesForMode.Add(profile);
        }
        else
        {
            _profilesByMode[profile.TargetMode] = new List<UIProfile> { profile };
        }
    }

    /// <inheritdoc />
    public UIProfile? GetProfileByName(string name)
    {
        return _profiles.GetValueOrDefault(name);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_activeProfile != null)
        {
            await _activeProfile.DeactivateAsync();
        }
    }
}