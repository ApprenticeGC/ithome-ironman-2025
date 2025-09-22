using GameConsole.Core.Abstractions;
using GameConsole.UI.Profiles.Implementations;

namespace GameConsole.UI.Profiles;

/// <summary>
/// Interface for the UI Profile Manager service.
/// </summary>
public interface IUIProfileManager : IService
{
    /// <summary>
    /// Gets the currently active profile.
    /// </summary>
    UIProfile? ActiveProfile { get; }
    
    /// <summary>
    /// Gets all available UI profiles.
    /// </summary>
    IReadOnlyCollection<UIProfile> AvailableProfiles { get; }
    
    /// <summary>
    /// Activates a profile based on the given context.
    /// </summary>
    /// <param name="context">The context for profile selection.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async activation operation.</returns>
    Task ActivateProfileAsync(UIProfileContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Registers a UI profile with the manager.
    /// </summary>
    /// <param name="profile">The profile to register.</param>
    void RegisterProfile(UIProfile profile);
    
    /// <summary>
    /// Gets the best matching profile for the given context.
    /// </summary>
    /// <param name="context">The context to match against.</param>
    /// <returns>The best matching profile, or null if none found.</returns>
    UIProfile? GetBestMatchingProfile(UIProfileContext context);
    
    /// <summary>
    /// Event raised when the active profile changes.
    /// </summary>
    event EventHandler<ProfileChangedEventArgs>? ProfileChanged;
}

/// <summary>
/// Event arguments for profile change events.
/// </summary>
public class ProfileChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the previously active profile.
    /// </summary>
    public UIProfile? PreviousProfile { get; }
    
    /// <summary>
    /// Gets the newly active profile.
    /// </summary>
    public UIProfile? NewProfile { get; }
    
    /// <summary>
    /// Gets the context that triggered the profile change.
    /// </summary>
    public UIProfileContext Context { get; }
    
    /// <summary>
    /// Initializes a new instance of the ProfileChangedEventArgs class.
    /// </summary>
    /// <param name="previousProfile">The previously active profile.</param>
    /// <param name="newProfile">The newly active profile.</param>
    /// <param name="context">The context that triggered the change.</param>
    public ProfileChangedEventArgs(UIProfile? previousProfile, UIProfile? newProfile, UIProfileContext context)
    {
        PreviousProfile = previousProfile;
        NewProfile = newProfile;
        Context = context;
    }
}

/// <summary>
/// Default implementation of the UI Profile Manager.
/// Handles profile detection, loading, and dynamic profile switching.
/// </summary>
public class UIProfileManager : IUIProfileManager
{
    private readonly List<UIProfile> _profiles = new();
    private UIProfile? _activeProfile;
    private bool _isRunning;

    /// <summary>
    /// Initializes a new instance of the UIProfileManager class.
    /// </summary>
    public UIProfileManager()
    {
        // Register built-in profiles
        RegisterProfile(new GameModeProfile());
        RegisterProfile(new EditorModeProfile());
    }

    /// <inheritdoc />
    public UIProfile? ActiveProfile => _activeProfile;

    /// <inheritdoc />
    public IReadOnlyCollection<UIProfile> AvailableProfiles => _profiles.AsReadOnly();

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public event EventHandler<ProfileChangedEventArgs>? ProfileChanged;

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Initialization logic can be added here if needed
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isRunning)
        {
            await StopAsync();
        }
        
        if (_activeProfile != null)
        {
            var context = new UIProfileContext { Mode = _activeProfile.TargetMode };
            await _activeProfile.OnDeactivateAsync(context);
        }
        
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public async Task ActivateProfileAsync(UIProfileContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var bestProfile = GetBestMatchingProfile(context);
        if (bestProfile == null)
        {
            throw new InvalidOperationException($"No suitable profile found for context: Mode={context.Mode}");
        }

        if (_activeProfile == bestProfile)
        {
            // Profile is already active, no need to change
            return;
        }

        var previousProfile = _activeProfile;

        // Deactivate current profile
        if (_activeProfile != null)
        {
            await _activeProfile.OnDeactivateAsync(context);
        }

        // Activate new profile
        _activeProfile = bestProfile;
        await _activeProfile.OnActivateAsync(context);

        // Notify listeners
        ProfileChanged?.Invoke(this, new ProfileChangedEventArgs(previousProfile, _activeProfile, context));
    }

    /// <inheritdoc />
    public void RegisterProfile(UIProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        
        if (_profiles.Any(p => p.Name == profile.Name))
        {
            throw new InvalidOperationException($"A profile with name '{profile.Name}' is already registered");
        }
        
        _profiles.Add(profile);
    }

    /// <inheritdoc />
    public UIProfile? GetBestMatchingProfile(UIProfileContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var applicableProfiles = _profiles
            .Where(p => p.Metadata.IsEnabled && p.IsApplicable(context))
            .OrderByDescending(p => p.Metadata.Priority)
            .ToList();

        return applicableProfiles.FirstOrDefault();
    }
}