using GameConsole.AI.Discovery;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameConsole.AI.Discovery.Tests;

public class CapabilityMatcherTests
{
    private readonly CapabilityMatcher _matcher;
    private readonly AgentMetadata _testAgent1;
    private readonly AgentMetadata _testAgent2;

    public CapabilityMatcherTests()
    {
        _matcher = new CapabilityMatcher(NullLogger<CapabilityMatcher>.Instance);
        
        _testAgent1 = new AgentMetadata
        {
            Id = "agent1",
            Name = "Test Agent 1",
            AgentType = typeof(TestAIAgent),
            Capabilities = new[] { typeof(ITestCapability) }.AsReadOnly(),
            Tags = new[] { "test", "basic" }.AsReadOnly(),
            Priority = 5,
            IsAvailable = true,
            ResourceRequirements = new AgentResourceRequirements
            {
                MinMemoryBytes = 1024 * 1024, // 1MB
                RequiredCpuCores = 1
            }
        };

        _testAgent2 = new AgentMetadata
        {
            Id = "agent2",
            Name = "Test Agent 2",
            AgentType = typeof(BasicTestAgent),
            Capabilities = Array.Empty<Type>().AsReadOnly(),
            Tags = new[] { "advanced" }.AsReadOnly(),
            Priority = 3,
            IsAvailable = false,
            ResourceRequirements = new AgentResourceRequirements
            {
                MinMemoryBytes = 2 * 1024 * 1024, // 2MB
                RequiredCpuCores = 2
            }
        };
    }

    [Fact]
    public void MeetsMinimumRequirements_WithMatchingRequirements_ReturnsTrue()
    {
        // Arrange
        var requirements = new TaskRequirements
        {
            RequiredCapabilities = new[] { typeof(ITestCapability) }.AsReadOnly(),
            RequiredTags = new[] { "test" }.AsReadOnly(),
            MinimumPriority = 3,
            OnlyAvailableAgents = true
        };

        // Act
        var result = _matcher.MeetsMinimumRequirements(_testAgent1, requirements);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void MeetsMinimumRequirements_WithMissingCapability_ReturnsFalse()
    {
        // Arrange
        var requirements = new TaskRequirements
        {
            RequiredCapabilities = new[] { typeof(ITestCapability) }.AsReadOnly()
        };

        // Act
        var result = _matcher.MeetsMinimumRequirements(_testAgent2, requirements);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void MeetsMinimumRequirements_WithMissingTag_ReturnsFalse()
    {
        // Arrange
        var requirements = new TaskRequirements
        {
            RequiredTags = new[] { "test" }.AsReadOnly()
        };

        // Act
        var result = _matcher.MeetsMinimumRequirements(_testAgent2, requirements);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void MeetsMinimumRequirements_WithLowPriority_ReturnsFalse()
    {
        // Arrange
        var requirements = new TaskRequirements
        {
            MinimumPriority = 10
        };

        // Act
        var result = _matcher.MeetsMinimumRequirements(_testAgent1, requirements);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void MeetsMinimumRequirements_WithUnavailableAgent_ReturnsFalse()
    {
        // Arrange
        var requirements = new TaskRequirements
        {
            OnlyAvailableAgents = true
        };

        // Act
        var result = _matcher.MeetsMinimumRequirements(_testAgent2, requirements);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CalculateCompatibilityScore_WithPerfectMatch_ReturnsHighScore()
    {
        // Arrange
        var requirements = new TaskRequirements
        {
            RequiredCapabilities = new[] { typeof(ITestCapability) }.AsReadOnly(),
            PreferredCapabilities = Array.Empty<Type>().AsReadOnly(),
            RequiredTags = new[] { "test" }.AsReadOnly(),
            PreferredTags = new[] { "basic" }.AsReadOnly(),
            MinimumPriority = 3,
            OnlyAvailableAgents = true
        };

        // Act
        var score = _matcher.CalculateCompatibilityScore(_testAgent1, requirements);

        // Assert
        Assert.True(score > 0.8); // High compatibility score
    }

    [Fact]
    public void CalculateCompatibilityScore_WithNoMatch_ReturnsLowScore()
    {
        // Arrange
        var requirements = new TaskRequirements
        {
            RequiredCapabilities = new[] { typeof(ITestCapability) }.AsReadOnly(),
            RequiredTags = new[] { "test" }.AsReadOnly(),
            MinimumPriority = 10,
            OnlyAvailableAgents = true
        };

        // Act
        var score = _matcher.CalculateCompatibilityScore(_testAgent2, requirements);

        // Assert
        Assert.True(score < 0.3); // Low compatibility score
    }

    [Fact]
    public async Task FindMatchingAgentsAsync_WithMultipleAgents_RanksCorrectly()
    {
        // Arrange
        var agents = new[] { _testAgent1, _testAgent2 };
        var requirements = new TaskRequirements
        {
            PreferredCapabilities = new[] { typeof(ITestCapability) }.AsReadOnly(),
            PreferredTags = new[] { "test" }.AsReadOnly(),
            OnlyAvailableAgents = false // Include unavailable agents for testing
        };

        // Act
        var matches = await _matcher.FindMatchingAgentsAsync(requirements, agents);

        // Assert
        Assert.Equal(2, matches.Count());
        
        var rankedMatches = matches.ToList();
        Assert.Equal("agent1", rankedMatches[0].Agent.Id); // Should rank higher
        Assert.Equal("agent2", rankedMatches[1].Agent.Id); // Should rank lower
        
        Assert.True(rankedMatches[0].Score > rankedMatches[1].Score);
    }

    [Fact]
    public async Task FindMatchingAgentsAsync_WithOnlyAvailableAgents_FiltersUnavailable()
    {
        // Arrange
        var agents = new[] { _testAgent1, _testAgent2 };
        var requirements = new TaskRequirements
        {
            OnlyAvailableAgents = true
        };

        // Act
        var matches = await _matcher.FindMatchingAgentsAsync(requirements, agents);

        // Assert
        Assert.Single(matches);
        Assert.Equal("agent1", matches.First().Agent.Id);
    }

    [Fact]
    public async Task FindMatchingAgentsAsync_WithNullRequirements_ThrowsException()
    {
        // Arrange
        var agents = new[] { _testAgent1 };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _matcher.FindMatchingAgentsAsync(null!, agents));
    }

    [Fact]
    public async Task FindMatchingAgentsAsync_WithNullAgents_ThrowsException()
    {
        // Arrange
        var requirements = new TaskRequirements();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _matcher.FindMatchingAgentsAsync(requirements, null!));
    }

    [Fact]
    public void CalculateCompatibilityScore_ResultsInValidRange()
    {
        // Arrange
        var requirements = new TaskRequirements();

        // Act
        var score1 = _matcher.CalculateCompatibilityScore(_testAgent1, requirements);
        var score2 = _matcher.CalculateCompatibilityScore(_testAgent2, requirements);

        // Assert
        Assert.InRange(score1, 0.0, 1.0);
        Assert.InRange(score2, 0.0, 1.0);
    }
}