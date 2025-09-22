using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using GameConsole.Core.Abstractions;
using GameConsole.Configuration.Core.Models;

namespace GameConsole.Configuration.Core;

/// <summary>
/// Centralized configuration manager for the GameConsole system.
/// Integrates with the service lifecycle and provides comprehensive configuration management.
/// </summary>
[Service("Configuration Manager", "1.0.0", "Centralized configuration management with environment support and validation", 
         Categories = new[] { "Configuration", "Infrastructure" }, 
         Lifetime = ServiceLifetime.Singleton)]
public sealed class ConfigurationManager : IConfigurationManager, IService
{
    private readonly ILogger<ConfigurationManager> _logger;
    private readonly List<IConfigurationProvider> _providers;
    private readonly IEnvironmentConfigurationResolver _environmentResolver;
    private readonly IConfigurationValidator _validator;
    private IConfigurationRoot? _configuration;
    private bool _isRunning;
    private readonly Dictionary<string, object> _runtimeOverrides = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationManager"/> class.
    /// </summary>
    public ConfigurationManager(
        ILogger<ConfigurationManager> logger,
        IEnvironmentConfigurationResolver environmentResolver,
        IConfigurationValidator validator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environmentResolver = environmentResolver ?? throw new ArgumentNullException(nameof(environmentResolver));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _providers = new List<IConfigurationProvider>();
    }

    /// <inheritdoc />
    public IConfigurationRoot Configuration => _configuration ?? throw new InvalidOperationException("Configuration not initialized. Call InitializeAsync first.");

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

    /// <summary>
    /// Adds a configuration provider to the manager.
    /// </summary>
    public void AddProvider(IConfigurationProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        
        _logger.LogDebug("Adding configuration provider: {ProviderName} (Priority: {Priority})", provider.Name, provider.Priority);
        _providers.Add(provider);
        _providers.Sort((a, b) => b.Priority.CompareTo(a.Priority)); // Higher priority first
        
        provider.Changed += OnProviderChanged;
    }

    /// <inheritdoc />
    public T GetSection<T>(string key) where T : class, new()
    {
        var section = new T();
        Configuration.GetSection(key).Bind(section);
        return section;
    }

    /// <inheritdoc />
    public T GetValue<T>(string key, T defaultValue = default!)
    {
        return Configuration.GetValue(key, defaultValue)!;
    }

    /// <inheritdoc />
    public async Task SetValueAsync(string key, object value)
    {
        _runtimeOverrides[key] = value;
        await RebuildConfigurationAsync();
        
        var context = new ConfigurationContext { Environment = _environmentResolver.CurrentEnvironment };
        OnConfigurationChanged(new[] { key }, context);
    }

    /// <inheritdoc />
    public async Task ReloadAsync()
    {
        _logger.LogInformation("Reloading configuration from all providers");
        await RebuildConfigurationAsync();
    }

    /// <inheritdoc />
    public async Task<ValidationResult> ValidateAsync(ConfigurationContext? context = null, CancellationToken cancellationToken = default)
    {
        context ??= new ConfigurationContext { Environment = _environmentResolver.CurrentEnvironment };
        return await _validator.ValidateAsync(Configuration, context, cancellationToken);
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing configuration manager");
        await RebuildConfigurationAsync();
        _logger.LogInformation("Configuration manager initialized with {ProviderCount} providers", _providers.Count);
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting configuration manager");
        _isRunning = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping configuration manager");
        _isRunning = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        foreach (var provider in _providers)
        {
            provider.Changed -= OnProviderChanged;
        }
        _providers.Clear();
        return ValueTask.CompletedTask;
    }

    private async Task RebuildConfigurationAsync()
    {
        var builder = new ConfigurationBuilder();
        var context = new ConfigurationContext { Environment = _environmentResolver.CurrentEnvironment };

        // Load from providers in priority order
        foreach (var provider in _providers.Where(p => p.CanLoad(context)))
        {
            await provider.LoadAsync(builder, context);
        }

        // Add runtime overrides
        if (_runtimeOverrides.Count > 0)
        {
            builder.AddInMemoryCollection(_runtimeOverrides.Select(kvp => 
                new KeyValuePair<string, string?>(kvp.Key, kvp.Value?.ToString())));
        }

        _configuration = builder.Build();
    }

    private void OnProviderChanged(object? sender, ConfigurationProviderChangeEventArgs e)
    {
        _logger.LogDebug("Configuration provider {ProviderName} reported changes: {Description}", e.ProviderName, e.ChangeDescription);
        
        Task.Run(async () =>
        {
            try
            {
                await ReloadAsync();
                var context = new ConfigurationContext { Environment = _environmentResolver.CurrentEnvironment };
                OnConfigurationChanged(Array.Empty<string>(), context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reloading configuration after provider change");
            }
        });
    }

    private void OnConfigurationChanged(IReadOnlyList<string> changedKeys, ConfigurationContext context)
    {
        ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs(changedKeys, context));
    }
}