using Akka.Actor;
using Akka.Cluster;
using GameConsole.AI.Actors.Core.Services;
using GameConsole.AI.Actors.Core.Messages;
using GameConsole.AI.Actors.Core.Configuration;
using GameConsole.AI.Actors.Core.Actors;
using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace GameConsole.AI.Actors.Core.Services;

/// <summary>
/// Implementation of the AI Actor Cluster Service that manages distributed AI agents.
/// </summary>
public class AIActorClusterService : IAIActorClusterService
{
    private readonly ILogger<AIActorClusterService> _logger;
    private readonly AIActorClusterConfig _config;
    private ActorSystem? _actorSystem;
    private IActorRef? _agentDirector;
    private bool _isRunning;

    public AIActorClusterService(
        ILogger<AIActorClusterService> logger, 
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Bind configuration - simplified approach for minimal changes
        _config = new AIActorClusterConfig();
        var section = configuration.GetSection("AIActorCluster");
        if (section.Exists())
        {
            // Manual binding to avoid dependency on Microsoft.Extensions.Configuration.Binder
            _config.SystemName = section["SystemName"] ?? _config.SystemName;
            _config.Hostname = section["Hostname"] ?? _config.Hostname;
            if (int.TryParse(section["Port"], out var port))
                _config.Port = port;
        }
    }

    public ActorSystem ActorSystem => _actorSystem ?? throw new InvalidOperationException("Service not started");

    public bool IsRunning => _isRunning;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing AI Actor Cluster Service");
        
        try
        {
            // Create Akka.NET configuration for clustering
            var akkaConfig = CreateAkkaConfig();
            
            // Create the actor system
            _actorSystem = ActorSystem.Create(_config.SystemName, akkaConfig);
            
            _logger.LogInformation("Actor system {SystemName} created", _config.SystemName);
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize AI Actor Cluster Service");
            return Task.FromException(ex);
        }
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_actorSystem == null)
            throw new InvalidOperationException("Service not initialized");
            
        _logger.LogInformation("Starting AI Actor Cluster Service");
        
        try
        {
            // Start the cluster
            var cluster = Cluster.Get(_actorSystem);
            
            // Create a simple logger for the director actor (avoiding the CreateLogger issue)
            var directorLogger = new LoggerFactory().CreateLogger<AgentDirectorActor>();
            
            // Create the Agent Director actor
            _agentDirector = _actorSystem.ActorOf(
                AgentDirectorActor.Props(directorLogger, _config), 
                "agent-director");
            
            _isRunning = true;
            
            _logger.LogInformation("AI Actor Cluster Service started successfully");
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start AI Actor Cluster Service");
            return Task.FromException(ex);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping AI Actor Cluster Service");
        
        try
        {
            if (_actorSystem != null)
            {
                await _actorSystem.Terminate();
                _actorSystem = null;
            }
            
            _agentDirector = null;
            _isRunning = false;
            
            _logger.LogInformation("AI Actor Cluster Service stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop AI Actor Cluster Service");
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_isRunning)
        {
            await StopAsync();
        }
    }

    public async Task<ClusterStateResponse> GetClusterStateAsync(CancellationToken cancellationToken = default)
    {
        EnsureStarted();
        
        var response = await _agentDirector!.Ask<ClusterStateResponse>(new GetClusterState(), _config.ClusterTimeout);
        return response;
    }

    public async Task<AgentStarted> StartAgentAsync(string agentType, AgentConfig config, CancellationToken cancellationToken = default)
    {
        EnsureStarted();
        
        var message = new StartAgent(agentType, config);
        var response = await _agentDirector!.Ask<AIMessage>(message, _config.ClusterTimeout);
        
        return response switch
        {
            AgentStarted started => started,
            ProcessFailed failed => throw new InvalidOperationException($"Failed to start agent: {failed.Exception.Message}", failed.Exception),
            _ => throw new InvalidOperationException($"Unexpected response type: {response.GetType()}")
        };
    }

    public async Task<AgentStopped> StopAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        EnsureStarted();
        
        var message = new StopAgent(agentId);
        var response = await _agentDirector!.Ask<AIMessage>(message, _config.ClusterTimeout);
        
        return response switch
        {
            AgentStopped stopped => stopped,
            ProcessFailed failed => throw new InvalidOperationException($"Failed to stop agent: {failed.Exception.Message}", failed.Exception),
            _ => throw new InvalidOperationException($"Unexpected response type: {response.GetType()}")
        };
    }

    public async Task<ProcessResponse> ProcessRequestAsync(ProcessRequest request, CancellationToken cancellationToken = default)
    {
        EnsureStarted();
        
        var response = await _agentDirector!.Ask<AIMessage>(request, _config.ClusterTimeout);
        
        return response switch
        {
            ProcessResponse processResponse => processResponse,
            ProcessFailed failed => throw new InvalidOperationException($"Processing failed: {failed.Exception.Message}", failed.Exception),
            _ => throw new InvalidOperationException($"Unexpected response type: {response.GetType()}")
        };
    }

    public async Task<RebalanceCompleted> RebalanceAgentsAsync(CancellationToken cancellationToken = default)
    {
        EnsureStarted();
        
        var response = await _agentDirector!.Ask<RebalanceCompleted>(new RebalanceAgents(), _config.ClusterTimeout);
        return response;
    }

    public Task<IEnumerable<BackendHealthResponse>> GetBackendHealthAsync(CancellationToken cancellationToken = default)
    {
        EnsureStarted();
        
        // For now, return empty list. In a full implementation, this would check all registered backends.
        return Task.FromResult(Enumerable.Empty<BackendHealthResponse>());
    }

    public Task<IEnumerable<AgentInfo>> GetActiveAgentsAsync(CancellationToken cancellationToken = default)
    {
        EnsureStarted();
        
        // For now, return empty list. In a full implementation, this would query all active agents.
        return Task.FromResult(Enumerable.Empty<AgentInfo>());
    }

    public Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new[]
        {
            typeof(IAIActorClusterService),
            typeof(IService),
            typeof(ICapabilityProvider)
        }.AsEnumerable());
    }

    public Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        var capabilities = new[]
        {
            typeof(IAIActorClusterService),
            typeof(IService),
            typeof(ICapabilityProvider)
        };
        
        return Task.FromResult(capabilities.Contains(typeof(T)));
    }

    public Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (typeof(T) == typeof(IAIActorClusterService))
            return Task.FromResult(this as T);
            
        return Task.FromResult<T?>(null);
    }

    private void EnsureStarted()
    {
        if (!_isRunning || _agentDirector == null)
            throw new InvalidOperationException("Service not started");
    }

    private string CreateAkkaConfig()
    {
        var seedNodes = _config.SeedNodes.Count > 0 
            ? string.Join(",", _config.SeedNodes.Select(node => $"\"{node}\""))
            : $"\"akka.tcp://{_config.SystemName}@{_config.Hostname}:{_config.Port}\"";

        return $@"
akka {{
  actor {{
    provider = cluster
  }}
  
  remote {{
    dot-netty.tcp {{
      hostname = ""{_config.Hostname}""
      port = {_config.Port}
    }}
  }}
  
  cluster {{
    seed-nodes = [{seedNodes}]
    roles = [{string.Join(",", _config.Roles.Select(r => $"\"{r}\""))}]
    min-nr-of-members = {_config.MinimumClusterSize}
    
    sharding {{
      number-of-shards = {_config.Sharding.ShardsPerAgentType}
      passivate-idle-entity-after = {_config.Sharding.PassivationTimeout.TotalSeconds}s
      rebalance-interval = {_config.Sharding.RebalanceInterval.TotalSeconds}s
    }}
    
    downing-provider-class = ""Akka.Cluster.SBR.SplitBrainResolverProvider, Akka.Cluster""
    split-brain-resolver {{
      active-strategy = keep-majority
    }}
  }}
  
  loggers = [""Akka.Logger.Extensions.Logging.LoggingLogger, Akka.Logger.Extensions.Logging""]
  logging-filter = ""Akka.Logger.Extensions.Logging.LoggingFilter, Akka.Logger.Extensions.Logging""
  
  stdout-loglevel = INFO
  loglevel = INFO
}}";
    }
}