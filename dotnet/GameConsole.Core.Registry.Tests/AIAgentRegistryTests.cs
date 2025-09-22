using GameConsole.Core.Abstractions;
using GameConsole.Core.Registry;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.Core.Registry.Tests;

/// <summary>
/// Tests for the AIAgentRegistry class.
/// </summary>
public class AIAgentRegistryTests : IAsyncDisposable
{
    private readonly AIAgentRegistry _registry;
    private readonly ILogger<AIAgentRegistry> _logger;

    public AIAgentRegistryTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Debug));
        _logger = loggerFactory.CreateLogger<AIAgentRegistry>();
        _registry = new AIAgentRegistry(_logger);
    }

    [Fact]
    public async Task InitializeAsync_Should_Complete_Without_Error()
    {
        // Act & Assert
        await _registry.InitializeAsync();
        Assert.False(_registry.IsRunning);
    }

    [Fact]
    public async Task StartAsync_Should_Set_IsRunning_True()
    {
        // Arrange
        await _registry.InitializeAsync();

        // Act
        await _registry.StartAsync();

        // Assert
        Assert.True(_registry.IsRunning);
    }

    [Fact]
    public async Task StopAsync_Should_Set_IsRunning_False()
    {
        // Arrange
        await _registry.InitializeAsync();
        await _registry.StartAsync();

        // Act
        await _registry.StopAsync();

        // Assert
        Assert.False(_registry.IsRunning);
    }

    [Fact]
    public async Task RegisterAsync_Should_Register_Agent_Successfully()
    {
        // Arrange
        await _registry.InitializeAsync();
        await _registry.StartAsync();

        var agent = new TestAIAgent("test-1", "Test Agent 1");

        // Act
        var result = await _registry.RegisterAsync(agent);

        // Assert
        Assert.True(result);
        Assert.True(await _registry.IsRegisteredAsync(agent.Id));
        Assert.Equal(1, await _registry.GetCountAsync());
    }

    [Fact]
    public async Task RegisterAsync_Should_Fail_For_Duplicate_Agent()
    {
        // Arrange
        await _registry.InitializeAsync();
        await _registry.StartAsync();

        var agent1 = new TestAIAgent("test-1", "Test Agent 1");
        var agent2 = new TestAIAgent("test-1", "Test Agent 2"); // Same ID

        // Act
        var result1 = await _registry.RegisterAsync(agent1);
        var result2 = await _registry.RegisterAsync(agent2);

        // Assert
        Assert.True(result1);
        Assert.False(result2);
        Assert.Equal(1, await _registry.GetCountAsync());
    }

    [Fact]
    public async Task UnregisterAsync_Should_Remove_Agent_Successfully()
    {
        // Arrange
        await _registry.InitializeAsync();
        await _registry.StartAsync();

        var agent = new TestAIAgent("test-1", "Test Agent 1");
        await _registry.RegisterAsync(agent);

        // Act
        var result = await _registry.UnregisterAsync(agent.Id);

        // Assert
        Assert.True(result);
        Assert.False(await _registry.IsRegisteredAsync(agent.Id));
        Assert.Equal(0, await _registry.GetCountAsync());
    }

    [Fact]
    public async Task UnregisterAsync_Should_Fail_For_Non_Existent_Agent()
    {
        // Arrange
        await _registry.InitializeAsync();
        await _registry.StartAsync();

        // Act
        var result = await _registry.UnregisterAsync("non-existent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Registered_Agent()
    {
        // Arrange
        await _registry.InitializeAsync();
        await _registry.StartAsync();

        var agent = new TestAIAgent("test-1", "Test Agent 1");
        await _registry.RegisterAsync(agent);

        // Act
        var retrieved = await _registry.GetByIdAsync(agent.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(agent.Id, retrieved.Id);
        Assert.Equal(agent.Name, retrieved.Name);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Null_For_Non_Existent_Agent()
    {
        // Arrange
        await _registry.InitializeAsync();
        await _registry.StartAsync();

        // Act
        var retrieved = await _registry.GetByIdAsync("non-existent");

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task GetAllAsync_Should_Return_All_Registered_Agents()
    {
        // Arrange
        await _registry.InitializeAsync();
        await _registry.StartAsync();

        var agent1 = new TestAIAgent("test-1", "Test Agent 1");
        var agent2 = new TestAIAgent("test-2", "Test Agent 2");
        
        await _registry.RegisterAsync(agent1);
        await _registry.RegisterAsync(agent2);

        // Act
        var allAgents = await _registry.GetAllAsync();

        // Assert
        Assert.Equal(2, allAgents.Count());
        Assert.Contains(allAgents, a => a.Id == agent1.Id);
        Assert.Contains(allAgents, a => a.Id == agent2.Id);
    }

    [Fact]
    public async Task ClearAsync_Should_Remove_All_Agents()
    {
        // Arrange
        await _registry.InitializeAsync();
        await _registry.StartAsync();

        var agent1 = new TestAIAgent("test-1", "Test Agent 1");
        var agent2 = new TestAIAgent("test-2", "Test Agent 2");
        
        await _registry.RegisterAsync(agent1);
        await _registry.RegisterAsync(agent2);

        // Act
        await _registry.ClearAsync();

        // Assert
        Assert.Equal(0, await _registry.GetCountAsync());
        var allAgents = await _registry.GetAllAsync();
        Assert.Empty(allAgents);
    }

    [Fact]
    public async Task RegisterAsync_Should_Trigger_AgentRegistered_Event()
    {
        // Arrange
        await _registry.InitializeAsync();
        await _registry.StartAsync();

        var agent = new TestAIAgent("test-1", "Test Agent 1");
        var eventTriggered = false;
        AIAgentRegisteredEventArgs? eventArgs = null;

        _registry.AgentRegistered += (sender, args) =>
        {
            eventTriggered = true;
            eventArgs = args;
            return Task.CompletedTask;
        };

        // Act
        await _registry.RegisterAsync(agent);

        // Assert
        Assert.True(eventTriggered);
        Assert.NotNull(eventArgs);
        Assert.Equal(agent.Id, eventArgs.Agent.Id);
    }

    [Fact]
    public async Task UnregisterAsync_Should_Trigger_AgentUnregistered_Event()
    {
        // Arrange
        await _registry.InitializeAsync();
        await _registry.StartAsync();

        var agent = new TestAIAgent("test-1", "Test Agent 1");
        await _registry.RegisterAsync(agent);

        var eventTriggered = false;
        AIAgentUnregisteredEventArgs? eventArgs = null;

        _registry.AgentUnregistered += (sender, args) =>
        {
            eventTriggered = true;
            eventArgs = args;
            return Task.CompletedTask;
        };

        // Act
        await _registry.UnregisterAsync(agent.Id);

        // Assert
        Assert.True(eventTriggered);
        Assert.NotNull(eventArgs);
        Assert.Equal(agent.Id, eventArgs.AgentId);
    }

    public async ValueTask DisposeAsync()
    {
        if (_registry != null)
        {
            await _registry.DisposeAsync();
        }
    }
}