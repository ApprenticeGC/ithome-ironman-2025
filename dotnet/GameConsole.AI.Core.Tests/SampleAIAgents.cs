using GameConsole.AI.Core;
using GameConsole.Plugins.Core;
using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Core.Tests;

/// <summary>
/// Sample PathFinding AI agent for testing purposes.
/// </summary>
[AIAgent("pathfinder-basic", "Basic PathFinder", "1.0.0", "A basic pathfinding AI agent", "Test Author", 
    AIAgentCapability.PathFinding, BehaviorType = "Navigation", Priority = 10)]
public class SamplePathFindingAgent : IAIAgent
{
    private readonly ILogger<SamplePathFindingAgent> _logger;
    private AIAgentState _state = AIAgentState.Uninitialized;
    private bool _isRunning = false;

    public SamplePathFindingAgent(ILogger<SamplePathFindingAgent> logger)
    {
        _logger = logger;
    }

    public IPluginMetadata Metadata { get; } = null!; // Not needed for test
    public IPluginContext? Context { get; set; }
    public AIAgentCapability Capabilities => AIAgentCapability.PathFinding;
    public AIAgentState State => _state;
    public bool IsRunning => _isRunning;

    public Task ConfigureAsync(IPluginContext context, CancellationToken cancellationToken = default)
    {
        Context = context;
        return Task.CompletedTask;
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _state = AIAgentState.Ready;
        _logger.LogInformation("PathFinding agent initialized");
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = true;
        _logger.LogInformation("PathFinding agent started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = false;
        _logger.LogInformation("PathFinding agent stopped");
        return Task.CompletedTask;
    }

    public Task ExecuteAsync(float deltaTime, IAIExecutionContext context, CancellationToken cancellationToken = default)
    {
        _state = AIAgentState.Executing;
        // Simulate pathfinding logic
        _state = AIAgentState.Ready;
        return Task.CompletedTask;
    }

    public bool CanExecute(IAIExecutionContext context) => _state == AIAgentState.Ready;

    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _state = AIAgentState.Ready;
        return Task.CompletedTask;
    }

    public Task<bool> CanUnloadAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

    public Task PrepareUnloadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

/// <summary>
/// Sample Decision Making AI agent for testing purposes.
/// </summary>
[AIAgent("decision-basic", "Basic Decision Maker", "1.0.0", "A basic decision making AI agent", "Test Author", 
    AIAgentCapability.DecisionMaking | AIAgentCapability.Combat, BehaviorType = "BehaviorTree", Priority = 5)]
public class SampleDecisionMakingAgent : IAIAgent
{
    private AIAgentState _state = AIAgentState.Uninitialized;
    private bool _isRunning = false;

    public IPluginMetadata Metadata { get; } = null!; // Not needed for test
    public IPluginContext? Context { get; set; }
    public AIAgentCapability Capabilities => AIAgentCapability.DecisionMaking | AIAgentCapability.Combat;
    public AIAgentState State => _state;
    public bool IsRunning => _isRunning;

    public Task ConfigureAsync(IPluginContext context, CancellationToken cancellationToken = default)
    {
        Context = context;
        return Task.CompletedTask;
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _state = AIAgentState.Ready;
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = true;
        return Task.CompletedTask;
    }
    
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = false;
        return Task.CompletedTask;
    }

    public Task ExecuteAsync(float deltaTime, IAIExecutionContext context, CancellationToken cancellationToken = default)
    {
        _state = AIAgentState.Executing;
        // Simulate decision making logic
        _state = AIAgentState.Ready;
        return Task.CompletedTask;
    }

    public bool CanExecute(IAIExecutionContext context) => _state == AIAgentState.Ready;
    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _state = AIAgentState.Ready;
        return Task.CompletedTask;
    }

    public Task<bool> CanUnloadAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task PrepareUnloadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

/// <summary>
/// Invalid AI agent for testing validation (missing attribute).
/// </summary>
public class InvalidAgentMissingAttribute : IAIAgent
{
    public IPluginMetadata Metadata { get; } = null!;
    public IPluginContext? Context { get; set; }
    public AIAgentCapability Capabilities => AIAgentCapability.None;
    public AIAgentState State => AIAgentState.Uninitialized;
    public bool IsRunning => false;

    public Task ConfigureAsync(IPluginContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task ExecuteAsync(float deltaTime, IAIExecutionContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public bool CanExecute(IAIExecutionContext context) => true;
    public Task ResetAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<bool> CanUnloadAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task PrepareUnloadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

/// <summary>
/// Invalid AI agent for testing validation (abstract class).
/// </summary>
[AIAgent("abstract-invalid", "Abstract Invalid", "1.0.0", "Invalid abstract agent", "Test Author")]
public abstract class InvalidAbstractAgent : IAIAgent
{
    public IPluginMetadata Metadata { get; } = null!;
    public IPluginContext? Context { get; set; }
    public AIAgentCapability Capabilities => AIAgentCapability.None;
    public AIAgentState State => AIAgentState.Uninitialized;
    public abstract bool IsRunning { get; }

    public abstract Task ConfigureAsync(IPluginContext context, CancellationToken cancellationToken = default);
    public abstract Task InitializeAsync(CancellationToken cancellationToken = default);
    public abstract Task StartAsync(CancellationToken cancellationToken = default);
    public abstract Task StopAsync(CancellationToken cancellationToken = default);
    public abstract Task ExecuteAsync(float deltaTime, IAIExecutionContext context, CancellationToken cancellationToken = default);
    public abstract bool CanExecute(IAIExecutionContext context);
    public abstract Task ResetAsync(CancellationToken cancellationToken = default);
    public abstract Task<bool> CanUnloadAsync(CancellationToken cancellationToken = default);
    public abstract Task PrepareUnloadAsync(CancellationToken cancellationToken = default);
    public abstract ValueTask DisposeAsync();
}