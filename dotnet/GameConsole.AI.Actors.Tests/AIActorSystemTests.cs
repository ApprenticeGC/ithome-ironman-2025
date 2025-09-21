using Akka.Actor;
using Akka.TestKit.Xunit2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using GameConsole.AI.Actors.Configuration;
using GameConsole.AI.Actors.System;
using GameConsole.AI.Actors.Messages;

namespace GameConsole.AI.Actors.Tests;

/// <summary>
/// Tests for the AI Actor System
/// </summary>
public class AIActorSystemTests : TestKit
{
    private readonly IServiceProvider _serviceProvider;

    public AIActorSystemTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        services.AddSingleton(new ActorSystemConfiguration());
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task AIActorSystem_ShouldInitializeAndStart()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<AIActorSystem>>();
        var config = _serviceProvider.GetRequiredService<ActorSystemConfiguration>();
        config.SystemName = "TestActorSystem";
        
        var aiActorSystem = new AIActorSystem(logger, config, _serviceProvider);

        // Act & Assert
        await aiActorSystem.InitializeAsync();
        Assert.False(aiActorSystem.IsRunning);

        await aiActorSystem.StartAsync();
        Assert.True(aiActorSystem.IsRunning);

        await aiActorSystem.StopAsync();
        Assert.False(aiActorSystem.IsRunning);
    }

    [Fact]
    public async Task AIActorSystem_ShouldProvideCapabilities()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<AIActorSystem>>();
        var config = _serviceProvider.GetRequiredService<ActorSystemConfiguration>();
        config.SystemName = "TestActorSystem";
        
        var aiActorSystem = new AIActorSystem(logger, config, _serviceProvider);
        await aiActorSystem.InitializeAsync();
        await aiActorSystem.StartAsync();

        // Act
        var capabilities = await aiActorSystem.GetCapabilitiesAsync();
        var hasActorSystemCapability = await aiActorSystem.HasCapabilityAsync<ActorSystem>();
        var actorSystemCapability = await aiActorSystem.GetCapabilityAsync<ActorSystem>();

        // Assert
        Assert.NotEmpty(capabilities);
        Assert.True(hasActorSystemCapability);
        Assert.NotNull(actorSystemCapability);

        // Cleanup
        await aiActorSystem.StopAsync();
    }

    [Fact]
    public async Task AIActorSystem_ShouldCreateSupervisorActors()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<AIActorSystem>>();
        var config = _serviceProvider.GetRequiredService<ActorSystemConfiguration>();
        config.SystemName = "TestActorSystem";
        
        var aiActorSystem = new AIActorSystem(logger, config, _serviceProvider);
        
        // Act
        await aiActorSystem.InitializeAsync();
        await aiActorSystem.StartAsync();

        // Assert
        var agentDirector = aiActorSystem.GetAgentDirector();
        var contextManager = aiActorSystem.GetContextManager();
        var actorSystem = aiActorSystem.GetActorSystem();

        Assert.NotNull(agentDirector);
        Assert.NotNull(contextManager);
        Assert.NotNull(actorSystem);

        // Cleanup
        await aiActorSystem.StopAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            (_serviceProvider as IDisposable)?.Dispose();
        }
        base.Dispose(disposing);
    }
}