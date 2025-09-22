using GameConsole.Deployment.Containers.Interfaces;
using GameConsole.Deployment.Containers.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.Deployment.Containers.Services;

/// <summary>
/// Main container orchestrator coordinating deployment operations across multiple providers.
/// Manages deployment lifecycle, provider selection, and status aggregation.
/// </summary>
public class ContainerOrchestrator : BaseDeploymentService, IContainerOrchestrator
{
    private readonly IEnumerable<IDeploymentProvider> _providers;
    private readonly IContainerHealthMonitor _healthMonitor;
    private readonly ConcurrentDictionary<string, OrchestratorDeploymentInfo> _deployments = new();

    public event EventHandler<DeploymentStatusChangedEventArgs>? DeploymentStatusChanged;

    public ContainerOrchestrator(
        IEnumerable<IDeploymentProvider> providers, 
        IContainerHealthMonitor healthMonitor,
        ILogger<ContainerOrchestrator> logger) : base(logger)
    {
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        _healthMonitor = healthMonitor ?? throw new ArgumentNullException(nameof(healthMonitor));
    }

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing Container Orchestrator with {ProviderCount} providers", _providers.Count());
        
        // Initialize all providers
        foreach (var provider in _providers)
        {
            try
            {
                await provider.InitializeAsync(cancellationToken);
                _logger.LogDebug("Initialized provider: {ProviderType}", provider.ProviderType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize provider: {ProviderType}", provider.ProviderType);
                throw;
            }
        }
        
        // Initialize health monitor
        await _healthMonitor.InitializeAsync(cancellationToken);
    }

    protected override async Task OnStartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Container Orchestrator");
        
        // Start all providers
        foreach (var provider in _providers)
        {
            await provider.StartAsync(cancellationToken);
        }
        
        // Start health monitor
        await _healthMonitor.StartAsync(cancellationToken);
        
        _logger.LogInformation("Container Orchestrator started with providers: {Providers}", 
            string.Join(", ", _providers.Select(p => p.ProviderType)));
    }

    protected override async Task OnStopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping Container Orchestrator");
        
        // Stop health monitor
        await _healthMonitor.StopAsync(cancellationToken);
        
        // Stop all providers
        foreach (var provider in _providers)
        {
            try
            {
                await provider.StopAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping provider: {ProviderType}", provider.ProviderType);
            }
        }
    }

    protected override async ValueTask OnDisposeAsync()
    {
        await _healthMonitor.DisposeAsync();
        
        foreach (var provider in _providers)
        {
            try
            {
                await provider.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing provider: {ProviderType}", provider.ProviderType);
            }
        }
        
        await base.OnDisposeAsync();
    }

    public async Task<DeploymentResult> DeployAsync(DeploymentConfiguration config, CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
            return DeploymentResult.CreateFailure("Orchestrator is not running");

        try
        {
            _logger.LogInformation("Deploying {DeploymentName} with image {Image}", config.Name, config.Image);

            // Select appropriate provider
            var provider = SelectProvider(config);
            if (provider == null)
            {
                return DeploymentResult.CreateFailure("No suitable deployment provider found for the configuration");
            }

            _logger.LogDebug("Using provider {ProviderType} for deployment {DeploymentName}", 
                provider.ProviderType, config.Name);

            // Deploy using selected provider
            var result = await provider.CreateDeploymentAsync(config, cancellationToken);
            
            if (result.Success)
            {
                // Track the deployment
                var deploymentInfo = new OrchestratorDeploymentInfo
                {
                    DeploymentId = result.DeploymentId,
                    Configuration = config,
                    Provider = provider,
                    CreatedAt = DateTime.UtcNow,
                    LastStatusCheck = DateTime.UtcNow
                };
                
                _deployments[result.DeploymentId] = deploymentInfo;

                // Start health monitoring if configured
                if (config.HealthCheck != null)
                {
                    await _healthMonitor.ConfigureHealthCheckAsync(result.DeploymentId, config.HealthCheck, cancellationToken);
                    await _healthMonitor.StartMonitoringAsync(result.DeploymentId, config.HealthCheck.Interval, cancellationToken);
                }

                _logger.LogInformation("Successfully deployed {DeploymentName} with ID {DeploymentId} using {ProviderType}", 
                    config.Name, result.DeploymentId, provider.ProviderType);
                
                // Notify about deployment creation
                OnDeploymentStatusChanged(result.DeploymentId, "Deployed", null);
            }

            return result;
        }
        catch (Exception ex)
        {
            return HandleException(ex, "deploying", () => 
                DeploymentResult.CreateFailure($"Failed to deploy: {ex.Message}", ex));
        }
    }

    public async Task<DeploymentResult> ScaleAsync(string deploymentId, int replicas, CancellationToken cancellationToken = default)
    {
        if (!_deployments.TryGetValue(deploymentId, out var deploymentInfo))
            return DeploymentResult.CreateFailure($"Deployment {deploymentId} not found");

        try
        {
            _logger.LogInformation("Scaling deployment {DeploymentId} to {Replicas} replicas", deploymentId, replicas);

            var result = await deploymentInfo.Provider.ScaleDeploymentAsync(deploymentId, replicas, cancellationToken);
            
            if (result.Success)
            {
                deploymentInfo.Configuration.Replicas = replicas;
                deploymentInfo.LastStatusCheck = DateTime.UtcNow;
                
                OnDeploymentStatusChanged(deploymentId, "Scaled", null);
            }

            return result;
        }
        catch (Exception ex)
        {
            return HandleException(ex, "scaling", () => 
                DeploymentResult.CreateFailure($"Failed to scale deployment: {ex.Message}", ex));
        }
    }

    public async Task<DeploymentStatus> GetStatusAsync(string deploymentId, CancellationToken cancellationToken = default)
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
            var status = await deploymentInfo.Provider.GetDeploymentStatusAsync(deploymentId, cancellationToken);
            deploymentInfo.LastStatusCheck = DateTime.UtcNow;
            
            // Enhance with health information
            try
            {
                var healthResult = await _healthMonitor.CheckHealthAsync(deploymentId, cancellationToken);
                status.HealthStatus = healthResult.Status;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to get health status for deployment {DeploymentId}", deploymentId);
            }

            return status;
        }
        catch (Exception ex)
        {
            return HandleException(ex, "getting status", () => new DeploymentStatus
            {
                DeploymentId = deploymentId,
                Status = "Error",
                LastUpdated = DateTime.UtcNow
            });
        }
    }

    public async Task<bool> RemoveAsync(string deploymentId, CancellationToken cancellationToken = default)
    {
        if (!_deployments.TryGetValue(deploymentId, out var deploymentInfo))
        {
            _logger.LogWarning("Deployment {DeploymentId} not found for removal", deploymentId);
            return false;
        }

        try
        {
            _logger.LogInformation("Removing deployment {DeploymentId}", deploymentId);

            // Stop health monitoring
            await _healthMonitor.StopMonitoringAsync(deploymentId, cancellationToken);

            // Remove deployment using provider
            var success = await deploymentInfo.Provider.DeleteDeploymentAsync(deploymentId, cancellationToken);
            
            if (success)
            {
                _deployments.TryRemove(deploymentId, out _);
                OnDeploymentStatusChanged(deploymentId, "Removed", null);
                
                _logger.LogInformation("Successfully removed deployment {DeploymentId}", deploymentId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove deployment {DeploymentId}", deploymentId);
            return false;
        }
    }

    public async Task<IEnumerable<DeploymentStatus>> ListDeploymentsAsync(CancellationToken cancellationToken = default)
    {
        var statuses = new List<DeploymentStatus>();
        
        foreach (var deployment in _deployments.Values)
        {
            try
            {
                var status = await GetStatusAsync(deployment.DeploymentId, cancellationToken);
                statuses.Add(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get status for deployment {DeploymentId}", deployment.DeploymentId);
                
                // Add error status
                statuses.Add(new DeploymentStatus
                {
                    DeploymentId = deployment.DeploymentId,
                    Status = "Error",
                    LastUpdated = DateTime.UtcNow
                });
            }
        }
        
        return statuses;
    }

    public async Task<DeploymentResult> UpdateAsync(string deploymentId, DeploymentConfiguration config, CancellationToken cancellationToken = default)
    {
        if (!_deployments.TryGetValue(deploymentId, out var deploymentInfo))
            return DeploymentResult.CreateFailure($"Deployment {deploymentId} not found");

        try
        {
            _logger.LogInformation("Updating deployment {DeploymentId}", deploymentId);

            var result = await deploymentInfo.Provider.UpdateDeploymentAsync(deploymentId, config, cancellationToken);
            
            if (result.Success)
            {
                deploymentInfo.Configuration = config;
                deploymentInfo.LastStatusCheck = DateTime.UtcNow;
                
                // Update health monitoring configuration if needed
                if (config.HealthCheck != null)
                {
                    await _healthMonitor.ConfigureHealthCheckAsync(deploymentId, config.HealthCheck, cancellationToken);
                }
                
                OnDeploymentStatusChanged(deploymentId, "Updated", null);
            }

            return result;
        }
        catch (Exception ex)
        {
            return HandleException(ex, "updating", () => 
                DeploymentResult.CreateFailure($"Failed to update deployment: {ex.Message}", ex));
        }
    }

    private IDeploymentProvider? SelectProvider(DeploymentConfiguration config)
    {
        // Simple provider selection logic - in production, this could be more sophisticated
        foreach (var provider in _providers)
        {
            if (provider.SupportsConfiguration(config))
            {
                return provider;
            }
        }
        
        return null;
    }

    private void OnDeploymentStatusChanged(string deploymentId, string newStatus, string? previousStatus)
    {
        try
        {
            if (_deployments.TryGetValue(deploymentId, out var deploymentInfo))
            {
                var args = new DeploymentStatusChangedEventArgs
                {
                    Deployment = new DeploymentStatus
                    {
                        DeploymentId = deploymentId,
                        Status = newStatus,
                        TotalReplicas = deploymentInfo.Configuration.Replicas,
                        LastUpdated = DateTime.UtcNow
                    },
                    PreviousStatus = previousStatus
                };

                DeploymentStatusChanged?.Invoke(this, args);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error raising deployment status changed event for {DeploymentId}", deploymentId);
        }
    }

    private class OrchestratorDeploymentInfo
    {
        public string DeploymentId { get; set; } = string.Empty;
        public DeploymentConfiguration Configuration { get; set; } = null!;
        public IDeploymentProvider Provider { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime LastStatusCheck { get; set; }
    }
}