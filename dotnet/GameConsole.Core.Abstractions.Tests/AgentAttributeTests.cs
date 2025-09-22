using System;
using System.Linq;
using GameConsole.Core.Abstractions;
using Xunit;

namespace GameConsole.Core.Abstractions.Tests;

public class AgentAttributeTests
{
    [Fact]
    public void Constructor_WithRequiredParameters_SetsProperties()
    {
        // Arrange & Act
        var attribute = new AgentAttribute("Test Agent", "2.0.0", "Test description");

        // Assert
        Assert.Equal("Test Agent", attribute.Name);
        Assert.Equal("2.0.0", attribute.Version);
        Assert.Equal("Test description", attribute.Description);
        Assert.Empty(attribute.Categories);
        Assert.Empty(attribute.Capabilities);
        Assert.Equal(ServiceLifetime.Scoped, attribute.Lifetime);
    }

    [Fact]
    public void Constructor_WithOnlyName_UsesDefaults()
    {
        // Arrange & Act
        var attribute = new AgentAttribute("Test Agent");

        // Assert
        Assert.Equal("Test Agent", attribute.Name);
        Assert.Equal("1.0.0", attribute.Version);
        Assert.Equal(string.Empty, attribute.Description);
        Assert.Empty(attribute.Categories);
        Assert.Empty(attribute.Capabilities);
        Assert.Equal(ServiceLifetime.Scoped, attribute.Lifetime);
    }

    [Fact]
    public void Constructor_WithNullName_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new AgentAttribute(null!));
        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullVersion_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new AgentAttribute("Test", null!));
        Assert.Equal("version", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullDescription_UsesEmptyString()
    {
        // Arrange & Act
        var attribute = new AgentAttribute("Test", "1.0.0", null!);

        // Assert
        Assert.Equal("Test", attribute.Name);
        Assert.Equal("1.0.0", attribute.Version);
        Assert.Equal(string.Empty, attribute.Description);
    }

    [Fact]
    public void Categories_CanBeSetAndRetrieved()
    {
        // Arrange
        var attribute = new AgentAttribute("Test Agent");
        var categories = new[] { "AI", "Game", "Strategy" };

        // Act
        attribute.Categories = categories;

        // Assert
        Assert.Equal(categories, attribute.Categories);
    }

    [Fact]
    public void Capabilities_CanBeSetAndRetrieved()
    {
        // Arrange
        var attribute = new AgentAttribute("Test Agent");
        var capabilities = new[] { "Planning", "Decision Making", "Learning" };

        // Act
        attribute.Capabilities = capabilities;

        // Assert
        Assert.Equal(capabilities, attribute.Capabilities);
    }

    [Fact]
    public void Lifetime_CanBeSetAndRetrieved()
    {
        // Arrange
        var attribute = new AgentAttribute("Test Agent");

        // Act
        attribute.Lifetime = ServiceLifetime.Singleton;

        // Assert
        Assert.Equal(ServiceLifetime.Singleton, attribute.Lifetime);
    }

    [Fact]
    public void AttributeUsage_IsCorrectlyConfigured()
    {
        // Arrange
        var attributeType = typeof(AgentAttribute);

        // Act
        var usageAttribute = attributeType.GetCustomAttributes(typeof(AttributeUsageAttribute), false)
                                         .Cast<AttributeUsageAttribute>()
                                         .First();

        // Assert
        Assert.NotNull(usageAttribute);
        Assert.Equal(AttributeTargets.Class, usageAttribute.ValidOn);
        Assert.False(usageAttribute.AllowMultiple);
        Assert.True(usageAttribute.Inherited);
    }
}