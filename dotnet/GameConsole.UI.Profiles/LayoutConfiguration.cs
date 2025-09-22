namespace GameConsole.UI.Profiles;

/// <summary>
/// Represents the type of UI panel.
/// </summary>
public enum PanelType
{
    /// <summary>
    /// Main console panel for command input/output.
    /// </summary>
    Console,
    
    /// <summary>
    /// Inspector panel for object properties.
    /// </summary>
    Inspector,
    
    /// <summary>
    /// Hierarchy panel for scene structure.
    /// </summary>
    Hierarchy,
    
    /// <summary>
    /// Asset browser panel for asset management.
    /// </summary>
    AssetBrowser,
    
    /// <summary>
    /// Performance monitoring panel.
    /// </summary>
    Performance,
    
    /// <summary>
    /// Debug panel for debugging tools.
    /// </summary>
    Debug
}

/// <summary>
/// Configuration for a UI panel.
/// </summary>
public class PanelConfiguration
{
    /// <summary>
    /// Gets or sets the name of the panel.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the panel.
    /// </summary>
    public PanelType Type { get; set; }

    /// <summary>
    /// Gets or sets the position of the panel.
    /// </summary>
    public Position Position { get; set; }

    /// <summary>
    /// Gets or sets the size of the panel.
    /// </summary>
    public Size Size { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the panel is visible.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets custom properties for the panel.
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Configuration for key bindings.
/// </summary>
public class KeyBindingSet
{
    private readonly Dictionary<string, string> _bindings = new();

    /// <summary>
    /// Gets the collection of key bindings.
    /// </summary>
    public IReadOnlyDictionary<string, string> Bindings => _bindings;

    /// <summary>
    /// Adds a key binding.
    /// </summary>
    /// <param name="key">The key combination.</param>
    /// <param name="command">The command to execute.</param>
    public void Add(string key, string command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        
        _bindings[key] = command;
    }

    /// <summary>
    /// Attempts to get the command for a key combination.
    /// </summary>
    /// <param name="key">The key combination.</param>
    /// <param name="command">The command if found.</param>
    /// <returns>True if a binding was found; otherwise, false.</returns>
    public bool TryGetCommand(string key, out string? command)
    {
        return _bindings.TryGetValue(key, out command);
    }
}

/// <summary>
/// Configuration for UI theme.
/// </summary>
public class ThemeConfiguration
{
    /// <summary>
    /// Gets or sets the primary color.
    /// </summary>
    public string PrimaryColor { get; set; } = "Blue";

    /// <summary>
    /// Gets or sets the background color.
    /// </summary>
    public string BackgroundColor { get; set; } = "Black";

    /// <summary>
    /// Gets or sets the text color.
    /// </summary>
    public string TextColor { get; set; } = "White";

    /// <summary>
    /// Gets or sets custom theme properties.
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Configuration for UI layout.
/// </summary>
public class LayoutConfiguration
{
    /// <summary>
    /// Gets or sets the collection of panel configurations.
    /// </summary>
    public IReadOnlyList<PanelConfiguration> Panels { get; set; } = Array.Empty<PanelConfiguration>();

    /// <summary>
    /// Gets or sets the key binding configuration.
    /// </summary>
    public KeyBindingSet KeyBindings { get; set; } = new();

    /// <summary>
    /// Gets or sets the theme configuration.
    /// </summary>
    public ThemeConfiguration Theme { get; set; } = new();

    /// <summary>
    /// Gets or sets custom layout properties.
    /// </summary>
    public Dictionary<string, object> CustomProperties { get; set; } = new();
}