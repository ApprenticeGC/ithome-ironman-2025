using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace GameConsole.UI.Configuration;

/// <summary>
/// Provides a fluent API for building UI profile configurations.
/// Supports method chaining for easy and intuitive configuration construction.
/// </summary>
public class ProfileConfigurationBuilder
{
    private readonly Dictionary<string, object> _settings = new();
    private readonly Dictionary<string, object> _metadata = new();
    private string _profileId = Guid.NewGuid().ToString();
    private string _name = string.Empty;
    private string _description = string.Empty;
    private string _version = "1.0.0";
    private ProfileScope _scope = ProfileScope.Global;
    private string _environment = "Default";
    private string? _parentProfileId;

    /// <summary>
    /// Sets the profile identifier.
    /// </summary>
    /// <param name="profileId">The unique profile identifier.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ProfileConfigurationBuilder WithProfileId(string profileId)
    {
        _profileId = profileId ?? throw new ArgumentNullException(nameof(profileId));
        return this;
    }

    /// <summary>
    /// Sets the profile name.
    /// </summary>
    /// <param name="name">The profile name.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ProfileConfigurationBuilder WithName(string name)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        return this;
    }

    /// <summary>
    /// Sets the profile description.
    /// </summary>
    /// <param name="description">The profile description.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ProfileConfigurationBuilder WithDescription(string description)
    {
        _description = description ?? throw new ArgumentNullException(nameof(description));
        return this;
    }

    /// <summary>
    /// Sets the profile version.
    /// </summary>
    /// <param name="version">The profile version.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ProfileConfigurationBuilder WithVersion(string version)
    {
        _version = version ?? throw new ArgumentNullException(nameof(version));
        return this;
    }

    /// <summary>
    /// Sets the profile scope.
    /// </summary>
    /// <param name="scope">The profile scope.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ProfileConfigurationBuilder WithScope(ProfileScope scope)
    {
        _scope = scope;
        return this;
    }

    /// <summary>
    /// Sets the target environment for this profile.
    /// </summary>
    /// <param name="environment">The environment name.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ProfileConfigurationBuilder ForEnvironment(string environment)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        return this;
    }

    /// <summary>
    /// Sets the parent profile this configuration inherits from.
    /// </summary>
    /// <param name="parentProfileId">The parent profile identifier.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ProfileConfigurationBuilder InheritsFrom(string parentProfileId)
    {
        _parentProfileId = parentProfileId;
        return this;
    }

    /// <summary>
    /// Adds a configuration setting.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The configuration value.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ProfileConfigurationBuilder WithSetting(string key, object value)
    {
        _settings[key] = value;
        return this;
    }

    /// <summary>
    /// Adds multiple configuration settings from a dictionary.
    /// </summary>
    /// <param name="settings">The settings dictionary.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ProfileConfigurationBuilder WithSettings(IDictionary<string, object> settings)
    {
        foreach (var kvp in settings)
        {
            _settings[kvp.Key] = kvp.Value;
        }
        return this;
    }

    /// <summary>
    /// Adds configuration settings from an object using reflection.
    /// </summary>
    /// <param name="settingsObject">The object containing settings.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ProfileConfigurationBuilder WithSettingsFrom(object settingsObject)
    {
        if (settingsObject == null)
            throw new ArgumentNullException(nameof(settingsObject));

        var json = JsonSerializer.Serialize(settingsObject);
        var dictionary = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        
        if (dictionary != null)
        {
            foreach (var kvp in dictionary)
            {
                _settings[kvp.Key] = ConvertJsonElement(kvp.Value);
            }
        }
        
        return this;
    }

    /// <summary>
    /// Adds metadata to the profile.
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
    /// Configures UI-specific settings using a delegate.
    /// </summary>
    /// <param name="configure">The configuration delegate.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ProfileConfigurationBuilder ConfigureUI(Action<UISettingsBuilder> configure)
    {
        var uiBuilder = new UISettingsBuilder();
        configure(uiBuilder);
        
        var uiSettings = uiBuilder.Build();
        foreach (var kvp in uiSettings)
        {
            _settings[$"UI:{kvp.Key}"] = kvp.Value;
        }
        
        return this;
    }

    /// <summary>
    /// Builds the profile configuration.
    /// </summary>
    /// <returns>A new instance of IProfileConfiguration.</returns>
    public IProfileConfiguration Build()
    {
        ValidateBuilder();
        
        var configurationBuilder = new ConfigurationBuilder();
        var settings = _settings.ToDictionary(
            kvp => kvp.Key, 
            kvp => kvp.Value?.ToString());
        configurationBuilder.AddInMemoryCollection(settings!);

        return new ProfileConfiguration(
            _profileId,
            _name,
            _description,
            _version,
            _scope,
            _environment,
            _parentProfileId,
            configurationBuilder.Build(),
            _metadata);
    }

    /// <summary>
    /// Creates a new builder instance from an existing configuration.
    /// </summary>
    /// <param name="existingConfiguration">The existing configuration to copy from.</param>
    /// <returns>A new builder instance pre-configured with existing values.</returns>
    public static ProfileConfigurationBuilder FromExisting(IProfileConfiguration existingConfiguration)
    {
        var builder = new ProfileConfigurationBuilder();
        
        builder._profileId = existingConfiguration.ProfileId;
        builder._name = existingConfiguration.Name;
        builder._description = existingConfiguration.Description;
        builder._version = existingConfiguration.Version;
        builder._scope = existingConfiguration.Scope;
        builder._environment = existingConfiguration.Environment;
        builder._parentProfileId = existingConfiguration.ParentProfileId;

        // Copy all configuration values
        foreach (var kvp in existingConfiguration.Configuration.AsEnumerable())
        {
            if (kvp.Value != null)
            {
                builder._settings[kvp.Key] = kvp.Value;
            }
        }

        // Copy metadata
        foreach (var kvp in existingConfiguration.Metadata)
        {
            builder._metadata[kvp.Key] = kvp.Value;
        }

        return builder;
    }

    private void ValidateBuilder()
    {
        if (string.IsNullOrWhiteSpace(_name))
            throw new InvalidOperationException("Profile name is required.");
        
        if (string.IsNullOrWhiteSpace(_version))
            throw new InvalidOperationException("Profile version is required.");
    }

    private static object ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.TryGetInt32(out var intVal) ? intVal : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null!,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToList(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
            _ => element.ToString()
        };
    }
}

/// <summary>
/// Builder for UI-specific configuration settings.
/// </summary>
public class UISettingsBuilder
{
    private readonly Dictionary<string, object> _settings = new();

    /// <summary>
    /// Sets the theme for the UI.
    /// </summary>
    /// <param name="theme">The theme name.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public UISettingsBuilder WithTheme(string theme)
    {
        _settings["Theme"] = theme;
        return this;
    }

    /// <summary>
    /// Sets the layout configuration.
    /// </summary>
    /// <param name="layout">The layout name.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public UISettingsBuilder WithLayout(string layout)
    {
        _settings["Layout"] = layout;
        return this;
    }

    /// <summary>
    /// Configures window settings.
    /// </summary>
    /// <param name="width">Window width.</param>
    /// <param name="height">Window height.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public UISettingsBuilder WithWindowSize(int width, int height)
    {
        _settings["Window:Width"] = width;
        _settings["Window:Height"] = height;
        return this;
    }

    /// <summary>
    /// Sets whether the interface should be fullscreen.
    /// </summary>
    /// <param name="fullscreen">True for fullscreen mode.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public UISettingsBuilder WithFullscreen(bool fullscreen)
    {
        _settings["Fullscreen"] = fullscreen;
        return this;
    }

    /// <summary>
    /// Adds a custom UI setting.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The setting value.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public UISettingsBuilder WithCustomSetting(string key, object value)
    {
        _settings[key] = value;
        return this;
    }

    internal Dictionary<string, object> Build() => new(_settings);
}