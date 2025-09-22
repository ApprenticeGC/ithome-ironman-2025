namespace GameConsole.UI.Profiles;

/// <summary>
/// Context information used for UI profile selection and activation.
/// </summary>
public class UIProfileContext
{
    /// <summary>
    /// Gets or sets the current console mode.
    /// </summary>
    public ConsoleMode Mode { get; set; }
    
    /// <summary>
    /// Gets or sets the current user role or permissions.
    /// </summary>
    public string? UserRole { get; set; }
    
    /// <summary>
    /// Gets or sets additional context properties.
    /// </summary>
    public IReadOnlyDictionary<string, object> Properties { get; set; } = 
        new Dictionary<string, object>();
    
    /// <summary>
    /// Gets or sets the current console dimensions.
    /// </summary>
    public ConsoleDimensions? Dimensions { get; set; }
    
    /// <summary>
    /// Gets a property value by key.
    /// </summary>
    /// <typeparam name="T">The type to cast the value to.</typeparam>
    /// <param name="key">The property key.</param>
    /// <returns>The property value, or default(T) if not found.</returns>
    public T? GetProperty<T>(string key)
    {
        if (Properties.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }
}

/// <summary>
/// Represents console dimensions for layout calculations.
/// </summary>
public class ConsoleDimensions
{
    /// <summary>
    /// Gets or sets the console width in characters.
    /// </summary>
    public int Width { get; set; }
    
    /// <summary>
    /// Gets or sets the console height in characters.
    /// </summary>
    public int Height { get; set; }
    
    /// <summary>
    /// Initializes a new instance of the ConsoleDimensions class.
    /// </summary>
    /// <param name="width">The console width.</param>
    /// <param name="height">The console height.</param>
    public ConsoleDimensions(int width, int height)
    {
        Width = width;
        Height = height;
    }
}