using GameConsole.Core.Abstractions;

namespace GameConsole.UI.Profiles;

/// <summary>
/// Manages UI profiles and handles dynamic switching between different interaction modes.
/// Provides profile caching, state preservation, and lifecycle management.
/// </summary>
public sealed class UIProfileManager : IService
{
    private readonly Dictionary<string, IUIProfile> _profiles = new();
    private readonly Dictionary<ConsoleMode, List<IUIProfile>> _profilesByMode = new();
    private readonly ProfileValidator _validator;
    private IUIProfile? _activeProfile;
    private IUIContext? _currentContext;
    private bool _isRunning;

    /// <summary>
    /// Initializes a new instance of the UIProfileManager.
    /// </summary>
    /// <param name="validator">The profile validator to use for consistency checking.</param>
    public UIProfileManager(ProfileValidator validator)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    /// <summary>
    /// Gets the currently active UI profile.
    /// </summary>
    public IUIProfile? ActiveProfile => _activeProfile;

    /// <summary>
    /// Gets all registered profiles.
    /// </summary>
    public IReadOnlyCollection<IUIProfile> AllProfiles => _profiles.Values;

    /// <summary>
    /// Gets a value indicating whether the manager is currently running.
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Registers a UI profile with the manager.
    /// </summary>
    /// <param name="profile">The profile to register.</param>
    public void RegisterProfile(IUIProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        _profiles[profile.Name] = profile;

        if (!_profilesByMode.ContainsKey(profile.TargetMode))
        {
            _profilesByMode[profile.TargetMode] = new List<IUIProfile>();
        }
        
        _profilesByMode[profile.TargetMode].Add(profile);
    }

    /// <summary>
    /// Unregisters a UI profile from the manager.
    /// </summary>
    /// <param name="profileName">The name of the profile to unregister.</param>
    /// <returns>True if the profile was unregistered; otherwise, false.</returns>
    public bool UnregisterProfile(string profileName)
    {
        ArgumentNullException.ThrowIfNull(profileName);

        if (!_profiles.TryGetValue(profileName, out var profile))
        {
            return false;
        }

        _profiles.Remove(profileName);
        _profilesByMode[profile.TargetMode].Remove(profile);

        return true;
    }

    /// <summary>
    /// Gets a profile by name.
    /// </summary>
    /// <param name="profileName">The name of the profile.</param>
    /// <returns>The profile if found; otherwise, null.</returns>
    public IUIProfile? GetProfile(string profileName)
    {
        ArgumentNullException.ThrowIfNull(profileName);
        return _profiles.GetValueOrDefault(profileName);
    }

    /// <summary>
    /// Gets all profiles for a specific console mode.
    /// </summary>
    /// <param name="mode">The console mode.</param>
    /// <returns>Collection of profiles for the specified mode.</returns>
    public IReadOnlyCollection<IUIProfile> GetProfilesForMode(ConsoleMode mode)
    {
        return _profilesByMode.GetValueOrDefault(mode, new List<IUIProfile>());
    }

    /// <summary>
    /// Switches to a different UI profile.
    /// </summary>
    /// <param name="profileName">The name of the profile to switch to.</param>
    /// <param name="context">The UI context for the switch operation.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>True if the switch was successful; otherwise, false.</returns>
    public async Task<bool> SwitchToProfileAsync(string profileName, IUIContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profileName);
        ArgumentNullException.ThrowIfNull(context);

        var targetProfile = GetProfile(profileName);
        if (targetProfile == null)
        {
            return false;
        }

        // Validate the target profile
        var validationResult = await _validator.ValidateProfileAsync(targetProfile, context, cancellationToken);
        if (!validationResult.IsValid)
        {
            return false;
        }

        // Check if the profile can be activated
        if (!await targetProfile.CanActivateAsync(context, cancellationToken))
        {
            return false;
        }

        // Deactivate current profile if any
        if (_activeProfile != null && _currentContext != null)
        {
            await _activeProfile.DeactivateAsync(_currentContext, cancellationToken);
        }

        // Activate the new profile
        await targetProfile.ActivateAsync(context, cancellationToken);

        _activeProfile = targetProfile;
        _currentContext = context;

        return true;
    }

    /// <summary>
    /// Initializes the profile manager.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Initialize any required resources
        return Task.CompletedTask;
    }

    /// <summary>
    /// Starts the profile manager.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = true;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the profile manager.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_activeProfile != null && _currentContext != null)
        {
            await _activeProfile.DeactivateAsync(_currentContext, cancellationToken);
            _activeProfile = null;
            _currentContext = null;
        }

        _isRunning = false;
    }

    /// <summary>
    /// Disposes of the profile manager and cleans up resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_isRunning)
        {
            await StopAsync();
        }

        _profiles.Clear();
        _profilesByMode.Clear();
    }
}