using GameConsole.Plugins.Core;
using Xunit;

namespace GameConsole.Plugins.Core.Tests;

public class AIAgentTests
{
    [Fact]
    public void IAIAgentCapability_Properties_ShouldBeSettable()
    {
        // Arrange & Act & Assert
        var capability = new TestAICapability("test-capability", "A test capability");
        
        Assert.Equal("test-capability", capability.CapabilityName);
        Assert.Equal("A test capability", capability.Description);
    }

    [Fact]
    public void AIAgentAttribute_Constructor_ShouldSetAllProperties()
    {
        // Arrange
        const string id = "test-agent";
        const string name = "Test Agent";
        const string version = "1.0.0";
        const string description = "A test AI agent";
        const string author = "Test Author";
        const string aiModel = "GPT-4";

        // Act
        var attribute = new AIAgentAttribute(id, name, version, description, author, aiModel);

        // Assert
        Assert.Equal(id, attribute.Id);
        Assert.Equal(name, attribute.Name);
        Assert.Equal(version, attribute.Version);
        Assert.Equal(description, attribute.Description);
        Assert.Equal(author, attribute.Author);
        Assert.Equal(aiModel, attribute.AIModel);
    }

    [Fact]
    public void AIAgentAttribute_Constructor_WithNullAIModel_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new AIAgentAttribute("id", "name", "1.0.0", "description", "author", null!));
    }

    [Fact]
    public void AIAgentAttribute_OptionalProperties_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var attribute = new AIAgentAttribute("id", "name", "1.0.0", "description", "author", "GPT-4");

        // Assert
        Assert.Empty(attribute.SupportedCapabilities);
        Assert.Equal(5, attribute.PerformanceRating);
        Assert.Equal("Medium", attribute.ResourceRequirements);
        Assert.False(attribute.RequiresInternetConnection);
        Assert.Equal(1, attribute.MaxConcurrentRequests);
    }

    [Fact]
    public void AIAgentAttribute_OptionalProperties_CanBeSet()
    {
        // Arrange
        var attribute = new AIAgentAttribute("id", "name", "1.0.0", "description", "author", "GPT-4")
        {
            SupportedCapabilities = new[] { "chat", "analysis" },
            PerformanceRating = 8,
            ResourceRequirements = "High",
            RequiresInternetConnection = true,
            MaxConcurrentRequests = 10
        };

        // Act & Assert
        Assert.Equal(new[] { "chat", "analysis" }, attribute.SupportedCapabilities);
        Assert.Equal(8, attribute.PerformanceRating);
        Assert.Equal("High", attribute.ResourceRequirements);
        Assert.True(attribute.RequiresInternetConnection);
        Assert.Equal(10, attribute.MaxConcurrentRequests);
    }

    [Fact]
    public async Task TestAIAgent_GetAICapabilitiesAsync_ShouldReturnCapabilities()
    {
        // Arrange
        var agent = new TestAIAgent();

        // Act
        var capabilities = await agent.GetAICapabilitiesAsync();

        // Assert
        Assert.NotNull(capabilities);
        Assert.Single(capabilities);
        Assert.Equal("test-capability", capabilities.First().CapabilityName);
    }

    [Fact]
    public async Task TestAIAgent_CanHandleRequestAsync_WithSupportedRequest_ShouldReturnTrue()
    {
        // Arrange
        var agent = new TestAIAgent();
        var request = "test request";

        // Act
        var canHandle = await agent.CanHandleRequestAsync(request);

        // Assert
        Assert.True(canHandle);
    }

    [Fact]
    public async Task TestAIAgent_ProcessRequestAsync_ShouldReturnProcessedResponse()
    {
        // Arrange
        var agent = new TestAIAgent();
        var request = "hello";

        // Act
        var response = await agent.ProcessRequestAsync<string, string>(request);

        // Assert
        Assert.Equal("Processed: hello", response);
    }
}

// Test implementations
public class TestAICapability : IAIAgentCapability
{
    public TestAICapability(string capabilityName, string description)
    {
        CapabilityName = capabilityName;
        Description = description;
    }

    public string CapabilityName { get; }
    public string Description { get; }
}

[AIAgent("test-agent", "Test AI Agent", "1.0.0", "A test AI agent for unit testing", "Test Author", "Mock AI")]
public class TestAIAgent : IAIAgent
{
    private readonly TestAICapability _capability = new("test-capability", "A test capability");

    public IPluginMetadata Metadata { get; } = new TestPluginMetadata();
    public IPluginContext? Context { get; set; }
    public bool IsRunning { get; private set; }

    public Task<IEnumerable<IAIAgentCapability>> GetAICapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<IAIAgentCapability>>(new[] { _capability });
    }

    public Task<bool> CanHandleRequestAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(request is string);
    }

    public Task<TResponse> ProcessRequestAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
    {
        if (request is string stringRequest && typeof(TResponse) == typeof(string))
        {
            var response = $"Processed: {stringRequest}";
            return Task.FromResult((TResponse)(object)response);
        }

        throw new NotSupportedException($"Cannot process request of type {typeof(TRequest)} to response of type {typeof(TResponse)}");
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        IsRunning = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        IsRunning = false;
        return Task.CompletedTask;
    }

    public Task ConfigureAsync(IPluginContext context, CancellationToken cancellationToken = default)
    {
        Context = context;
        return Task.CompletedTask;
    }

    public Task<bool> CanUnloadAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task PrepareUnloadAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        IsRunning = false;
        return ValueTask.CompletedTask;
    }
}

public class TestPluginMetadata : IPluginMetadata
{
    public string Id => "test-agent";
    public string Name => "Test AI Agent";
    public Version Version => new(1, 0, 0);
    public string Description => "A test AI agent for unit testing";
    public string Author => "Test Author";
    public IReadOnlyList<string> Dependencies => Array.Empty<string>();
    public IReadOnlyDictionary<string, object> Properties => new Dictionary<string, object>();
}