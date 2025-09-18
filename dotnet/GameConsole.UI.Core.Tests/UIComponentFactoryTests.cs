using Xunit;
using GameConsole.UI.Core;

namespace GameConsole.UI.Core.Tests;

/// <summary>
/// Tests for the UIComponentFactory and BasicUIComponent classes.
/// </summary>
public class UIComponentFactoryTests
{
    [Fact]
    public void Constructor_ShouldSetFrameworkTypeAndRegisterDefaultComponents()
    {
        // Arrange & Act
        var factory = new UIComponentFactory(UIFrameworkType.Web);
        
        // Assert
        Assert.Equal(UIFrameworkType.Web, factory.FrameworkType);
        Assert.Contains("text", factory.SupportedComponentTypes);
        Assert.Contains("button", factory.SupportedComponentTypes);
        Assert.Contains("input", factory.SupportedComponentTypes);
        Assert.Contains("container", factory.SupportedComponentTypes);
    }
    
    [Fact]
    public async Task CreateComponentAsync_WithValidType_ShouldReturnComponent()
    {
        // Arrange
        var factory = new UIComponentFactory(UIFrameworkType.Console);
        var context = UIContext.Create(UIFrameworkType.Console, UICapabilities.TextInput);
        
        // Act
        var component = await factory.CreateComponentAsync("text", context);
        
        // Assert
        Assert.NotNull(component);
        Assert.Equal("text", component.ComponentType);
        Assert.True(component.IsVisible);
        Assert.True(component.IsEnabled);
    }
    
    [Fact]
    public async Task CreateComponentAsync_WithInvalidType_ShouldThrowArgumentException()
    {
        // Arrange
        var factory = new UIComponentFactory(UIFrameworkType.Desktop);
        var context = UIContext.Create(UIFrameworkType.Desktop, UICapabilities.MouseInteraction);
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            factory.CreateComponentAsync("invalid_type", context));
    }
    
    [Fact]
    public async Task CreateComponentAsync_WithData_ShouldSetComponentData()
    {
        // Arrange
        var factory = new UIComponentFactory(UIFrameworkType.Web);
        var context = UIContext.Create(UIFrameworkType.Web, UICapabilities.FormInput);
        const string testData = "Hello World";
        
        // Act
        var component = await factory.CreateComponentAsync("text", testData, context);
        
        // Assert
        Assert.Equal(testData, component.Data);
    }
    
    [Fact]
    public async Task CreateComponentAsync_WithProperties_ShouldSetComponentProperties()
    {
        // Arrange
        var factory = new UIComponentFactory(UIFrameworkType.Console);
        var context = UIContext.Create(UIFrameworkType.Console, UICapabilities.ColorDisplay);
        var properties = new Dictionary<string, object>
        {
            { "color", "red" },
            { "size", 12 }
        };
        
        // Act
        var component = await factory.CreateComponentAsync("button", properties, context);
        
        // Assert
        Assert.Equal("red", component.GetProperty<string>("color"));
        Assert.Equal(12, component.GetProperty<int>("size"));
    }
    
    [Theory]
    [InlineData("text", true)]
    [InlineData("button", true)]
    [InlineData("invalid_type", false)]
    public void CanCreateComponent_ShouldReturnCorrectResult(string componentType, bool expected)
    {
        // Arrange
        var factory = new UIComponentFactory(UIFrameworkType.Desktop);
        
        // Act
        var result = factory.CanCreateComponent(componentType);
        
        // Assert
        Assert.Equal(expected, result);
    }
}