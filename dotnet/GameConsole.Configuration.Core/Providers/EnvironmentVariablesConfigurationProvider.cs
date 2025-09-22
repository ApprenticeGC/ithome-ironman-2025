using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using GameConsole.Configuration.Core.Models;

namespace GameConsole.Configuration.Core.Providers;

/// <summary>
/// Configuration provider that loads environment variables with GameConsole-specific prefixes.
/// </summary>
public sealed class EnvironmentVariablesConfigurationProvider : IConfigurationProvider
{
    private readonly ILogger<EnvironmentVariablesConfigurationProvider> _logger;
    private readonly string _prefix;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentVariablesConfigurationProvider"/> class.
    /// </summary>
    public EnvironmentVariablesConfigurationProvider(ILogger<EnvironmentVariablesConfigurationProvider> logger, string prefix = "GAMECONSOLE_", int priority = 200)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
        Priority = priority;
    }

    /// <inheritdoc />
    public string Name => "Environment Variables Configuration Provider";

    /// <inheritdoc />
    public int Priority { get; }

    /// <inheritdoc />
    public bool SupportsHotReload => false; // Environment variables don't typically change during runtime

    /// <inheritdoc />
    public event EventHandler<ConfigurationProviderChangeEventArgs>? Changed
    {
        add { /* Environment variables don't typically change, so no implementation needed */ }
        remove { /* Environment variables don't typically change, so no implementation needed */ }
    }

    /// <inheritdoc />
    public bool CanLoad(ConfigurationContext context)
    {
        // Environment variables are always available
        return true;
    }

    /// <inheritdoc />
    public Task LoadAsync(IConfigurationBuilder builder, ConfigurationContext context)
    {
        _logger.LogDebug("Loading environment variables with prefix: {Prefix}", _prefix);
        
        builder.AddEnvironmentVariables(_prefix);
        
        // Also add environment-specific variables if context specifies an environment
        if (!string.IsNullOrEmpty(context.Environment))
        {
            var envSpecificPrefix = $"{_prefix}{context.Environment.ToUpperInvariant()}_";
            _logger.LogDebug("Loading environment-specific variables with prefix: {Prefix}", envSpecificPrefix);
            builder.AddEnvironmentVariables(envSpecificPrefix);
        }

        return Task.CompletedTask;
    }
}