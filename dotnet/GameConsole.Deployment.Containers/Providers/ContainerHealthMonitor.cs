using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;
using System.Reactive.Subjects;

namespace GameConsole.Deployment.Containers.Providers;

/// <summary>
/// Container health monitoring service.
/// Provides health checking, monitoring, and alerting for containerized deployments.
/// </summary>
[Service("ContainerHealthMonitor", "1.0.0", "Container health monitoring service", Lifetime = ServiceLifetime.Singleton, Categories = new[] { "Deployment" })]
public class ContainerHealthMonitor : IHealthMonitorProvider
{
    private readonly ILogger<ContainerHealthMonitor> _logger;
    private readonly Dictionary<string, HealthMonitoringContext> _monitoringContexts = new();
    private readonly Dictionary<string, HealthCheckConfiguration> _healthConfigurations = new();
    private bool _isRunning;

    /// <inheritdoc />
    public string ProviderName => "ContainerHealthMonitor";

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    public ContainerHealthMonitor(ILogger<ContainerHealthMonitor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing ContainerHealthMonitor");
        _logger.LogInformation("ContainerHealthMonitor initialized successfully");
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting ContainerHealthMonitor");
        _isRunning = true;
        _logger.LogInformation("ContainerHealthMonitor started successfully");
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping ContainerHealthMonitor");
        _isRunning = false;

        // Stop all monitoring contexts
        foreach (var context in _monitoringContexts.Values)
        {
            context.Subject.OnCompleted();
            context.CancellationTokenSource.Cancel();
        }

        _monitoringContexts.Clear();
        _logger.LogInformation("ContainerHealthMonitor stopped successfully");
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isRunning)
        {
            await StopAsync();
        }

        foreach (var context in _monitoringContexts.Values)
        {
            context.Subject.Dispose();
            context.CancellationTokenSource.Dispose();
        }

        _monitoringContexts.Clear();
        _healthConfigurations.Clear();
        _logger.LogInformation("ContainerHealthMonitor disposed");
    }

    /// <inheritdoc />
    public IObservable<HealthStatus> MonitorHealthAsync(string deploymentId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting health monitoring for deployment {DeploymentId}", deploymentId);

        if (_monitoringContexts.TryGetValue(deploymentId, out var existingContext))
        {
            _logger.LogDebug("Returning existing monitoring context for {DeploymentId}", deploymentId);
            return existingContext.Subject;
        }

        var context = new HealthMonitoringContext
        {
            DeploymentId = deploymentId,
            Subject = new Subject<HealthStatus>(),
            CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
        };

        _monitoringContexts[deploymentId] = context;

        // Start background monitoring
        _ = Task.Run(async () => await RunHealthMonitoringLoop(context), context.CancellationTokenSource.Token);

        _logger.LogInformation("Health monitoring started for deployment {DeploymentId}", deploymentId);
        return context.Subject;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(string deploymentId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Performing health check for deployment {DeploymentId}", deploymentId);

        try
        {
            var startTime = DateTimeOffset.UtcNow;
            
            // Simulate health check process
            await SimulateHealthCheck(deploymentId, cancellationToken);
            
            var responseTime = DateTimeOffset.UtcNow - startTime;

            // Simulate different health states based on deployment ID for demo
            var healthState = DetermineHealthState(deploymentId);
            var message = GetHealthMessage(healthState);

            var result = new HealthCheckResult
            {
                Status = healthState,
                Message = message,
                ResponseTime = responseTime,
                Data = new Dictionary<string, object>
                {
                    { "DeploymentId", deploymentId },
                    { "CheckType", "ContainerHealth" },
                    { "Provider", ProviderName }
                }
            };

            _logger.LogDebug("Health check completed for {DeploymentId}. Status: {Status}, Response Time: {ResponseTime}ms", 
                deploymentId, healthState, responseTime.TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for deployment {DeploymentId}", deploymentId);
            
            return new HealthCheckResult
            {
                Status = HealthState.Unhealthy,
                Message = $"Health check failed: {ex.Message}",
                ResponseTime = TimeSpan.Zero,
                Data = new Dictionary<string, object>
                {
                    { "DeploymentId", deploymentId },
                    { "Error", ex.Message }
                }
            };
        }
    }

    /// <inheritdoc />
    public async Task ConfigureHealthCheckAsync(string deploymentId, HealthCheckConfiguration configuration, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Configuring health check for deployment {DeploymentId}", deploymentId);

        _healthConfigurations[deploymentId] = configuration;

        // If monitoring is already running, update the configuration
        if (_monitoringContexts.TryGetValue(deploymentId, out var context))
        {
            _logger.LogDebug("Updating existing monitoring configuration for {DeploymentId}", deploymentId);
            // In a real implementation, this would update the monitoring parameters
        }

        _logger.LogInformation("Health check configuration updated for deployment {DeploymentId}", deploymentId);
        await Task.CompletedTask;
    }

    private async Task RunHealthMonitoringLoop(HealthMonitoringContext context)
    {
        _logger.LogDebug("Starting health monitoring loop for deployment {DeploymentId}", context.DeploymentId);

        try
        {
            while (!context.CancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var healthResult = await CheckHealthAsync(context.DeploymentId, context.CancellationTokenSource.Token);
                    
                    var healthStatus = new HealthStatus
                    {
                        Status = healthResult.Status,
                        Message = healthResult.Message,
                        Components = new List<ComponentHealth>
                        {
                            new()
                            {
                                Name = "Container",
                                Status = healthResult.Status,
                                Message = healthResult.Message
                            }
                        }
                    };

                    context.Subject.OnNext(healthStatus);

                    // Get monitoring interval from configuration or use default
                    var interval = _healthConfigurations.TryGetValue(context.DeploymentId, out var config) 
                        ? config.Interval 
                        : TimeSpan.FromSeconds(30);

                    await Task.Delay(interval, context.CancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in health monitoring loop for deployment {DeploymentId}", context.DeploymentId);
                    
                    var errorStatus = new HealthStatus
                    {
                        Status = HealthState.Unknown,
                        Message = $"Monitoring error: {ex.Message}"
                    };
                    
                    context.Subject.OnNext(errorStatus);
                    
                    // Wait before retrying
                    await Task.Delay(TimeSpan.FromSeconds(10), context.CancellationTokenSource.Token);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Health monitoring loop cancelled for deployment {DeploymentId}", context.DeploymentId);
        }
        finally
        {
            context.Subject.OnCompleted();
            _monitoringContexts.Remove(context.DeploymentId);
            _logger.LogDebug("Health monitoring loop ended for deployment {DeploymentId}", context.DeploymentId);
        }
    }

    private async Task SimulateHealthCheck(string deploymentId, CancellationToken cancellationToken)
    {
        // Simulate health check operations
        await Task.Delay(Random.Shared.Next(50, 200), cancellationToken); // Simulate variable response time
    }

    private HealthState DetermineHealthState(string deploymentId)
    {
        // Simulate different health states for demo purposes
        var hash = deploymentId.GetHashCode();
        var states = Enum.GetValues<HealthState>();
        return states[Math.Abs(hash) % states.Length];
    }

    private string GetHealthMessage(HealthState state)
    {
        return state switch
        {
            HealthState.Healthy => "All containers are healthy and responding",
            HealthState.Unhealthy => "One or more containers are not responding",
            HealthState.Starting => "Containers are starting up",
            HealthState.Degraded => "Containers are running but performance is degraded",
            HealthState.Unknown => "Health status could not be determined",
            _ => "Unknown health state"
        };
    }

    private class HealthMonitoringContext
    {
        public required string DeploymentId { get; init; }
        public required Subject<HealthStatus> Subject { get; init; }
        public required CancellationTokenSource CancellationTokenSource { get; init; }
    }
}