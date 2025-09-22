using GameConsole.AI.Core;
using GameConsole.Core.Abstractions;
using Xunit;

namespace GameConsole.AI.Core.Tests;

/// <summary>
/// Test implementation of AI agent for testing purposes.
/// </summary>
public class TestAIAgent : BaseAIAgent
{
    private readonly List<Type> _capabilities = new();
    private readonly List<Type> _handledRequestTypes = new();

    public TestAIAgent(string agentId, string name, int priority = 50)
    {
        TestAgentId = agentId;
        TestName = name;
        TestPriority = priority;
    }

    public string TestAgentId { get; }
    public string TestName { get; }
    public int TestPriority { get; }

    public override string AgentId => TestAgentId;
    public override string Name => TestName;
    public override string Version => "1.0.0";
    public override string Description => $"Test AI Agent: {Name}";
    public override int Priority => TestPriority;

    public void AddCapability<T>() => _capabilities.Add(typeof(T));
    public void AddHandledRequestType<T>() => _handledRequestTypes.Add(typeof(T));

    public override Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Type>>(_capabilities);
    }

    public override Task<bool> CanHandleAsync<TRequest>(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_handledRequestTypes.Contains(typeof(TRequest)));
    }

    public override Task<TResponse> ProcessAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
    {
        // Simple test implementation that returns a default response
        var response = Activator.CreateInstance<TResponse>();
        return Task.FromResult(response);
    }
}

/// <summary>
/// Test capability interfaces for testing purposes.
/// </summary>
public interface ITestCapabilityA { }
public interface ITestCapabilityB { }

/// <summary>
/// Test request types for testing purposes.
/// </summary>
public class TestRequestA { }
public class TestRequestB { }
public class TestResponseA { }
public class TestResponseB { }

/// <summary>
/// Tests for the AIAgentRegistry class.
/// </summary>
public class AIAgentRegistryTests
{
    [Fact]
    public async Task RegisterAgentAsync_ValidAgent_RegistersSuccessfully()
    {
        // Arrange
        var registry = new AIAgentRegistry();
        var agent = new TestAIAgent("test-agent-1", "Test Agent 1");
        
        await registry.InitializeAsync();
        await registry.StartAsync();

        // Act
        await registry.RegisterAgentAsync(agent);

        // Assert
        var registeredAgent = await registry.GetAgentAsync("test-agent-1");
        Assert.NotNull(registeredAgent);
        Assert.Equal("test-agent-1", registeredAgent.AgentId);
        Assert.Equal("Test Agent 1", registeredAgent.Name);
    }

    [Fact]
    public async Task RegisterAgentAsync_NullAgent_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new AIAgentRegistry();
        await registry.InitializeAsync();
        await registry.StartAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => registry.RegisterAgentAsync(null!));
    }

    [Fact]
    public async Task RegisterAgentAsync_DuplicateAgentId_ThrowsArgumentException()
    {
        // Arrange
        var registry = new AIAgentRegistry();
        var agent1 = new TestAIAgent("duplicate-id", "Agent 1");
        var agent2 = new TestAIAgent("duplicate-id", "Agent 2");
        
        await registry.InitializeAsync();
        await registry.StartAsync();
        await registry.RegisterAgentAsync(agent1);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => registry.RegisterAgentAsync(agent2));
    }

    [Fact]
    public async Task UnregisterAgentAsync_ExistingAgent_UnregistersSuccessfully()
    {
        // Arrange
        var registry = new AIAgentRegistry();
        var agent = new TestAIAgent("test-agent", "Test Agent");
        
        await registry.InitializeAsync();
        await registry.StartAsync();
        await registry.RegisterAgentAsync(agent);

        // Act
        await registry.UnregisterAgentAsync("test-agent");

        // Assert
        var retrievedAgent = await registry.GetAgentAsync("test-agent");
        Assert.Null(retrievedAgent);
    }

    [Fact]
    public async Task UnregisterAgentAsync_NonExistentAgent_DoesNotThrow()
    {
        // Arrange
        var registry = new AIAgentRegistry();
        await registry.InitializeAsync();
        await registry.StartAsync();

        // Act & Assert - Should not throw
        await registry.UnregisterAgentAsync("non-existent-agent");
    }

    [Fact]
    public async Task GetAllAgentsAsync_MultipleAgents_ReturnsAllAgents()
    {
        // Arrange
        var registry = new AIAgentRegistry();
        var agent1 = new TestAIAgent("agent-1", "Agent 1");
        var agent2 = new TestAIAgent("agent-2", "Agent 2");
        var agent3 = new TestAIAgent("agent-3", "Agent 3");
        
        await registry.InitializeAsync();
        await registry.StartAsync();
        await registry.RegisterAgentAsync(agent1);
        await registry.RegisterAgentAsync(agent2);
        await registry.RegisterAgentAsync(agent3);

        // Act
        var allAgents = await registry.GetAllAgentsAsync();

        // Assert
        Assert.Equal(3, allAgents.Count());
        Assert.Contains(allAgents, a => a.AgentId == "agent-1");
        Assert.Contains(allAgents, a => a.AgentId == "agent-2");
        Assert.Contains(allAgents, a => a.AgentId == "agent-3");
    }

    [Fact]
    public async Task DiscoverAgentsByCapabilityAsync_AgentsWithCapability_ReturnsOrderedByPriority()
    {
        // Arrange
        var registry = new AIAgentRegistry();
        var lowPriorityAgent = new TestAIAgent("low", "Low Priority", 10);
        var highPriorityAgent = new TestAIAgent("high", "High Priority", 100);
        var mediumPriorityAgent = new TestAIAgent("medium", "Medium Priority", 50);
        
        lowPriorityAgent.AddCapability<ITestCapabilityA>();
        highPriorityAgent.AddCapability<ITestCapabilityA>();
        mediumPriorityAgent.AddCapability<ITestCapabilityA>();
        
        await registry.InitializeAsync();
        await registry.StartAsync();
        await registry.RegisterAgentAsync(lowPriorityAgent);
        await registry.RegisterAgentAsync(highPriorityAgent);
        await registry.RegisterAgentAsync(mediumPriorityAgent);

        // Act
        var agents = await registry.DiscoverAgentsByCapabilityAsync<ITestCapabilityA>();

        // Assert
        var agentsList = agents.ToList();
        Assert.Equal(3, agentsList.Count);
        Assert.Equal("high", agentsList[0].AgentId); // Highest priority first
        Assert.Equal("medium", agentsList[1].AgentId);
        Assert.Equal("low", agentsList[2].AgentId);
    }

    [Fact]
    public async Task DiscoverAgentsByRequestAsync_AgentsHandlingRequest_ReturnsOrderedByPriority()
    {
        // Arrange
        var registry = new AIAgentRegistry();
        var lowPriorityAgent = new TestAIAgent("low", "Low Priority", 20);
        var highPriorityAgent = new TestAIAgent("high", "High Priority", 80);
        
        lowPriorityAgent.AddHandledRequestType<TestRequestA>();
        highPriorityAgent.AddHandledRequestType<TestRequestA>();
        
        await registry.InitializeAsync();
        await registry.StartAsync();
        await registry.RegisterAgentAsync(lowPriorityAgent);
        await registry.RegisterAgentAsync(highPriorityAgent);

        // Act
        var agents = await registry.DiscoverAgentsByRequestAsync<TestRequestA>();

        // Assert
        var agentsList = agents.ToList();
        Assert.Equal(2, agentsList.Count);
        Assert.Equal("high", agentsList[0].AgentId); // Highest priority first
        Assert.Equal("low", agentsList[1].AgentId);
    }

    [Fact]
    public async Task GetBestAgentForCapabilityAsync_MultipleAgents_ReturnsHighestPriority()
    {
        // Arrange
        var registry = new AIAgentRegistry();
        var agent1 = new TestAIAgent("agent-1", "Agent 1", 30);
        var agent2 = new TestAIAgent("agent-2", "Agent 2", 70);
        
        agent1.AddCapability<ITestCapabilityB>();
        agent2.AddCapability<ITestCapabilityB>();
        
        await registry.InitializeAsync();
        await registry.StartAsync();
        await registry.RegisterAgentAsync(agent1);
        await registry.RegisterAgentAsync(agent2);

        // Act
        var bestAgent = await registry.GetBestAgentForCapabilityAsync<ITestCapabilityB>();

        // Assert
        Assert.NotNull(bestAgent);
        Assert.Equal("agent-2", bestAgent.AgentId);
    }

    [Fact]
    public async Task GetBestAgentForRequestAsync_MultipleAgents_ReturnsHighestPriority()
    {
        // Arrange
        var registry = new AIAgentRegistry();
        var agent1 = new TestAIAgent("agent-1", "Agent 1", 25);
        var agent2 = new TestAIAgent("agent-2", "Agent 2", 75);
        
        agent1.AddHandledRequestType<TestRequestB>();
        agent2.AddHandledRequestType<TestRequestB>();
        
        await registry.InitializeAsync();
        await registry.StartAsync();
        await registry.RegisterAgentAsync(agent1);
        await registry.RegisterAgentAsync(agent2);

        // Act
        var bestAgent = await registry.GetBestAgentForRequestAsync<TestRequestB>();

        // Assert
        Assert.NotNull(bestAgent);
        Assert.Equal("agent-2", bestAgent.AgentId);
    }

    [Fact]
    public async Task AgentRegistered_Event_FiredOnRegistration()
    {
        // Arrange
        var registry = new AIAgentRegistry();
        var agent = new TestAIAgent("event-test", "Event Test Agent");
        IAIAgent? eventAgent = null;
        
        await registry.InitializeAsync();
        await registry.StartAsync();
        
        registry.AgentRegistered += (sender, a) => eventAgent = a;

        // Act
        await registry.RegisterAgentAsync(agent);

        // Assert
        Assert.NotNull(eventAgent);
        Assert.Equal("event-test", eventAgent.AgentId);
    }

    [Fact]
    public async Task AgentUnregistered_Event_FiredOnUnregistration()
    {
        // Arrange
        var registry = new AIAgentRegistry();
        var agent = new TestAIAgent("unregister-test", "Unregister Test Agent");
        string? unregisteredAgentId = null;
        
        await registry.InitializeAsync();
        await registry.StartAsync();
        await registry.RegisterAgentAsync(agent);
        
        registry.AgentUnregistered += (sender, id) => unregisteredAgentId = id;

        // Act
        await registry.UnregisterAgentAsync("unregister-test");

        // Assert
        Assert.Equal("unregister-test", unregisteredAgentId);
    }

    [Fact]
    public void IsRunning_InitialState_ReturnsFalse()
    {
        // Arrange
        var registry = new AIAgentRegistry();

        // Act & Assert
        Assert.False(registry.IsRunning);
    }

    [Fact]
    public async Task IsRunning_AfterStart_ReturnsTrue()
    {
        // Arrange
        var registry = new AIAgentRegistry();
        await registry.InitializeAsync();

        // Act
        await registry.StartAsync();

        // Assert
        Assert.True(registry.IsRunning);
    }

    [Fact]
    public async Task IsRunning_AfterStop_ReturnsFalse()
    {
        // Arrange
        var registry = new AIAgentRegistry();
        await registry.InitializeAsync();
        await registry.StartAsync();

        // Act
        await registry.StopAsync();

        // Assert
        Assert.False(registry.IsRunning);
    }
}