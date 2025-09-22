using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.UI.Services;

/// <summary>
/// Service that manages UI profiles and provides profile switching capabilities.
/// Coordinates between different UI modes (TUI, Unity-like, Godot-like) and manages
/// provider/system swapping to simulate different behaviors.
/// </summary>
[Service("UI Profile Configuration", "1.0.0", 
    "Manages UI profiles and coordinates provider switching for different interface modes", 
    Categories = new[] { "UI", "Configuration", "Profile" }, 
    Lifetime = ServiceLifetime.Singleton)]
public class UIProfileConfigurationService : IService, IUIProfileProvider
{
    private readonly ILogger<UIProfileConfigurationService> _logger;
    private readonly ConcurrentDictionary<string, IUIProfile> _profiles;
    private IUIProfile? _activeProfile;
    private bool _isRunning;

    public event EventHandler<UIProfileChangeEventArgs>? ProfileChanged;

    public UIProfileConfigurationService(ILogger<UIProfileConfigurationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _profiles = new ConcurrentDictionary<string, IUIProfile>();
        _isRunning = false;
    }

    public IUIProfile? ActiveProfile => _activeProfile;
    public bool IsRunning => _isRunning;

    #region IService Implementation

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing UI Profile Configuration Service");
        
        try
        {
            // Register built-in profiles
            await RegisterBuiltInProfilesAsync(cancellationToken);
            
            // Load saved configuration
            await LoadConfigurationAsync(cancellationToken);
            
            _logger.LogInformation("Initialized UI Profile Configuration Service with {Count} profiles", 
                _profiles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize UI Profile Configuration Service");
            throw;
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting UI Profile Configuration Service");
        
        try
        {
            // Set default profile if none is active
            if (_activeProfile == null)
            {
                var defaultProfile = _profiles.Values.FirstOrDefault(p => p.ProfileType == UIProfileType.TUI)
                                   ?? _profiles.Values.FirstOrDefault();
                
                if (defaultProfile != null)
                {
                    await SwitchProfileAsync(defaultProfile.Id, cancellationToken);
                }
            }
            
            _isRunning = true;
            _logger.LogInformation("Started UI Profile Configuration Service");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start UI Profile Configuration Service");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping UI Profile Configuration Service");
        
        try
        {
            // Deactivate current profile
            if (_activeProfile?.IsActive == true)
            {
                await _activeProfile.DeactivateAsync(cancellationToken);
            }
            
            // Save current configuration
            await SaveConfigurationAsync(cancellationToken);
            
            _isRunning = false;
            _logger.LogInformation("Stopped UI Profile Configuration Service");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop UI Profile Configuration Service");
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_isRunning)
        {
            await StopAsync();
        }
        
        // Dispose of all profiles if they implement IAsyncDisposable
        foreach (var profile in _profiles.Values)
        {
            if (profile is IAsyncDisposable disposableProfile)
            {
                await disposableProfile.DisposeAsync();
            }
        }
        
        _profiles.Clear();
    }

    #endregion

    #region IUIProfileProvider Implementation

    public Task<IEnumerable<IUIProfile>> GetAvailableProfilesAsync(CancellationToken cancellationToken = default)
    {
        var profiles = _profiles.Values.ToList();
        _logger.LogDebug("Retrieved {Count} available profiles", profiles.Count);
        return Task.FromResult<IEnumerable<IUIProfile>>(profiles);
    }

    public Task<IEnumerable<IUIProfile>> GetProfilesByTypeAsync(UIProfileType profileType, CancellationToken cancellationToken = default)
    {
        var profiles = _profiles.Values
            .Where(p => p.ProfileType == profileType)
            .ToList();
        
        _logger.LogDebug("Retrieved {Count} profiles of type {ProfileType}", profiles.Count, profileType);
        return Task.FromResult<IEnumerable<IUIProfile>>(profiles);
    }

    public Task<IUIProfile?> GetProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profileId))
            throw new ArgumentException("Profile ID cannot be null or empty", nameof(profileId));

        _profiles.TryGetValue(profileId, out var profile);
        _logger.LogDebug("Retrieved profile {ProfileId}: {Found}", profileId, profile != null ? "Found" : "Not Found");
        
        return Task.FromResult(profile);
    }

    public async Task SwitchProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profileId))
            throw new ArgumentException("Profile ID cannot be null or empty", nameof(profileId));

        if (!_profiles.TryGetValue(profileId, out var newProfile))
        {
            throw new ArgumentException($"Profile '{profileId}' not found", nameof(profileId));
        }

        if (_activeProfile?.Id == profileId)
        {
            _logger.LogDebug("Profile {ProfileId} is already active", profileId);
            return;
        }

        _logger.LogInformation("Switching UI profile from {CurrentProfile} to {NewProfile}", 
            _activeProfile?.Name ?? "None", newProfile.Name);

        var previousProfile = _activeProfile;
        
        try
        {
            // Deactivate current profile
            if (previousProfile?.IsActive == true)
            {
                await previousProfile.DeactivateAsync(cancellationToken);
            }

            // Activate new profile
            await newProfile.ActivateAsync(cancellationToken);
            _activeProfile = newProfile;

            // Raise profile changed event
            var eventArgs = new UIProfileChangeEventArgs(previousProfile, newProfile);
            ProfileChanged?.Invoke(this, eventArgs);

            _logger.LogInformation("Successfully switched to profile: {ProfileName} ({ProfileType})", 
                newProfile.Name, newProfile.ProfileType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to switch to profile: {ProfileName}", newProfile.Name);
            
            // Attempt to reactivate previous profile on failure
            if (previousProfile != null && !previousProfile.IsActive)
            {
                try
                {
                    await previousProfile.ActivateAsync(cancellationToken);
                    _activeProfile = previousProfile;
                    _logger.LogWarning("Restored previous profile: {ProfileName}", previousProfile.Name);
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx, "Failed to restore previous profile: {ProfileName}", previousProfile.Name);
                    _activeProfile = null;
                }
            }
            
            throw;
        }
    }

    public Task RegisterProfileAsync(IUIProfile profile, CancellationToken cancellationToken = default)
    {
        if (profile == null)
            throw new ArgumentNullException(nameof(profile));

        if (string.IsNullOrWhiteSpace(profile.Id))
            throw new ArgumentException("Profile ID cannot be null or empty");

        if (_profiles.ContainsKey(profile.Id))
        {
            throw new ArgumentException($"Profile with ID '{profile.Id}' is already registered");
        }

        _profiles[profile.Id] = profile;
        _logger.LogInformation("Registered UI profile: {ProfileName} ({ProfileType})", 
            profile.Name, profile.ProfileType);

        return Task.CompletedTask;
    }

    public Task UnregisterProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profileId))
            throw new ArgumentException("Profile ID cannot be null or empty", nameof(profileId));

        if (_activeProfile?.Id == profileId)
        {
            throw new InvalidOperationException("Cannot unregister the currently active profile");
        }

        if (_profiles.TryRemove(profileId, out var removedProfile))
        {
            _logger.LogInformation("Unregistered UI profile: {ProfileName}", removedProfile.Name);
        }
        else
        {
            _logger.LogWarning("Attempted to unregister non-existent profile: {ProfileId}", profileId);
        }

        return Task.CompletedTask;
    }

    public async Task SaveConfigurationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Saving UI profile configuration");

            // In a real implementation, this would save profile configuration to a file or database
            // For now, we'll just simulate the save operation
            await Task.Delay(10, cancellationToken); // Simulate I/O delay

            _logger.LogDebug("Saved UI profile configuration with {ProfileCount} profiles", 
                _profiles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save UI profile configuration");
            throw;
        }
    }

    public async Task LoadConfigurationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Loading UI profile configuration");

            // In a real implementation, this would load from a file or database
            // For now, we'll just simulate loading
            await Task.Delay(10, cancellationToken); // Simulate I/O delay

            _logger.LogDebug("Loaded UI profile configuration");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load UI profile configuration");
            // Don't throw - we can continue with default profiles
        }
    }

    public Task<bool> ValidateProfileAsync(IUIProfile profile, CancellationToken cancellationToken = default)
    {
        if (profile == null)
            return Task.FromResult(false);

        // Basic validation
        var isValid = !string.IsNullOrWhiteSpace(profile.Id) &&
                     !string.IsNullOrWhiteSpace(profile.Name) &&
                     !string.IsNullOrWhiteSpace(profile.Version);

        _logger.LogDebug("Validated profile {ProfileId}: {IsValid}", profile.Id, isValid);
        return Task.FromResult(isValid);
    }

    #endregion

    #region ICapabilityProvider Implementation

    public Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var capabilities = new[] { typeof(IUIProfileProvider) };
        return Task.FromResult<IEnumerable<Type>>(capabilities);
    }

    public Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        var hasCapability = typeof(T) == typeof(IUIProfileProvider);
        return Task.FromResult(hasCapability);
    }

    public Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (typeof(T) == typeof(IUIProfileProvider))
        {
            return Task.FromResult(this as T);
        }
        return Task.FromResult<T?>(null);
    }

    #endregion

    #region Private Methods

    private async Task RegisterBuiltInProfilesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Registering built-in UI profiles");

        // Create simple null loggers for the profiles to avoid extra dependencies
        var nullLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        var tuiLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<TUIProfile>();
        var unityLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<UnityProfile>();
        var godotLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<GodotProfile>();

        // Register TUI profile
        var tuiProfile = new TUIProfile(tuiLogger);
        await RegisterProfileAsync(tuiProfile, cancellationToken);

        // Register Unity profile
        var unityProfile = new UnityProfile(unityLogger);
        await RegisterProfileAsync(unityProfile, cancellationToken);

        // Register Godot profile
        var godotProfile = new GodotProfile(godotLogger);
        await RegisterProfileAsync(godotProfile, cancellationToken);

        _logger.LogInformation("Registered {Count} built-in UI profiles", 3);
    }

    #endregion
}