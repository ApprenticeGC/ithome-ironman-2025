using GameConsole.AI.Services;
using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace GameConsole.AI.Local;

/// <summary>
/// AI Resource Manager for GPU/CPU allocation and optimization.
/// Implements resource management and monitoring for AI inference operations.
/// </summary>
[Service("AI", "Resources", "Local")]
public class AIResourceManager : IResourceManagerCapability
{
    private readonly ILogger<AIResourceManager> _logger;
    private readonly ConcurrentDictionary<string, ResourceAllocation> _allocations = new();
    private readonly object _allocationLock = new();
    private long _totalAllocatedMemoryMB;
    private readonly long _maxSystemMemoryMB;
    
    public AIResourceManager(ILogger<AIResourceManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _maxSystemMemoryMB = GetSystemMemoryMB();
        
        _logger.LogDebug("Initialized AIResourceManager with {MaxMemoryMB}MB system memory", _maxSystemMemoryMB);
    }

    #region IResourceManagerCapability Implementation

    public async Task<bool> AllocateResourcesAsync(ResourceConfiguration config, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return false;

        await Task.Yield(); // Make truly async

        lock (_allocationLock)
        {
            // Check if we have enough memory available
            var availableMemory = _maxSystemMemoryMB - _totalAllocatedMemoryMB;
            if (config.MaxMemoryMB > availableMemory)
            {
                _logger.LogWarning("Insufficient memory for allocation. Requested: {RequestedMB}MB, Available: {AvailableMB}MB", 
                    config.MaxMemoryMB, availableMemory);
                return false;
            }

            var allocationId = Guid.NewGuid().ToString();
            var allocation = new ResourceAllocation(
                allocationId,
                config,
                DateTime.UtcNow,
                0 // Initial inference count
            );

            _allocations[allocationId] = allocation;
            _totalAllocatedMemoryMB += config.MaxMemoryMB;
            
            _logger.LogInformation("Allocated resources: {AllocationId}, Memory: {MemoryMB}MB, Device: {Device}", 
                allocationId, config.MaxMemoryMB, config.PreferredDevice);
            
            return true;
        }
    }

    public async Task ReleaseResourcesAsync(string modelId, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        var allocationsToRemove = new List<string>();
        
        lock (_allocationLock)
        {
            foreach (var kvp in _allocations)
            {
                if (kvp.Key.Contains(modelId) || kvp.Value.ModelId == modelId)
                {
                    allocationsToRemove.Add(kvp.Key);
                    _totalAllocatedMemoryMB -= kvp.Value.Config.MaxMemoryMB;
                    
                    _logger.LogInformation("Released resources for model: {ModelId}, Memory: {MemoryMB}MB", 
                        modelId, kvp.Value.Config.MaxMemoryMB);
                }
            }

            foreach (var allocationId in allocationsToRemove)
            {
                _allocations.TryRemove(allocationId, out _);
            }
        }

        await Task.CompletedTask;
    }

    public async Task<IEnumerable<ExecutionDevice>> GetAvailableDevicesAsync(CancellationToken cancellationToken = default)
    {
        var availableDevices = new List<ExecutionDevice> { ExecutionDevice.CPU };

        // Check for CUDA availability (simplified check)
        if (IsDeviceAvailable(ExecutionDevice.CUDA))
            availableDevices.Add(ExecutionDevice.CUDA);

        // Check for other devices (simplified)
        if (IsDeviceAvailable(ExecutionDevice.DirectML))
            availableDevices.Add(ExecutionDevice.DirectML);

        _logger.LogDebug("Available devices: {Devices}", string.Join(", ", availableDevices));
        
        return await Task.FromResult(availableDevices);
    }

    public async Task<ExecutionDevice> GetOptimalDeviceAsync(ResourceConfiguration config, CancellationToken cancellationToken = default)
    {
        var availableDevices = await GetAvailableDevicesAsync(cancellationToken);
        
        // Prefer the requested device if available
        if (availableDevices.Contains(config.PreferredDevice))
        {
            _logger.LogDebug("Using preferred device: {Device}", config.PreferredDevice);
            return config.PreferredDevice;
        }

        // Fall back to best available device
        var optimalDevice = availableDevices.Contains(ExecutionDevice.CUDA) ? ExecutionDevice.CUDA : ExecutionDevice.CPU;
        
        _logger.LogDebug("Falling back to optimal device: {Device}", optimalDevice);
        return optimalDevice;
    }

    #endregion

    #region ICapabilityProvider Implementation

    public async Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(new[] { typeof(IResourceManagerCapability) });
    }

    public async Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(typeof(T) == typeof(IResourceManagerCapability));
    }

    public async Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (typeof(T) == typeof(IResourceManagerCapability))
            return await Task.FromResult(this as T);
        
        return await Task.FromResult<T?>(null);
    }

    #endregion

    #region Resource Monitoring

    public ResourceStats GetCurrentStats()
    {
        var process = Process.GetCurrentProcess();
        var memoryUsedMB = process.WorkingSet64 / (1024 * 1024);
        var cpuUsage = GetCpuUsage();
        
        return new ResourceStats(
            MemoryUsedMB: memoryUsedMB,
            MemoryAvailableMB: _maxSystemMemoryMB - _totalAllocatedMemoryMB,
            CpuUsagePercent: cpuUsage,
            GpuUsagePercent: 0.0, // TODO: Implement GPU usage monitoring
            ActiveInferences: _allocations.Count,
            QueuedInferences: 0 // TODO: Implement inference queue monitoring
        );
    }

    #endregion

    #region Private Helpers

    private static long GetSystemMemoryMB()
    {
        try
        {
            var memoryInfo = GC.GetGCMemoryInfo();
            return memoryInfo.TotalAvailableMemoryBytes / (1024 * 1024);
        }
        catch
        {
            // Fallback to 4GB if unable to determine
            return 4096;
        }
    }

    private static bool IsDeviceAvailable(ExecutionDevice device)
    {
        // Simplified device detection - in a real implementation,
        // this would check for actual device availability
        return device switch
        {
            ExecutionDevice.CPU => true,
            ExecutionDevice.CUDA => Environment.GetEnvironmentVariable("CUDA_VISIBLE_DEVICES") != null,
            ExecutionDevice.DirectML => OperatingSystem.IsWindows(),
            _ => false
        };
    }

    private static double GetCpuUsage()
    {
        // Simplified CPU usage calculation
        // In a real implementation, this would use performance counters
        return Random.Shared.NextDouble() * 10; // Mock 0-10% usage
    }

    private record ResourceAllocation(
        string Id,
        ResourceConfiguration Config,
        DateTime AllocatedAt,
        int InferenceCount,
        string? ModelId = null
    );

    #endregion
}