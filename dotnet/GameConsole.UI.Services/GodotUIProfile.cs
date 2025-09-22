using GameConsole.UI.Core;
using Microsoft.Extensions.Logging;

namespace GameConsole.UI.Services;

/// <summary>
/// Godot-style UI profile implementation.
/// Provides Godot-specific UI behaviors and conventions.
/// </summary>
public class GodotUIProfile : BaseUIProfile
{
    /// <summary>
    /// Initializes a new instance of the GodotUIProfile class.
    /// </summary>
    /// <param name="configuration">The profile configuration.</param>
    /// <param name="logger">The logger instance.</param>
    public GodotUIProfile(UIProfileConfiguration configuration, ILogger logger)
        : base(configuration, logger)
    {
    }

    protected override async Task OnActivateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Applying Godot UI profile settings");
        
        // Set Godot-specific default configurations
        Configuration.Properties["AnchorMode"] = GetConfigurationValue("AnchorMode", "Stretch");
        Configuration.Properties["MarginContainer"] = GetConfigurationValue("MarginContainer", true);
        Configuration.Properties["SceneTreeAutoAcceptQuit"] = GetConfigurationValue("SceneTreeAutoAcceptQuit", true);
        Configuration.Properties["UIScalingMode"] = GetConfigurationValue("UIScalingMode", "Viewport");
        Configuration.Properties["ThemeType"] = GetConfigurationValue("ThemeType", "Default");
        
        await base.OnActivateAsync(cancellationToken);
    }

    protected override UIProfileValidationResult OnValidate()
    {
        var warnings = new List<string>();

        // Validate Godot-specific configuration
        var anchorMode = GetConfigurationValue<string>("AnchorMode", "Stretch");
        var validAnchorModes = new[] { "Stretch", "TopLeft", "TopRight", "BottomLeft", "BottomRight", "Center" };
        if (!validAnchorModes.Contains(anchorMode))
        {
            warnings.Add($"AnchorMode '{anchorMode}' is not a recognized Godot anchor mode");
        }

        var scalingMode = GetConfigurationValue<string>("UIScalingMode", "Viewport");
        var validScalingModes = new[] { "Viewport", "Canvas", "Disabled" };
        if (!validScalingModes.Contains(scalingMode))
        {
            warnings.Add($"UIScalingMode '{scalingMode}' is not a recognized Godot scaling mode");
        }

        if (warnings.Count > 0)
        {
            return UIProfileValidationResult.SuccessWithWarnings(warnings.ToArray());
        }

        return base.OnValidate();
    }
}