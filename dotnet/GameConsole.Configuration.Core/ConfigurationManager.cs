using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GameConsole.Configuration.Core;

/// <summary>
/// Default implementation of IConfigurationManager providing centralized
/// configuration management with provider chain and hot-reload support.
/// </summary>
public class ConfigurationManager : IConfigurationManager
{
    private readonly ILogger<ConfigurationManager> _logger;
    private readonly IEnumerable<IConfigurationProvider> _configurationProviders;
    private readonly IEnvironmentConfigurationResolver _environmentResolver;
    private readonly IConfigurationValidator _validator;
    private IConfiguration _configuration;
    private ConfigurationContext _context;
    private bool _isRunning;

    /// <summary>
    /// Initializes a new instance of the ConfigurationManager class.
    /// </summary>
    public ConfigurationManager(
        ILogger<ConfigurationManager> logger,
        IEnumerable<IConfigurationProvider> configurationProviders,
        IEnvironmentConfigurationResolver environmentResolver,
        IConfigurationValidator validator,
        ConfigurationContext context)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configurationProviders = configurationProviders ?? throw new ArgumentNullException(nameof(configurationProviders));
        _environmentResolver = environmentResolver ?? throw new ArgumentNullException(nameof(environmentResolver));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        
        _configuration = new ConfigurationBuilder().Build(); // Empty initial configuration
    }

    /// <inheritdoc />
    public IConfiguration Configuration => _configuration;

    /// <inheritdoc />
    public ConfigurationContext Context => _context;

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing ConfigurationManager for environment '{Environment}' and mode '{Mode}'", 
            _context.Environment, _context.Mode);

        try
        {
            await BuildConfigurationAsync(cancellationToken);
            
            // Subscribe to provider change events if they support reload
            foreach (var provider in _configurationProviders.Where(p => p.SupportsReload))
            {
                provider.ConfigurationChanged += OnProviderConfigurationChanged;
            }
            
            _logger.LogInformation("ConfigurationManager initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize ConfigurationManager");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting ConfigurationManager");
        
        // Validate configuration on startup
        var validationResult = await ValidateAsync(cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors);
            _logger.LogWarning("Configuration validation failed: {Errors}", errors);
            
            // Log warnings but don't fail startup for warnings
            if (validationResult.Warnings.Any())
            {
                var warnings = string.Join(", ", validationResult.Warnings);
                _logger.LogWarning("Configuration validation warnings: {Warnings}", warnings);
            }
        }
        
        _isRunning = true;
        _logger.LogInformation("ConfigurationManager started");
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping ConfigurationManager");
        
        // Unsubscribe from provider events
        foreach (var provider in _configurationProviders.Where(p => p.SupportsReload))
        {
            provider.ConfigurationChanged -= OnProviderConfigurationChanged;
        }
        
        _isRunning = false;
        _logger.LogInformation("ConfigurationManager stopped");
        
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Reloading configuration");
        
        var previousConfiguration = _configuration;
        await BuildConfigurationAsync(cancellationToken);
        
        // Raise configuration changed event
        ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs
        {
            SectionPath = string.Empty, // Full configuration reload
            PreviousConfiguration = previousConfiguration,
            NewConfiguration = _configuration
        });
        
        _logger.LogInformation("Configuration reloaded successfully");
    }

    /// <inheritdoc />
    public async Task<T?> GetSectionAsync<T>(string sectionKey, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionKey);
        
        var section = _configuration.GetSection(sectionKey);
        if (!section.Exists())
        {
            _logger.LogWarning("Configuration section '{SectionKey}' not found", sectionKey);
            return null;
        }

        try
        {
            var result = section.Get<T>();
            
            // Validate the bound configuration
            if (result != null)
            {
                var validationResult = await _validator.ValidateAsync(result, sectionKey, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var errors = string.Join(", ", validationResult.Errors);
                    _logger.LogWarning("Configuration section '{SectionKey}' validation failed: {Errors}", 
                        sectionKey, errors);
                }
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bind configuration section '{SectionKey}' to type '{Type}'", 
                sectionKey, typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ConfigurationValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Validating complete configuration");
        
        var errors = new List<string>();
        var warnings = new List<string>();
        
        // Basic configuration validation
        if (_configuration == null)
        {
            errors.Add("Configuration is null");
            return new ConfigurationValidationResult { IsValid = false, Errors = errors };
        }
        
        // Validate each registered section type
        foreach (var supportedType in _validator.SupportedTypes)
        {
            var sectionKey = supportedType.Name.Replace("Configuration", "", StringComparison.OrdinalIgnoreCase);
            if (sectionKey.Length == 0) sectionKey = supportedType.Name;
            
            var section = _configuration.GetSection(sectionKey);
            if (section.Exists())
            {
                var sectionResult = await _validator.ValidateSectionAsync(_configuration, sectionKey, supportedType, cancellationToken);
                errors.AddRange(sectionResult.Errors);
                warnings.AddRange(sectionResult.Warnings);
            }
        }
        
        return new ConfigurationValidationResult 
        { 
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }

    /// <inheritdoc />
    public async Task<ConfigurationValidationResult> ValidateSectionAsync(string sectionKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionKey);
        
        var section = _configuration.GetSection(sectionKey);
        if (!section.Exists())
        {
            return new ConfigurationValidationResult 
            { 
                IsValid = false, 
                Errors = new[] { $"Configuration section '{sectionKey}' not found" }
            };
        }
        
        // Find matching type for section
        var matchingType = _validator.SupportedTypes
            .FirstOrDefault(t => t.Name.Equals($"{sectionKey}Configuration", StringComparison.OrdinalIgnoreCase) ||
                                t.Name.Equals(sectionKey, StringComparison.OrdinalIgnoreCase));
        
        if (matchingType == null)
        {
            return new ConfigurationValidationResult
            {
                IsValid = false,
                Errors = new[] { $"No registered validation type found for section '{sectionKey}'" }
            };
        }
        
        return await _validator.ValidateSectionAsync(_configuration, sectionKey, matchingType, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isRunning)
        {
            await StopAsync();
        }
        
        GC.SuppressFinalize(this);
    }

    private async Task BuildConfigurationAsync(CancellationToken cancellationToken)
    {
        var builder = new ConfigurationBuilder();
        
        // Apply providers in priority order
        var orderedProviders = _configurationProviders
            .Where(p => p.CanApplyAsync(_context).Result)
            .OrderBy(p => p.Priority);
        
        foreach (var provider in orderedProviders)
        {
            _logger.LogDebug("Applying configuration provider '{ProviderName}' with priority {Priority}", 
                provider.Name, provider.Priority);
            await provider.BuildConfigurationAsync(builder, _context);
        }
        
        var baseConfiguration = builder.Build();
        
        // Apply environment-specific resolution
        _configuration = await _environmentResolver.ResolveAsync(_context, baseConfiguration, cancellationToken);
    }

    private async void OnProviderConfigurationChanged(object? sender, EventArgs e)
    {
        try
        {
            _logger.LogInformation("Configuration provider change detected, reloading configuration");
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload configuration after provider change");
        }
    }
}