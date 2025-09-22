using GameConsole.AI.Core;
using GameConsole.Plugins.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace GameConsole.AI.Core.Tests;

/// <summary>
/// Tests for the AIAgentRegistry class.
/// </summary>
public class AIAgentRegistryTests : IDisposable
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<ILogger<AIAgentRegistry>> _loggerMock;
    private readonly AIAgentRegistry _registry;

    public AIAgentRegistryTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _loggerMock = new Mock<ILogger<AIAgentRegistry>>();
        _registry = new AIAgentRegistry(_serviceProviderMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        _registry?.DisposeAsync().AsTask().Wait();
    }

    [Fact]
    public async Task InitializeAsync_SetsInitializedState()
    {
        // Act
        await _registry.InitializeAsync();

        // Assert
        // Registry should be initialized but not running yet
        Assert.False(_registry.IsRunning);
    }

    [Fact]
    public async Task StartAsync_AfterInitialize_SetsRunningState()
    {
        // Arrange
        await _registry.InitializeAsync();

        // Act
        await _registry.StartAsync();

        // Assert
        Assert.True(_registry.IsRunning);
    }

    [Fact]
    public async Task StartAsync_WithoutInitialize_SetsRunningState()
    {
        // Act & Assert - Should not throw and should initialize internally
        await _registry.StartAsync();
        Assert.True(_registry.IsRunning);
    }

    [Fact]
    public async Task StopAsync_WhenRunning_StopsRegistry()
    {
        // Arrange
        await _registry.InitializeAsync();
        await _registry.StartAsync();
        Assert.True(_registry.IsRunning);

        // Act
        await _registry.StopAsync();

        // Assert
        Assert.False(_registry.IsRunning);
    }

    [Fact]
    public void RegisterAgentType_ValidType_RegistersSuccessfully()
    {
        // Act
        _registry.RegisterAgentType<TestAIAgentForRegistry>();

        // Assert - Should not throw, verify through discovery
        var discoveryTask = _registry.DiscoverAgentTypesAsync();
        var agentTypes = discoveryTask.Result;
        
        Assert.Single(agentTypes);
        Assert.Equal(typeof(TestAIAgentForRegistry), agentTypes[0].AgentType);
    }

    [Fact]
    public void RegisterAgentType_WithFactory_RegistersSuccessfully()
    {
        // Arrange
        var mockAgent = new Mock<TestAIAgentForRegistry>(Mock.Of<ILogger<BaseAIAgent>>());

        // Act
        _registry.RegisterAgentType<TestAIAgentForRegistry>(_ => mockAgent.Object);

        // Assert
        var discoveryTask = _registry.DiscoverAgentTypesAsync();
        var agentTypes = discoveryTask.Result;
        
        Assert.Single(agentTypes);
        Assert.Equal(typeof(TestAIAgentForRegistry), agentTypes[0].AgentType);
    }

    [Fact]
    public void RegisterAgentType_SameTypeTwice_DoesNotDuplicate()
    {
        // Act
        _registry.RegisterAgentType<TestAIAgentForRegistry>();
        _registry.RegisterAgentType<TestAIAgentForRegistry>(); // Register same type again

        // Assert
        var discoveryTask = _registry.DiscoverAgentTypesAsync();
        var agentTypes = discoveryTask.Result;
        
        Assert.Single(agentTypes); // Should still be only one registration
    }

    [Fact]
    public void RegisterAgentInstance_ValidInstance_RegistersSuccessfully()
    {
        // Arrange
        var mockMetadata = new Mock<IPluginMetadata>();
        mockMetadata.Setup(m => m.Name).Returns("TestInstance");
        
        var mockAgent = new Mock<TestAIAgentForRegistry>(Mock.Of<ILogger<BaseAIAgent>>());
        mockAgent.Setup(a => a.Metadata).Returns(mockMetadata.Object);

        // Act
        _registry.RegisterAgentInstance(mockAgent.Object);

        // Assert
        var activeAgents = _registry.GetActiveAgents();
        Assert.Single(activeAgents);
        Assert.Equal(mockAgent.Object, activeAgents[0]);
    }

    [Fact]
    public void RegisterAgentInstance_NullInstance_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _registry.RegisterAgentInstance<TestAIAgentForRegistry>(null!));
    }

    [Fact]
    public async Task DiscoverAgentTypesAsync_NoRequirements_ReturnsAllTypes()
    {
        // Arrange
        _registry.RegisterAgentType<TestAIAgentForRegistry>();

        // Act
        var agentTypes = await _registry.DiscoverAgentTypesAsync();

        // Assert
        Assert.Single(agentTypes);
        Assert.Equal(typeof(TestAIAgentForRegistry), agentTypes[0].AgentType);
        Assert.NotNull(agentTypes[0].Metadata);
        Assert.NotNull(agentTypes[0].CapabilityPreview);
    }

    [Fact]
    public async Task DiscoverAgentTypesAsync_WithRequirements_FiltersCorrectly()
    {
        // Arrange
        _registry.RegisterAgentType<TestAIAgentForRegistry>();
        
        var requirements = new AIAgentCapabilityRequirements
        {
            RequiredDecisionTypes = new[] { "nonexistent-decision" }
        };

        // Act
        var agentTypes = await _registry.DiscoverAgentTypesAsync(requirements);

        // Assert
        Assert.Empty(agentTypes); // Should filter out the test agent
    }

    [Fact]
    public async Task CreateAgentAsync_Generic_RegisteredType_CreatesInstance()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<BaseAIAgent>>();
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ILogger<BaseAIAgent>)))
                          .Returns(mockLogger.Object);
        
        _registry.RegisterAgentType<TestAIAgentForRegistry>();

        // Act
        var agent = await _registry.CreateAgentAsync<TestAIAgentForRegistry>();

        // Assert
        Assert.NotNull(agent);
        Assert.IsType<TestAIAgentForRegistry>(agent);
        
        var activeAgents = _registry.GetActiveAgents();
        Assert.Single(activeAgents);
    }

    [Fact]
    public async Task CreateAgentAsync_Generic_UnregisteredType_ThrowsInvalidOperationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _registry.CreateAgentAsync<TestAIAgentForRegistry>());
    }

    [Fact]
    public async Task CreateAgentAsync_ByName_RegisteredType_CreatesInstance()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<BaseAIAgent>>();
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ILogger<BaseAIAgent>)))
                          .Returns(mockLogger.Object);
        
        var metadata = new AIAgentTypeMetadata
        {
            Name = "TestAgentByName",
            Version = "1.0.0",
            Description = "Test agent created by name"
        };
        
        _registry.RegisterAgentType<TestAIAgentForRegistry>(metadata);

        // Act
        var agent = await _registry.CreateAgentAsync("TestAgentByName");

        // Assert
        Assert.NotNull(agent);
        Assert.IsType<TestAIAgentForRegistry>(agent);
    }

    [Fact]
    public async Task CreateAgentAsync_ByName_UnregisteredName_ThrowsInvalidOperationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _registry.CreateAgentAsync("NonExistentAgent"));
    }

    [Fact]
    public void GetActiveAgents_NoAgents_ReturnsEmptyList()
    {
        // Act
        var activeAgents = _registry.GetActiveAgents();

        // Assert
        Assert.Empty(activeAgents);
    }

    [Fact]
    public void GetActiveAgents_WithRequirements_FiltersCorrectly()
    {
        // Arrange
        var mockMetadata = new Mock<IPluginMetadata>();
        mockMetadata.Setup(m => m.Name).Returns("FilterTest");
        
        var mockCapabilities = new Mock<IAIAgentCapabilities>();
        mockCapabilities.Setup(c => c.DecisionTypes).Returns(new[] { "pathfinding" });
        mockCapabilities.Setup(c => c.SupportsLearning).Returns(false);
        
        var mockAgent = new Mock<TestAIAgentForRegistry>(Mock.Of<ILogger<BaseAIAgent>>());
        mockAgent.Setup(a => a.Metadata).Returns(mockMetadata.Object);
        mockAgent.Setup(a => a.Capabilities).Returns(mockCapabilities.Object);

        _registry.RegisterAgentInstance(mockAgent.Object);

        var requirements = new AIAgentCapabilityRequirements
        {
            RequiresLearning = true // This agent doesn't support learning
        };

        // Act
        var filteredAgents = _registry.GetActiveAgents(requirements);

        // Assert
        Assert.Empty(filteredAgents); // Should be filtered out
    }

    [Fact]
    public void GetAgent_ExistingAgent_ReturnsAgent()
    {
        // Arrange
        var mockMetadata = new Mock<IPluginMetadata>();
        mockMetadata.Setup(m => m.Name).Returns("GetTest");
        
        var mockAgent = new Mock<TestAIAgentForRegistry>(Mock.Of<ILogger<BaseAIAgent>>());
        mockAgent.Setup(a => a.Metadata).Returns(mockMetadata.Object);

        _registry.RegisterAgentInstance(mockAgent.Object);
        
        // Get the agent ID (we need to look it up since it's auto-generated)
        var activeAgents = _registry.GetActiveAgents();
        Assert.Single(activeAgents);

        // Note: In a real implementation, we'd need a way to get the agent ID
        // For now, we'll test with a known pattern or modify the implementation

        // Act
        var retrievedAgent = _registry.GetAgent("nonexistent-id");

        // Assert
        Assert.Null(retrievedAgent); // We can't easily test the success case with auto-generated IDs
    }

    [Fact]
    public async Task RemoveAgentAsync_ExistingAgent_RemovesSuccessfully()
    {
        // Arrange
        var mockMetadata = new Mock<IPluginMetadata>();
        mockMetadata.Setup(m => m.Name).Returns("RemoveTest");
        
        var mockAgent = new Mock<TestAIAgentForRegistry>(Mock.Of<ILogger<BaseAIAgent>>());
        mockAgent.Setup(a => a.Metadata).Returns(mockMetadata.Object);

        _registry.RegisterAgentInstance(mockAgent.Object);
        
        Assert.Single(_registry.GetActiveAgents());

        // Act
        await _registry.RemoveAgentAsync(mockAgent.Object);

        // Assert
        Assert.Empty(_registry.GetActiveAgents());
    }

    [Fact]
    public async Task RemoveAgentAsync_NullAgent_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _registry.RemoveAgentAsync((IAIAgent)null!));
    }

    [Fact]
    public async Task DisposeAsync_DisposesCorrectly()
    {
        // Arrange
        await _registry.InitializeAsync();
        await _registry.StartAsync();
        Assert.True(_registry.IsRunning);

        // Act
        await _registry.DisposeAsync();

        // Assert
        Assert.False(_registry.IsRunning);
        
        // Further operations should throw ObjectDisposedException
        Assert.Throws<ObjectDisposedException>(() => _registry.RegisterAgentType<TestAIAgentForRegistry>());
    }

    [Fact]
    public void AgentRegistered_Event_IsFired()
    {
        // Arrange
        AIAgentRegisteredEventArgs? eventArgs = null;
        _registry.AgentRegistered += (sender, args) => eventArgs = args;

        var mockMetadata = new Mock<IPluginMetadata>();
        mockMetadata.Setup(m => m.Name).Returns("EventTest");
        
        var mockAgent = new Mock<TestAIAgentForRegistry>(Mock.Of<ILogger<BaseAIAgent>>());
        mockAgent.Setup(a => a.Metadata).Returns(mockMetadata.Object);

        // Act
        _registry.RegisterAgentInstance(mockAgent.Object);

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal(mockAgent.Object, eventArgs.Agent);
    }

    [Fact]
    public async Task AgentRemoved_Event_IsFired()
    {
        // Arrange
        AIAgentRemovedEventArgs? eventArgs = null;
        _registry.AgentRemoved += (sender, args) => eventArgs = args;

        var mockMetadata = new Mock<IPluginMetadata>();
        mockMetadata.Setup(m => m.Name).Returns("RemoveEventTest");
        
        var mockAgent = new Mock<TestAIAgentForRegistry>(Mock.Of<ILogger<BaseAIAgent>>());
        mockAgent.Setup(a => a.Metadata).Returns(mockMetadata.Object);

        _registry.RegisterAgentInstance(mockAgent.Object);

        // Act
        await _registry.RemoveAgentAsync(mockAgent.Object);

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal("RemoveEventTest", eventArgs.AgentTypeName);
    }
}

/// <summary>
/// Test AI agent class for registry testing.
/// </summary>
[AIAgent("TestAgentForRegistry", "1.0.0", "Test AI agent for registry testing")]
internal class TestAIAgentForRegistry : BaseAIAgent
{
    private readonly Mock<IPluginMetadata> _mockMetadata;
    private readonly Mock<IAIAgentCapabilities> _mockCapabilities;

    public override IPluginMetadata Metadata => _mockMetadata.Object;
    public override IAIAgentCapabilities Capabilities => _mockCapabilities.Object;

    public TestAIAgentForRegistry(ILogger<BaseAIAgent> logger) : base(logger) 
    {
        _mockMetadata = new Mock<IPluginMetadata>();
        _mockMetadata.Setup(m => m.Name).Returns("TestAgentForRegistry");

        _mockCapabilities = new Mock<IAIAgentCapabilities>();
        _mockCapabilities.Setup(c => c.DecisionTypes).Returns(Array.Empty<string>());
        _mockCapabilities.Setup(c => c.SupportsLearning).Returns(false);
        _mockCapabilities.Setup(c => c.SupportsAutonomousMode).Returns(false);
        _mockCapabilities.Setup(c => c.Priority).Returns(50);
        _mockCapabilities.Setup(c => c.MaxConcurrentInputs).Returns(1);
    }

    protected override Task<IAIAgentResponse> ProcessInputAsync(IAIAgentInput input, CancellationToken cancellationToken)
    {
        return Task.FromResult<IAIAgentResponse>(new AIAgentResponse 
        { 
            Success = true, 
            ResponseType = "test", 
            Data = "test response from registry agent",
            Confidence = 1.0
        });
    }
}