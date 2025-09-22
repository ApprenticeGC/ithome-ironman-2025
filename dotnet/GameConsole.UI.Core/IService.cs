using GameConsole.Core.Abstractions;

namespace GameConsole.UI.Core;

/// <summary>
/// Core UI service interface for console-based user interface operations.
/// Provides menu display, message output, and basic console interaction capabilities.
/// </summary>
public interface IService : GameConsole.Core.Abstractions.IService
{
    /// <summary>
    /// Displays a message in the console UI.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the display operation.</returns>
    Task DisplayMessageAsync(UIMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Displays a menu and returns the selected item ID.
    /// </summary>
    /// <param name="menu">The menu to display.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The ID of the selected menu item.</returns>
    Task<string> DisplayMenuAsync(Menu menu, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the console display.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the clear operation.</returns>
    Task ClearDisplayAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Displays a prompt and waits for user input.
    /// </summary>
    /// <param name="prompt">The prompt message to display.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The user's input response.</returns>
    Task<string> PromptInputAsync(string prompt, CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for advanced message formatting and styling.
/// </summary>
public interface IMessageFormattingCapability : ICapabilityProvider
{
    /// <summary>
    /// Formats a message with color and styling based on message type.
    /// </summary>
    /// <param name="message">The message to format.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The formatted message with styling information.</returns>
    Task<string> FormatMessageAsync(UIMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies custom styling to text.
    /// </summary>
    /// <param name="text">The text to style.</param>
    /// <param name="foregroundColor">Foreground color name (e.g., "Red", "Green").</param>
    /// <param name="backgroundColor">Background color name (optional).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The styled text.</returns>
    Task<string> ApplyStyleAsync(string text, string foregroundColor, string? backgroundColor = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for menu navigation and keyboard shortcuts.
/// </summary>
public interface IMenuNavigationCapability : ICapabilityProvider
{
    /// <summary>
    /// Enables keyboard navigation for menu selection.
    /// </summary>
    /// <param name="menu">The menu to enable navigation for.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the navigation setup.</returns>
    Task EnableKeyboardNavigationAsync(Menu menu, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets up keyboard shortcuts for menu items.
    /// </summary>
    /// <param name="shortcuts">Dictionary mapping keys to menu item IDs.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the shortcut setup.</returns>
    Task SetupShortcutsAsync(Dictionary<string, string> shortcuts, CancellationToken cancellationToken = default);
}