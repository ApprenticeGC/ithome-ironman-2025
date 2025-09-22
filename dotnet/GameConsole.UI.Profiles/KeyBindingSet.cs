namespace GameConsole.UI.Profiles;

/// <summary>
/// Represents a key binding for a command.
/// </summary>
public class KeyBinding
{
    /// <summary>
    /// Gets or sets the command ID this binding executes.
    /// </summary>
    public string CommandId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the key combination (e.g., "Ctrl+S", "F1").
    /// </summary>
    public string Keys { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the description of this key binding.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets whether this binding is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// A collection of key bindings for a UI profile.
/// </summary>
public class KeyBindingSet
{
    private readonly Dictionary<string, KeyBinding> _bindings = new();
    
    /// <summary>
    /// Gets all key bindings in this set.
    /// </summary>
    public IReadOnlyCollection<KeyBinding> Bindings => _bindings.Values;
    
    /// <summary>
    /// Adds a key binding to this set.
    /// </summary>
    /// <param name="binding">The key binding to add.</param>
    public void Add(KeyBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        _bindings[binding.Keys] = binding;
    }
    
    /// <summary>
    /// Gets a key binding by its key combination.
    /// </summary>
    /// <param name="keys">The key combination.</param>
    /// <returns>The key binding if found, null otherwise.</returns>
    public KeyBinding? GetBinding(string keys)
    {
        return _bindings.TryGetValue(keys, out var binding) ? binding : null;
    }
    
    /// <summary>
    /// Gets all bindings for a specific command.
    /// </summary>
    /// <param name="commandId">The command ID.</param>
    /// <returns>Key bindings for the specified command.</returns>
    public IEnumerable<KeyBinding> GetBindingsForCommand(string commandId)
    {
        return _bindings.Values
            .Where(b => string.Equals(b.CommandId, commandId, StringComparison.OrdinalIgnoreCase));
    }
}