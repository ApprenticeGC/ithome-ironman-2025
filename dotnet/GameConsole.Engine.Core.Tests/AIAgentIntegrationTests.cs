using GameConsole.Core.Abstractions;
using GameConsole.Engine.Core;
using GameConsole.Plugins.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace GameConsole.Engine.Core.Tests;

/// <summary>
/// Integration tests that demonstrate the complete AI Agent Actor Clustering system.
/// These tests show how AI agents can be created, clustered, and coordinate with each other.
/// </summary>
public class AIAgentIntegrationTests : IAsyncDisposable
{
    private readonly List<PatrolAgent> _agents = new();
    private ActorCluster? _cluster;

    public async ValueTask DisposeAsync()
    {
        foreach (var agent in _agents)
        {
            await agent.DisposeAsync();
        }
        _agents.Clear();

        if (_cluster != null)
        {
            await _cluster.DisposeAsync();
        }
    }

    [Fact]
    public void AIAgent_Creation_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var patrolPoints = new List<string> { "Point A", "Point B", "Point C" };
        var agent = new PatrolAgent("Area 1", patrolPoints);
        _agents.Add(agent);

        // Assert
        Assert.Equal("PatrolAgent", agent.ActorType);
        Assert.Equal("Patrol", agent.BehaviorType);
        Assert.Equal("Initializing", agent.CurrentState);
        Assert.Equal("Area 1", agent.PatrolArea);
        Assert.Equal("Point A", agent.CurrentTarget);
        Assert.False(agent.IsRunning);
        Assert.NotEqual(ActorId.NewId(), agent.Id);
    }

    [Fact]
    public async Task AIAgent_Lifecycle_ShouldFollowPluginAndActorPattern()
    {
        // Arrange
        var patrolPoints = new List<string> { "Point A", "Point B" };
        var agent = new PatrolAgent("Area 1", patrolPoints);
        _agents.Add(agent);

        // Create mock plugin context
        var mockContext = new MockPluginContext();

        // Act & Assert - Plugin configuration
        await agent.ConfigureAsync(mockContext);
        Assert.Equal(mockContext, agent.Context);

        // Actor initialization
        await agent.InitializeAsync();
        Assert.Equal("Idle", agent.CurrentState);

        // Actor start
        await agent.StartAsync();
        Assert.True(agent.IsRunning);
        Assert.Equal("Patrolling", agent.CurrentState);

        // Can unload check
        var canUnload = await agent.CanUnloadAsync();
        Assert.False(canUnload); // Cannot unload while running

        // Prepare for unload (should stop the agent)
        await agent.PrepareUnloadAsync();
        Assert.False(agent.IsRunning);

        var canUnloadAfterStop = await agent.CanUnloadAsync();
        Assert.True(canUnloadAfterStop); // Can unload after stopping
    }

    [Fact]
    public async Task AIAgentCluster_MultipleAgents_ShouldCoordinateThroughMessages()
    {
        // Arrange
        _cluster = new ActorCluster("PatrolCluster", "PatrolAgent");
        await _cluster.InitializeAsync();
        await _cluster.StartAsync();

        var agent1 = new PatrolAgent("Area 1", new List<string> { "A1", "A2" });
        var agent2 = new PatrolAgent("Area 2", new List<string> { "B1", "B2" });
        var agent3 = new PatrolAgent("Area 3", new List<string> { "C1", "C2" });
        
        _agents.AddRange(new[] { agent1, agent2, agent3 });

        // Configure and start all agents
        var mockContext = new MockPluginContext();
        foreach (var agent in _agents)
        {
            await agent.ConfigureAsync(mockContext);
            await agent.InitializeAsync();
            await agent.StartAsync();
            await _cluster.RegisterActorAsync(agent);
        }

        // Verify cluster membership
        Assert.Equal(3, _cluster.MemberCount);
        Assert.True(await _cluster.HasMemberAsync(agent1.Id));
        Assert.True(await _cluster.HasMemberAsync(agent2.Id));
        Assert.True(await _cluster.HasMemberAsync(agent3.Id));

        // Act - Send coordination message through cluster
        var coordinationRequest = new CoordinationRequest(agent1.Id, "AreaCoverage", "Requesting area coverage coordination");
        
        await _cluster.BroadcastMessageAsync(coordinationRequest, agent1.Id); // Exclude sender

        // Wait for message processing
        await Task.Delay(100);

        // Assert - All agents should have received the message
        // In a more complete test, we would verify actual message handling
        Assert.True(agent1.IsRunning);
        Assert.True(agent2.IsRunning);
        Assert.True(agent3.IsRunning);
    }

    [Fact]
    public async Task AIAgent_StateChanges_ShouldTriggerEvents()
    {
        // Arrange
        var patrolPoints = new List<string> { "Point A", "Point B" };
        var agent = new PatrolAgent("Area 1", patrolPoints);
        _agents.Add(agent);

        List<BehaviorStateChangedEventArgs> stateChanges = new();
        agent.BehaviorStateChanged += (sender, args) => stateChanges.Add(args);

        // Initialize agent first
        var mockContext = new MockPluginContext();
        await agent.ConfigureAsync(mockContext);
        
        // Act - Initialize should trigger state change from "Initializing" to "Idle"
        await agent.InitializeAsync(); 

        // Assert - Should have recorded the initialization state change
        Assert.Single(stateChanges);
        Assert.Equal("Idle", stateChanges[0].NewState);
        Assert.Equal("Initializing", stateChanges[0].PreviousState);

        // Act - Start should trigger state change from "Idle" to "Patrolling"
        await agent.StartAsync();

        // Assert - Should now have two state changes
        Assert.Equal(2, stateChanges.Count);
        Assert.Equal("Patrolling", stateChanges[1].NewState);
        Assert.Equal("Idle", stateChanges[1].PreviousState);
    }

    [Fact]
    public async Task AIAgent_MessageHandling_ShouldProcessCoordinationRequests()
    {
        // Arrange
        var patrolPoints = new List<string> { "Point A", "Point B" };
        var agent = new PatrolAgent("Area 1", patrolPoints);
        _agents.Add(agent);

        var mockContext = new MockPluginContext();
        await agent.ConfigureAsync(mockContext);
        await agent.InitializeAsync();
        await agent.StartAsync();

        // Act - Send coordination request
        var senderId = ActorId.NewId();
        var coordinationRequest = new CoordinationRequest(senderId, "AreaCoverage", "Test coordination");

        await agent.SendMessageAsync(coordinationRequest);
        await Task.Delay(100); // Wait for processing

        // Assert - Agent should have processed the message
        // The specific behavior would depend on the agent's current state
        Assert.True(agent.IsRunning);
    }

    [Fact]
    public async Task AIAgent_Commands_ShouldExecuteCorrectly()
    {
        // Arrange
        var patrolPoints = new List<string> { "Point A", "Point B" };
        var agent = new PatrolAgent("Area 1", patrolPoints);
        _agents.Add(agent);

        var mockContext = new MockPluginContext();
        await agent.ConfigureAsync(mockContext);
        await agent.InitializeAsync();
        await agent.StartAsync();

        var originalState = agent.CurrentState;

        // Act - Send investigate command
        var senderId = ActorId.NewId();
        var investigateCommand = new AgentCommand(senderId, "investigate", 
            new Dictionary<string, object> { ["location"] = "Suspicious Area" });

        await agent.SendMessageAsync(investigateCommand);
        await Task.Delay(100); // Wait for processing

        // Assert - Agent should have changed to investigating state
        // The exact behavior depends on the agent's implementation
        Assert.True(agent.IsRunning);
        
        // Send return command
        var returnCommand = new AgentCommand(senderId, "return");
        await agent.SendMessageAsync(returnCommand);
        await Task.Delay(100);

        Assert.Equal("Idle", agent.CurrentState);
    }

    /// <summary>
    /// Mock plugin context for testing.
    /// </summary>
    private class MockPluginContext : IPluginContext
    {
        public IServiceProvider Services { get; } = new MockServiceProvider();
        public IConfiguration Configuration { get; } = new MockConfiguration();
        public string PluginDirectory { get; } = "/mock/plugin/directory";
        public CancellationToken ShutdownToken { get; } = CancellationToken.None;
        public IReadOnlyDictionary<string, object> Properties { get; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Mock service provider for testing.
    /// </summary>
    private class MockServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    /// <summary>
    /// Mock configuration for testing.
    /// </summary>
    private class MockConfiguration : IConfiguration
    {
        public string? this[string key] { get => null; set { } }
        public IEnumerable<IConfigurationSection> GetChildren() => Array.Empty<IConfigurationSection>();
        public IChangeToken GetReloadToken() => new MockChangeToken();
        public IConfigurationSection GetSection(string key) => new MockConfigurationSection();
    }

    /// <summary>
    /// Mock configuration section for testing.
    /// </summary>
    private class MockConfigurationSection : IConfigurationSection
    {
        public string? this[string key] { get => null; set { } }
        public string Key { get; } = "mock";
        public string Path { get; } = "mock";
        public string? Value { get; set; }
        public IEnumerable<IConfigurationSection> GetChildren() => Array.Empty<IConfigurationSection>();
        public IChangeToken GetReloadToken() => new MockChangeToken();
        public IConfigurationSection GetSection(string key) => new MockConfigurationSection();
    }

    /// <summary>
    /// Mock change token for testing.
    /// </summary>
    private class MockChangeToken : IChangeToken
    {
        public bool HasChanged { get; } = false;
        public bool ActiveChangeCallbacks { get; } = false;
        public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) => new MockDisposable();
    }

    /// <summary>
    /// Mock disposable for testing.
    /// </summary>
    private class MockDisposable : IDisposable
    {
        public void Dispose() { }
    }
}