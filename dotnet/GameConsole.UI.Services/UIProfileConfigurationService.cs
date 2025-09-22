using GameConsole.Core.Abstractions;
using GameConsole.UI.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace GameConsole.UI.Services;

/// <summary>
/// UI Profile Configuration Service for managing UI profiles including loading, saving, and switching between profiles.
/// Provides persistence, validation, and event notifications for UI profile changes.
/// </summary>
[Service("UI Profile Configuration", "1.0.0", "Manages UI profiles with configuration persistence and validation", 
    Categories = new[] { "UI", "Configuration", "Profile" }, Lifetime = ServiceLifetime.Singleton)]
public class UIProfileConfigurationService : BaseUIService, IUIProfileCapability, IUIProfileEvents
{
    private readonly IConfiguration _configuration;
    private readonly ConcurrentDictionary<string, UIProfile> _profiles;
    private string _activeProfileId = string.Empty;
    private readonly string _configurationDirectory;
    private readonly JsonSerializerOptions _jsonOptions;

    // Built-in profiles
    private static readonly UIProfile[] BuiltInProfiles = new[]
    {
        new UIProfile
        {
            Id = "tui-default",
            Name = "TUI Default",
            Description = "Default Terminal User Interface profile optimized for console applications",
            Mode = "TUI",
            IsBuiltIn = true,
            Settings = new UIProfileSettings
            {
                Theme = new UIThemeSettings
                {
                    ColorScheme = "Default",
                    FontFamily = "Consolas",
                    FontSize = 14,
                    DarkMode = true
                },
                Layout = new UILayoutSettings
                {
                    Width = "100%",
                    Height = "100%",
                    Padding = 4,
                    ShowBorders = true
                },
                Input = new UIInputSettings
                {
                    PreferredInput = "Keyboard",
                    KeyboardShortcuts = true,
                    MouseEnabled = false
                },
                Rendering = new UIRenderingSettings
                {
                    MaxFPS = 30,
                    UseHardwareAcceleration = false,
                    Quality = "Medium"
                }
            }
        },
        new UIProfile
        {
            Id = "gui-default", 
            Name = "GUI Default",
            Description = "Default Graphical User Interface profile for windowed applications",
            Mode = "GUI",
            IsBuiltIn = true,
            Settings = new UIProfileSettings
            {
                Theme = new UIThemeSettings
                {
                    ColorScheme = "Windows",
                    FontFamily = "Segoe UI",
                    FontSize = 12,
                    DarkMode = false
                },
                Layout = new UILayoutSettings
                {
                    Width = "1024px",
                    Height = "768px", 
                    Padding = 8,
                    ShowBorders = true
                },
                Input = new UIInputSettings
                {
                    PreferredInput = "Mouse",
                    KeyboardShortcuts = true,
                    MouseEnabled = true
                },
                Rendering = new UIRenderingSettings
                {
                    MaxFPS = 60,
                    UseHardwareAcceleration = true,
                    Quality = "High"
                }
            }
        }
    };

    public UIProfileConfigurationService(ILogger<UIProfileConfigurationService> logger, IConfiguration configuration) 
        : base(logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _profiles = new ConcurrentDictionary<string, UIProfile>();
        _configurationDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GameConsole", "UI", "Profiles");
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    #region BaseUIService Overrides

    public override IUIProfileCapability ProfileManager => this;

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Initializing UI profile configuration service");
        
        // Ensure configuration directory exists
        Directory.CreateDirectory(_configurationDirectory);
        
        // Load built-in profiles
        foreach (var profile in BuiltInProfiles)
        {
            _profiles.TryAdd(profile.Id, profile);
        }
        
        // Load user profiles from disk
        await LoadUserProfilesAsync(cancellationToken);
        
        // Set default active profile if none is set
        if (string.IsNullOrEmpty(_activeProfileId) && _profiles.Any())
        {
            _activeProfileId = "tui-default";
        }
    }

    public override async Task<bool> ApplyProfileAsync(UIProfile profile, CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        
        var validationResult = await ValidateProfileAsync(profile, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Cannot apply invalid profile {ProfileId}: {Errors}", 
                profile.Id, string.Join(", ", validationResult.Errors));
            return false;
        }
        
        _logger.LogInformation("Applying UI profile {ProfileId} ({ProfileName})", profile.Id, profile.Name);
        
        // In a real implementation, this would configure the UI system
        // For now, we just log the application
        _logger.LogDebug("Profile settings: Mode={Mode}, Theme={Theme}, Layout={Layout}", 
            profile.Mode, profile.Settings.Theme.ColorScheme, 
            $"{profile.Settings.Layout.Width}x{profile.Settings.Layout.Height}");
        
        return true;
    }

    public override Task<string> GetCurrentModeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        
        if (_profiles.TryGetValue(_activeProfileId, out var activeProfile))
        {
            return Task.FromResult(activeProfile.Mode);
        }
        
        return Task.FromResult("Unknown");
    }

    public override Task<IEnumerable<string>> GetSupportedModesAsync(CancellationToken cancellationToken = default)
    {
        var modes = new[] { "TUI", "GUI", "Mixed" };
        return Task.FromResult<IEnumerable<string>>(modes);
    }

    #endregion

    #region IUIProfileCapability Implementation

    public Task<IEnumerable<UIProfile>> GetAllProfilesAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        return Task.FromResult<IEnumerable<UIProfile>>(_profiles.Values.ToArray());
    }

    public Task<UIProfile?> GetProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        _profiles.TryGetValue(profileId, out var profile);
        return Task.FromResult(profile);
    }

    public Task<UIProfile?> GetActiveProfileAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        
        if (string.IsNullOrEmpty(_activeProfileId))
            return Task.FromResult<UIProfile?>(null);
        
        _profiles.TryGetValue(_activeProfileId, out var profile);
        return Task.FromResult(profile);
    }

    public async Task<bool> CreateProfileAsync(UIProfile profile, CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        
        var validationResult = await ValidateProfileAsync(profile, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Cannot create invalid profile {ProfileId}: {Errors}", 
                profile.Id, string.Join(", ", validationResult.Errors));
            return false;
        }
        
        if (_profiles.ContainsKey(profile.Id))
        {
            _logger.LogWarning("Profile {ProfileId} already exists", profile.Id);
            return false;
        }
        
        var profileToStore = profile with { 
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
            IsBuiltIn = false
        };
        
        _profiles.TryAdd(profile.Id, profileToStore);
        
        // Save to disk if not built-in
        await SaveProfileToDiskAsync(profileToStore, cancellationToken);
        
        _logger.LogInformation("Created UI profile {ProfileId} ({ProfileName})", profile.Id, profile.Name);
        ProfileCreated?.Invoke(this, new UIProfileChangedEventArgs(profileToStore, UIProfileChangeType.Created));
        
        return true;
    }

    public async Task<bool> UpdateProfileAsync(UIProfile profile, CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        
        if (!_profiles.TryGetValue(profile.Id, out var existingProfile))
        {
            _logger.LogWarning("Profile {ProfileId} not found for update", profile.Id);
            return false;
        }
        
        if (existingProfile.IsBuiltIn)
        {
            _logger.LogWarning("Cannot update built-in profile {ProfileId}", profile.Id);
            return false;
        }
        
        var validationResult = await ValidateProfileAsync(profile, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Cannot update to invalid profile {ProfileId}: {Errors}", 
                profile.Id, string.Join(", ", validationResult.Errors));
            return false;
        }
        
        var updatedProfile = profile with { 
            ModifiedAt = DateTimeOffset.UtcNow,
            CreatedAt = existingProfile.CreatedAt
        };
        
        _profiles.TryUpdate(profile.Id, updatedProfile, existingProfile);
        
        // Save to disk
        await SaveProfileToDiskAsync(updatedProfile, cancellationToken);
        
        _logger.LogInformation("Updated UI profile {ProfileId} ({ProfileName})", profile.Id, profile.Name);
        ProfileUpdated?.Invoke(this, new UIProfileChangedEventArgs(updatedProfile, UIProfileChangeType.Updated));
        
        return true;
    }

    public async Task<bool> DeleteProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        
        if (!_profiles.TryGetValue(profileId, out var profile))
        {
            _logger.LogWarning("Profile {ProfileId} not found for deletion", profileId);
            return false;
        }
        
        if (profile.IsBuiltIn)
        {
            _logger.LogWarning("Cannot delete built-in profile {ProfileId}", profileId);
            return false;
        }
        
        if (profileId == _activeProfileId)
        {
            _logger.LogWarning("Cannot delete active profile {ProfileId}", profileId);
            return false;
        }
        
        if (_profiles.TryRemove(profileId, out var removedProfile))
        {
            // Delete from disk
            await DeleteProfileFromDiskAsync(profileId, cancellationToken);
            
            _logger.LogInformation("Deleted UI profile {ProfileId} ({ProfileName})", profileId, removedProfile.Name);
            ProfileDeleted?.Invoke(this, new UIProfileChangedEventArgs(removedProfile, UIProfileChangeType.Deleted));
            
            return true;
        }
        
        return false;
    }

    public async Task<bool> ActivateProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        
        if (!_profiles.TryGetValue(profileId, out var profile))
        {
            _logger.LogWarning("Profile {ProfileId} not found for activation", profileId);
            return false;
        }
        
        _activeProfileId = profileId;
        
        // Apply the profile
        await ApplyProfileAsync(profile, cancellationToken);
        
        _logger.LogInformation("Activated UI profile {ProfileId} ({ProfileName})", profileId, profile.Name);
        ProfileActivated?.Invoke(this, new UIProfileChangedEventArgs(profile, UIProfileChangeType.Activated));
        
        return true;
    }

    public Task<UIProfileValidationResult> ValidateProfileAsync(UIProfile profile, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        
        // Basic validation
        if (string.IsNullOrWhiteSpace(profile.Id))
            errors.Add("Profile ID cannot be empty");
        
        if (string.IsNullOrWhiteSpace(profile.Name))
            errors.Add("Profile name cannot be empty");
        
        if (string.IsNullOrWhiteSpace(profile.Mode))
            errors.Add("Profile mode cannot be empty");
        
        // Settings validation
        if (profile.Settings.Theme.FontSize < 6 || profile.Settings.Theme.FontSize > 72)
            warnings.Add("Font size should be between 6 and 72 pixels");
        
        if (profile.Settings.Rendering.MaxFPS < 1 || profile.Settings.Rendering.MaxFPS > 240)
            warnings.Add("Max FPS should be between 1 and 240");
        
        if (errors.Any())
        {
            return Task.FromResult(UIProfileValidationResult.Failed(errors.ToArray()));
        }
        
        if (warnings.Any())
        {
            return Task.FromResult(UIProfileValidationResult.WithWarnings(warnings.ToArray()));
        }
        
        return Task.FromResult(UIProfileValidationResult.Success());
    }

    #endregion

    #region IUIProfileEvents Implementation

    public event EventHandler<UIProfileChangedEventArgs>? ProfileCreated;
    public event EventHandler<UIProfileChangedEventArgs>? ProfileUpdated;
    public event EventHandler<UIProfileChangedEventArgs>? ProfileDeleted;
    public event EventHandler<UIProfileChangedEventArgs>? ProfileActivated;

    #endregion

    #region Private Methods

    private async Task LoadUserProfilesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var profileFiles = Directory.GetFiles(_configurationDirectory, "*.json");
            
            foreach (var file in profileFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (BuiltInProfiles.Any(p => p.Id == fileName))
                    continue; // Skip built-in profiles
                
                try
                {
                    var json = await File.ReadAllTextAsync(file, cancellationToken);
                    var profile = JsonSerializer.Deserialize<UIProfile>(json, _jsonOptions);
                    
                    if (profile != null && !string.IsNullOrEmpty(profile.Id))
                    {
                        _profiles.TryAdd(profile.Id, profile);
                        _logger.LogDebug("Loaded UI profile {ProfileId} from {File}", profile.Id, file);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load UI profile from {File}", file);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load user profiles from {Directory}", _configurationDirectory);
        }
    }

    private async Task SaveProfileToDiskAsync(UIProfile profile, CancellationToken cancellationToken = default)
    {
        if (profile.IsBuiltIn)
            return;
        
        try
        {
            var filePath = Path.Combine(_configurationDirectory, $"{profile.Id}.json");
            var json = JsonSerializer.Serialize(profile, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json, cancellationToken);
            
            _logger.LogDebug("Saved UI profile {ProfileId} to {File}", profile.Id, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save UI profile {ProfileId} to disk", profile.Id);
        }
    }

    private Task DeleteProfileFromDiskAsync(string profileId, CancellationToken cancellationToken = default)
    {
        try
        {
            var filePath = Path.Combine(_configurationDirectory, $"{profileId}.json");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogDebug("Deleted UI profile file {File}", filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete UI profile {ProfileId} from disk", profileId);
        }
        
        return Task.CompletedTask;
    }

    #endregion
}