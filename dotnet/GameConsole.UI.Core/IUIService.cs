using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameConsole.Core.Abstractions;

namespace GameConsole.UI.Core;

/// <summary>
/// Core UI service for console interactions
/// </summary>
public interface IUIService : IService
{
    /// <summary>
    /// Current active console mode
    /// </summary>
    ConsoleMode CurrentMode { get; }

    /// <summary>
    /// Currently loaded UI profile
    /// </summary>
    UIProfile? ActiveProfile { get; }

    /// <summary>
    /// Available UI capabilities
    /// </summary>
    UICapabilities Capabilities { get; }

    /// <summary>
    /// Switch to a different console mode
    /// </summary>
    Task<bool> SwitchModeAsync(ConsoleMode mode);

    /// <summary>
    /// Load and activate a UI profile
    /// </summary>
    Task<bool> LoadProfileAsync(string profileName);

    /// <summary>
    /// Execute a UI command in the current context
    /// </summary>
    Task<UICommandResult> ExecuteCommandAsync(string command, UIContext context);

    /// <summary>
    /// Render UI content to the console
    /// </summary>
    Task RenderAsync(UIRenderRequest request);

    /// <summary>
    /// Get input from the user
    /// </summary>
    Task<string> GetInputAsync(UIInputRequest request);

    /// <summary>
    /// Display a message to the user
    /// </summary>
    Task DisplayMessageAsync(UIMessage message);

    /// <summary>
    /// Get all available profiles for the current mode
    /// </summary>
    Task<IEnumerable<string>> GetAvailableProfilesAsync(ConsoleMode? mode = null);

    /// <summary>
    /// Get profile information
    /// </summary>
    Task<UIProfileMetadata?> GetProfileMetadataAsync(string profileName);
}