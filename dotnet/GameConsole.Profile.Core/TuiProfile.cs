namespace GameConsole.Profile.Core;

/// <summary>
/// TUI (Terminal User Interface) profile configuration.
/// This is the primary profile for console-based interactions, providing text-based UI capabilities.
/// </summary>
public class TuiProfile : IProfileConfiguration
{
    /// <inheritdoc />
    public string ProfileId => "tui";

    /// <inheritdoc />
    public string DisplayName => "Terminal User Interface (TUI)";

    /// <inheritdoc />
    public string Description => "Text-based user interface optimized for terminal/console interactions";

    /// <inheritdoc />
    public int Priority => 100; // Highest priority - TUI-first approach

    /// <inheritdoc />
    public Task<bool> IsSupported(CancellationToken cancellationToken = default)
    {
        // TUI is always supported as it's the base interface
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<IEnumerable<Type>> GetCapabilityProviders(CancellationToken cancellationToken = default)
    {
        // TUI providers would be implemented in separate service projects
        var providers = new List<Type>
        {
            // These would be actual types when TUI providers are implemented
            // typeof(ConsoleInputProvider),
            // typeof(ConsoleOutputProvider),
            // typeof(TerminalGraphicsProvider)
        };

        return Task.FromResult<IEnumerable<Type>>(providers);
    }

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, object>> GetConfigurationSettings(CancellationToken cancellationToken = default)
    {
        var settings = new Dictionary<string, object>
        {
            ["ui.mode"] = "terminal",
            ["graphics.mode"] = "text",
            ["input.method"] = "keyboard",
            ["output.colors"] = true,
            ["display.width"] = 80,
            ["display.height"] = 24
        };

        return Task.FromResult<IReadOnlyDictionary<string, object>>(settings);
    }
}