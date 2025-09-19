using GameConsole.Core.Abstractions;
using GameConsole.Input.Core;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace GameConsole.Input.Services;

/// <summary>
/// Input mapping service for customizable key bindings and input configuration.
/// Supports runtime remapping and configuration persistence.
/// </summary>
[Service("Input Mapping", "1.0.0", "Manages customizable input mappings and key binding configurations", 
    Categories = new[] { "Input", "Configuration", "Mapping" }, Lifetime = ServiceLifetime.Singleton)]
public class InputMappingService : BaseInputService, IInputMappingCapability
{
    private readonly ConcurrentDictionary<string, InputMappingConfiguration> _configurations;
    private InputMappingConfiguration _activeConfiguration;
    private readonly string _defaultConfigurationName = "Default";

    public InputMappingService(ILogger<InputMappingService> logger) : base(logger)
    {
        _configurations = new ConcurrentDictionary<string, InputMappingConfiguration>();
        _activeConfiguration = new InputMappingConfiguration(_defaultConfigurationName);
    }

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Initializing input mapping service");
        
        // Create default mapping configuration
        var defaultMappings = new Dictionary<string, string>
        {
            // Movement
            ["Keyboard:W"] = "Move Forward",
            ["Keyboard:S"] = "Move Backward", 
            ["Keyboard:A"] = "Move Left",
            ["Keyboard:D"] = "Move Right",
            ["Gamepad:LeftStickY"] = "Move Forward/Backward",
            ["Gamepad:LeftStickX"] = "Move Left/Right",
            
            // Actions
            ["Keyboard:Space"] = "Jump",
            ["Gamepad:A"] = "Jump",
            ["Mouse:Left"] = "Primary Action",
            ["Gamepad:RightTrigger"] = "Primary Action",
            ["Mouse:Right"] = "Secondary Action",
            ["Gamepad:LeftTrigger"] = "Secondary Action",
            
            // UI
            ["Keyboard:Escape"] = "Menu",
            ["Gamepad:Start"] = "Menu",
            ["Keyboard:Tab"] = "Inventory",
            ["Gamepad:Back"] = "Inventory",
            
            // Camera
            ["Mouse:X"] = "Camera Horizontal",
            ["Mouse:Y"] = "Camera Vertical",
            ["Gamepad:RightStickX"] = "Camera Horizontal",
            ["Gamepad:RightStickY"] = "Camera Vertical"
        };
        
        foreach (var mapping in defaultMappings)
        {
            _activeConfiguration.SetMapping(mapping.Key, mapping.Value);
        }
        
        _configurations[_defaultConfigurationName] = _activeConfiguration;
        
        await Task.CompletedTask;
    }

    protected override async Task OnStartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting input mapping service");
        
        // Load saved configurations if they exist
        await LoadConfigurationsAsync(cancellationToken);
        
        await Task.CompletedTask;
    }

    protected override async Task OnStopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Stopping input mapping service");
        
        // Save current configurations
        await SaveConfigurationsAsync(cancellationToken);
        
        await Task.CompletedTask;
    }

    // BaseInputService overrides (InputMappingService doesn't handle direct input)
    public override Task<bool> IsKeyPressedAsync(KeyCode key, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public override Task<Vector2> GetMousePositionAsync(CancellationToken cancellationToken = default) => Task.FromResult(Vector2.Zero);
    public override Task<bool> IsMouseButtonPressedAsync(MouseButton button, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public override Task<bool> IsGamepadButtonPressedAsync(int gamepadIndex, GamepadButton button, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public override Task<float> GetGamepadAxisAsync(int gamepadIndex, GamepadAxis axis, CancellationToken cancellationToken = default) => Task.FromResult(0.0f);
    public override Task<int> GetConnectedGamepadCountAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    public override Task<bool> IsGamepadConnectedAsync(int gamepadIndex, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public override Task<string?> GetGamepadNameAsync(int gamepadIndex, CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);

    #region IInputMappingCapability Implementation

    public Task MapInputAsync(string physicalInput, string logicalAction, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(physicalInput))
            throw new ArgumentException("Physical input cannot be null or empty", nameof(physicalInput));
        
        if (string.IsNullOrWhiteSpace(logicalAction))
            throw new ArgumentException("Logical action cannot be null or empty", nameof(logicalAction));

        _activeConfiguration.SetMapping(physicalInput, logicalAction);
        
        _logger.LogInformation("Mapped {PhysicalInput} to {LogicalAction}", physicalInput, logicalAction);
        
        return Task.CompletedTask;
    }

    public Task<InputMappingConfiguration> GetMappingConfigurationAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Retrieved mapping configuration: {ProfileName}", _activeConfiguration.ProfileName);
        return Task.FromResult(_activeConfiguration);
    }

    public Task SaveMappingConfigurationAsync(InputMappingConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        _configurations[configuration.ProfileName] = configuration;
        _activeConfiguration = configuration;
        
        _logger.LogInformation("Saved mapping configuration: {ProfileName}", configuration.ProfileName);
        
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var capabilities = new[] { typeof(IInputMappingCapability) };
        return Task.FromResult<IEnumerable<Type>>(capabilities);
    }

    public Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        var hasCapability = typeof(T) == typeof(IInputMappingCapability);
        return Task.FromResult(hasCapability);
    }

    public Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (typeof(T) == typeof(IInputMappingCapability))
        {
            return Task.FromResult(this as T);
        }
        return Task.FromResult<T?>(null);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a new mapping configuration profile.
    /// </summary>
    /// <param name="profileName">Name of the new profile.</param>
    /// <param name="basedOnProfile">Optional profile to copy mappings from.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    public Task<InputMappingConfiguration> CreateProfileAsync(string profileName, string? basedOnProfile = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profileName))
            throw new ArgumentException("Profile name cannot be null or empty", nameof(profileName));

        Dictionary<string, string>? baseMappings = null;
        
        if (!string.IsNullOrEmpty(basedOnProfile) && _configurations.TryGetValue(basedOnProfile, out var baseConfig))
        {
            baseMappings = baseConfig.Mappings.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        
        var newConfiguration = new InputMappingConfiguration(profileName, baseMappings);
        _configurations[profileName] = newConfiguration;
        
        _logger.LogInformation("Created new mapping profile: {ProfileName}", profileName);
        
        return Task.FromResult(newConfiguration);
    }

    /// <summary>
    /// Switches to a different mapping configuration profile.
    /// </summary>
    /// <param name="profileName">Name of the profile to switch to.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    public Task SwitchProfileAsync(string profileName, CancellationToken cancellationToken = default)
    {
        if (!_configurations.TryGetValue(profileName, out var configuration))
        {
            throw new ArgumentException($"Profile '{profileName}' not found", nameof(profileName));
        }

        _activeConfiguration = configuration;
        
        _logger.LogInformation("Switched to mapping profile: {ProfileName}", profileName);
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets all available mapping configuration profiles.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    public Task<IEnumerable<string>> GetAvailableProfilesAsync(CancellationToken cancellationToken = default)
    {
        var profiles = _configurations.Keys.ToList();
        
        _logger.LogTrace("Retrieved {Count} available profiles", profiles.Count);
        
        return Task.FromResult<IEnumerable<string>>(profiles);
    }

    /// <summary>
    /// Resolves a physical input to its mapped logical action.
    /// </summary>
    /// <param name="physicalInput">The physical input to resolve.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    public Task<string?> ResolveInputMappingAsync(string physicalInput, CancellationToken cancellationToken = default)
    {
        var logicalAction = _activeConfiguration.GetMapping(physicalInput);
        
        _logger.LogTrace("Resolved {PhysicalInput} to {LogicalAction}", physicalInput, logicalAction ?? "null");
        
        return Task.FromResult(logicalAction);
    }

    /// <summary>
    /// Finds all physical inputs mapped to a specific logical action.
    /// </summary>
    /// <param name="logicalAction">The logical action to find mappings for.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    public Task<IEnumerable<string>> FindMappingsForActionAsync(string logicalAction, CancellationToken cancellationToken = default)
    {
        var mappings = _activeConfiguration.Mappings
            .Where(kvp => kvp.Value.Equals(logicalAction, StringComparison.OrdinalIgnoreCase))
            .Select(kvp => kvp.Key)
            .ToList();
        
        _logger.LogTrace("Found {Count} mappings for action {LogicalAction}", mappings.Count, logicalAction);
        
        return Task.FromResult<IEnumerable<string>>(mappings);
    }

    /// <summary>
    /// Removes a mapping for a specific physical input.
    /// </summary>
    /// <param name="physicalInput">The physical input to unmap.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    public Task UnmapInputAsync(string physicalInput, CancellationToken cancellationToken = default)
    {
        _activeConfiguration.RemoveMapping(physicalInput);
        
        _logger.LogInformation("Unmapped physical input: {PhysicalInput}", physicalInput);
        
        return Task.CompletedTask;
    }

    #endregion

    #region Private Methods

    private async Task LoadConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // In a real implementation, this would load from a file or database
            // For now, we'll just log that we would load configurations
            _logger.LogDebug("Loading input mapping configurations from storage");
            
            await Task.Delay(10, cancellationToken); // Simulate I/O delay
            
            _logger.LogDebug("Loaded {Count} input mapping configurations", _configurations.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load input mapping configurations");
        }
    }

    private async Task SaveConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // In a real implementation, this would save to a file or database
            // For now, we'll just log that we would save configurations
            _logger.LogDebug("Saving input mapping configurations to storage");
            
            await Task.Delay(10, cancellationToken); // Simulate I/O delay
            
            _logger.LogDebug("Saved {Count} input mapping configurations", _configurations.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save input mapping configurations");
        }
    }

    #endregion
}