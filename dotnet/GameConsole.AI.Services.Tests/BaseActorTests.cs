using Microsoft.Extensions.Logging;
using GameConsole.AI.Services;
using GameConsole.Engine.Core;
using Xunit;

namespace GameConsole.AI.Services.Tests;

/// <summary>
/// Test implementation of BaseActor for testing purposes.
/// </summary>
public class TestActor : BaseActor
{
    public TestActor(string actorId, ILogger logger) : base(actorId, logger)
    {
    }

    public List<ActorMessage> ProcessedMessages { get; } = new List<ActorMessage>();

    protected override Task OnProcessMessageAsync(ActorMessage message, CancellationToken cancellationToken = default)
    {
        ProcessedMessages.Add(message);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Test message implementation for testing purposes.
/// </summary>
public class TestMessage : ActorMessage
{
    public string Content { get; }

    public TestMessage(string content, string? senderId = null) : base(senderId)
    {
        Content = content;
    }
}

/// <summary>
/// Tests for the BaseActor implementation.
/// </summary>
public class BaseActorTests
{
    private readonly ILogger<TestActor> _logger;

    public BaseActorTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<TestActor>();
    }

    [Fact]
    public async Task Actor_InitializeStartStop_LifecycleWorksCorrectly()
    {
        // Arrange
        var actor = new TestActor("test-actor-1", _logger);

        // Act & Assert - Initial state
        Assert.Equal("test-actor-1", actor.ActorId);
        Assert.Equal(ActorState.Created, actor.State);
        Assert.Null(actor.ClusterId);

        // Initialize
        await actor.InitializeAsync();
        Assert.Equal(ActorState.Initializing, actor.State);

        // Start
        await actor.StartAsync();
        Assert.Equal(ActorState.Active, actor.State);

        // Pause
        await actor.PauseAsync();
        Assert.Equal(ActorState.Paused, actor.State);

        // Resume
        await actor.ResumeAsync();
        Assert.Equal(ActorState.Active, actor.State);

        // Stop
        await actor.StopAsync();
        Assert.Equal(ActorState.Stopped, actor.State);

        // Cleanup
        await actor.DisposeAsync();
    }

    [Fact]
    public async Task Actor_SendMessage_ProcessesMessageWhenActive()
    {
        // Arrange
        var actor = new TestActor("test-actor-2", _logger);
        await actor.InitializeAsync();
        await actor.StartAsync();

        var message = new TestMessage("Hello World", "sender-1");

        // Act
        await actor.SendMessageAsync(message);
        
        // Wait a moment for message processing
        await Task.Delay(100);

        // Assert
        Assert.Single(actor.ProcessedMessages);
        Assert.Equal("Hello World", ((TestMessage)actor.ProcessedMessages[0]).Content);

        // Cleanup
        await actor.DisposeAsync();
    }

    [Fact]
    public async Task Actor_JoinLeaveCluster_UpdatesClusterId()
    {
        // Arrange
        var actor = new TestActor("test-actor-3", _logger);
        await actor.InitializeAsync();

        // Act & Assert - Join cluster
        await actor.JoinClusterAsync("cluster-1");
        Assert.Equal("cluster-1", actor.ClusterId);

        // Leave cluster
        await actor.LeaveClusterAsync();
        Assert.Null(actor.ClusterId);

        // Cleanup
        await actor.DisposeAsync();
    }

    [Fact]
    public async Task Actor_GetMetrics_ReturnsExpectedMetrics()
    {
        // Arrange
        var actor = new TestActor("test-actor-4", _logger);
        await actor.InitializeAsync();
        await actor.StartAsync();
        await actor.JoinClusterAsync("test-cluster");

        // Act
        var metrics = await actor.GetMetricsAsync();

        // Assert
        Assert.Equal("test-actor-4", metrics["ActorId"]);
        Assert.Equal("Active", metrics["State"]);
        Assert.Equal("test-cluster", metrics["ClusterId"]);
        Assert.Equal(0, metrics["QueuedMessages"]);
        Assert.True(metrics.ContainsKey("LastUpdated"));

        // Cleanup
        await actor.DisposeAsync();
    }

    [Fact]
    public async Task Actor_StateChangedEvent_RaisedCorrectly()
    {
        // Arrange
        var actor = new TestActor("test-actor-5", _logger);
        var stateChanges = new List<ActorStateChangedEventArgs>();
        
        actor.StateChanged += (sender, args) => stateChanges.Add(args);

        // Act
        await actor.InitializeAsync();
        await actor.StartAsync();
        await actor.StopAsync();

        // Assert
        Assert.Equal(3, stateChanges.Count);
        
        Assert.Equal("test-actor-5", stateChanges[0].ActorId);
        Assert.Equal(ActorState.Created, stateChanges[0].PreviousState);
        Assert.Equal(ActorState.Initializing, stateChanges[0].NewState);

        Assert.Equal(ActorState.Initializing, stateChanges[1].PreviousState);
        Assert.Equal(ActorState.Active, stateChanges[1].NewState);

        Assert.Equal(ActorState.Active, stateChanges[2].PreviousState);
        Assert.Equal(ActorState.Stopping, stateChanges[2].NewState);

        // Cleanup
        await actor.DisposeAsync();
    }
}