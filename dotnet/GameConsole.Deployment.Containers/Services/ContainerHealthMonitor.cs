using GameConsole.Deployment.Containers.Interfaces;
using GameConsole.Deployment.Containers.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Text.Json;

namespace GameConsole.Deployment.Containers.Services;

/// <summary>
/// Container health monitor providing periodic health checks and event notifications.
/// Supports HTTP-based health checks with configurable intervals and failure thresholds.
/// </summary>
public class ContainerHealthMonitor : BaseDeploymentService, IContainerHealthMonitor
{
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, HealthMonitorInfo> _monitoredDeployments = new();
    private readonly ConcurrentDictionary<string, List<HealthCheckResult>> _healthHistory = new();
    private readonly Subject<ContainerHealthEvent> _healthChangedSubject = new();
    private readonly Subject<HealthCheckFailedEventArgs> _healthCheckFailedSubject = new();
    private readonly Subject<HealthCheckRecoveredEventArgs> _healthCheckRecoveredSubject = new();

    private const int MaxHistorySize = 100;

    public event EventHandler<ContainerHealthEvent>? HealthChanged;
    public event EventHandler<HealthCheckFailedEventArgs>? HealthCheckFailed;
    public event EventHandler<HealthCheckRecoveredEventArgs>? HealthCheckRecovered;

    public ContainerHealthMonitor(ILogger<ContainerHealthMonitor> logger) : base(logger)
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        
        // Subscribe to subjects to raise events
        _healthChangedSubject.Subscribe(args => HealthChanged?.Invoke(this, args));
        _healthCheckFailedSubject.Subscribe(args => HealthCheckFailed?.Invoke(this, args));
        _healthCheckRecoveredSubject.Subscribe(args => HealthCheckRecovered?.Invoke(this, args));
    }

    protected override async ValueTask OnDisposeAsync()
    {
        // Stop all monitoring
        var deploymentIds = _monitoredDeployments.Keys.ToList();
        foreach (var deploymentId in deploymentIds)
        {
            await StopMonitoringAsync(deploymentId);
        }

        _healthChangedSubject?.Dispose();
        _healthCheckFailedSubject?.Dispose();
        _healthCheckRecoveredSubject?.Dispose();
        _httpClient?.Dispose();
        
        await base.OnDisposeAsync();
    }

    public async Task<HealthCheckResult> CheckHealthAsync(string deploymentId, CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
            return HealthCheckResult.Unhealthy("Health monitor service is not running");

        try
        {
            _logger.LogDebug("Performing health check for deployment {DeploymentId}", deploymentId);

            var monitorInfo = _monitoredDeployments.GetValueOrDefault(deploymentId);
            var healthConfig = monitorInfo?.HealthConfiguration ?? new HealthCheckConfiguration();

            var result = await PerformHttpHealthCheckAsync(deploymentId, healthConfig, cancellationToken);
            
            // Store in history
            StoreHealthCheckResult(deploymentId, result);
            
            return result;
        }
        catch (Exception ex)
        {
            return HandleException(ex, "checking health", () => 
                HealthCheckResult.Unhealthy($"Health check failed: {ex.Message}"));
        }
    }

    public Task StartMonitoringAsync(string deploymentId, TimeSpan interval, CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
        {
            _logger.LogWarning("Cannot start monitoring - service is not running");
            return Task.CompletedTask;
        }

        try
        {
            _logger.LogInformation("Starting health monitoring for deployment {DeploymentId} with interval {Interval}", 
                deploymentId, interval);

            var monitorInfo = new HealthMonitorInfo
            {
                DeploymentId = deploymentId,
                Interval = interval,
                HealthConfiguration = new HealthCheckConfiguration(),
                CancellationTokenSource = new CancellationTokenSource()
            };

            _monitoredDeployments[deploymentId] = monitorInfo;

            // Start monitoring task
            _ = Task.Run(async () => await MonitoringLoopAsync(monitorInfo), cancellationToken);

            _logger.LogInformation("Started health monitoring for deployment {DeploymentId}", deploymentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start monitoring for deployment {DeploymentId}", deploymentId);
        }
        
        return Task.CompletedTask;
    }

    public Task StopMonitoringAsync(string deploymentId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_monitoredDeployments.TryRemove(deploymentId, out var monitorInfo))
            {
                _logger.LogInformation("Stopping health monitoring for deployment {DeploymentId}", deploymentId);
                
                monitorInfo.CancellationTokenSource.Cancel();
                monitorInfo.CancellationTokenSource.Dispose();
                
                _logger.LogInformation("Stopped health monitoring for deployment {DeploymentId}", deploymentId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop monitoring for deployment {DeploymentId}", deploymentId);
        }
        
        return Task.CompletedTask;
    }

    public async Task<Dictionary<string, HealthCheckResult>> GetAllHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, HealthCheckResult>();
        
        foreach (var deploymentId in _monitoredDeployments.Keys)
        {
            var result = await CheckHealthAsync(deploymentId, cancellationToken);
            results[deploymentId] = result;
        }
        
        return results;
    }

    public Task ConfigureHealthCheckAsync(string deploymentId, HealthCheckConfiguration healthConfig, CancellationToken cancellationToken = default)
    {
        if (_monitoredDeployments.TryGetValue(deploymentId, out var monitorInfo))
        {
            monitorInfo.HealthConfiguration = healthConfig;
            _logger.LogInformation("Updated health check configuration for deployment {DeploymentId}", deploymentId);
        }
        else
        {
            _logger.LogWarning("Deployment {DeploymentId} is not being monitored", deploymentId);
        }
        
        return Task.CompletedTask;
    }

    public Task<IEnumerable<HealthCheckResult>> GetHealthHistoryAsync(string deploymentId, int limit = 50, CancellationToken cancellationToken = default)
    {
        if (_healthHistory.TryGetValue(deploymentId, out var history))
        {
            return Task.FromResult<IEnumerable<HealthCheckResult>>(history.TakeLast(Math.Min(limit, history.Count)).ToList());
        }
        
        return Task.FromResult(Enumerable.Empty<HealthCheckResult>());
    }

    private async Task MonitoringLoopAsync(HealthMonitorInfo monitorInfo)
    {
        var deploymentId = monitorInfo.DeploymentId;
        var consecutiveFailures = 0;
        var wasHealthy = true;
        var lastFailureTime = DateTime.MinValue;

        _logger.LogDebug("Starting monitoring loop for deployment {DeploymentId}", deploymentId);

        try
        {
            while (!monitorInfo.CancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var result = await PerformHttpHealthCheckAsync(deploymentId, monitorInfo.HealthConfiguration, 
                        monitorInfo.CancellationTokenSource.Token);

                    StoreHealthCheckResult(deploymentId, result);

                    var isCurrentlyHealthy = result.IsHealthy;
                    
                    if (isCurrentlyHealthy)
                    {
                        if (!wasHealthy)
                        {
                            // Recovery detected
                            var recoveryArgs = new HealthCheckRecoveredEventArgs
                            {
                                DeploymentId = deploymentId,
                                HealthCheck = result,
                                DowntimeDuration = lastFailureTime != DateTime.MinValue 
                                    ? DateTime.UtcNow - lastFailureTime 
                                    : TimeSpan.Zero
                            };
                            
                            _healthCheckRecoveredSubject.OnNext(recoveryArgs);
                            _logger.LogInformation("Deployment {DeploymentId} has recovered", deploymentId);
                        }
                        
                        consecutiveFailures = 0;
                        wasHealthy = true;
                    }
                    else
                    {
                        consecutiveFailures++;
                        
                        if (wasHealthy)
                        {
                            lastFailureTime = DateTime.UtcNow;
                        }

                        var failureArgs = new HealthCheckFailedEventArgs
                        {
                            DeploymentId = deploymentId,
                            HealthCheck = result,
                            ConsecutiveFailures = consecutiveFailures,
                            ShouldTriggerRecovery = consecutiveFailures >= monitorInfo.HealthConfiguration.FailureThreshold
                        };
                        
                        _healthCheckFailedSubject.OnNext(failureArgs);
                        wasHealthy = false;
                    }

                    // Always publish health change event
                    var healthEvent = new ContainerHealthEvent
                    {
                        DeploymentId = deploymentId,
                        HealthCheck = result,
                        PreviousStatus = wasHealthy != isCurrentlyHealthy ? (wasHealthy ? "Healthy" : "Unhealthy") : null
                    };
                    
                    _healthChangedSubject.OnNext(healthEvent);

                    await Task.Delay(monitorInfo.Interval, monitorInfo.CancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during health check for deployment {DeploymentId}", deploymentId);
                    
                    var errorResult = HealthCheckResult.Unhealthy($"Health check error: {ex.Message}");
                    StoreHealthCheckResult(deploymentId, errorResult);
                    
                    await Task.Delay(monitorInfo.Interval, monitorInfo.CancellationTokenSource.Token);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when monitoring is stopped
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Monitoring loop failed for deployment {DeploymentId}", deploymentId);
        }

        _logger.LogDebug("Monitoring loop stopped for deployment {DeploymentId}", deploymentId);
    }

    private async Task<HealthCheckResult> PerformHttpHealthCheckAsync(string deploymentId, HealthCheckConfiguration config, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            // For this basic implementation, we'll use a simple HTTP check
            // In a real implementation, this would need to get actual container endpoints
            var healthUrl = $"http://localhost:{config.Port}{config.Path}";
            
            using var response = await _httpClient.GetAsync(healthUrl, cancellationToken);
            var responseTime = DateTime.UtcNow - startTime;
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                return HealthCheckResult.Healthy($"HTTP {(int)response.StatusCode}", responseTime);
            }
            else
            {
                return HealthCheckResult.Unhealthy($"HTTP {(int)response.StatusCode}", responseTime);
            }
        }
        catch (HttpRequestException ex)
        {
            var responseTime = DateTime.UtcNow - startTime;
            return HealthCheckResult.Unhealthy($"HTTP request failed: {ex.Message}", responseTime);
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TaskCanceledException)
        {
            // Timeout
            var responseTime = DateTime.UtcNow - startTime;
            return HealthCheckResult.Unhealthy("Health check timeout", responseTime);
        }
    }

    private void StoreHealthCheckResult(string deploymentId, HealthCheckResult result)
    {
        var history = _healthHistory.GetOrAdd(deploymentId, _ => new List<HealthCheckResult>());
        
        lock (history)
        {
            history.Add(result);
            
            // Keep only the last MaxHistorySize entries
            if (history.Count > MaxHistorySize)
            {
                history.RemoveRange(0, history.Count - MaxHistorySize);
            }
        }
    }

    private class HealthMonitorInfo
    {
        public string DeploymentId { get; set; } = string.Empty;
        public TimeSpan Interval { get; set; }
        public HealthCheckConfiguration HealthConfiguration { get; set; } = new();
        public CancellationTokenSource CancellationTokenSource { get; set; } = new();
    }
}