using GameConsole.Input.Core.Types;
using Xunit;

namespace GameConsole.Input.Core.Tests.Types;

public class Vector2Tests
{
    [Fact]
    public void Constructor_SetsXAndYComponents()
    {
        // Arrange & Act
        var vector = new Vector2(3.5f, 4.2f);

        // Assert
        Assert.Equal(3.5f, vector.X);
        Assert.Equal(4.2f, vector.Y);
    }

    [Fact]
    public void Zero_ReturnsZeroVector()
    {
        // Act
        var zero = Vector2.Zero;

        // Assert
        Assert.Equal(0f, zero.X);
        Assert.Equal(0f, zero.Y);
    }

    [Fact]
    public void One_ReturnsUnitVector()
    {
        // Act
        var one = Vector2.One;

        // Assert
        Assert.Equal(1f, one.X);
        Assert.Equal(1f, one.Y);
    }

    [Fact]
    public void Magnitude_CalculatesCorrectLength()
    {
        // Arrange
        var vector = new Vector2(3f, 4f);

        // Act
        var magnitude = vector.Magnitude;

        // Assert
        Assert.Equal(5f, magnitude, precision: 5);
    }

    [Fact]
    public void SqrMagnitude_CalculatesSquaredLength()
    {
        // Arrange
        var vector = new Vector2(3f, 4f);

        // Act
        var sqrMagnitude = vector.SqrMagnitude;

        // Assert
        Assert.Equal(25f, sqrMagnitude);
    }

    [Fact]
    public void Normalized_ReturnsUnitVector()
    {
        // Arrange
        var vector = new Vector2(6f, 8f);

        // Act
        var normalized = vector.Normalized;

        // Assert
        Assert.Equal(0.6f, normalized.X, precision: 5);
        Assert.Equal(0.8f, normalized.Y, precision: 5);
        Assert.Equal(1f, normalized.Magnitude, precision: 5);
    }

    [Fact]
    public void Normalized_ZeroVector_ReturnsZero()
    {
        // Arrange
        var vector = Vector2.Zero;

        // Act
        var normalized = vector.Normalized;

        // Assert
        Assert.Equal(Vector2.Zero, normalized);
    }

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        // Arrange
        var vector1 = new Vector2(1.5f, 2.5f);
        var vector2 = new Vector2(1.5f, 2.5f);

        // Act & Assert
        Assert.True(vector1.Equals(vector2));
        Assert.True(vector1 == vector2);
        Assert.False(vector1 != vector2);
    }

    [Fact]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        // Arrange
        var vector1 = new Vector2(1.5f, 2.5f);
        var vector2 = new Vector2(1.5f, 2.6f);

        // Act & Assert
        Assert.False(vector1.Equals(vector2));
        Assert.False(vector1 == vector2);
        Assert.True(vector1 != vector2);
    }

    [Fact]
    public void Addition_CombinesComponents()
    {
        // Arrange
        var vector1 = new Vector2(1f, 2f);
        var vector2 = new Vector2(3f, 4f);

        // Act
        var result = vector1 + vector2;

        // Assert
        Assert.Equal(new Vector2(4f, 6f), result);
    }

    [Fact]
    public void Subtraction_SubtractsComponents()
    {
        // Arrange
        var vector1 = new Vector2(5f, 6f);
        var vector2 = new Vector2(2f, 3f);

        // Act
        var result = vector1 - vector2;

        // Assert
        Assert.Equal(new Vector2(3f, 3f), result);
    }

    [Fact]
    public void ScalarMultiplication_ScalesComponents()
    {
        // Arrange
        var vector = new Vector2(2f, 3f);
        var scalar = 2.5f;

        // Act
        var result1 = vector * scalar;
        var result2 = scalar * vector;

        // Assert
        var expected = new Vector2(5f, 7.5f);
        Assert.Equal(expected, result1);
        Assert.Equal(expected, result2);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var vector = new Vector2(1.234f, 5.678f);

        // Act
        var result = vector.ToString();

        // Assert
        Assert.Equal("(1.23, 5.68)", result);
    }

    [Fact]
    public void GetHashCode_SameVectors_ReturnsSameHash()
    {
        // Arrange
        var vector1 = new Vector2(1.5f, 2.5f);
        var vector2 = new Vector2(1.5f, 2.5f);

        // Act
        var hash1 = vector1.GetHashCode();
        var hash2 = vector2.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
    }
}