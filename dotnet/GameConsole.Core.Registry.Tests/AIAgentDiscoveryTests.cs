using GameConsole.Core.Abstractions;
using GameConsole.Core.Registry;
using Xunit;

namespace GameConsole.Core.Registry.Tests;

/// <summary>
/// Tests for AI Agent Discovery functionality.
/// </summary>
public class AIAgentDiscoveryTests : IDisposable
{
    private readonly AIAgentDiscoveryService _discoveryService;

    public AIAgentDiscoveryTests()
    {
        _discoveryService = new AIAgentDiscoveryService();
    }

    [Fact]
    public async Task DiscoverAgentsAsync_Should_Find_Attributed_Agents()
    {
        // Act
        var agents = await _discoveryService.DiscoverAgentsAsync();

        // Assert
        Assert.NotNull(agents);
        var agentList = agents.ToList();
        
        // Should find our test AI agent
        var testAgent = agentList.FirstOrDefault(a => a.Metadata.Id == "test.ai.agent");
        Assert.NotNull(testAgent);
        Assert.Equal("Test AI Agent", testAgent.Metadata.Name);
        Assert.Equal("TestAuthor", testAgent.Metadata.Author);
    }

    [Fact]
    public async Task DiscoverAgentsByCapabilityAsync_Should_Filter_By_Capability()
    {
        // Act
        var agents = await _discoveryService.DiscoverAgentsByCapabilityAsync<ITestCapability>();

        // Assert
        Assert.NotNull(agents);
        var agentList = agents.ToList();
        
        // Should only find agents that provide ITestCapability
        Assert.All(agentList, agent => 
            Assert.Contains(typeof(ITestCapability), agent.Metadata.ProvidedCapabilities));
    }

    [Fact]
    public async Task DiscoverAgentsByTagsAsync_Should_Filter_By_Tags()
    {
        // Act
        var agents = await _discoveryService.DiscoverAgentsByTagsAsync(new[] { "test" });

        // Assert
        Assert.NotNull(agents);
        var agentList = agents.ToList();
        
        // Should find agents with "test" tag
        foreach (var agent in agentList)
        {
            Assert.True(agent.Metadata.Properties.TryGetValue("Tags", out var tagsValue));
            var tags = Assert.IsType<string[]>(tagsValue);
            Assert.Contains("test", tags, StringComparer.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task GetAgentDescriptorAsync_Should_Return_Agent_By_Id()
    {
        // Act
        var agent = await _discoveryService.GetAgentDescriptorAsync("test.ai.agent");

        // Assert
        Assert.NotNull(agent);
        Assert.Equal("test.ai.agent", agent.Metadata.Id);
        Assert.Equal("Test AI Agent", agent.Metadata.Name);
    }

    [Fact]
    public async Task ValidateAndOrderDependenciesAsync_Should_Order_By_Dependencies()
    {
        // Arrange
        var agents = new[]
        {
            CreateTestAgentDescriptor("agent.b", "Agent B", new[] { "agent.a" }),
            CreateTestAgentDescriptor("agent.a", "Agent A", Array.Empty<string>()),
            CreateTestAgentDescriptor("agent.c", "Agent C", new[] { "agent.a", "agent.b" })
        };

        // Act
        var ordered = await _discoveryService.ValidateAndOrderDependenciesAsync(agents);

        // Assert
        var orderedList = ordered.ToList();
        Assert.Equal(3, orderedList.Count);
        
        // Agent A should come first (no dependencies)
        Assert.Equal("agent.a", orderedList[0].Metadata.Id);
        
        // Agent B should come second (depends on A)
        Assert.Equal("agent.b", orderedList[1].Metadata.Id);
        
        // Agent C should come last (depends on A and B)
        Assert.Equal("agent.c", orderedList[2].Metadata.Id);
    }

    [Fact]
    public async Task ValidateAndOrderDependenciesAsync_Should_Detect_Circular_Dependencies()
    {
        // Arrange
        var agents = new[]
        {
            CreateTestAgentDescriptor("agent.a", "Agent A", new[] { "agent.b" }),
            CreateTestAgentDescriptor("agent.b", "Agent B", new[] { "agent.a" })
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _discoveryService.ValidateAndOrderDependenciesAsync(agents));
    }

    [Fact]
    public async Task ValidateAndOrderDependenciesAsync_Should_Mark_Unsatisfied_Dependencies()
    {
        // Arrange
        var agents = new[]
        {
            CreateTestAgentDescriptor("agent.a", "Agent A", new[] { "missing.agent" })
        };

        // Act
        var ordered = await _discoveryService.ValidateAndOrderDependenciesAsync(agents);

        // Assert
        var agent = ordered.First();
        Assert.False(agent.CanCreate);
        Assert.Contains("missing.agent", agent.UnsatisfiedDependencies);
    }

    private static AIAgentDescriptor CreateTestAgentDescriptor(string id, string name, string[] dependencies)
    {
        var metadata = new TestAIAgentMetadata(id, name, dependencies);
        return new AIAgentDescriptor(typeof(IAIAgent), typeof(TestAIAgent), metadata);
    }

    public void Dispose()
    {
        // No cleanup needed for discovery service
    }
}

// Test implementations

public interface ITestCapability
{
    Task<string> ProcessAsync(string input);
}

[AIAgent("test.ai.agent", "Test AI Agent", "1.0.0", "A test AI agent for unit testing", "TestAuthor",
    ProvidedCapabilities = new[] { "GameConsole.Core.Registry.Tests.ITestCapability" },
    Tags = new[] { "test", "unit-test" })]
public class TestAIAgent : IAIAgent
{
    public IAIAgentMetadata Metadata { get; }
    public bool IsRunning { get; private set; }
    public bool IsReady { get; private set; }

    public TestAIAgent()
    {
        var attribute = GetType().GetCustomAttribute(typeof(AIAgentAttribute), false) as AIAgentAttribute;
        Metadata = new AIAgentMetadataFromAttribute(attribute!, GetType());
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        IsRunning = true;
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        IsReady = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        IsReady = false;
        IsRunning = false;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Type>>(new[] { typeof(ITestCapability) });
    }

    public Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(typeof(T) == typeof(ITestCapability));
    }

    public Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (typeof(T) == typeof(ITestCapability))
            return Task.FromResult<T?>(new TestCapabilityImplementation() as T);
        return Task.FromResult<T?>(null);
    }

    public Task ConfigureAsync(IAIAgentConfiguration configuration, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<AIAgentHealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(AIAgentHealthStatus.Healthy());
    }
}

public class TestCapabilityImplementation : ITestCapability
{
    public Task<string> ProcessAsync(string input)
    {
        return Task.FromResult($"Processed: {input}");
    }
}

public class TestAIAgentMetadata : IAIAgentMetadata
{
    public TestAIAgentMetadata(string id, string name, string[] dependencies)
    {
        Id = id;
        Name = name;
        Version = new Version("1.0.0");
        Description = "Test agent";
        Author = "Test";
        Dependencies = dependencies.ToList().AsReadOnly();
        ProvidedCapabilities = new List<Type> { typeof(ITestCapability) }.AsReadOnly();
        RequiredCapabilities = new List<Type>().AsReadOnly();
        Properties = new Dictionary<string, object>
        {
            ["Tags"] = new[] { "test" }
        }.AsReadOnly();
    }

    public string Id { get; }
    public string Name { get; }
    public Version Version { get; }
    public string Description { get; }
    public string Author { get; }
    public IReadOnlyList<string> Dependencies { get; }
    public IReadOnlyList<Type> ProvidedCapabilities { get; }
    public IReadOnlyList<Type> RequiredCapabilities { get; }
    public IReadOnlyDictionary<string, object> Properties { get; }
}