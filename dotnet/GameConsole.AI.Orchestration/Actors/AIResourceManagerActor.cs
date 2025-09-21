using Akka.Actor;
using Akka.Event;
using GameConsole.AI.Orchestration.Messages;
using GameConsole.AI.Orchestration.Services;

namespace GameConsole.AI.Orchestration.Actors;

/// <summary>
/// Actor responsible for managing AI resource allocation and optimization.
/// Handles resource scaling, health monitoring, and allocation optimization.
/// </summary>
public class AIResourceManagerActor : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly Dictionary<string, ResourcePool> _resourcePools = new();
    private readonly Dictionary<string, ResourceAllocationInfo> _activeAllocations = new();
    private readonly ResourceMetricsTracker _metricsTracker = new();
    private readonly Dictionary<string, ResourceHealthInfo> _healthStatus = new();

    public AIResourceManagerActor()
    {
        Receive<AllocateResources>(Handle);
        Receive<ReleaseResources>(Handle);
        Receive<GetResourceMetrics>(Handle);
        Receive<OptimizeResources>(Handle);
        Receive<AgentAvailable>(Handle);
        Receive<AgentUnavailable>(Handle);
        Receive<AgentHeartbeat>(Handle);
        Receive<MonitorResourceHealth>(Handle);
        Receive<ScaleResources>(Handle);
        Receive<ResourceHealthCheck>(Handle);

        // Initialize default resource pools
        InitializeResourcePools();

        // Start periodic health monitoring and optimization
        Context.System.Scheduler.ScheduleTellRepeatedly(
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(30),
            Self,
            new ResourceHealthCheck(),
            ActorRefs.NoSender);

        Context.System.Scheduler.ScheduleTellRepeatedly(
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(5),
            Self,
            new OptimizeResources(Self),
            ActorRefs.NoSender);
    }

    private void Handle(AllocateResources message)
    {
        _log.Info("Allocating resources for request {RequestId}: {AgentType} x{Instances}",
            message.Request.RequestId, message.Request.RequiredAgentType, message.Request.RequiredInstances);

        try
        {
            var allocation = AllocateResourcesInternal(message.Request);
            
            var allocationInfo = new ResourceAllocationInfo
            {
                RequestId = message.Request.RequestId,
                AgentType = message.Request.RequiredAgentType,
                AllocatedInstances = allocation.AllocatedAgents.Count,
                AllocatedAt = DateTime.UtcNow,
                AllocatedAgents = allocation.AllocatedAgents
            };

            _activeAllocations[message.Request.RequestId] = allocationInfo;

            message.Sender.Tell(new ResourcesAllocated(message.Request.RequestId, allocation));

            if (allocation.Success)
            {
                _metricsTracker.SuccessfulAllocations++;
                _metricsTracker.TotalWaitTime += allocation.WaitTime;
            }
            else
            {
                _metricsTracker.FailedAllocations++;
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to allocate resources for request {RequestId}", message.Request.RequestId);
            message.Sender.Tell(new ResourceAllocationFailed(message.Request.RequestId, ex.Message));
            _metricsTracker.FailedAllocations++;
        }
    }

    private void Handle(ReleaseResources message)
    {
        _log.Info("Releasing resources for allocation {AllocationId}", message.AllocationId);

        if (_activeAllocations.TryGetValue(message.AllocationId, out var allocation))
        {
            // Return resources to the pool
            if (_resourcePools.TryGetValue(allocation.AgentType, out var pool))
            {
                foreach (var agentId in allocation.AllocatedAgents)
                {
                    if (pool.AllocatedAgents.Contains(agentId))
                    {
                        pool.AllocatedAgents.Remove(agentId);
                        pool.AvailableAgents.Add(agentId);
                        _log.Debug("Returned agent {AgentId} to {AgentType} pool", agentId, allocation.AgentType);
                    }
                }
            }

            _activeAllocations.Remove(message.AllocationId);
            _log.Info("Released {Count} agents from allocation {AllocationId}", 
                allocation.AllocatedAgents.Count, message.AllocationId);
        }
        else
        {
            _log.Warning("Cannot release allocation {AllocationId} - allocation not found", message.AllocationId);
        }
    }

    private void Handle(GetResourceMetrics message)
    {
        var totalAgents = _resourcePools.Values.Sum(p => p.TotalCapacity);
        var activeAgents = _resourcePools.Values.Sum(p => p.AllocatedAgents.Count);
        var availableAgents = _resourcePools.Values.Sum(p => p.AvailableAgents.Count);

        var agentDistribution = _resourcePools.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.TotalCapacity);

        var metrics = new ResourceMetrics
        {
            ActiveTasks = _activeAllocations.Count,
            QueuedTasks = 0, // This would come from the task scheduler
            CpuUtilization = CalculateAverageCpuUtilization(),
            MemoryUtilization = CalculateAverageMemoryUtilization(),
            AvailableAgents = availableAgents,
            TotalAgents = totalAgents,
            AgentTypeDistribution = agentDistribution,
            LastUpdated = DateTime.UtcNow
        };

        message.Sender.Tell(metrics);
    }

    private void Handle(OptimizeResources message)
    {
        _log.Info("Starting resource optimization");

        try
        {
            var optimizationResult = OptimizeResourcesInternal();
            
            _log.Info("Resource optimization completed: {OptimizationPerformed}, Efficiency gain: {EfficiencyGain}%",
                optimizationResult.OptimizationPerformed, optimizationResult.EfficiencyGain * 100);

            message.Sender.Tell(optimizationResult);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Resource optimization failed");
            
            var failedResult = new ResourceOptimizationResult
            {
                OptimizationPerformed = false,
                EfficiencyGain = 0,
                OptimizationActions = new List<string> { $"Optimization failed: {ex.Message}" }
            };
            
            message.Sender.Tell(failedResult);
        }
    }

    private void Handle(AgentAvailable message)
    {
        var agentType = message.AgentType;
        
        if (!_resourcePools.TryGetValue(agentType, out var pool))
        {
            pool = new ResourcePool
            {
                AgentType = agentType,
                TotalCapacity = 0,
                AvailableAgents = new HashSet<string>(),
                AllocatedAgents = new HashSet<string>()
            };
            _resourcePools[agentType] = pool;
        }

        if (!pool.AvailableAgents.Contains(message.AgentId) && !pool.AllocatedAgents.Contains(message.AgentId))
        {
            pool.AvailableAgents.Add(message.AgentId);
            pool.TotalCapacity++;

            _log.Info("Added agent {AgentId} to {AgentType} pool (capacity: {Capacity})",
                message.AgentId, agentType, pool.TotalCapacity);
        }

        // Update health status
        _healthStatus[message.AgentId] = new ResourceHealthInfo
        {
            AgentId = message.AgentId,
            AgentType = agentType,
            Status = HealthLevel.Healthy,
            LastSeen = DateTime.UtcNow,
            Capabilities = message.Capabilities
        };
    }

    private void Handle(AgentUnavailable message)
    {
        _log.Info("Agent {AgentId} is no longer available: {Reason}", message.AgentId, message.Reason);

        // Remove from all pools
        foreach (var pool in _resourcePools.Values)
        {
            if (pool.AvailableAgents.Remove(message.AgentId))
            {
                pool.TotalCapacity--;
                _log.Debug("Removed agent {AgentId} from {AgentType} available pool", 
                    message.AgentId, pool.AgentType);
            }
            
            if (pool.AllocatedAgents.Remove(message.AgentId))
            {
                pool.TotalCapacity--;
                _log.Debug("Removed agent {AgentId} from {AgentType} allocated pool", 
                    message.AgentId, pool.AgentType);
            }
        }

        // Update health status
        if (_healthStatus.TryGetValue(message.AgentId, out var healthInfo))
        {
            healthInfo.Status = HealthLevel.Offline;
            healthInfo.LastSeen = DateTime.UtcNow;
        }
    }

    private void Handle(AgentHeartbeat message)
    {
        if (_healthStatus.TryGetValue(message.AgentId, out var healthInfo))
        {
            healthInfo.LastSeen = message.Timestamp;
            healthInfo.CurrentLoad = message.CurrentLoad;
            healthInfo.Status = DetermineHealthLevel(message.CurrentLoad, message.Status);
        }
    }

    private void Handle(MonitorResourceHealth message)
    {
        var healthStatus = MonitorResourceHealthInternal();
        message.Sender.Tell(healthStatus);
    }

    private void Handle(ScaleResources message)
    {
        _log.Info("Scaling {ResourceType} resources: {Direction} to {TargetInstances} instances",
            message.Request.ResourceType, message.Request.Direction, message.Request.TargetInstances);

        try
        {
            var scalingResult = ScaleResourcesInternal(message.Request);
            message.Sender.Tell(scalingResult);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Resource scaling failed for {ResourceType}", message.Request.ResourceType);
            
            var failedResult = new ScalingResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
            
            message.Sender.Tell(failedResult);
        }
    }

    private void Handle(ResourceHealthCheck message)
    {
        PerformHealthCheck();
    }

    private void InitializeResourcePools()
    {
        // Initialize default pools for common agent types
        var defaultAgentTypes = new[] { "DirectorAgent", "DialogueAgent", "CodexAgent", "AnalysisAgent" };
        
        foreach (var agentType in defaultAgentTypes)
        {
            _resourcePools[agentType] = new ResourcePool
            {
                AgentType = agentType,
                TotalCapacity = 0,
                AvailableAgents = new HashSet<string>(),
                AllocatedAgents = new HashSet<string>()
            };
        }

        _log.Info("Initialized {Count} resource pools", _resourcePools.Count);
    }

    private ResourceAllocation AllocateResourcesInternal(ResourceRequest request)
    {
        var startTime = DateTime.UtcNow;
        var agentType = request.RequiredAgentType;
        var requiredInstances = request.RequiredInstances;

        if (!_resourcePools.TryGetValue(agentType, out var pool))
        {
            return new ResourceAllocation
            {
                RequestId = request.RequestId,
                Success = false,
                ErrorMessage = $"No resource pool found for agent type: {agentType}"
            };
        }

        if (pool.AvailableAgents.Count < requiredInstances)
        {
            // Check if we should wait for resources
            var waitTime = DateTime.UtcNow - startTime;
            if (waitTime < request.MaxWaitTime)
            {
                // In a real implementation, this would wait for resources to become available
                // For now, we'll return partial allocation if any agents are available
            }

            if (pool.AvailableAgents.Count == 0)
            {
                return new ResourceAllocation
                {
                    RequestId = request.RequestId,
                    Success = false,
                    WaitTime = waitTime,
                    ErrorMessage = $"No available agents of type {agentType}"
                };
            }
        }

        // Allocate available agents
        var agentsToAllocate = pool.AvailableAgents
            .Take(Math.Min(requiredInstances, pool.AvailableAgents.Count))
            .ToList();

        foreach (var agentId in agentsToAllocate)
        {
            pool.AvailableAgents.Remove(agentId);
            pool.AllocatedAgents.Add(agentId);
        }

        return new ResourceAllocation
        {
            RequestId = request.RequestId,
            Success = agentsToAllocate.Count >= requiredInstances,
            AllocatedAgents = agentsToAllocate,
            WaitTime = DateTime.UtcNow - startTime
        };
    }

    private ResourceOptimizationResult OptimizeResourcesInternal()
    {
        var optimizationActions = new List<string>();
        var initialEfficiency = CalculateEfficiency();

        // Rebalance agent distribution
        RebalanceAgentPools(optimizationActions);

        // Clean up stale allocations
        CleanupStaleAllocations(optimizationActions);

        // Optimize pool sizes based on usage patterns
        OptimizePoolSizes(optimizationActions);

        var finalEfficiency = CalculateEfficiency();
        var efficiencyGain = finalEfficiency - initialEfficiency;

        return new ResourceOptimizationResult
        {
            OptimizationPerformed = optimizationActions.Any(),
            EfficiencyGain = efficiencyGain,
            OptimizationActions = optimizationActions,
            PostOptimizationMetrics = new ResourceMetrics
            {
                TotalAgents = _resourcePools.Values.Sum(p => p.TotalCapacity),
                AvailableAgents = _resourcePools.Values.Sum(p => p.AvailableAgents.Count),
                LastUpdated = DateTime.UtcNow
            }
        };
    }

    private ResourceHealthStatus MonitorResourceHealthInternal()
    {
        var healthyCount = _healthStatus.Count(h => h.Value.Status == HealthLevel.Healthy);
        var warningCount = _healthStatus.Count(h => h.Value.Status == HealthLevel.Warning);
        var criticalCount = _healthStatus.Count(h => h.Value.Status == HealthLevel.Critical);
        var offlineCount = _healthStatus.Count(h => h.Value.Status == HealthLevel.Offline);

        var overallHealth = criticalCount > 0 ? HealthLevel.Critical :
                           warningCount > healthyCount ? HealthLevel.Warning :
                           healthyCount > 0 ? HealthLevel.Healthy :
                           HealthLevel.Offline;

        var componentHealth = _resourcePools.ToDictionary(
            kv => kv.Key,
            kv => DeterminePoolHealth(kv.Value));

        var healthIssues = _healthStatus.Values
            .Where(h => h.Status != HealthLevel.Healthy)
            .Select(h => $"Agent {h.AgentId} ({h.AgentType}): {h.Status}")
            .ToList();

        return new ResourceHealthStatus
        {
            OverallHealth = overallHealth,
            ComponentHealth = componentHealth,
            HealthIssues = healthIssues,
            LastHealthCheck = DateTime.UtcNow
        };
    }

    private ScalingResult ScaleResourcesInternal(ScalingRequest request)
    {
        if (!_resourcePools.TryGetValue(request.ResourceType, out var pool))
        {
            return new ScalingResult
            {
                Success = false,
                ErrorMessage = $"Resource type {request.ResourceType} not found"
            };
        }

        var previousInstances = pool.TotalCapacity;
        var scalingTime = DateTime.UtcNow;

        // Simulate scaling operation
        // In a real implementation, this would interface with the actual resource provisioning system
        switch (request.Direction)
        {
            case ScalingDirection.Up:
                // Scale up - add more capacity (simulated)
                var additionalCapacity = request.TargetInstances - previousInstances;
                if (additionalCapacity > 0)
                {
                    for (int i = 0; i < additionalCapacity; i++)
                    {
                        var newAgentId = $"{request.ResourceType}-{Guid.NewGuid():N}[..8]";
                        pool.AvailableAgents.Add(newAgentId);
                        pool.TotalCapacity++;
                    }
                }
                break;

            case ScalingDirection.Down:
                // Scale down - remove capacity
                var capacityToRemove = Math.Min(previousInstances - request.TargetInstances, pool.AvailableAgents.Count);
                if (capacityToRemove > 0)
                {
                    var agentsToRemove = pool.AvailableAgents.Take(capacityToRemove).ToList();
                    foreach (var agentId in agentsToRemove)
                    {
                        pool.AvailableAgents.Remove(agentId);
                        pool.TotalCapacity--;
                    }
                }
                break;
        }

        return new ScalingResult
        {
            Success = true,
            PreviousInstances = previousInstances,
            CurrentInstances = pool.TotalCapacity,
            ScalingTime = DateTime.UtcNow - scalingTime
        };
    }

    private void PerformHealthCheck()
    {
        var staleAgents = _healthStatus.Values
            .Where(h => DateTime.UtcNow - h.LastSeen > TimeSpan.FromMinutes(5))
            .ToList();

        foreach (var staleAgent in staleAgents)
        {
            if (staleAgent.Status != HealthLevel.Offline)
            {
                _log.Warning("Agent {AgentId} appears to be stale (last seen: {LastSeen})",
                    staleAgent.AgentId, staleAgent.LastSeen);
                
                staleAgent.Status = HealthLevel.Offline;
                
                // Remove from pools if offline too long
                if (DateTime.UtcNow - staleAgent.LastSeen > TimeSpan.FromMinutes(10))
                {
                    Self.Tell(new AgentUnavailable(staleAgent.AgentId, "Health check timeout"));
                }
            }
        }
    }

    private double CalculateEfficiency()
    {
        var totalCapacity = _resourcePools.Values.Sum(p => p.TotalCapacity);
        if (totalCapacity == 0) return 0.0;

        var utilizationRate = (double)_resourcePools.Values.Sum(p => p.AllocatedAgents.Count) / totalCapacity;
        return utilizationRate;
    }

    private double CalculateAverageCpuUtilization()
    {
        var healthyAgents = _healthStatus.Values.Where(h => h.Status == HealthLevel.Healthy).ToList();
        return healthyAgents.Any() ? healthyAgents.Average(h => h.CurrentLoad) : 0.0;
    }

    private double CalculateAverageMemoryUtilization()
    {
        // Simplified memory calculation based on load
        return CalculateAverageCpuUtilization() * 0.8; // Assume memory follows CPU usage
    }

    private HealthLevel DetermineHealthLevel(double currentLoad, AgentStatus status)
    {
        return status switch
        {
            AgentStatus.Offline or AgentStatus.Error => HealthLevel.Offline,
            AgentStatus.Busy when currentLoad > 0.9 => HealthLevel.Critical,
            AgentStatus.Busy when currentLoad > 0.7 => HealthLevel.Warning,
            _ => HealthLevel.Healthy
        };
    }

    private HealthLevel DeterminePoolHealth(ResourcePool pool)
    {
        if (pool.TotalCapacity == 0) return HealthLevel.Offline;
        
        var utilizationRate = (double)pool.AllocatedAgents.Count / pool.TotalCapacity;
        return utilizationRate switch
        {
            > 0.9 => HealthLevel.Critical,
            > 0.7 => HealthLevel.Warning,
            _ => HealthLevel.Healthy
        };
    }

    private void RebalanceAgentPools(List<string> optimizationActions)
    {
        // Simple rebalancing logic - move agents between pools if needed
        var underutilizedPools = _resourcePools.Values
            .Where(p => p.TotalCapacity > 0 && (double)p.AllocatedAgents.Count / p.TotalCapacity < 0.3)
            .ToList();

        var overutilizedPools = _resourcePools.Values
            .Where(p => p.TotalCapacity > 0 && (double)p.AllocatedAgents.Count / p.TotalCapacity > 0.8)
            .ToList();

        if (underutilizedPools.Any() && overutilizedPools.Any())
        {
            optimizationActions.Add($"Identified {underutilizedPools.Count} underutilized and {overutilizedPools.Count} overutilized pools for rebalancing");
        }
    }

    private void CleanupStaleAllocations(List<string> optimizationActions)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-2);
        var staleAllocations = _activeAllocations
            .Where(kv => kv.Value.AllocatedAt < cutoffTime)
            .Select(kv => kv.Key)
            .ToList();

        foreach (var allocationId in staleAllocations)
        {
            Self.Tell(new ReleaseResources(allocationId));
        }

        if (staleAllocations.Any())
        {
            optimizationActions.Add($"Cleaned up {staleAllocations.Count} stale resource allocations");
        }
    }

    private void OptimizePoolSizes(List<string> optimizationActions)
    {
        // Simple pool size optimization based on usage patterns
        foreach (var pool in _resourcePools.Values)
        {
            var utilizationRate = pool.TotalCapacity > 0 ? (double)pool.AllocatedAgents.Count / pool.TotalCapacity : 0;
            
            if (utilizationRate > 0.9 && pool.AvailableAgents.Count == 0)
            {
                optimizationActions.Add($"Pool {pool.AgentType} is overutilized ({utilizationRate:P0}) - consider scaling up");
            }
            else if (utilizationRate < 0.2 && pool.AvailableAgents.Count > 2)
            {
                optimizationActions.Add($"Pool {pool.AgentType} is underutilized ({utilizationRate:P0}) - consider scaling down");
            }
        }
    }

    protected override void PreStart()
    {
        _log.Info("AIResourceManagerActor started");
    }

    protected override void PostStop()
    {
        _log.Info("AIResourceManagerActor stopped");
    }

    #region Internal Messages and State

    /// <summary>
    /// Internal message to monitor resource health.
    /// </summary>
    /// <param name="Sender">Actor that requested health monitoring.</param>
    private record MonitorResourceHealth(IActorRef Sender);

    /// <summary>
    /// Internal message to scale resources.
    /// </summary>
    /// <param name="Request">Scaling request.</param>
    /// <param name="Sender">Actor that requested scaling.</param>
    private record ScaleResources(ScalingRequest Request, IActorRef Sender);

    /// <summary>
    /// Internal message for periodic resource health checks.
    /// </summary>
    private record ResourceHealthCheck();

    /// <summary>
    /// Resource pool for managing agents of a specific type.
    /// </summary>
    private class ResourcePool
    {
        public string AgentType { get; set; } = string.Empty;
        public int TotalCapacity { get; set; }
        public HashSet<string> AvailableAgents { get; set; } = new();
        public HashSet<string> AllocatedAgents { get; set; } = new();
    }

    /// <summary>
    /// Information about a resource allocation.
    /// </summary>
    private class ResourceAllocationInfo
    {
        public string RequestId { get; set; } = string.Empty;
        public string AgentType { get; set; } = string.Empty;
        public int AllocatedInstances { get; set; }
        public DateTime AllocatedAt { get; set; }
        public List<string> AllocatedAgents { get; set; } = new();
    }

    /// <summary>
    /// Health information for a resource.
    /// </summary>
    private class ResourceHealthInfo
    {
        public string AgentId { get; set; } = string.Empty;
        public string AgentType { get; set; } = string.Empty;
        public HealthLevel Status { get; set; }
        public DateTime LastSeen { get; set; }
        public double CurrentLoad { get; set; }
        public List<string> Capabilities { get; set; } = new();
    }

    /// <summary>
    /// Resource metrics tracking.
    /// </summary>
    private class ResourceMetricsTracker
    {
        public int SuccessfulAllocations { get; set; }
        public int FailedAllocations { get; set; }
        public TimeSpan TotalWaitTime { get; set; }
    }

    #endregion
}