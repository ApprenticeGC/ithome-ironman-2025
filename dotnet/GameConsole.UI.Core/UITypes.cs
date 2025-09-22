using System;

namespace GameConsole.UI.Core;

/// <summary>
/// Supported console operating modes
/// </summary>
[Flags]
public enum ConsoleMode
{
    Game = 1,
    Editor = 2, 
    Debug = 4,
    All = Game | Editor | Debug
}

/// <summary>
/// UI capabilities supported by profiles
/// </summary>
[Flags]
public enum UICapabilities
{
    TextDisplay = 1 << 0,
    MenuNavigation = 1 << 1,
    FormInput = 1 << 2,
    TableDisplay = 1 << 3,
    TreeView = 1 << 4,
    KeyboardShortcuts = 1 << 5,
    MouseInteraction = 1 << 6,
    ColorDisplay = 1 << 7,
    ProgressIndicators = 1 << 8,
    StatusBar = 1 << 9
}

/// <summary>
/// Layout types for UI rendering
/// </summary>
public enum LayoutType
{
    SingleColumn,
    TwoColumn,
    ThreeColumn,
    Tabbed,
    Split
}

/// <summary>
/// Priority levels for UI commands
/// </summary>
public enum UICommandPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// UI message types
/// </summary>
public enum UIMessageType
{
    Info,
    Warning,
    Error,
    Success,
    Debug
}