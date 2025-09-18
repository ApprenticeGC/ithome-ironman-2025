using System.Text.Json;
using System.Xml.Linq;

namespace GameConsole.UI.Configuration;

/// <summary>
/// Fluent builder for creating profile configurations with type safety and validation.
/// Provides a convenient API for constructing complex profile configurations.
/// </summary>
public class ProfileConfigurationBuilder
{
    private string _id = string.Empty;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private Version _version = new(1, 0, 0);
    private ConfigurationScope _scope = ConfigurationScope.Global;
    private string _environment = "Default";
    private string? _inheritsFrom;
    private readonly Dictionary<string, object?> _settings = [];

    /// <summary>
    /// Sets the unique identifier for the profile configuration.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public ProfileConfigurationBuilder WithId(string id)
    {
        _id = id ?? throw new ArgumentNullException(nameof(id));
        return this;
    }

    /// <summary>
    /// Sets the human-readable name for the profile configuration.
    /// </summary>
    /// <param name="name">The profile name.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public ProfileConfigurationBuilder WithName(string name)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        return this;
    }

    /// <summary>
    /// Sets the description for the profile configuration.
    /// </summary>
    /// <param name="description">The profile description.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public ProfileConfigurationBuilder WithDescription(string description)
    {
        _description = description ?? throw new ArgumentNullException(nameof(description));
        return this;
    }

    /// <summary>
    /// Sets the version for the profile configuration.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public ProfileConfigurationBuilder WithVersion(Version version)
    {
        _version = version ?? throw new ArgumentNullException(nameof(version));
        return this;
    }

    /// <summary>
    /// Sets the version using major, minor, and patch components.
    /// </summary>
    /// <param name="major">Major version number.</param>
    /// <param name="minor">Minor version number.</param>
    /// <param name="patch">Patch version number.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public ProfileConfigurationBuilder WithVersion(int major, int minor, int patch = 0)
    {
        _version = new Version(major, minor, patch);
        return this;
    }

    /// <summary>
    /// Sets the configuration scope.
    /// </summary>
    /// <param name="scope">The configuration scope.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public ProfileConfigurationBuilder WithScope(ConfigurationScope scope)
    {
        _scope = scope;
        return this;
    }

    /// <summary>
    /// Sets the target environment for the configuration.
    /// </summary>
    /// <param name="environment">The environment name.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public ProfileConfigurationBuilder WithEnvironment(string environment)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        return this;
    }

    /// <summary>
    /// Sets the parent profile that this configuration inherits from.
    /// </summary>
    /// <param name="parentId">The ID of the parent profile.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public ProfileConfigurationBuilder InheritsFrom(string parentId)
    {
        _inheritsFrom = parentId;
        return this;
    }

    /// <summary>
    /// Adds a configuration setting with the specified key and value.
    /// </summary>
    /// <typeparam name="T">The type of the configuration value.</typeparam>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The configuration value.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public ProfileConfigurationBuilder WithSetting<T>(string key, T value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        _settings[key] = value;
        return this;
    }

    /// <summary>
    /// Adds multiple configuration settings from a dictionary.
    /// </summary>
    /// <param name="settings">The configuration settings to add.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public ProfileConfigurationBuilder WithSettings(IReadOnlyDictionary<string, object?> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        foreach (var (key, value) in settings)
        {
            _settings[key] = value;
        }
        return this;
    }

    /// <summary>
    /// Removes a configuration setting.
    /// </summary>
    /// <param name="key">The configuration key to remove.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public ProfileConfigurationBuilder RemoveSetting(string key)
    {
        _settings.Remove(key);
        return this;
    }

    /// <summary>
    /// Builds the profile configuration instance.
    /// </summary>
    /// <returns>A new IProfileConfiguration instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when required properties are not set.</exception>
    public IProfileConfiguration Build()
    {
        if (string.IsNullOrWhiteSpace(_id))
            throw new InvalidOperationException("Profile ID is required.");

        if (string.IsNullOrWhiteSpace(_name))
            throw new InvalidOperationException("Profile name is required.");

        return new ProfileConfiguration
        {
            Id = _id,
            Name = _name,
            Description = _description,
            Version = _version,
            Scope = _scope,
            Environment = _environment,
            InheritsFrom = _inheritsFrom,
            Settings = _settings.AsReadOnly()
        };
    }

    /// <summary>
    /// Creates a new builder instance from an existing configuration.
    /// </summary>
    /// <param name="configuration">The configuration to copy.</param>
    /// <returns>A new builder instance with copied values.</returns>
    public static ProfileConfigurationBuilder FromConfiguration(IProfileConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new ProfileConfigurationBuilder()
            .WithId(configuration.Id)
            .WithName(configuration.Name)
            .WithDescription(configuration.Description)
            .WithVersion(configuration.Version)
            .WithScope(configuration.Scope)
            .WithEnvironment(configuration.Environment)
            .InheritsFrom(configuration.InheritsFrom)
            .WithSettings(configuration.Settings);
    }
}

/// <summary>
/// Default implementation of IProfileConfiguration.
/// </summary>
internal class ProfileConfiguration : IProfileConfiguration
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required Version Version { get; init; }
    public required ConfigurationScope Scope { get; init; }
    public required string Environment { get; init; }
    public string? InheritsFrom { get; init; }
    public required IReadOnlyDictionary<string, object?> Settings { get; init; }

    public T? GetValue<T>(string key)
    {
        if (!Settings.TryGetValue(key, out var value))
            return default;

        return ConvertValue<T>(value);
    }

    public T GetValue<T>(string key, T defaultValue)
    {
        if (!Settings.TryGetValue(key, out var value))
            return defaultValue;

        var converted = ConvertValue<T>(value);
        return converted ?? defaultValue;
    }

    public bool HasValue(string key) => Settings.ContainsKey(key);

    public async Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
    {
        var errors = new List<ValidationError>();
        var warnings = new List<ValidationWarning>();

        // Basic validation
        if (string.IsNullOrWhiteSpace(Id))
            errors.Add(new ValidationError { Property = nameof(Id), Message = "Profile ID cannot be empty." });

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add(new ValidationError { Property = nameof(Name), Message = "Profile name cannot be empty." });

        // Version validation
        if (Version.Major < 1)
            warnings.Add(new ValidationWarning { Property = nameof(Version), Message = "Major version should be at least 1." });

        await Task.CompletedTask; // Placeholder for async validation logic

        return errors.Count > 0 
            ? ValidationResult.Failure(errors.ToArray()) 
            : warnings.Count > 0 
                ? ValidationResult.SuccessWithWarnings(warnings.ToArray())
                : ValidationResult.Success();
    }

    public IProfileConfiguration WithOverrides(IReadOnlyDictionary<string, object?> overrides)
    {
        ArgumentNullException.ThrowIfNull(overrides);

        var newSettings = new Dictionary<string, object?>(Settings);
        foreach (var (key, value) in overrides)
        {
            newSettings[key] = value;
        }

        return new ProfileConfiguration
        {
            Id = Id,
            Name = Name,
            Description = Description,
            Version = Version,
            Scope = Scope,
            Environment = Environment,
            InheritsFrom = InheritsFrom,
            Settings = newSettings.AsReadOnly()
        };
    }

    public string ToJson()
    {
        var data = new
        {
            Id,
            Name,
            Description,
            Version = Version.ToString(),
            Scope = Scope.ToString(),
            Environment,
            InheritsFrom,
            Settings
        };

        return JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
    }

    public string ToXml()
    {
        var doc = new XElement("ProfileConfiguration",
            new XElement("Id", Id),
            new XElement("Name", Name),
            new XElement("Description", Description),
            new XElement("Version", Version.ToString()),
            new XElement("Scope", Scope.ToString()),
            new XElement("Environment", Environment),
            InheritsFrom != null ? new XElement("InheritsFrom", InheritsFrom) : null,
            new XElement("Settings",
                Settings.Select(kvp => new XElement("Setting",
                    new XAttribute("key", kvp.Key),
                    new XAttribute("value", kvp.Value?.ToString() ?? "")
                ))
            )
        );

        return doc.ToString();
    }

    private static T? ConvertValue<T>(object? value)
    {
        if (value is null)
            return default;

        if (value is T directValue)
            return directValue;

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return default;
        }
    }
}