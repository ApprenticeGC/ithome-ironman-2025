using GameConsole.Core.Abstractions;
using Xunit;

namespace GameConsole.Core.Abstractions.Tests;

public class ClusterIdTests
{
    [Fact]
    public void ClusterId_NewId_ShouldCreateUniqueIds()
    {
        // Act
        var id1 = ClusterId.NewId();
        var id2 = ClusterId.NewId();

        // Assert
        Assert.NotEqual(id1, id2);
        Assert.NotEqual(Guid.Empty, id1.Value);
        Assert.NotEqual(Guid.Empty, id2.Value);
    }

    [Fact]
    public void ClusterId_FromString_ShouldParseValidGuid()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var guidString = guid.ToString();

        // Act
        var clusterId = ClusterId.FromString(guidString);

        // Assert
        Assert.Equal(guid, clusterId.Value);
    }

    [Fact]
    public void ClusterId_FromString_ShouldThrowForInvalidGuid()
    {
        // Arrange
        var invalidGuid = "not-a-guid";

        // Act & Assert
        Assert.Throws<FormatException>(() => ClusterId.FromString(invalidGuid));
    }

    [Fact]
    public void ClusterId_Equality_ShouldWorkCorrectly()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id1 = new ClusterId(guid);
        var id2 = new ClusterId(guid);
        var id3 = ClusterId.NewId();

        // Assert
        Assert.Equal(id1, id2);
        Assert.True(id1 == id2);
        Assert.False(id1 != id2);
        Assert.NotEqual(id1, id3);
        Assert.False(id1 == id3);
        Assert.True(id1 != id3);
    }

    [Fact]
    public void ClusterId_GetHashCode_ShouldBeConsistent()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id1 = new ClusterId(guid);
        var id2 = new ClusterId(guid);

        // Assert
        Assert.Equal(id1.GetHashCode(), id2.GetHashCode());
    }

    [Fact]
    public void ClusterId_ToString_ShouldReturnGuidString()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var clusterId = new ClusterId(guid);

        // Act
        var result = clusterId.ToString();

        // Assert
        Assert.Equal(guid.ToString(), result);
    }

    [Fact]
    public void ClusterId_ImplicitConversion_ShouldWorkCorrectly()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        ClusterId clusterId = guid; // Implicit conversion from Guid
        Guid convertedGuid = clusterId; // Implicit conversion to Guid

        // Assert
        Assert.Equal(guid, clusterId.Value);
        Assert.Equal(guid, convertedGuid);
    }
}