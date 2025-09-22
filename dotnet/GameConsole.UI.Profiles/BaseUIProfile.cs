using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GameConsole.UI.Profiles;

/// <summary>
/// Base implementation of IUIProfile providing common functionality.
/// </summary>
public abstract class BaseUIProfile : IUIProfile
{
    protected readonly ILogger _logger;
    private readonly Dictionary<string, object> _properties;
    private bool _isActive;

    protected BaseUIProfile(string id, string name, string description, UIMode mode, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Profile ID cannot be null or whitespace.", nameof(id));
        
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Profile name cannot be null or whitespace.", nameof(name));

        Id = id;
        Name = name;
        Description = description ?? string.Empty;
        Mode = mode;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _properties = new Dictionary<string, object>();
    }

    #region IUIProfile Implementation

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public UIMode Mode { get; }
    public bool IsActive => _isActive;
    public IReadOnlyDictionary<string, object> Properties => _properties.AsReadOnly();

    public virtual async Task ActivateAsync(CancellationToken cancellationToken = default)
    {
        if (_isActive)
        {
            _logger.LogWarning("Profile {ProfileId} is already active", Id);
            return;
        }

        _logger.LogInformation("Activating UI profile: {ProfileName} ({ProfileId})", Name, Id);
        
        try
        {
            await OnActivateAsync(cancellationToken);
            _isActive = true;
            _logger.LogInformation("Successfully activated UI profile: {ProfileName} ({ProfileId})", Name, Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to activate UI profile: {ProfileName} ({ProfileId})", Name, Id);
            throw;
        }
    }

    public virtual async Task DeactivateAsync(CancellationToken cancellationToken = default)
    {
        if (!_isActive)
        {
            _logger.LogWarning("Profile {ProfileId} is not active", Id);
            return;
        }

        _logger.LogInformation("Deactivating UI profile: {ProfileName} ({ProfileId})", Name, Id);
        
        try
        {
            await OnDeactivateAsync(cancellationToken);
            _isActive = false;
            _logger.LogInformation("Successfully deactivated UI profile: {ProfileName} ({ProfileId})", Name, Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate UI profile: {ProfileName} ({ProfileId})", Name, Id);
            throw;
        }
    }

    public T GetProperty<T>(string key, T defaultValue = default!)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Property key cannot be null or whitespace.", nameof(key));

        if (!_properties.TryGetValue(key, out var value))
            return defaultValue;

        try
        {
            if (value is T directValue)
                return directValue;

            if (value is JsonElement jsonElement)
                return jsonElement.Deserialize<T>() ?? defaultValue;

            // Try to convert the value to the requested type
            return (T)Convert.ChangeType(value, typeof(T)) ?? defaultValue;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to convert property {Key} to type {Type}, returning default value", key, typeof(T).Name);
            return defaultValue;
        }
    }

    #endregion

    #region Protected Methods for Derived Classes

    /// <summary>
    /// Called when the profile is being activated. Override to implement profile-specific activation logic.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async activation operation.</returns>
    protected virtual Task OnActivateAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>
    /// Called when the profile is being deactivated. Override to implement profile-specific deactivation logic.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async deactivation operation.</returns>
    protected virtual Task OnDeactivateAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>
    /// Sets a property value for this profile.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The property value.</param>
    protected void SetProperty(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Property key cannot be null or whitespace.", nameof(key));

        _properties[key] = value;
    }

    /// <summary>
    /// Removes a property from this profile.
    /// </summary>
    /// <param name="key">The property key to remove.</param>
    /// <returns>True if the property was removed, false if it didn't exist.</returns>
    protected bool RemoveProperty(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        return _properties.Remove(key);
    }

    /// <summary>
    /// Clears all properties from this profile.
    /// </summary>
    protected void ClearProperties()
    {
        _properties.Clear();
    }

    #endregion
}