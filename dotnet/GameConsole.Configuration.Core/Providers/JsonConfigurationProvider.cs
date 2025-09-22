using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using GameConsole.Configuration.Core.Models;

namespace GameConsole.Configuration.Core.Providers;

/// <summary>
/// Configuration provider that loads JSON configuration files with environment-specific support.
/// </summary>
public sealed class JsonConfigurationProvider : IConfigurationProvider
{
    private readonly ILogger<JsonConfigurationProvider> _logger;
    private readonly string _basePath;
    private readonly bool _optional;
    private FileSystemWatcher? _fileWatcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonConfigurationProvider"/> class.
    /// </summary>
    public JsonConfigurationProvider(ILogger<JsonConfigurationProvider> logger, string basePath, bool optional = false, int priority = 100)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
        _optional = optional;
        Priority = priority;
    }

    /// <inheritdoc />
    public string Name => "JSON Configuration Provider";

    /// <inheritdoc />
    public int Priority { get; }

    /// <inheritdoc />
    public bool SupportsHotReload => true;

    /// <inheritdoc />
    public event EventHandler<ConfigurationProviderChangeEventArgs>? Changed;

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
                _logger.LogDebug("Loading JSON configuration from: {Path}", path);
                builder.AddJsonFile(path, optional: _optional, reloadOnChange: false);
                
                if (SupportsHotReload)
                {
                    SetupFileWatcher(path);
                }
            }
            else if (!_optional)
            {
                _logger.LogWarning("Required JSON configuration file not found: {Path}", path);
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

        // Environment-specific file (e.g., appsettings.Development.json)
        if (!string.IsNullOrEmpty(context.Environment))
        {
            var envFile = Path.Combine(directory, $"{baseFileName}.{context.Environment}{extension}");
            paths.Add(envFile);
        }

        // Mode-specific file (e.g., appsettings.Game.json)
        if (!string.IsNullOrEmpty(context.Mode))
        {
            var modeFile = Path.Combine(directory, $"{baseFileName}.{context.Mode}{extension}");
            paths.Add(modeFile);
        }

        return paths;
    }

    private void SetupFileWatcher(string path)
    {
        if (_fileWatcher != null) return;

        var directory = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(directory)) return;

        try
        {
            _fileWatcher = new FileSystemWatcher(directory)
            {
                Filter = Path.GetFileName(path),
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            _fileWatcher.Changed += OnFileChanged;
            _logger.LogDebug("File watcher setup for: {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not setup file watcher for: {Path}", path);
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        _logger.LogDebug("Configuration file changed: {Path}", e.FullPath);
        Changed?.Invoke(this, new ConfigurationProviderChangeEventArgs(Name, $"File changed: {e.FullPath}"));
    }

    /// <summary>
    /// Disposes the file watcher.
    /// </summary>
    public void Dispose()
    {
        _fileWatcher?.Dispose();
        _fileWatcher = null;
    }
}