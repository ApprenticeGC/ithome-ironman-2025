using Microsoft.Extensions.Logging;

namespace GameConsole.UI.Profiles;

/// <summary>
/// Abstract base class for UI profiles providing common functionality.
/// </summary>
public abstract class UIProfile : IUIProfile
{
    protected readonly ILogger? _logger;

    /// <inheritdoc />
    public string Name { get; protected set; } = string.Empty;

    /// <inheritdoc />
    public ConsoleMode TargetMode { get; protected set; }

    /// <inheritdoc />
    public UIProfileMetadata Metadata { get; protected set; } = new();

    /// <summary>
    /// Initializes a new instance of the UIProfile class.
    /// </summary>
    /// <param name="name">Profile name.</param>
    /// <param name="targetMode">Target console mode.</param>
    /// <param name="logger">Optional logger instance.</param>
    protected UIProfile(string name, ConsoleMode targetMode, ILogger? logger = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        TargetMode = targetMode;
        _logger = logger;
    }

    /// <inheritdoc />
    public abstract CommandSet GetCommandSet();

    /// <inheritdoc />
    public abstract LayoutConfiguration GetLayoutConfiguration();

    /// <inheritdoc />
    public abstract IReadOnlyDictionary<string, string> GetServiceProviderConfiguration();

    /// <inheritdoc />
    public virtual ProfileValidationResult Validate()
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // Validate name
        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Profile name cannot be empty");

        // Validate command set
        var commandSet = GetCommandSet();
        if (commandSet.Commands.Count == 0)
            warnings.Add("Profile has no commands defined");

        // Validate layout
        var layout = GetLayoutConfiguration();
        if (!layout.IsValid())
            errors.Add("Layout configuration is invalid");

        // Validate service provider configuration
        var serviceConfig = GetServiceProviderConfiguration();
        if (serviceConfig.Count == 0)
            warnings.Add("Profile has no service provider configurations");

        return errors.Count == 0 
            ? ProfileValidationResult.Success() 
            : ProfileValidationResult.Failed(errors, warnings);
    }

    /// <inheritdoc />
    public virtual async Task OnActivatedAsync(IUIProfile? previousProfile, CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Activating UI profile: {ProfileName} (mode: {TargetMode})", Name, TargetMode);
        
        // Default implementation - subclasses can override for specific behavior
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual async Task OnDeactivatedAsync(IUIProfile? nextProfile, CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Deactivating UI profile: {ProfileName}", Name);
        
        // Default implementation - subclasses can override for specific behavior
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual IUIProfile CreateVariant(string name, ProfileModifications modifications)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Variant name cannot be empty", nameof(name));

        return new VariantProfile(this, name, modifications, _logger);
    }

    /// <summary>
    /// Creates a default command set with common commands.
    /// Subclasses can call this and then customize the result.
    /// </summary>
    /// <returns>Default command set.</returns>
    protected virtual CommandSet CreateDefaultCommandSet()
    {
        var commandSet = new CommandSet();

        // Add common commands
        commandSet.AddCommand("exit", new CommandDefinition 
        { 
            Category = "System", 
            Description = "Exit the application",
            Priority = 1000,
            KeyboardShortcuts = new[] { "Alt+F4", "Ctrl+Q" }
        });

        commandSet.AddCommand("help", new CommandDefinition 
        { 
            Category = "System", 
            Description = "Show help information",
            Priority = 999,
            KeyboardShortcuts = new[] { "F1", "Ctrl+?" }
        });

        return commandSet;
    }

    /// <summary>
    /// Creates a default layout configuration.
    /// Subclasses can call this and then customize the result.
    /// </summary>
    /// <returns>Default layout configuration.</returns>
    protected virtual LayoutConfiguration CreateDefaultLayout()
    {
        return new LayoutConfiguration
        {
            Theme = new ThemeConfiguration
            {
                ColorScheme = "Dark",
                FontFamily = "Consolas",
                FontSize = 12
            },
            Window = new WindowConfiguration
            {
                Width = 1200,
                Height = 800,
                IsResizable = true
            },
            Navigation = new NavigationConfiguration
            {
                ShowMenuBar = true,
                ShowToolbar = true,
                ShowStatusBar = true
            }
        };
    }
}

/// <summary>
/// Internal implementation for profile variants created through CreateVariant.
/// </summary>
internal class VariantProfile : UIProfile
{
    private readonly IUIProfile _baseProfile;
    private readonly ProfileModifications _modifications;

    public VariantProfile(IUIProfile baseProfile, string name, ProfileModifications modifications, ILogger? logger = null)
        : base(name, baseProfile.TargetMode, logger)
    {
        _baseProfile = baseProfile;
        _modifications = modifications;

        // Apply metadata override if provided
        if (modifications.MetadataOverride != null)
        {
            Metadata = modifications.MetadataOverride;
        }
        else
        {
            // Create modified metadata based on base
            var baseMeta = baseProfile.Metadata;
            Metadata = new UIProfileMetadata
            {
                Version = baseMeta.Version,
                Author = baseMeta.Author,
                Description = $"Variant of {baseProfile.Name}: {baseMeta.Description}",
                Tags = baseMeta.Tags,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                IsBuiltIn = false,
                Priority = baseMeta.Priority,
                CompatibleModes = baseMeta.CompatibleModes
            };
        }
    }

    public override CommandSet GetCommandSet()
    {
        var baseCommandSet = _baseProfile.GetCommandSet();
        var modifiedCommandSet = new CommandSet();

        // Copy all base commands
        foreach (var (name, definition) in baseCommandSet.Commands)
        {
            modifiedCommandSet.AddCommand(name, definition);
        }

        // Apply command changes
        foreach (var (name, definition) in _modifications.CommandChanges)
        {
            modifiedCommandSet.AddCommand(name, definition);
        }

        return modifiedCommandSet;
    }

    public override LayoutConfiguration GetLayoutConfiguration()
    {
        return _modifications.LayoutOverride ?? _baseProfile.GetLayoutConfiguration();
    }

    public override IReadOnlyDictionary<string, string> GetServiceProviderConfiguration()
    {
        var baseConfig = _baseProfile.GetServiceProviderConfiguration();
        
        if (_modifications.ServiceProviderChanges.Count == 0)
            return baseConfig;

        var modifiedConfig = new Dictionary<string, string>(baseConfig);
        
        // Apply service provider changes
        foreach (var (key, value) in _modifications.ServiceProviderChanges)
        {
            modifiedConfig[key] = value;
        }

        return modifiedConfig.AsReadOnly();
    }
}