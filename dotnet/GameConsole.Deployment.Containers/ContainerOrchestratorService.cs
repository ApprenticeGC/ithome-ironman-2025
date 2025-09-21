using GameConsole.Core.Abstractions;
using GameConsole.Deployment.Containers.Providers;
using Microsoft.Extensions.Logging;

namespace GameConsole.Deployment.Containers;

/// <summary>
/// Container orchestration service implementation.
/// Coordinates multiple deployment providers and provides unified container management.
/// </summary>
[Service("ContainerOrchestratorService", "1.0.0", "Container orchestration service for deployment management", Lifetime = ServiceLifetime.Singleton, Categories = new[] { "Deployment" })]
public class ContainerOrchestratorService : IContainerOrchestrator
{
    private readonly ILogger<ContainerOrchestratorService> _logger;
    private readonly List<IDeploymentProvider> _deploymentProviders = new();
    private readonly Dictionary<string, string> _deploymentProviderMap = new(); // deploymentId -> providerName
    private bool _isRunning;

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    public ContainerOrchestratorService(ILogger<ContainerOrchestratorService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers a deployment provider.
    /// </summary>
    /// <param name="provider">The deployment provider to register.</param>
    public void RegisterProvider(IDeploymentProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        
        _deploymentProviders.Add(provider);
        _logger.LogInformation("Registered deployment provider: {ProviderName}", provider.ProviderName);
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing ContainerOrchestratorService");
        
        // Initialize all registered providers
        foreach (var provider in _deploymentProviders)
        {
            try
            {
                await provider.InitializeAsync(cancellationToken);
                _logger.LogInformation("Initialized provider: {ProviderName}", provider.ProviderName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize provider: {ProviderName}", provider.ProviderName);
            }
        }
        
        _logger.LogInformation("ContainerOrchestratorService initialized with {ProviderCount} providers", _deploymentProviders.Count);
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting ContainerOrchestratorService");
        
        // Start all registered providers
        foreach (var provider in _deploymentProviders)
        {
            try
            {
                await provider.StartAsync(cancellationToken);
                _logger.LogInformation("Started provider: {ProviderName}", provider.ProviderName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start provider: {ProviderName}", provider.ProviderName);
            }
        }
        
        _isRunning = true;
        _logger.LogInformation("ContainerOrchestratorService started successfully");
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping ContainerOrchestratorService");
        
        // Stop all registered providers
        foreach (var provider in _deploymentProviders)
        {
            try
            {
                await provider.StopAsync(cancellationToken);
                _logger.LogInformation("Stopped provider: {ProviderName}", provider.ProviderName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop provider: {ProviderName}", provider.ProviderName);
            }
        }
        
        _isRunning = false;
        _logger.LogInformation("ContainerOrchestratorService stopped successfully");
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isRunning)
        {
            await StopAsync();
        }

        foreach (var provider in _deploymentProviders)
        {
            try
            {
                await provider.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing provider: {ProviderName}", provider.ProviderName);
            }
        }

        _deploymentProviders.Clear();
        _deploymentProviderMap.Clear();
        _logger.LogInformation("ContainerOrchestratorService disposed");
    }

    /// <inheritdoc />
    public async Task<DeploymentResult> DeployAsync(DeploymentConfiguration deployment, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deploying application {DeploymentId}: {Name}", deployment.Id, deployment.Name);

        if (!_isRunning)
        {
            throw new InvalidOperationException("Container orchestrator is not running");
        }

        // Select the best provider for this deployment
        var provider = await SelectProviderAsync(deployment, cancellationToken);
        if (provider == null)
        {
            var errorMessage = "No suitable deployment provider found";
            _logger.LogError(errorMessage);
            return new DeploymentResult
            {
                DeploymentId = deployment.Id,
                Success = false,
                Message = errorMessage
            };
        }

        try
        {
            // Validate the deployment configuration with the selected provider
            var validationResult = await provider.ValidateConfigurationAsync(deployment, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessage = $"Deployment configuration validation failed: {string.Join(", ", validationResult.Errors.Select(e => e.Message))}";
                _logger.LogError(errorMessage);
                return new DeploymentResult
                {
                    DeploymentId = deployment.Id,
                    Success = false,
                    Message = errorMessage
                };
            }

            // Log validation warnings if any
            foreach (var warning in validationResult.Warnings)
            {
                _logger.LogWarning("Deployment validation warning for {Property}: {Message}", warning.Property, warning.Message);
            }

            // Deploy using the selected provider
            var result = await provider.DeployAsync(deployment, cancellationToken);
            
            if (result.Success)
            {
                // Track which provider was used for this deployment
                _deploymentProviderMap[deployment.Id] = provider.ProviderName;
                _logger.LogInformation("Successfully deployed {DeploymentId} using provider {ProviderName}", 
                    deployment.Id, provider.ProviderName);
            }
            else
            {
                _logger.LogError("Deployment failed for {DeploymentId}: {Message}", deployment.Id, result.Message);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during deployment of {DeploymentId}", deployment.Id);
            return new DeploymentResult
            {
                DeploymentId = deployment.Id,
                Success = false,
                Message = $"Deployment failed with error: {ex.Message}"
            };
        }
    }

    /// <inheritdoc />
    public async Task<ScalingResult> ScaleAsync(string deploymentId, int replicas, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Scaling deployment {DeploymentId} to {Replicas} replicas", deploymentId, replicas);

        if (!_isRunning)
        {
            throw new InvalidOperationException("Container orchestrator is not running");
        }

        var provider = GetProviderForDeployment(deploymentId);
        if (provider == null)
        {
            return new ScalingResult
            {
                DeploymentId = deploymentId,
                Success = false,
                PreviousReplicas = 0,
                TargetReplicas = replicas,
                CurrentReplicas = 0,
                Message = "Deployment not found or provider not available"
            };
        }

        try
        {
            var result = await provider.ScaleAsync(deploymentId, replicas, cancellationToken);
            
            if (result.Success)
            {
                _logger.LogInformation("Successfully scaled deployment {DeploymentId} to {Replicas} replicas using provider {ProviderName}", 
                    deploymentId, replicas, provider.ProviderName);
            }
            else
            {
                _logger.LogError("Scaling failed for {DeploymentId}: {Message}", deploymentId, result.Message);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during scaling of {DeploymentId}", deploymentId);
            return new ScalingResult
            {
                DeploymentId = deploymentId,
                Success = false,
                PreviousReplicas = 0,
                TargetReplicas = replicas,
                CurrentReplicas = 0,
                Message = $"Scaling failed with error: {ex.Message}"
            };
        }
    }

    /// <inheritdoc />
    public async Task<OperationResult> RemoveAsync(string deploymentId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Removing deployment {DeploymentId}", deploymentId);

        if (!_isRunning)
        {
            throw new InvalidOperationException("Container orchestrator is not running");
        }

        var provider = GetProviderForDeployment(deploymentId);
        if (provider == null)
        {
            return new OperationResult
            {
                Success = false,
                Message = "Deployment not found or provider not available"
            };
        }

        try
        {
            var result = await provider.RemoveAsync(deploymentId, cancellationToken);
            
            if (result.Success)
            {
                // Clean up tracking information
                _deploymentProviderMap.Remove(deploymentId);
                _logger.LogInformation("Successfully removed deployment {DeploymentId} using provider {ProviderName}", 
                    deploymentId, provider.ProviderName);
            }
            else
            {
                _logger.LogError("Removal failed for {DeploymentId}: {Message}", deploymentId, result.Message);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during removal of {DeploymentId}", deploymentId);
            return new OperationResult
            {
                Success = false,
                Message = $"Removal failed with error: {ex.Message}"
            };
        }
    }

    /// <inheritdoc />
    public async Task<DeploymentStatus> GetStatusAsync(string deploymentId, CancellationToken cancellationToken = default)
    {
        var provider = GetProviderForDeployment(deploymentId);
        if (provider == null)
        {
            throw new InvalidOperationException($"Deployment {deploymentId} not found or provider not available");
        }

        return await provider.GetStatusAsync(deploymentId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DeploymentInfo>> ListDeploymentsAsync(CancellationToken cancellationToken = default)
    {
        var allDeployments = new List<DeploymentInfo>();

        foreach (var provider in _deploymentProviders)
        {
            try
            {
                var deployments = await provider.ListDeploymentsAsync(cancellationToken);
                allDeployments.AddRange(deployments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list deployments from provider {ProviderName}", provider.ProviderName);
            }
        }

        _logger.LogDebug("Listed {DeploymentCount} deployments from {ProviderCount} providers", 
            allDeployments.Count, _deploymentProviders.Count);

        return allDeployments;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<LogEntry>> GetLogsAsync(string deploymentId, LogOptions? options = null, CancellationToken cancellationToken = default)
    {
        var provider = GetProviderForDeployment(deploymentId);
        if (provider == null)
        {
            return Enumerable.Empty<LogEntry>();
        }

        try
        {
            return await provider.GetLogsAsync(deploymentId, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve logs for deployment {DeploymentId}", deploymentId);
            return Enumerable.Empty<LogEntry>();
        }
    }

    private async Task<IDeploymentProvider?> SelectProviderAsync(DeploymentConfiguration deployment, CancellationToken cancellationToken)
    {
        // Provider selection strategy - prefer Kubernetes for multi-replica deployments, Docker for simple ones
        var availableProviders = new List<IDeploymentProvider>();

        foreach (var provider in _deploymentProviders)
        {
            try
            {
                if (await provider.IsAvailableAsync(cancellationToken))
                {
                    availableProviders.Add(provider);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Provider {ProviderName} availability check failed", provider.ProviderName);
            }
        }

        if (availableProviders.Count == 0)
        {
            return null;
        }

        // Simple selection logic: prefer Kubernetes for complex deployments, Docker for simple ones
        if (deployment.Replicas > 1 || deployment.Volumes.Count > 0)
        {
            var kubernetesProvider = availableProviders.FirstOrDefault(p => p.ProviderName == "Kubernetes");
            if (kubernetesProvider != null)
            {
                _logger.LogDebug("Selected Kubernetes provider for complex deployment {DeploymentId}", deployment.Id);
                return kubernetesProvider;
            }
        }

        // Fall back to first available provider
        var selectedProvider = availableProviders.First();
        _logger.LogDebug("Selected provider {ProviderName} for deployment {DeploymentId}", 
            selectedProvider.ProviderName, deployment.Id);
        return selectedProvider;
    }

    private IDeploymentProvider? GetProviderForDeployment(string deploymentId)
    {
        if (!_deploymentProviderMap.TryGetValue(deploymentId, out var providerName))
        {
            return null;
        }

        return _deploymentProviders.FirstOrDefault(p => p.ProviderName == providerName);
    }
}