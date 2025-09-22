using GameConsole.UI.Core;
using Microsoft.Extensions.Logging;

namespace GameConsole.UI.Services;

/// <summary>
/// Unity-style UI profile implementation.
/// Provides Unity-specific UI behaviors and conventions.
/// </summary>
public class UnityUIProfile : BaseUIProfile
{
    /// <summary>
    /// Initializes a new instance of the UnityUIProfile class.
    /// </summary>
    /// <param name="configuration">The profile configuration.</param>
    /// <param name="logger">The logger instance.</param>
    public UnityUIProfile(UIProfileConfiguration configuration, ILogger logger)
        : base(configuration, logger)
    {
    }

    protected override async Task OnActivateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Applying Unity UI profile settings");
        
        // Set Unity-specific default configurations
        Configuration.Properties["CanvasScalerMode"] = GetConfigurationValue("CanvasScalerMode", "ScaleWithScreenSize");
        Configuration.Properties["ReferenceResolution"] = GetConfigurationValue("ReferenceResolution", "1920x1080");
        Configuration.Properties["MatchWidthOrHeight"] = GetConfigurationValue("MatchWidthOrHeight", 0.5f);
        Configuration.Properties["UILayoutMode"] = GetConfigurationValue("UILayoutMode", "Immediate");
        
        await base.OnActivateAsync(cancellationToken);
    }

    protected override UIProfileValidationResult OnValidate()
    {
        var warnings = new List<string>();

        // Validate Unity-specific configuration
        var referenceResolution = GetConfigurationValue<string>("ReferenceResolution", "1920x1080");
        if (!referenceResolution.Contains('x'))
        {
            warnings.Add("ReferenceResolution should be in format 'widthxheight'");
        }

        var matchValue = GetConfigurationValue<float>("MatchWidthOrHeight", 0.5f);
        if (matchValue < 0 || matchValue > 1)
        {
            warnings.Add("MatchWidthOrHeight should be between 0 and 1");
        }

        if (warnings.Count > 0)
        {
            return UIProfileValidationResult.SuccessWithWarnings(warnings.ToArray());
        }

        return base.OnValidate();
    }
}