using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace GameConsole.AI.Local;

/// <summary>
/// AI resource manager for GPU/CPU allocation and monitoring.
/// Optimizes resource usage and provides fallback mechanisms.
/// </summary>
internal sealed class AIResourceManagerService : IAIResourceManager, IAsyncDisposable
{
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, ResourceAllocation> _allocations = new();
    private readonly PerformanceCounter? _cpuCounter;
    private readonly Timer _monitoringTimer;
    
    private ResourceConstraints _constraints = new();
    private ResourceMetrics _currentUtilization = new();
    private long _totalAllocatedMemory = 0;
    private bool _disposed = false;

    public AIResourceManagerService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        try
        {
            // Initialize performance counters (Windows-specific)
            if (OperatingSystem.IsWindows())
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize performance counters");
        }

        // Start monitoring timer (every 5 seconds)
        _monitoringTimer = new Timer(UpdateResourceMetrics, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        
        _logger.LogDebug("AIResourceManager initialized");
    }

    public ResourceMetrics CurrentUtilization => _currentUtilization;
    public ResourceConstraints Constraints => _constraints;

    public async Task<ResourceAllocation> AllocateResourcesAsync(long requiredMemoryBytes, double estimatedDurationMs, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Requesting resource allocation: {MemoryMB}MB for {Duration}ms", 
            requiredMemoryBytes / (1024 * 1024), estimatedDurationMs);

        // Check if allocation would exceed constraints
        var potentialTotalMemory = Interlocked.Read(ref _totalAllocatedMemory) + requiredMemoryBytes;
        if (potentialTotalMemory > _constraints.MaxMemoryBytes)
        {
            // Try to free up memory by triggering GC and optimization
            await OptimizeAllocationAsync(cancellationToken);
            
            potentialTotalMemory = Interlocked.Read(ref _totalAllocatedMemory) + requiredMemoryBytes;
            if (potentialTotalMemory > _constraints.MaxMemoryBytes)
            {
                var exception = new InsufficientMemoryException(
                    $"Cannot allocate {requiredMemoryBytes / (1024 * 1024)}MB. " +
                    $"Would exceed maximum of {_constraints.MaxMemoryBytes / (1024 * 1024)}MB");
                _logger.LogError("Resource allocation failed: {Exception}", exception.Message);
                throw exception;
            }
        }

        // Check CPU utilization
        if (_currentUtilization.CpuUtilizationPercent > _constraints.MaxCpuUtilizationPercent)
        {
            _logger.LogWarning("CPU utilization ({Current}%) exceeds threshold ({Max}%)", 
                _currentUtilization.CpuUtilizationPercent, _constraints.MaxCpuUtilizationPercent);
            
            // Wait briefly for CPU to cool down
            await Task.Delay(100, cancellationToken);
        }

        // Create allocation
        var allocation = new ResourceAllocation
        {
            AllocatedMemoryBytes = requiredMemoryBytes
        };

        _allocations[allocation.Id] = allocation;
        Interlocked.Add(ref _totalAllocatedMemory, requiredMemoryBytes);

        _logger.LogDebug("Allocated resources: {AllocationId} with {MemoryMB}MB", 
            allocation.Id, requiredMemoryBytes / (1024 * 1024));

        return allocation;
    }

    public async Task ReleaseResourcesAsync(ResourceAllocation allocation, CancellationToken cancellationToken = default)
    {
        if (allocation == null) throw new ArgumentNullException(nameof(allocation));

        if (_allocations.TryRemove(allocation.Id, out var existingAllocation))
        {
            Interlocked.Add(ref _totalAllocatedMemory, -existingAllocation.AllocatedMemoryBytes);
            existingAllocation.IsActive = false;
            
            _logger.LogDebug("Released resources: {AllocationId} with {MemoryMB}MB", 
                allocation.Id, existingAllocation.AllocatedMemoryBytes / (1024 * 1024));
        }
        else
        {
            _logger.LogWarning("Attempted to release unknown allocation: {AllocationId}", allocation.Id);
        }

        await Task.CompletedTask;
    }

    public async Task MonitorResourcesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting resource monitoring");
        
        try
        {
            while (!cancellationToken.IsCancellationRequested && !_disposed)
            {
                UpdateResourceMetrics(null);
                
                // Check for resource pressure and trigger cleanup if needed
                if (_currentUtilization.MemoryUsageBytes > _constraints.MaxMemoryBytes * 0.8)
                {
                    _logger.LogInformation("Memory usage at {Usage}%, triggering cleanup", 
                        (_currentUtilization.MemoryUsageBytes * 100) / _constraints.MaxMemoryBytes);
                    
                    await OptimizeAllocationAsync(cancellationToken);
                }

                // Clean up expired allocations
                await CleanupExpiredAllocationsAsync(cancellationToken);
                
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in resource monitoring loop");
        }
        
        _logger.LogDebug("Resource monitoring stopped");
    }

    public async Task OptimizeAllocationAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting resource optimization");

        try
        {
            // Force garbage collection
            GC.Collect(2, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();

            // Clean up inactive allocations
            var inactiveAllocations = _allocations.Values.Where(a => !a.IsActive).ToList();
            foreach (var allocation in inactiveAllocations)
            {
                await ReleaseResourcesAsync(allocation, cancellationToken);
            }

            // Clean up old allocations (older than 1 hour)
            var oldAllocations = _allocations.Values
                .Where(a => DateTime.UtcNow - a.AllocatedAt > TimeSpan.FromHours(1))
                .ToList();

            foreach (var allocation in oldAllocations)
            {
                _logger.LogInformation("Releasing old allocation: {AllocationId} (age: {Age})", 
                    allocation.Id, DateTime.UtcNow - allocation.AllocatedAt);
                await ReleaseResourcesAsync(allocation, cancellationToken);
            }

            UpdateResourceMetrics(null);
            _logger.LogDebug("Resource optimization completed. Current memory usage: {MemoryMB}MB", 
                _currentUtilization.MemoryUsageBytes / (1024 * 1024));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during resource optimization");
        }
    }

    private void UpdateResourceMetrics(object? state)
    {
        try
        {
            var newMetrics = new ResourceMetrics();

            // Get memory usage
            newMetrics.MemoryUsageBytes = GC.GetTotalMemory(false);
            newMetrics.AvailableMemoryBytes = _constraints.MaxMemoryBytes - Interlocked.Read(ref _totalAllocatedMemory);

            // Get CPU usage
            try
            {
                if (_cpuCounter != null && OperatingSystem.IsWindows())
                {
                    // Performance counter requires a delay between calls for accurate reading
                    var cpuUsage = _cpuCounter.NextValue();
                    newMetrics.CpuUtilizationPercent = Math.Max(0, Math.Min(100, cpuUsage));
                }
                else
                {
                    // Fallback: use process CPU time (less accurate)
                    using var process = Process.GetCurrentProcess();
                    newMetrics.CpuUtilizationPercent = Math.Min(100, process.TotalProcessorTime.TotalMilliseconds / Environment.TickCount * 100);
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Error reading CPU metrics");
                newMetrics.CpuUtilizationPercent = 0;
            }

            // GPU usage would require platform-specific implementation
            newMetrics.GpuUtilizationPercent = 0;

            _currentUtilization = newMetrics;

            _logger.LogTrace("Resource metrics updated: CPU={Cpu}%, Memory={Memory}MB", 
                newMetrics.CpuUtilizationPercent, newMetrics.MemoryUsageBytes / (1024 * 1024));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error updating resource metrics");
        }
    }

    private async Task CleanupExpiredAllocationsAsync(CancellationToken cancellationToken)
    {
        var expiredAllocations = _allocations.Values
            .Where(a => DateTime.UtcNow - a.AllocatedAt > TimeSpan.FromMinutes(30))
            .ToList();

        foreach (var allocation in expiredAllocations)
        {
            _logger.LogInformation("Cleaning up expired allocation: {AllocationId}", allocation.Id);
            await ReleaseResourcesAsync(allocation, cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        _logger.LogDebug("Disposing AIResourceManager");

        try
        {
            _disposed = true;
            
            // Stop monitoring
            await _monitoringTimer.DisposeAsync();

            // Release all allocations
            var allAllocations = _allocations.Values.ToList();
            foreach (var allocation in allAllocations)
            {
                await ReleaseResourcesAsync(allocation, CancellationToken.None);
            }

            // Dispose performance counters
            _cpuCounter?.Dispose();

            _logger.LogDebug("AIResourceManager disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing AIResourceManager");
        }
    }
}