using System.Collections.Concurrent;
using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace GameConsole.Profile.Core;

/// <summary>
/// Default implementation of IProfileManager that manages UI profile configurations
/// and handles switching between different interface behaviors.
/// </summary>
public class ProfileManager : IProfileManager
{
    private readonly ConcurrentDictionary<string, IProfileConfiguration> _profiles = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProfileManager>? _logger;
    private IProfileConfiguration? _activeProfile;
    private bool _isRunning;
    private bool _disposed;

    /// <inheritdoc />
    public IProfileConfiguration? ActiveProfile => _activeProfile;

    /// <inheritdoc />
    public event EventHandler<ProfileChangedEventArgs>? ProfileChanged;

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Initializes a new instance of the ProfileManager class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency resolution.</param>
    /// <param name="logger">Optional logger for profile management operations.</param>
    public ProfileManager(IServiceProvider serviceProvider, ILogger<ProfileManager>? logger = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger;
    }

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ProfileManager));

        _logger?.LogInformation("Initializing ProfileManager");
        
        // Register built-in profiles
        RegisterBuiltInProfiles();
        
        _logger?.LogDebug("Registered {ProfileCount} built-in profiles", _profiles.Count);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ProfileManager));

        _logger?.LogInformation("Starting ProfileManager");
        
        // Activate the best available profile
        var bestProfile = await ActivateBestProfile(cancellationToken);
        if (bestProfile != null)
        {
            _logger?.LogInformation("Auto-activated profile: {ProfileName}", bestProfile.DisplayName);
        }
        else
        {
            _logger?.LogWarning("No suitable profile found for auto-activation");
        }

        _isRunning = true;
        _logger?.LogInformation("Started ProfileManager");
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ProfileManager));

        _logger?.LogInformation("Stopping ProfileManager");
        
        await DeactivateCurrentProfile(cancellationToken);
        _isRunning = false;
        
        _logger?.LogInformation("Stopped ProfileManager");
    }

    /// <inheritdoc />
    public Task<IEnumerable<IProfileConfiguration>> GetAvailableProfiles(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ProfileManager));

        return Task.FromResult<IEnumerable<IProfileConfiguration>>(_profiles.Values.ToList());
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IProfileConfiguration>> GetSupportedProfiles(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ProfileManager));

        var supportedProfiles = new List<IProfileConfiguration>();
        
        foreach (var profile in _profiles.Values)
        {
            try
            {
                if (await profile.IsSupported(cancellationToken))
                {
                    supportedProfiles.Add(profile);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error checking support for profile {ProfileId}", profile.ProfileId);
            }
        }

        return supportedProfiles.OrderByDescending(p => p.Priority);
    }

    /// <inheritdoc />
    public void RegisterProfile(IProfileConfiguration profile)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ProfileManager));
        
        if (profile == null)
            throw new ArgumentNullException(nameof(profile));

        _profiles.AddOrUpdate(profile.ProfileId, profile, (_, _) => profile);
        _logger?.LogDebug("Registered profile: {ProfileId} - {DisplayName}", profile.ProfileId, profile.DisplayName);
    }

    /// <inheritdoc />
    public async Task<bool> ActivateProfile(string profileId, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ProfileManager));
        
        if (profileId == null)
            throw new ArgumentNullException(nameof(profileId));

        if (!_profiles.TryGetValue(profileId, out var profile))
        {
            _logger?.LogWarning("Profile not found: {ProfileId}", profileId);
            return false;
        }

        try
        {
            if (!await profile.IsSupported(cancellationToken))
            {
                _logger?.LogWarning("Profile {ProfileId} is not supported in current environment", profileId);
                return false;
            }

            var previousProfile = _activeProfile;
            _activeProfile = profile;

            // Raise profile changed event
            ProfileChanged?.Invoke(this, new ProfileChangedEventArgs(previousProfile, _activeProfile));

            _logger?.LogInformation("Activated profile: {ProfileId} - {DisplayName}", profile.ProfileId, profile.DisplayName);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error activating profile {ProfileId}", profileId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<IProfileConfiguration?> ActivateBestProfile(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ProfileManager));

        var supportedProfiles = await GetSupportedProfiles(cancellationToken);
        var bestProfile = supportedProfiles.FirstOrDefault();

        if (bestProfile != null && await ActivateProfile(bestProfile.ProfileId, cancellationToken))
        {
            return bestProfile;
        }

        return null;
    }

    /// <inheritdoc />
    public Task DeactivateCurrentProfile(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ProfileManager));

        if (_activeProfile != null)
        {
            var previousProfile = _activeProfile;
            _activeProfile = null;

            // Raise profile changed event
            ProfileChanged?.Invoke(this, new ProfileChangedEventArgs(previousProfile, null));

            _logger?.LogInformation("Deactivated profile: {ProfileId}", previousProfile.ProfileId);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Registers built-in profile configurations.
    /// </summary>
    private void RegisterBuiltInProfiles()
    {
        // Register TUI (Terminal User Interface) profile - highest priority as it's TUI-first
        RegisterProfile(new TuiProfile());
        
        // Register Unity-like profile
        RegisterProfile(new UnityProfile());
        
        // Register Godot-like profile
        RegisterProfile(new GodotProfile());
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        
        await StopAsync();
        _disposed = true;
    }
}