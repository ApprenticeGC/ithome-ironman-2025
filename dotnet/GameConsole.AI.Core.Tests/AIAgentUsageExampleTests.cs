using GameConsole.AI.Core;
using GameConsole.Plugins.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Xunit;

namespace GameConsole.AI.Core.Tests;

/// <summary>
/// Example implementations and usage tests for AI agent interfaces.
/// These tests demonstrate how the AI agent framework would be used in practice.
/// </summary>
public class AIAgentUsageExampleTests
{
    [Fact]
    public async Task ExampleAIAgent_Should_Support_Full_Lifecycle()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var agent = new ExampleAIAgent();
        var context = new TestPluginContext();
        
        // Act & Assert - Test agent lifecycle
        Assert.False(agent.IsRunning);
        
        // Configure
        await agent.ConfigureAsync(context);
        Assert.Same(context, agent.Context);
        Assert.NotNull(agent.Metadata);
        
        // Initialize
        await agent.InitializeAsync();
        
        // Start
        await agent.StartAsync();
        Assert.True(agent.IsRunning);
        
        // Process a request
        var request = new TestAIAgentRequest("req-1", "query", "Test query", new Dictionary<string, object>(), DateTimeOffset.UtcNow);
        var response = await agent.ProcessRequestAsync(request);
        Assert.True(response.Success);
        Assert.Equal("req-1", response.RequestId);
        
        // Get status
        var status = await agent.GetStatusAsync();
        Assert.True(status.IsHealthy);
        Assert.Equal("running", status.State);
        
        // Run diagnostics
        var diagnosticRequest = new TestDiagnosticRequest("health-check", "ping", null, new Dictionary<string, object>());
        var diagnosticResult = await agent.RunDiagnosticsAsync(diagnosticRequest);
        Assert.True(diagnosticResult.Passed);
        
        // Train (if supported)
        var trainingData = new TestTrainingData("supervised", "test data", new Dictionary<string, object>(), "test", 1.0);
        await agent.TrainAsync(trainingData);
        
        // Check if can unload (should be false while running)
        var canUnloadWhileRunning = await agent.CanUnloadAsync();
        Assert.False(canUnloadWhileRunning);
        
        // Stop
        await agent.StopAsync();
        Assert.False(agent.IsRunning);
        
        // Check if can unload (should be true when stopped)
        var canUnloadWhenStopped = await agent.CanUnloadAsync();
        Assert.True(canUnloadWhenStopped);
        
        // Prepare for unload
        await agent.PrepareUnloadAsync();
        
        // Dispose
        await agent.DisposeAsync();
    }

    [Fact]
    public void ExampleAIAgent_Should_Have_Correct_Metadata_From_Attribute()
    {
        // Arrange
        var agentType = typeof(ExampleAIAgent);
        
        // Act
        var attribute = agentType.GetCustomAttributes(typeof(AIAgentAttribute), false)
            .Cast<AIAgentAttribute>()
            .FirstOrDefault();
        
        // Assert
        Assert.NotNull(attribute);
        Assert.Equal("example.ai.agent", attribute.Id);
        Assert.Equal("Example AI Agent", attribute.Name);
        Assert.Equal("1.0.0", attribute.Version);
        Assert.Equal("An example AI agent for testing", attribute.Description);
        Assert.Equal("Test Team", attribute.Author);
        Assert.Equal("conversational", attribute.AgentType);
        Assert.Contains("chat", attribute.Capabilities);
        Assert.Contains("nlp", attribute.Capabilities);
        Assert.Contains("http", attribute.SupportedProtocols);
        Assert.True(attribute.SupportsLearning);
        Assert.Equal(128, attribute.MinMemoryMB);
        Assert.Equal(512, attribute.RecommendedMemoryMB);
    }

    [Fact]
    public async Task AIAgentRegistry_Should_Support_Discovery_And_Management_Workflow()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var registry = new AIAgentRegistry(serviceProvider);
        var assembly = typeof(AIAgentUsageExampleTests).Assembly;
        
        // Act & Assert - Discovery
        var discoveredAgents = await registry.DiscoverAgentsAsync(assembly);
        Assert.NotEmpty(discoveredAgents);
        
        var exampleAgent = discoveredAgents.FirstOrDefault(a => a.Metadata.Id == "example.ai.agent");
        Assert.NotNull(exampleAgent);
        
        // Register discovered agent
        await registry.RegisterAgentAsync(exampleAgent);
        Assert.True(registry.IsAgentRegistered("example.ai.agent"));
        
        // Search by capabilities
        var chatAgents = registry.FindAgentsByCapabilities("chat");
        Assert.Contains(chatAgents, a => a.Metadata.Id == "example.ai.agent");
        
        // Search by type
        var conversationalAgents = registry.FindAgentsByType("conversational");
        Assert.Contains(conversationalAgents, a => a.Metadata.Id == "example.ai.agent");
        
        // Create agent instance
        var agentInstance = await registry.CreateAgentAsync("example.ai.agent");
        Assert.NotNull(agentInstance);
        Assert.Equal("example.ai.agent", agentInstance.Metadata.Id);
        
        // Unregister
        var unregistered = await registry.UnregisterAgentAsync("example.ai.agent");
        Assert.True(unregistered);
        Assert.False(registry.IsAgentRegistered("example.ai.agent"));
    }

    #region Test Implementation Classes

    [AIAgent("example.ai.agent", "Example AI Agent", "1.0.0", "An example AI agent for testing", "Test Team", "conversational",
        Capabilities = new[] { "chat", "nlp", "reasoning" },
        SupportedProtocols = new[] { "http", "websocket" },
        SupportsLearning = true,
        MinMemoryMB = 128,
        RecommendedMemoryMB = 512,
        RequiresNetwork = true,
        MaxConcurrentInstances = 3)]
    private class ExampleAIAgent : IAIAgent
    {
        public IAIAgentMetadata Metadata { get; private set; }
        IPluginMetadata IPlugin.Metadata => Metadata;
        public IPluginContext? Context { get; set; }
        public bool IsRunning { get; private set; }

        public ExampleAIAgent()
        {
            // Initialize metadata from attribute
            InitializeMetadata();
        }

        public Task ConfigureAsync(IPluginContext context, CancellationToken cancellationToken = default)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            
            // Create metadata from attribute
            var attribute = GetType().GetCustomAttribute<AIAgentAttribute>();
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
            
            return Task.CompletedTask;
        }

        public Task<bool> CanUnloadAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(!IsRunning);
        }

        public Task PrepareUnloadAsync(CancellationToken cancellationToken = default)
        {
            // Cleanup logic would go here
            return Task.CompletedTask;
        }

        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (Context == null)
                throw new InvalidOperationException("Agent must be configured before initialization");
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
            // Simple echo response for testing
            var response = new TestAIAgentResponse(
                request.RequestId,
                true,
                $"Processed: {request.Content}",
                null,
                new Dictionary<string, object> { { "processed_at", DateTimeOffset.UtcNow } },
                DateTimeOffset.UtcNow,
                TimeSpan.FromMilliseconds(50));
            
            return Task.FromResult<IAIAgentResponse>(response);
        }

        public Task TrainAsync(IAIAgentTrainingData trainingData, CancellationToken cancellationToken = default)
        {
            // Simulate training
            return Task.Delay(10, cancellationToken);
        }

        public Task<IAIAgentStatus> GetStatusAsync(CancellationToken cancellationToken = default)
        {
            var status = new TestAIAgentStatus(
                true,
                IsRunning ? "running" : "stopped",
                0,
                50.0,
                128.0,
                5.0,
                DateTimeOffset.UtcNow,
                new Dictionary<string, object>
                {
                    { "version", "1.0.0" },
                    { "uptime", TimeSpan.FromMinutes(5) }
                });
            
            return Task.FromResult<IAIAgentStatus>(status);
        }

        public Task<IAIAgentDiagnosticResult> RunDiagnosticsAsync(IAIAgentDiagnosticRequest diagnosticRequest, CancellationToken cancellationToken = default)
        {
            var result = new TestDiagnosticResult(
                diagnosticRequest.DiagnosticType,
                true,
                "pong",
                null,
                new Dictionary<string, object> { { "response_time_ms", 1 } },
                DateTimeOffset.UtcNow);
            
            return Task.FromResult<IAIAgentDiagnosticResult>(result);
        }
    }

    private class TestPluginContext : IPluginContext
    {
        public IServiceProvider Services { get; } = new TestServiceProvider();
        public IConfiguration Configuration { get; } = new ConfigurationBuilder().Build();
        public string PluginDirectory => "/plugins/ai/example";
        public CancellationToken ShutdownToken => CancellationToken.None;
        public IReadOnlyDictionary<string, object> Properties => 
            new Dictionary<string, object>
            {
                { "HostVersion", "1.0.0" },
                { "Environment", "Test" }
            };
    }

    private class TestServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private class TestAIAgentRequest : IAIAgentRequest
    {
        public TestAIAgentRequest(string requestId, string requestType, object content, IReadOnlyDictionary<string, object> context, DateTimeOffset timestamp)
        {
            RequestId = requestId;
            RequestType = requestType;
            Content = content;
            Context = context;
            Timestamp = timestamp;
        }

        public string RequestId { get; }
        public string RequestType { get; }
        public object Content { get; }
        public IReadOnlyDictionary<string, object> Context { get; }
        public DateTimeOffset Timestamp { get; }
    }

    private class TestAIAgentResponse : IAIAgentResponse
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

    private class TestAIAgentStatus : IAIAgentStatus
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

    private class TestDiagnosticRequest : IAIAgentDiagnosticRequest
    {
        public TestDiagnosticRequest(string diagnosticType, object testData, object? expectedResult, IReadOnlyDictionary<string, object> parameters)
        {
            DiagnosticType = diagnosticType;
            TestData = testData;
            ExpectedResult = expectedResult;
            Parameters = parameters;
        }

        public string DiagnosticType { get; }
        public object TestData { get; }
        public object? ExpectedResult { get; }
        public IReadOnlyDictionary<string, object> Parameters { get; }
    }

    private class TestDiagnosticResult : IAIAgentDiagnosticResult
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

    private class TestTrainingData : IAIAgentTrainingData
    {
        public TestTrainingData(string dataType, object data, IReadOnlyDictionary<string, object> parameters, string source, double qualityScore)
        {
            DataType = dataType;
            Data = data;
            Parameters = parameters;
            Source = source;
            QualityScore = qualityScore;
        }

        public string DataType { get; }
        public object Data { get; }
        public IReadOnlyDictionary<string, object> Parameters { get; }
        public string Source { get; }
        public double QualityScore { get; }
    }

    #endregion
}