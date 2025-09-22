using GameConsole.UI.Core;
using Xunit;

namespace GameConsole.UI.Console.Tests;

/// <summary>
/// Tests for core UI types and data structures.
/// </summary>
public class UITypesTests
{
    [Fact]
    public void Position_Constructor_ShouldSetCoordinates()
    {
        // Arrange & Act
        var position = new Position(10, 20);

        // Assert
        Assert.Equal(10, position.X);
        Assert.Equal(20, position.Y);
    }

    [Fact]
    public void Position_Origin_ShouldReturnZeroZero()
    {
        // Act
        var origin = Position.Origin;

        // Assert
        Assert.Equal(0, origin.X);
        Assert.Equal(0, origin.Y);
    }

    [Fact]
    public void Position_Add_ShouldReturnCorrectSum()
    {
        // Arrange
        var pos1 = new Position(10, 20);
        var pos2 = new Position(5, 15);

        // Act
        var result = pos1.Add(pos2);

        // Assert
        Assert.Equal(15, result.X);
        Assert.Equal(35, result.Y);
    }

    [Fact]
    public void Position_Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var pos1 = new Position(10, 20);
        var pos2 = new Position(10, 20);

        // Act & Assert
        Assert.True(pos1.Equals(pos2));
        Assert.True(pos1.Equals((object)pos2));
        Assert.Equal(pos1.GetHashCode(), pos2.GetHashCode());
    }

    [Fact]
    public void Position_Equals_WithDifferentValues_ShouldReturnFalse()
    {
        // Arrange
        var pos1 = new Position(10, 20);
        var pos2 = new Position(15, 25);

        // Act & Assert
        Assert.False(pos1.Equals(pos2));
        Assert.False(pos1.Equals((object)pos2));
    }

    [Fact]
    public void Size_Constructor_ShouldSetDimensions()
    {
        // Arrange & Act
        var size = new Size(100, 50);

        // Assert
        Assert.Equal(100, size.Width);
        Assert.Equal(50, size.Height);
    }

    [Fact]
    public void Size_Empty_ShouldReturnZeroZero()
    {
        // Act
        var empty = Size.Empty;

        // Assert
        Assert.Equal(0, empty.Width);
        Assert.Equal(0, empty.Height);
    }

    [Fact]
    public void Size_Area_ShouldReturnCorrectCalculation()
    {
        // Arrange
        var size = new Size(10, 5);

        // Act
        var area = size.Area;

        // Assert
        Assert.Equal(50, area);
    }

    [Fact]
    public void Size_Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var size1 = new Size(100, 50);
        var size2 = new Size(100, 50);

        // Act & Assert
        Assert.True(size1.Equals(size2));
        Assert.True(size1.Equals((object)size2));
        Assert.Equal(size1.GetHashCode(), size2.GetHashCode());
    }

    [Fact]
    public void Rectangle_Constructor_ShouldSetPositionAndSize()
    {
        // Arrange
        var position = new Position(10, 20);
        var size = new Size(100, 50);

        // Act
        var rectangle = new Rectangle(position, size);

        // Assert
        Assert.Equal(position, rectangle.Position);
        Assert.Equal(size, rectangle.Size);
    }

    [Fact]
    public void Rectangle_Properties_ShouldReturnCorrectValues()
    {
        // Arrange
        var rectangle = new Rectangle(new Position(10, 20), new Size(100, 50));

        // Act & Assert
        Assert.Equal(10, rectangle.Left);
        Assert.Equal(20, rectangle.Top);
        Assert.Equal(109, rectangle.Right); // Left + Width - 1
        Assert.Equal(69, rectangle.Bottom); // Top + Height - 1
    }

    [Fact]
    public void Rectangle_Contains_WithPositionInside_ShouldReturnTrue()
    {
        // Arrange
        var rectangle = new Rectangle(new Position(10, 20), new Size(100, 50));
        var insidePosition = new Position(50, 30);

        // Act
        var result = rectangle.Contains(insidePosition);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Rectangle_Contains_WithPositionOutside_ShouldReturnFalse()
    {
        // Arrange
        var rectangle = new Rectangle(new Position(10, 20), new Size(100, 50));
        var outsidePosition = new Position(5, 15);

        // Act
        var result = rectangle.Contains(outsidePosition);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Rectangle_Contains_WithPositionOnBoundary_ShouldReturnTrue()
    {
        // Arrange
        var rectangle = new Rectangle(new Position(10, 20), new Size(10, 10));

        // Act & Assert
        Assert.True(rectangle.Contains(new Position(10, 20))); // Top-left
        Assert.True(rectangle.Contains(new Position(19, 29))); // Bottom-right
        Assert.True(rectangle.Contains(new Position(10, 29))); // Bottom-left
        Assert.True(rectangle.Contains(new Position(19, 20))); // Top-right
    }

    [Fact]
    public void TextStyle_Constructor_ShouldSetProperties()
    {
        // Arrange & Act
        var style = new TextStyle(ConsoleColor.Red, ConsoleColor.Blue, TextAttributes.Bold);

        // Assert
        Assert.Equal(ConsoleColor.Red, style.ForegroundColor);
        Assert.Equal(ConsoleColor.Blue, style.BackgroundColor);
        Assert.Equal(TextAttributes.Bold, style.Attributes);
    }

    [Fact]
    public void TextStyle_DefaultConstructor_ShouldSetDefaults()
    {
        // Act
        var style = new TextStyle();

        // Assert
        Assert.Null(style.ForegroundColor);
        Assert.Null(style.BackgroundColor);
        Assert.Equal(TextAttributes.None, style.Attributes);
    }

    [Theory]
    [InlineData(TextAttributes.None)]
    [InlineData(TextAttributes.Bold)]
    [InlineData(TextAttributes.Underline)]
    [InlineData(TextAttributes.Reverse)]
    [InlineData(TextAttributes.Blink)]
    [InlineData(TextAttributes.Bold | TextAttributes.Underline)]
    public void TextAttributes_EnumValues_ShouldWorkCorrectly(TextAttributes attributes)
    {
        // Arrange
        var style = new TextStyle(attributes: attributes);

        // Act & Assert
        Assert.Equal(attributes, style.Attributes);
    }

    [Fact]
    public void SizeChangedEventArgs_Constructor_ShouldSetProperties()
    {
        // Arrange
        var oldSize = new Size(80, 25);
        var newSize = new Size(120, 30);

        // Act
        var eventArgs = new SizeChangedEventArgs(oldSize, newSize);

        // Assert
        Assert.Equal(oldSize, eventArgs.OldSize);
        Assert.Equal(newSize, eventArgs.NewSize);
    }

    [Fact]
    public void Rectangle_Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var rect1 = new Rectangle(new Position(10, 20), new Size(100, 50));
        var rect2 = new Rectangle(new Position(10, 20), new Size(100, 50));

        // Act & Assert
        Assert.True(rect1.Equals(rect2));
        Assert.True(rect1.Equals((object)rect2));
    }

    [Fact]
    public void TextStyle_Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var style1 = new TextStyle(ConsoleColor.Red, ConsoleColor.Blue, TextAttributes.Bold);
        var style2 = new TextStyle(ConsoleColor.Red, ConsoleColor.Blue, TextAttributes.Bold);

        // Act & Assert
        Assert.True(style1.Equals(style2));
        Assert.True(style1.Equals((object)style2));
    }
}