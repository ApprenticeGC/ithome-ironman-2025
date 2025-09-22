using Akka.Actor;
using GameConsole.AI.Actors.Core.Messages;
using GameConsole.AI.Actors.Core.Services;
using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Actors.Core.Actors;

/// <summary>
/// Base AI Agent actor that handles processing requests for a specific AI capability.
/// This actor represents a single AI agent instance within the cluster.
/// </summary>
public class AIAgentActor : ReceiveActor
{
    private readonly ILogger _logger;
    private readonly AgentConfig _config;
    private readonly Dictionary<string, DateTime> _activeRequests = new();
    private DateTime _lastActivity = DateTime.UtcNow;

    public AIAgentActor(ILogger logger, AgentConfig config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));

        SetupMessageHandlers();
        
        _logger.LogInformation("AIAgentActor {AgentId} of type {AgentType} started", 
            _config.AgentId, _config.AgentType);
    }

    private void SetupMessageHandlers()
    {
        Receive<ProcessRequest>(HandleProcessRequest);
        Receive<BackendHealthCheck>(HandleHealthCheck);
    }

    private void HandleProcessRequest(ProcessRequest message)
    {
        try
        {
            var startTime = DateTime.UtcNow;
            _lastActivity = startTime;
            
            _logger.LogDebug("Processing request {RequestId} on agent {AgentId}", 
                message.RequestId, _config.AgentId);

            // Check if we're at capacity
            if (_activeRequests.Count >= _config.MaxConcurrentRequests)
            {
                _logger.LogWarning("Agent {AgentId} is at capacity ({ActiveRequests}/{MaxRequests})", 
                    _config.AgentId, _activeRequests.Count, _config.MaxConcurrentRequests);
                    
                var overloadResponse = new ProcessFailed(
                    message.RequestId, 
                    new InvalidOperationException("Agent at capacity"), 
                    _config.AgentId);
                    
                Sender.Tell(overloadResponse);
                return;
            }

            // Track the active request
            _activeRequests[message.RequestId] = startTime;

            // Simulate processing work (in real implementation, this would call AI backend)
            var processingTask = SimulateProcessing(message);
            
            processingTask.ContinueWith(task =>
            {
                _activeRequests.Remove(message.RequestId);
                
                if (task.IsFaulted)
                {
                    _logger.LogError(task.Exception, "Processing failed for request {RequestId}", message.RequestId);
                    
                    var failedResponse = new ProcessFailed(
                        message.RequestId, 
                        task.Exception?.GetBaseException() ?? new Exception("Unknown error"), 
                        _config.AgentId);
                        
                    message.Sender.Tell(failedResponse);
                }
                else
                {
                    var processingTime = DateTime.UtcNow - startTime;
                    var response = new ProcessResponse(message.RequestId, task.Result, processingTime);
                    message.Sender.Tell(response);
                    
                    _logger.LogDebug("Completed request {RequestId} in {ProcessingTime}ms", 
                        message.RequestId, processingTime.TotalMilliseconds);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle process request {RequestId}", message.RequestId);
            
            _activeRequests.Remove(message.RequestId);
            var failedResponse = new ProcessFailed(message.RequestId, ex, _config.AgentId);
            Sender.Tell(failedResponse);
        }
    }

    private void HandleHealthCheck(BackendHealthCheck message)
    {
        try
        {
            var startTime = DateTime.UtcNow;
            
            // Simulate health check (in real implementation, this would check AI backend)
            var isHealthy = true;
            var responseTime = DateTime.UtcNow - startTime;
            
            var response = new BackendHealthResponse(
                _config.Backend.Name,
                isHealthy,
                responseTime,
                isHealthy ? null : "Backend unavailable");
                
            Sender.Tell(response);
            
            _logger.LogDebug("Health check for {BackendName}: {Status} ({ResponseTime}ms)", 
                _config.Backend.Name, isHealthy ? "Healthy" : "Unhealthy", responseTime.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for backend {BackendName}", _config.Backend.Name);
            
            var response = new BackendHealthResponse(
                _config.Backend.Name,
                false,
                TimeSpan.Zero,
                ex.Message);
                
            Sender.Tell(response);
        }
    }

    private async Task<object> SimulateProcessing(ProcessRequest request)
    {
        // Simulate processing delay
        await Task.Delay(TimeSpan.FromMilliseconds(100 + new Random().Next(900)));
        
        // Return a mock response based on agent type
        return _config.AgentType switch
        {
            "dialogue" => new { Response = "AI dialogue response", Type = "dialogue" },
            "analysis" => new { Analysis = "AI analysis result", Confidence = 0.95 },
            "codegen" => new { Code = "// Generated code", Language = "C#" },
            _ => new { Result = "Generic AI response", AgentType = _config.AgentType }
        };
    }

    public AgentInfo GetAgentInfo()
    {
        return new AgentInfo(
            _config.AgentId,
            _config.AgentType,
            Self.Path.Address.ToString(),
            "Active",
            _lastActivity,
            _activeRequests.Count);
    }

    protected override void PostStop()
    {
        _logger.LogInformation("AIAgentActor {AgentId} stopped", _config.AgentId);
        base.PostStop();
    }

    public static Props Props(ILogger logger, AgentConfig config)
    {
        return Akka.Actor.Props.Create(() => new AIAgentActor(logger, config));
    }
}