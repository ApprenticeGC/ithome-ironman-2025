using GameConsole.Core.Abstractions;
using Xunit;

namespace GameConsole.Core.Abstractions.Tests;

/// <summary>
/// Tests for the AIAgentAttribute class.
/// </summary>
public class AIAgentAttributeTests
{
    [Fact]
    public void Constructor_Should_Initialize_Required_Properties()
    {
        // Arrange & Act
        var attribute = new AIAgentAttribute("test-agent", "TestBot", "Test Agent");

        // Assert
        Assert.Equal("test-agent", attribute.AgentId);
        Assert.Equal("TestBot", attribute.AgentType);
        Assert.Equal("Test Agent", attribute.Name);
        Assert.Equal("1.0.0", attribute.Version); // Default value
        Assert.Equal(string.Empty, attribute.Description); // Default value
        Assert.Equal(0, attribute.Priority); // Default value
        Assert.Equal(ServiceLifetime.Scoped, attribute.Lifetime); // Default value
        Assert.Empty(attribute.Capabilities);
        Assert.Empty(attribute.Tags);
    }

    [Fact]
    public void Constructor_Should_Initialize_All_Properties()
    {
        // Arrange & Act
        var attribute = new AIAgentAttribute("test-agent", "TestBot", "Test Agent", "2.0.0", "Test description")
        {
            Priority = 5,
            Capabilities = new[] { "Chat", "Analysis" },
            Lifetime = ServiceLifetime.Singleton,
            Tags = new[] { "AI", "Bot" }
        };

        // Assert
        Assert.Equal("test-agent", attribute.AgentId);
        Assert.Equal("TestBot", attribute.AgentType);
        Assert.Equal("Test Agent", attribute.Name);
        Assert.Equal("2.0.0", attribute.Version);
        Assert.Equal("Test description", attribute.Description);
        Assert.Equal(5, attribute.Priority);
        Assert.Equal(ServiceLifetime.Singleton, attribute.Lifetime);
        Assert.Equal(new[] { "Chat", "Analysis" }, attribute.Capabilities);
        Assert.Equal(new[] { "AI", "Bot" }, attribute.Tags);
    }

    [Theory]
    [InlineData(null, "TestBot", "Test Agent")]
    [InlineData("", "TestBot", "Test Agent")]
    [InlineData("test-agent", null, "Test Agent")]
    [InlineData("test-agent", "", "Test Agent")]
    [InlineData("test-agent", "TestBot", null)]
    [InlineData("test-agent", "TestBot", "")]
    public void Constructor_Should_Throw_ArgumentNullException_For_Required_Parameters(string? agentId, string? agentType, string? name)
    {
        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => new AIAgentAttribute(agentId!, agentType!, name!));
    }

    [Fact]
    public void Constructor_Should_Throw_ArgumentNullException_For_Null_Version()
    {
        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => new AIAgentAttribute("test-agent", "TestBot", "Test Agent", null!));
    }

    [Fact]
    public void Attribute_Should_Be_Applicable_To_Classes_Only()
    {
        // Arrange
        var attributeUsage = typeof(AIAgentAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .First();

        // Assert
        Assert.Equal(AttributeTargets.Class, attributeUsage.ValidOn);
        Assert.False(attributeUsage.AllowMultiple);
        Assert.True(attributeUsage.Inherited);
    }
}

/// <summary>
/// Tests for AI agent capability interfaces and implementations.
/// </summary>
public class AIAgentCapabilityTests
{
    [Fact]
    public void TestCapability_Should_Implement_IAIAgentCapability()
    {
        // Arrange
        var capability = new TestAIAgentCapability();

        // Act & Assert
        Assert.IsAssignableFrom<IAIAgentCapability>(capability);
        Assert.Equal("TestCapability", capability.Name);
        Assert.Equal("1.0.0", capability.Version);
        Assert.Equal("A test capability for unit testing", capability.Description);
    }
}

// Test helper classes
public class TestAIAgentCapability : IAIAgentCapability
{
    public string Name => "TestCapability";
    public string Version => "1.0.0";
    public string Description => "A test capability for unit testing";
}