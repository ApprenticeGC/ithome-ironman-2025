using GameConsole.Core.Abstractions;
using GameConsole.Profile.Core;
using Microsoft.Extensions.Logging;

namespace GameConsole.Profile.Services;

/// <summary>
/// Base implementation for profile managers providing common functionality.
/// </summary>
public abstract class BaseProfileManager : IProfileManager
{
    protected readonly ILogger _logger;
    protected readonly IProfileProvider _profileProvider;
    private bool _isRunning;
    private bool _disposed;
    private IProfile? _activeProfile;

    protected BaseProfileManager(ILogger logger, IProfileProvider profileProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _profileProvider = profileProvider ?? throw new ArgumentNullException(nameof(profileProvider));
    }

    #region IService Implementation

    public bool IsRunning => _isRunning && !_disposed;

    public virtual async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BaseProfileManager));
        
        _logger.LogInformation("Initializing {ServiceType}", GetType().Name);
        await OnInitializeAsync(cancellationToken);
        
        // Load the active profile
        var activeProfileId = await _profileProvider.GetActiveProfileIdAsync(cancellationToken);
        if (!string.IsNullOrEmpty(activeProfileId))
        {
            _activeProfile = await _profileProvider.LoadProfileAsync(activeProfileId, cancellationToken);
            if (_activeProfile != null)
            {
                _logger.LogInformation("Loaded active profile: {ProfileName} ({ProfileId})", 
                    _activeProfile.Name, _activeProfile.Id);
            }
        }
        
        _logger.LogInformation("Initialized {ServiceType}", GetType().Name);
    }

    public virtual async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BaseProfileManager));
        
        _logger.LogInformation("Starting {ServiceType}", GetType().Name);
        await OnStartAsync(cancellationToken);
        _isRunning = true;
        _logger.LogInformation("Started {ServiceType}", GetType().Name);
    }

    public virtual async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping {ServiceType}", GetType().Name);
        _isRunning = false;
        await OnStopAsync(cancellationToken);
        _logger.LogInformation("Stopped {ServiceType}", GetType().Name);
    }

    public virtual async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        
        if (_isRunning)
        {
            await StopAsync();
        }
        
        await OnDisposeAsync();
        _disposed = true;
    }

    #endregion

    #region IProfileManager Implementation

    public event EventHandler<ProfileChangedEventArgs>? ActiveProfileChanged;

    public virtual async Task<IProfile> GetActiveProfileAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        if (_activeProfile == null)
        {
            // Try to load default profile or create one
            _activeProfile = await GetOrCreateDefaultProfileAsync(cancellationToken);
        }
        
        return _activeProfile;
    }

    public virtual async Task SetActiveProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        if (string.IsNullOrWhiteSpace(profileId))
            throw new ArgumentException("Profile ID cannot be null or empty.", nameof(profileId));

        var newProfile = await _profileProvider.LoadProfileAsync(profileId, cancellationToken);
        if (newProfile == null)
            throw new InvalidOperationException($"Profile with ID '{profileId}' not found.");

        var oldProfile = _activeProfile;
        _activeProfile = newProfile;
        
        await _profileProvider.SetActiveProfileIdAsync(profileId, cancellationToken);
        
        _logger.LogInformation("Active profile changed from {OldProfile} to {NewProfile}", 
            oldProfile?.Name ?? "None", newProfile.Name);
        
        ActiveProfileChanged?.Invoke(this, new ProfileChangedEventArgs(oldProfile, newProfile));
    }

    public virtual async Task<IEnumerable<IProfile>> GetAllProfilesAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return await _profileProvider.LoadProfilesAsync(cancellationToken);
    }

    public virtual async Task<IProfile?> GetProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        if (string.IsNullOrWhiteSpace(profileId))
            return null;
            
        return await _profileProvider.LoadProfileAsync(profileId, cancellationToken);
    }

    public virtual async Task<IProfile> CreateProfileAsync(string name, ProfileType type, string? description = null, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Profile name cannot be null or empty.", nameof(name));

        var profile = new Core.Profile(
            id: Guid.NewGuid().ToString(),
            name: name,
            type: type,
            description: description ?? string.Empty
        );
        
        // Apply default configurations based on type
        await ApplyDefaultConfigurationsAsync(profile, cancellationToken);
        
        await _profileProvider.SaveProfileAsync(profile, cancellationToken);
        
        _logger.LogInformation("Created new profile: {ProfileName} ({ProfileType})", name, type);
        
        return profile;
    }

    public virtual async Task UpdateProfileAsync(IProfile profile, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        if (profile == null)
            throw new ArgumentNullException(nameof(profile));
            
        if (profile.IsReadOnly)
            throw new InvalidOperationException("Cannot update a read-only profile.");
        
        await _profileProvider.SaveProfileAsync(profile, cancellationToken);
        
        _logger.LogInformation("Updated profile: {ProfileName} ({ProfileId})", profile.Name, profile.Id);
    }

    public virtual async Task<bool> DeleteProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        if (string.IsNullOrWhiteSpace(profileId))
            return false;
        
        var profile = await _profileProvider.LoadProfileAsync(profileId, cancellationToken);
        if (profile?.IsReadOnly == true)
            throw new InvalidOperationException("Cannot delete a read-only profile.");
        
        // If deleting the active profile, switch to default
        if (_activeProfile?.Id == profileId)
        {
            var defaultProfile = await GetOrCreateDefaultProfileAsync(cancellationToken);
            await SetActiveProfileAsync(defaultProfile.Id, cancellationToken);
        }
        
        var result = await _profileProvider.DeleteProfileAsync(profileId, cancellationToken);
        
        if (result)
        {
            _logger.LogInformation("Deleted profile: {ProfileId}", profileId);
        }
        
        return result;
    }

    public virtual async Task<bool> ValidateProfileAsync(IProfile profile, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        if (profile == null)
            return false;
            
        // Basic validation
        if (string.IsNullOrWhiteSpace(profile.Id) || string.IsNullOrWhiteSpace(profile.Name))
            return false;
        
        // Custom validation logic
        return await OnValidateProfileAsync(profile, cancellationToken);
    }

    #endregion

    #region Protected Methods for Derived Classes

    protected virtual Task OnInitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    protected virtual Task OnStartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    protected virtual Task OnStopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    protected virtual ValueTask OnDisposeAsync() => ValueTask.CompletedTask;
    
    protected virtual Task<bool> OnValidateProfileAsync(IProfile profile, CancellationToken cancellationToken = default) => Task.FromResult(true);
    
    protected virtual async Task ApplyDefaultConfigurationsAsync(Core.Profile profile, CancellationToken cancellationToken = default)
    {
        // Derived classes can override to provide type-specific configurations
        await Task.CompletedTask;
    }
    
    protected async Task<IProfile> GetOrCreateDefaultProfileAsync(CancellationToken cancellationToken = default)
    {
        var profiles = await _profileProvider.LoadProfilesAsync(cancellationToken);
        var defaultProfile = profiles.FirstOrDefault(p => p.Type == ProfileType.Default);
        
        if (defaultProfile == null)
        {
            // Create a default profile
            defaultProfile = await CreateDefaultProfileAsync(cancellationToken);
        }
        
        return defaultProfile;
    }
    
    protected virtual async Task<IProfile> CreateDefaultProfileAsync(CancellationToken cancellationToken = default)
    {
        var defaultProfile = new Core.Profile(
            id: "default",
            name: "Default Profile",
            type: ProfileType.Default,
            description: "Default system profile with standard configurations",
            isReadOnly: true
        );
        
        await _profileProvider.SaveProfileAsync(defaultProfile, cancellationToken);
        return defaultProfile;
    }

    protected void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(GetType().Name);
    }

    protected void ThrowIfNotRunning()
    {
        ThrowIfDisposed();
        if (!_isRunning) throw new InvalidOperationException($"{GetType().Name} is not running");
    }

    #endregion
}