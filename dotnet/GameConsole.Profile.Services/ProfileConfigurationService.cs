using GameConsole.Core.Registry;
using GameConsole.Profile.Core;
using Microsoft.Extensions.Logging;

namespace GameConsole.Profile.Services;

/// <summary>
/// Service responsible for applying profile configurations to service registrations.
/// </summary>
public class ProfileConfigurationService : IProfileConfiguration
{
    private readonly ILogger<ProfileConfigurationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileConfigurationService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ProfileConfigurationService(ILogger<ProfileConfigurationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task ApplyProfileAsync(IProfile profile, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        if (profile == null)
            throw new ArgumentNullException(nameof(profile));

        if (serviceProvider == null)
            throw new ArgumentNullException(nameof(serviceProvider));

        _logger.LogInformation("Applying profile configuration: {ProfileName} ({ProfileId})", profile.Name, profile.Id);

        // Get the service registry from the service provider
        var registry = serviceProvider.GetService(typeof(IServiceRegistry)) as IServiceRegistry;
        if (registry == null)
        {
            throw new InvalidOperationException("IServiceRegistry not found in service provider. Cannot apply profile configuration.");
        }

        var appliedServices = 0;
        var skippedServices = 0;

        foreach (var serviceConfig in profile.ServiceConfigurations)
        {
            try
            {
                if (!serviceConfig.Value.Enabled)
                {
                    _logger.LogDebug("Skipping disabled service: {ServiceInterface}", serviceConfig.Key);
                    skippedServices++;
                    continue;
                }

                await ApplyServiceConfigurationAsync(registry, serviceConfig.Key, serviceConfig.Value, cancellationToken);
                appliedServices++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply service configuration for {ServiceInterface}: {Error}", 
                    serviceConfig.Key, ex.Message);
                
                // Continue with other services rather than failing entirely
                skippedServices++;
            }
        }

        _logger.LogInformation("Profile application completed: {AppliedServices} applied, {SkippedServices} skipped", 
            appliedServices, skippedServices);

        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<bool> ValidateProfileAsync(IProfile profile, CancellationToken cancellationToken = default)
    {
        if (profile == null)
            return false;

        var errors = await GetValidationErrorsAsync(profile, cancellationToken);
        return !errors.Any();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetValidationErrorsAsync(IProfile profile, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (profile == null)
        {
            errors.Add("Profile cannot be null");
            return errors;
        }

        // Validate basic profile properties
        if (string.IsNullOrWhiteSpace(profile.Id))
            errors.Add("Profile ID cannot be empty");

        if (string.IsNullOrWhiteSpace(profile.Name))
            errors.Add("Profile name cannot be empty");

        if (string.IsNullOrWhiteSpace(profile.Version))
            errors.Add("Profile version cannot be empty");

        // Validate service configurations
        foreach (var serviceConfig in profile.ServiceConfigurations)
        {
            var serviceErrors = ValidateServiceConfiguration(serviceConfig.Key, serviceConfig.Value);
            errors.AddRange(serviceErrors);
        }

        // Check for duplicate service registrations
        var serviceInterfaces = profile.ServiceConfigurations.Keys.ToList();
        var duplicates = serviceInterfaces.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key);
        
        foreach (var duplicate in duplicates)
        {
            errors.Add($"Duplicate service configuration for: {duplicate}");
        }

        await Task.CompletedTask;
        return errors;
    }

    private async Task ApplyServiceConfigurationAsync(IServiceRegistry registry, string serviceInterface, 
        ServiceConfiguration config, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Applying service configuration: {ServiceInterface} -> {Implementation}", 
            serviceInterface, config.Implementation);

        // In a real implementation, you would:
        // 1. Resolve the implementation type by name
        // 2. Register it with the appropriate lifetime
        // 3. Configure capabilities and settings
        
        // For now, we'll simulate the registration
        // This would need to be implemented based on your actual service registry capabilities
        
        switch (config.Lifetime.ToLower())
        {
            case "singleton":
                // registry.RegisterSingleton(serviceType, implementationType);
                _logger.LogDebug("Would register {ServiceInterface} as singleton with {Implementation}", 
                    serviceInterface, config.Implementation);
                break;
            case "scoped":
                // registry.RegisterScoped(serviceType, implementationType);
                _logger.LogDebug("Would register {ServiceInterface} as scoped with {Implementation}", 
                    serviceInterface, config.Implementation);
                break;
            case "transient":
                // registry.RegisterTransient(serviceType, implementationType);
                _logger.LogDebug("Would register {ServiceInterface} as transient with {Implementation}", 
                    serviceInterface, config.Implementation);
                break;
            default:
                _logger.LogWarning("Unknown service lifetime: {Lifetime}. Using singleton as default.", config.Lifetime);
                break;
        }

        // Apply capabilities (would be implementation-specific)
        foreach (var capability in config.Capabilities)
        {
            _logger.LogDebug("Would enable capability {Capability} for {ServiceInterface}", capability, serviceInterface);
        }

        // Apply settings (would be implementation-specific)
        foreach (var setting in config.Settings)
        {
            _logger.LogDebug("Would apply setting {SettingKey}={SettingValue} to {ServiceInterface}", 
                setting.Key, setting.Value, serviceInterface);
        }

        await Task.CompletedTask;
    }

    private IEnumerable<string> ValidateServiceConfiguration(string serviceInterface, ServiceConfiguration config)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(serviceInterface))
        {
            errors.Add("Service interface name cannot be empty");
        }

        if (config == null)
        {
            errors.Add($"Service configuration for {serviceInterface} cannot be null");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(config.Implementation))
        {
            errors.Add($"Implementation for {serviceInterface} cannot be empty");
        }

        // Validate lifetime values
        var validLifetimes = new[] { "singleton", "scoped", "transient" };
        if (!validLifetimes.Contains(config.Lifetime.ToLower()))
        {
            errors.Add($"Invalid lifetime '{config.Lifetime}' for {serviceInterface}. Must be one of: {string.Join(", ", validLifetimes)}");
        }

        // Validate capabilities
        if (config.Capabilities != null)
        {
            foreach (var capability in config.Capabilities)
            {
                if (string.IsNullOrWhiteSpace(capability))
                {
                    errors.Add($"Empty capability name found for {serviceInterface}");
                }
            }
        }

        // Validate settings
        if (config.Settings != null)
        {
            foreach (var setting in config.Settings)
            {
                if (string.IsNullOrWhiteSpace(setting.Key))
                {
                    errors.Add($"Empty setting key found for {serviceInterface}");
                }
            }
        }

        return errors;
    }
}