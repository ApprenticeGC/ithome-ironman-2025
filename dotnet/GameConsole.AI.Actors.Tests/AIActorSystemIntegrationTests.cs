using Akka.TestKit.Xunit2;
using GameConsole.AI.Actors.Configuration;
using GameConsole.AI.Actors.Messages;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.AI.Actors.Tests;

/// <summary>
/// Integration tests for the AI Actor System
/// </summary>
public class AIActorSystemIntegrationTests : TestKit
{
    private readonly ILogger<AIActorSystem> _logger;

    public AIActorSystemIntegrationTests()
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<AIActorSystem>();
    }

    [Fact]
    public async Task AIActorSystem_Should_Initialize_Successfully()
    {
        // Arrange
        var configuration = new ActorSystemConfiguration();
        var aiActorSystem = new AIActorSystem(configuration, _logger);

        // Act
        await aiActorSystem.InitializeAsync();

        // Assert
        Assert.NotNull(aiActorSystem.ActorSystem);
        Assert.Equal("GameConsole-AI", aiActorSystem.ActorSystem.Name);
    }

    [Fact]
    public async Task AIActorSystem_Should_Start_And_Stop_Successfully()
    {
        // Arrange
        var configuration = new ActorSystemConfiguration
        {
            Cluster = new ClusterConfiguration
            {
                SeedNodes = new List<string>() // Empty to avoid clustering in tests
            }
        };
        var aiActorSystem = new AIActorSystem(configuration, _logger);

        // Act & Assert
        await aiActorSystem.InitializeAsync();
        Assert.False(aiActorSystem.IsRunning);

        await aiActorSystem.StartAsync();
        Assert.True(aiActorSystem.IsRunning);

        await aiActorSystem.StopAsync();
        Assert.False(aiActorSystem.IsRunning);
    }

    [Fact]
    public async Task AIActorSystem_Should_Create_And_Stop_Agents()
    {
        // Arrange
        var configuration = new ActorSystemConfiguration
        {
            Cluster = new ClusterConfiguration
            {
                SeedNodes = new List<string>() // Empty to avoid clustering in tests
            }
        };
        var aiActorSystem = new AIActorSystem(configuration, _logger);

        await aiActorSystem.InitializeAsync();
        await aiActorSystem.StartAsync();

        // Act
        var createResult = await aiActorSystem.CreateAgentAsync("test-agent", "placeholder", null);
        var stopResult = await aiActorSystem.StopAgentAsync("test-agent");

        // Assert
        Assert.True(createResult);
        Assert.True(stopResult);

        // Cleanup
        await aiActorSystem.StopAsync();
        await aiActorSystem.DisposeAsync();
    }

    [Fact]
    public async Task AIActorSystem_Should_Get_System_Health()
    {
        // Arrange
        var configuration = new ActorSystemConfiguration
        {
            Cluster = new ClusterConfiguration
            {
                SeedNodes = new List<string>() // Empty to avoid clustering in tests
            }
        };
        var aiActorSystem = new AIActorSystem(configuration, _logger);

        await aiActorSystem.InitializeAsync();
        await aiActorSystem.StartAsync();

        // Act
        var health = await aiActorSystem.GetSystemHealthAsync();

        // Assert
        Assert.NotNull(health);
        Assert.True(health.IsHealthy);
        Assert.True(health.Uptime.TotalMilliseconds >= 0);

        // Cleanup
        await aiActorSystem.StopAsync();
        await aiActorSystem.DisposeAsync();
    }

    [Fact]
    public void ActorSystemConfiguration_Should_Build_Valid_Config()
    {
        // Arrange
        var configuration = new ActorSystemConfiguration
        {
            SystemName = "Test-System",
            Cluster = new ClusterConfiguration
            {
                Hostname = "testhost",
                Port = 8080,
                SeedNodes = new List<string> { "akka.tcp://Test-System@testhost:8080" },
                Roles = new List<string> { "test-role" }
            },
            Persistence = new PersistenceConfiguration
            {
                Enabled = true,
                JournalPlugin = "akka.persistence.journal.inmem",
                SnapshotPlugin = "akka.persistence.snapshot-store.inmem"
            }
        };

        // Act
        var config = configuration.BuildConfiguration();

        // Assert
        Assert.NotNull(config);
        Assert.True(config.HasPath("akka.actor.provider"));
        Assert.True(config.HasPath("akka.remote.dot-netty.tcp.hostname"));
        Assert.True(config.HasPath("akka.cluster.seed-nodes"));
        Assert.True(config.HasPath("akka.persistence.journal.plugin"));
    }

    [Fact]
    public async Task AIActorSystem_Should_Handle_Duplicate_Agent_Creation()
    {
        // Arrange
        var configuration = new ActorSystemConfiguration
        {
            Cluster = new ClusterConfiguration
            {
                SeedNodes = new List<string>() // Empty to avoid clustering in tests
            }
        };
        var aiActorSystem = new AIActorSystem(configuration, _logger);

        await aiActorSystem.InitializeAsync();
        await aiActorSystem.StartAsync();

        // Act
        var firstResult = await aiActorSystem.CreateAgentAsync("duplicate-agent", "placeholder", null);
        var secondResult = await aiActorSystem.CreateAgentAsync("duplicate-agent", "placeholder", null);

        // Assert
        Assert.True(firstResult);
        Assert.False(secondResult); // Should fail for duplicate

        // Cleanup
        await aiActorSystem.StopAgentAsync("duplicate-agent");
        await aiActorSystem.StopAsync();
        await aiActorSystem.DisposeAsync();
    }

    [Fact]
    public async Task AIActorSystem_Should_Handle_Stopping_Nonexistent_Agent()
    {
        // Arrange
        var configuration = new ActorSystemConfiguration
        {
            Cluster = new ClusterConfiguration
            {
                SeedNodes = new List<string>() // Empty to avoid clustering in tests
            }
        };
        var aiActorSystem = new AIActorSystem(configuration, _logger);

        await aiActorSystem.InitializeAsync();
        await aiActorSystem.StartAsync();

        // Act
        var result = await aiActorSystem.StopAgentAsync("nonexistent-agent");

        // Assert
        Assert.False(result); // Should fail for nonexistent agent

        // Cleanup
        await aiActorSystem.StopAsync();
        await aiActorSystem.DisposeAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}