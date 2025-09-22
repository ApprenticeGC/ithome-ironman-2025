using GameConsole.AI.Core;
using GameConsole.AI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.AI.Services.Tests;

/// <summary>
/// Tests for AI Agent Actor Clustering functionality.
/// Validates the core clustering capabilities and distributed coordination.
/// </summary>
public class AIClusteringTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ILogger<BaseClusterableAIAgent> _agentLogger;
    private readonly ILogger<AkkaAIClusterCoordinatorService> _coordinatorLogger;

    public AIClusteringTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        
        _serviceProvider = services.BuildServiceProvider();
        
        _agentLogger = _serviceProvider.GetRequiredService<ILogger<BaseClusterableAIAgent>>();
        _coordinatorLogger = _serviceProvider.GetRequiredService<ILogger<AkkaAIClusterCoordinatorService>>();
    }

    [Fact]
    public void AI_Agent_Should_Have_Required_Properties()
    {
        // Arrange & Act
        var agent = new BaseClusterableAIAgent("agent-001", "worker", _agentLogger);

        // Assert
        Assert.Equal("agent-001", agent.AgentId);
        Assert.Equal("worker", agent.ClusterRole);
        Assert.Equal(ClusterMembershipStatus.Disconnected, agent.Status);
    }

    [Fact]
    public void AI_Agent_Should_Implement_Capability_Interface()
    {
        // Arrange
        var agent = new BaseClusterableAIAgent("agent-001", "worker", _agentLogger);

        // Act & Assert
        Assert.True(agent.HasCapability<IClusterableAIAgent>());
        Assert.NotNull(agent.GetCapability<IClusterableAIAgent>());
        Assert.Same(agent, agent.GetCapability<IClusterableAIAgent>());
    }

    [Fact]
    public async Task AI_Cluster_Coordinator_Should_Register_Agents()
    {
        // Arrange
        var coordinator = new AkkaAIClusterCoordinatorService(_coordinatorLogger);
        var agent = new BaseClusterableAIAgent("agent-001", "worker", _agentLogger);

        // Act
        await coordinator.RegisterAgentAsync(agent);
        var registeredAgents = await coordinator.GetRegisteredAgentsAsync();

        // Assert
        Assert.Single(registeredAgents);
        Assert.Contains(agent, registeredAgents);
    }

    [Fact]
    public async Task AI_Cluster_Coordinator_Should_Unregister_Agents()
    {
        // Arrange
        var coordinator = new AkkaAIClusterCoordinatorService(_coordinatorLogger);
        var agent = new BaseClusterableAIAgent("agent-001", "worker", _agentLogger);

        await coordinator.RegisterAgentAsync(agent);

        // Act
        await coordinator.UnregisterAgentAsync(agent.AgentId);
        var registeredAgents = await coordinator.GetRegisteredAgentsAsync();

        // Assert
        Assert.Empty(registeredAgents);
    }

    [Fact]
    public async Task AI_Cluster_Coordinator_Should_Filter_Agents_By_Role()
    {
        // Arrange
        var coordinator = new AkkaAIClusterCoordinatorService(_coordinatorLogger);
        var workerAgent = new BaseClusterableAIAgent("worker-001", "worker", _agentLogger);
        var coordinatorAgent = new BaseClusterableAIAgent("coord-001", "coordinator", _agentLogger);

        await coordinator.RegisterAgentAsync(workerAgent);
        await coordinator.RegisterAgentAsync(coordinatorAgent);

        // Act
        var workers = await coordinator.GetAgentsByRoleAsync("worker");
        var coordinators = await coordinator.GetAgentsByRoleAsync("coordinator");

        // Assert
        Assert.Single(workers);
        Assert.Contains(workerAgent, workers);
        
        Assert.Single(coordinators);
        Assert.Contains(coordinatorAgent, coordinators);
    }

    [Fact]
    public async Task AI_Cluster_Configuration_Should_Have_Default_Values()
    {
        // Arrange & Act
        var config = new AIClusterConfiguration();

        // Assert
        Assert.Equal("localhost", config.Hostname);
        Assert.Equal(8080, config.Port);
        Assert.Equal("ai-agents", config.ClusterName);
        Assert.Equal(TimeSpan.FromSeconds(5), config.HeartbeatInterval);
        Assert.Empty(config.SeedNodes);
    }

    [Fact]
    public async Task AI_Cluster_Health_Should_Reflect_Member_Count()
    {
        // Arrange
        var coordinator = new AkkaAIClusterCoordinatorService(_coordinatorLogger);

        // Act
        var health = await coordinator.GetClusterHealthAsync();

        // Assert
        Assert.NotNull(health);
        Assert.True(health.LastUpdated <= DateTime.UtcNow);
        Assert.Equal(0, health.TotalMembers); // No agents registered yet
    }

    [Theory]
    [InlineData("agent-001", "worker", ClusterMembershipStatus.Disconnected)]
    [InlineData("agent-002", "coordinator", ClusterMembershipStatus.Disconnected)]
    [InlineData("agent-003", "analyzer", ClusterMembershipStatus.Disconnected)]
    public void AI_Agent_Should_Start_With_Disconnected_Status(string agentId, string role, ClusterMembershipStatus expectedStatus)
    {
        // Arrange & Act
        var agent = new BaseClusterableAIAgent(agentId, role, _agentLogger);

        // Assert
        Assert.Equal(expectedStatus, agent.Status);
    }

    [Fact]
    public void Cluster_Member_Info_Should_Store_Member_Details()
    {
        // Arrange
        var joinedAt = DateTime.UtcNow;
        
        // Act
        var memberInfo = new ClusterMemberInfo(
            "agent-001", 
            "worker", 
            "localhost:8080", 
            ClusterMembershipStatus.Active, 
            joinedAt);

        // Assert
        Assert.Equal("agent-001", memberInfo.AgentId);
        Assert.Equal("worker", memberInfo.Role);
        Assert.Equal("localhost:8080", memberInfo.Address);
        Assert.Equal(ClusterMembershipStatus.Active, memberInfo.Status);
        Assert.Equal(joinedAt, memberInfo.JoinedAt);
    }

    [Fact]
    public void Cluster_Topology_Change_Event_Should_Contain_Relevant_Data()
    {
        // Arrange
        var memberInfo = new ClusterMemberInfo(
            "agent-001", "worker", "localhost:8080", ClusterMembershipStatus.Active, DateTime.UtcNow);
        var healthInfo = new ClusterHealthInfo(1, 1, 0, true, memberInfo, DateTime.UtcNow);

        // Act
        var topologyEvent = new ClusterTopologyChangedEventArgs(
            ClusterTopologyChangeType.MemberJoined, memberInfo, healthInfo);

        // Assert
        Assert.Equal(ClusterTopologyChangeType.MemberJoined, topologyEvent.ChangeType);
        Assert.Equal(memberInfo, topologyEvent.AffectedMember);
        Assert.Equal(healthInfo, topologyEvent.CurrentHealth);
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }
}