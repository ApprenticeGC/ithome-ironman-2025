using GameConsole.Core.Abstractions;
using GameConsole.Core.Registry;
using Xunit;

namespace GameConsole.Core.Registry.Tests;

/// <summary>
/// Tests for the AIAgentRegistry class focusing on agent registration and discovery.
/// </summary>
public class AIAgentRegistryTests : IAsyncDisposable
{
    private readonly AIAgentRegistry _registry;

    public AIAgentRegistryTests()
    {
        _registry = new AIAgentRegistry();
    }

    [Fact]
    public async Task RegisterAgentAsync_Should_Register_Agent_Successfully()
    {
        // Arrange
        var agent = new TestAIAgent("test-agent", "TestBot", priority: 1);

        // Act
        await _registry.RegisterAgentAsync(agent);

        // Assert
        var isRegistered = await _registry.IsRegisteredAsync("test-agent");
        Assert.True(isRegistered);
    }

    [Fact]
    public async Task RegisterAgentAsync_Should_Throw_If_Agent_Already_Registered()
    {
        // Arrange
        var agent1 = new TestAIAgent("test-agent", "TestBot", priority: 1);
        var agent2 = new TestAIAgent("test-agent", "TestBot", priority: 2);
        await _registry.RegisterAgentAsync(agent1);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _registry.RegisterAgentAsync(agent2));
    }

    [Fact]
    public async Task UnregisterAgentAsync_Should_Remove_Agent()
    {
        // Arrange
        var agent = new TestAIAgent("test-agent", "TestBot", priority: 1);
        await _registry.RegisterAgentAsync(agent);

        // Act
        var result = await _registry.UnregisterAgentAsync("test-agent");

        // Assert
        Assert.True(result);
        var isRegistered = await _registry.IsRegisteredAsync("test-agent");
        Assert.False(isRegistered);
    }

    [Fact]
    public async Task UnregisterAgentAsync_Should_Return_False_If_Agent_Not_Found()
    {
        // Act
        var result = await _registry.UnregisterAgentAsync("non-existent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetAgentAsync_Should_Return_Registered_Agent()
    {
        // Arrange
        var agent = new TestAIAgent("test-agent", "TestBot", priority: 1);
        await _registry.RegisterAgentAsync(agent);

        // Act
        var result = await _registry.GetAgentAsync("test-agent");

        // Assert
        Assert.NotNull(result);
        Assert.Same(agent, result);
    }

    [Fact]
    public async Task GetAgentAsync_Should_Return_Null_If_Agent_Not_Found()
    {
        // Act
        var result = await _registry.GetAgentAsync("non-existent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAgentsByTypeAsync_Should_Return_Agents_By_Type_Ordered_By_Priority()
    {
        // Arrange
        var agent1 = new TestAIAgent("agent-1", "TestBot", priority: 1);
        var agent2 = new TestAIAgent("agent-2", "TestBot", priority: 3);
        var agent3 = new TestAIAgent("agent-3", "Assistant", priority: 2);
        
        await _registry.RegisterAgentAsync(agent1);
        await _registry.RegisterAgentAsync(agent2);
        await _registry.RegisterAgentAsync(agent3);

        // Act
        var testBots = await _registry.GetAgentsByTypeAsync("TestBot");

        // Assert
        var testBotList = testBots.ToList();
        Assert.Equal(2, testBotList.Count);
        Assert.Same(agent2, testBotList[0]); // Higher priority first
        Assert.Same(agent1, testBotList[1]);
    }

    [Fact]
    public async Task FindCapableAgentsAsync_Should_Return_Agents_That_Can_Handle_Request()
    {
        // Arrange
        var request = new TestRequest("test message");
        var agent1 = new TestAIAgent("agent-1", "TestBot", priority: 1, canHandle: true);
        var agent2 = new TestAIAgent("agent-2", "TestBot", priority: 2, canHandle: false);
        var agent3 = new TestAIAgent("agent-3", "TestBot", priority: 3, canHandle: true);
        
        await _registry.RegisterAgentAsync(agent1);
        await _registry.RegisterAgentAsync(agent2);
        await _registry.RegisterAgentAsync(agent3);

        // Act
        var capableAgents = await _registry.FindCapableAgentsAsync(request);

        // Assert
        var capableList = capableAgents.ToList();
        Assert.Equal(2, capableList.Count);
        Assert.Same(agent3, capableList[0]); // Higher priority first
        Assert.Same(agent1, capableList[1]);
    }

    [Fact]
    public async Task FindAgentsByCapabilityAsync_Should_Return_Agents_With_Capability()
    {
        // Arrange
        var agent1 = new TestAIAgent("agent-1", "TestBot", priority: 1, hasCapability: true);
        var agent2 = new TestAIAgent("agent-2", "TestBot", priority: 2, hasCapability: false);
        var agent3 = new TestAIAgent("agent-3", "Assistant", priority: 3, hasCapability: true);
        
        await _registry.RegisterAgentAsync(agent1);
        await _registry.RegisterAgentAsync(agent2);
        await _registry.RegisterAgentAsync(agent3);

        // Act
        var capableAgents = await _registry.FindAgentsByCapabilityAsync<ITestCapability>();

        // Assert
        var capableList = capableAgents.ToList();
        Assert.Equal(2, capableList.Count);
        Assert.Same(agent3, capableList[0]); // Higher priority first
        Assert.Same(agent1, capableList[1]);
    }

    [Fact]
    public async Task GetAllAgentsAsync_Should_Return_All_Registered_Agents()
    {
        // Arrange
        var agent1 = new TestAIAgent("agent-1", "TestBot", priority: 1);
        var agent2 = new TestAIAgent("agent-2", "Assistant", priority: 2);
        
        await _registry.RegisterAgentAsync(agent1);
        await _registry.RegisterAgentAsync(agent2);

        // Act
        var allAgents = await _registry.GetAllAgentsAsync();

        // Assert
        var allList = allAgents.ToList();
        Assert.Equal(2, allList.Count);
        Assert.Contains(agent1, allList);
        Assert.Contains(agent2, allList);
    }

    public async ValueTask DisposeAsync()
    {
        await _registry.DisposeAsync();
    }
}

// Test helper classes
public interface ITestCapability : IAIAgentCapability
{
}

public class TestCapability : ITestCapability
{
    public string Name => "TestCapability";
    public string Version => "1.0.0";
    public string Description => "Test capability for unit testing";
}

public class TestRequest
{
    public string Message { get; }

    public TestRequest(string message)
    {
        Message = message;
    }
}

public class TestAIAgent : IAIAgent
{
    private readonly bool _canHandle;
    private readonly bool _hasCapability;
    private bool _isRunning;

    public TestAIAgent(string agentId, string agentType, int priority = 0, bool canHandle = true, bool hasCapability = true)
    {
        AgentId = agentId;
        AgentType = agentType;
        Priority = priority;
        _canHandle = canHandle;
        _hasCapability = hasCapability;
    }

    public string AgentId { get; }
    public string AgentType { get; }
    public int Priority { get; }
    public bool IsRunning => _isRunning;

    public Task<bool> CanHandleAsync(object request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_canHandle);
    }

    public Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var capabilities = _hasCapability 
            ? new[] { typeof(ITestCapability) } 
            : Array.Empty<Type>();
        return Task.FromResult(capabilities.AsEnumerable());
    }

    public Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_hasCapability && typeof(T) == typeof(ITestCapability));
    }

    public Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (_hasCapability && typeof(T) == typeof(ITestCapability))
        {
            return Task.FromResult(new TestCapability() as T);
        }
        return Task.FromResult<T?>(null);
    }

    // IService implementation - minimal for testing
    public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    
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
    
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}