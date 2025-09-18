namespace GameConsole.Plugins.Lifecycle;

/// <summary>
/// Metadata information about a plugin.
/// </summary>
public record PluginMetadata(
    string Id,
    string Name,
    Version Version,
    string Description,
    string AssemblyPath,
    IReadOnlyList<string> Dependencies
)
{
    /// <summary>
    /// Creates a new instance of PluginMetadata with default values.
    /// </summary>
    public static PluginMetadata Create(string id, string name, Version version, string assemblyPath) =>
        new(id, name, version, string.Empty, assemblyPath, Array.Empty<string>());
}

/// <summary>
/// Result of a plugin load operation.
/// </summary>
public record PluginLoadResult(
    bool Success,
    PluginMetadata? Metadata,
    string? ErrorMessage = null
);

/// <summary>
/// Result of a plugin validation operation.
/// </summary>
public record ValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors
)
{
    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ValidationResult Success() => new(true, Array.Empty<string>());

    /// <summary>
    /// Creates a failed validation result with errors.
    /// </summary>
    public static ValidationResult Failure(params string[] errors) => new(false, errors);
}

/// <summary>
/// Result of a plugin health check operation.
/// </summary>
public record HealthCheckResult(
    PluginHealth Health,
    string? Message = null,
    TimeSpan? ResponseTime = null
);

/// <summary>
/// Represents a loaded plugin with its metadata and current state.
/// </summary>
public record LoadedPlugin(
    PluginMetadata Metadata,
    PluginState State,
    DateTime LoadedAt,
    DateTime? LastHealthCheck = null,
    PluginHealth Health = PluginHealth.Unknown
);

/// <summary>
/// Event arguments for plugin lifecycle events.
/// </summary>
public record PluginLifecycleEventArgs(
    string PluginId,
    PluginState OldState,
    PluginState NewState,
    DateTime Timestamp,
    string? Message = null
);