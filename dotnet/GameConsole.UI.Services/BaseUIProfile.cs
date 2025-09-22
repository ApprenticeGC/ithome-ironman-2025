using GameConsole.UI.Core;
using Microsoft.Extensions.Logging;

namespace GameConsole.UI.Services;

/// <summary>
/// Base implementation for UI profiles providing common functionality.
/// </summary>
public abstract class BaseUIProfile : IUIProfile
{
    protected readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the BaseUIProfile class.
    /// </summary>
    /// <param name="configuration">The profile configuration.</param>
    /// <param name="logger">The logger instance.</param>
    protected BaseUIProfile(UIProfileConfiguration configuration, ILogger logger)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region IUIProfile Implementation

    /// <inheritdoc />
    public UIProfileConfiguration Configuration { get; }

    /// <inheritdoc />
    public string Id => Configuration.Id;

    /// <inheritdoc />
    public string Name => Configuration.Name;

    /// <inheritdoc />
    public UIProfileType ProfileType => Configuration.ProfileType;

    /// <inheritdoc />
    public bool IsEnabled => Configuration.IsEnabled;

    /// <inheritdoc />
    public virtual async Task ActivateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Activating UI profile: {ProfileName} ({ProfileType})", Name, ProfileType);
        
        if (!IsEnabled)
        {
            throw new InvalidOperationException($"Cannot activate disabled profile: {Name}");
        }

        var validationResult = await ValidateAsync(cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException($"Profile validation failed: {string.Join(", ", validationResult.Errors)}");
        }

        await OnActivateAsync(cancellationToken);
        _logger.LogInformation("Successfully activated UI profile: {ProfileName}", Name);
    }

    /// <inheritdoc />
    public virtual async Task DeactivateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deactivating UI profile: {ProfileName} ({ProfileType})", Name, ProfileType);
        await OnDeactivateAsync(cancellationToken);
        _logger.LogInformation("Successfully deactivated UI profile: {ProfileName}", Name);
    }

    /// <inheritdoc />
    public virtual Task<UIProfileValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Configuration.Id))
        {
            errors.Add("Profile ID cannot be null or empty");
        }

        if (string.IsNullOrWhiteSpace(Configuration.Name))
        {
            errors.Add("Profile Name cannot be null or empty");
        }

        if (errors.Count > 0)
        {
            return Task.FromResult(UIProfileValidationResult.Failure(errors.ToArray()));
        }

        return Task.FromResult(OnValidate());
    }

    /// <inheritdoc />
    public T GetConfigurationValue<T>(string key, T defaultValue = default!)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return defaultValue;
        }

        if (Configuration.Properties.TryGetValue(key, out var value))
        {
            try
            {
                if (value is T typedValue)
                {
                    return typedValue;
                }

                if (typeof(T) == typeof(string))
                {
                    return (T)(object)value.ToString()!;
                }

                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to convert configuration value for key '{Key}' to type {Type}", key, typeof(T).Name);
                return defaultValue;
            }
        }

        return defaultValue;
    }

    #endregion

    #region Protected Virtual Methods

    /// <summary>
    /// Called when the profile is being activated. Override in derived classes for custom activation logic.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async activation operation.</returns>
    protected virtual Task OnActivateAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the profile is being deactivated. Override in derived classes for custom deactivation logic.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async deactivation operation.</returns>
    protected virtual Task OnDeactivateAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called during profile validation. Override in derived classes for custom validation logic.
    /// </summary>
    /// <returns>The validation result.</returns>
    protected virtual UIProfileValidationResult OnValidate()
    {
        return UIProfileValidationResult.Success();
    }

    #endregion
}