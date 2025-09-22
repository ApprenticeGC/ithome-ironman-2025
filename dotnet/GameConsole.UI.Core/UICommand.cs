using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GameConsole.UI.Core;

/// <summary>
/// Represents a UI command available in a profile
/// </summary>
public class UICommand
{
    public UICommand(
        string name,
        string description,
        UICommandHandler handler,
        UICommandPriority priority = UICommandPriority.Normal,
        IReadOnlyList<string>? aliases = null,
        bool requiresConfirmation = false,
        ConsoleMode supportedModes = ConsoleMode.All)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Handler = handler ?? throw new ArgumentNullException(nameof(handler));
        Priority = priority;
        Aliases = aliases ?? new List<string>();
        RequiresConfirmation = requiresConfirmation;
        SupportedModes = supportedModes;
    }

    public string Name { get; }
    public string Description { get; }
    public UICommandHandler Handler { get; }
    public UICommandPriority Priority { get; }
    public IReadOnlyList<string> Aliases { get; }
    public bool RequiresConfirmation { get; }
    public ConsoleMode SupportedModes { get; }
}

/// <summary>
/// Handler delegate for UI commands
/// </summary>
public delegate Task<UICommandResult> UICommandHandler(UIContext context);

/// <summary>
/// Attribute for marking UI command methods
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class UICommandAttribute : Attribute
{
    public UICommandAttribute(string name, string description)
    {
        Name = name;
        Description = description;
    }

    public string Name { get; }
    public string Description { get; }
    public UICommandPriority Priority { get; set; } = UICommandPriority.Normal;
    public ConsoleMode SupportedModes { get; set; } = ConsoleMode.All;
    public bool RequiresConfirmation { get; set; } = false;
}