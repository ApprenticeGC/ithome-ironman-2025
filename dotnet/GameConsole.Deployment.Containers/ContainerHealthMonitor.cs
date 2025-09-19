using GameConsole.Core.Abstractions;
using GameConsole.Core.Registry;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Reactive.Subjects;
using System.Diagnostics;

namespace GameConsole.Deployment.Containers;

/// <summary>
/// Service for monitoring container health and providing recovery capabilities.
/// </summary>
[Service("Container Health Monitor", "1.0.0", "Health monitoring service for containerized applications", 
    Lifetime = ServiceLifetime.Singleton, Categories = new[] { "Deployment", "Monitoring" })]
public class ContainerHealthMonitor : IContainerHealthMonitor
{
    private readonly ILogger<ContainerHealthMonitor> _logger;
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, HealthMonitoringContext> _monitoringContexts = new();
    private readonly ConcurrentDictionary<string, ServiceHealthStatus> _healthStatuses = new();
    private readonly Subject<HealthStatusChangedEventArgs> _healthStatusChangedSubject = new();
    private readonly Timer _monitoringTimer;
    private bool _isRunning;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerHealthMonitor"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ContainerHealthMonitor(ILogger<ContainerHealthMonitor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        
        // Timer for periodic health checks - check every 10 seconds
        _monitoringTimer = new Timer(PerformPeriodicHealthChecks, null, Timeout.Infinite, Timeout.Infinite);
        
        // Subscribe to health status changes
        _healthStatusChangedSubject.Subscribe(args =>
        {
            HealthStatusChanged?.Invoke(this, args);
        });
    }

    #region Events

    public event EventHandler<HealthStatusChangedEventArgs>? HealthStatusChanged;

    #endregion

    #region IService Implementation

    public bool IsRunning => _isRunning && !_disposed;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        _logger.LogInformation("Initializing ContainerHealthMonitor");

        try
        {
            // Configure HTTP client for health checks
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "GameConsole-HealthMonitor/1.0");
            
            _logger.LogInformation("Initialized ContainerHealthMonitor");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize ContainerHealthMonitor");
            throw;
        }
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        _logger.LogInformation("Starting ContainerHealthMonitor");
        
        _isRunning = true;
        
        // Start the monitoring timer
        _monitoringTimer.Change(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
        
        _logger.LogInformation("Started ContainerHealthMonitor");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping ContainerHealthMonitor");
        
        _isRunning = false;
        
        // Stop the monitoring timer
        _monitoringTimer.Change(Timeout.Infinite, Timeout.Infinite);
        
        _logger.LogInformation("Stopped ContainerHealthMonitor");
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        if (_isRunning)
        {
            await StopAsync();
        }

        _monitoringTimer?.Dispose();
        _httpClient?.Dispose();
        _healthStatusChangedSubject?.Dispose();
        _disposed = true;
    }

    #endregion

    #region ICapabilityProvider Implementation

    public Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var capabilities = new List<Type>
        {
            typeof(IContainerHealthMonitor),
            typeof(IService)
        };
        return Task.FromResult<IEnumerable<Type>>(capabilities);
    }

    public Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        var hasCapability = typeof(T) == typeof(IContainerHealthMonitor) || typeof(T) == typeof(IService);
        return Task.FromResult(hasCapability);
    }

    public Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (typeof(T) == typeof(IContainerHealthMonitor))
        {
            return Task.FromResult(this as T);
        }
        
        return Task.FromResult<T?>(null);
    }

    #endregion

    #region IContainerHealthMonitor Implementation

    public async Task<HealthCheckResult> CheckHealthAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        _logger.LogDebug("Performing health check for service {ServiceName}", serviceName);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Get health check configuration
            if (!_monitoringContexts.TryGetValue(serviceName, out var context))
            {
                return new HealthCheckResult
                {
                    ServiceName = serviceName,
                    Status = HealthStatus.Unknown,
                    Message = "Service not monitored",
                    ResponseTime = stopwatch.Elapsed
                };
            }

            var healthCheck = context.HealthCheck;
            var endpoint = $"http://{serviceName}:{healthCheck.Port}{healthCheck.Path}";

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(healthCheck.Timeout);

            var response = await _httpClient.GetAsync(endpoint, cts.Token);
            stopwatch.Stop();

            var isHealthy = response.IsSuccessStatusCode;
            var status = isHealthy ? HealthStatus.Healthy : HealthStatus.Unhealthy;
            
            var result = new HealthCheckResult
            {
                ServiceName = serviceName,
                Status = status,
                Message = isHealthy ? "Service is healthy" : $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}",
                ResponseTime = stopwatch.Elapsed,
                Details = new Dictionary<string, object>
                {
                    { "StatusCode", (int)response.StatusCode },
                    { "Endpoint", endpoint },
                    { "ResponseTimeMs", stopwatch.ElapsedMilliseconds }
                }
            };

            // Update health status tracking
            UpdateServiceHealthStatus(serviceName, result);

            _logger.LogDebug("Health check for service {ServiceName} completed: {Status} ({ResponseTime}ms)",
                serviceName, status, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            return new HealthCheckResult
            {
                ServiceName = serviceName,
                Status = HealthStatus.Unknown,
                Message = "Health check cancelled",
                ResponseTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Health check failed for service {ServiceName}", serviceName);

            var result = new HealthCheckResult
            {
                ServiceName = serviceName,
                Status = HealthStatus.Unhealthy,
                Message = ex.Message,
                ResponseTime = stopwatch.Elapsed,
                Details = new Dictionary<string, object>
                {
                    { "ExceptionType", ex.GetType().Name },
                    { "ResponseTimeMs", stopwatch.ElapsedMilliseconds }
                }
            };

            // Update health status tracking
            UpdateServiceHealthStatus(serviceName, result);

            return result;
        }
    }

    public Task StartMonitoringAsync(string serviceName, HealthCheckConfiguration healthCheck, CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        _logger.LogInformation("Starting health monitoring for service {ServiceName}", serviceName);

        var context = new HealthMonitoringContext
        {
            ServiceName = serviceName,
            HealthCheck = healthCheck,
            StartedAt = DateTime.UtcNow,
            NextCheckAt = DateTime.UtcNow.Add(healthCheck.InitialDelay)
        };

        _monitoringContexts.AddOrUpdate(serviceName, context, (key, existing) => context);

        // Initialize health status
        _healthStatuses[serviceName] = new ServiceHealthStatus
        {
            ServiceName = serviceName,
            Status = HealthStatus.Unknown,
            TotalChecks = 0,
            ConsecutiveFailures = 0
        };

        _logger.LogInformation("Health monitoring started for service {ServiceName} with {Interval}s interval",
            serviceName, healthCheck.Interval.TotalSeconds);

        return Task.CompletedTask;
    }

    public Task StopMonitoringAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        _logger.LogInformation("Stopping health monitoring for service {ServiceName}", serviceName);

        _monitoringContexts.TryRemove(serviceName, out _);
        _healthStatuses.TryRemove(serviceName, out _);

        _logger.LogInformation("Health monitoring stopped for service {ServiceName}", serviceName);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<ServiceHealthStatus>> GetHealthStatusesAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        return Task.FromResult<IEnumerable<ServiceHealthStatus>>(_healthStatuses.Values.ToList());
    }

    #endregion

    #region Private Methods

    private void PerformPeriodicHealthChecks(object? state)
    {
        if (!_isRunning || _disposed)
            return;

        try
        {
            var now = DateTime.UtcNow;
            var servicesToCheck = _monitoringContexts.Values
                .Where(context => now >= context.NextCheckAt)
                .ToList();

            if (!servicesToCheck.Any())
                return;

            _logger.LogDebug("Performing periodic health checks for {ServiceCount} services", servicesToCheck.Count);

            // Run health checks in parallel
            var healthCheckTasks = servicesToCheck.Select(async context =>
            {
                try
                {
                    var result = await CheckHealthAsync(context.ServiceName, CancellationToken.None);
                    
                    // Update next check time
                    context.NextCheckAt = now.Add(context.HealthCheck.Interval);
                    context.LastCheckAt = now;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during periodic health check for service {ServiceName}", context.ServiceName);
                }
            });

            // Fire and forget - don't await to avoid blocking the timer
            _ = Task.WhenAll(healthCheckTasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during periodic health checks");
        }
    }

    private void UpdateServiceHealthStatus(string serviceName, HealthCheckResult result)
    {
        var healthStatus = _healthStatuses.GetOrAdd(serviceName, _ => new ServiceHealthStatus
        {
            ServiceName = serviceName,
            Status = HealthStatus.Unknown
        });

        var previousStatus = healthStatus.Status;
        healthStatus.TotalChecks++;

        switch (result.Status)
        {
            case HealthStatus.Healthy:
                healthStatus.Status = HealthStatus.Healthy;
                healthStatus.LastHealthyAt = result.CheckedAt;
                healthStatus.ConsecutiveFailures = 0;
                break;

            case HealthStatus.Unhealthy:
                healthStatus.ConsecutiveFailures++;
                healthStatus.LastUnhealthyAt = result.CheckedAt;

                // Only mark as unhealthy after consecutive failures exceed threshold
                if (_monitoringContexts.TryGetValue(serviceName, out var context))
                {
                    if (healthStatus.ConsecutiveFailures >= context.HealthCheck.FailureThreshold)
                    {
                        healthStatus.Status = HealthStatus.Unhealthy;
                    }
                }
                else
                {
                    healthStatus.Status = HealthStatus.Unhealthy;
                }
                break;

            case HealthStatus.Degraded:
                healthStatus.Status = HealthStatus.Degraded;
                break;

            default:
                healthStatus.Status = result.Status;
                break;
        }

        // Add result details to status
        healthStatus.Details["LastCheckResult"] = result;
        healthStatus.Details["LastCheckAt"] = result.CheckedAt;
        healthStatus.Details["LastResponseTime"] = result.ResponseTime.TotalMilliseconds;

        // Fire status change event if status changed
        if (previousStatus != healthStatus.Status)
        {
            _logger.LogInformation("Health status changed for service {ServiceName}: {PreviousStatus} -> {NewStatus}",
                serviceName, previousStatus, healthStatus.Status);

            var eventArgs = new HealthStatusChangedEventArgs
            {
                ServiceName = serviceName,
                PreviousStatus = previousStatus,
                NewStatus = healthStatus.Status,
                HealthCheckResult = result
            };

            try
            {
                _healthStatusChangedSubject.OnNext(eventArgs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error firing health status changed event for service {ServiceName}", serviceName);
            }
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ContainerHealthMonitor));
    }

    private void ThrowIfNotRunning()
    {
        ThrowIfDisposed();
        if (!_isRunning) throw new InvalidOperationException("ContainerHealthMonitor is not running");
    }

    #endregion

    #region Private Types

    private class HealthMonitoringContext
    {
        public string ServiceName { get; set; } = string.Empty;
        public HealthCheckConfiguration HealthCheck { get; set; } = new();
        public DateTime StartedAt { get; set; }
        public DateTime NextCheckAt { get; set; }
        public DateTime? LastCheckAt { get; set; }
    }

    #endregion
}