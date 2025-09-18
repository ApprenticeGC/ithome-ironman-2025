using GameConsole.Core.Abstractions;
using Xunit;

namespace GameConsole.Core.Abstractions.Tests;

/// <summary>
/// Tests for the ServiceAttribute class.
/// </summary>
public class ServiceAttributeTests
{
    [Fact]
    public void ServiceAttribute_Should_Require_Name_Parameter()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new ServiceAttribute(null!));
        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void ServiceAttribute_Should_Set_Required_Properties()
    {
        // Arrange
        const string name = "TestService";
        const string version = "2.0.0";
        const string description = "Test service description";
        
        // Act
        var attribute = new ServiceAttribute(name, version, description);
        
        // Assert
        Assert.Equal(name, attribute.Name);
        Assert.Equal(version, attribute.Version);
        Assert.Equal(description, attribute.Description);
    }

    [Fact]
    public void ServiceAttribute_Should_Use_Default_Values()
    {
        // Arrange
        const string name = "TestService";
        
        // Act
        var attribute = new ServiceAttribute(name);
        
        // Assert
        Assert.Equal(name, attribute.Name);
        Assert.Equal("1.0.0", attribute.Version);
        Assert.Equal(string.Empty, attribute.Description);
    }

    [Fact]
    public void ServiceAttribute_Should_Initialize_Categories_As_Empty_Array()
    {
        // Arrange
        const string name = "TestService";
        
        // Act
        var attribute = new ServiceAttribute(name);
        
        // Assert
        Assert.NotNull(attribute.Categories);
        Assert.Empty(attribute.Categories);
    }

    [Fact]
    public void ServiceAttribute_Should_Default_To_Scoped_Lifetime()
    {
        // Arrange
        const string name = "TestService";
        
        // Act
        var attribute = new ServiceAttribute(name);
        
        // Assert
        Assert.Equal(ServiceLifetime.Scoped, attribute.Lifetime);
    }

    [Fact]
    public void ServiceAttribute_Should_Allow_Setting_Categories()
    {
        // Arrange
        const string name = "TestService";
        var categories = new[] { "Audio", "Core", "System" };
        
        // Act
        var attribute = new ServiceAttribute(name)
        {
            Categories = categories
        };
        
        // Assert
        Assert.Equal(categories, attribute.Categories);
    }

    [Fact]
    public void ServiceAttribute_Should_Allow_Setting_Lifetime()
    {
        // Arrange
        const string name = "TestService";
        
        // Act
        var attribute = new ServiceAttribute(name)
        {
            Lifetime = ServiceLifetime.Singleton
        };
        
        // Assert
        Assert.Equal(ServiceLifetime.Singleton, attribute.Lifetime);
    }

    [Theory]
    [InlineData(ServiceLifetime.Transient)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Singleton)]
    public void ServiceLifetime_Should_Have_All_Expected_Values(ServiceLifetime lifetime)
    {
        // Arrange & Act
        var attribute = new ServiceAttribute("TestService")
        {
            Lifetime = lifetime
        };
        
        // Assert
        Assert.Equal(lifetime, attribute.Lifetime);
    }

    [Fact]
    public void ServiceAttribute_Should_Have_Correct_AttributeUsage()
    {
        // Arrange
        var attributeType = typeof(ServiceAttribute);
        
        // Act
        var attributeUsage = attributeType.GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .First();
        
        // Assert
        Assert.Equal(AttributeTargets.Class, attributeUsage.ValidOn);
        Assert.False(attributeUsage.AllowMultiple);
        Assert.True(attributeUsage.Inherited);
    }
}
