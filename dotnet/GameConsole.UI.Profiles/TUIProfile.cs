using Microsoft.Extensions.Logging;

namespace GameConsole.UI.Profiles;

/// <summary>
/// Default Text-based User Interface (TUI) profile implementation.
/// This is the primary UI mode for the GameConsole framework.
/// </summary>
public class TUIProfile : BaseUIProfile
{
    public const string DefaultId = "tui-default";
    public const string DefaultName = "Text UI (Default)";
    public const string DefaultDescription = "Default text-based user interface suitable for console and terminal environments.";

    public TUIProfile(ILogger logger) 
        : base(DefaultId, DefaultName, DefaultDescription, UIMode.TUI, logger)
    {
        InitializeDefaultProperties();
    }

    public TUIProfile(string id, string name, string description, ILogger logger)
        : base(id, name, description, UIMode.TUI, logger)
    {
        InitializeDefaultProperties();
    }

    protected override Task OnActivateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Activating TUI profile with console-based rendering");
        
        // Configure TUI-specific settings
        SetProperty("renderer", "console");
        SetProperty("colorSupport", Console.IsErrorRedirected ? false : true);
        SetProperty("inputMethod", "keyboard");
        SetProperty("maxWidth", 120);
        SetProperty("maxHeight", 40);
        
        return Task.CompletedTask;
    }

    protected override Task OnDeactivateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deactivating TUI profile");
        return Task.CompletedTask;
    }

    private void InitializeDefaultProperties()
    {
        SetProperty("framework", "GameConsole");
        SetProperty("uiType", "TUI");
        SetProperty("supportsColor", true);
        SetProperty("supportsInput", true);
        SetProperty("defaultFont", "Consolas");
        SetProperty("backgroundColor", "Black");
        SetProperty("foregroundColor", "White");
    }
}