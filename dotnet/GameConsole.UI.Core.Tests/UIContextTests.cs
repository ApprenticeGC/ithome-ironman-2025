using GameConsole.UI.Core;
using Xunit;

namespace GameConsole.UI.Core.Tests;

public class UIContextTests
{
    [Fact]
    public void Create_ShouldCreateMinimalContext()
    {
        // Act
        var context = UIContext.Create();
        
        // Assert
        Assert.Empty(context.Args);
        Assert.Empty(context.State);
        Assert.Equal(UIMode.CLI, context.CurrentMode);
        Assert.Equal(FrameworkType.Console, context.FrameworkType);
        Assert.Equal(UICapabilities.None, context.SupportedCapabilities);
        Assert.NotNull(context.Preferences);
        Assert.NotNull(context.Style);
    }

    [Fact]
    public void Create_WithParameters_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var mode = UIMode.TUI;
        var frameworkType = FrameworkType.Desktop;
        var capabilities = UICapabilities.MouseInteraction | UICapabilities.ColorDisplay;
        
        // Act
        var context = UIContext.Create(mode, frameworkType, capabilities);
        
        // Assert
        Assert.Equal(mode, context.CurrentMode);
        Assert.Equal(frameworkType, context.FrameworkType);
        Assert.Equal(capabilities, context.SupportedCapabilities);
    }

    [Fact]
    public void HasCapability_ShouldReturnCorrectValue()
    {
        // Arrange
        var capabilities = UICapabilities.TextInput | UICapabilities.MouseInteraction;
        var context = UIContext.Create(capabilities: capabilities);
        
        // Act & Assert
        Assert.True(context.HasCapability(UICapabilities.TextInput));
        Assert.True(context.HasCapability(UICapabilities.MouseInteraction));
        Assert.False(context.HasCapability(UICapabilities.FileSelection));
    }

    [Fact]
    public void GetState_ShouldReturnDefaultWhenKeyNotFound()
    {
        // Arrange
        var context = UIContext.Create();
        
        // Act
        var result = context.GetState<string>("nonexistent", "default");
        
        // Assert
        Assert.Equal("default", result);
    }

    [Fact]
    public void WithState_ShouldCreateNewContextWithState()
    {
        // Arrange
        var original = UIContext.Create();
        
        // Act
        var updated = original.WithState("key", "value");
        
        // Assert
        Assert.Empty(original.State);
        Assert.Single(updated.State);
        Assert.Equal("value", updated.GetState<string>("key"));
    }

    [Fact]
    public void WithCapabilities_ShouldAddCapabilities()
    {
        // Arrange
        var original = UIContext.Create(capabilities: UICapabilities.TextInput);
        
        // Act
        var updated = original.WithCapabilities(UICapabilities.MouseInteraction);
        
        // Assert
        Assert.True(original.HasCapability(UICapabilities.TextInput));
        Assert.False(original.HasCapability(UICapabilities.MouseInteraction));
        
        Assert.True(updated.HasCapability(UICapabilities.TextInput));
        Assert.True(updated.HasCapability(UICapabilities.MouseInteraction));
    }
}