using GameConsole.Core.Abstractions;
using GameConsole.Engine.Core;
using Xunit;

namespace GameConsole.Engine.Core.Tests;

public class ActorClusterTests : IAsyncDisposable
{
    private ActorCluster? _cluster;
    private readonly List<TestActor> _actors = new();

    public async ValueTask DisposeAsync()
    {
        if (_cluster != null)
        {
            await _cluster.DisposeAsync();
        }
        
        foreach (var actor in _actors)
        {
            await actor.DisposeAsync();
        }
        _actors.Clear();
    }

    private async Task<TestActor> CreateAndStartActor()
    {
        var actor = new TestActor();
        await actor.InitializeAsync();
        await actor.StartAsync();
        _actors.Add(actor);
        return actor;
    }

    [Fact]
    public void ActorCluster_Initialize_ShouldSetCorrectProperties()
    {
        // Arrange
        var clusterName = "TestCluster";
        var actorType = "TestActor";
        var clusterId = ClusterId.NewId();

        // Act
        _cluster = new ActorCluster(clusterName, actorType, clusterId);

        // Assert
        Assert.Equal(clusterId, _cluster.Id);
        Assert.Equal(clusterName, _cluster.ClusterName);
        Assert.Equal(actorType, _cluster.ActorType);
        Assert.Equal(0, _cluster.MemberCount);
        Assert.False(_cluster.IsRunning);
    }

    [Fact]
    public async Task ActorCluster_StartStop_ShouldFollowCorrectLifecycle()
    {
        // Arrange
        _cluster = new ActorCluster("TestCluster", "TestActor");

        // Act & Assert - Initial state
        Assert.False(_cluster.IsRunning);

        // Initialize and Start
        await _cluster.InitializeAsync();
        await _cluster.StartAsync();
        Assert.True(_cluster.IsRunning);

        // Stop
        await _cluster.StopAsync();
        Assert.False(_cluster.IsRunning);
    }

    [Fact]
    public async Task ActorCluster_RegisterActor_ShouldAddToCluster()
    {
        // Arrange
        _cluster = new ActorCluster("TestCluster", "TestActor");
        await _cluster.InitializeAsync();
        await _cluster.StartAsync();

        var actor = await CreateAndStartActor();

        bool membershipEventFired = false;
        _cluster.MembershipChanged += (sender, args) =>
        {
            membershipEventFired = true;
            Assert.Equal(_cluster.Id, args.ClusterId);
            Assert.Equal(actor.Id, args.ActorId);
            Assert.True(args.IsJoining);
        };

        // Act
        await _cluster.RegisterActorAsync(actor);

        // Assert
        Assert.Equal(1, _cluster.MemberCount);
        Assert.True(await _cluster.HasMemberAsync(actor.Id));
        Assert.True(membershipEventFired);

        var memberIds = await _cluster.GetMemberIdsAsync();
        Assert.Contains(actor.Id, memberIds);
    }

    [Fact]
    public async Task ActorCluster_RegisterActor_WrongType_ShouldThrow()
    {
        // Arrange
        _cluster = new ActorCluster("TestCluster", "DifferentType");
        await _cluster.InitializeAsync();
        await _cluster.StartAsync();

        var actor = await CreateAndStartActor(); // This is a "TestActor" type

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _cluster.RegisterActorAsync(actor));
    }

    [Fact]
    public async Task ActorCluster_UnregisterActor_ShouldRemoveFromCluster()
    {
        // Arrange
        _cluster = new ActorCluster("TestCluster", "TestActor");
        await _cluster.InitializeAsync();
        await _cluster.StartAsync();

        var actor = await CreateAndStartActor();
        await _cluster.RegisterActorAsync(actor);

        bool membershipEventFired = false;
        _cluster.MembershipChanged += (sender, args) =>
        {
            if (!args.IsJoining) // Only check for leaving events
            {
                membershipEventFired = true;
                Assert.Equal(_cluster.Id, args.ClusterId);
                Assert.Equal(actor.Id, args.ActorId);
                Assert.False(args.IsJoining);
            }
        };

        // Act
        await _cluster.UnregisterActorAsync(actor.Id);

        // Assert
        Assert.Equal(0, _cluster.MemberCount);
        Assert.False(await _cluster.HasMemberAsync(actor.Id));
        Assert.True(membershipEventFired);

        var memberIds = await _cluster.GetMemberIdsAsync();
        Assert.DoesNotContain(actor.Id, memberIds);
    }

    [Fact]
    public async Task ActorCluster_BroadcastMessage_ShouldSendToAllMembers()
    {
        // Arrange
        _cluster = new ActorCluster("TestCluster", "TestActor");
        await _cluster.InitializeAsync();
        await _cluster.StartAsync();

        var actor1 = await CreateAndStartActor();
        var actor2 = await CreateAndStartActor();
        var actor3 = await CreateAndStartActor();

        await _cluster.RegisterActorAsync(actor1);
        await _cluster.RegisterActorAsync(actor2);
        await _cluster.RegisterActorAsync(actor3);

        var message = new TestMessage("broadcast");

        // Act
        await _cluster.BroadcastMessageAsync(message);
        
        // Wait for message processing
        await Task.Delay(100);

        // Assert
        Assert.Contains(message, actor1.ReceivedMessages);
        Assert.Contains(message, actor2.ReceivedMessages);
        Assert.Contains(message, actor3.ReceivedMessages);
    }

    [Fact]
    public async Task ActorCluster_BroadcastMessage_WithExclude_ShouldSkipExcludedActor()
    {
        // Arrange
        _cluster = new ActorCluster("TestCluster", "TestActor");
        await _cluster.InitializeAsync();
        await _cluster.StartAsync();

        var actor1 = await CreateAndStartActor();
        var actor2 = await CreateAndStartActor();

        await _cluster.RegisterActorAsync(actor1);
        await _cluster.RegisterActorAsync(actor2);

        var message = new TestMessage("broadcast");

        // Act
        await _cluster.BroadcastMessageAsync(message, actor1.Id);
        
        // Wait for message processing
        await Task.Delay(100);

        // Assert
        Assert.DoesNotContain(message, actor1.ReceivedMessages);
        Assert.Contains(message, actor2.ReceivedMessages);
    }

    [Fact]
    public async Task ActorCluster_SendMessageToActor_ShouldSendToSpecificActor()
    {
        // Arrange
        _cluster = new ActorCluster("TestCluster", "TestActor");
        await _cluster.InitializeAsync();
        await _cluster.StartAsync();

        var actor1 = await CreateAndStartActor();
        var actor2 = await CreateAndStartActor();

        await _cluster.RegisterActorAsync(actor1);
        await _cluster.RegisterActorAsync(actor2);

        var message = new TestMessage("specific");

        // Act
        await _cluster.SendMessageToActorAsync(actor2.Id, message);
        
        // Wait for message processing
        await Task.Delay(100);

        // Assert
        Assert.DoesNotContain(message, actor1.ReceivedMessages);
        Assert.Contains(message, actor2.ReceivedMessages);
    }

    [Fact]
    public async Task ActorCluster_SendMessageToActor_NotInCluster_ShouldThrow()
    {
        // Arrange
        _cluster = new ActorCluster("TestCluster", "TestActor");
        await _cluster.InitializeAsync();
        await _cluster.StartAsync();

        var actor = await CreateAndStartActor();
        var message = new TestMessage("test");

        // Act & Assert - Actor not registered with cluster
        await Assert.ThrowsAsync<ArgumentException>(() => _cluster.SendMessageToActorAsync(actor.Id, message));
    }

    [Fact]
    public async Task ActorCluster_OperationsWhenNotRunning_ShouldThrow()
    {
        // Arrange
        _cluster = new ActorCluster("TestCluster", "TestActor");
        var actor = await CreateAndStartActor();
        var message = new TestMessage("test");

        // Act & Assert - All operations should throw when not running
        await Assert.ThrowsAsync<InvalidOperationException>(() => _cluster.GetMemberIdsAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => _cluster.RegisterActorAsync(actor));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _cluster.UnregisterActorAsync(actor.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _cluster.HasMemberAsync(actor.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _cluster.BroadcastMessageAsync(message));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _cluster.SendMessageToActorAsync(actor.Id, message));
    }
}