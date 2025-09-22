using GameConsole.Core.Abstractions;
using GameConsole.UI.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace GameConsole.UI.Services;

/// <summary>
/// Implementation of the UI profile service that manages UI profiles in the GameConsole system.
/// </summary>
public class UIProfileService : IUIProfileService
{
    private readonly ILogger<UIProfileService> _logger;
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string, IUIProfile> _profiles;
    private readonly object _lockObject = new object();
    private bool _isRunning;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the UIProfileService class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="configuration">The configuration provider.</param>
    public UIProfileService(ILogger<UIProfileService> logger, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _profiles = new Dictionary<string, IUIProfile>();
    }

    #region IService Implementation

    /// <inheritdoc />
    public bool IsRunning => _isRunning && !_disposed;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        _logger.LogInformation("Initializing UIProfileService");
        
        await LoadDefaultProfilesAsync(cancellationToken);
        await ReloadProfilesAsync(cancellationToken);
        
        _logger.LogInformation("UIProfileService initialized with {ProfileCount} profiles", _profiles.Count);
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        _logger.LogInformation("Starting UIProfileService");
        _isRunning = true;
        
        // Set default profile if none is active
        if (ActiveProfile == null)
        {
            var defaultProfile = _profiles.Values.FirstOrDefault(p => p.Configuration.IsDefault) 
                               ?? _profiles.Values.FirstOrDefault();
            
            if (defaultProfile != null)
            {
                await SwitchToProfileAsync(defaultProfile.Id, cancellationToken);
            }
        }
        
        _logger.LogInformation("UIProfileService started successfully");
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning) return;
        
        _logger.LogInformation("Stopping UIProfileService");
        _isRunning = false;
        
        // Deactivate current profile
        if (ActiveProfile != null)
        {
            try
            {
                await ActiveProfile.DeactivateAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error deactivating profile {ProfileName} during service stop", ActiveProfile.Name);
            }
        }
        
        _logger.LogInformation("UIProfileService stopped");
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        
        if (_isRunning)
        {
            await StopAsync();
        }
        
        lock (_lockObject)
        {
            _profiles.Clear();
        }
        
        _disposed = true;
    }

    #endregion

    #region ICapabilityProvider Implementation

    /// <inheritdoc />
    public Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var capabilities = new Type[]
        {
            typeof(IUIProfileService),
            typeof(IUIProfile)
        };
        
        return Task.FromResult<IEnumerable<Type>>(capabilities);
    }

    /// <inheritdoc />
    public Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        var hasCapability = typeof(T) == typeof(IUIProfileService) || 
                           typeof(T) == typeof(IUIProfile);
        
        return Task.FromResult(hasCapability);
    }

    /// <inheritdoc />
    public Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (typeof(T) == typeof(IUIProfileService))
        {
            return Task.FromResult(this as T);
        }
        
        if (typeof(T) == typeof(IUIProfile))
        {
            return Task.FromResult(ActiveProfile as T);
        }
        
        return Task.FromResult<T?>(null);
    }

    #endregion

    #region IUIProfileService Implementation

    /// <inheritdoc />
    public IUIProfile? ActiveProfile { get; private set; }

    /// <inheritdoc />
    public event EventHandler<UIProfileChangedEventArgs>? ProfileChanged;

    /// <inheritdoc />
    public Task<IEnumerable<IUIProfile>> GetAvailableProfilesAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        lock (_lockObject)
        {
            return Task.FromResult<IEnumerable<IUIProfile>>(_profiles.Values.ToList());
        }
    }

    /// <inheritdoc />
    public Task<IUIProfile?> GetProfileByIdAsync(string profileId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        if (string.IsNullOrWhiteSpace(profileId))
        {
            return Task.FromResult<IUIProfile?>(null);
        }

        lock (_lockObject)
        {
            _profiles.TryGetValue(profileId, out var profile);
            return Task.FromResult(profile);
        }
    }

    /// <inheritdoc />
    public Task<IEnumerable<IUIProfile>> GetProfilesByTypeAsync(UIProfileType profileType, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        lock (_lockObject)
        {
            var matchingProfiles = _profiles.Values
                .Where(p => p.ProfileType == profileType)
                .ToList();
            
            return Task.FromResult<IEnumerable<IUIProfile>>(matchingProfiles);
        }
    }

    /// <inheritdoc />
    public async Task<bool> SwitchToProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfNotRunning();
        
        if (string.IsNullOrWhiteSpace(profileId))
        {
            _logger.LogWarning("Cannot switch to profile with null or empty ID");
            return false;
        }

        var newProfile = await GetProfileByIdAsync(profileId, cancellationToken);
        if (newProfile == null)
        {
            _logger.LogWarning("Profile not found: {ProfileId}", profileId);
            return false;
        }

        if (!newProfile.IsEnabled)
        {
            _logger.LogWarning("Cannot switch to disabled profile: {ProfileName}", newProfile.Name);
            return false;
        }

        var previousProfile = ActiveProfile;
        
        try
        {
            // Deactivate current profile
            if (previousProfile != null && previousProfile.Id != profileId)
            {
                await previousProfile.DeactivateAsync(cancellationToken);
            }

            // Activate new profile
            if (previousProfile?.Id != profileId)
            {
                await newProfile.ActivateAsync(cancellationToken);
            }

            ActiveProfile = newProfile;
            
            // Raise profile changed event
            ProfileChanged?.Invoke(this, new UIProfileChangedEventArgs(previousProfile, newProfile));
            
            _logger.LogInformation("Successfully switched to profile: {ProfileName} ({ProfileType})", 
                newProfile.Name, newProfile.ProfileType);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to switch to profile: {ProfileName}", newProfile.Name);
            return false;
        }
    }

    /// <inheritdoc />
    public Task ReloadProfilesAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        _logger.LogInformation("Reloading UI profiles from configuration");
        
        // Load profiles from configuration or other sources
        // For now, we keep the default profiles that were loaded during initialization
        
        _logger.LogInformation("Reloaded {ProfileCount} UI profiles", _profiles.Count);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<Dictionary<string, UIProfileValidationResult>> ValidateAllProfilesAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        var results = new Dictionary<string, UIProfileValidationResult>();
        
        lock (_lockObject)
        {
            foreach (var profile in _profiles.Values)
            {
                try
                {
                    var validationResult = profile.ValidateAsync(cancellationToken).Result;
                    results[profile.Id] = validationResult;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating profile {ProfileName}", profile.Name);
                    results[profile.Id] = UIProfileValidationResult.Failure($"Validation exception: {ex.Message}");
                }
            }
        }
        
        return Task.FromResult(results);
    }

    #endregion

    #region Private Methods

    private Task LoadDefaultProfilesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Loading default UI profiles");
        
        // Create default profiles
        var profiles = new[]
        {
            CreateDefaultProfile(UIProfileType.CustomTUI, "default-tui", "Default TUI", 
                "Default Terminal User Interface profile optimized for console-based UI", true),
            CreateDefaultProfile(UIProfileType.Unity, "unity-default", "Unity Default", 
                "Unity-style UI profile with Unity-specific behaviors", false),
            CreateDefaultProfile(UIProfileType.Godot, "godot-default", "Godot Default", 
                "Godot-style UI profile with Godot-specific behaviors", false),
            CreateDefaultProfile(UIProfileType.Default, "basic-default", "Basic Default", 
                "Basic UI profile with minimal functionality", false)
        };

        foreach (var profile in profiles)
        {
            lock (_lockObject)
            {
                _profiles[profile.Id] = profile;
            }
        }
        
        _logger.LogDebug("Loaded {ProfileCount} default UI profiles", profiles.Length);
        return Task.CompletedTask;
    }

    private IUIProfile CreateDefaultProfile(UIProfileType profileType, string id, string name, string description, bool isDefault)
    {
        var configuration = new UIProfileConfiguration
        {
            Id = id,
            Name = name,
            Description = description,
            ProfileType = profileType,
            IsDefault = isDefault,
            IsEnabled = true
        };

        return profileType switch
        {
            UIProfileType.Unity => new UnityUIProfile(configuration, _logger),
            UIProfileType.Godot => new GodotUIProfile(configuration, _logger),
            UIProfileType.CustomTUI => new CustomTUIProfile(configuration, _logger),
            _ => new DefaultUIProfile(configuration, _logger)
        };
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(UIProfileService));
    }

    private void ThrowIfNotRunning()
    {
        ThrowIfDisposed();
        if (!_isRunning) throw new InvalidOperationException("UIProfileService is not running");
    }

    #endregion
}