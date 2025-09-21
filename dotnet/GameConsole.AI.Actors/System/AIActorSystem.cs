using System.Text;
using Akka.Actor;
using Akka.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using GameConsole.Core.Abstractions;
using GameConsole.AI.Actors.Configuration;
using GameConsole.AI.Actors.Actors;
using GameConsole.AI.Actors.Messages;

namespace GameConsole.AI.Actors.System;

/// <summary>
/// AI Actor System - manages the Akka.NET actor system for AI orchestration.
/// Implements IService for integration with GameConsole service infrastructure.
/// </summary>
public class AIActorSystem : IService, ICapabilityProvider
{
    private readonly ILogger<AIActorSystem> _logger;
    private readonly ActorSystemConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    
    private ActorSystem? _actorSystem;
    private IActorRef? _agentDirector;
    private IActorRef? _contextManager;
    
    private bool _isInitialized = false;
    private bool _isRunning = false;

    public bool IsRunning => _isRunning;

    public AIActorSystem(
        ILogger<AIActorSystem> logger,
        ActorSystemConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            _logger.LogWarning("AIActorSystem is already initialized");
            return;
        }

        try
        {
            _logger.LogInformation("Initializing AI Actor System: {SystemName}", _configuration.SystemName);

            // Build Akka configuration
            var akkaConfig = BuildAkkaConfiguration();
            
            // Create actor system
            _actorSystem = ActorSystem.Create(_configuration.SystemName, akkaConfig);
            
            // Create supervisor actors
            await CreateSupervisorActors(cancellationToken);
            
            _isInitialized = true;
            _logger.LogInformation("AI Actor System initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize AI Actor System");
            throw;
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("AIActorSystem must be initialized before starting");
        }

        if (_isRunning)
        {
            _logger.LogWarning("AIActorSystem is already running");
            return;
        }

        try
        {
            _logger.LogInformation("Starting AI Actor System");
            
            // Wait briefly for actor system to be fully ready
            await Task.Delay(100, cancellationToken);
            
            _isRunning = true;
            
            _logger.LogInformation("AI Actor System started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start AI Actor System");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
        {
            _logger.LogWarning("AIActorSystem is not running");
            return;
        }

        try
        {
            _logger.LogInformation("Stopping AI Actor System");
            
            if (_actorSystem != null)
            {
                await _actorSystem.Terminate();
                await _actorSystem.WhenTerminated;
            }
            
            _isRunning = false;
            _logger.LogInformation("AI Actor System stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping AI Actor System");
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_isRunning)
        {
            await StopAsync();
        }
        
        _actorSystem?.Dispose();
    }

    /// <summary>
    /// Get the agent director actor reference.
    /// </summary>
    public IActorRef? GetAgentDirector() => _agentDirector;

    /// <summary>
    /// Get the context manager actor reference.
    /// </summary>
    public IActorRef? GetContextManager() => _contextManager;

    /// <summary>
    /// Get the underlying actor system.
    /// </summary>
    public ActorSystem? GetActorSystem() => _actorSystem;

    /// <summary>
    /// Register an AI agent with the system.
    /// </summary>
    public async Task<bool> RegisterAgentAsync(string agentId, Props agentProps, AgentMetadata metadata, CancellationToken cancellationToken = default)
    {
        if (!_isRunning || _agentDirector == null || _actorSystem == null)
        {
            throw new InvalidOperationException("AIActorSystem is not running");
        }

        try
        {
            // Create the agent actor
            var agentRef = _actorSystem.ActorOf(agentProps, agentId);
            
            // Register with director
            var registerMessage = new RegisterAgent(agentId, agentRef, metadata);
            var response = await _agentDirector.Ask<AgentRegistered>(registerMessage, TimeSpan.FromSeconds(10), cancellationToken);
            
            _logger.LogInformation("Registered agent {AgentId}: {Success}", agentId, response.Success);
            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register agent {AgentId}", agentId);
            return false;
        }
    }

    // ICapabilityProvider implementation
    public Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var capabilities = new Type[]
        {
            typeof(IActorRef), // Can provide actor references
            typeof(ActorSystem), // Can provide actor system
            typeof(AIActorSystem) // Can provide self
        };
        
        return Task.FromResult(capabilities.AsEnumerable());
    }

    public Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        var requestedType = typeof(T);
        var hasCapability = requestedType == typeof(IActorRef) ||
                           requestedType == typeof(ActorSystem) ||
                           requestedType == typeof(AIActorSystem);
        
        return Task.FromResult(hasCapability);
    }

    public Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        var requestedType = typeof(T);
        
        if (requestedType == typeof(ActorSystem))
        {
            return Task.FromResult(_actorSystem as T);
        }
        
        if (requestedType == typeof(AIActorSystem))
        {
            return Task.FromResult(this as T);
        }
        
        if (requestedType == typeof(IActorRef))
        {
            return Task.FromResult(_agentDirector as T);
        }
        
        return Task.FromResult<T?>(null);
    }

    private Config BuildAkkaConfiguration()
    {
        var configBuilder = new StringBuilder();
        
        // Basic actor system configuration
        configBuilder.AppendLine("akka {");
        configBuilder.AppendLine("  actor {");
        configBuilder.AppendLine("    provider = \"cluster\"");
        configBuilder.AppendLine("    default-dispatcher {");
        configBuilder.AppendLine($"      throughput = {_configuration.ActorSystem.DefaultThroughput}");
        configBuilder.AppendLine("      executor = \"thread-pool-executor\"");
        configBuilder.AppendLine("      thread-pool-executor {");
        configBuilder.AppendLine($"        fixed-pool-size = {_configuration.ActorSystem.MaxDispatcherThreads}");
        configBuilder.AppendLine("      }");
        configBuilder.AppendLine("    }");
        configBuilder.AppendLine($"    creation-timeout = {_configuration.ActorSystem.ActorCreationTimeoutMs}ms");
        configBuilder.AppendLine("  }");

        // Clustering configuration
        if (_configuration.Clustering.Enabled)
        {
            configBuilder.AppendLine("  cluster {");
            configBuilder.AppendLine($"    min-nr-of-members = {_configuration.Clustering.MinimumClusterSize}");
            configBuilder.AppendLine($"    roles = [{string.Join(", ", _configuration.Clustering.Roles.Select(r => $"\"{r}\""))}]");
            configBuilder.AppendLine($"    seed-nodes = [{string.Join(", ", _configuration.Clustering.SeedNodes.Select(s => $"\"{s}\""))}]");
            configBuilder.AppendLine("  }");
            configBuilder.AppendLine("  remote {");
            configBuilder.AppendLine("    dot-netty.tcp {");
            configBuilder.AppendLine($"      hostname = \"{_configuration.Clustering.Hostname}\"");
            configBuilder.AppendLine($"      port = {_configuration.Clustering.Port}");
            configBuilder.AppendLine("    }");
            configBuilder.AppendLine("  }");
        }
        else
        {
            configBuilder.AppendLine("  cluster {");
            configBuilder.AppendLine("    min-nr-of-members = 1");
            configBuilder.AppendLine("  }");
        }

        // Logging configuration
        configBuilder.AppendLine($"  loglevel = {_configuration.Logging.LogLevel}");
        configBuilder.AppendLine($"  log-dead-letters = {(_configuration.Logging.LogDeadLetters ? "on" : "off")}");

        configBuilder.AppendLine("}");

        var configString = configBuilder.ToString();
        _logger.LogDebug("Generated Akka configuration: {Config}", configString);
        
        return ConfigurationFactory.ParseString(configString);
    }

    private async Task CreateSupervisorActors(CancellationToken cancellationToken)
    {
        if (_actorSystem == null)
        {
            throw new InvalidOperationException("Actor system not initialized");
        }

        // Create agent director
        var agentDirectorProps = Props.Create(() => new AgentDirectorActor(
            _serviceProvider.GetRequiredService<ILogger<AgentDirectorActor>>()));
        _agentDirector = _actorSystem.ActorOf(agentDirectorProps, "agent-director");

        // Create context manager
        var contextManagerProps = Props.Create(() => new ContextManagerActor(
            _serviceProvider.GetRequiredService<ILogger<ContextManagerActor>>()));
        _contextManager = _actorSystem.ActorOf(contextManagerProps, "context-manager");

        // Wait briefly for actors to initialize
        await Task.Delay(50, cancellationToken);

        _logger.LogInformation("Created supervisor actors: agent-director, context-manager");
    }
}