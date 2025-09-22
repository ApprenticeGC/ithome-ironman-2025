using GameConsole.AI.Core;
using GameConsole.Plugins.Core;
using Microsoft.Extensions.Logging;
using Moq;

namespace GameConsole.AI.Core.Tests;

/// <summary>
/// Tests for the AIAgentAttribute class and related functionality.
/// </summary>
public class AIAgentAttributeTests
{
    [Fact]
    public void AIAgentAttribute_Constructor_SetsProperties()
    {
        // Arrange
        const string name = "TestAgent";
        const string version = "1.0.0";
        const string description = "Test AI agent";

        // Act
        var attribute = new AIAgentAttribute(name, version, description);

        // Assert
        Assert.Equal(name, attribute.Name);
        Assert.Equal(version, attribute.Version);
        Assert.Equal(description, attribute.Description);
        Assert.Empty(attribute.Categories);
        Assert.Empty(attribute.DecisionTypes);
        Assert.False(attribute.SupportsLearning);
        Assert.False(attribute.SupportsAutonomousMode);
        Assert.Equal(50, attribute.Priority);
        Assert.Equal(1, attribute.MaxConcurrentInputs);
    }

    [Fact]
    public void AIAgentAttribute_Constructor_WithNullName_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AIAgentAttribute(null!, "1.0.0", "Description"));
    }

    [Fact]
    public void AIAgentAttribute_Constructor_WithNullVersion_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AIAgentAttribute("Name", null!, "Description"));
    }

    [Fact]
    public void AIAgentAttribute_Constructor_WithNullDescription_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AIAgentAttribute("Name", "1.0.0", null!));
    }

    [Fact]
    public void AIAgentAttribute_Properties_CanBeSet()
    {
        // Arrange
        var attribute = new AIAgentAttribute("TestAgent", "1.0.0", "Test description")
        {
            Categories = new[] { "Combat", "Strategy" },
            DecisionTypes = new[] { "pathfinding", "combat" },
            SupportsLearning = true,
            SupportsAutonomousMode = true,
            Priority = 75,
            MaxConcurrentInputs = 5
        };

        // Assert
        Assert.Equal(new[] { "Combat", "Strategy" }, attribute.Categories);
        Assert.Equal(new[] { "pathfinding", "combat" }, attribute.DecisionTypes);
        Assert.True(attribute.SupportsLearning);
        Assert.True(attribute.SupportsAutonomousMode);
        Assert.Equal(75, attribute.Priority);
        Assert.Equal(5, attribute.MaxConcurrentInputs);
    }

    [Fact]
    public void GetAIAgentAttribute_WithAttributePresent_ReturnsAttribute()
    {
        // Arrange
        var type = typeof(TestAIAgentWithAttribute);

        // Act
        var attribute = type.GetAIAgentAttribute();

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal("TestAgent", attribute.Name);
        Assert.Equal("1.0.0", attribute.Version);
        Assert.Equal("Test AI agent for unit testing", attribute.Description);
    }

    [Fact]
    public void GetAIAgentAttribute_WithoutAttribute_ReturnsNull()
    {
        // Arrange
        var type = typeof(TestAIAgentWithoutAttribute);

        // Act
        var attribute = type.GetAIAgentAttribute();

        // Assert
        Assert.Null(attribute);
    }

    [Fact]
    public void HasAIAgentAttribute_WithAttributePresent_ReturnsTrue()
    {
        // Arrange
        var type = typeof(TestAIAgentWithAttribute);

        // Act
        var hasAttribute = type.HasAIAgentAttribute();

        // Assert
        Assert.True(hasAttribute);
    }

    [Fact]
    public void HasAIAgentAttribute_WithoutAttribute_ReturnsFalse()
    {
        // Arrange
        var type = typeof(TestAIAgentWithoutAttribute);

        // Act
        var hasAttribute = type.HasAIAgentAttribute();

        // Assert
        Assert.False(hasAttribute);
    }

    [Fact]
    public void ToMetadata_ConvertsAttributeCorrectly()
    {
        // Arrange
        var attribute = new AIAgentAttribute("TestAgent", "1.0.0", "Test description")
        {
            Categories = new[] { "Combat" },
            DecisionTypes = new[] { "pathfinding" },
            SupportsLearning = true,
            SupportsAutonomousMode = true,
            Priority = 75,
            MaxConcurrentInputs = 5
        };

        // Act
        var metadata = attribute.ToMetadata();

        // Assert
        Assert.Equal("TestAgent", metadata.Name);
        Assert.Equal("1.0.0", metadata.Version);
        Assert.Equal("Test description", metadata.Description);
        Assert.Equal(new[] { "Combat" }, metadata.Categories);
        
        Assert.Equal(new[] { "pathfinding" }, metadata.Properties["DecisionTypes"]);
        Assert.Equal(true, metadata.Properties["SupportsLearning"]);
        Assert.Equal(true, metadata.Properties["SupportsAutonomousMode"]);
        Assert.Equal(75, metadata.Properties["Priority"]);
        Assert.Equal(5, metadata.Properties["MaxConcurrentInputs"]);
    }
}

/// <summary>
/// Test AI agent with attribute for testing.
/// </summary>
[AIAgent("TestAgent", "1.0.0", "Test AI agent for unit testing")]
internal class TestAIAgentWithAttribute : BaseAIAgent
{
    public override IPluginMetadata Metadata { get; } = Mock.Of<IPluginMetadata>();

    public override IAIAgentCapabilities Capabilities { get; } = Mock.Of<IAIAgentCapabilities>();

    public TestAIAgentWithAttribute(ILogger<BaseAIAgent> logger) : base(logger) { }

    protected override Task<IAIAgentResponse> ProcessInputAsync(IAIAgentInput input, CancellationToken cancellationToken)
    {
        return Task.FromResult<IAIAgentResponse>(new AIAgentResponse 
        { 
            Success = true, 
            ResponseType = "test", 
            Data = "test response" 
        });
    }
}

/// <summary>
/// Test AI agent without attribute for testing.
/// </summary>
internal class TestAIAgentWithoutAttribute : BaseAIAgent
{
    public override IPluginMetadata Metadata { get; } = Mock.Of<IPluginMetadata>();

    public override IAIAgentCapabilities Capabilities { get; } = Mock.Of<IAIAgentCapabilities>();

    public TestAIAgentWithoutAttribute(ILogger<BaseAIAgent> logger) : base(logger) { }

    protected override Task<IAIAgentResponse> ProcessInputAsync(IAIAgentInput input, CancellationToken cancellationToken)
    {
        return Task.FromResult<IAIAgentResponse>(new AIAgentResponse 
        { 
            Success = true, 
            ResponseType = "test", 
            Data = "test response" 
        });
    }
}