using GameConsole.Core.Abstractions;
using GameConsole.Core.Registry;
using Microsoft.Extensions.Logging;
using k8s;
using k8s.Models;
using System.Collections.Concurrent;
using System.Text;

namespace GameConsole.Deployment.Containers;

/// <summary>
/// Kubernetes implementation of the container orchestrator service.
/// </summary>
[Service("Kubernetes Deployment Provider", "1.0.0", "Container orchestrator implementation using Kubernetes", 
    Lifetime = ServiceLifetime.Singleton, Categories = new[] { "Deployment" })]
public class KubernetesDeploymentProvider : IContainerOrchestrator
{
    private readonly ILogger<KubernetesDeploymentProvider> _logger;
    private readonly Kubernetes _kubernetesClient;
    private readonly ConcurrentDictionary<string, ServiceInfo> _services = new();
    private readonly string _namespace;
    private bool _isRunning;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="KubernetesDeploymentProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="namespace">The Kubernetes namespace to deploy to.</param>
    public KubernetesDeploymentProvider(ILogger<KubernetesDeploymentProvider> logger, string @namespace = "gameconsole")
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _namespace = @namespace;

        // Initialize Kubernetes client
        var config = KubernetesClientConfiguration.InClusterConfig();
        if (config == null)
        {
            // Fall back to local kubeconfig
            config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
        }
        
        _kubernetesClient = new Kubernetes(config);
    }

    #region IService Implementation

    public bool IsRunning => _isRunning && !_disposed;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        _logger.LogInformation("Initializing KubernetesDeploymentProvider");

        try
        {
            // Verify cluster connectivity
            var version = await _kubernetesClient.Version.GetCodeAsync(cancellationToken: cancellationToken);
            _logger.LogInformation("Connected to Kubernetes cluster version {Version}", version.GitVersion);

            // Ensure namespace exists
            await EnsureNamespaceAsync(cancellationToken);

            _logger.LogInformation("Initialized KubernetesDeploymentProvider");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize KubernetesDeploymentProvider");
            throw;
        }
    }

    #pragma warning disable CS1998 // Async method lacks 'await' operators
    public async Task StartAsync(CancellationToken cancellationToken = default)
    #pragma warning restore CS1998
    {
        ThrowIfDisposed();
        _logger.LogInformation("Starting KubernetesDeploymentProvider");
        
        _isRunning = true;
        
        // Load existing services
        await RefreshServiceListAsync(cancellationToken);
        
        _logger.LogInformation("Started KubernetesDeploymentProvider with {ServiceCount} existing services", _services.Count);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping KubernetesDeploymentProvider");
        _isRunning = false;
        _logger.LogInformation("Stopped KubernetesDeploymentProvider");
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        if (_isRunning)
        {
            await StopAsync();
        }

        _kubernetesClient?.Dispose();
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
            var deployment = CreateDeployment(configuration);
            var service = CreateService(configuration);

            // Create deployment
            var deploymentResult = await _kubernetesClient.AppsV1.CreateNamespacedDeploymentAsync(
                deployment, _namespace, cancellationToken: cancellationToken);

            // Create service if ports are exposed
            V1Service? serviceResult = null;
            if (configuration.PortMappings.Any())
            {
                serviceResult = await _kubernetesClient.CoreV1.CreateNamespacedServiceAsync(
                    service, _namespace, cancellationToken: cancellationToken);
            }

            // Store service info
            var serviceInfo = new ServiceInfo
            {
                ServiceName = configuration.ServiceName,
                Image = configuration.Image,
                Strategy = configuration.Strategy,
                CreatedAt = DateTime.UtcNow,
                Status = DeploymentStatus.Deploying,
                Labels = configuration.Labels
            };
            _services[configuration.ServiceName] = serviceInfo;

            _logger.LogInformation("Successfully deployed service {ServiceName}", configuration.ServiceName);

            return new DeploymentResult
            {
                IsSuccess = true,
                DeploymentId = deploymentResult.Metadata.Uid,
                ServiceName = configuration.ServiceName,
                Metadata = new Dictionary<string, object>
                {
                    { "DeploymentName", deploymentResult.Metadata.Name },
                    { "ServiceName", serviceResult?.Metadata?.Name ?? "none" },
                    { "Namespace", _namespace }
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

        try
        {
            var deployment = await _kubernetesClient.AppsV1.ReadNamespacedDeploymentAsync(
                serviceName, _namespace, cancellationToken: cancellationToken);

            var previousCount = deployment.Spec.Replicas ?? 0;

            // Update replica count
            deployment.Spec.Replicas = instanceCount;
            
            await _kubernetesClient.AppsV1.ReplaceNamespacedDeploymentAsync(
                deployment, serviceName, _namespace, cancellationToken: cancellationToken);

            _logger.LogInformation("Successfully scaled service {ServiceName} from {PreviousCount} to {NewCount} instances",
                serviceName, previousCount, instanceCount);

            return new ScalingResult
            {
                IsSuccess = true,
                ServiceName = serviceName,
                PreviousInstanceCount = previousCount,
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
            var greenConfig = newConfiguration with { ServiceName = greenServiceName };
            
            // Deploy green version
            var greenResult = await DeployAsync(greenConfig, cancellationToken);
            if (!greenResult.IsSuccess)
            {
                return greenResult;
            }

            // Wait for green deployment to be ready
            await WaitForDeploymentReadyAsync(greenServiceName, TimeSpan.FromMinutes(5), cancellationToken);

            // Update service selector to point to green deployment
            try
            {
                var service = await _kubernetesClient.CoreV1.ReadNamespacedServiceAsync(
                    serviceName, _namespace, cancellationToken: cancellationToken);
                
                service.Spec.Selector["version"] = "green";
                await _kubernetesClient.CoreV1.ReplaceNamespacedServiceAsync(
                    service, serviceName, _namespace, cancellationToken: cancellationToken);

                // Wait a moment for traffic to switch
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);

                // Remove blue deployment
                await RemoveServiceAsync(serviceName, cancellationToken);

                // Rename green deployment to original name
                var greenDeployment = await _kubernetesClient.AppsV1.ReadNamespacedDeploymentAsync(
                    greenServiceName, _namespace, cancellationToken: cancellationToken);
                
                greenDeployment.Metadata.Name = serviceName;
                greenDeployment.Spec.Selector.MatchLabels["app"] = serviceName;
                greenDeployment.Spec.Template.Metadata.Labels["app"] = serviceName;

                await _kubernetesClient.AppsV1.CreateNamespacedDeploymentAsync(
                    greenDeployment, _namespace, cancellationToken: cancellationToken);

                await _kubernetesClient.AppsV1.DeleteNamespacedDeploymentAsync(
                    greenServiceName, _namespace, cancellationToken: cancellationToken);

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
                _logger.LogError(ex, "Failed to switch traffic during blue-green deployment for service {ServiceName}", serviceName);
                // Clean up green deployment
                await RemoveServiceAsync(greenServiceName, cancellationToken);
                throw;
            }
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
            var deployment = await _kubernetesClient.AppsV1.ReadNamespacedDeploymentAsync(
                serviceName, _namespace, cancellationToken: cancellationToken);

            var status = deployment.Status;
            var runningInstances = status.ReadyReplicas ?? 0;
            var desiredInstances = deployment.Spec.Replicas ?? 0;

            var deploymentStatus = DeploymentStatus.Unknown;
            if (status.Replicas == status.ReadyReplicas && runningInstances > 0)
            {
                deploymentStatus = DeploymentStatus.Running;
            }
            else if (status.ReadyReplicas > 0)
            {
                deploymentStatus = DeploymentStatus.Updating;
            }
            else if (status.Replicas > 0)
            {
                deploymentStatus = DeploymentStatus.Deploying;
            }

            return new ServiceStatus
            {
                ServiceName = serviceName,
                Status = deploymentStatus,
                RunningInstances = runningInstances,
                DesiredInstances = desiredInstances,
                Details = new Dictionary<string, object>
                {
                    { "Replicas", status.Replicas ?? 0 },
                    { "UpdatedReplicas", status.UpdatedReplicas ?? 0 },
                    { "AvailableReplicas", status.AvailableReplicas ?? 0 },
                    { "ReadyReplicas", status.ReadyReplicas ?? 0 }
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
            // Remove deployment
            try
            {
                await _kubernetesClient.AppsV1.DeleteNamespacedDeploymentAsync(
                    serviceName, _namespace, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove deployment {ServiceName}", serviceName);
            }

            // Remove service
            try
            {
                await _kubernetesClient.CoreV1.DeleteNamespacedServiceAsync(
                    serviceName, _namespace, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove service {ServiceName}", serviceName);
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

    private async Task EnsureNamespaceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _kubernetesClient.CoreV1.ReadNamespaceAsync(_namespace, cancellationToken: cancellationToken);
        }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogInformation("Creating namespace: {Namespace}", _namespace);
            await _kubernetesClient.CoreV1.CreateNamespaceAsync(new V1Namespace
            {
                Metadata = new V1ObjectMeta
                {
                    Name = _namespace,
                    Labels = new Dictionary<string, string>
                    {
                        { "project", "gameconsole" },
                        { "managed-by", "deployment-containers" }
                    }
                }
            }, cancellationToken: cancellationToken);
        }
    }

    private V1Deployment CreateDeployment(ContainerConfiguration configuration)
    {
        var labels = new Dictionary<string, string>(configuration.Labels)
        {
            { "app", configuration.ServiceName },
            { "version", "blue" },
            { "managed-by", "deployment-containers" }
        };

        var container = new V1Container
        {
            Name = configuration.ServiceName,
            Image = configuration.Image,
            Env = configuration.EnvironmentVariables.Select(kv => new V1EnvVar
            {
                Name = kv.Key,
                Value = kv.Value
            }).ToList(),
            Ports = configuration.PortMappings.Keys.Select(port => new V1ContainerPort
            {
                ContainerPort = port,
                Protocol = "TCP"
            }).ToList()
        };

        // Apply resource limits
        if (configuration.ResourceLimits != null)
        {
            var limits = new Dictionary<string, ResourceQuantity>();
            var requests = new Dictionary<string, ResourceQuantity>();

            if (configuration.ResourceLimits.CpuLimit.HasValue)
            {
                limits["cpu"] = new ResourceQuantity($"{configuration.ResourceLimits.CpuLimit.Value}");
            }
            
            if (configuration.ResourceLimits.MemoryLimit.HasValue)
            {
                limits["memory"] = new ResourceQuantity($"{configuration.ResourceLimits.MemoryLimit.Value}");
            }

            if (configuration.ResourceLimits.CpuRequest.HasValue)
            {
                requests["cpu"] = new ResourceQuantity($"{configuration.ResourceLimits.CpuRequest.Value}");
            }
            
            if (configuration.ResourceLimits.MemoryRequest.HasValue)
            {
                requests["memory"] = new ResourceQuantity($"{configuration.ResourceLimits.MemoryRequest.Value}");
            }

            if (limits.Any() || requests.Any())
            {
                container.Resources = new V1ResourceRequirements
                {
                    Limits = limits.Any() ? limits : null,
                    Requests = requests.Any() ? requests : null
                };
            }
        }

        // Add health check if configured
        if (configuration.HealthCheck != null)
        {
            container.ReadinessProbe = new V1Probe
            {
                HttpGet = new V1HTTPGetAction
                {
                    Path = configuration.HealthCheck.Path,
                    Port = configuration.HealthCheck.Port
                },
                InitialDelaySeconds = (int)configuration.HealthCheck.InitialDelay.TotalSeconds,
                PeriodSeconds = (int)configuration.HealthCheck.Interval.TotalSeconds,
                TimeoutSeconds = (int)configuration.HealthCheck.Timeout.TotalSeconds,
                FailureThreshold = configuration.HealthCheck.FailureThreshold,
                SuccessThreshold = configuration.HealthCheck.SuccessThreshold
            };

            container.LivenessProbe = new V1Probe
            {
                HttpGet = new V1HTTPGetAction
                {
                    Path = configuration.HealthCheck.Path,
                    Port = configuration.HealthCheck.Port
                },
                InitialDelaySeconds = (int)configuration.HealthCheck.InitialDelay.TotalSeconds,
                PeriodSeconds = (int)configuration.HealthCheck.Interval.TotalSeconds,
                TimeoutSeconds = (int)configuration.HealthCheck.Timeout.TotalSeconds,
                FailureThreshold = configuration.HealthCheck.FailureThreshold
            };
        }

        return new V1Deployment
        {
            Metadata = new V1ObjectMeta
            {
                Name = configuration.ServiceName,
                Labels = labels
            },
            Spec = new V1DeploymentSpec
            {
                Replicas = configuration.Replicas,
                Selector = new V1LabelSelector
                {
                    MatchLabels = new Dictionary<string, string> { { "app", configuration.ServiceName } }
                },
                Template = new V1PodTemplateSpec
                {
                    Metadata = new V1ObjectMeta
                    {
                        Labels = labels
                    },
                    Spec = new V1PodSpec
                    {
                        Containers = new List<V1Container> { container }
                    }
                },
                Strategy = new V1DeploymentStrategy
                {
                    Type = configuration.Strategy switch
                    {
                        DeploymentStrategy.RollingUpdate => "RollingUpdate",
                        DeploymentStrategy.Recreate => "Recreate",
                        _ => "RollingUpdate"
                    }
                }
            }
        };
    }

    private V1Service CreateService(ContainerConfiguration configuration)
    {
        var labels = new Dictionary<string, string>(configuration.Labels)
        {
            { "app", configuration.ServiceName },
            { "managed-by", "deployment-containers" }
        };

        return new V1Service
        {
            Metadata = new V1ObjectMeta
            {
                Name = configuration.ServiceName,
                Labels = labels
            },
            Spec = new V1ServiceSpec
            {
                Selector = new Dictionary<string, string> { { "app", configuration.ServiceName } },
                Ports = configuration.PortMappings.Select(kv => new V1ServicePort
                {
                    Port = kv.Value,
                    TargetPort = kv.Key,
                    Protocol = "TCP"
                }).ToList(),
                Type = "ClusterIP"
            }
        };
    }

    private async Task RefreshServiceListAsync(CancellationToken cancellationToken)
    {
        try
        {
            var deployments = await _kubernetesClient.AppsV1.ListNamespacedDeploymentAsync(
                _namespace, labelSelector: "managed-by=deployment-containers", cancellationToken: cancellationToken);

            _services.Clear();
            
            foreach (var deployment in deployments.Items)
            {
                var status = deployment.Status;
                _services[deployment.Metadata.Name] = new ServiceInfo
                {
                    ServiceName = deployment.Metadata.Name,
                    Image = deployment.Spec.Template.Spec.Containers.FirstOrDefault()?.Image ?? "unknown",
                    CreatedAt = deployment.Metadata.CreationTimestamp ?? DateTime.MinValue,
                    Status = (status.ReadyReplicas ?? 0) == (deployment.Spec.Replicas ?? 0) && (status.ReadyReplicas ?? 0) > 0 
                        ? DeploymentStatus.Running 
                        : DeploymentStatus.Deploying,
                    Labels = deployment.Metadata.Labels != null ? new Dictionary<string, string>(deployment.Metadata.Labels) : new Dictionary<string, string>()
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh service list");
        }
    }

    private async Task WaitForDeploymentReadyAsync(string serviceName, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        while (stopwatch.Elapsed < timeout)
        {
            try
            {
                var deployment = await _kubernetesClient.AppsV1.ReadNamespacedDeploymentAsync(
                    serviceName, _namespace, cancellationToken: cancellationToken);

                if (deployment.Status.ReadyReplicas == deployment.Spec.Replicas && deployment.Status.ReadyReplicas > 0)
                {
                    return;
                }

                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking deployment status for {ServiceName}", serviceName);
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }

        throw new TimeoutException($"Deployment {serviceName} did not become ready within {timeout}");
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(KubernetesDeploymentProvider));
    }

    private void ThrowIfNotRunning()
    {
        ThrowIfDisposed();
        if (!_isRunning) throw new InvalidOperationException("KubernetesDeploymentProvider is not running");
    }

    #endregion
}