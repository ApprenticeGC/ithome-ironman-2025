using GameConsole.AI.Core;
using GameConsole.Plugins.Core;
using Microsoft.Extensions.Logging;
using Moq;

namespace GameConsole.AI.Core.Tests;

/// <summary>
/// Tests for the BaseAIAgent class.
/// </summary>
public class BaseAIAgentTests : IDisposable
{
    private readonly Mock<ILogger<BaseAIAgent>> _loggerMock;
    private readonly TestBaseAIAgent _agent;

    public BaseAIAgentTests()
    {
        _loggerMock = new Mock<ILogger<BaseAIAgent>>();
        _agent = new TestBaseAIAgent(_loggerMock.Object);
    }

    public void Dispose()
    {
        _agent?.DisposeAsync().AsTask().Wait();
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestBaseAIAgent(null!));
    }

    [Fact]
    public void IsRunning_InitialState_ReturnsFalse()
    {
        // Act & Assert
        Assert.False(_agent.IsRunning);
    }

    [Fact]
    public async Task InitializeAsync_SetsInitializedState()
    {
        // Act
        await _agent.InitializeAsync();

        // Assert
        Assert.False(_agent.IsRunning); // Initialized but not started
        Assert.True(_agent.InitializeCalled);
    }

    [Fact]
    public async Task StartAsync_AfterInitialize_SetsRunningState()
    {
        // Arrange
        await _agent.InitializeAsync();

        // Act
        await _agent.StartAsync();

        // Assert
        Assert.True(_agent.IsRunning);
        Assert.True(_agent.StartCalled);
        Assert.True(_agent.State.IsActive);
    }

    [Fact]
    public async Task StopAsync_WhenRunning_StopsAgent()
    {
        // Arrange
        await _agent.InitializeAsync();
        await _agent.StartAsync();
        Assert.True(_agent.IsRunning);

        // Act
        await _agent.StopAsync();

        // Assert
        Assert.False(_agent.IsRunning);
        Assert.True(_agent.StopCalled);
        Assert.False(_agent.State.IsActive);
    }

    [Fact]
    public async Task DisposeAsync_DisposesCorrectly()
    {
        // Arrange
        await _agent.InitializeAsync();
        await _agent.StartAsync();

        // Act
        await _agent.DisposeAsync();

        // Assert
        Assert.False(_agent.IsRunning);
        Assert.True(_agent.DisposeCalled);
    }

    [Fact]
    public async Task ConfigureAsync_SetsContext()
    {
        // Arrange
        var mockContext = new Mock<IPluginContext>();

        // Act
        await _agent.ConfigureAsync(mockContext.Object);

        // Assert
        Assert.Equal(mockContext.Object, _agent.Context);
        Assert.True(_agent.ConfigureCalled);
    }

    [Fact]
    public async Task ConfigureAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _agent.ConfigureAsync(null!));
    }

    [Fact]
    public async Task CanUnloadAsync_DefaultImplementation_ReturnsTrue()
    {
        // Act
        var canUnload = await _agent.CanUnloadAsync();

        // Assert
        Assert.True(canUnload);
    }

    [Fact]
    public async Task PrepareUnloadAsync_CallsOnPrepareUnload()
    {
        // Act
        await _agent.PrepareUnloadAsync();

        // Assert
        Assert.True(_agent.PrepareUnloadCalled);
    }

    [Fact]
    public async Task ProcessAsync_ValidInput_ReturnsResponse()
    {
        // Arrange
        await _agent.InitializeAsync();
        await _agent.StartAsync();

        var input = new AIAgentInput
        {
            InputType = "test",
            Data = "test data",
            Priority = 50
        };

        // Act
        var response = await _agent.ProcessAsync(input);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Equal("test-response", response.ResponseType);
        Assert.Equal(1, _agent.State.DecisionCount);
        Assert.NotNull(_agent.State.LastDecisionTime);
        Assert.True(_agent.DecisionMadeCalled);
    }

    [Fact]
    public async Task ProcessAsync_WithNullInput_ThrowsArgumentNullException()
    {
        // Arrange
        await _agent.InitializeAsync();
        await _agent.StartAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _agent.ProcessAsync(null!));
    }

    [Fact]
    public async Task ProcessAsync_WhenNotActive_ThrowsInvalidOperationException()
    {
        // Arrange - Don't start the agent
        await _agent.InitializeAsync();

        var input = new AIAgentInput
        {
            InputType = "test",
            Data = "test data"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _agent.ProcessAsync(input));
    }

    [Fact]
    public async Task ProcessAsync_WhenProcessingThrows_ReturnsErrorResponse()
    {
        // Arrange
        await _agent.InitializeAsync();
        await _agent.StartAsync();
        
        _agent.ShouldThrowOnProcess = true;

        var input = new AIAgentInput
        {
            InputType = "error-test",
            Data = "test data"
        };

        // Act
        var response = await _agent.ProcessAsync(input);

        // Assert
        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Equal("error", response.ResponseType);
        Assert.NotNull(response.Error);
        Assert.Equal(0.0, response.Confidence);
    }

    [Fact]
    public async Task UpdateAsync_WithLearningSupported_ProcessesFeedback()
    {
        // Arrange
        _agent.SetSupportsLearning(true);
        
        var feedback = new AIAgentFeedback
        {
            ResponseId = "test-response-id",
            FeedbackType = "reward",
            Score = 0.8
        };

        // Act
        await _agent.UpdateAsync(feedback);

        // Assert
        Assert.True(_agent.FeedbackProcessedCalled);
    }

    [Fact]
    public async Task UpdateAsync_WithoutLearningSupport_LogsWarning()
    {
        // Arrange
        _agent.SetSupportsLearning(false);
        
        var feedback = new AIAgentFeedback
        {
            ResponseId = "test-response-id",
            FeedbackType = "reward",
            Score = 0.8
        };

        // Act
        await _agent.UpdateAsync(feedback);

        // Assert
        Assert.False(_agent.FeedbackProcessedCalled);
        // Verify warning was logged (would need to setup logger mock verification)
    }

    [Fact]
    public async Task UpdateAsync_WithNullFeedback_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _agent.UpdateAsync(null!));
    }

    [Fact]
    public async Task ResetAsync_CallsOnReset()
    {
        // Arrange
        await _agent.InitializeAsync();
        await _agent.StartAsync();

        // Process some inputs to increment decision count
        var input = new AIAgentInput { InputType = "test", Data = "data" };
        await _agent.ProcessAsync(input);
        await _agent.ProcessAsync(input);
        
        Assert.Equal(2, _agent.State.DecisionCount);

        // Act
        await _agent.ResetAsync(preserveConfiguration: true);

        // Assert
        Assert.True(_agent.ResetCalled);
        Assert.Equal(0, _agent.State.DecisionCount);
        Assert.Null(_agent.State.LastDecisionTime);
    }

    [Fact]
    public void State_ReflectsCurrentAgentState()
    {
        // Arrange
        var state = _agent.State;

        // Assert
        Assert.False(state.IsActive);
        Assert.False(state.IsLearning);
        Assert.Equal(0, state.DecisionCount);
        Assert.Null(state.LastDecisionTime);
        Assert.NotNull(state.Metrics);
        Assert.NotNull(state.Configuration);
    }

    [Fact]
    public async Task MultipleOperations_InSequence_WorkCorrectly()
    {
        // Arrange & Act
        await _agent.InitializeAsync();
        Assert.True(_agent.InitializeCalled);
        
        await _agent.StartAsync();
        Assert.True(_agent.IsRunning);

        var input = new AIAgentInput { InputType = "test", Data = "data" };
        var response = await _agent.ProcessAsync(input);
        Assert.True(response.Success);

        await _agent.StopAsync();
        Assert.False(_agent.IsRunning);

        await _agent.DisposeAsync();
        Assert.True(_agent.DisposeCalled);

        // Assert - Verify proper sequence was followed
        Assert.True(_agent.InitializeCalled);
        Assert.True(_agent.StartCalled);
        Assert.True(_agent.StopCalled);
        Assert.True(_agent.DisposeCalled);
    }
}

/// <summary>
/// Test implementation of BaseAIAgent for testing purposes.
/// </summary>
internal class TestBaseAIAgent : BaseAIAgent
{
    private readonly Mock<IPluginMetadata> _mockMetadata;
    private readonly Mock<IAIAgentCapabilities> _mockCapabilities;

    public bool InitializeCalled { get; private set; }
    public bool StartCalled { get; private set; }
    public bool StopCalled { get; private set; }
    public bool DisposeCalled { get; private set; }
    public bool ConfigureCalled { get; private set; }
    public bool PrepareUnloadCalled { get; private set; }
    public bool DecisionMadeCalled { get; private set; }
    public bool FeedbackProcessedCalled { get; private set; }
    public bool ResetCalled { get; private set; }
    public bool ShouldThrowOnProcess { get; set; }

    public override IPluginMetadata Metadata => _mockMetadata.Object;
    public override IAIAgentCapabilities Capabilities => _mockCapabilities.Object;

    public TestBaseAIAgent(ILogger<BaseAIAgent> logger) : base(logger)
    {
        _mockMetadata = new Mock<IPluginMetadata>();
        _mockMetadata.Setup(m => m.Name).Returns("TestAgent");
        _mockMetadata.Setup(m => m.Version).Returns(new Version("1.0.0"));
        _mockMetadata.Setup(m => m.Description).Returns("Test AI agent");

        _mockCapabilities = new Mock<IAIAgentCapabilities>();
        _mockCapabilities.Setup(c => c.DecisionTypes).Returns(new[] { "test" });
        _mockCapabilities.Setup(c => c.SupportsLearning).Returns(false);
        _mockCapabilities.Setup(c => c.SupportsAutonomousMode).Returns(false);
        _mockCapabilities.Setup(c => c.Priority).Returns(50);
        _mockCapabilities.Setup(c => c.MaxConcurrentInputs).Returns(1);
        _mockCapabilities.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());
    }

    public void SetSupportsLearning(bool supportsLearning)
    {
        _mockCapabilities.Setup(c => c.SupportsLearning).Returns(supportsLearning);
    }

    protected override void OnInitialize()
    {
        InitializeCalled = true;
        base.OnInitialize();
    }

    protected override void OnStart()
    {
        StartCalled = true;
        base.OnStart();
    }

    protected override void OnStop()
    {
        StopCalled = true;
        base.OnStop();
    }

    protected override void OnDispose()
    {
        DisposeCalled = true;
        base.OnDispose();
    }

    protected override void OnConfigure(IPluginContext context)
    {
        ConfigureCalled = true;
        base.OnConfigure(context);
    }

    protected override void OnPrepareUnload()
    {
        PrepareUnloadCalled = true;
        base.OnPrepareUnload();
    }

    protected override void OnDecisionMade(IAIAgentInput input, IAIAgentResponse response, double processingTimeMs)
    {
        DecisionMadeCalled = true;
        base.OnDecisionMade(input, response, processingTimeMs);
    }

    protected override Task ProcessFeedbackAsync(IAIAgentFeedback feedback, CancellationToken cancellationToken)
    {
        FeedbackProcessedCalled = true;
        return base.ProcessFeedbackAsync(feedback, cancellationToken);
    }

    protected override Task OnResetAsync(bool preserveConfiguration, CancellationToken cancellationToken)
    {
        ResetCalled = true;
        return base.OnResetAsync(preserveConfiguration, cancellationToken);
    }

    protected override Task<IAIAgentResponse> ProcessInputAsync(IAIAgentInput input, CancellationToken cancellationToken)
    {
        if (ShouldThrowOnProcess)
        {
            throw new InvalidOperationException("Test exception during processing");
        }

        return Task.FromResult<IAIAgentResponse>(new AIAgentResponse
        {
            Success = true,
            ResponseType = "test-response",
            Data = $"Processed: {input.Data}",
            Confidence = 1.0,
            Metadata = new Dictionary<string, object>
            {
                ["inputType"] = input.InputType,
                ["timestamp"] = DateTime.UtcNow
            }
        });
    }

    protected override IAIAgentMetrics GetCurrentMetrics()
    {
        return new AIAgentMetrics
        {
            AverageProcessingTimeMs = 10.0,
            SuccessRate = 1.0,
            MemoryUsageBytes = 1024,
            AdditionalMetrics = new Dictionary<string, object>
            {
                ["testMetric"] = "testValue"
            }
        };
    }

    protected override IReadOnlyDictionary<string, object> GetCurrentConfiguration()
    {
        return new Dictionary<string, object>
        {
            ["testConfig"] = "testConfigValue"
        };
    }
}