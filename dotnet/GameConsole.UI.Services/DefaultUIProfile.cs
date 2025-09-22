using GameConsole.UI.Core;
using Microsoft.Extensions.Logging;

namespace GameConsole.UI.Services;

/// <summary>
/// Default UI profile implementation providing basic UI functionality.
/// This profile serves as a fallback when no other specific profile is suitable.
/// </summary>
public class DefaultUIProfile : BaseUIProfile
{
    /// <summary>
    /// Initializes a new instance of the DefaultUIProfile class.
    /// </summary>
    /// <param name="configuration">The profile configuration.</param>
    /// <param name="logger">The logger instance.</param>
    public DefaultUIProfile(UIProfileConfiguration configuration, ILogger logger)
        : base(configuration, logger)
    {
    }

    protected override async Task OnActivateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Applying Default UI profile settings");
        
        // Set basic default configurations
        Configuration.Properties["UIStyle"] = GetConfigurationValue("UIStyle", "Basic");
        Configuration.Properties["InteractionMode"] = GetConfigurationValue("InteractionMode", "Standard");
        Configuration.Properties["AnimationsEnabled"] = GetConfigurationValue("AnimationsEnabled", false);
        Configuration.Properties["HighContrastMode"] = GetConfigurationValue("HighContrastMode", false);
        
        await base.OnActivateAsync(cancellationToken);
    }

    protected override UIProfileValidationResult OnValidate()
    {
        // Default profile has minimal validation requirements
        return base.OnValidate();
    }
}