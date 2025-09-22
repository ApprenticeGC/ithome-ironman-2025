using GameConsole.AI.Discovery;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameConsole.AI.Discovery.Tests;

public class AIAgentDiscoveryTests
{
    private readonly AIAgentDiscovery _discovery;

    public AIAgentDiscoveryTests()
    {
        _discovery = new AIAgentDiscovery(NullLogger<AIAgentDiscovery>.Instance);
    }

    [Fact]
    public void IsValidAgentType_WithValidAgent_ReturnsTrue()
    {
        // Arrange
        var agentType = typeof(TestAIAgent);

        // Act
        var result = _discovery.IsValidAgentType(agentType);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidAgentType_WithNullType_ReturnsFalse()
    {
        // Act
        var result = _discovery.IsValidAgentType(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidAgentType_WithAbstractType_ReturnsFalse()
    {
        // Arrange
        var agentType = typeof(AbstractTestAgent);

        // Act
        var result = _discovery.IsValidAgentType(agentType);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidAgentType_WithInterface_ReturnsFalse()
    {
        // Arrange
        var agentType = typeof(IAIAgent);

        // Act
        var result = _discovery.IsValidAgentType(agentType);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidAgentType_WithNonAgentType_ReturnsFalse()
    {
        // Arrange
        var agentType = typeof(string);

        // Act
        var result = _discovery.IsValidAgentType(agentType);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ExtractAgentMetadata_WithAttributedAgent_ExtractsCorrectMetadata()
    {
        // Arrange
        var agentType = typeof(TestAIAgent);

        // Act
        var metadata = _discovery.ExtractAgentMetadata(agentType);

        // Assert
        Assert.Equal("test-agent-1", metadata.Id);
        Assert.Equal("Test Agent", metadata.Name);
        Assert.Equal("A test agent for unit testing", metadata.Description);
        Assert.Equal("1.0.0", metadata.Version);
        Assert.Equal(5, metadata.Priority);
        Assert.Contains("test", metadata.Tags);
        Assert.Contains("basic", metadata.Tags);
        Assert.Equal(agentType, metadata.AgentType);
        Assert.Equal(1024 * 1024, metadata.ResourceRequirements.MinMemoryBytes);
        Assert.Equal(1, metadata.ResourceRequirements.RequiredCpuCores);
        Assert.False(metadata.ResourceRequirements.RequiresGpu);
        Assert.Equal(NetworkAccessLevel.None, metadata.ResourceRequirements.NetworkAccess);
    }

    [Fact]
    public void ExtractAgentMetadata_WithBasicAgent_ExtractsDefaultMetadata()
    {
        // Arrange
        var agentType = typeof(BasicTestAgent);

        // Act
        var metadata = _discovery.ExtractAgentMetadata(agentType);

        // Assert
        Assert.Equal("GameConsole.AI.Discovery.Tests.BasicTestAgent", metadata.Id);
        Assert.Equal("BasicTestAgent", metadata.Name);
        Assert.Null(metadata.Description);
        Assert.Null(metadata.Version);
        Assert.Equal(0, metadata.Priority);
        Assert.Empty(metadata.Tags);
        Assert.Equal(agentType, metadata.AgentType);
    }

    [Fact]
    public async Task DiscoverAgentsAsync_WithNonExistentDirectory_ReturnsEmpty()
    {
        // Arrange
        var searchPath = "/non/existent/path";

        // Act
        var result = await _discovery.DiscoverAgentsAsync(searchPath);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("")]
    public async Task DiscoverAgentsAsync_WithInvalidPath_ThrowsException(string searchPath)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _discovery.DiscoverAgentsAsync(searchPath));
    }

    [Fact]
    public async Task DiscoverAgentsAsync_WithNullPath_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _discovery.DiscoverAgentsAsync(null!));
    }

    [Theory]
    [InlineData("")]
    public async Task DiscoverAgentsInAssemblyAsync_WithInvalidPath_ThrowsException(string assemblyPath)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _discovery.DiscoverAgentsInAssemblyAsync(assemblyPath));
    }

    [Fact]
    public async Task DiscoverAgentsInAssemblyAsync_WithNullPath_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _discovery.DiscoverAgentsInAssemblyAsync(null!));
    }

    [Fact]
    public void ExtractAgentMetadata_WithNullType_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _discovery.ExtractAgentMetadata(null!));
    }
}

// Helper abstract class for testing
public abstract class AbstractTestAgent : IAIAgent
{
    public abstract string Id { get; }
    public abstract string Name { get; }
    public abstract AgentStatus Status { get; }

    public abstract Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default);
    public abstract Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default);
    public abstract Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class;
    public abstract Task InitializeAsync(AgentInitializationContext context, CancellationToken cancellationToken = default);
    public abstract Task ShutdownAsync(CancellationToken cancellationToken = default);
    public abstract Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
}