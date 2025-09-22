using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace GameConsole.UI.Profiles;

/// <summary>
/// Default implementation of IUIProfileService that manages UI profiles.
/// </summary>
[Service("UI Profile Service", "Manages UI profiles and profile switching")]
public class UIProfileService : IUIProfileService
{
    private readonly ILogger<UIProfileService> _logger;
    private readonly Dictionary<string, IUIProfile> _profiles;
    private readonly object _lock = new();
    private IUIProfile? _activeProfile;
    private bool _isRunning;
    private bool _disposed;

    public UIProfileService(ILogger<UIProfileService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _profiles = new Dictionary<string, IUIProfile>(StringComparer.OrdinalIgnoreCase);
    }

    #region IUIProfileService Implementation

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

    public IReadOnlyCollection<IUIProfile> Profiles
    {
        get
        {
            lock (_lock)
            {
                return _profiles.Values.ToArray();
            }
        }
    }

    public event EventHandler<ProfileChangedEventArgs>? ProfileChanged;

    public async Task RegisterProfileAsync(IUIProfile profile, CancellationToken cancellationToken = default)
    {
        if (profile == null)
            throw new ArgumentNullException(nameof(profile));

        ThrowIfDisposed();

        lock (_lock)
        {
            if (_profiles.ContainsKey(profile.Id))
            {
                _logger.LogWarning("Profile with ID {ProfileId} is already registered", profile.Id);
                return;
            }

            _profiles[profile.Id] = profile;
        }

        _logger.LogInformation("Registered UI profile: {ProfileName} ({ProfileId}) - Mode: {Mode}", 
            profile.Name, profile.Id, profile.Mode);

        await Task.CompletedTask;
    }

    public async Task UnregisterProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profileId))
            throw new ArgumentException("Profile ID cannot be null or whitespace.", nameof(profileId));

        ThrowIfDisposed();

        IUIProfile? profileToRemove = null;
        bool wasActive = false;

        lock (_lock)
        {
            if (_profiles.TryGetValue(profileId, out profileToRemove))
            {
                wasActive = profileToRemove == _activeProfile;
                _profiles.Remove(profileId);

                if (wasActive)
                {
                    _activeProfile = null;
                }
            }
        }

        if (profileToRemove != null)
        {
            // Deactivate if it was active
            if (wasActive && profileToRemove.IsActive)
            {
                await profileToRemove.DeactivateAsync(cancellationToken);
                OnProfileChanged(profileToRemove, null);
            }

            _logger.LogInformation("Unregistered UI profile: {ProfileName} ({ProfileId})", 
                profileToRemove.Name, profileToRemove.Id);
        }
        else
        {
            _logger.LogWarning("Attempted to unregister non-existent profile: {ProfileId}", profileId);
        }
    }

    public async Task ActivateProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profileId))
            throw new ArgumentException("Profile ID cannot be null or whitespace.", nameof(profileId));

        ThrowIfDisposed();
        ThrowIfNotRunning();

        IUIProfile? profileToActivate;
        IUIProfile? previousProfile;

        lock (_lock)
        {
            if (!_profiles.TryGetValue(profileId, out profileToActivate))
            {
                throw new InvalidOperationException($"Profile with ID '{profileId}' is not registered.");
            }

            previousProfile = _activeProfile;
            
            if (previousProfile == profileToActivate)
            {
                _logger.LogInformation("Profile {ProfileId} is already active", profileId);
                return;
            }
        }

        // Deactivate current profile if any
        if (previousProfile != null && previousProfile.IsActive)
        {
            await previousProfile.DeactivateAsync(cancellationToken);
        }

        // Activate new profile
        await profileToActivate.ActivateAsync(cancellationToken);

        lock (_lock)
        {
            _activeProfile = profileToActivate;
        }

        OnProfileChanged(previousProfile, profileToActivate);
    }

    public IUIProfile? GetProfile(string profileId)
    {
        if (string.IsNullOrWhiteSpace(profileId))
            return null;

        lock (_lock)
        {
            _profiles.TryGetValue(profileId, out var profile);
            return profile;
        }
    }

    public IReadOnlyCollection<IUIProfile> GetProfilesByMode(UIMode mode)
    {
        lock (_lock)
        {
            return _profiles.Values.Where(p => p.Mode == mode).ToArray();
        }
    }

    #endregion

    #region IService Implementation

    public bool IsRunning => _isRunning && !_disposed;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _logger.LogInformation("Initializing UI Profile Service");

        // Initialize with default TUI profile
        var defaultProfile = new TUIProfile(_logger);
        await RegisterProfileAsync(defaultProfile, cancellationToken);

        _logger.LogInformation("Initialized UI Profile Service with {ProfileCount} profiles", _profiles.Count);
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _logger.LogInformation("Starting UI Profile Service");
        _isRunning = true;

        // Activate the default TUI profile if no profile is active
        lock (_lock)
        {
            if (_activeProfile == null && _profiles.ContainsKey(TUIProfile.DefaultId))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ActivateProfileAsync(TUIProfile.DefaultId, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to activate default TUI profile on startup");
                    }
                }, cancellationToken);
            }
        }

        _logger.LogInformation("Started UI Profile Service");
        
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping UI Profile Service");

        _isRunning = false;

        // Deactivate current profile
        var currentProfile = ActiveProfile;
        if (currentProfile != null && currentProfile.IsActive)
        {
            await currentProfile.DeactivateAsync(cancellationToken);
            lock (_lock)
            {
                _activeProfile = null;
            }
        }

        _logger.LogInformation("Stopped UI Profile Service");
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        if (_isRunning)
        {
            await StopAsync();
        }

        // Clear all profiles
        lock (_lock)
        {
            _profiles.Clear();
            _activeProfile = null;
        }

        _disposed = true;
    }

    #endregion

    #region Private Methods

    private void OnProfileChanged(IUIProfile? previous, IUIProfile? current)
    {
        ProfileChanged?.Invoke(this, new ProfileChangedEventArgs(previous, current));
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(UIProfileService));
    }

    private void ThrowIfNotRunning()
    {
        ThrowIfDisposed();
        if (!_isRunning) throw new InvalidOperationException("UI Profile Service is not running.");
    }

    #endregion
}