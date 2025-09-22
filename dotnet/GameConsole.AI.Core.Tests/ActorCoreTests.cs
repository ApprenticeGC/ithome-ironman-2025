using GameConsole.AI.Core;
using Xunit;

namespace GameConsole.AI.Core.Tests;

/// <summary>
/// Tests for core actor interfaces and basic functionality.
/// </summary>
public class ActorCoreTests
{
    [Fact]
    public void ActorAddress_Should_Create_From_String()
    {
        // Arrange
        const string path = "/system/test-actor";
        
        // Act
        var address = ActorAddress.From(path);
        
        // Assert
        Assert.Equal(path, address.Path);
        Assert.Equal(path, address.ToString());
    }
    
    [Fact]
    public void ActorAddress_Should_Support_Implicit_Conversion()
    {
        // Arrange
        const string path = "/system/test-actor";
        
        // Act
        ActorAddress address = path;
        
        // Assert
        Assert.Equal(path, address.Path);
    }
    
    [Fact]
    public void ActorAddress_Should_Be_Comparable()
    {
        // Arrange
        const string path = "/system/test-actor";
        var address1 = ActorAddress.From(path);
        var address2 = ActorAddress.From(path);
        var address3 = ActorAddress.From("/system/other-actor");
        
        // Act & Assert
        Assert.Equal(address1, address2);
        Assert.NotEqual(address1, address3);
        Assert.True(address1.Equals(address2));
        Assert.False(address1.Equals(address3));
    }
    
    [Fact]
    public void SystemMessages_Should_Have_Proper_Types()
    {
        // Arrange & Act
        var startMessage = new SystemMessages.StartMessage();
        var stopMessage = new SystemMessages.StopMessage("test reason");
        var statusRequest = new SystemMessages.StatusRequestMessage();
        var statusResponse = new SystemMessages.StatusResponseMessage("Running");
        
        // Assert
        Assert.NotNull(startMessage.MessageId);
        Assert.NotEqual(Guid.Empty, startMessage.MessageId);
        Assert.NotEqual(DateTimeOffset.MinValue, startMessage.Timestamp);
        
        Assert.NotNull(stopMessage.MessageId);
        Assert.Equal("test reason", stopMessage.Reason);
        
        Assert.NotNull(statusRequest.MessageId);
        
        Assert.NotNull(statusResponse.MessageId);
        Assert.Equal("Running", statusResponse.Status);
    }
    
    [Fact]
    public void AIAgentMessages_Should_Have_Proper_Structure()
    {
        // Arrange
        var taskId = "task-123";
        var taskType = "TestTask";
        var parameters = new Dictionary<string, object> { ["param1"] = "value1" };
        
        // Act
        var taskRequest = new AIAgentMessages.TaskRequestMessage(taskId, taskType, parameters);
        var taskCompleted = new AIAgentMessages.TaskCompletedMessage(taskId, true);
        var taskProgress = new AIAgentMessages.TaskProgressMessage(taskId, 0.5, "In Progress");
        
        // Assert
        Assert.Equal(taskId, taskRequest.TaskId);
        Assert.Equal(taskType, taskRequest.TaskType);
        Assert.Equal(parameters, taskRequest.Parameters);
        
        Assert.Equal(taskId, taskCompleted.TaskId);
        Assert.True(taskCompleted.Success);
        
        Assert.Equal(taskId, taskProgress.TaskId);
        Assert.Equal(0.5, taskProgress.Progress);
        Assert.Equal("In Progress", taskProgress.Status);
    }
    
    [Fact]
    public void ClusterMember_Should_Initialize_With_Default_Values()
    {
        // Arrange & Act
        var member = new ClusterMember();
        
        // Assert
        Assert.Empty(member.NodeId);
        Assert.Empty(member.Hostname);
        Assert.Equal(0, member.Port);
        Assert.Equal(ClusterMemberStatus.Joining, member.Status);
        Assert.NotNull(member.Metadata);
        Assert.Empty(member.Metadata);
    }
    
    [Fact]
    public void ClusterState_Should_Calculate_Active_Members()
    {
        // Arrange
        var state = new ClusterState
        {
            Members = new List<ClusterMember>
            {
                new ClusterMember { NodeId = "1", Status = ClusterMemberStatus.Up },
                new ClusterMember { NodeId = "2", Status = ClusterMemberStatus.Up },
                new ClusterMember { NodeId = "3", Status = ClusterMemberStatus.Leaving },
                new ClusterMember { NodeId = "4", Status = ClusterMemberStatus.Unreachable }
            }
        };
        
        // Act & Assert
        Assert.Equal(2, state.ActiveMemberCount);
    }
    
    [Fact]
    public void ClusterState_Should_Identify_Leader()
    {
        // Arrange
        var leader = new ClusterMember { NodeId = "leader-1" };
        var self = new ClusterMember { NodeId = "leader-1" };
        
        var state = new ClusterState
        {
            Leader = leader,
            Self = self
        };
        
        // Act & Assert
        Assert.True(state.IsLeader);
        
        // Test non-leader case
        state.Self = new ClusterMember { NodeId = "member-2" };
        Assert.False(state.IsLeader);
    }
}

/// <summary>
/// Mock implementation of AIAgent for testing purposes.
/// </summary>
public class TestAIAgent : AIAgent
{
    public bool InitializeCalled { get; private set; }
    public bool StartCalled { get; private set; }
    public bool StopCalled { get; private set; }
    public List<IActorMessage> ReceivedMessages { get; private set; } = new();
    
    public TestAIAgent(string name) : base(name) { }
    
    protected override Task OnInitializeAsync(CancellationToken cancellationToken = default)
    {
        InitializeCalled = true;
        return Task.CompletedTask;
    }
    
    protected override Task OnStartAsync(CancellationToken cancellationToken = default)
    {
        StartCalled = true;
        return Task.CompletedTask;
    }
    
    protected override Task OnStopAsync(CancellationToken cancellationToken = default)
    {
        StopCalled = true;
        return Task.CompletedTask;
    }
    
    protected override Task HandleCustomMessage(IActorMessage message, IActorContext context, CancellationToken cancellationToken = default)
    {
        ReceivedMessages.Add(message);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Tests for the AI Agent base class functionality.
/// </summary>
public class AIAgentTests
{
    [Fact]
    public async Task AIAgent_Should_Initialize_Correctly()
    {
        // Arrange
        var agent = new TestAIAgent("test-agent");
        
        // Act
        await agent.InitializeAsync();
        
        // Assert
        Assert.True(agent.InitializeCalled);
        Assert.Equal("test-agent", agent.Name);
        Assert.False(agent.IsRunning);
    }
    
    [Fact]
    public async Task AIAgent_Should_Start_And_Stop()
    {
        // Arrange
        var agent = new TestAIAgent("test-agent");
        await agent.InitializeAsync();
        
        // Act
        await agent.StartAsync();
        
        // Assert
        Assert.True(agent.StartCalled);
        Assert.True(agent.IsRunning);
        
        // Act
        await agent.StopAsync();
        
        // Assert
        Assert.True(agent.StopCalled);
        Assert.False(agent.IsRunning);
    }
    
    [Fact]
    public async Task AIAgent_Should_Dispose_Properly()
    {
        // Arrange
        var agent = new TestAIAgent("test-agent");
        await agent.InitializeAsync();
        await agent.StartAsync();
        
        // Act
        await agent.DisposeAsync();
        
        // Assert
        Assert.False(agent.IsRunning);
        Assert.True(agent.StopCalled);
    }
}