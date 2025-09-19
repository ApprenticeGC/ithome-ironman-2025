using GameConsole.Core.Abstractions;
using GameConsole.Core.Registry;
using Microsoft.Extensions.Logging;
using Docker.DotNet;
using Docker.DotNet.Models;
using System.Collections.Concurrent;

namespace GameConsole.Deployment.Containers;

/// <summary>
/// Docker implementation of the container orchestrator service.
/// </summary>
[Service("Docker Deployment Provider", "1.0.0", "Container orchestrator implementation using Docker", 
    Lifetime = ServiceLifetime.Singleton, Categories = new[] { "Deployment" })]
public class DockerDeploymentProvider : IContainerOrchestrator
{
    private readonly ILogger<DockerDeploymentProvider> _logger;
    private readonly DockerClient _dockerClient;
    private readonly ConcurrentDictionary<string, ServiceInfo> _services = new();
    private bool _isRunning;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DockerDeploymentProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public DockerDeploymentProvider(ILogger<DockerDeploymentProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Initialize Docker client - this will connect to the default Docker daemon
        _dockerClient = new DockerClientConfiguration().CreateClient();
    }

    #region IService Implementation

    public bool IsRunning => _isRunning && !_disposed;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        _logger.LogInformation("Initializing DockerDeploymentProvider");

        try
        {
            // Verify Docker daemon is accessible
            var version = await _dockerClient.System.GetVersionAsync(cancellationToken);
            _logger.LogInformation("Connected to Docker daemon version {Version}", version.Version);

            // Create default network if it doesn't exist
            await EnsureGameConsoleNetworkAsync(cancellationToken);

            _logger.LogInformation("Initialized DockerDeploymentProvider");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize DockerDeploymentProvider");
            throw;
        }
    }

    #pragma warning disable CS1998 // Async method lacks 'await' operators
    public async Task StartAsync(CancellationToken cancellationToken = default)
    #pragma warning restore CS1998
    {
        ThrowIfDisposed();
        _logger.LogInformation("Starting DockerDeploymentProvider");
        
        _isRunning = true;
        
        // Load existing services
        await RefreshServiceListAsync(cancellationToken);
        
        _logger.LogInformation("Started DockerDeploymentProvider with {ServiceCount} existing services", _services.Count);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping DockerDeploymentProvider");
        _isRunning = false;
        _logger.LogInformation("Stopped DockerDeploymentProvider");
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        if (_isRunning)
        {
            await StopAsync();
        }

        _dockerClient?.Dispose();
        _disposed = true;
    }

    #endregion

    #region ICapabilityProvider Implementation

    public Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var capabilities = new List<Type>
        {
            typeof(IContainerOrchestrator),
            typeof(IService)
        };
        return Task.FromResult<IEnumerable<Type>>(capabilities);
    }

    public Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        var hasCapability = typeof(T) == typeof(IContainerOrchestrator) || typeof(T) == typeof(IService);
        return Task.FromResult(hasCapability);
    }

    public Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (typeof(T) == typeof(IContainerOrchestrator))
        {
            return Task.FromResult(this as T);
        }
        
        return Task.FromResult<T?>(null);
    }

    #endregion

    #region IContainerOrchestrator Implementation

    public async Task<DeploymentResult> DeployAsync(ContainerConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        _logger.LogInformation("Deploying service {ServiceName} with image {Image}", configuration.ServiceName, configuration.Image);

        try
        {
            // Check if service already exists
            if (_services.ContainsKey(configuration.ServiceName))
            {
                _logger.LogWarning("Service {ServiceName} already exists. Use scaling or update operations instead.", configuration.ServiceName);
                return new DeploymentResult
                {
                    IsSuccess = false,
                    ServiceName = configuration.ServiceName,
                    ErrorMessage = "Service already exists"
                };
            }

            // Create container configuration
            var createParameters = new CreateContainerParameters
            {
                Image = configuration.Image,
                Name = configuration.ServiceName,
                Env = configuration.EnvironmentVariables.Select(kv => $"{kv.Key}={kv.Value}").ToArray(),
                Labels = configuration.Labels.ToDictionary(kv => kv.Key, kv => kv.Value),
                ExposedPorts = configuration.PortMappings.Keys.ToDictionary(port => $"{port}/tcp", _ => new EmptyStruct()),
                HostConfig = new HostConfig
                {
                    PortBindings = configuration.PortMappings.ToDictionary(
                        kv => $"{kv.Key}/tcp",
                        kv => (IList<PortBinding>)new List<PortBinding> { new() { HostPort = kv.Value.ToString() } }
                    ),
                    Binds = configuration.VolumeMounts.Select(kv => $"{kv.Key}:{kv.Value}").ToArray(),
                    NetworkMode = "gameconsole-network"
                }
            };

            // Apply resource limits if specified
            if (configuration.ResourceLimits != null)
            {
                createParameters.HostConfig.Memory = configuration.ResourceLimits.MemoryLimit ?? 0;
                if (configuration.ResourceLimits.CpuLimit.HasValue)
                {
                    createParameters.HostConfig.CPUQuota = (long)(configuration.ResourceLimits.CpuLimit.Value * 100000);
                    createParameters.HostConfig.CPUPeriod = 100000;
                }
            }

            // Create and start container
            var response = await _dockerClient.Containers.CreateContainerAsync(createParameters, cancellationToken);
            await _dockerClient.Containers.StartContainerAsync(response.ID, new ContainerStartParameters(), cancellationToken);

            // Store service info
            var serviceInfo = new ServiceInfo
            {
                ServiceName = configuration.ServiceName,
                Image = configuration.Image,
                Strategy = configuration.Strategy,
                CreatedAt = DateTime.UtcNow,
                Status = DeploymentStatus.Running,
                Labels = configuration.Labels
            };
            _services[configuration.ServiceName] = serviceInfo;

            _logger.LogInformation("Successfully deployed service {ServiceName}", configuration.ServiceName);

            return new DeploymentResult
            {
                IsSuccess = true,
                DeploymentId = response.ID,
                ServiceName = configuration.ServiceName,
                Metadata = new Dictionary<string, object>
                {
                    { "ContainerId", response.ID },
                    { "Image", configuration.Image }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deploy service {ServiceName}", configuration.ServiceName);
            return new DeploymentResult
            {
                IsSuccess = false,
                ServiceName = configuration.ServiceName,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<ScalingResult> ScaleAsync(string serviceName, int instanceCount, CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        _logger.LogInformation("Scaling service {ServiceName} to {InstanceCount} instances", serviceName, instanceCount);

        // Docker doesn't have built-in service scaling like Docker Swarm
        // This is a simplified implementation that manages individual containers
        try
        {
            var containers = await GetServiceContainersAsync(serviceName, cancellationToken);
            var currentCount = containers.Count;

            if (instanceCount > currentCount)
            {
                // Scale up - this is a simplified implementation
                _logger.LogWarning("Scaling up containers not fully implemented for standalone Docker");
                return new ScalingResult
                {
                    IsSuccess = false,
                    ServiceName = serviceName,
                    PreviousInstanceCount = currentCount,
                    NewInstanceCount = currentCount,
                    ErrorMessage = "Scale up not implemented for standalone Docker"
                };
            }
            else if (instanceCount < currentCount)
            {
                // Scale down
                var containersToRemove = containers.Skip(instanceCount).ToList();
                foreach (var container in containersToRemove)
                {
                    await _dockerClient.Containers.StopContainerAsync(container.ID, new ContainerStopParameters(), cancellationToken);
                    await _dockerClient.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters(), cancellationToken);
                }

                return new ScalingResult
                {
                    IsSuccess = true,
                    ServiceName = serviceName,
                    PreviousInstanceCount = currentCount,
                    NewInstanceCount = instanceCount
                };
            }

            return new ScalingResult
            {
                IsSuccess = true,
                ServiceName = serviceName,
                PreviousInstanceCount = currentCount,
                NewInstanceCount = instanceCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scale service {ServiceName}", serviceName);
            return new ScalingResult
            {
                IsSuccess = false,
                ServiceName = serviceName,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<DeploymentResult> BlueGreenDeployAsync(string serviceName, ContainerConfiguration newConfiguration, CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        _logger.LogInformation("Performing blue-green deployment for service {ServiceName}", serviceName);

        try
        {
            var greenServiceName = $"{serviceName}-green";
            
            // Deploy green version
            var greenConfig = newConfiguration with { ServiceName = greenServiceName };
            var greenResult = await DeployAsync(greenConfig, cancellationToken);
            
            if (!greenResult.IsSuccess)
            {
                return greenResult;
            }

            // Wait for green to be healthy (simplified check)
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

            // Switch traffic (in a real implementation, this would update load balancer)
            // For now, we'll stop the blue version and rename green to the original name
            await RemoveServiceAsync(serviceName, cancellationToken);
            
            // Rename green container to original name
            var greenContainers = await GetServiceContainersAsync(greenServiceName, cancellationToken);
            if (greenContainers.Any())
            {
                var greenContainer = greenContainers.First();
                await _dockerClient.Containers.RenameContainerAsync(greenContainer.ID, new ContainerRenameParameters { NewName = serviceName }, cancellationToken);
            }

            // Update service info
            if (_services.TryRemove(greenServiceName, out var greenInfo))
            {
                greenInfo.ServiceName = serviceName;
                _services[serviceName] = greenInfo;
            }

            _logger.LogInformation("Successfully completed blue-green deployment for service {ServiceName}", serviceName);

            return new DeploymentResult
            {
                IsSuccess = true,
                DeploymentId = greenResult.DeploymentId,
                ServiceName = serviceName,
                Metadata = greenResult.Metadata
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed blue-green deployment for service {ServiceName}", serviceName);
            return new DeploymentResult
            {
                IsSuccess = false,
                ServiceName = serviceName,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<ServiceStatus> GetServiceStatusAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();

        try
        {
            var containers = await GetServiceContainersAsync(serviceName, cancellationToken);
            var runningContainers = containers.Where(c => c.State == "running").ToList();

            return new ServiceStatus
            {
                ServiceName = serviceName,
                Status = runningContainers.Any() ? DeploymentStatus.Running : DeploymentStatus.Terminated,
                RunningInstances = runningContainers.Count,
                DesiredInstances = containers.Count,
                Details = new Dictionary<string, object>
                {
                    { "TotalContainers", containers.Count },
                    { "RunningContainers", runningContainers.Count }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get status for service {ServiceName}", serviceName);
            return new ServiceStatus
            {
                ServiceName = serviceName,
                Status = DeploymentStatus.Unknown,
                Details = new Dictionary<string, object> { { "Error", ex.Message } }
            };
        }
    }

    public async Task<IEnumerable<ServiceInfo>> ListServicesAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        await RefreshServiceListAsync(cancellationToken);
        return _services.Values.ToList();
    }

    public async Task RemoveServiceAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        _logger.LogInformation("Removing service {ServiceName}", serviceName);

        try
        {
            var containers = await GetServiceContainersAsync(serviceName, cancellationToken);
            
            foreach (var container in containers)
            {
                try
                {
                    await _dockerClient.Containers.StopContainerAsync(container.ID, new ContainerStopParameters(), cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to stop container {ContainerId}", container.ID);
                }

                try
                {
                    await _dockerClient.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters(), cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to remove container {ContainerId}", container.ID);
                }
            }

            _services.TryRemove(serviceName, out _);
            _logger.LogInformation("Successfully removed service {ServiceName}", serviceName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove service {ServiceName}", serviceName);
            throw;
        }
    }

    #endregion

    #region Private Helper Methods

    private async Task EnsureGameConsoleNetworkAsync(CancellationToken cancellationToken)
    {
        const string networkName = "gameconsole-network";
        
        try
        {
            await _dockerClient.Networks.InspectNetworkAsync(networkName, cancellationToken);
        }
        catch (DockerNetworkNotFoundException)
        {
            _logger.LogInformation("Creating GameConsole network: {NetworkName}", networkName);
            await _dockerClient.Networks.CreateNetworkAsync(new NetworksCreateParameters
            {
                Name = networkName,
                Driver = "bridge",
                Labels = new Dictionary<string, string>
                {
                    { "project", "gameconsole" },
                    { "managed-by", "deployment-containers" }
                }
            }, cancellationToken);
        }
    }

    private async Task RefreshServiceListAsync(CancellationToken cancellationToken)
    {
        try
        {
            var containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = true,
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    { "label", new Dictionary<string, bool> { { "managed-by=deployment-containers", true } } }
                }
            }, cancellationToken);

            _services.Clear();
            
            foreach (var container in containers)
            {
                var serviceName = container.Names.FirstOrDefault()?.TrimStart('/') ?? container.ID[..12];
                _services[serviceName] = new ServiceInfo
                {
                    ServiceName = serviceName,
                    Image = container.Image,
                    CreatedAt = container.Created,
                    Status = container.State switch
                    {
                        "running" => DeploymentStatus.Running,
                        "exited" => DeploymentStatus.Terminated,
                        "created" => DeploymentStatus.Deploying,
                        _ => DeploymentStatus.Unknown
                    },
                    Labels = container.Labels != null ? new Dictionary<string, string>(container.Labels) : new Dictionary<string, string>()
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh service list");
        }
    }

    private async Task<List<ContainerListResponse>> GetServiceContainersAsync(string serviceName, CancellationToken cancellationToken)
    {
        var containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters
        {
            All = true,
            Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                { "name", new Dictionary<string, bool> { { serviceName, true } } }
            }
        }, cancellationToken);

        return containers.ToList();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(DockerDeploymentProvider));
    }

    private void ThrowIfNotRunning()
    {
        ThrowIfDisposed();
        if (!_isRunning) throw new InvalidOperationException("DockerDeploymentProvider is not running");
    }

    #endregion
}