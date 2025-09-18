namespace GameConsole.AI.Local;

/// <summary>
/// Resource requirements for AI operations.
/// </summary>
public class ResourceRequirements
{
    /// <summary>
    /// Gets or sets the required CPU usage percentage.
    /// </summary>
    public int RequiredCpuPercent { get; set; }

    /// <summary>
    /// Gets or sets the required memory in MB.
    /// </summary>
    public long RequiredMemoryMB { get; set; }

    /// <summary>
    /// Gets or sets the required GPU memory in MB.
    /// </summary>
    public long RequiredGpuMemoryMB { get; set; }

    /// <summary>
    /// Gets or sets the preferred execution provider.
    /// </summary>
    public ExecutionProvider PreferredProvider { get; set; } = ExecutionProvider.CPU;

    /// <summary>
    /// Gets or sets the maximum execution time in milliseconds.
    /// </summary>
    public int MaxExecutionTimeMs { get; set; } = 30000;

    /// <summary>
    /// Gets or sets the priority for resource allocation.
    /// </summary>
    public InferencePriority Priority { get; set; } = InferencePriority.Normal;
}

/// <summary>
/// Allocated resources for AI operations.
/// </summary>
public class AllocatedResources
{
    /// <summary>
    /// Gets or sets the unique identifier for the resource allocation.
    /// </summary>
    public string AllocationId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the allocated CPU percentage.
    /// </summary>
    public int AllocatedCpuPercent { get; set; }

    /// <summary>
    /// Gets or sets the allocated memory in MB.
    /// </summary>
    public long AllocatedMemoryMB { get; set; }

    /// <summary>
    /// Gets or sets the allocated GPU memory in MB.
    /// </summary>
    public long AllocatedGpuMemoryMB { get; set; }

    /// <summary>
    /// Gets or sets the execution provider assigned.
    /// </summary>
    public ExecutionProvider AssignedProvider { get; set; }

    /// <summary>
    /// Gets or sets the allocation timestamp.
    /// </summary>
    public DateTimeOffset AllocatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets whether the allocation is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Current resource usage statistics.
/// </summary>
public class ResourceUsageStatistics
{
    /// <summary>
    /// Gets or sets the current CPU usage percentage.
    /// </summary>
    public double CurrentCpuUsage { get; set; }

    /// <summary>
    /// Gets or sets the current memory usage in MB.
    /// </summary>
    public long CurrentMemoryUsageMB { get; set; }

    /// <summary>
    /// Gets or sets the current GPU usage percentage.
    /// </summary>
    public double CurrentGpuUsage { get; set; }

    /// <summary>
    /// Gets or sets the current GPU memory usage in MB.
    /// </summary>
    public long CurrentGpuMemoryUsageMB { get; set; }

    /// <summary>
    /// Gets or sets the available memory in MB.
    /// </summary>
    public long AvailableMemoryMB { get; set; }

    /// <summary>
    /// Gets or sets the available GPU memory in MB.
    /// </summary>
    public long AvailableGpuMemoryMB { get; set; }

    /// <summary>
    /// Gets or sets the number of active allocations.
    /// </summary>
    public int ActiveAllocations { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when statistics were collected.
    /// </summary>
    public DateTimeOffset CollectedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Resource capabilities of the system.
/// </summary>
public class ResourceCapabilities
{
    /// <summary>
    /// Gets or sets the total CPU cores available.
    /// </summary>
    public int TotalCpuCores { get; set; }

    /// <summary>
    /// Gets or sets the total system memory in MB.
    /// </summary>
    public long TotalMemoryMB { get; set; }

    /// <summary>
    /// Gets or sets whether GPU is available.
    /// </summary>
    public bool HasGpu { get; set; }

    /// <summary>
    /// Gets or sets the total GPU memory in MB.
    /// </summary>
    public long TotalGpuMemoryMB { get; set; }

    /// <summary>
    /// Gets or sets the GPU device name.
    /// </summary>
    public string GpuDeviceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the supported execution providers.
    /// </summary>
    public IEnumerable<ExecutionProvider> SupportedProviders { get; set; } = [];

    /// <summary>
    /// Gets or sets the maximum concurrent operations supported.
    /// </summary>
    public int MaxConcurrentOperations { get; set; }
}

/// <summary>
/// Resource limits for system protection.
/// </summary>
public class ResourceLimits
{
    /// <summary>
    /// Gets or sets the maximum CPU usage percentage allowed.
    /// </summary>
    public int MaxCpuUsagePercent { get; set; } = 80;

    /// <summary>
    /// Gets or sets the maximum memory usage in MB.
    /// </summary>
    public long MaxMemoryUsageMB { get; set; }

    /// <summary>
    /// Gets or sets the maximum GPU memory usage in MB.
    /// </summary>
    public long MaxGpuMemoryUsageMB { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of concurrent allocations.
    /// </summary>
    public int MaxConcurrentAllocations { get; set; } = 10;

    /// <summary>
    /// Gets or sets the emergency threshold for triggering cleanup.
    /// </summary>
    public double EmergencyThresholdPercent { get; set; } = 90.0;
}