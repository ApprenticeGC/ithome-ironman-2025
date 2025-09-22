using GameConsole.Core.Abstractions;
using GameConsole.Core.Registry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GameConsole.UI.Configuration;

/// <summary>
/// Service implementation for managing UI profile configurations.
/// </summary>
[Service(ServiceLifetime.Singleton, Categories = ["ui", "configuration"])]
public class ProfileConfigurationService : IProfileConfigurationService, IService
{
    private readonly ILogger<ProfileConfigurationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string, UIProfile> _profiles = new();
    private string? _activeProfileId;
    private bool _isRunning;
    private bool _disposed;

    public ProfileConfigurationService(
        ILogger<ProfileConfigurationService> logger,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    #region IService Implementation

    public bool IsRunning => _isRunning && !_disposed;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ProfileConfigurationService));
        
        _logger.LogInformation("Initializing ProfileConfigurationService");
        await LoadProfilesAsync(cancellationToken);
        _logger.LogInformation("Initialized ProfileConfigurationService with {ProfileCount} profiles", _profiles.Count);
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ProfileConfigurationService));
        
        _logger.LogInformation("Starting ProfileConfigurationService");
        _isRunning = true;
        _logger.LogInformation("Started ProfileConfigurationService");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping ProfileConfigurationService");
        _isRunning = false;
        _logger.LogInformation("Stopped ProfileConfigurationService");
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;
        
        _logger.LogDebug("Disposing ProfileConfigurationService");
        _disposed = true;
        return ValueTask.CompletedTask;
    }

    #endregion

    #region IProfileConfigurationService Implementation

    public Task<IEnumerable<IUIProfile>> GetAvailableProfilesAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return Task.FromResult(_profiles.Values.Cast<IUIProfile>());
    }

    public Task<IUIProfile?> GetActiveProfileAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        if (_activeProfileId == null)
            return Task.FromResult<IUIProfile?>(null);
            
        _profiles.TryGetValue(_activeProfileId, out var profile);
        return Task.FromResult<IUIProfile?>(profile);
    }

    public Task<IUIProfile?> GetProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profileId))
            throw new ArgumentException("Profile ID cannot be null or empty", nameof(profileId));
            
        ThrowIfDisposed();
        
        _profiles.TryGetValue(profileId, out var profile);
        return Task.FromResult<IUIProfile?>(profile);
    }

    public async Task<bool> ActivateProfileAsync(string profileId, IServiceRegistry serviceRegistry, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profileId))
            throw new ArgumentException("Profile ID cannot be null or empty", nameof(profileId));
        if (serviceRegistry == null)
            throw new ArgumentNullException(nameof(serviceRegistry));
            
        ThrowIfDisposed();
        ThrowIfNotRunning();
        
        if (!_profiles.TryGetValue(profileId, out var profile))
        {
            _logger.LogWarning("Profile '{ProfileId}' not found", profileId);
            return false;
        }
        
        var validationResult = await ValidateProfileAsync(profile, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Profile '{ProfileId}' validation failed: {Errors}", 
                profileId, string.Join(", ", validationResult.Errors));
            return false;
        }
        
        try
        {
            // Deactivate current profile
            if (_activeProfileId != null && _profiles.TryGetValue(_activeProfileId, out var currentProfile))
            {
                currentProfile.SetActive(false);
            }
            
            // Apply service configurations
            foreach (var (serviceType, implementationType) in profile.ServiceConfigurations)
            {
                _logger.LogDebug("Registering {ServiceType} -> {ImplementationType} for profile '{ProfileId}'",
                    serviceType.Name, implementationType.Name, profileId);
                    
                // Use scoped registration as the default for profile-based services
                serviceRegistry.RegisterScoped(serviceType, implementationType);
            }
            
            // Activate the new profile
            profile.SetActive(true);
            _activeProfileId = profileId;
            
            _logger.LogInformation("Successfully activated profile '{ProfileId}' ({ProfileName})", 
                profileId, profile.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to activate profile '{ProfileId}'", profileId);
            return false;
        }
    }

    public Task<ProfileValidationResult> ValidateProfileAsync(IUIProfile profile, CancellationToken cancellationToken = default)
    {
        if (profile == null)
            throw new ArgumentNullException(nameof(profile));
            
        ThrowIfDisposed();
        
        var errors = new List<string>();
        var warnings = new List<string>();
        
        // Validate basic profile properties
        if (string.IsNullOrWhiteSpace(profile.Id))
            errors.Add("Profile ID is required");
        if (string.IsNullOrWhiteSpace(profile.Name))
            errors.Add("Profile name is required");
            
        // Validate service configurations
        foreach (var (serviceType, implementationType) in profile.ServiceConfigurations)
        {
            if (!serviceType.IsInterface && !serviceType.IsAbstract)
            {
                warnings.Add($"Service type {serviceType.Name} is not an interface or abstract class");
            }
            
            if (!serviceType.IsAssignableFrom(implementationType))
            {
                errors.Add($"Implementation type {implementationType.Name} does not implement service type {serviceType.Name}");
            }
        }
        
        // Validate capabilities
        foreach (var capabilityType in profile.EnabledCapabilities)
        {
            if (!capabilityType.IsInterface)
            {
                warnings.Add($"Capability type {capabilityType.Name} is not an interface");
            }
        }
        
        bool isValid = errors.Count == 0;
        return Task.FromResult(isValid 
            ? ProfileValidationResult.Success(warnings) 
            : ProfileValidationResult.Failure(errors, warnings));
    }

    public async Task LoadProfilesAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        _logger.LogDebug("Loading UI profiles from configuration");
        
        _profiles.Clear();
        
        // Load default profiles
        LoadDefaultProfiles();
        
        // TODO: Load profiles from configuration files
        // This would read from appsettings.json or dedicated profile configuration files
        
        _logger.LogInformation("Loaded {ProfileCount} UI profiles", _profiles.Count);
    }

    #endregion

    #region Private Methods

    private void LoadDefaultProfiles()
    {
        // Console Profile - minimal TUI-only mode
        var consoleProfile = new UIProfile(
            id: "console",
            name: "Console Mode",
            description: "Minimal TUI-only mode for console applications",
            serviceConfigurations: new Dictionary<Type, Type>(),
            enabledCapabilities: new HashSet<Type>(),
            settings: new Dictionary<string, object>
            {
                ["ui.mode"] = "console",
                ["graphics.enabled"] = false,
                ["audio.enabled"] = false
            });

        _profiles[consoleProfile.Id] = consoleProfile;

        // TODO: Add Unity-like and Godot-like profiles when the corresponding services are available
        _logger.LogDebug("Loaded default profiles: {ProfileIds}", string.Join(", ", _profiles.Keys));
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ProfileConfigurationService));
    }
    
    private void ThrowIfNotRunning()
    {
        ThrowIfDisposed();
        if (!_isRunning) throw new InvalidOperationException("ProfileConfigurationService is not running");
    }

    #endregion
}