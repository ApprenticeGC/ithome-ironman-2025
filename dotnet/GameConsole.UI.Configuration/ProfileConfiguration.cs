using Microsoft.Extensions.Configuration;

namespace GameConsole.UI.Configuration;

/// <summary>
/// Default implementation of IProfileConfiguration that provides profile configuration functionality.
/// </summary>
internal class ProfileConfiguration : IProfileConfiguration
{
    public string ProfileId { get; }
    public string Name { get; }
    public string Description { get; }
    public string Version { get; }
    public ProfileScope Scope { get; }
    public string Environment { get; }
    public string? ParentProfileId { get; }
    public IConfiguration Configuration { get; }
    public IReadOnlyDictionary<string, object> Metadata { get; }

    internal ProfileConfiguration(
        string profileId,
        string name,
        string description,
        string version,
        ProfileScope scope,
        string environment,
        string? parentProfileId,
        IConfiguration configuration,
        IReadOnlyDictionary<string, object> metadata)
    {
        ProfileId = profileId ?? throw new ArgumentNullException(nameof(profileId));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Version = version ?? throw new ArgumentNullException(nameof(version));
        Scope = scope;
        Environment = environment ?? throw new ArgumentNullException(nameof(environment));
        ParentProfileId = parentProfileId;
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
    }

    public T GetValue<T>(string key, T defaultValue = default!)
    {
        return Configuration.GetValue<T>(key) ?? defaultValue;
    }

    public bool HasKey(string key)
    {
        return Configuration.GetSection(key).Exists();
    }

    public async Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
    {
        var validator = new ConfigurationValidator();
        return await validator.ValidateAsync(this, cancellationToken);
    }
}