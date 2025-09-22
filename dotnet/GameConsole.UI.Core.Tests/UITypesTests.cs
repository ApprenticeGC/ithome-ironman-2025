using GameConsole.UI.Core;
using Xunit;

namespace GameConsole.UI.Core.Tests;

/// <summary>
/// Tests for UI type structures and basic functionality.
/// </summary>
public class UITypesTests
{
    [Fact]
    public void UIPosition_Constructor_SetsProperties()
    {
        // Arrange & Act
        var position = new UIPosition(10, 20);

        // Assert
        Assert.Equal(10, position.X);
        Assert.Equal(20, position.Y);
    }

    [Fact]
    public void UIPosition_Zero_ReturnsCorrectValue()
    {
        // Act
        var zero = UIPosition.Zero;

        // Assert
        Assert.Equal(0, zero.X);
        Assert.Equal(0, zero.Y);
    }

    [Fact]
    public void UISize_Constructor_SetsProperties()
    {
        // Arrange & Act
        var size = new UISize(100, 50);

        // Assert
        Assert.Equal(100, size.Width);
        Assert.Equal(50, size.Height);
    }

    [Fact]
    public void UISize_Empty_ReturnsCorrectValue()
    {
        // Act
        var empty = UISize.Empty;

        // Assert
        Assert.Equal(0, empty.Width);
        Assert.Equal(0, empty.Height);
    }

    [Fact]
    public void UIRect_Constructor_SetsProperties()
    {
        // Arrange
        var position = new UIPosition(10, 20);
        var size = new UISize(100, 50);

        // Act
        var rect = new UIRect(position, size);

        // Assert
        Assert.Equal(position, rect.Position);
        Assert.Equal(size, rect.Size);
    }

    [Fact]
    public void UIRect_IntConstructor_SetsProperties()
    {
        // Act
        var rect = new UIRect(10, 20, 100, 50);

        // Assert
        Assert.Equal(10, rect.Position.X);
        Assert.Equal(20, rect.Position.Y);
        Assert.Equal(100, rect.Size.Width);
        Assert.Equal(50, rect.Size.Height);
    }

    [Fact]
    public void UIRect_BoundaryProperties_CalculateCorrectly()
    {
        // Arrange
        var rect = new UIRect(10, 20, 100, 50);

        // Assert
        Assert.Equal(10, rect.Left);
        Assert.Equal(20, rect.Top);
        Assert.Equal(110, rect.Right);
        Assert.Equal(70, rect.Bottom);
    }

    [Theory]
    [InlineData(UIColor.Black, 0)]
    [InlineData(UIColor.White, 15)]
    [InlineData(UIColor.Red, 12)]
    [InlineData(UIColor.Green, 10)]
    public void UIColor_EnumValues_HaveCorrectIntegerValues(UIColor color, int expectedValue)
    {
        // Assert
        Assert.Equal(expectedValue, (int)color);
    }

    [Theory]
    [InlineData(UIAlignment.Left)]
    [InlineData(UIAlignment.Center)]
    [InlineData(UIAlignment.Right)]
    [InlineData(UIAlignment.Top)]
    [InlineData(UIAlignment.Middle)]
    [InlineData(UIAlignment.Bottom)]
    public void UIAlignment_AllValues_AreDefined(UIAlignment alignment)
    {
        // Act & Assert - Should not throw
        var name = alignment.ToString();
        Assert.NotEmpty(name);
    }

    [Theory]
    [InlineData(UIState.Normal)]
    [InlineData(UIState.Focused)]
    [InlineData(UIState.Pressed)]
    [InlineData(UIState.Disabled)]
    [InlineData(UIState.Hovered)]
    public void UIState_AllValues_AreDefined(UIState state)
    {
        // Act & Assert - Should not throw
        var name = state.ToString();
        Assert.NotEmpty(name);
    }
}