using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using System.Text.Json;

namespace GameConsole.UI.Configuration;

/// <summary>
/// Fluent builder for creating profile configurations with type safety and validation.
/// </summary>
public sealed class ProfileConfigurationBuilder
{
    private string _id = string.Empty;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private Version _version = new(1, 0, 0);
    private string _environment = "Development";
    private string? _parentProfileId;
    private readonly Dictionary<string, object> _metadata = new();
    private readonly ConfigurationBuilder _configurationBuilder = new();

    /// <summary>
    /// Sets the unique identifier for the profile configuration.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ProfileConfigurationBuilder WithId(string id)
    {
        _id = id ?? throw new ArgumentNullException(nameof(id));
        return this;
    }

    /// <summary>
    /// Sets the display name for the profile configuration.
    /// </summary>
    /// <param name="name">The display name.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ProfileConfigurationBuilder WithName(string name)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        return this;
    }

    /// <summary>
    /// Sets the description for the profile configuration.
    /// </summary>
    /// <param name="description">The description.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ProfileConfigurationBuilder WithDescription(string description)
    {
        _description = description ?? throw new ArgumentNullException(nameof(description));
        return this;
    }

    /// <summary>
    /// Sets the version for the profile configuration.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ProfileConfigurationBuilder WithVersion(Version version)
    {
        _version = version ?? throw new ArgumentNullException(nameof(version));
        return this;
    }

    /// <summary>
    /// Sets the environment for the profile configuration.
    /// </summary>
    /// <param name="environment">The environment (Development, Staging, Production).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ProfileConfigurationBuilder ForEnvironment(string environment)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        return this;
    }

    /// <summary>
    /// Sets the parent profile this configuration inherits from.
    /// </summary>
    /// <param name="parentProfileId">The parent profile ID.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ProfileConfigurationBuilder InheritsFrom(string? parentProfileId)
    {
        _parentProfileId = parentProfileId;
        return this;
    }

    /// <summary>
    /// Adds metadata to the profile configuration.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ProfileConfigurationBuilder WithMetadata(string key, object value)
    {
        _metadata[key] = value;
        return this;
    }

    /// <summary>
    /// Adds a configuration source from a JSON file.
    /// </summary>
    /// <param name="filePath">The path to the JSON configuration file.</param>
    /// <param name="optional">Whether the file is optional.</param>
    /// <param name="reloadOnChange">Whether to reload on file change.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ProfileConfigurationBuilder AddJsonFile(string filePath, bool optional = false, bool reloadOnChange = false)
    {
        _configurationBuilder.AddJsonFile(filePath, optional, reloadOnChange);
        return this;
    }

    /// <summary>
    /// Adds a configuration source from a JSON string.
    /// </summary>
    /// <param name="json">The JSON configuration string.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ProfileConfigurationBuilder AddJsonString(string json)
    {
        var tempBuilder = new ConfigurationBuilder();
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        tempBuilder.AddJsonStream(stream);
        var tempConfig = tempBuilder.Build();
        
        // Convert to key-value pairs and add to main builder
        var data = new Dictionary<string, string?>();
        AddConfigurationData(tempConfig, string.Empty, data);
        _configurationBuilder.AddInMemoryCollection(data);
        
        return this;
    }

    /// <summary>
    /// Adds a configuration source from in-memory key-value pairs.
    /// </summary>
    /// <param name="initialData">The initial configuration data.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ProfileConfigurationBuilder AddInMemoryCollection(IEnumerable<KeyValuePair<string, string?>> initialData)
    {
        _configurationBuilder.AddInMemoryCollection(initialData);
        return this;
    }

    /// <summary>
    /// Adds environment variables as a configuration source.
    /// </summary>
    /// <param name="prefix">Optional prefix to filter environment variables.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ProfileConfigurationBuilder AddEnvironmentVariables(string? prefix = null)
    {
        if (string.IsNullOrEmpty(prefix))
            _configurationBuilder.Add(new EnvironmentVariablesConfigurationSource());
        else
            _configurationBuilder.Add(new EnvironmentVariablesConfigurationSource { Prefix = prefix });
        return this;
    }

    /// <summary>
    /// Builds the profile configuration.
    /// </summary>
    /// <returns>The constructed profile configuration.</returns>
    /// <exception cref="InvalidOperationException">Thrown when required properties are not set.</exception>
    public IProfileConfiguration Build()
    {
        if (string.IsNullOrEmpty(_id))
            throw new InvalidOperationException("Profile ID must be set using WithId().");
        
        if (string.IsNullOrEmpty(_name))
            throw new InvalidOperationException("Profile name must be set using WithName().");

        if (string.IsNullOrEmpty(_description))
            throw new InvalidOperationException("Profile description must be set using WithDescription().");

        // Add default metadata
        _metadata.TryAdd("CreatedAt", DateTime.UtcNow);
        _metadata.TryAdd("CreatedBy", Environment.UserName);

        var configuration = _configurationBuilder.Build();
        
        return new ProfileConfiguration(
            _id,
            _name, 
            _description,
            _version,
            _environment,
            _parentProfileId,
            configuration,
            _metadata.AsReadOnly());
    }

    /// <summary>
    /// Creates a new profile configuration builder.
    /// </summary>
    /// <returns>A new builder instance.</returns>
    public static ProfileConfigurationBuilder Create() => new();

    /// <summary>
    /// Recursively adds configuration data from a configuration section.
    /// </summary>
    /// <param name="configuration">The configuration to process.</param>
    /// <param name="prefix">The prefix for the current section.</param>
    /// <param name="data">The dictionary to add the data to.</param>
    private static void AddConfigurationData(IConfiguration configuration, string prefix, Dictionary<string, string?> data)
    {
        foreach (var child in configuration.GetChildren())
        {
            var key = string.IsNullOrEmpty(prefix) ? child.Key : $"{prefix}:{child.Key}";
            
            if (child.Value != null)
            {
                data[key] = child.Value;
            }

            AddConfigurationData(child, key, data);
        }
    }
}