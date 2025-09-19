using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Collections.ObjectModel;

namespace GameConsole.UI.Configuration;

/// <summary>
/// Default implementation of IProfileConfiguration.
/// </summary>
internal sealed class ProfileConfiguration : IProfileConfiguration
{
    /// <summary>
    /// Initializes a new instance of the ProfileConfiguration class.
    /// </summary>
    /// <param name="id">The unique identifier for the profile.</param>
    /// <param name="name">The display name of the profile.</param>
    /// <param name="description">The description of the profile.</param>
    /// <param name="version">The version of the configuration schema.</param>
    /// <param name="environment">The target environment.</param>
    /// <param name="parentProfileId">The parent profile ID, if any.</param>
    /// <param name="configuration">The configuration data.</param>
    /// <param name="metadata">The configuration metadata.</param>
    public ProfileConfiguration(
        string id,
        string name,
        string description,
        Version version,
        string environment,
        string? parentProfileId,
        IConfiguration configuration,
        IReadOnlyDictionary<string, object> metadata)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Version = version ?? throw new ArgumentNullException(nameof(version));
        Environment = environment ?? throw new ArgumentNullException(nameof(environment));
        ParentProfileId = parentProfileId;
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string Description { get; }

    /// <inheritdoc />
    public Version Version { get; }

    /// <inheritdoc />
    public string Environment { get; }

    /// <inheritdoc />
    public string? ParentProfileId { get; }

    /// <inheritdoc />
    public IConfiguration Configuration { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> Metadata { get; }

    /// <inheritdoc />
    public async Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // Basic validation
        if (string.IsNullOrWhiteSpace(Id))
            errors.Add("Profile ID cannot be null or empty.");
        
        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Profile name cannot be null or empty.");

        if (string.IsNullOrWhiteSpace(Description))
            errors.Add("Profile description cannot be null or empty.");

        // Environment validation
        var validEnvironments = new[] { "Development", "Staging", "Production", "Test" };
        if (!validEnvironments.Contains(Environment))
            warnings.Add($"Environment '{Environment}' is not a standard environment. Valid values: {string.Join(", ", validEnvironments)}");

        // Version validation
        if (Version.Major < 1)
            errors.Add("Version major number must be at least 1.");

        // Configuration validation
        if (Configuration == null)
            errors.Add("Configuration cannot be null.");

        // Simulate async work
        await Task.Delay(1, cancellationToken);

        return errors.Count > 0 
            ? ValidationResult.Failure(errors.ToArray(), warnings.ToArray())
            : warnings.Count > 0 
                ? ValidationResult.SuccessWithWarnings(warnings.ToArray())
                : ValidationResult.Success();
    }

    /// <inheritdoc />
    public T GetSection<T>(string sectionPath) where T : class, new()
    {
        if (string.IsNullOrWhiteSpace(sectionPath))
            throw new ArgumentException("Section path cannot be null or empty.", nameof(sectionPath));

        var section = Configuration.GetSection(sectionPath);
        var instance = new T();
        
        // Manual binding since Bind() extension is not available
        foreach (var child in section.GetChildren())
        {
            var property = typeof(T).GetProperty(child.Key);
            if (property != null && property.CanWrite && child.Value != null)
            {
                try
                {
                    var convertedValue = Convert.ChangeType(child.Value, property.PropertyType);
                    property.SetValue(instance, convertedValue);
                }
                catch
                {
                    // Skip properties that cannot be converted
                }
            }
        }
        
        return instance;
    }
}