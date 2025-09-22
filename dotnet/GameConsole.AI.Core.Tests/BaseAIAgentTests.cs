using GameConsole.AI.Core;
using GameConsole.Core.Abstractions;
using Xunit;

namespace GameConsole.AI.Core.Tests;

/// <summary>
/// Concrete test implementation of BaseAIAgent for testing purposes.
/// </summary>
public class ConcreteTestAgent : BaseAIAgent
{
    private readonly List<Type> _capabilities = new();
    private readonly List<Type> _handledRequestTypes = new();

    public ConcreteTestAgent(string agentId, string name, string version, string description, int priority = 50)
    {
        TestAgentId = agentId;
        TestName = name;
        TestVersion = version;
        TestDescription = description;
        TestPriority = priority;
    }

    public string TestAgentId { get; }
    public string TestName { get; }
    public string TestVersion { get; }
    public string TestDescription { get; }
    public int TestPriority { get; }

    public override string AgentId => TestAgentId;
    public override string Name => TestName;
    public override string Version => TestVersion;
    public override string Description => TestDescription;
    public override int Priority => TestPriority;

    // Test helpers
    public void AddCapability<T>() => _capabilities.Add(typeof(T));
    public void AddHandledRequestType<T>() => _handledRequestTypes.Add(typeof(T));

    public bool InitializeCalled { get; private set; }
    public bool StartCalled { get; private set; }
    public bool StopCalled { get; private set; }

    public override Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        InitializeCalled = true;
        return base.InitializeAsync(cancellationToken);
    }

    public override Task StartAsync(CancellationToken cancellationToken = default)
    {
        StartCalled = true;
        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken = default)
    {
        StopCalled = true;
        return base.StopAsync(cancellationToken);
    }

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
        // Simple test implementation
        if (typeof(TResponse) == typeof(TestResponseA))
        {
            var response = new TestResponseA() as TResponse;
            return Task.FromResult(response!);
        }
        
        var defaultResponse = Activator.CreateInstance<TResponse>();
        return Task.FromResult(defaultResponse);
    }
}

/// <summary>
/// Capability interface that our test agent can implement directly.
/// </summary>
public interface IDirectCapability
{
    Task<string> DoSomethingAsync();
}

/// <summary>
/// Test agent that directly implements a capability interface.
/// </summary>
public class DirectCapabilityAgent : BaseAIAgent, IDirectCapability
{
    public override string AgentId => "direct-capability-agent";
    public override string Name => "Direct Capability Agent";
    public override string Version => "1.0.0";
    public override string Description => "Agent that directly implements a capability";

    public override Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Type>>(new[] { typeof(IDirectCapability) });
    }

    public override Task<bool> CanHandleAsync<TRequest>(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(typeof(TRequest) == typeof(TestRequestA));
    }

    public override Task<TResponse> ProcessAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
    {
        var response = Activator.CreateInstance<TResponse>();
        return Task.FromResult(response);
    }

    public Task<string> DoSomethingAsync()
    {
        return Task.FromResult("Direct capability executed");
    }
}

/// <summary>
/// Tests for the BaseAIAgent abstract class.
/// </summary>
public class BaseAIAgentTests
{
    [Fact]
    public void Properties_ConcreteImplementation_ReturnsCorrectValues()
    {
        // Arrange & Act
        var agent = new ConcreteTestAgent(
            "test-agent-123",
            "Test Agent Name",
            "2.1.0",
            "This is a test agent for unit testing",
            75
        );

        // Assert
        Assert.Equal("test-agent-123", agent.AgentId);
        Assert.Equal("Test Agent Name", agent.Name);
        Assert.Equal("2.1.0", agent.Version);
        Assert.Equal("This is a test agent for unit testing", agent.Description);
        Assert.Equal(75, agent.Priority);
    }

    [Fact]
    public void Priority_DefaultImplementation_Returns50()
    {
        // Arrange & Act
        var agent = new ConcreteTestAgent("test", "Test", "1.0", "Test description");

        // Assert
        Assert.Equal(50, agent.Priority);
    }

    [Fact]
    public void IsRunning_InitialState_ReturnsFalse()
    {
        // Arrange
        var agent = new ConcreteTestAgent("test", "Test", "1.0", "Test");

        // Act & Assert
        Assert.False(agent.IsRunning);
    }

    [Fact]
    public async Task StartAsync_CallsVirtualMethod_SetsIsRunningTrue()
    {
        // Arrange
        var agent = new ConcreteTestAgent("test", "Test", "1.0", "Test");

        // Act
        await agent.StartAsync();

        // Assert
        Assert.True(agent.IsRunning);
        Assert.True(agent.StartCalled);
    }

    [Fact]
    public async Task StopAsync_CallsVirtualMethod_SetsIsRunningFalse()
    {
        // Arrange
        var agent = new ConcreteTestAgent("test", "Test", "1.0", "Test");
        await agent.StartAsync();

        // Act
        await agent.StopAsync();

        // Assert
        Assert.False(agent.IsRunning);
        Assert.True(agent.StopCalled);
    }

    [Fact]
    public async Task InitializeAsync_CallsVirtualMethod_CompletesSuccessfully()
    {
        // Arrange
        var agent = new ConcreteTestAgent("test", "Test", "1.0", "Test");

        // Act
        await agent.InitializeAsync();

        // Assert
        Assert.True(agent.InitializeCalled);
    }

    [Fact]
    public async Task HasCapabilityAsync_CapabilityExists_ReturnsTrue()
    {
        // Arrange
        var agent = new ConcreteTestAgent("test", "Test", "1.0", "Test");
        agent.AddCapability<ITestCapabilityA>();

        // Act
        var hasCapability = await agent.HasCapabilityAsync<ITestCapabilityA>();

        // Assert
        Assert.True(hasCapability);
    }

    [Fact]
    public async Task HasCapabilityAsync_CapabilityDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var agent = new ConcreteTestAgent("test", "Test", "1.0", "Test");

        // Act
        var hasCapability = await agent.HasCapabilityAsync<ITestCapabilityB>();

        // Assert
        Assert.False(hasCapability);
    }

    [Fact]
    public async Task HasCapabilityAsync_DirectImplementation_ReturnsTrue()
    {
        // Arrange
        var agent = new DirectCapabilityAgent();

        // Act
        var hasCapability = await agent.HasCapabilityAsync<IDirectCapability>();

        // Assert
        Assert.True(hasCapability);
    }

    [Fact]
    public async Task GetCapabilityAsync_DirectImplementation_ReturnsAgentInstance()
    {
        // Arrange
        var agent = new DirectCapabilityAgent();

        // Act
        var capability = await agent.GetCapabilityAsync<IDirectCapability>();

        // Assert
        Assert.NotNull(capability);
        Assert.Same(agent, capability);
    }

    [Fact]
    public async Task GetCapabilityAsync_NotImplemented_ReturnsNull()
    {
        // Arrange
        var agent = new ConcreteTestAgent("test", "Test", "1.0", "Test");

        // Act
        var capability = await agent.GetCapabilityAsync<ITestCapabilityA>();

        // Assert
        Assert.Null(capability);
    }

    [Fact]
    public async Task CanHandleAsync_HandledRequestType_ReturnsTrue()
    {
        // Arrange
        var agent = new ConcreteTestAgent("test", "Test", "1.0", "Test");
        agent.AddHandledRequestType<TestRequestA>();

        // Act
        var canHandle = await agent.CanHandleAsync<TestRequestA>();

        // Assert
        Assert.True(canHandle);
    }

    [Fact]
    public async Task CanHandleAsync_NotHandledRequestType_ReturnsFalse()
    {
        // Arrange
        var agent = new ConcreteTestAgent("test", "Test", "1.0", "Test");

        // Act
        var canHandle = await agent.CanHandleAsync<TestRequestB>();

        // Assert
        Assert.False(canHandle);
    }

    [Fact]
    public async Task ProcessAsync_ValidRequestAndResponse_ProcessesSuccessfully()
    {
        // Arrange
        var agent = new ConcreteTestAgent("test", "Test", "1.0", "Test");
        var request = new TestRequestA();

        // Act
        var response = await agent.ProcessAsync<TestRequestA, TestResponseA>(request);

        // Assert
        Assert.NotNull(response);
        Assert.IsType<TestResponseA>(response);
    }

    [Fact]
    public async Task DisposeAsync_StopsAgentIfRunning()
    {
        // Arrange
        var agent = new ConcreteTestAgent("test", "Test", "1.0", "Test");
        await agent.StartAsync();
        Assert.True(agent.IsRunning);

        // Act
        await agent.DisposeAsync();

        // Assert
        Assert.False(agent.IsRunning);
        Assert.True(agent.StopCalled);
    }

    [Fact]
    public async Task DisposeAsync_AlreadyStopped_CompletesSuccessfully()
    {
        // Arrange
        var agent = new ConcreteTestAgent("test", "Test", "1.0", "Test");
        Assert.False(agent.IsRunning);

        // Act & Assert - Should not throw
        await agent.DisposeAsync();
        
        Assert.False(agent.IsRunning);
    }

    [Fact]
    public async Task GetCapabilitiesAsync_MultipleCapabilities_ReturnsAllCapabilities()
    {
        // Arrange
        var agent = new ConcreteTestAgent("test", "Test", "1.0", "Test");
        agent.AddCapability<ITestCapabilityA>();
        agent.AddCapability<ITestCapabilityB>();

        // Act
        var capabilities = await agent.GetCapabilitiesAsync();

        // Assert
        var capabilityList = capabilities.ToList();
        Assert.Equal(2, capabilityList.Count);
        Assert.Contains(typeof(ITestCapabilityA), capabilityList);
        Assert.Contains(typeof(ITestCapabilityB), capabilityList);
    }

    [Fact]
    public async Task DirectCapabilityIntegration_WorksEndToEnd()
    {
        // Arrange
        var agent = new DirectCapabilityAgent();

        // Act - Test capability detection
        var hasCapability = await agent.HasCapabilityAsync<IDirectCapability>();
        var capability = await agent.GetCapabilityAsync<IDirectCapability>();
        var result = await capability!.DoSomethingAsync();

        // Assert
        Assert.True(hasCapability);
        Assert.NotNull(capability);
        Assert.Equal("Direct capability executed", result);
    }
}