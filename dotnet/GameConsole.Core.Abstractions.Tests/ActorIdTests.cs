using GameConsole.Core.Abstractions;
using Xunit;

namespace GameConsole.Core.Abstractions.Tests;

public class ActorIdTests
{
    [Fact]
    public void ActorId_NewId_ShouldCreateUniqueIds()
    {
        // Act
        var id1 = ActorId.NewId();
        var id2 = ActorId.NewId();

        // Assert
        Assert.NotEqual(id1, id2);
        Assert.NotEqual(Guid.Empty, id1.Value);
        Assert.NotEqual(Guid.Empty, id2.Value);
    }

    [Fact]
    public void ActorId_FromString_ShouldParseValidGuid()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var guidString = guid.ToString();

        // Act
        var actorId = ActorId.FromString(guidString);

        // Assert
        Assert.Equal(guid, actorId.Value);
    }

    [Fact]
    public void ActorId_FromString_ShouldThrowForInvalidGuid()
    {
        // Arrange
        var invalidGuid = "not-a-guid";

        // Act & Assert
        Assert.Throws<FormatException>(() => ActorId.FromString(invalidGuid));
    }

    [Fact]
    public void ActorId_Equality_ShouldWorkCorrectly()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id1 = new ActorId(guid);
        var id2 = new ActorId(guid);
        var id3 = ActorId.NewId();

        // Assert
        Assert.Equal(id1, id2);
        Assert.True(id1 == id2);
        Assert.False(id1 != id2);
        Assert.NotEqual(id1, id3);
        Assert.False(id1 == id3);
        Assert.True(id1 != id3);
    }

    [Fact]
    public void ActorId_GetHashCode_ShouldBeConsistent()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id1 = new ActorId(guid);
        var id2 = new ActorId(guid);

        // Assert
        Assert.Equal(id1.GetHashCode(), id2.GetHashCode());
    }

    [Fact]
    public void ActorId_ToString_ShouldReturnGuidString()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var actorId = new ActorId(guid);

        // Act
        var result = actorId.ToString();

        // Assert
        Assert.Equal(guid.ToString(), result);
    }

    [Fact]
    public void ActorId_ImplicitConversion_ShouldWorkCorrectly()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        ActorId actorId = guid; // Implicit conversion from Guid
        Guid convertedGuid = actorId; // Implicit conversion to Guid

        // Assert
        Assert.Equal(guid, actorId.Value);
        Assert.Equal(guid, convertedGuid);
    }
}