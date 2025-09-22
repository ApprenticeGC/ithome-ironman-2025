using GameConsole.AI.Services;
using GameConsole.AI.Services.Implementation;
using Microsoft.Extensions.Logging;
using Moq;

namespace GameConsole.AI.Services.Tests;

/// <summary>
/// Integration tests for BasicAIService focusing on agent discovery and registration functionality.
/// These tests validate RFC-007-02 implementation.
/// </summary>
public class BasicAIServiceTests : IAsyncDisposable
{
    private readonly BasicAIService _service;
    private readonly Mock<ILogger<BasicAIService>> _mockLogger;

    public BasicAIServiceTests()
    {
        _mockLogger = new Mock<ILogger<BasicAIService>>();
        _service = new BasicAIService(_mockLogger.Object);
    }

    public async ValueTask DisposeAsync()
    {
        await _service.DisposeAsync();
    }

    [Fact]
    public async Task InitializeAsync_RegistersDefaultAgents()
    {
        // Act
        await _service.InitializeAsync();

        // Assert
        var agents = _service.GetAvailableAgents();
        Assert.NotEmpty(agents);
        Assert.Contains("text-generator", agents);
        Assert.Contains("code-assistant", agents);
        Assert.Contains("dialogue-master", agents);
    }

    [Fact]
    public async Task StartAsync_SetsIsRunningTrue()
    {
        // Arrange
        await _service.InitializeAsync();
        Assert.False(_service.IsRunning);

        // Act
        await _service.StartAsync();

        // Assert
        Assert.True(_service.IsRunning);
    }

    [Fact]
    public async Task StopAsync_SetsIsRunningFalse()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        Assert.True(_service.IsRunning);

        // Act
        await _service.StopAsync();

        // Assert
        Assert.False(_service.IsRunning);
    }

    [Fact]
    public async Task GetAvailableAgents_ReturnsRegisteredAgentIds()
    {
        // Arrange
        await _service.InitializeAsync();

        // Act
        var agents = _service.GetAvailableAgents().ToList();

        // Assert
        Assert.Equal(3, agents.Count);
        Assert.All(agents, agentId => Assert.False(string.IsNullOrWhiteSpace(agentId)));
    }

    [Fact]
    public async Task GetAgentInfoAsync_ExistingAgent_ReturnsMetadata()
    {
        // Arrange
        await _service.InitializeAsync();
        const string agentId = "text-generator";

        // Act
        var metadata = await _service.GetAgentInfoAsync(agentId);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(agentId, metadata.Id);
        Assert.Equal("Text Generator", metadata.Name);
        Assert.True(metadata.Capabilities.HasFlag(AgentCapabilities.TextGeneration));
        Assert.NotEmpty(metadata.Properties);
    }

    [Fact]
    public async Task GetAgentInfoAsync_NonExistentAgent_ReturnsNull()
    {
        // Arrange
        await _service.InitializeAsync();

        // Act
        var metadata = await _service.GetAgentInfoAsync("non-existent");

        // Assert
        Assert.Null(metadata);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task GetAgentInfoAsync_InvalidAgentId_ThrowsArgumentException(string agentId)
    {
        // Arrange
        await _service.InitializeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetAgentInfoAsync(agentId));
    }

    [Fact]
    public async Task RegisterAgentAsync_NewAgent_ReturnsTrue()
    {
        // Arrange
        await _service.InitializeAsync();
        var newAgent = new AgentMetadata(
            "test-agent",
            "Test Agent",
            "A test agent for unit testing",
            AgentCapabilities.TextGeneration);

        // Act
        var result = await _service.RegisterAgentAsync(newAgent);

        // Assert
        Assert.True(result);
        
        var agents = _service.GetAvailableAgents();
        Assert.Contains("test-agent", agents);
        
        var retrievedAgent = await _service.GetAgentInfoAsync("test-agent");
        Assert.NotNull(retrievedAgent);
        Assert.Equal("Test Agent", retrievedAgent.Name);
    }

    [Fact]
    public async Task RegisterAgentAsync_DuplicateAgent_ReturnsFalse()
    {
        // Arrange
        await _service.InitializeAsync();
        var agent = new AgentMetadata(
            "duplicate-agent",
            "Duplicate Agent",
            "An agent to test duplicate registration",
            AgentCapabilities.TextGeneration);

        // Act
        var firstResult = await _service.RegisterAgentAsync(agent);
        var secondResult = await _service.RegisterAgentAsync(agent);

        // Assert
        Assert.True(firstResult);
        Assert.False(secondResult);
    }

    [Fact]
    public async Task RegisterAgentAsync_NullAgent_ThrowsArgumentNullException()
    {
        // Arrange
        await _service.InitializeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.RegisterAgentAsync(null!));
    }

    [Fact]
    public async Task UnregisterAgentAsync_ExistingAgent_ReturnsTrue()
    {
        // Arrange
        await _service.InitializeAsync();
        var agent = new AgentMetadata(
            "temp-agent",
            "Temporary Agent",
            "An agent to test unregistration",
            AgentCapabilities.TextGeneration);
        
        await _service.RegisterAgentAsync(agent);
        Assert.Contains("temp-agent", _service.GetAvailableAgents());

        // Act
        var result = await _service.UnregisterAgentAsync("temp-agent");

        // Assert
        Assert.True(result);
        Assert.DoesNotContain("temp-agent", _service.GetAvailableAgents());
    }

    [Fact]
    public async Task UnregisterAgentAsync_NonExistentAgent_ReturnsFalse()
    {
        // Arrange
        await _service.InitializeAsync();

        // Act
        var result = await _service.UnregisterAgentAsync("non-existent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task InvokeAgentAsync_ExistingAgent_ReturnsResponse()
    {
        // Arrange
        await _service.InitializeAsync();
        const string agentId = "text-generator";
        const string input = "Hello world";

        // Act
        var response = await _service.InvokeAgentAsync(agentId, input);

        // Assert
        Assert.NotNull(response);
        Assert.Contains("Text Generator", response);
        Assert.Contains(input, response);
        Assert.Contains("TextGeneration", response);
    }

    [Fact]
    public async Task InvokeAgentAsync_NonExistentAgent_ThrowsInvalidOperationException()
    {
        // Arrange
        await _service.InitializeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.InvokeAgentAsync("non-existent", "test"));
    }

    [Fact]
    public async Task StreamAgentAsync_ExistingAgent_ReturnsStreamingResponse()
    {
        // Arrange
        await _service.InitializeAsync();
        const string agentId = "text-generator";
        const string input = "Stream test";

        // Act
        var chunks = new List<string>();
        await foreach (var chunk in _service.StreamAgentAsync(agentId, input))
        {
            chunks.Add(chunk);
        }

        // Assert
        Assert.NotEmpty(chunks);
        Assert.All(chunks, chunk => Assert.False(string.IsNullOrEmpty(chunk)));
        
        var fullResponse = string.Join("", chunks);
        Assert.Contains("Text Generator", fullResponse);
        Assert.Contains(input, fullResponse);
    }

    [Fact]
    public async Task CreateConversationAsync_ExistingAgent_ReturnsConversationId()
    {
        // Arrange
        await _service.InitializeAsync();
        const string agentId = "dialogue-master";

        // Act
        var conversationId = await _service.CreateConversationAsync(agentId);

        // Assert
        Assert.NotNull(conversationId);
        Assert.False(string.IsNullOrWhiteSpace(conversationId));
    }

    [Fact]
    public async Task EndConversationAsync_ExistingConversation_ReturnsTrue()
    {
        // Arrange
        await _service.InitializeAsync();
        const string agentId = "dialogue-master";
        var conversationId = await _service.CreateConversationAsync(agentId);

        // Act
        var result = await _service.EndConversationAsync(conversationId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task InvokeInConversationAsync_ExistingConversation_ReturnsResponse()
    {
        // Arrange
        await _service.InitializeAsync();
        const string agentId = "dialogue-master";
        var conversationId = await _service.CreateConversationAsync(agentId);
        const string input = "Hello in conversation";

        // Act
        var response = await _service.InvokeInConversationAsync(conversationId, input);

        // Assert
        Assert.NotNull(response);
        Assert.Contains(conversationId, response);
        Assert.Contains(input, response);
        Assert.Contains("Message #1", response);
    }

    [Fact]
    public void HasCapability_SupportedCapabilities_ReturnsTrue()
    {
        // Act & Assert
        Assert.True(_service.HasCapability<IStreamingCapability>());
        Assert.True(_service.HasCapability<IConversationCapability>());
    }

    [Fact]
    public void GetCapability_SupportedCapabilities_ReturnsService()
    {
        // Act
        var streamingCapability = _service.GetCapability<IStreamingCapability>();
        var conversationCapability = _service.GetCapability<IConversationCapability>();

        // Assert
        Assert.NotNull(streamingCapability);
        Assert.Same(_service, streamingCapability);
        Assert.NotNull(conversationCapability);
        Assert.Same(_service, conversationCapability);
    }
}