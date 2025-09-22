using GameConsole.AI.Core;
using GameConsole.Plugins.Core;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameConsole.AI.Core.Tests;

/// <summary>
/// Tests for the AIAgentRegistry functionality.
/// </summary>
public class AIAgentRegistryTests
{
    private readonly IServiceProvider _serviceProvider;

    public AIAgentRegistryTests()
    {
        var services = new ServiceCollection();
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task DiscoverAgentsAsync_Should_Find_Agents_In_Assembly()
    {
        // Arrange
        var registry = new AIAgentRegistry(_serviceProvider);
        var assembly = typeof(AIAgentRegistryTests).Assembly;

        // Act
        var agents = await registry.DiscoverAgentsAsync(assembly);

        // Assert
        Assert.NotEmpty(agents);
        Assert.Contains(agents, a => a.Metadata.Id == "test.example");
    }

    [Fact]
    public async Task RegisterAgentAsync_Should_Add_Agent_To_Registry()
    {
        // Arrange
        var registry = new AIAgentRegistry(_serviceProvider);
        var metadata = CreateTestMetadata("test.register", "Register Test");
        var descriptor = new AIAgentDescriptor(metadata, typeof(TestAIAgent), typeof(AIAgentRegistryTests).Assembly);

        // Act
        await registry.RegisterAgentAsync(descriptor);

        // Assert
        Assert.True(registry.IsAgentRegistered("test.register"));
        Assert.NotNull(registry.GetAgent("test.register"));
    }

    [Fact]
    public async Task UnregisterAgentAsync_Should_Remove_Agent_From_Registry()
    {
        // Arrange
        var registry = new AIAgentRegistry(_serviceProvider);
        var metadata = CreateTestMetadata("test.unregister", "Unregister Test");
        var descriptor = new AIAgentDescriptor(metadata, typeof(TestAIAgent), typeof(AIAgentRegistryTests).Assembly);
        await registry.RegisterAgentAsync(descriptor);

        // Act
        var result = await registry.UnregisterAgentAsync("test.unregister");

        // Assert
        Assert.True(result);
        Assert.False(registry.IsAgentRegistered("test.unregister"));
        Assert.Null(registry.GetAgent("test.unregister"));
    }

    [Fact]
    public async Task FindAgentsByCapabilities_Should_Return_Matching_Agents()
    {
        // Arrange
        var registry = new AIAgentRegistry(_serviceProvider);
        var metadata1 = CreateTestMetadata("test.chat", "Chat Agent", capabilities: new[] { "chat", "nlp" });
        var metadata2 = CreateTestMetadata("test.analysis", "Analysis Agent", capabilities: new[] { "analysis", "nlp" });
        var descriptor1 = new AIAgentDescriptor(metadata1, typeof(TestAIAgent), typeof(AIAgentRegistryTests).Assembly);
        var descriptor2 = new AIAgentDescriptor(metadata2, typeof(TestAIAgent), typeof(AIAgentRegistryTests).Assembly);
        
        await registry.RegisterAgentAsync(descriptor1);
        await registry.RegisterAgentAsync(descriptor2);

        // Act
        var nlpAgents = registry.FindAgentsByCapabilities("nlp");
        var chatAgents = registry.FindAgentsByCapabilities("chat");

        // Assert
        Assert.Equal(2, nlpAgents.Count);
        Assert.Single(chatAgents);
        Assert.Equal("test.chat", chatAgents.First().Metadata.Id);
    }

    [Fact]
    public async Task FindAgentsByType_Should_Return_Matching_Agents()
    {
        // Arrange
        var registry = new AIAgentRegistry(_serviceProvider);
        var metadata1 = CreateTestMetadata("test.conversational1", "Chat Agent 1", agentType: "conversational");
        var metadata2 = CreateTestMetadata("test.conversational2", "Chat Agent 2", agentType: "conversational");
        var metadata3 = CreateTestMetadata("test.automation", "Automation Agent", agentType: "automation");
        var descriptor1 = new AIAgentDescriptor(metadata1, typeof(TestAIAgent), typeof(AIAgentRegistryTests).Assembly);
        var descriptor2 = new AIAgentDescriptor(metadata2, typeof(TestAIAgent), typeof(AIAgentRegistryTests).Assembly);
        var descriptor3 = new AIAgentDescriptor(metadata3, typeof(TestAIAgent), typeof(AIAgentRegistryTests).Assembly);
        
        await registry.RegisterAgentAsync(descriptor1);
        await registry.RegisterAgentAsync(descriptor2);
        await registry.RegisterAgentAsync(descriptor3);

        // Act
        var conversationalAgents = registry.FindAgentsByType("conversational");
        var automationAgents = registry.FindAgentsByType("automation");

        // Assert
        Assert.Equal(2, conversationalAgents.Count);
        Assert.Single(automationAgents);
    }

    [Fact]
    public async Task CreateAgentAsync_Should_Create_Valid_Agent_Instance()
    {
        // Arrange
        var registry = new AIAgentRegistry(_serviceProvider);
        var metadata = CreateTestMetadata("test.create", "Create Test");
        var descriptor = new AIAgentDescriptor(metadata, typeof(TestAIAgent), typeof(AIAgentRegistryTests).Assembly);
        await registry.RegisterAgentAsync(descriptor);

        // Act
        var agent = await registry.CreateAgentAsync("test.create");

        // Assert
        Assert.NotNull(agent);
        Assert.IsType<TestAIAgent>(agent);
        Assert.Equal("test.create", agent.Metadata.Id);
    }

    private static AIAgentMetadata CreateTestMetadata(
        string id, 
        string name, 
        string agentType = "test",
        string[]? capabilities = null,
        string[]? protocols = null)
    {
        var resourceRequirements = new AIAgentResourceRequirements(64, 256, false, false, 1);
        return new AIAgentMetadata(
            id,
            name,
            new Version(1, 0, 0),
            "Test agent for unit tests",
            "Test Team",
            agentType,
            capabilities ?? Array.Empty<string>(),
            resourceRequirements,
            protocols ?? new[] { "test" },
            false,
            Array.Empty<string>());
    }

    #region Test Implementation Classes

    [AIAgent("test.example", "Example Test Agent", "1.0.0", "An example AI agent for testing", "Test Team", "example",
        Capabilities = new[] { "test", "example" },
        SupportedProtocols = new[] { "test" })]
    public class TestAIAgent : IAIAgent
    {
        public IAIAgentMetadata Metadata { get; private set; }
        IPluginMetadata IPlugin.Metadata => Metadata;
        public IPluginContext? Context { get; set; }
        public bool IsRunning { get; private set; }

        public TestAIAgent()
        {
            // Initialize metadata from attribute
            InitializeMetadata();
        }

        public Task ConfigureAsync(IPluginContext context, CancellationToken cancellationToken = default)
        {
            Context = context;
            // Metadata is already set in constructor
            return Task.CompletedTask;
        }

        private void InitializeMetadata()
        {
            var attribute = GetType().GetCustomAttributes(typeof(AIAgentAttribute), false).FirstOrDefault() as AIAgentAttribute;
            if (attribute != null)
            {
                var resourceRequirements = new AIAgentResourceRequirements(
                    attribute.MinMemoryMB,
                    attribute.RecommendedMemoryMB,
                    attribute.RequiresGPU,
                    attribute.RequiresNetwork,
                    attribute.MaxConcurrentInstances);

                Metadata = new AIAgentMetadata(
                    attribute.Id,
                    attribute.Name,
                    Version.Parse(attribute.Version),
                    attribute.Description,
                    attribute.Author,
                    attribute.AgentType,
                    attribute.Capabilities,
                    resourceRequirements,
                    attribute.SupportedProtocols,
                    attribute.SupportsLearning,
                    attribute.Dependencies);
            }
            else
            {
                // Default metadata if no attribute found
                var defaultResourceRequirements = new AIAgentResourceRequirements();
                Metadata = new AIAgentMetadata(
                    "test.default",
                    "Test Agent",
                    new Version(1, 0, 0),
                    "Default test agent",
                    "Test Team",
                    "test",
                    Array.Empty<string>(),
                    defaultResourceRequirements,
                    new[] { "test" },
                    false,
                    Array.Empty<string>());
            }
        }

        public Task<bool> CanUnloadAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(!IsRunning);
        }

        public Task PrepareUnloadAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
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

        public ValueTask DisposeAsync()
        {
            IsRunning = false;
            return ValueTask.CompletedTask;
        }

        public Task<IAIAgentResponse> ProcessRequestAsync(IAIAgentRequest request, CancellationToken cancellationToken = default)
        {
            var response = new TestAIAgentResponse(request.RequestId, true, "Test response", null, new Dictionary<string, object>(), DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(10));
            return Task.FromResult<IAIAgentResponse>(response);
        }

        public Task TrainAsync(IAIAgentTrainingData trainingData, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<IAIAgentStatus> GetStatusAsync(CancellationToken cancellationToken = default)
        {
            var status = new TestAIAgentStatus(true, "idle", 0, 0, 64, 0, DateTimeOffset.UtcNow, new Dictionary<string, object>());
            return Task.FromResult<IAIAgentStatus>(status);
        }

        public Task<IAIAgentDiagnosticResult> RunDiagnosticsAsync(IAIAgentDiagnosticRequest diagnosticRequest, CancellationToken cancellationToken = default)
        {
            var result = new TestDiagnosticResult(diagnosticRequest.DiagnosticType, true, "Test result", null, new Dictionary<string, object>(), DateTimeOffset.UtcNow);
            return Task.FromResult<IAIAgentDiagnosticResult>(result);
        }
    }

    public class TestAIAgentResponse : IAIAgentResponse
    {
        public TestAIAgentResponse(string requestId, bool success, object? content, string? errorMessage, IReadOnlyDictionary<string, object> metadata, DateTimeOffset timestamp, TimeSpan processingTime)
        {
            RequestId = requestId;
            Success = success;
            Content = content;
            ErrorMessage = errorMessage;
            Metadata = metadata;
            Timestamp = timestamp;
            ProcessingTime = processingTime;
        }

        public string RequestId { get; }
        public bool Success { get; }
        public object? Content { get; }
        public string? ErrorMessage { get; }
        public IReadOnlyDictionary<string, object> Metadata { get; }
        public DateTimeOffset Timestamp { get; }
        public TimeSpan ProcessingTime { get; }
    }

    public class TestAIAgentStatus : IAIAgentStatus
    {
        public TestAIAgentStatus(bool isHealthy, string state, long requestsProcessed, double averageResponseTimeMs, double memoryUsageMB, double cpuUsagePercent, DateTimeOffset lastActivity, IReadOnlyDictionary<string, object> additionalMetrics)
        {
            IsHealthy = isHealthy;
            State = state;
            RequestsProcessed = requestsProcessed;
            AverageResponseTimeMs = averageResponseTimeMs;
            MemoryUsageMB = memoryUsageMB;
            CpuUsagePercent = cpuUsagePercent;
            LastActivity = lastActivity;
            AdditionalMetrics = additionalMetrics;
        }

        public bool IsHealthy { get; }
        public string State { get; }
        public long RequestsProcessed { get; }
        public double AverageResponseTimeMs { get; }
        public double MemoryUsageMB { get; }
        public double CpuUsagePercent { get; }
        public DateTimeOffset LastActivity { get; }
        public IReadOnlyDictionary<string, object> AdditionalMetrics { get; }
    }

    public class TestDiagnosticResult : IAIAgentDiagnosticResult
    {
        public TestDiagnosticResult(string diagnosticType, bool passed, object? actualResult, string? errorDetails, IReadOnlyDictionary<string, object> metrics, DateTimeOffset completedAt)
        {
            DiagnosticType = diagnosticType;
            Passed = passed;
            ActualResult = actualResult;
            ErrorDetails = errorDetails;
            Metrics = metrics;
            CompletedAt = completedAt;
        }

        public string DiagnosticType { get; }
        public bool Passed { get; }
        public object? ActualResult { get; }
        public string? ErrorDetails { get; }
        public IReadOnlyDictionary<string, object> Metrics { get; }
        public DateTimeOffset CompletedAt { get; }
    }

    #endregion
}