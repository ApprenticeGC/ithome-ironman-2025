using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.Configuration.Security;

/// <summary>
/// UI profile service for managing UI configuration profiles.
/// Supports creating, switching, and persisting UI profiles with themes, layout modes, and settings.
/// </summary>
[Service("UI Profile Configuration", "1.0.0", "Manages UI profile configurations including themes, layouts, and accessibility settings", 
    Categories = new[] { "UI", "Configuration", "Profile" }, Lifetime = ServiceLifetime.Singleton)]
public class UIProfileService : IService, IUIProfileCapability
{
    private readonly ConcurrentDictionary<string, UIProfileConfiguration> _profiles;
    private UIProfileConfiguration _activeProfile;
    private readonly string _defaultProfileName = "Default";
    private readonly IConfiguration? _configuration;
    private readonly ILogger<UIProfileService> _logger;
    private bool _isRunning = false;

    public UIProfileService(ILogger<UIProfileService> logger, IConfiguration? configuration = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _profiles = new ConcurrentDictionary<string, UIProfileConfiguration>();
        _configuration = configuration;
        
        // Create default profile
        _activeProfile = CreateDefaultProfile();
        _profiles[_defaultProfileName] = _activeProfile;
    }

    public bool IsRunning => _isRunning;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Initializing UI profile service");
        
        // Load any additional profiles from configuration if available
        await LoadProfilesFromConfigurationAsync(cancellationToken);
        
        _logger.LogInformation("Initialized UI profile service with {ProfileCount} profiles", _profiles.Count);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting UI profile service");
        
        // Load saved profiles if they exist
        await LoadProfilesAsync(cancellationToken);
        
        _isRunning = true;
        _logger.LogInformation("Started UI profile service");
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Stopping UI profile service");
        
        // Save current profiles
        await SaveProfilesAsync(cancellationToken);
        
        _isRunning = false;
        _logger.LogInformation("Stopped UI profile service");
    }

    public async ValueTask DisposeAsync()
    {
        if (_isRunning)
        {
            await StopAsync();
        }
    }

    #region IUIProfileCapability Implementation

    public Task<UIProfileConfiguration> GetUIProfileConfigurationAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Retrieved current UI profile configuration: {ProfileName}", _activeProfile.ProfileName);
        return Task.FromResult(_activeProfile);
    }

    public Task SaveUIProfileConfigurationAsync(UIProfileConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        _profiles[configuration.ProfileName] = configuration;
        _activeProfile = configuration;
        
        _logger.LogInformation("Saved UI profile configuration: {ProfileName}", configuration.ProfileName);
        
        return Task.CompletedTask;
    }

    public Task<UIProfileConfiguration> CreateUIProfileAsync(string profileName, UITheme theme = UITheme.Default, 
        UILayoutMode layoutMode = UILayoutMode.Auto, string? basedOnProfile = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profileName))
            throw new ArgumentException("Profile name cannot be null or empty", nameof(profileName));

        if (_profiles.ContainsKey(profileName))
            throw new ArgumentException($"Profile '{profileName}' already exists", nameof(profileName));

        Dictionary<string, string>? baseSettings = null;
        
        if (!string.IsNullOrEmpty(basedOnProfile) && _profiles.TryGetValue(basedOnProfile, out var baseProfile))
        {
            baseSettings = baseProfile.Settings.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        
        var newProfile = new UIProfileConfiguration(profileName, theme, layoutMode, baseSettings);
        
        // Apply theme-specific defaults if no base profile
        if (baseSettings == null)
        {
            ApplyThemeDefaults(newProfile, theme);
            ApplyLayoutDefaults(newProfile, layoutMode);
        }
        
        _profiles[profileName] = newProfile;
        
        _logger.LogInformation("Created new UI profile: {ProfileName} with theme {Theme} and layout {LayoutMode}", 
            profileName, theme, layoutMode);
        
        return Task.FromResult(newProfile);
    }

    public Task SwitchUIProfileAsync(string profileName, CancellationToken cancellationToken = default)
    {
        if (!_profiles.TryGetValue(profileName, out var profile))
        {
            throw new ArgumentException($"UI profile '{profileName}' not found", nameof(profileName));
        }

        _activeProfile = profile;
        
        _logger.LogInformation("Switched to UI profile: {ProfileName}", profileName);
        
        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> GetAvailableUIProfilesAsync(CancellationToken cancellationToken = default)
    {
        var profiles = _profiles.Keys.ToList();
        
        _logger.LogTrace("Retrieved {Count} available UI profiles", profiles.Count);
        
        return Task.FromResult<IEnumerable<string>>(profiles);
    }

    public Task DeleteUIProfileAsync(string profileName, CancellationToken cancellationToken = default)
    {
        if (profileName == _defaultProfileName)
            throw new ArgumentException("Cannot delete the default profile", nameof(profileName));
        
        if (!_profiles.TryRemove(profileName, out _))
        {
            throw new ArgumentException($"UI profile '{profileName}' not found", nameof(profileName));
        }
        
        // Switch to default if we just deleted the active profile
        if (_activeProfile.ProfileName == profileName)
        {
            _activeProfile = _profiles[_defaultProfileName];
        }
        
        _logger.LogInformation("Deleted UI profile: {ProfileName}", profileName);
        
        return Task.CompletedTask;
    }

    public Task UpdateUISettingAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Setting key cannot be null or empty", nameof(key));
        
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        _activeProfile.SetSetting(key, value);
        
        _logger.LogTrace("Updated UI setting {Key} = {Value} in profile {ProfileName}", key, value, _activeProfile.ProfileName);
        
        return Task.CompletedTask;
    }

    public Task<string?> GetUISettingAsync(string key, CancellationToken cancellationToken = default)
    {
        var value = _activeProfile.GetSetting(key);
        
        _logger.LogTrace("Retrieved UI setting {Key} = {Value} from profile {ProfileName}", key, value ?? "null", _activeProfile.ProfileName);
        
        return Task.FromResult(value);
    }

    public Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var capabilities = new[] { typeof(IUIProfileCapability) };
        return Task.FromResult<IEnumerable<Type>>(capabilities);
    }

    public Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        var hasCapability = typeof(T) == typeof(IUIProfileCapability);
        return Task.FromResult(hasCapability);
    }

    public Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (typeof(T) == typeof(IUIProfileCapability))
        {
            return Task.FromResult(this as T);
        }
        return Task.FromResult<T?>(null);
    }

    #endregion

    #region Private Methods

    private UIProfileConfiguration CreateDefaultProfile()
    {
        var defaultSettings = new Dictionary<string, string>
        {
            [UISettingKeys.FontFamily] = "Consolas, Monaco, monospace",
            [UISettingKeys.FontSize] = "12",
            [UISettingKeys.AnimationsEnabled] = "true",
            [UISettingKeys.SoundEffectsEnabled] = "true",
            [UISettingKeys.ScreenReaderEnabled] = "false",
            [UISettingKeys.UIProvider] = "Auto",
            [UISettingKeys.ColorScheme] = "Default",
            [UISettingKeys.WindowOpacity] = "1.0"
        };
        
        return new UIProfileConfiguration(_defaultProfileName, UITheme.Default, UILayoutMode.Auto, defaultSettings);
    }

    private void ApplyThemeDefaults(UIProfileConfiguration profile, UITheme theme)
    {
        switch (theme)
        {
            case UITheme.Dark:
                profile.SetSetting(UISettingKeys.ColorScheme, "Dark");
                profile.SetSetting("UI.BackgroundColor", "#1e1e1e");
                profile.SetSetting("UI.TextColor", "#ffffff");
                break;
                
            case UITheme.Light:
                profile.SetSetting(UISettingKeys.ColorScheme, "Light");
                profile.SetSetting("UI.BackgroundColor", "#ffffff");
                profile.SetSetting("UI.TextColor", "#000000");
                break;
                
            case UITheme.HighContrast:
                profile.SetSetting(UISettingKeys.ColorScheme, "HighContrast");
                profile.SetSetting("UI.BackgroundColor", "#000000");
                profile.SetSetting("UI.TextColor", "#ffffff");
                profile.SetSetting(UISettingKeys.FontSize, "14");
                break;
        }
    }

    private void ApplyLayoutDefaults(UIProfileConfiguration profile, UILayoutMode layoutMode)
    {
        switch (layoutMode)
        {
            case UILayoutMode.TUI:
                profile.SetSetting(UISettingKeys.UIProvider, "TUI");
                profile.SetSetting(UISettingKeys.AnimationsEnabled, "false");
                break;
                
            case UILayoutMode.GUI:
                profile.SetSetting(UISettingKeys.UIProvider, "GUI");
                profile.SetSetting(UISettingKeys.AnimationsEnabled, "true");
                break;
                
            case UILayoutMode.Hybrid:
                profile.SetSetting(UISettingKeys.UIProvider, "Hybrid");
                profile.SetSetting(UISettingKeys.AnimationsEnabled, "true");
                break;
        }
    }

    private async Task LoadProfilesFromConfigurationAsync(CancellationToken cancellationToken = default)
    {
        if (_configuration == null)
            return;
            
        try
        {
            // Load additional profiles from configuration if available
            var profilesSection = _configuration.GetSection("UIProfiles");
            if (profilesSection.Exists())
            {
                _logger.LogDebug("Loading UI profiles from configuration");
                // Configuration loading logic would go here
                await Task.Delay(10, cancellationToken); // Simulate async work
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load UI profiles from configuration");
        }
    }

    private async Task LoadProfilesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // In a real implementation, this would load from a file or database
            _logger.LogDebug("Loading UI profiles from storage");
            
            await Task.Delay(10, cancellationToken); // Simulate I/O delay
            
            _logger.LogDebug("Loaded {Count} UI profiles", _profiles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load UI profiles");
        }
    }

    private async Task SaveProfilesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // In a real implementation, this would save to a file or database
            _logger.LogDebug("Saving UI profiles to storage");
            
            await Task.Delay(10, cancellationToken); // Simulate I/O delay
            
            _logger.LogDebug("Saved {Count} UI profiles", _profiles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save UI profiles");
        }
    }

    #endregion
}