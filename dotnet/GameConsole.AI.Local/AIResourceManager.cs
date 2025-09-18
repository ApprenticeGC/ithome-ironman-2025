using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace GameConsole.AI.Local;

/// <summary>
/// Implementation of AI resource manager for GPU/CPU allocation and optimization.
/// </summary>
public class AIResourceManager : IAIResourceManager
{
    private readonly ILogger<AIResourceManager> _logger;
    private readonly ConcurrentDictionary<string, AllocatedResources> _allocations = new();
    private ResourceLimits _resourceLimits = new();
    private readonly object _lockObject = new();

    /// <summary>
    /// Initializes a new instance of the AIResourceManager class.
    /// </summary>
    /// <param name="logger">Logger for the resource manager.</param>
    public AIResourceManager(ILogger<AIResourceManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<AllocatedResources> AllocateResourcesAsync(ResourceRequirements requirements, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Allocating resources: CPU {CpuPercent}%, Memory {MemoryMB}MB, GPU Memory {GpuMemoryMB}MB", 
            requirements.RequiredCpuPercent, requirements.RequiredMemoryMB, requirements.RequiredGpuMemoryMB);

        // Check if requirements can be satisfied
        if (!await CanSatisfyRequirementsAsync(requirements, cancellationToken))
        {
            throw new InvalidOperationException("Insufficient resources to satisfy allocation request");
        }

        var allocation = new AllocatedResources
        {
            AllocatedCpuPercent = Math.Min(requirements.RequiredCpuPercent, _resourceLimits.MaxCpuUsagePercent),
            AllocatedMemoryMB = Math.Min(requirements.RequiredMemoryMB, _resourceLimits.MaxMemoryUsageMB),
            AllocatedGpuMemoryMB = Math.Min(requirements.RequiredGpuMemoryMB, _resourceLimits.MaxGpuMemoryUsageMB),
            AssignedProvider = requirements.PreferredProvider,
            AllocatedAt = DateTimeOffset.UtcNow,
            IsActive = true
        };

        _allocations.TryAdd(allocation.AllocationId, allocation);
        
        _logger.LogInformation("Resources allocated: {AllocationId} - CPU {CpuPercent}%, Memory {MemoryMB}MB", 
            allocation.AllocationId, allocation.AllocatedCpuPercent, allocation.AllocatedMemoryMB);

        await Task.CompletedTask;
        return allocation;
    }

    /// <inheritdoc />
    public async Task ReleaseResourcesAsync(string resourceId, CancellationToken cancellationToken = default)
    {
        if (_allocations.TryRemove(resourceId, out var allocation))
        {
            allocation.IsActive = false;
            _logger.LogInformation("Resources released: {AllocationId}", resourceId);
        }
        else
        {
            _logger.LogWarning("Attempted to release unknown resource allocation: {AllocationId}", resourceId);
        }

        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<ResourceUsageStatistics> GetResourceUsageAsync(CancellationToken cancellationToken = default)
    {
        var process = Process.GetCurrentProcess();
        var totalMemoryMB = GC.GetTotalMemory(false) / (1024 * 1024);
        
        // Calculate allocated resources
        var activeAllocations = _allocations.Values.Where(a => a.IsActive).ToList();
        var totalAllocatedCpu = activeAllocations.Sum(a => a.AllocatedCpuPercent);
        var totalAllocatedMemory = activeAllocations.Sum(a => a.AllocatedMemoryMB);
        var totalAllocatedGpuMemory = activeAllocations.Sum(a => a.AllocatedGpuMemoryMB);

        var capabilities = await GetResourceCapabilitiesAsync(cancellationToken);

        return new ResourceUsageStatistics
        {
            CurrentCpuUsage = Math.Min(totalAllocatedCpu, 100),
            CurrentMemoryUsageMB = totalMemoryMB,
            CurrentGpuUsage = capabilities.HasGpu ? Math.Min(totalAllocatedGpuMemory / (double)capabilities.TotalGpuMemoryMB * 100, 100) : 0,
            CurrentGpuMemoryUsageMB = totalAllocatedGpuMemory,
            AvailableMemoryMB = Math.Max(0, capabilities.TotalMemoryMB - totalMemoryMB),
            AvailableGpuMemoryMB = Math.Max(0, capabilities.TotalGpuMemoryMB - totalAllocatedGpuMemory),
            ActiveAllocations = activeAllocations.Count,
            CollectedAt = DateTimeOffset.UtcNow
        };
    }

    /// <inheritdoc />
    public async Task OptimizeAllocationAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Optimizing resource allocation");

        // Clean up inactive allocations
        var inactiveAllocations = _allocations.Where(kvp => !kvp.Value.IsActive).ToList();
        foreach (var (key, _) in inactiveAllocations)
        {
            _allocations.TryRemove(key, out _);
        }

        // Trigger garbage collection to free memory
        if (_allocations.Count > 0)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        _logger.LogInformation("Resource optimization completed. Active allocations: {Count}", 
            _allocations.Count(kvp => kvp.Value.IsActive));

        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<ResourceCapabilities> GetResourceCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var totalMemoryMB = GetTotalSystemMemoryMB();
        var cpuCores = Environment.ProcessorCount;

        // Basic GPU detection - in a real implementation, you'd use proper GPU libraries
        var hasGpu = await HasGpuSupportAsync();
        var gpuMemoryMB = hasGpu ? GetEstimatedGpuMemoryMB() : 0;

        return new ResourceCapabilities
        {
            TotalCpuCores = cpuCores,
            TotalMemoryMB = totalMemoryMB,
            HasGpu = hasGpu,
            TotalGpuMemoryMB = gpuMemoryMB,
            GpuDeviceName = hasGpu ? "Detected GPU Device" : string.Empty,
            SupportedProviders = GetSupportedExecutionProviders(hasGpu),
            MaxConcurrentOperations = Math.Max(1, cpuCores / 2)
        };
    }

    /// <inheritdoc />
    public async Task SetResourceLimitsAsync(ResourceLimits limits, CancellationToken cancellationToken = default)
    {
        lock (_lockObject)
        {
            _resourceLimits = limits ?? throw new ArgumentNullException(nameof(limits));
        }

        _logger.LogInformation("Resource limits updated: CPU {CpuPercent}%, Memory {MemoryMB}MB, GPU Memory {GpuMemoryMB}MB", 
            limits.MaxCpuUsagePercent, limits.MaxMemoryUsageMB, limits.MaxGpuMemoryUsageMB);

        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<bool> CanSatisfyRequirementsAsync(ResourceRequirements requirements, CancellationToken cancellationToken = default)
    {
        var usage = await GetResourceUsageAsync(cancellationToken);
        var capabilities = await GetResourceCapabilitiesAsync(cancellationToken);

        // Check CPU availability
        var availableCpu = _resourceLimits.MaxCpuUsagePercent - usage.CurrentCpuUsage;
        if (requirements.RequiredCpuPercent > availableCpu)
        {
            _logger.LogWarning("Insufficient CPU: Required {Required}%, Available {Available}%", 
                requirements.RequiredCpuPercent, availableCpu);
            return false;
        }

        // Check memory availability
        if (requirements.RequiredMemoryMB > usage.AvailableMemoryMB)
        {
            _logger.LogWarning("Insufficient memory: Required {Required}MB, Available {Available}MB", 
                requirements.RequiredMemoryMB, usage.AvailableMemoryMB);
            return false;
        }

        // Check GPU memory availability if required
        if (requirements.RequiredGpuMemoryMB > 0)
        {
            if (!capabilities.HasGpu)
            {
                _logger.LogWarning("GPU memory required but no GPU available");
                return false;
            }

            if (requirements.RequiredGpuMemoryMB > usage.AvailableGpuMemoryMB)
            {
                _logger.LogWarning("Insufficient GPU memory: Required {Required}MB, Available {Available}MB", 
                    requirements.RequiredGpuMemoryMB, usage.AvailableGpuMemoryMB);
                return false;
            }
        }

        // Check allocation limits
        var activeAllocations = _allocations.Count(kvp => kvp.Value.IsActive);
        if (activeAllocations >= _resourceLimits.MaxConcurrentAllocations)
        {
            _logger.LogWarning("Maximum concurrent allocations reached: {Count}/{Max}", 
                activeAllocations, _resourceLimits.MaxConcurrentAllocations);
            return false;
        }

        return true;
    }

    private static long GetTotalSystemMemoryMB()
    {
        try
        {
            var gc = GC.GetGCMemoryInfo();
            return gc.TotalAvailableMemoryBytes / (1024 * 1024);
        }
        catch
        {
            // Fallback estimate
            return 8192; // 8GB default
        }
    }

    private static async Task<bool> HasGpuSupportAsync()
    {
        // Simple GPU detection - in production, use proper GPU libraries
        await Task.CompletedTask;
        
        // Check for common GPU environment variables
        var cudaPath = Environment.GetEnvironmentVariable("CUDA_PATH");
        var rocmPath = Environment.GetEnvironmentVariable("ROCM_PATH");
        
        return !string.IsNullOrEmpty(cudaPath) || !string.IsNullOrEmpty(rocmPath);
    }

    private static long GetEstimatedGpuMemoryMB()
    {
        // Conservative estimate - in production, query actual GPU memory
        return 4096; // 4GB default estimate
    }

    private static IEnumerable<ExecutionProvider> GetSupportedExecutionProviders(bool hasGpu)
    {
        var providers = new List<ExecutionProvider> { ExecutionProvider.CPU };

        if (hasGpu)
        {
            if (OperatingSystem.IsWindows())
            {
                providers.Add(ExecutionProvider.DirectML);
            }
            
            if (OperatingSystem.IsMacOS())
            {
                providers.Add(ExecutionProvider.CoreML);
            }
            
            // Add CUDA if available
            var cudaPath = Environment.GetEnvironmentVariable("CUDA_PATH");
            if (!string.IsNullOrEmpty(cudaPath))
            {
                providers.Add(ExecutionProvider.CUDA);
            }
        }

        return providers;
    }
}