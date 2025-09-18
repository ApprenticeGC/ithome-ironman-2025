using Xunit;
using GameConsole.UI.Core;

namespace GameConsole.UI.Core.Tests;

/// <summary>
/// Tests for the UIContext class functionality.
/// </summary>
public class UIContextTests
{
    [Fact]
    public void Create_ShouldReturnUIContextWithCorrectValues()
    {
        // Arrange
        var frameworkType = UIFrameworkType.Console;
        var capabilities = UICapabilities.TextInput | UICapabilities.ColorDisplay;
        
        // Act
        var context = UIContext.Create(frameworkType, capabilities);
        
        // Assert
        Assert.Equal(frameworkType, context.FrameworkType);
        Assert.Equal(capabilities, context.Capabilities);
        Assert.Empty(context.Args);
        Assert.Empty(context.State);
        Assert.Empty(context.Properties);
    }
    
    [Fact]
    public void WithState_ShouldReturnNewUIContextWithUpdatedState()
    {
        // Arrange
        var originalContext = UIContext.Create(UIFrameworkType.Web, UICapabilities.TextInput);
        const string key = "testKey";
        const string value = "testValue";
        
        // Act
        var updatedContext = originalContext.WithState(key, value);
        
        // Assert
        Assert.NotSame(originalContext, updatedContext);
        Assert.Empty(originalContext.State);
        Assert.Single(updatedContext.State);
        Assert.Equal(value, updatedContext.State[key]);
    }
    
    [Fact]
    public void WithProperty_ShouldReturnNewUIContextWithUpdatedProperties()
    {
        // Arrange
        var originalContext = UIContext.Create(UIFrameworkType.Desktop, UICapabilities.MouseInteraction);
        const string key = "propertyKey";
        const int value = 42;
        
        // Act
        var updatedContext = originalContext.WithProperty(key, value);
        
        // Assert
        Assert.NotSame(originalContext, updatedContext);
        Assert.Empty(originalContext.Properties);
        Assert.Single(updatedContext.Properties);
        Assert.Equal(value, updatedContext.Properties[key]);
    }
    
    [Theory]
    [InlineData(UICapabilities.TextInput, UICapabilities.TextInput, true)]
    [InlineData(UICapabilities.TextInput | UICapabilities.ColorDisplay, UICapabilities.TextInput, true)]
    [InlineData(UICapabilities.TextInput, UICapabilities.MouseInteraction, false)]
    [InlineData(UICapabilities.None, UICapabilities.TextInput, false)]
    public void SupportsCapability_ShouldReturnCorrectResult(UICapabilities contextCapabilities, UICapabilities testCapability, bool expected)
    {
        // Arrange
        var context = UIContext.Create(UIFrameworkType.Console, contextCapabilities);
        
        // Act
        var result = context.SupportsCapability(testCapability);
        
        // Assert
        Assert.Equal(expected, result);
    }
}