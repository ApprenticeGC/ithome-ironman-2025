using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.UI.Services;

/// <summary>
/// Base implementation of a UI profile that provides common functionality.
/// </summary>
public abstract class BaseUIProfile : IUIProfile
{
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, object> _configuration;
    private readonly Dictionary<Type, Type> _preferredProviders;
    private bool _isActive;

    protected BaseUIProfile(
        string id, 
        string name, 
        string description, 
        UIProfileType profileType, 
        string version,
        ILogger logger)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        ProfileType = profileType;
        Version = version ?? throw new ArgumentNullException(nameof(version));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _configuration = new ConcurrentDictionary<string, object>();
        _preferredProviders = new Dictionary<Type, Type>();
        _isActive = false;
        
        InitializeDefaultConfiguration();
        InitializePreferredProviders();
    }

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public UIProfileType ProfileType { get; }
    public string Version { get; }
    public bool IsActive => _isActive;

    public IReadOnlyDictionary<string, object> Configuration => 
        new Dictionary<string, object>(_configuration);

    public IReadOnlyDictionary<Type, Type> PreferredProviders => 
        new Dictionary<Type, Type>(_preferredProviders);

    public virtual async Task ActivateAsync(CancellationToken cancellationToken = default)
    {
        if (_isActive)
        {
            _logger.LogWarning("Profile {ProfileId} is already active", Id);
            return;
        }

        _logger.LogInformation("Activating UI profile: {ProfileName} ({ProfileType})", Name, ProfileType);
        
        try
        {
            await OnActivatingAsync(cancellationToken);
            _isActive = true;
            await OnActivatedAsync(cancellationToken);
            
            _logger.LogInformation("Successfully activated UI profile: {ProfileName}", Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to activate UI profile: {ProfileName}", Name);
            _isActive = false;
            throw;
        }
    }

    public virtual async Task DeactivateAsync(CancellationToken cancellationToken = default)
    {
        if (!_isActive)
        {
            _logger.LogWarning("Profile {ProfileId} is already inactive", Id);
            return;
        }

        _logger.LogInformation("Deactivating UI profile: {ProfileName}", Name);
        
        try
        {
            await OnDeactivatingAsync(cancellationToken);
            _isActive = false;
            await OnDeactivatedAsync(cancellationToken);
            
            _logger.LogInformation("Successfully deactivated UI profile: {ProfileName}", Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate UI profile: {ProfileName}", Name);
            throw;
        }
    }

    public T GetConfiguration<T>(string key, T defaultValue = default!)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Configuration key cannot be null or empty", nameof(key));

        if (_configuration.TryGetValue(key, out var value))
        {
            try
            {
                if (value is T typedValue)
                    return typedValue;
                
                // Attempt conversion
                return (T)Convert.ChangeType(value, typeof(T))!;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to convert configuration value for key {Key} to type {Type}", key, typeof(T));
                return defaultValue;
            }
        }

        return defaultValue;
    }

    public void SetConfiguration(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Configuration key cannot be null or empty", nameof(key));

        if (value == null)
        {
            _configuration.TryRemove(key, out _);
            _logger.LogDebug("Removed configuration key: {Key}", key);
        }
        else
        {
            _configuration[key] = value;
            _logger.LogDebug("Set configuration {Key} = {Value} (Type: {Type})", key, value, value.GetType().Name);
        }
    }

    /// <summary>
    /// Adds a preferred provider mapping for this profile.
    /// </summary>
    /// <typeparam name="TInterface">The service interface.</typeparam>
    /// <typeparam name="TImplementation">The preferred implementation.</typeparam>
    protected void AddPreferredProvider<TInterface, TImplementation>()
        where TImplementation : class, TInterface
    {
        _preferredProviders[typeof(TInterface)] = typeof(TImplementation);
        _logger.LogDebug("Added preferred provider mapping: {Interface} -> {Implementation}", 
            typeof(TInterface).Name, typeof(TImplementation).Name);
    }

    /// <summary>
    /// Called during profile initialization to set up default configuration values.
    /// Override this method to provide profile-specific default configuration.
    /// </summary>
    protected virtual void InitializeDefaultConfiguration()
    {
        // Base configuration common to all profiles
        SetConfiguration("CreatedAt", DateTime.UtcNow);
        SetConfiguration("LastActivated", DateTime.MinValue);
    }

    /// <summary>
    /// Called during profile initialization to set up preferred providers.
    /// Override this method to specify which providers should be used for this profile.
    /// </summary>
    protected virtual void InitializePreferredProviders()
    {
        // Base class doesn't specify any providers - derived classes should override
    }

    /// <summary>
    /// Called when the profile is being activated but before it's marked as active.
    /// Override this method to perform profile-specific activation logic.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    protected virtual Task OnActivatingAsync(CancellationToken cancellationToken = default)
    {
        SetConfiguration("LastActivated", DateTime.UtcNow);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called after the profile has been marked as active.
    /// Override this method to perform post-activation logic.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    protected virtual Task OnActivatedAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the profile is being deactivated but before it's marked as inactive.
    /// Override this method to perform profile-specific deactivation logic.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    protected virtual Task OnDeactivatingAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called after the profile has been marked as inactive.
    /// Override this method to perform post-deactivation cleanup.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    protected virtual Task OnDeactivatedAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}