using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using GameConsole.Configuration.Core.Models;

namespace GameConsole.Configuration.Core.Providers;

/// <summary>
/// Configuration provider that loads XML configuration files with environment-specific support.
/// </summary>
public sealed class XmlConfigurationProvider : IConfigurationProvider
{
    private readonly ILogger<XmlConfigurationProvider> _logger;
    private readonly string _basePath;
    private readonly bool _optional;

    /// <summary>
    /// Initializes a new instance of the <see cref="XmlConfigurationProvider"/> class.
    /// </summary>
    public XmlConfigurationProvider(ILogger<XmlConfigurationProvider> logger, string basePath, bool optional = false, int priority = 90)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
        _optional = optional;
        Priority = priority;
    }

    /// <inheritdoc />
    public string Name => "XML Configuration Provider";

    /// <inheritdoc />
    public int Priority { get; }

    /// <inheritdoc />
    public bool SupportsHotReload => false; // XML doesn't have built-in reload support like JSON

    /// <inheritdoc />
    public event EventHandler<ConfigurationProviderChangeEventArgs>? Changed
    {
        add { /* XML provider doesn't support hot-reload in this implementation */ }
        remove { /* XML provider doesn't support hot-reload in this implementation */ }
    }

    /// <inheritdoc />
    public bool CanLoad(ConfigurationContext context)
    {
        var paths = GetConfigurationPaths(context);
        return _optional || paths.Any(File.Exists);
    }

    /// <inheritdoc />
    public Task LoadAsync(IConfigurationBuilder builder, ConfigurationContext context)
    {
        var paths = GetConfigurationPaths(context);
        
        foreach (var path in paths)
        {
            if (File.Exists(path))
            {
                _logger.LogDebug("Loading XML configuration from: {Path}", path);
                builder.AddXmlFile(path, optional: _optional, reloadOnChange: false);
            }
            else if (!_optional)
            {
                _logger.LogWarning("Required XML configuration file not found: {Path}", path);
            }
        }

        return Task.CompletedTask;
    }

    private IReadOnlyList<string> GetConfigurationPaths(ConfigurationContext context)
    {
        var paths = new List<string>();
        var baseFileName = Path.GetFileNameWithoutExtension(_basePath);
        var directory = Path.GetDirectoryName(_basePath) ?? "";
        var extension = Path.GetExtension(_basePath);

        // Base configuration file
        paths.Add(_basePath);

        // Environment-specific file (e.g., config.Development.xml)
        if (!string.IsNullOrEmpty(context.Environment))
        {
            var envFile = Path.Combine(directory, $"{baseFileName}.{context.Environment}{extension}");
            paths.Add(envFile);
        }

        // Mode-specific file (e.g., config.Game.xml)
        if (!string.IsNullOrEmpty(context.Mode))
        {
            var modeFile = Path.Combine(directory, $"{baseFileName}.{context.Mode}{extension}");
            paths.Add(modeFile);
        }

        return paths;
    }
}