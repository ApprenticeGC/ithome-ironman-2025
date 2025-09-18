using Akka.Actor;
using Akka.Cluster;
using Akka.Event;
using GameConsole.AI.Actors.Configuration;
using GameConsole.AI.Actors.Messages;
using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Actors;

/// <summary>
/// AI Actor System service that manages the Akka.NET actor system for AI orchestration
/// </summary>
public class AIActorSystem : IService
{
    private readonly ILogger<AIActorSystem> _logger;
    private readonly ActorSystemConfiguration _configuration;
    private ActorSystem? _actorSystem;
    private Cluster? _cluster;
    private IActorRef? _systemManager;
    private readonly Dictionary<string, IActorRef> _agents;
    private DateTime _startTime;

    /// <summary>
    /// Gets a value indicating whether the service is currently running
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Gets the underlying Akka.NET actor system
    /// </summary>
    public ActorSystem? ActorSystem => _actorSystem;

    /// <summary>
    /// Gets the cluster instance if clustering is enabled
    /// </summary>
    public Cluster? Cluster => _cluster;

    /// <summary>
    /// Initializes a new instance of the AIActorSystem
    /// </summary>
    /// <param name="configuration">Actor system configuration</param>
    /// <param name="logger">Logger instance</param>
    public AIActorSystem(ActorSystemConfiguration configuration, ILogger<AIActorSystem> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _agents = new Dictionary<string, IActorRef>();
    }

    /// <summary>
    /// Initializes the AI Actor System asynchronously
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>A task representing the async initialization operation</returns>
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_actorSystem != null)
        {
            _logger.LogWarning("AI Actor System is already initialized");
            return Task.CompletedTask;
        }

        try
        {
            _logger.LogInformation("Initializing AI Actor System with name: {SystemName}", _configuration.SystemName);

            // Build Akka configuration
            var config = _configuration.BuildConfiguration();
            
            // Create the actor system
            _actorSystem = ActorSystem.Create(_configuration.SystemName, config);
            
            // Initialize cluster if enabled
            if (_configuration.Cluster.SeedNodes.Any())
            {
                _cluster = Cluster.Get(_actorSystem);
                _logger.LogInformation("Cluster support enabled with seed nodes: {SeedNodes}", 
                    string.Join(", ", _configuration.Cluster.SeedNodes));
            }

            // Create system manager actor
            _systemManager = _actorSystem.ActorOf(
                Props.Create<AISystemManagerActor>(),
                "system-manager");

            _logger.LogInformation("AI Actor System initialized successfully");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize AI Actor System");
            throw;
        }
    }

    /// <summary>
    /// Starts the AI Actor System asynchronously
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>A task representing the async start operation</returns>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_actorSystem == null)
        {
            throw new InvalidOperationException("AI Actor System must be initialized before starting");
        }

        if (IsRunning)
        {
            _logger.LogWarning("AI Actor System is already running");
            return;
        }

        try
        {
            _logger.LogInformation("Starting AI Actor System");
            _startTime = DateTime.UtcNow;

            // Join cluster if clustering is enabled
            if (_cluster != null && _configuration.Cluster.SeedNodes.Any())
            {
                await JoinClusterAsync(cancellationToken);
            }

            // Send start system message to system manager
            if (_systemManager != null)
            {
                _systemManager.Tell(new StartSystem());
            }

            IsRunning = true;
            _logger.LogInformation("AI Actor System started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start AI Actor System");
            throw;
        }
    }

    /// <summary>
    /// Stops the AI Actor System asynchronously
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>A task representing the async stop operation</returns>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning || _actorSystem == null)
        {
            _logger.LogWarning("AI Actor System is not running");
            return;
        }

        try
        {
            _logger.LogInformation("Stopping AI Actor System");

            // Send stop system message to system manager
            if (_systemManager != null)
            {
                _systemManager.Tell(new StopSystem());
            }

            // Stop all managed agents
            await StopAllAgentsAsync(cancellationToken);

            // Leave cluster if clustering is enabled
            if (_cluster != null)
            {
                await LeaveClusterAsync(cancellationToken);
            }

            IsRunning = false;
            _logger.LogInformation("AI Actor System stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while stopping AI Actor System");
            throw;
        }
    }

    /// <summary>
    /// Disposes the AI Actor System asynchronously
    /// </summary>
    /// <returns>A task representing the async dispose operation</returns>
    public async ValueTask DisposeAsync()
    {
        if (!IsRunning)
        {
            await StopAsync();
        }

        if (_actorSystem != null)
        {
            try
            {
                _logger.LogInformation("Terminating AI Actor System");
                await _actorSystem.Terminate();
                _logger.LogInformation("AI Actor System terminated");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while terminating AI Actor System");
            }
            finally
            {
                _actorSystem = null;
                _cluster = null;
                _systemManager = null;
                _agents.Clear();
            }
        }
    }

    /// <summary>
    /// Creates an AI agent of the specified type
    /// </summary>
    /// <param name="agentId">Unique identifier for the agent</param>
    /// <param name="agentType">Type of agent to create</param>
    /// <param name="configuration">Agent-specific configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if agent was created successfully</returns>
    public async Task<bool> CreateAgentAsync(string agentId, string agentType, object? configuration = null, CancellationToken cancellationToken = default)
    {
        if (_actorSystem == null || !IsRunning)
        {
            throw new InvalidOperationException("AI Actor System is not running");
        }

        if (_agents.ContainsKey(agentId))
        {
            _logger.LogWarning("Agent {AgentId} already exists", agentId);
            return false;
        }

        try
        {
            _logger.LogInformation("Creating agent {AgentId} of type {AgentType}", agentId, agentType);

            var createMessage = new CreateAgent(agentId, agentType, configuration);
            
            if (_systemManager != null)
            {
                var response = await _systemManager.Ask<bool>(createMessage, TimeSpan.FromSeconds(30), cancellationToken);
                
                if (response)
                {
                    // Note: In a real implementation, we would get the agent reference from the system manager
                    // For now, we'll track it as created
                    _logger.LogInformation("Agent {AgentId} created successfully", agentId);
                    return true;
                }
            }

            _logger.LogWarning("Failed to create agent {AgentId}", agentId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating agent {AgentId}", agentId);
            return false;
        }
    }

    /// <summary>
    /// Stops an AI agent
    /// </summary>
    /// <param name="agentId">Unique identifier for the agent</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if agent was stopped successfully</returns>
    public async Task<bool> StopAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        if (_actorSystem == null || !IsRunning)
        {
            throw new InvalidOperationException("AI Actor System is not running");
        }

        try
        {
            _logger.LogInformation("Stopping agent {AgentId}", agentId);

            var stopMessage = new StopAgent(agentId);
            
            if (_systemManager != null)
            {
                var response = await _systemManager.Ask<bool>(stopMessage, TimeSpan.FromSeconds(30), cancellationToken);
                
                if (response)
                {
                    _agents.Remove(agentId);
                    _logger.LogInformation("Agent {AgentId} stopped successfully", agentId);
                    return true;
                }
            }

            _logger.LogWarning("Failed to stop agent {AgentId}", agentId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping agent {AgentId}", agentId);
            return false;
        }
    }

    /// <summary>
    /// Gets the current system health status
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>System health information</returns>
    public async Task<SystemHealth?> GetSystemHealthAsync(CancellationToken cancellationToken = default)
    {
        if (_actorSystem == null || !IsRunning)
        {
            return new SystemHealth(false, 0, 0, TimeSpan.Zero);
        }

        try
        {
            if (_systemManager != null)
            {
                var response = await _systemManager.Ask<SystemHealth>(
                    new GetSystemHealth(), 
                    TimeSpan.FromSeconds(10), 
                    cancellationToken);
                
                return response;
            }

            // Fallback health status
            var uptime = DateTime.UtcNow - _startTime;
            return new SystemHealth(true, _agents.Count, 0, uptime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system health");
            return new SystemHealth(false, 0, 0, TimeSpan.Zero);
        }
    }

    /// <summary>
    /// Joins the cluster asynchronously
    /// </summary>
    private async Task JoinClusterAsync(CancellationToken cancellationToken)
    {
        if (_cluster == null) return;

        _logger.LogInformation("Joining cluster");
        
        // Register for cluster events
        _cluster.Subscribe(_actorSystem!.DeadLetters, typeof(ClusterEvent.IMemberEvent));
        
        // Join the cluster with the first seed node
        var firstSeedNode = _configuration.Cluster.SeedNodes.FirstOrDefault();
        if (!string.IsNullOrEmpty(firstSeedNode))
        {
            var address = Address.Parse(firstSeedNode);
            _cluster.Join(address);
            _logger.LogInformation("Attempting to join cluster via seed node: {SeedNode}", firstSeedNode);
        }

        // Wait for cluster formation (simplified)
        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
    }

    /// <summary>
    /// Leaves the cluster asynchronously
    /// </summary>
    private async Task LeaveClusterAsync(CancellationToken cancellationToken)
    {
        if (_cluster == null) return;

        _logger.LogInformation("Leaving cluster");
        _cluster.Leave(_cluster.SelfAddress);
        
        // Wait for graceful leave
        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }

    /// <summary>
    /// Stops all managed agents
    /// </summary>
    private async Task StopAllAgentsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping all managed agents");
        
        var stopTasks = _agents.Keys.Select(agentId => StopAgentAsync(agentId, cancellationToken));
        await Task.WhenAll(stopTasks);
        
        _agents.Clear();
    }

    /// <summary>
    /// Placeholder for getting Self reference in cluster operations
    /// In a real implementation, this would be properly implemented
    /// </summary>
    private IActorRef Self => _systemManager ?? ActorRefs.Nobody;
}

/// <summary>
/// System manager actor that handles system-level operations
/// </summary>
internal class AISystemManagerActor : ReceiveActor
{
    private readonly Dictionary<string, IActorRef> _agents = new();
    private DateTime _startTime = DateTime.UtcNow;
    private readonly Dictionary<string, object> _conversations = new();
    private readonly ILoggingAdapter _logger;

    public AISystemManagerActor()
    {
        _logger = Context.GetLogger();
        
        Receive<StartSystem>(HandleStartSystem);
        Receive<StopSystem>(HandleStopSystem);
        Receive<GetSystemHealth>(HandleGetSystemHealth);
        Receive<CreateAgent>(HandleCreateAgent);
        Receive<StopAgent>(HandleStopAgent);
    }

    private void HandleStartSystem(StartSystem message)
    {
        _logger.Info("AI System Manager started");
        _startTime = DateTime.UtcNow;
        Sender.Tell(true);
    }

    private void HandleStopSystem(StopSystem message)
    {
        _logger.Info("AI System Manager stopping");
        
        // Stop all agents
        foreach (var agent in _agents.Values)
        {
            agent.Tell(PoisonPill.Instance);
        }
        _agents.Clear();
        
        Sender.Tell(true);
    }

    private void HandleGetSystemHealth(GetSystemHealth message)
    {
        var uptime = DateTime.UtcNow - _startTime;
        var health = new SystemHealth(true, _agents.Count, _conversations.Count, uptime);
        Sender.Tell(health);
    }

    private void HandleCreateAgent(CreateAgent message)
    {
        try
        {
            if (_agents.ContainsKey(message.AgentId))
            {
                Sender.Tell(false);
                return;
            }

            // In a real implementation, we would create specific agent types based on message.AgentType
            // For now, we'll create a placeholder actor
            var agentRef = Context.ActorOf(
                Props.Create(() => new PlaceholderAIActor(message.AgentId, message.Configuration)),
                $"agent-{message.AgentId}");

            _agents[message.AgentId] = agentRef;
            Sender.Tell(true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create agent {AgentId}", message.AgentId);
            Sender.Tell(false);
        }
    }

    private void HandleStopAgent(StopAgent message)
    {
        if (_agents.TryGetValue(message.AgentId, out var agentRef))
        {
            agentRef.Tell(PoisonPill.Instance);
            _agents.Remove(message.AgentId);
            Sender.Tell(true);
        }
        else
        {
            Sender.Tell(false);
        }
    }
}

/// <summary>
/// Placeholder AI actor for demonstration purposes
/// </summary>
internal class PlaceholderAIActor : Actors.BaseAIActor
{
    public PlaceholderAIActor(string agentId, object? configuration = null) 
        : base(agentId, configuration)
    {
    }

    protected override void SetupSpecificMessageHandlers()
    {
        // Placeholder implementation
        Receive<InvokeAgent>(msg =>
        {
            var response = new AgentResponse(AgentId, $"Placeholder response for: {msg.Input}", msg.ConversationId);
            Sender.Tell(response);
        });
    }
}