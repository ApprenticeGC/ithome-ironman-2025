using Microsoft.Extensions.Configuration;

namespace GameConsole.Configuration.Core;

/// <summary>
/// Represents the context for configuration operations, providing environment
/// and application-specific information for configuration resolution.
/// </summary>
public class ConfigurationContext
{
    /// <summary>
    /// Gets the current environment name (Development, Staging, Production, etc.).
    /// </summary>
    public required string Environment { get; init; }
    
    /// <summary>
    /// Gets the application mode (Game, Editor, etc.).
    /// </summary>
    public required string Mode { get; init; }
    
    /// <summary>
    /// Gets additional context properties for configuration resolution.
    /// </summary>
    public Dictionary<string, object?> Properties { get; init; } = new();
}

/// <summary>
/// Represents the result of a configuration validation operation.
/// </summary>
public class ConfigurationValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the configuration is valid.
    /// </summary>
    public required bool IsValid { get; init; }
    
    /// <summary>
    /// Gets the validation errors, if any.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    
    /// <summary>
    /// Gets validation warnings, if any.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Configuration change notification event arguments.
/// </summary>
public class ConfigurationChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the configuration section that changed.
    /// </summary>
    public required string SectionPath { get; init; }
    
    /// <summary>
    /// Gets the previous configuration.
    /// </summary>
    public IConfiguration? PreviousConfiguration { get; init; }
    
    /// <summary>
    /// Gets the new configuration.
    /// </summary>
    public required IConfiguration NewConfiguration { get; init; }
}