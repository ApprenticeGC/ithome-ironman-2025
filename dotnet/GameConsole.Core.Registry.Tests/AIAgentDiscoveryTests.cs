using GameConsole.Core.Abstractions;
using GameConsole.Core.Registry;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.Core.Registry.Tests;

/// <summary>
/// Tests for the AIAgentDiscovery class.
/// </summary>
public class AIAgentDiscoveryTests : IAsyncDisposable
{
    private readonly AIAgentRegistry _registry;
    private readonly AIAgentDiscovery _discovery;
    private readonly ILogger<AIAgentRegistry> _registryLogger;
    private readonly ILogger<AIAgentDiscovery> _discoveryLogger;

    public AIAgentDiscoveryTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Debug));
        _registryLogger = loggerFactory.CreateLogger<AIAgentRegistry>();
        _discoveryLogger = loggerFactory.CreateLogger<AIAgentDiscovery>();
        
        _registry = new AIAgentRegistry(_registryLogger);
        _discovery = new AIAgentDiscovery(_registry, _discoveryLogger);
    }

    [Fact]
    public async Task InitializeAsync_Should_Complete_Without_Error()
    {
        // Act & Assert
        await _discovery.InitializeAsync();
        Assert.False(_discovery.IsRunning);
    }

    [Fact]
    public async Task StartAsync_Should_Set_IsRunning_True()
    {
        // Arrange
        await _discovery.InitializeAsync();

        // Act
        await _discovery.StartAsync();

        // Assert
        Assert.True(_discovery.IsRunning);
    }

    [Fact]
    public async Task DiscoverAllAsync_Should_Return_All_Registered_Agents()
    {
        // Arrange
        await _registry.InitializeAsync();
        await _registry.StartAsync();
        await _discovery.InitializeAsync();
        await _discovery.StartAsync();

        var agent1 = new TestAIAgent("test-1", "Agent 1", categories: ["category1"]);
        var agent2 = new TestAIAgent("test-2", "Agent 2", categories: ["category2"]);
        
        await _registry.RegisterAsync(agent1);
        await _registry.RegisterAsync(agent2);

        // Act
        var discoveredAgents = await _discovery.DiscoverAllAsync();

        // Assert
        Assert.Equal(2, discoveredAgents.Count());
        Assert.Contains(discoveredAgents, a => a.Id == agent1.Id);
        Assert.Contains(discoveredAgents, a => a.Id == agent2.Id);
    }

    [Fact]
    public async Task DiscoverByCategoryAsync_Should_Return_Matching_Agents()
    {
        // Arrange
        await _registry.InitializeAsync();
        await _registry.StartAsync();
        await _discovery.InitializeAsync();
        await _discovery.StartAsync();

        var agent1 = new TestAIAgent("test-1", "Agent 1", categories: ["AI", "Vision"]);
        var agent2 = new TestAIAgent("test-2", "Agent 2", categories: ["AI", "Language"]);
        var agent3 = new TestAIAgent("test-3", "Agent 3", categories: ["Utility"]);
        
        await _registry.RegisterAsync(agent1);
        await _registry.RegisterAsync(agent2);
        await _registry.RegisterAsync(agent3);

        // Act
        var aiAgents = await _discovery.DiscoverByCategoryAsync(["AI"]);
        var utilityAgents = await _discovery.DiscoverByCategoryAsync(["Utility"]);

        // Assert
        Assert.Equal(2, aiAgents.Count());
        Assert.Contains(aiAgents, a => a.Id == agent1.Id);
        Assert.Contains(aiAgents, a => a.Id == agent2.Id);

        Assert.Single(utilityAgents);
        Assert.Contains(utilityAgents, a => a.Id == agent3.Id);
    }

    [Fact]
    public async Task FindByIdAsync_Should_Return_Correct_Agent()
    {
        // Arrange
        await _registry.InitializeAsync();
        await _registry.StartAsync();
        await _discovery.InitializeAsync();
        await _discovery.StartAsync();

        var agent1 = new TestAIAgent("test-1", "Agent 1");
        var agent2 = new TestAIAgent("test-2", "Agent 2");
        
        await _registry.RegisterAsync(agent1);
        await _registry.RegisterAsync(agent2);

        // Act
        var found = await _discovery.FindByIdAsync("test-1");
        var notFound = await _discovery.FindByIdAsync("test-3");

        // Assert
        Assert.NotNull(found);
        Assert.Equal("test-1", found.Id);
        Assert.Equal("Agent 1", found.Name);
        
        Assert.Null(notFound);
    }

    [Fact]
    public async Task FindByNameAsync_Should_Support_Exact_And_Partial_Matching()
    {
        // Arrange
        await _registry.InitializeAsync();
        await _registry.StartAsync();
        await _discovery.InitializeAsync();
        await _discovery.StartAsync();

        var agent1 = new TestAIAgent("test-1", "Data Processing Agent");
        var agent2 = new TestAIAgent("test-2", "Image Processing Agent");
        var agent3 = new TestAIAgent("test-3", "Voice Assistant");
        
        await _registry.RegisterAsync(agent1);
        await _registry.RegisterAsync(agent2);
        await _registry.RegisterAsync(agent3);

        // Act
        var exactMatch = await _discovery.FindByNameAsync("Voice Assistant", exactMatch: true);
        var partialMatch = await _discovery.FindByNameAsync("Processing", exactMatch: false);

        // Assert
        Assert.Single(exactMatch);
        Assert.Equal("test-3", exactMatch.First().Id);

        Assert.Equal(2, partialMatch.Count());
        Assert.Contains(partialMatch, a => a.Id == agent1.Id);
        Assert.Contains(partialMatch, a => a.Id == agent2.Id);
    }

    [Fact]
    public async Task DiscoverAsync_With_Criteria_Should_Filter_Correctly()
    {
        // Arrange
        await _registry.InitializeAsync();
        await _registry.StartAsync();
        await _discovery.InitializeAsync();
        await _discovery.StartAsync();

        var agent1 = new TestAIAgent("test-1", "AI Agent Alpha", categories: ["AI", "Processing"], version: "1.0.0");
        var agent2 = new TestAIAgent("test-2", "AI Agent Beta", categories: ["AI", "Vision"], version: "2.0.0");
        var agent3 = new TestAIAgent("test-3", "Utility Tool", categories: ["Utility"], version: "1.0.0");
        
        await _registry.RegisterAsync(agent1);
        await _registry.RegisterAsync(agent2);
        await _registry.RegisterAsync(agent3);

        // Act
        var aiCriteria = AIAgentDiscoveryCriteria.WithCategories("AI");
        var versionCriteria = new AIAgentDiscoveryCriteria { VersionFilter = "1.0.0" };
        var nameCriteria = new AIAgentDiscoveryCriteria { NameFilter = "Alpha" };

        var aiAgents = await _discovery.DiscoverAsync(aiCriteria);
        var v1Agents = await _discovery.DiscoverAsync(versionCriteria);
        var alphaAgents = await _discovery.DiscoverAsync(nameCriteria);

        // Assert
        Assert.Equal(2, aiAgents.Count());
        Assert.Contains(aiAgents, a => a.Id == agent1.Id);
        Assert.Contains(aiAgents, a => a.Id == agent2.Id);

        Assert.Equal(2, v1Agents.Count());
        Assert.Contains(v1Agents, a => a.Id == agent1.Id);
        Assert.Contains(v1Agents, a => a.Id == agent3.Id);

        Assert.Single(alphaAgents);
        Assert.Contains(alphaAgents, a => a.Id == agent1.Id);
    }

    [Fact]
    public async Task DiscoverByCapabilityAsync_Should_Return_Agents_With_Capability()
    {
        // Arrange
        await _registry.InitializeAsync();
        await _registry.StartAsync();
        await _discovery.InitializeAsync();
        await _discovery.StartAsync();

        var agentWithCapability = new TestAIAgentWithCapability("test-1", "Agent with capability");
        var agentWithoutCapability = new TestAIAgent("test-2", "Agent without capability");
        
        await _registry.RegisterAsync(agentWithCapability);
        await _registry.RegisterAsync(agentWithoutCapability);

        // Act
        var agentsWithCapability = await _discovery.DiscoverByCapabilityAsync<ITestCapability>();

        // Assert
        Assert.Single(agentsWithCapability);
        Assert.Equal("test-1", agentsWithCapability.First().Id);
    }

    public async ValueTask DisposeAsync()
    {
        if (_discovery != null)
        {
            await _discovery.DisposeAsync();
        }
        if (_registry != null)
        {
            await _registry.DisposeAsync();
        }
    }
}