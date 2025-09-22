using GameConsole.UI.Core;
using Microsoft.Extensions.Logging;

namespace GameConsole.UI.Services;

/// <summary>
/// Custom Terminal User Interface (TUI) profile implementation.
/// Provides TUI-optimized behaviors and conventions following the TUI-first architecture.
/// </summary>
public class CustomTUIProfile : BaseUIProfile
{
    /// <summary>
    /// Initializes a new instance of the CustomTUIProfile class.
    /// </summary>
    /// <param name="configuration">The profile configuration.</param>
    /// <param name="logger">The logger instance.</param>
    public CustomTUIProfile(UIProfileConfiguration configuration, ILogger logger)
        : base(configuration, logger)
    {
    }

    protected override async Task OnActivateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Applying Custom TUI profile settings");
        
        // Set TUI-specific default configurations
        Configuration.Properties["ColorScheme"] = GetConfigurationValue("ColorScheme", "DefaultTerminal");
        Configuration.Properties["BorderStyle"] = GetConfigurationValue("BorderStyle", "Single");
        Configuration.Properties["KeyNavigationEnabled"] = GetConfigurationValue("KeyNavigationEnabled", true);
        Configuration.Properties["MouseSupportEnabled"] = GetConfigurationValue("MouseSupportEnabled", true);
        Configuration.Properties["VirtualTerminalEnabled"] = GetConfigurationValue("VirtualTerminalEnabled", true);
        Configuration.Properties["RefreshRate"] = GetConfigurationValue("RefreshRate", 30);
        Configuration.Properties["BufferSize"] = GetConfigurationValue("BufferSize", "120x40");
        
        await base.OnActivateAsync(cancellationToken);
    }

    protected override UIProfileValidationResult OnValidate()
    {
        var warnings = new List<string>();

        // Validate TUI-specific configuration
        var colorScheme = GetConfigurationValue<string>("ColorScheme", "DefaultTerminal");
        var validColorSchemes = new[] { "DefaultTerminal", "Dark", "Light", "HighContrast", "Solarized" };
        if (!validColorSchemes.Contains(colorScheme))
        {
            warnings.Add($"ColorScheme '{colorScheme}' is not a recognized TUI color scheme");
        }

        var borderStyle = GetConfigurationValue<string>("BorderStyle", "Single");
        var validBorderStyles = new[] { "Single", "Double", "Rounded", "Thick", "None" };
        if (!validBorderStyles.Contains(borderStyle))
        {
            warnings.Add($"BorderStyle '{borderStyle}' is not a recognized TUI border style");
        }

        var refreshRate = GetConfigurationValue<int>("RefreshRate", 30);
        if (refreshRate < 1 || refreshRate > 120)
        {
            warnings.Add("RefreshRate should be between 1 and 120 FPS for optimal TUI performance");
        }

        var bufferSize = GetConfigurationValue<string>("BufferSize", "120x40");
        if (!bufferSize.Contains('x'))
        {
            warnings.Add("BufferSize should be in format 'widthxheight'");
        }

        if (warnings.Count > 0)
        {
            return UIProfileValidationResult.SuccessWithWarnings(warnings.ToArray());
        }

        return base.OnValidate();
    }
}