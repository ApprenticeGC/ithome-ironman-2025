namespace GameConsole.ECS.Core;

/// <summary>
/// Statistics about component pooling for a specific component type.
/// </summary>
public sealed class ComponentPoolStats
{
    /// <summary>
    /// The component type these stats apply to.
    /// </summary>
    public Type ComponentType { get; }

    /// <summary>
    /// Current number of pooled instances.
    /// </summary>
    public int PooledCount { get; }

    /// <summary>
    /// Current number of active instances.
    /// </summary>
    public int ActiveCount { get; }

    /// <summary>
    /// Total number of instances created.
    /// </summary>
    public int TotalCreated { get; }

    /// <summary>
    /// Total number of instances returned to pool.
    /// </summary>
    public int TotalReturned { get; }

    /// <summary>
    /// Maximum pool size (0 for unlimited).
    /// </summary>
    public int MaxPoolSize { get; }

    /// <summary>
    /// Pool hit ratio (returned / created).
    /// </summary>
    public float HitRatio => TotalCreated > 0 ? (float)TotalReturned / TotalCreated : 0f;

    public ComponentPoolStats(Type componentType, int pooledCount, int activeCount, int totalCreated, int totalReturned, int maxPoolSize)
    {
        ComponentType = componentType ?? throw new ArgumentNullException(nameof(componentType));
        PooledCount = pooledCount;
        ActiveCount = activeCount;
        TotalCreated = totalCreated;
        TotalReturned = totalReturned;
        MaxPoolSize = maxPoolSize;
    }
}

/// <summary>
/// Overall performance statistics for an ECS world.
/// </summary>
public sealed class ECSPerformanceStats
{
    /// <summary>
    /// World identifier these stats belong to.
    /// </summary>
    public Guid WorldId { get; }

    /// <summary>
    /// Total number of update cycles measured.
    /// </summary>
    public long UpdateCycles { get; }

    /// <summary>
    /// Average time per update cycle in milliseconds.
    /// </summary>
    public double AverageUpdateTime { get; }

    /// <summary>
    /// Minimum update time in milliseconds.
    /// </summary>
    public double MinUpdateTime { get; }

    /// <summary>
    /// Maximum update time in milliseconds.
    /// </summary>
    public double MaxUpdateTime { get; }

    /// <summary>
    /// Current number of entities.
    /// </summary>
    public int EntityCount { get; }

    /// <summary>
    /// Current number of systems.
    /// </summary>
    public int SystemCount { get; }

    /// <summary>
    /// Total memory used by components in bytes.
    /// </summary>
    public long ComponentMemoryUsage { get; }

    /// <summary>
    /// Frames per second based on delta time.
    /// </summary>
    public double FramesPerSecond { get; }

    public ECSPerformanceStats(Guid worldId, long updateCycles, double averageUpdateTime, double minUpdateTime, double maxUpdateTime, 
        int entityCount, int systemCount, long componentMemoryUsage, double framesPerSecond)
    {
        WorldId = worldId;
        UpdateCycles = updateCycles;
        AverageUpdateTime = averageUpdateTime;
        MinUpdateTime = minUpdateTime;
        MaxUpdateTime = maxUpdateTime;
        EntityCount = entityCount;
        SystemCount = systemCount;
        ComponentMemoryUsage = componentMemoryUsage;
        FramesPerSecond = framesPerSecond;
    }
}

/// <summary>
/// Performance statistics for a specific system.
/// </summary>
public sealed class SystemPerformanceStats
{
    /// <summary>
    /// The system these stats belong to.
    /// </summary>
    public Type SystemType { get; }

    /// <summary>
    /// Total number of update calls.
    /// </summary>
    public long UpdateCalls { get; }

    /// <summary>
    /// Average execution time per update in milliseconds.
    /// </summary>
    public double AverageExecutionTime { get; }

    /// <summary>
    /// Minimum execution time in milliseconds.
    /// </summary>
    public double MinExecutionTime { get; }

    /// <summary>
    /// Maximum execution time in milliseconds.
    /// </summary>
    public double MaxExecutionTime { get; }

    /// <summary>
    /// Total execution time across all updates in milliseconds.
    /// </summary>
    public double TotalExecutionTime { get; }

    /// <summary>
    /// System priority.
    /// </summary>
    public int Priority { get; }

    /// <summary>
    /// Whether this system can execute in parallel.
    /// </summary>
    public bool CanExecuteInParallel { get; }

    public SystemPerformanceStats(Type systemType, long updateCalls, double averageExecutionTime, double minExecutionTime, 
        double maxExecutionTime, double totalExecutionTime, int priority, bool canExecuteInParallel)
    {
        SystemType = systemType ?? throw new ArgumentNullException(nameof(systemType));
        UpdateCalls = updateCalls;
        AverageExecutionTime = averageExecutionTime;
        MinExecutionTime = minExecutionTime;
        MaxExecutionTime = maxExecutionTime;
        TotalExecutionTime = totalExecutionTime;
        Priority = priority;
        CanExecuteInParallel = canExecuteInParallel;
    }
}

/// <summary>
/// Supported serialization formats for ECS world persistence.
/// </summary>
public enum SerializationFormat
{
    /// <summary>
    /// Binary format (fast, compact).
    /// </summary>
    Binary,

    /// <summary>
    /// JSON format (human readable, slower).
    /// </summary>
    Json,

    /// <summary>
    /// MessagePack format (fast, compact, cross-platform).
    /// </summary>
    MessagePack
}