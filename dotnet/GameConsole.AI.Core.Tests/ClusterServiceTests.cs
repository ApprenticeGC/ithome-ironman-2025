using GameConsole.AI.Core;
using Xunit;

namespace GameConsole.AI.Core.Tests;

/// <summary>
/// Tests for ClusterMember functionality.
/// </summary>
public class ClusterMemberTests
{
    [Fact]
    public void ClusterMember_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var address = "akka://GameConsole@127.0.0.1:8080";
        var roles = new HashSet<string> { "frontend", "backend" };
        var state = ClusterState.Up;

        // Act
        var member = new ClusterMember(address, roles, state);

        // Assert
        Assert.Equal(address, member.Address);
        Assert.Equal(roles, member.Roles);
        Assert.Equal(state, member.State);
    }

    [Fact]
    public void ClusterMember_Constructor_ThrowsOnNullAddress()
    {
        // Arrange
        var roles = new HashSet<string> { "frontend" };
        var state = ClusterState.Up;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ClusterMember(null!, roles, state));
    }

    [Fact]
    public void ClusterMember_Constructor_ThrowsOnNullRoles()
    {
        // Arrange
        var address = "akka://GameConsole@127.0.0.1:8080";
        var state = ClusterState.Up;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ClusterMember(address, null!, state));
    }

    [Theory]
    [InlineData("frontend", true)]
    [InlineData("backend", true)]
    [InlineData("worker", false)]
    [InlineData("", false)]
    public void HasRole_ReturnsCorrectResult(string role, bool expected)
    {
        // Arrange
        var address = "akka://GameConsole@127.0.0.1:8080";
        var roles = new HashSet<string> { "frontend", "backend" };
        var state = ClusterState.Up;
        var member = new ClusterMember(address, roles, state);

        // Act
        var result = member.HasRole(role);

        // Assert
        Assert.Equal(expected, result);
    }
}

/// <summary>
/// Tests for ClusterStateChangedEventArgs.
/// </summary>
public class ClusterStateChangedEventArgsTests
{
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var previousState = ClusterState.Joining;
        var newState = ClusterState.Up;

        // Act
        var eventArgs = new ClusterStateChangedEventArgs(previousState, newState);

        // Assert
        Assert.Equal(previousState, eventArgs.PreviousState);
        Assert.Equal(newState, eventArgs.NewState);
    }
}

/// <summary>
/// Tests for ClusterMemberEventArgs.
/// </summary>
public class ClusterMemberEventArgsTests
{
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var member = new ClusterMember(
            "akka://GameConsole@127.0.0.1:8080",
            new HashSet<string> { "frontend" },
            ClusterState.Up);

        // Act
        var eventArgs = new ClusterMemberEventArgs(member);

        // Assert
        Assert.Equal(member, eventArgs.Member);
    }

    [Fact]
    public void Constructor_ThrowsOnNullMember()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ClusterMemberEventArgs(null!));
    }
}