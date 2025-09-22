using Docker.DotNet;
using Docker.DotNet.Models;
using GameConsole.Deployment.Containers.Interfaces;
using GameConsole.Deployment.Containers.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.Deployment.Containers.Services;

/// <summary>
/// Docker deployment provider implementing container deployment using Docker.DotNet.
/// Supports basic Docker operations including create, update, scale, and delete containers.
/// </summary>
public class DockerDeploymentProvider : BaseDeploymentService, IDeploymentProvider
{
    private readonly DockerClient _dockerClient;
    private readonly ConcurrentDictionary<string, DeploymentInfo> _deployments = new();

    public string ProviderType => "Docker";

    public DockerDeploymentProvider(ILogger<DockerDeploymentProvider> logger) : base(logger)
    {
        // Create Docker client - in production, this should be configurable
        _dockerClient = new DockerClientConfiguration().CreateClient();
    }

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Test Docker connection
            await _dockerClient.System.PingAsync(cancellationToken);
            _logger.LogDebug("Docker connection verified");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Docker daemon");
            throw new InvalidOperationException("Docker daemon is not available", ex);
        }
    }

    protected override async ValueTask OnDisposeAsync()
    {
        _dockerClient?.Dispose();
        await base.OnDisposeAsync();
    }

    public async Task<DeploymentResult> CreateDeploymentAsync(DeploymentConfiguration config, CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
            return DeploymentResult.CreateFailure("Service is not running");

        try
        {
            _logger.LogInformation("Creating Docker deployment {DeploymentName} with image {Image}", 
                config.Name, config.Image);

            var deploymentId = GenerateDeploymentId(config.Name);
            var deploymentInfo = new DeploymentInfo
            {
                Id = deploymentId,
                Name = config.Name,
                Configuration = config,
                Status = "Creating",
                ContainerIds = new List<string>()
            };

            _deployments[deploymentId] = deploymentInfo;

            // Create containers based on replica count
            for (int i = 0; i < config.Replicas; i++)
            {
                var containerName = $"{config.Name}-{i}";
                var containerId = await CreateContainerAsync(containerName, config, cancellationToken);
                deploymentInfo.ContainerIds.Add(containerId);
                
                // Start the container
                await _dockerClient.Containers.StartContainerAsync(containerId, 
                    new ContainerStartParameters(), cancellationToken);
            }

            deploymentInfo.Status = "Running";
            deploymentInfo.LastUpdated = DateTime.UtcNow;

            _logger.LogInformation("Successfully created Docker deployment {DeploymentId} with {ReplicaCount} replicas", 
                deploymentId, config.Replicas);

            return DeploymentResult.CreateSuccess(deploymentId, 
                $"Docker deployment created with {config.Replicas} containers");
        }
        catch (Exception ex)
        {
            return HandleException(ex, "creating deployment", () => 
                DeploymentResult.CreateFailure($"Failed to create deployment: {ex.Message}", ex));
        }
    }

    public async Task<DeploymentResult> UpdateDeploymentAsync(string deploymentId, DeploymentConfiguration config, CancellationToken cancellationToken = default)
    {
        if (!_deployments.TryGetValue(deploymentId, out var deploymentInfo))
            return DeploymentResult.CreateFailure($"Deployment {deploymentId} not found");

        try
        {
            _logger.LogInformation("Updating Docker deployment {DeploymentId}", deploymentId);

            // For simplicity, we'll recreate containers for updates
            // In production, this should be more sophisticated (rolling updates, etc.)
            await DeleteDeploymentAsync(deploymentId, cancellationToken);
            
            var result = await CreateDeploymentAsync(config, cancellationToken);
            if (result.Success)
            {
                // Update the deployment ID to maintain consistency
                result.DeploymentId = deploymentId;
            }

            return result;
        }
        catch (Exception ex)
        {
            return HandleException(ex, "updating deployment", () => 
                DeploymentResult.CreateFailure($"Failed to update deployment: {ex.Message}", ex));
        }
    }

    public async Task<bool> DeleteDeploymentAsync(string deploymentId, CancellationToken cancellationToken = default)
    {
        if (!_deployments.TryGetValue(deploymentId, out var deploymentInfo))
        {
            _logger.LogWarning("Deployment {DeploymentId} not found for deletion", deploymentId);
            return false;
        }

        try
        {
            _logger.LogInformation("Deleting Docker deployment {DeploymentId}", deploymentId);

            // Stop and remove all containers
            foreach (var containerId in deploymentInfo.ContainerIds)
            {
                try
                {
                    await _dockerClient.Containers.StopContainerAsync(containerId, 
                        new ContainerStopParameters { WaitBeforeKillSeconds = 30 }, cancellationToken);
                    
                    await _dockerClient.Containers.RemoveContainerAsync(containerId, 
                        new ContainerRemoveParameters { Force = true }, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to remove container {ContainerId}", containerId);
                }
            }

            _deployments.TryRemove(deploymentId, out _);
            _logger.LogInformation("Successfully deleted Docker deployment {DeploymentId}", deploymentId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete deployment {DeploymentId}", deploymentId);
            return false;
        }
    }

    public async Task<DeploymentStatus> GetDeploymentStatusAsync(string deploymentId, CancellationToken cancellationToken = default)
    {
        if (!_deployments.TryGetValue(deploymentId, out var deploymentInfo))
        {
            return new DeploymentStatus
            {
                DeploymentId = deploymentId,
                Status = "NotFound",
                LastUpdated = DateTime.UtcNow
            };
        }

        try
        {
            var readyReplicas = 0;
            
            // Check container statuses
            foreach (var containerId in deploymentInfo.ContainerIds)
            {
                try
                {
                    var container = await _dockerClient.Containers.InspectContainerAsync(containerId, cancellationToken);
                    if (container.State.Running)
                    {
                        readyReplicas++;
                    }
                }
                catch
                {
                    // Container may have been removed externally
                }
            }

            return new DeploymentStatus
            {
                DeploymentId = deploymentId,
                Status = readyReplicas == deploymentInfo.Configuration.Replicas ? "Running" : "Partial",
                ReadyReplicas = readyReplicas,
                TotalReplicas = deploymentInfo.Configuration.Replicas,
                LastUpdated = DateTime.UtcNow,
                HealthStatus = readyReplicas > 0 ? "Healthy" : "Unhealthy"
            };
        }
        catch (Exception ex)
        {
            return HandleException(ex, "getting deployment status", () => new DeploymentStatus
            {
                DeploymentId = deploymentId,
                Status = "Error",
                LastUpdated = DateTime.UtcNow
            });
        }
    }

    public async Task<DeploymentResult> ScaleDeploymentAsync(string deploymentId, int replicas, CancellationToken cancellationToken = default)
    {
        if (!_deployments.TryGetValue(deploymentId, out var deploymentInfo))
            return DeploymentResult.CreateFailure($"Deployment {deploymentId} not found");

        try
        {
            _logger.LogInformation("Scaling Docker deployment {DeploymentId} to {Replicas} replicas", 
                deploymentId, replicas);

            var currentReplicas = deploymentInfo.ContainerIds.Count;
            
            if (replicas > currentReplicas)
            {
                // Scale up - create more containers
                for (int i = currentReplicas; i < replicas; i++)
                {
                    var containerName = $"{deploymentInfo.Name}-{i}";
                    var containerId = await CreateContainerAsync(containerName, deploymentInfo.Configuration, cancellationToken);
                    deploymentInfo.ContainerIds.Add(containerId);
                    
                    await _dockerClient.Containers.StartContainerAsync(containerId, 
                        new ContainerStartParameters(), cancellationToken);
                }
            }
            else if (replicas < currentReplicas)
            {
                // Scale down - remove excess containers
                var containersToRemove = deploymentInfo.ContainerIds.Skip(replicas).ToList();
                foreach (var containerId in containersToRemove)
                {
                    await _dockerClient.Containers.StopContainerAsync(containerId, 
                        new ContainerStopParameters { WaitBeforeKillSeconds = 30 }, cancellationToken);
                    await _dockerClient.Containers.RemoveContainerAsync(containerId, 
                        new ContainerRemoveParameters { Force = true }, cancellationToken);
                    deploymentInfo.ContainerIds.Remove(containerId);
                }
            }

            deploymentInfo.Configuration.Replicas = replicas;
            deploymentInfo.LastUpdated = DateTime.UtcNow;

            return DeploymentResult.CreateSuccess(deploymentId, $"Scaled to {replicas} replicas");
        }
        catch (Exception ex)
        {
            return HandleException(ex, "scaling deployment", () => 
                DeploymentResult.CreateFailure($"Failed to scale deployment: {ex.Message}", ex));
        }
    }

    public async Task<IEnumerable<DeploymentStatus>> ListDeploymentsAsync(CancellationToken cancellationToken = default)
    {
        var statuses = new List<DeploymentStatus>();
        
        foreach (var deployment in _deployments.Values)
        {
            var status = await GetDeploymentStatusAsync(deployment.Id, cancellationToken);
            statuses.Add(status);
        }
        
        return statuses;
    }

    public bool SupportsConfiguration(DeploymentConfiguration config)
    {
        // Docker can handle most basic configurations
        return !string.IsNullOrEmpty(config.Image) && config.Replicas > 0;
    }

    public async Task<Dictionary<string, object>> GetCapabilitiesAsync()
    {
        var capabilities = new Dictionary<string, object>
        {
            ["ProviderType"] = ProviderType,
            ["SupportsScaling"] = true,
            ["SupportsRollingUpdates"] = false, // Not implemented yet
            ["SupportsHealthChecks"] = false, // Not implemented yet
            ["MaxReplicas"] = 100 // Reasonable limit for Docker
        };

        try
        {
            var version = await _dockerClient.System.GetVersionAsync();
            capabilities["DockerVersion"] = version.Version;
            capabilities["DockerApiVersion"] = version.APIVersion;
        }
        catch
        {
            // Ignore version check failures
        }

        return capabilities;
    }

    private async Task<string> CreateContainerAsync(string containerName, DeploymentConfiguration config, CancellationToken cancellationToken)
    {
        var createParams = new CreateContainerParameters
        {
            Name = containerName,
            Image = config.Image,
            Env = config.Environment.Select(kvp => $"{kvp.Key}={kvp.Value}").ToList(),
            Labels = config.Labels,
            HostConfig = new HostConfig()
        };

        // Configure port mappings
        if (config.PortMappings.Any())
        {
            createParams.HostConfig.PortBindings = new Dictionary<string, IList<PortBinding>>();
            foreach (var portMapping in config.PortMappings)
            {
                var containerPort = $"{portMapping.Value}/tcp";
                createParams.HostConfig.PortBindings[containerPort] = new List<PortBinding>
                {
                    new PortBinding { HostPort = portMapping.Key.ToString() }
                };
            }
        }

        // Configure resource limits
        if (config.ResourceLimits != null)
        {
            if (config.ResourceLimits.MemoryLimit.HasValue)
            {
                createParams.HostConfig.Memory = config.ResourceLimits.MemoryLimit.Value * 1024 * 1024; // Convert MB to bytes
            }
            if (config.ResourceLimits.CpuLimit.HasValue)
            {
                createParams.HostConfig.NanoCPUs = config.ResourceLimits.CpuLimit.Value * 1000000; // Convert millicores to nanocores
            }
        }

        var response = await _dockerClient.Containers.CreateContainerAsync(createParams, cancellationToken);
        return response.ID;
    }

    private static string GenerateDeploymentId(string name)
    {
        return $"{name}-{Guid.NewGuid():N}";
    }

    private class DeploymentInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DeploymentConfiguration Configuration { get; set; } = null!;
        public string Status { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
        public List<string> ContainerIds { get; set; } = new();
    }
}