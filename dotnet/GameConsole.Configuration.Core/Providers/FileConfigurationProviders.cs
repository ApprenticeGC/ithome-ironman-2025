using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GameConsole.Configuration.Core.Providers;

/// <summary>
/// Base class for file-based configuration providers (JSON, XML, etc.).
/// </summary>
public abstract class FileConfigurationProvider : IConfigurationProvider
{
    protected readonly ILogger Logger;
    protected readonly string BasePath;
    protected readonly bool ReloadOnChange;

    /// <summary>
    /// Initializes a new instance of the FileConfigurationProvider class.
    /// </summary>
    protected FileConfigurationProvider(
        ILogger logger,
        string name,
        ConfigurationPriority priority,
        string basePath = "config",
        bool reloadOnChange = true)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Priority = priority;
        BasePath = basePath;
        ReloadOnChange = reloadOnChange;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public ConfigurationPriority Priority { get; }

    /// <inheritdoc />
    public bool SupportsReload => ReloadOnChange;

    /// <inheritdoc />
    public event EventHandler? ConfigurationChanged;

    /// <inheritdoc />
    public virtual async Task<bool> CanApplyAsync(ConfigurationContext context)
    {
        var configFiles = await GetConfigurationFilesAsync(context);
        return configFiles.Any(File.Exists);
    }

    /// <inheritdoc />
    public virtual async Task BuildConfigurationAsync(IConfigurationBuilder builder, ConfigurationContext context)
    {
        var configFiles = await GetConfigurationFilesAsync(context);
        
        foreach (var configFile in configFiles.Where(File.Exists))
        {
            Logger.LogDebug("Adding configuration file '{ConfigFile}' from provider '{ProviderName}'", 
                configFile, Name);
            AddConfigurationSource(builder, configFile);
        }
    }

    /// <summary>
    /// Gets the configuration files to load for the given context.
    /// </summary>
    /// <param name="context">The configuration context.</param>
    /// <returns>The ordered list of configuration file paths.</returns>
    protected virtual async Task<IEnumerable<string>> GetConfigurationFilesAsync(ConfigurationContext context)
    {
        var files = new List<string>();
        var extension = GetFileExtension();
        
        // Base configuration file
        files.Add(Path.Combine(BasePath, $"appsettings{extension}"));
        
        // Environment-specific file
        files.Add(Path.Combine(BasePath, $"appsettings.{context.Environment}{extension}"));
        
        // Mode-specific file
        files.Add(Path.Combine(BasePath, $"appsettings.{context.Mode}{extension}"));
        
        // Environment + Mode specific file
        files.Add(Path.Combine(BasePath, $"appsettings.{context.Environment}.{context.Mode}{extension}"));
        
        return await Task.FromResult(files);
    }

    /// <summary>
    /// Gets the file extension for this provider (e.g., ".json", ".xml").
    /// </summary>
    protected abstract string GetFileExtension();

    /// <summary>
    /// Adds the configuration source to the builder for the specified file.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="filePath">The path to the configuration file.</param>
    protected abstract void AddConfigurationSource(IConfigurationBuilder builder, string filePath);

    /// <summary>
    /// Raises the ConfigurationChanged event.
    /// </summary>
    protected virtual void OnConfigurationChanged()
    {
        ConfigurationChanged?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// JSON file configuration provider.
/// </summary>
public class JsonConfigurationProvider : FileConfigurationProvider
{
    /// <summary>
    /// Initializes a new instance of the JsonConfigurationProvider class.
    /// </summary>
    public JsonConfigurationProvider(
        ILogger<JsonConfigurationProvider> logger,
        string basePath = "config",
        bool reloadOnChange = true)
        : base(logger, "JSON", ConfigurationPriority.Base, basePath, reloadOnChange)
    {
    }

    /// <inheritdoc />
    protected override string GetFileExtension() => ".json";

    /// <inheritdoc />
    protected override void AddConfigurationSource(IConfigurationBuilder builder, string filePath)
    {
        builder.AddJsonFile(filePath, optional: true, reloadOnChange: ReloadOnChange);
    }
}

/// <summary>
/// XML file configuration provider.
/// </summary>
public class XmlConfigurationProvider : FileConfigurationProvider
{
    /// <summary>
    /// Initializes a new instance of the XmlConfigurationProvider class.
    /// </summary>
    public XmlConfigurationProvider(
        ILogger<XmlConfigurationProvider> logger,
        string basePath = "config",
        bool reloadOnChange = true)
        : base(logger, "XML", ConfigurationPriority.Base, basePath, reloadOnChange)
    {
    }

    /// <inheritdoc />
    protected override string GetFileExtension() => ".xml";

    /// <inheritdoc />
    protected override void AddConfigurationSource(IConfigurationBuilder builder, string filePath)
    {
        builder.AddXmlFile(filePath, optional: true, reloadOnChange: ReloadOnChange);
    }
}

/// <summary>
/// Environment variables configuration provider.
/// </summary>
public class EnvironmentVariablesConfigurationProvider : IConfigurationProvider
{
    private readonly ILogger<EnvironmentVariablesConfigurationProvider> _logger;
    private readonly string? _prefix;

    /// <summary>
    /// Initializes a new instance of the EnvironmentVariablesConfigurationProvider class.
    /// </summary>
    public EnvironmentVariablesConfigurationProvider(
        ILogger<EnvironmentVariablesConfigurationProvider> logger,
        string? prefix = "GameConsole_")
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _prefix = prefix;
    }

    /// <inheritdoc />
    public string Name => "EnvironmentVariables";

    /// <inheritdoc />
    public ConfigurationPriority Priority => ConfigurationPriority.EnvironmentVariables;

    /// <inheritdoc />
    public bool SupportsReload => false;

    /// <inheritdoc />
#pragma warning disable CS0067 // Event is never used - environment variables don't support hot reload
    public event EventHandler? ConfigurationChanged;
#pragma warning restore CS0067

    /// <inheritdoc />
    public async Task<bool> CanApplyAsync(ConfigurationContext context)
    {
        // Environment variables are always available
        return await Task.FromResult(true);
    }

    /// <inheritdoc />
    public async Task BuildConfigurationAsync(IConfigurationBuilder builder, ConfigurationContext context)
    {
        _logger.LogDebug("Adding environment variables with prefix '{Prefix}'", _prefix ?? "<none>");
        
        if (string.IsNullOrEmpty(_prefix))
        {
            builder.AddEnvironmentVariables();
        }
        else
        {
            builder.AddEnvironmentVariables(_prefix);
        }
        
        await Task.CompletedTask;
    }
}