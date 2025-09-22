using System.Reflection;
using GameConsole.AI.Core;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.AI.Core.Tests;

/// <summary>
/// Tests for the AIAgentDiscovery service (RFC-007-02).
/// </summary>
public class AIAgentDiscoveryTests
{
    private readonly AIAgentDiscovery _discovery;
    private readonly Assembly _testAssembly;

    public AIAgentDiscoveryTests()
    {
        var logger = new TestLogger<AIAgentDiscovery>();
        _discovery = new AIAgentDiscovery(logger);
        _testAssembly = Assembly.GetExecutingAssembly();
    }

    [Fact]
    public async Task InitializeAsync_Should_Complete_Successfully()
    {
        // Act
        await _discovery.InitializeAsync();

        // Assert
        // No specific assertion needed - should complete without throwing
    }

    [Fact]
    public async Task StartAsync_Should_Complete_After_Initialize()
    {
        // Arrange
        await _discovery.InitializeAsync();

        // Act
        await _discovery.StartAsync();

        // Assert
        // No specific assertion needed - should complete without throwing
    }

    [Fact]
    public void DiscoverAgents_Should_Find_Valid_AI_Agents_In_Assembly()
    {
        // Act
        var discoveredAgents = _discovery.DiscoverAgents(_testAssembly).ToList();

        // Assert
        Assert.NotEmpty(discoveredAgents);
        
        // Should find our sample agents
        var pathFindingAgent = discoveredAgents.FirstOrDefault(a => a.Id == "pathfinder-basic");
        Assert.NotNull(pathFindingAgent);
        Assert.Equal("Basic PathFinder", pathFindingAgent.Name);
        Assert.Equal(AIAgentCapability.PathFinding, pathFindingAgent.Capabilities);
        Assert.Equal(typeof(SamplePathFindingAgent), pathFindingAgent.AgentType);

        var decisionMakingAgent = discoveredAgents.FirstOrDefault(a => a.Id == "decision-basic");
        Assert.NotNull(decisionMakingAgent);
        Assert.Equal("Basic Decision Maker", decisionMakingAgent.Name);
        Assert.Equal(AIAgentCapability.DecisionMaking | AIAgentCapability.Combat, decisionMakingAgent.Capabilities);
        Assert.Equal(typeof(SampleDecisionMakingAgent), decisionMakingAgent.AgentType);
    }

    [Fact]
    public void DiscoverAgents_Should_Skip_Invalid_Agents()
    {
        // Act
        var discoveredAgents = _discovery.DiscoverAgents(_testAssembly).ToList();

        // Assert
        // Should not find invalid agents
        Assert.DoesNotContain(discoveredAgents, a => a.AgentType == typeof(InvalidAgentMissingAttribute));
        Assert.DoesNotContain(discoveredAgents, a => a.AgentType == typeof(InvalidAbstractAgent));
    }

    [Fact]
    public void DiscoverAgents_Multiple_Assemblies_Should_Return_Combined_Results()
    {
        // Arrange
        var assemblies = new[] { _testAssembly };

        // Act
        var discoveredAgents = _discovery.DiscoverAgents(assemblies).ToList();

        // Assert
        Assert.NotEmpty(discoveredAgents);
        
        // Should find both sample agents
        Assert.Contains(discoveredAgents, a => a.Id == "pathfinder-basic");
        Assert.Contains(discoveredAgents, a => a.Id == "decision-basic");
    }

    [Fact]
    public void DiscoverAllAgents_Should_Find_Agents_In_Current_Domain()
    {
        // Act
        var discoveredAgents = _discovery.DiscoverAllAgents().ToList();

        // Assert
        Assert.NotEmpty(discoveredAgents);
        
        // Should find our test agents since they're in the current domain
        Assert.Contains(discoveredAgents, a => a.Id == "pathfinder-basic");
        Assert.Contains(discoveredAgents, a => a.Id == "decision-basic");
    }

    [Fact]
    public void ValidateAgentType_Should_Return_True_For_Valid_Agent()
    {
        // Act & Assert
        Assert.True(_discovery.ValidateAgentType(typeof(SamplePathFindingAgent)));
        Assert.True(_discovery.ValidateAgentType(typeof(SampleDecisionMakingAgent)));
    }

    [Fact]
    public void ValidateAgentType_Should_Return_False_For_Invalid_Agent()
    {
        // Act & Assert
        Assert.False(_discovery.ValidateAgentType(typeof(InvalidAgentMissingAttribute)));
        Assert.False(_discovery.ValidateAgentType(typeof(InvalidAbstractAgent)));
        Assert.False(_discovery.ValidateAgentType(typeof(string))); // Non-IAIAgent type
    }

    [Fact]
    public void ValidateAgentTypeDetailed_Should_Provide_Detailed_Validation_Info()
    {
        // Act
        var validResult = _discovery.ValidateAgentTypeDetailed(typeof(SamplePathFindingAgent));
        var invalidResult = _discovery.ValidateAgentTypeDetailed(typeof(InvalidAgentMissingAttribute));
        var abstractResult = _discovery.ValidateAgentTypeDetailed(typeof(InvalidAbstractAgent));

        // Assert
        Assert.True(validResult.IsValid);
        Assert.Empty(validResult.Errors);

        Assert.False(invalidResult.IsValid);
        Assert.Contains(invalidResult.Errors, e => e.Contains("does not have AIAgentAttribute"));

        Assert.False(abstractResult.IsValid);
        Assert.Contains(abstractResult.Errors, e => e.Contains("must be a concrete class"));
    }

    [Fact]
    public void ValidateAgentType_Should_Handle_Null_Type()
    {
        // Act
        var result = _discovery.ValidateAgentTypeDetailed(null!);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("cannot be null"));
    }

    [Fact]
    public void DiscoverAgents_Should_Handle_Null_Assembly()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _discovery.DiscoverAgents((Assembly)null!));
    }

    [Fact]
    public void DiscoverAgents_Should_Handle_Null_Assembly_Collection()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _discovery.DiscoverAgents((IEnumerable<Assembly>)null!));
    }

    [Fact]
    public async Task DisposeAsync_Should_Complete_Successfully()
    {
        // Arrange
        await _discovery.InitializeAsync();
        await _discovery.StartAsync();

        // Act
        await _discovery.DisposeAsync();

        // Assert
        // No specific assertion needed - should complete without throwing
    }
}

/// <summary>
/// Simple test logger for unit tests.
/// </summary>
public class TestLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}