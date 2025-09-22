using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace GameConsole.UI.Core;

/// <summary>
/// UI context passed to profiles and commands
/// </summary>
public class UIContext
{
    public UIContext(string[] args, Dictionary<string, object> state, ConsoleMode currentMode, UIPreferences preferences)
    {
        Args = args ?? throw new ArgumentNullException(nameof(args));
        State = state ?? throw new ArgumentNullException(nameof(state));
        CurrentMode = currentMode;
        Preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
    }

    public string[] Args { get; }
    public Dictionary<string, object> State { get; }
    public ConsoleMode CurrentMode { get; }
    public UIPreferences Preferences { get; }
}

/// <summary>
/// User interface preferences
/// </summary>
public class UIPreferences
{
    public UIPreferences(bool useColor = true, int maxWidth = 120, int maxHeight = 40, string theme = "Default")
    {
        UseColor = useColor;
        MaxWidth = maxWidth;
        MaxHeight = maxHeight;
        Theme = theme ?? "Default";
    }

    public bool UseColor { get; }
    public int MaxWidth { get; }
    public int MaxHeight { get; }
    public string Theme { get; }
}

/// <summary>
/// Command set for a UI profile
/// </summary>
public class CommandSet
{
    public CommandSet(IReadOnlyList<UICommand> commands, IReadOnlyDictionary<string, string> aliases, string defaultCommand = "help")
    {
        Commands = commands ?? throw new ArgumentNullException(nameof(commands));
        Aliases = aliases ?? throw new ArgumentNullException(nameof(aliases));
        DefaultCommand = defaultCommand ?? "help";
    }

    public IReadOnlyList<UICommand> Commands { get; }
    public IReadOnlyDictionary<string, string> Aliases { get; }
    public string DefaultCommand { get; }
}

/// <summary>
/// Layout configuration for UI rendering
/// </summary>
public class LayoutConfiguration
{
    public LayoutConfiguration(LayoutType layout, int columns = 1, bool showStatusBar = true, bool showMenuBar = false, string statusFormat = "{mode} | {time}")
    {
        Layout = layout;
        Columns = columns;
        ShowStatusBar = showStatusBar;
        ShowMenuBar = showMenuBar;
        StatusFormat = statusFormat ?? "{mode} | {time}";
    }

    public LayoutType Layout { get; }
    public int Columns { get; }
    public bool ShowStatusBar { get; }
    public bool ShowMenuBar { get; }
    public string StatusFormat { get; }
}

/// <summary>
/// UI profile metadata
/// </summary>
public class UIProfileMetadata
{
    public UIProfileMetadata(string description = "", string version = "1.0.0", string author = "", bool isBuiltIn = false)
    {
        Description = description ?? "";
        Version = version ?? "1.0.0";
        Author = author ?? "";
        IsBuiltIn = isBuiltIn;
    }

    public string Description { get; }
    public string Version { get; }
    public string Author { get; }
    public bool IsBuiltIn { get; }
}

/// <summary>
/// Configuration for UI profiles
/// </summary>
public class UIConfiguration
{
    public UIConfiguration(UIPreferences preferences, Dictionary<string, object> settings, bool enableLogging = true, bool enableMetrics = false)
    {
        Preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        EnableLogging = enableLogging;
        EnableMetrics = enableMetrics;
    }

    public UIPreferences Preferences { get; }
    public Dictionary<string, object> Settings { get; }
    public bool EnableLogging { get; }
    public bool EnableMetrics { get; }
}

/// <summary>
/// Result of executing a UI command
/// </summary>
public class UICommandResult
{
    public UICommandResult(bool success, string message = "", Dictionary<string, object>? data = null, Exception? error = null)
    {
        Success = success;
        Message = message ?? "";
        Data = data;
        Error = error;
    }

    public bool Success { get; }
    public string Message { get; }
    public Dictionary<string, object>? Data { get; }
    public Exception? Error { get; }
}

/// <summary>
/// Request for rendering UI content
/// </summary>
public class UIRenderRequest
{
    public UIRenderRequest(string content, LayoutConfiguration layout, UIRenderOptions options)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        Layout = layout ?? throw new ArgumentNullException(nameof(layout));
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string Content { get; }
    public LayoutConfiguration Layout { get; }
    public UIRenderOptions Options { get; }
}

/// <summary>
/// Options for UI rendering
/// </summary>
public class UIRenderOptions
{
    public UIRenderOptions(bool clearScreen = false, bool useColor = true, int indentLevel = 0, string? prefix = null)
    {
        ClearScreen = clearScreen;
        UseColor = useColor;
        IndentLevel = indentLevel;
        Prefix = prefix;
    }

    public bool ClearScreen { get; }
    public bool UseColor { get; }
    public int IndentLevel { get; }
    public string? Prefix { get; }
}

/// <summary>
/// Request for user input
/// </summary>
public class UIInputRequest
{
    public UIInputRequest(string prompt, string? defaultValue = null, bool isPassword = false, bool isRequired = true, string? validationPattern = null)
    {
        Prompt = prompt ?? throw new ArgumentNullException(nameof(prompt));
        DefaultValue = defaultValue;
        IsPassword = isPassword;
        IsRequired = isRequired;
        ValidationPattern = validationPattern;
    }

    public string Prompt { get; }
    public string? DefaultValue { get; }
    public bool IsPassword { get; }
    public bool IsRequired { get; }
    public string? ValidationPattern { get; }
}

/// <summary>
/// UI message to display to user
/// </summary>
public class UIMessage
{
    public UIMessage(UIMessageType type, string content, string? title = null, bool requireConfirmation = false)
    {
        Type = type;
        Content = content ?? throw new ArgumentNullException(nameof(content));
        Title = title;
        RequireConfirmation = requireConfirmation;
    }

    public UIMessageType Type { get; }
    public string Content { get; }
    public string? Title { get; }
    public bool RequireConfirmation { get; }
}