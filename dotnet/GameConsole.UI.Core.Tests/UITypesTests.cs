using GameConsole.UI.Core;
using Xunit;

namespace GameConsole.UI.Core.Tests;

/// <summary>
/// Tests for UI types and basic structures.
/// </summary>
public class UITypesTests
{
    [Fact]
    public void ConsolePosition_ShouldCreateCorrectly()
    {
        // Arrange & Act
        var position = new ConsolePosition(10, 20);
        
        // Assert
        Assert.Equal(10, position.X);
        Assert.Equal(20, position.Y);
    }
    
    [Fact]
    public void ComponentSize_ShouldCreateCorrectly()
    {
        // Arrange & Act
        var size = new ComponentSize(100, 50);
        
        // Assert
        Assert.Equal(100, size.Width);
        Assert.Equal(50, size.Height);
    }
    
    [Fact]
    public void ComponentBounds_ShouldCalculateRightAndBottom()
    {
        // Arrange
        var position = new ConsolePosition(5, 10);
        var size = new ComponentSize(20, 15);
        var bounds = new ComponentBounds(position, size);
        
        // Act & Assert
        Assert.Equal(25, bounds.Right);  // 5 + 20
        Assert.Equal(25, bounds.Bottom); // 10 + 15
    }
    
    [Fact]
    public void ComponentBounds_Contains_ShouldWorkCorrectly()
    {
        // Arrange
        var bounds = new ComponentBounds(
            new ConsolePosition(10, 10), 
            new ComponentSize(20, 20));
        
        // Act & Assert
        Assert.True(bounds.Contains(new ConsolePosition(15, 15))); // Inside
        Assert.True(bounds.Contains(new ConsolePosition(10, 10))); // Top-left corner
        Assert.False(bounds.Contains(new ConsolePosition(5, 5)));   // Outside left-top
        Assert.False(bounds.Contains(new ConsolePosition(30, 30))); // Outside right-bottom
        Assert.False(bounds.Contains(new ConsolePosition(30, 10))); // On right edge (exclusive)
    }
    
    [Fact]
    public void Spacing_UniformConstructor_ShouldSetAllSides()
    {
        // Arrange & Act
        var spacing = new Spacing(5);
        
        // Assert
        Assert.Equal(5, spacing.Left);
        Assert.Equal(5, spacing.Top);
        Assert.Equal(5, spacing.Right);
        Assert.Equal(5, spacing.Bottom);
        Assert.Equal(10, spacing.Horizontal); // Left + Right
        Assert.Equal(10, spacing.Vertical);   // Top + Bottom
    }
    
    [Fact]
    public void Spacing_HorizontalVerticalConstructor_ShouldSetCorrectly()
    {
        // Arrange & Act
        var spacing = new Spacing(horizontal: 8, vertical: 4);
        
        // Assert
        Assert.Equal(8, spacing.Left);
        Assert.Equal(4, spacing.Top);
        Assert.Equal(8, spacing.Right);
        Assert.Equal(4, spacing.Bottom);
        Assert.Equal(16, spacing.Horizontal);
        Assert.Equal(8, spacing.Vertical);
    }
    
    [Fact]
    public void ComponentStyle_ShouldCreateWithDefaults()
    {
        // Arrange & Act
        var style = new ComponentStyle();
        
        // Assert
        Assert.Null(style.Foreground);
        Assert.Null(style.Background);
        Assert.Equal(BorderStyle.None, style.Border);
        Assert.Equal(TextAlignment.Left, style.Alignment);
        Assert.False(style.Bold);
        Assert.False(style.Underline);
    }
    
    [Fact]
    public void ComponentStyle_ShouldCreateWithCustomValues()
    {
        // Arrange & Act
        var style = new ComponentStyle(
            Foreground: ConsoleColor.Red,
            Background: ConsoleColor.Blue,
            Border: BorderStyle.Single,
            Alignment: TextAlignment.Center,
            Bold: true,
            Underline: true);
        
        // Assert
        Assert.Equal(ConsoleColor.Red, style.Foreground);
        Assert.Equal(ConsoleColor.Blue, style.Background);
        Assert.Equal(BorderStyle.Single, style.Border);
        Assert.Equal(TextAlignment.Center, style.Alignment);
        Assert.True(style.Bold);
        Assert.True(style.Underline);
    }
    
    [Fact]
    public void ComponentConfiguration_ShouldCreateWithRequiredId()
    {
        // Arrange & Act
        var config = new ComponentConfiguration("test-component");
        
        // Assert
        Assert.Equal("test-component", config.Id);
        Assert.Null(config.ParentId);
        Assert.Null(config.Bounds);
        Assert.Null(config.Style);
        Assert.Equal(Visibility.Visible, config.Visibility);
        Assert.False(config.CanFocus);
        Assert.Null(config.Properties);
    }
    
    [Fact]
    public void MenuItemResult_ShouldCreateCorrectly()
    {
        // Arrange & Act
        var result = new MenuItemResult("menu-item-1", "File", "/file", WasCancelled: false);
        
        // Assert
        Assert.Equal("menu-item-1", result.ItemId);
        Assert.Equal("File", result.DisplayText);
        Assert.Equal("/file", result.Value);
        Assert.False(result.WasCancelled);
    }
    
    [Theory]
    [InlineData(BorderStyle.None)]
    [InlineData(BorderStyle.Single)]
    [InlineData(BorderStyle.Double)]
    [InlineData(BorderStyle.Rounded)]
    [InlineData(BorderStyle.Thick)]
    [InlineData(BorderStyle.Dashed)]
    [InlineData(BorderStyle.Dotted)]
    public void BorderStyle_AllValuesSupported(BorderStyle borderStyle)
    {
        // Arrange & Act
        var style = new ComponentStyle(Border: borderStyle);
        
        // Assert
        Assert.Equal(borderStyle, style.Border);
    }
    
    [Theory]
    [InlineData(DialogResult.None)]
    [InlineData(DialogResult.OK)]
    [InlineData(DialogResult.Cancel)]
    [InlineData(DialogResult.Yes)]
    [InlineData(DialogResult.No)]
    [InlineData(DialogResult.Retry)]
    [InlineData(DialogResult.Ignore)]
    [InlineData(DialogResult.Abort)]
    public void DialogResult_AllValuesSupported(DialogResult result)
    {
        // This test ensures all dialog result values are properly defined
        Assert.True(Enum.IsDefined(typeof(DialogResult), result));
    }
}