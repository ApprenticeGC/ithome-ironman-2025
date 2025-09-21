using Microsoft.Extensions.Logging;

namespace GameConsole.UI.Profiles;

/// <summary>
/// Abstract base class for UI profile implementations.
/// </summary>
public abstract class BaseUIProfile : IUIProfile
{
    /// <summary>
    /// Logger for this profile.
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// Profile state information.
    /// </summary>
    protected ProfileState State { get; private set; } = new();

    /// <summary>
    /// Initializes a new instance of the BaseUIProfile class.
    /// </summary>
    /// <param name="logger">Logger for this profile.</param>
    protected BaseUIProfile(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public abstract string Id { get; }

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public abstract UIMode TargetMode { get; }

    /// <inheritdoc />
    public virtual UIProfileMetadata Metadata { get; protected set; } = new();

    /// <inheritdoc />
    public virtual async Task<bool> CanActivateAsync(UIContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogDebug("Checking if profile {ProfileId} can activate in context", Id);
            
            // Basic capability checks
            var requiredCapabilities = GetSupportedCapabilities();
            
            // Check platform compatibility
            if (!IsPlatformCompatible(context))
            {
                Logger.LogDebug("Profile {ProfileId} not compatible with platform {Platform}", Id, context.Platform);
                return false;
            }

            // Check display requirements
            if (!IsDisplayCompatible(context))
            {
                Logger.LogDebug("Profile {ProfileId} not compatible with display capabilities", Id);
                return false;
            }

            // Check resource requirements
            if (!AreResourcesAvailable(context))
            {
                Logger.LogDebug("Profile {ProfileId} resource requirements not met", Id);
                return false;
            }

            // Allow derived classes to add custom validation
            var customValidation = await OnCanActivateAsync(context, cancellationToken);
            
            Logger.LogDebug("Profile {ProfileId} activation check result: {CanActivate}", Id, customValidation);
            return customValidation;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking if profile {ProfileId} can activate", Id);
            return false;
        }
    }

    /// <inheritdoc />
    public virtual async Task ActivateAsync(UIContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogInformation("Activating UI profile {ProfileId} ({ProfileName})", Id, Name);
            
            State = State with { 
                IsActive = true, 
                ActivatedAt = DateTime.UtcNow,
                LastContext = context 
            };

            await OnActivateAsync(context, cancellationToken);
            
            Logger.LogInformation("Successfully activated UI profile {ProfileId}", Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to activate UI profile {ProfileId}", Id);
            State = State with { IsActive = false };
            throw;
        }
    }

    /// <inheritdoc />
    public virtual async Task DeactivateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogInformation("Deactivating UI profile {ProfileId}", Id);
            
            await OnDeactivateAsync(cancellationToken);
            
            State = State with { 
                IsActive = false, 
                DeactivatedAt = DateTime.UtcNow 
            };
            
            Logger.LogInformation("Successfully deactivated UI profile {ProfileId}", Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deactivating UI profile {ProfileId}", Id);
            throw;
        }
    }

    /// <inheritdoc />
    public abstract CommandSet GetCommandSet();

    /// <inheritdoc />
    public abstract LayoutConfiguration GetLayoutConfiguration();

    /// <inheritdoc />
    public abstract UICapabilities GetSupportedCapabilities();

    /// <summary>
    /// Checks if the profile is compatible with the target platform.
    /// </summary>
    /// <param name="context">The UI context to check.</param>
    /// <returns>True if compatible, false otherwise.</returns>
    protected virtual bool IsPlatformCompatible(UIContext context)
    {
        // Default implementation accepts all platforms
        return true;
    }

    /// <summary>
    /// Checks if the profile is compatible with display capabilities.
    /// </summary>
    /// <param name="context">The UI context to check.</param>
    /// <returns>True if compatible, false otherwise.</returns>
    protected virtual bool IsDisplayCompatible(UIContext context)
    {
        var capabilities = GetSupportedCapabilities();
        
        // If profile requires graphical elements, check display availability
        if (capabilities.HasFlag(UICapabilities.GraphicalElements))
        {
            return context.Display.HasGraphicalDisplay;
        }
        
        return true;
    }

    /// <summary>
    /// Checks if required system resources are available.
    /// </summary>
    /// <param name="context">The UI context to check.</param>
    /// <returns>True if resources are available, false otherwise.</returns>
    protected virtual bool AreResourcesAvailable(UIContext context)
    {
        // Check memory requirements
        var maxMemoryMB = context.User.Performance.MaxMemoryUsageMB;
        var availableMemoryMB = context.Runtime.Resources.AvailableMemoryMB;
        
        return availableMemoryMB >= maxMemoryMB;
    }

    /// <summary>
    /// Override this method to provide custom activation validation logic.
    /// </summary>
    /// <param name="context">The UI context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the profile can be activated, false otherwise.</returns>
    protected virtual Task<bool> OnCanActivateAsync(UIContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    /// <summary>
    /// Override this method to provide custom activation logic.
    /// </summary>
    /// <param name="context">The UI context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected virtual Task OnActivateAsync(UIContext context, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Override this method to provide custom deactivation logic.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected virtual Task OnDeactivateAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// State information for a UI profile.
/// </summary>
public record ProfileState
{
    /// <summary>
    /// Whether the profile is currently active.
    /// </summary>
    public bool IsActive { get; init; } = false;

    /// <summary>
    /// When the profile was last activated.
    /// </summary>
    public DateTime? ActivatedAt { get; init; }

    /// <summary>
    /// When the profile was last deactivated.
    /// </summary>
    public DateTime? DeactivatedAt { get; init; }

    /// <summary>
    /// The last context the profile was activated with.
    /// </summary>
    public UIContext? LastContext { get; init; }

    /// <summary>
    /// Number of times the profile has been activated.
    /// </summary>
    public int ActivationCount { get; init; } = 0;

    /// <summary>
    /// Additional state data.
    /// </summary>
    public IReadOnlyDictionary<string, object> Data { get; init; } = new Dictionary<string, object>();
}