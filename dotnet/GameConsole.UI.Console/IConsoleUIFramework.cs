namespace GameConsole.UI.Console;

/// <summary>
/// Console UI framework interface for managing text-based UI components and rendering.
/// </summary>
public interface IConsoleUIFramework
{
    /// <summary>
    /// Creates a new console menu component.
    /// </summary>
    /// <param name="title">Menu title.</param>
    /// <param name="items">Menu items.</param>
    /// <returns>A new console menu instance.</returns>
    IConsoleMenu CreateMenu(string title, IEnumerable<string> items);

    /// <summary>
    /// Creates a new console table component.
    /// </summary>
    /// <param name="headers">Table column headers.</param>
    /// <returns>A new console table instance.</returns>
    IConsoleTable CreateTable(IEnumerable<string> headers);

    /// <summary>
    /// Creates a new console progress bar component.
    /// </summary>
    /// <param name="label">Progress bar label.</param>
    /// <param name="maxValue">Maximum progress value.</param>
    /// <returns>A new console progress bar instance.</returns>
    IConsoleProgressBar CreateProgressBar(string label, int maxValue);

    /// <summary>
    /// Creates a console text component with optional formatting.
    /// </summary>
    /// <param name="text">Text content.</param>
    /// <param name="style">Text style options.</param>
    /// <returns>A new console text component.</returns>
    IConsoleText CreateText(string text, ConsoleTextStyle? style = null);

    /// <summary>
    /// Gets all registered UI components.
    /// </summary>
    /// <returns>Collection of all UI components.</returns>
    IEnumerable<IConsoleComponent> GetComponents();

    /// <summary>
    /// Adds a component to the framework.
    /// </summary>
    /// <param name="component">Component to add.</param>
    void AddComponent(IConsoleComponent component);

    /// <summary>
    /// Removes a component from the framework.
    /// </summary>
    /// <param name="component">Component to remove.</param>
    /// <returns>True if component was removed, false otherwise.</returns>
    bool RemoveComponent(IConsoleComponent component);

    /// <summary>
    /// Clears all components from the framework.
    /// </summary>
    void ClearComponents();
}