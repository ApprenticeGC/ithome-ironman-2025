using Akka.Actor;
using Akka.TestKit.Xunit2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using GameConsole.AI.Actors.Actors;
using GameConsole.AI.Actors.Messages;

namespace GameConsole.AI.Actors.Tests;

/// <summary>
/// Tests for the Agent Director Actor
/// </summary>
public class AgentDirectorActorTests : TestKit
{
    private readonly ILogger<AgentDirectorActor> _logger;

    public AgentDirectorActorTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        var serviceProvider = services.BuildServiceProvider();
        _logger = serviceProvider.GetRequiredService<ILogger<AgentDirectorActor>>();
    }

    [Fact]
    public void AgentDirectorActor_ShouldHandleGetAvailableAgents_WhenNoAgentsRegistered()
    {
        // Arrange
        var agentDirectorProps = Props.Create(() => new AgentDirectorActor(_logger));
        var agentDirector = ActorOf(agentDirectorProps, "agent-director");

        // Act
        agentDirector.Tell(new GetAvailableAgents());

        // Assert
        var response = ExpectMsg<AvailableAgentsResponse>();
        Assert.Empty(response.AgentIds);
    }

    [Fact]
    public void AgentDirectorActor_ShouldRegisterAndUnregisterAgent()
    {
        // Arrange
        var agentDirectorProps = Props.Create(() => new AgentDirectorActor(_logger));
        var agentDirector = ActorOf(agentDirectorProps, "agent-director");

        var mockAgent = ActorOf(Props.Create(() => new MockAIActor()), "mock-agent");
        var metadata = new AgentMetadata("mock-agent", "Mock Agent", "Test agent", "1.0.0", new[] { "test" }, true);

        // Act - Register Agent
        agentDirector.Tell(new RegisterAgent("mock-agent", mockAgent, metadata));
        var registerResponse = ExpectMsg<AgentRegistered>();
        Assert.True(registerResponse.Success);
        Assert.Equal("mock-agent", registerResponse.AgentId);

        // Verify agent is available
        agentDirector.Tell(new GetAvailableAgents());
        var availableResponse = ExpectMsg<AvailableAgentsResponse>();
        Assert.Single(availableResponse.AgentIds);
        Assert.Contains("mock-agent", availableResponse.AgentIds);

        // Act - Unregister Agent
        agentDirector.Tell(new UnregisterAgent("mock-agent"));
        var unregisterResponse = ExpectMsg<AgentUnregistered>();
        Assert.True(unregisterResponse.Success);
        Assert.Equal("mock-agent", unregisterResponse.AgentId);

        // Verify agent is no longer available
        agentDirector.Tell(new GetAvailableAgents());
        var finalResponse = ExpectMsg<AvailableAgentsResponse>();
        Assert.Empty(finalResponse.AgentIds);
    }

    [Fact]
    public void AgentDirectorActor_ShouldRouteInvokeAgentMessage()
    {
        // Arrange
        var agentDirectorProps = Props.Create(() => new AgentDirectorActor(_logger));
        var agentDirector = ActorOf(agentDirectorProps, "agent-director");

        var mockAgent = ActorOf(Props.Create(() => new MockAIActor()), "mock-agent");
        var metadata = new AgentMetadata("mock-agent", "Mock Agent", "Test agent", "1.0.0", new[] { "test" }, true);

        // Register the agent
        agentDirector.Tell(new RegisterAgent("mock-agent", mockAgent, metadata));
        ExpectMsg<AgentRegistered>();

        // Act
        agentDirector.Tell(new InvokeAgent("mock-agent", "test input"));

        // Assert
        var response = ExpectMsg<AgentResponse>();
        Assert.Equal("mock-agent", response.AgentId);
        Assert.True(response.Success);
        Assert.Equal("Mock response to: test input", response.Output);
    }
}

/// <summary>
/// Mock AI Actor for testing purposes
/// </summary>
public class MockAIActor : BaseAIActor
{
    public MockAIActor() : base(new MockLogger())
    {
    }

    protected override AgentResponse ProcessInvokeAgent(InvokeAgent message)
    {
        return new AgentResponse(message.AgentId, $"Mock response to: {message.Input}", true);
    }

    protected override void ProcessStreamAgent(StreamAgent message)
    {
        Sender.Tell(new AgentStreamChunk(message.AgentId, $"Mock stream chunk: {message.Input}", true));
    }

    protected override AgentMetadata GetAgentMetadata()
    {
        return new AgentMetadata("mock-agent", "Mock Agent", "Test agent", "1.0.0", new[] { "test" }, true);
    }
}

/// <summary>
/// Mock logger for testing
/// </summary>
public class MockLogger : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}