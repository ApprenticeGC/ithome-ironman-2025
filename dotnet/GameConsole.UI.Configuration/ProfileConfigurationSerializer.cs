using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GameConsole.UI.Configuration;

/// <summary>
/// Provides serialization and deserialization capabilities for profile configurations.
/// Supports JSON and XML formats with extensible serialization options.
/// </summary>
public class ProfileConfigurationSerializer
{
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<ProfileConfigurationSerializer>? _logger;

    public ProfileConfigurationSerializer(ILogger<ProfileConfigurationSerializer>? logger = null)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    /// <summary>
    /// Serializes a profile configuration to JSON format.
    /// </summary>
    /// <param name="configuration">The configuration to serialize.</param>
    /// <returns>JSON string representation of the configuration.</returns>
    public string SerializeToJson(IProfileConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        var dto = CreateSerializationDto(configuration);
        var json = JsonSerializer.Serialize(dto, _jsonOptions);
        
        _logger?.LogDebug("Serialized profile {ProfileId} to JSON ({Size} characters)", 
            configuration.ProfileId, json.Length);
        
        return json;
    }

    /// <summary>
    /// Deserializes a profile configuration from JSON format.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized profile configuration.</returns>
    public IProfileConfiguration DeserializeFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("JSON content cannot be null or empty", nameof(json));

        var dto = JsonSerializer.Deserialize<ProfileConfigurationDto>(json, _jsonOptions);
        if (dto == null)
            throw new InvalidOperationException("Failed to deserialize JSON to profile configuration");

        var configuration = CreateConfigurationFromDto(dto);
        
        _logger?.LogDebug("Deserialized profile {ProfileId} from JSON", configuration.ProfileId);
        
        return configuration;
    }

    /// <summary>
    /// Serializes a profile configuration to XML format.
    /// </summary>
    /// <param name="configuration">The configuration to serialize.</param>
    /// <returns>XML string representation of the configuration.</returns>
    public string SerializeToXml(IProfileConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        var dto = CreateSerializationDto(configuration);
        var serializer = new XmlSerializer(typeof(ProfileConfigurationDto));
        
        using var stringWriter = new StringWriter();
        using var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
        {
            Indent = true,
            OmitXmlDeclaration = false
        });
        
        serializer.Serialize(xmlWriter, dto);
        var xml = stringWriter.ToString();
        
        _logger?.LogDebug("Serialized profile {ProfileId} to XML ({Size} characters)", 
            configuration.ProfileId, xml.Length);
        
        return xml;
    }

    /// <summary>
    /// Deserializes a profile configuration from XML format.
    /// </summary>
    /// <param name="xml">The XML string to deserialize.</param>
    /// <returns>The deserialized profile configuration.</returns>
    public IProfileConfiguration DeserializeFromXml(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            throw new ArgumentException("XML content cannot be null or empty", nameof(xml));

        var serializer = new XmlSerializer(typeof(ProfileConfigurationDto));
        
        using var stringReader = new StringReader(xml);
        using var xmlReader = XmlReader.Create(stringReader);
        
        var dto = serializer.Deserialize(xmlReader) as ProfileConfigurationDto;
        if (dto == null)
            throw new InvalidOperationException("Failed to deserialize XML to profile configuration");

        var configuration = CreateConfigurationFromDto(dto);
        
        _logger?.LogDebug("Deserialized profile {ProfileId} from XML", configuration.ProfileId);
        
        return configuration;
    }

    /// <summary>
    /// Saves a profile configuration to a JSON file.
    /// </summary>
    /// <param name="configuration">The configuration to save.</param>
    /// <param name="filePath">The file path to save to.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    public async Task SaveToJsonFileAsync(IProfileConfiguration configuration, string filePath, CancellationToken cancellationToken = default)
    {
        var json = SerializeToJson(configuration);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
        
        _logger?.LogInformation("Saved profile {ProfileId} to JSON file: {FilePath}", 
            configuration.ProfileId, filePath);
    }

    /// <summary>
    /// Loads a profile configuration from a JSON file.
    /// </summary>
    /// <param name="filePath">The file path to load from.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The loaded profile configuration.</returns>
    public async Task<IProfileConfiguration> LoadFromJsonFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        var configuration = DeserializeFromJson(json);
        
        _logger?.LogInformation("Loaded profile {ProfileId} from JSON file: {FilePath}", 
            configuration.ProfileId, filePath);
        
        return configuration;
    }

    /// <summary>
    /// Saves a profile configuration to an XML file.
    /// </summary>
    /// <param name="configuration">The configuration to save.</param>
    /// <param name="filePath">The file path to save to.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    public async Task SaveToXmlFileAsync(IProfileConfiguration configuration, string filePath, CancellationToken cancellationToken = default)
    {
        var xml = SerializeToXml(configuration);
        await File.WriteAllTextAsync(filePath, xml, cancellationToken);
        
        _logger?.LogInformation("Saved profile {ProfileId} to XML file: {FilePath}", 
            configuration.ProfileId, filePath);
    }

    /// <summary>
    /// Loads a profile configuration from an XML file.
    /// </summary>
    /// <param name="filePath">The file path to load from.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The loaded profile configuration.</returns>
    public async Task<IProfileConfiguration> LoadFromXmlFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var xml = await File.ReadAllTextAsync(filePath, cancellationToken);
        var configuration = DeserializeFromXml(xml);
        
        _logger?.LogInformation("Loaded profile {ProfileId} from XML file: {FilePath}", 
            configuration.ProfileId, filePath);
        
        return configuration;
    }

    private ProfileConfigurationDto CreateSerializationDto(IProfileConfiguration configuration)
    {
        // Convert configuration to dictionary for serialization
        var configurationValues = new Dictionary<string, object>();
        foreach (var section in configuration.Configuration.GetChildren())
        {
            GetConfigurationValues(section, string.Empty, configurationValues);
        }

        return new ProfileConfigurationDto
        {
            ProfileId = configuration.ProfileId,
            Name = configuration.Name,
            Description = configuration.Description,
            Version = configuration.Version,
            Scope = configuration.Scope,
            Environment = configuration.Environment,
            ParentProfileId = configuration.ParentProfileId,
            Settings = configurationValues,
            Metadata = configuration.Metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            CreatedAt = DateTime.UtcNow,
            SerializationVersion = "1.0"
        };
    }

    private IProfileConfiguration CreateConfigurationFromDto(ProfileConfigurationDto dto)
    {
        var builder = new ProfileConfigurationBuilder()
            .WithProfileId(dto.ProfileId)
            .WithName(dto.Name)
            .WithDescription(dto.Description ?? string.Empty)
            .WithVersion(dto.Version)
            .WithScope(dto.Scope)
            .ForEnvironment(dto.Environment);

        if (!string.IsNullOrEmpty(dto.ParentProfileId))
        {
            builder.InheritsFrom(dto.ParentProfileId);
        }

        if (dto.Settings != null)
        {
            builder.WithSettings(dto.Settings);
        }

        if (dto.Metadata != null)
        {
            foreach (var kvp in dto.Metadata)
            {
                builder.WithMetadata(kvp.Key, kvp.Value);
            }
        }

        return builder.Build();
    }

    private static void GetConfigurationValues(IConfigurationSection section, string prefix, Dictionary<string, object> values)
    {
        var key = string.IsNullOrEmpty(prefix) ? section.Key : $"{prefix}:{section.Key}";
        
        if (section.Value != null)
        {
            values[key] = section.Value;
        }

        foreach (var child in section.GetChildren())
        {
            GetConfigurationValues(child, key, values);
        }
    }
}

/// <summary>
/// Data transfer object for profile configuration serialization.
/// </summary>
[XmlRoot("ProfileConfiguration")]
public class ProfileConfigurationDto
{
    [JsonPropertyName("profileId")]
    [XmlElement("ProfileId")]
    public string ProfileId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    [XmlElement("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    [XmlElement("Description")]
    public string? Description { get; set; }

    [JsonPropertyName("version")]
    [XmlElement("Version")]
    public string Version { get; set; } = "1.0.0";

    [JsonPropertyName("scope")]
    [XmlElement("Scope")]
    public ProfileScope Scope { get; set; }

    [JsonPropertyName("environment")]
    [XmlElement("Environment")]
    public string Environment { get; set; } = "Default";

    [JsonPropertyName("parentProfileId")]
    [XmlElement("ParentProfileId")]
    public string? ParentProfileId { get; set; }

    [JsonPropertyName("settings")]
    [XmlElement("Settings")]
    public Dictionary<string, object>? Settings { get; set; }

    [JsonPropertyName("metadata")]
    [XmlElement("Metadata")]
    public Dictionary<string, object>? Metadata { get; set; }

    [JsonPropertyName("createdAt")]
    [XmlElement("CreatedAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("serializationVersion")]
    [XmlElement("SerializationVersion")]
    public string SerializationVersion { get; set; } = "1.0";
}