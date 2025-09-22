using GameConsole.AI.Core;
using Xunit;

namespace GameConsole.AI.Core.Tests;

/// <summary>
/// Tests for the AIAgentAttribute class functionality.
/// </summary>
public class AIAgentAttributeTests
{
    [Fact]
    public void AIAgentAttribute_Should_Initialize_With_Required_Parameters()
    {
        // Arrange
        const string id = "test.agent";
        const string name = "Test Agent";
        const string version = "1.0.0";
        const string description = "A test AI agent";
        const string author = "Test Team";
        const string agentType = "test";

        // Act
        var attribute = new AIAgentAttribute(id, name, version, description, author, agentType);

        // Assert
        Assert.Equal(id, attribute.Id);
        Assert.Equal(name, attribute.Name);
        Assert.Equal(version, attribute.Version);
        Assert.Equal(description, attribute.Description);
        Assert.Equal(author, attribute.Author);
        Assert.Equal(agentType, attribute.AgentType);
    }

    [Fact]
    public void AIAgentAttribute_Should_Have_Default_Values()
    {
        // Arrange
        var attribute = new AIAgentAttribute("test.agent", "Test Agent", "1.0.0", "Test", "Test Team", "test");

        // Act & Assert
        Assert.Empty(attribute.Capabilities);
        Assert.Empty(attribute.SupportedProtocols);
        Assert.False(attribute.SupportsLearning);
        Assert.Equal(64, attribute.MinMemoryMB);
        Assert.Equal(256, attribute.RecommendedMemoryMB);
        Assert.False(attribute.RequiresGPU);
        Assert.False(attribute.RequiresNetwork);
        Assert.Equal(1, attribute.MaxConcurrentInstances);
        Assert.True(attribute.AllowMultipleInstances);
    }

    [Fact]
    public void AIAgentAttribute_Should_Allow_Setting_AI_Specific_Properties()
    {
        // Arrange
        var attribute = new AIAgentAttribute("test.agent", "Test Agent", "1.0.0", "Test", "Test Team", "test")
        {
            Capabilities = new[] { "chat", "analysis" },
            SupportedProtocols = new[] { "http", "websocket" },
            SupportsLearning = true,
            MinMemoryMB = 128,
            RecommendedMemoryMB = 512,
            RequiresGPU = true,
            RequiresNetwork = true,
            MaxConcurrentInstances = 5,
            AllowMultipleInstances = false
        };

        // Act & Assert
        Assert.Equal(new[] { "chat", "analysis" }, attribute.Capabilities);
        Assert.Equal(new[] { "http", "websocket" }, attribute.SupportedProtocols);
        Assert.True(attribute.SupportsLearning);
        Assert.Equal(128, attribute.MinMemoryMB);
        Assert.Equal(512, attribute.RecommendedMemoryMB);
        Assert.True(attribute.RequiresGPU);
        Assert.True(attribute.RequiresNetwork);
        Assert.Equal(5, attribute.MaxConcurrentInstances);
        Assert.False(attribute.AllowMultipleInstances);
    }

    [Fact]
    public void AIAgentAttribute_Should_Throw_When_AgentType_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new AIAgentAttribute("test.agent", "Test Agent", "1.0.0", "Test", "Test Team", null!));
    }
}