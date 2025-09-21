using GameConsole.Core.Abstractions;
using GameConsole.UI.Profiles.Implementations;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace GameConsole.UI.Profiles;

/// <summary>
/// Main service for managing UI profiles, including registration, switching, and state management.
/// Implements state machine pattern for profile transitions with caching for performance.
/// </summary>
[Service("UI Profile Manager", "1.0.0", "Manages UI profiles for dynamic interface switching", 
    Categories = new[] { "UI", "Profile", "Management" }, Lifetime = ServiceLifetime.Singleton)]
public class UIProfileManager : IUIProfileManager, IService
{
    private readonly ILogger<UIProfileManager> _logger;
    private readonly ConcurrentDictionary<string, IUIProfile> _profiles;
    private readonly ConcurrentDictionary<string, string> _stateCache;
    private readonly object _activationLock = new();

    private IUIProfile? _activeProfile;
    private UIContext _currentContext;
    private bool _isRunning;

    /// <summary>
    /// Initializes a new instance of the UIProfileManager class.
    /// </summary>
    /// <param name="logger">Logger for this service.</param>
    public UIProfileManager(ILogger<UIProfileManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _profiles = new ConcurrentDictionary<string, IUIProfile>();
        _stateCache = new ConcurrentDictionary<string, string>();
        _currentContext = new UIContext(); // Default context
    }

    #region IUIProfileManager Implementation

    /// <inheritdoc />
    public IUIProfile? ActiveProfile => _activeProfile;

    /// <inheritdoc />
    public UIContext CurrentContext => _currentContext;

    /// <inheritdoc />
    public event EventHandler<UIProfileActivatedEventArgs>? ProfileActivated;

    /// <inheritdoc />
    public event EventHandler<UIProfileDeactivatedEventArgs>? ProfileDeactivated;

    /// <inheritdoc />
    public event EventHandler<UIProfileSwitchingEventArgs>? ProfileSwitching;

    /// <inheritdoc />
    public Task RegisterProfileAsync(IUIProfile profile, CancellationToken cancellationToken = default)
    {
        if (profile == null) throw new ArgumentNullException(nameof(profile));

        _logger.LogInformation("Registering UI profile: {ProfileId} ({ProfileName})", profile.Id, profile.Name);

        if (!_profiles.TryAdd(profile.Id, profile))
        {
            throw new InvalidOperationException($"Profile with ID '{profile.Id}' is already registered");
        }

        _logger.LogInformation("Successfully registered UI profile: {ProfileId}", profile.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task UnregisterProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profileId)) throw new ArgumentException("Profile ID cannot be null or empty", nameof(profileId));

        _logger.LogInformation("Unregistering UI profile: {ProfileId}", profileId);

        // If this is the active profile, deactivate it first
        if (_activeProfile?.Id == profileId)
        {
            await DeactivateCurrentProfileAsync("Profile unregistered", cancellationToken);
        }

        if (_profiles.TryRemove(profileId, out var removedProfile))
        {
            _logger.LogInformation("Successfully unregistered UI profile: {ProfileId}", profileId);
        }
        else
        {
            _logger.LogWarning("Attempted to unregister non-existent profile: {ProfileId}", profileId);
        }

        // Remove cached state for this profile
        _stateCache.TryRemove(profileId, out _);
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<IUIProfile>> GetRegisteredProfilesAsync(CancellationToken cancellationToken = default)
    {
        var profiles = _profiles.Values.ToList();
        _logger.LogTrace("Retrieved {Count} registered profiles", profiles.Count);
        return Task.FromResult<IReadOnlyCollection<IUIProfile>>(profiles);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<IUIProfile>> GetCompatibleProfilesAsync(UIMode mode, CancellationToken cancellationToken = default)
    {
        var compatibleProfiles = new List<IUIProfile>();

        foreach (var profile in _profiles.Values.Where(p => p.TargetMode == mode))
        {
            if (await profile.CanActivateAsync(_currentContext, cancellationToken))
            {
                compatibleProfiles.Add(profile);
            }
        }

        // Sort by priority (metadata priority)
        var sortedProfiles = compatibleProfiles
            .OrderByDescending(p => p.Metadata.Priority)
            .ThenBy(p => p.Name)
            .ToList();

        _logger.LogDebug("Found {Count} compatible profiles for mode {Mode}", sortedProfiles.Count, mode);
        return sortedProfiles;
    }

    /// <inheritdoc />
    public async Task SwitchProfileAsync(string profileId, ProfileSwitchOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profileId)) throw new ArgumentException("Profile ID cannot be null or empty", nameof(profileId));

        options ??= new ProfileSwitchOptions();

        if (!_profiles.TryGetValue(profileId, out var targetProfile))
        {
            throw new ArgumentException($"Profile with ID '{profileId}' not found", nameof(profileId));
        }

        await SwitchToProfileAsync(targetProfile, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SwitchToBestProfileAsync(UIMode mode, ProfileSwitchOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new ProfileSwitchOptions();

        var compatibleProfiles = await GetCompatibleProfilesAsync(mode, cancellationToken);
        var bestProfile = compatibleProfiles.FirstOrDefault();

        if (bestProfile == null)
        {
            throw new InvalidOperationException($"No compatible profiles found for mode {mode}");
        }

        _logger.LogInformation("Switching to best profile for mode {Mode}: {ProfileId}", mode, bestProfile.Id);
        await SwitchToProfileAsync(bestProfile, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateContextAsync(UIContext context, bool autoSwitch = false, CancellationToken cancellationToken = default)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        _logger.LogDebug("Updating UI context (autoSwitch: {AutoSwitch})", autoSwitch);

        var previousContext = _currentContext;
        _currentContext = context;

        if (autoSwitch && _activeProfile != null)
        {
            // Check if current profile is still compatible
            var stillCompatible = await _activeProfile.CanActivateAsync(context, cancellationToken);
            
            if (!stillCompatible)
            {
                _logger.LogInformation("Current profile {ProfileId} no longer compatible, searching for alternative", _activeProfile.Id);
                
                var compatibleProfiles = await GetCompatibleProfilesAsync(_activeProfile.TargetMode, cancellationToken);
                var alternativeProfile = compatibleProfiles.FirstOrDefault(p => p.Id != _activeProfile.Id);
                
                if (alternativeProfile != null)
                {
                    await SwitchToProfileAsync(alternativeProfile, new ProfileSwitchOptions(), cancellationToken);
                }
                else
                {
                    _logger.LogWarning("No alternative compatible profile found for mode {Mode}", _activeProfile.TargetMode);
                }
            }
        }

        _logger.LogDebug("UI context updated successfully");
    }

    /// <inheritdoc />
    public async Task<ProfileValidationResult> ValidateProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profileId)) throw new ArgumentException("Profile ID cannot be null or empty", nameof(profileId));

        if (!_profiles.TryGetValue(profileId, out var profile))
        {
            return new ProfileValidationResult
            {
                IsValid = false,
                Errors = new[] { $"Profile '{profileId}' not found" }
            };
        }

        var validator = new ProfileValidator(_logger);
        return await validator.ValidateAsync(profile, _currentContext, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IUIProfile?> GetProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profileId)) return Task.FromResult<IUIProfile?>(null);

        _profiles.TryGetValue(profileId, out var profile);
        return Task.FromResult<IUIProfile?>(profile);
    }

    /// <inheritdoc />
    public Task<string> SaveProfileStateAsync(CancellationToken cancellationToken = default)
    {
        var stateToken = Guid.NewGuid().ToString("N");
        
        var state = new ProfileManagerState
        {
            ActiveProfileId = _activeProfile?.Id,
            Context = _currentContext,
            SavedAt = DateTime.UtcNow
        };

        var stateJson = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = false });
        _stateCache.TryAdd(stateToken, stateJson);

        _logger.LogDebug("Saved profile state with token: {StateToken}", stateToken);
        return Task.FromResult(stateToken);
    }

    /// <inheritdoc />
    public async Task RestoreProfileStateAsync(string stateToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(stateToken)) throw new ArgumentException("State token cannot be null or empty", nameof(stateToken));

        if (!_stateCache.TryRemove(stateToken, out var stateJson))
        {
            throw new ArgumentException($"Invalid or expired state token: {stateToken}", nameof(stateToken));
        }

        var state = JsonSerializer.Deserialize<ProfileManagerState>(stateJson);
        if (state == null)
        {
            throw new InvalidOperationException("Failed to deserialize profile state");
        }

        _logger.LogDebug("Restoring profile state from token: {StateToken}", stateToken);

        // Restore context
        _currentContext = state.Context;

        // Restore active profile if specified
        if (!string.IsNullOrEmpty(state.ActiveProfileId))
        {
            await SwitchProfileAsync(state.ActiveProfileId, new ProfileSwitchOptions(), cancellationToken);
        }

        _logger.LogInformation("Successfully restored profile state");
    }

    #endregion

    #region IService Implementation

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing UI Profile Manager");

        // Register built-in profiles
        await RegisterBuiltInProfilesAsync(cancellationToken);

        _logger.LogInformation("UI Profile Manager initialized successfully");
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting UI Profile Manager");
        
        _isRunning = true;

        // Auto-select best profile based on current context
        await AutoSelectInitialProfileAsync(cancellationToken);

        _logger.LogInformation("UI Profile Manager started successfully");
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping UI Profile Manager");

        // Deactivate current profile
        if (_activeProfile != null)
        {
            await DeactivateCurrentProfileAsync("Service stopping", cancellationToken);
        }

        _isRunning = false;
        
        _logger.LogInformation("UI Profile Manager stopped successfully");
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isRunning)
        {
            await StopAsync();
        }

        // Clear all caches and profiles
        _profiles.Clear();
        _stateCache.Clear();

        _logger.LogDebug("UI Profile Manager disposed");
    }

    #endregion

    #region Private Methods

    private async Task RegisterBuiltInProfilesAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Registering built-in UI profiles");

        // Register console profile - use the main logger for simplicity
        var consoleProfile = new ConsoleUIProfile(_logger);
        await RegisterProfileAsync(consoleProfile, cancellationToken);

        // Register web profile
        var webProfile = new WebUIProfile(_logger);
        await RegisterProfileAsync(webProfile, cancellationToken);

        // Register desktop profile
        var desktopProfile = new DesktopUIProfile(_logger);
        await RegisterProfileAsync(desktopProfile, cancellationToken);

        _logger.LogInformation("Registered {Count} built-in UI profiles", 3);
    }

    private async Task AutoSelectInitialProfileAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Auto-selecting initial UI profile");

        // Determine best mode based on context
        var bestMode = DetermineBestUIMode(_currentContext);
        
        try
        {
            await SwitchToBestProfileAsync(bestMode, new ProfileSwitchOptions(), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to auto-select profile for mode {Mode}, falling back to console", bestMode);
            
            // Fallback to console profile
            if (bestMode != UIMode.Console)
            {
                try
                {
                    await SwitchToBestProfileAsync(UIMode.Console, new ProfileSwitchOptions(), cancellationToken);
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Failed to activate fallback console profile");
                }
            }
        }
    }

    private UIMode DetermineBestUIMode(UIContext context)
    {
        // Simple heuristics for mode selection
        if (!context.Display.HasGraphicalDisplay)
        {
            return UIMode.Console;
        }

        if (context.Display.Width >= 1024 && context.Display.Height >= 768)
        {
            return UIMode.Desktop;
        }

        if (context.Runtime.HasNetworkAccess)
        {
            return UIMode.Web;
        }

        return UIMode.Console;
    }

    private async Task SwitchToProfileAsync(IUIProfile targetProfile, ProfileSwitchOptions options, CancellationToken cancellationToken)
    {
        lock (_activationLock)
        {
            // Prevent concurrent profile switches
            if (_activeProfile?.Id == targetProfile.Id)
            {
                _logger.LogDebug("Profile {ProfileId} is already active", targetProfile.Id);
                return;
            }
        }

        // Raise switching event
        var switchingArgs = new UIProfileSwitchingEventArgs(_activeProfile, targetProfile, options);
        ProfileSwitching?.Invoke(this, switchingArgs);

        if (switchingArgs.Cancel)
        {
            _logger.LogInformation("Profile switch to {ProfileId} was cancelled", targetProfile.Id);
            return;
        }

        try
        {
            // Validate target profile if requested
            if (options.ValidateTarget)
            {
                var validationResult = await ValidateProfileAsync(targetProfile.Id, cancellationToken);
                if (!validationResult.IsValid)
                {
                    throw new InvalidOperationException($"Target profile validation failed: {string.Join(", ", validationResult.Errors)}");
                }
            }

            // Save current state if requested
            string? stateToken = null;
            if (options.PreserveState && _activeProfile != null)
            {
                stateToken = await SaveProfileStateAsync(cancellationToken);
            }

            // Deactivate current profile
            if (_activeProfile != null)
            {
                await DeactivateCurrentProfileAsync("Switching profiles", cancellationToken);
            }

            // Activate new profile
            await ActivateProfileAsync(targetProfile, cancellationToken);

            _logger.LogInformation("Successfully switched to UI profile: {ProfileId} ({ProfileName})", 
                targetProfile.Id, targetProfile.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to switch to UI profile: {ProfileId}", targetProfile.Id);
            throw;
        }
    }

    private async Task ActivateProfileAsync(IUIProfile profile, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Activating UI profile: {ProfileId}", profile.Id);

        await profile.ActivateAsync(_currentContext, cancellationToken);
        
        lock (_activationLock)
        {
            _activeProfile = profile;
        }

        // Raise activated event
        ProfileActivated?.Invoke(this, new UIProfileActivatedEventArgs(profile, _currentContext));
    }

    private async Task DeactivateCurrentProfileAsync(string reason, CancellationToken cancellationToken)
    {
        if (_activeProfile == null) return;

        _logger.LogDebug("Deactivating current UI profile: {ProfileId} (reason: {Reason})", _activeProfile.Id, reason);

        var profileToDeactivate = _activeProfile;
        
        lock (_activationLock)
        {
            _activeProfile = null;
        }

        try
        {
            await profileToDeactivate.DeactivateAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating profile {ProfileId}", profileToDeactivate.Id);
        }

        // Raise deactivated event
        ProfileDeactivated?.Invoke(this, new UIProfileDeactivatedEventArgs(profileToDeactivate, reason));
    }

    #endregion

    #region State Management

    /// <summary>
    /// Internal state representation for save/restore operations.
    /// </summary>
    private record ProfileManagerState
    {
        public string? ActiveProfileId { get; init; }
        public UIContext Context { get; init; } = new();
        public DateTime SavedAt { get; init; }
    }

    #endregion
}