using GameConsole.AI.Actors.Core;
using Xunit;

namespace GameConsole.AI.Actors.Core.Tests;

public class ClusterNodeTests
{
    [Fact]
    public void Constructor_SetsRequiredProperties()
    {
        // Arrange
        var address = "node1@localhost:8080";

        // Act
        var node = new ClusterNode
        {
            Address = address
        };

        // Assert
        Assert.Equal(address, node.Address);
        Assert.Equal(ClusterNodeStatus.Joining, node.Status);
        Assert.Empty(node.Roles);
        Assert.True(node.LastSeen <= DateTime.UtcNow);
    }

    [Fact]
    public void Constructor_WithAllProperties_SetsCorrectly()
    {
        // Arrange
        var address = "node1@localhost:8080";
        var status = ClusterNodeStatus.Up;
        var roles = new[] { "worker", "api" };
        var lastSeen = DateTime.UtcNow.AddMinutes(-5);

        // Act
        var node = new ClusterNode
        {
            Address = address,
            Status = status,
            Roles = roles,
            LastSeen = lastSeen
        };

        // Assert
        Assert.Equal(address, node.Address);
        Assert.Equal(status, node.Status);
        Assert.Equal(roles, node.Roles);
        Assert.Equal(lastSeen, node.LastSeen);
    }

    [Fact]
    public void AddressComparison_WorksCorrectly()
    {
        // Arrange
        var node1 = new ClusterNode { Address = "node1@localhost:8080" };
        var node2 = new ClusterNode { Address = "node1@localhost:8080" };
        var node3 = new ClusterNode { Address = "node2@localhost:8080" };

        // Act & Assert
        Assert.Equal(node1.Address, node2.Address);
        Assert.NotEqual(node1.Address, node3.Address);
    }

    [Fact]
    public void StatusComparison_WorksCorrectly()
    {
        // Arrange
        var node1 = new ClusterNode { Address = "test@localhost", Status = ClusterNodeStatus.Up };
        var node2 = new ClusterNode { Address = "test@localhost", Status = ClusterNodeStatus.Down };

        // Act & Assert
        Assert.NotEqual(node1.Status, node2.Status);
        Assert.Equal(ClusterNodeStatus.Up, node1.Status);
    }
}

public class ClusterMembershipChangedEventArgsTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        // Arrange
        var node = new ClusterNode { Address = "test@localhost" };
        var changeType = ClusterMembershipChangeType.NodeJoined;

        // Act
        var eventArgs = new ClusterMembershipChangedEventArgs
        {
            Node = node,
            ChangeType = changeType
        };

        // Assert
        Assert.Equal(node, eventArgs.Node);
        Assert.Equal(changeType, eventArgs.ChangeType);
    }
}