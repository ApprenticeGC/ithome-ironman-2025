using GameConsole.Core.Registry;
using GameConsole.Plugins.Core;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;
using Xunit;

namespace GameConsole.Core.Registry.Tests;

public class AIAgentDiscoveryServiceTests : IDisposable
{
    private AIAgentDiscoveryService _service;
    private Mock<ILogger<AIAgentDiscoveryService>> _mockLogger;

    public AIAgentDiscoveryServiceTests()
    {
        _mockLogger = new Mock<ILogger<AIAgentDiscoveryService>>();
        _service = new AIAgentDiscoveryService(_mockLogger.Object);
    }

    public void Dispose()
    {
        _service?.DisposeAsync().AsTask().Wait();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AIAgentDiscoveryService(null!));
    }

    [Fact]
    public void IsRunning_InitialState_ShouldBeFalse()
    {
        // Assert
        Assert.False(_service.IsRunning);
    }

    [Fact]
    public async Task StartAsync_ShouldSetIsRunningToTrue()
    {
        // Act
        await _service.StartAsync();

        // Assert
        Assert.True(_service.IsRunning);
    }

    [Fact]
    public async Task StopAsync_ShouldSetIsRunningToFalse()
    {
        // Arrange
        await _service.StartAsync();

        // Act
        await _service.StopAsync();

        // Assert
        Assert.False(_service.IsRunning);
    }

    [Fact]
    public async Task RegisterAgentAsync_WithValidAgent_ShouldRegisterSuccessfully()
    {
        // Arrange
        var agent = new TestAIAgent();

        // Act & Assert
        await _service.RegisterAgentAsync(agent); // Should not throw
    }

    [Fact]
    public async Task RegisterAgentAsync_WithNullAgent_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.RegisterAgentAsync(null!));
    }

    [Fact]
    public async Task RegisterAgentAsync_WithDuplicateAgent_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var agent = new TestAIAgent();
        await _service.RegisterAgentAsync(agent);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.RegisterAgentAsync(agent));
    }

    [Fact]
    public async Task UnregisterAgentAsync_WithExistingAgent_ShouldReturnTrue()
    {
        // Arrange
        var agent = new TestAIAgent();
        await _service.RegisterAgentAsync(agent);

        // Act
        var result = await _service.UnregisterAgentAsync(agent.Metadata.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task UnregisterAgentAsync_WithNonExistentAgent_ShouldReturnFalse()
    {
        // Act
        var result = await _service.UnregisterAgentAsync("non-existent");

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(null!)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UnregisterAgentAsync_WithNullOrEmptyId_ShouldThrowArgumentException(string? agentId)
    {
        // Act & Assert
        #pragma warning disable CS8604 // Possible null reference argument
        await Assert.ThrowsAsync<ArgumentException>(() => _service.UnregisterAgentAsync(agentId));
        #pragma warning restore CS8604 // Possible null reference argument
    }

    [Fact]
    public async Task DiscoverAgentsAsync_WithNoAgents_ShouldReturnEmptyCollection()
    {
        // Act
        var agents = await _service.DiscoverAgentsAsync();

        // Assert
        Assert.NotNull(agents);
        Assert.Empty(agents);
    }

    [Fact]
    public async Task DiscoverAgentsAsync_WithRegisteredAgents_ShouldReturnAllAgents()
    {
        // Arrange
        var agent1 = new TestAIAgent();
        var agent2 = new TestAIAgent2();
        await _service.RegisterAgentAsync(agent1);
        await _service.RegisterAgentAsync(agent2);

        // Act
        var agents = await _service.DiscoverAgentsAsync();

        // Assert
        Assert.NotNull(agents);
        Assert.Equal(2, agents.Count());
    }

    [Fact]
    public async Task FindAgentsByCapabilityAsync_WithMatchingCapability_ShouldReturnMatchingAgents()
    {
        // Arrange
        var agent = new TestAIAgent();
        await _service.RegisterAgentAsync(agent);

        // Act
        var agents = await _service.FindAgentsByCapabilityAsync<TestAICapability>();

        // Assert
        Assert.NotNull(agents);
        Assert.Single(agents);
        Assert.Same(agent, agents.First());
    }

    [Fact]
    public async Task FindAgentsByCapabilityNameAsync_WithMatchingCapabilityName_ShouldReturnMatchingAgents()
    {
        // Arrange
        var agent = new TestAIAgent();
        await _service.RegisterAgentAsync(agent);

        // Act
        var agents = await _service.FindAgentsByCapabilityNameAsync("test-capability");

        // Assert
        Assert.NotNull(agents);
        Assert.Single(agents);
        Assert.Same(agent, agents.First());
    }

    [Theory]
    [InlineData(null!)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task FindAgentsByCapabilityNameAsync_WithNullOrEmptyName_ShouldThrowArgumentException(string? capabilityName)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.FindAgentsByCapabilityNameAsync(capabilityName));
    }

    [Fact]
    public async Task GetAgentByIdAsync_WithExistingAgent_ShouldReturnAgent()
    {
        // Arrange
        var agent = new TestAIAgent();
        await _service.RegisterAgentAsync(agent);

        // Act
        var foundAgent = await _service.GetAgentByIdAsync(agent.Metadata.Id);

        // Assert
        Assert.NotNull(foundAgent);
        Assert.Same(agent, foundAgent);
    }

    [Fact]
    public async Task GetAgentByIdAsync_WithNonExistentAgent_ShouldReturnNull()
    {
        // Act
        var foundAgent = await _service.GetAgentByIdAsync("non-existent");

        // Assert
        Assert.Null(foundAgent);
    }

    [Theory]
    [InlineData(null!)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetAgentByIdAsync_WithNullOrEmptyId_ShouldThrowArgumentException(string? agentId)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetAgentByIdAsync(agentId));
    }

    [Fact]
    public async Task GetRunningAgentsAsync_WithRunningAgents_ShouldReturnOnlyRunningAgents()
    {
        // Arrange
        var runningAgent = new TestAIAgent();
        var stoppedAgent = new TestAIAgent2();
        
        await _service.RegisterAgentAsync(runningAgent);
        await _service.RegisterAgentAsync(stoppedAgent);
        
        await runningAgent.StartAsync();
        // stoppedAgent remains stopped

        // Act
        var runningAgents = await _service.GetRunningAgentsAsync();

        // Assert
        Assert.NotNull(runningAgents);
        Assert.Single(runningAgents);
        Assert.Same(runningAgent, runningAgents.First());
    }

    [Fact]
    public async Task GetRegisteredAgentCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var agent1 = new TestAIAgent();
        var agent2 = new TestAIAgent2();

        // Act & Assert - Initial count
        var initialCount = await _service.GetRegisteredAgentCountAsync();
        Assert.Equal(0, initialCount);

        // Act & Assert - After registering agents
        await _service.RegisterAgentAsync(agent1);
        await _service.RegisterAgentAsync(agent2);
        var finalCount = await _service.GetRegisteredAgentCountAsync();
        Assert.Equal(2, finalCount);
    }

    [Fact]
    public async Task RegisterFromAssemblyAsync_WithNullAssembly_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.RegisterFromAssemblyAsync(null!));
    }

    [Fact]
    public async Task RegisterFromAssemblyAsync_WithValidAssembly_ShouldRegisterAgents()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        var registeredCount = await _service.RegisterFromAssemblyAsync(assembly);

        // Assert - Should register TestAIAgent and TestAIAgent2 (if they have AIAgentAttribute)
        Assert.True(registeredCount > 0);
    }
}

// Test implementations
public class TestAICapability : IAIAgentCapability
{
    public string CapabilityName => "test-capability";
    public string Description => "A test capability";
}

[AIAgent("test-agent", "Test AI Agent", "1.0.0", "A test AI agent for unit testing", "Test Author", "Mock AI")]
public class TestAIAgent : IAIAgent
{
    private readonly TestAICapability _capability = new();

    public IPluginMetadata Metadata { get; } = new TestPluginMetadata("test-agent", "Test AI Agent");
    public IPluginContext? Context { get; set; }
    public bool IsRunning { get; private set; }

    public Task<IEnumerable<IAIAgentCapability>> GetAICapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<IAIAgentCapability>>(new[] { _capability });
    }

    public Task<bool> CanHandleRequestAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(request is string);
    }

    public Task<TResponse> ProcessRequestAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
    {
        if (request is string stringRequest && typeof(TResponse) == typeof(string))
        {
            var response = $"Processed: {stringRequest}";
            return Task.FromResult((TResponse)(object)response);
        }

        throw new NotSupportedException($"Cannot process request of type {typeof(TRequest)} to response of type {typeof(TResponse)}");
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        IsRunning = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        IsRunning = false;
        return Task.CompletedTask;
    }

    public Task ConfigureAsync(IPluginContext context, CancellationToken cancellationToken = default)
    {
        Context = context;
        return Task.CompletedTask;
    }

    public Task<bool> CanUnloadAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task PrepareUnloadAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        IsRunning = false;
        return ValueTask.CompletedTask;
    }
}

[AIAgent("test-agent-2", "Test AI Agent 2", "1.0.0", "Another test AI agent for unit testing", "Test Author", "Mock AI")]
public class TestAIAgent2 : IAIAgent
{
    public IPluginMetadata Metadata { get; } = new TestPluginMetadata("test-agent-2", "Test AI Agent 2");
    public IPluginContext? Context { get; set; }
    public bool IsRunning { get; private set; }

    public Task<IEnumerable<IAIAgentCapability>> GetAICapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<IAIAgentCapability>>(Array.Empty<IAIAgentCapability>());
    }

    public Task<bool> CanHandleRequestAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    public Task<TResponse> ProcessRequestAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("This test agent doesn't process requests");
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        IsRunning = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        IsRunning = false;
        return Task.CompletedTask;
    }

    public Task ConfigureAsync(IPluginContext context, CancellationToken cancellationToken = default)
    {
        Context = context;
        return Task.CompletedTask;
    }

    public Task<bool> CanUnloadAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task PrepareUnloadAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        IsRunning = false;
        return ValueTask.CompletedTask;
    }
}

public class TestPluginMetadata : IPluginMetadata
{
    public TestPluginMetadata(string id, string name)
    {
        Id = id;
        Name = name;
    }

    public string Id { get; }
    public string Name { get; }
    public Version Version => new(1, 0, 0);
    public string Description => "A test AI agent for unit testing";
    public string Author => "Test Author";
    public IReadOnlyList<string> Dependencies => Array.Empty<string>();
    public IReadOnlyDictionary<string, object> Properties => new Dictionary<string, object>();
}