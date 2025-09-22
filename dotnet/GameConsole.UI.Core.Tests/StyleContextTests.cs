using GameConsole.UI.Core;
using Xunit;

namespace GameConsole.UI.Core.Tests;

public class StyleContextTests
{
    [Fact]
    public void Empty_ShouldCreateEmptyContext()
    {
        // Act
        var context = StyleContext.Empty;
        
        // Assert
        Assert.Empty(context.Properties);
        Assert.Null(context.Theme);
        Assert.True(context.IsResponsive);
        Assert.Null(context.MediaQueries);
    }

    [Fact]
    public void GetProperty_ShouldReturnCorrectValue()
    {
        // Arrange
        var properties = new Dictionary<string, object>
        {
            ["color"] = "red",
            ["fontSize"] = 16
        };
        var context = new StyleContext(properties);
        
        // Act & Assert
        Assert.Equal("red", context.GetProperty<string>("color"));
        Assert.Equal(16, context.GetProperty<int>("fontSize"));
        Assert.Equal("blue", context.GetProperty<string>("background", "blue"));
        Assert.Equal(0, context.GetProperty<int>("margin", 0));
    }

    [Fact]
    public void WithProperty_ShouldCreateNewContextWithProperty()
    {
        // Arrange
        var original = StyleContext.Empty;
        
        // Act
        var updated = original.WithProperty("color", "red");
        
        // Assert
        Assert.Empty(original.Properties);
        Assert.Single(updated.Properties);
        Assert.Equal("red", updated.GetProperty<string>("color"));
    }

    [Fact]
    public void WithProperty_ShouldPreserveExistingProperties()
    {
        // Arrange
        var original = StyleContext.Empty.WithProperty("color", "red");
        
        // Act
        var updated = original.WithProperty("fontSize", 16);
        
        // Assert
        Assert.Equal(2, updated.Properties.Count);
        Assert.Equal("red", updated.GetProperty<string>("color"));
        Assert.Equal(16, updated.GetProperty<int>("fontSize"));
    }

    [Fact]
    public void WithProperty_ShouldOverwriteExistingProperty()
    {
        // Arrange
        var original = StyleContext.Empty.WithProperty("color", "red");
        
        // Act
        var updated = original.WithProperty("color", "blue");
        
        // Assert
        Assert.Single(updated.Properties);
        Assert.Equal("blue", updated.GetProperty<string>("color"));
    }

    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        // Arrange
        var properties = new Dictionary<string, object> { ["color"] = "red" };
        var mediaQueries = new Dictionary<string, object> { ["mobile"] = "max-width: 768px" };
        
        // Act
        var context = new StyleContext(properties, "dark", false, mediaQueries);
        
        // Assert
        Assert.Single(context.Properties);
        Assert.Equal("dark", context.Theme);
        Assert.False(context.IsResponsive);
        Assert.Single(context.MediaQueries!);
    }
}