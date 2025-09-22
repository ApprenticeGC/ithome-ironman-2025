using GameConsole.Deployment.Containers.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.Deployment.Containers.Tests;

/// <summary>
/// Tests for the base deployment service to verify common service lifecycle behavior.
/// </summary>
public class BaseDeploymentServiceTests
{
    private readonly TestLogger _logger;

    public BaseDeploymentServiceTests()
    {
        _logger = new TestLogger();
    }

    [Fact]
    public void Constructor_Should_Require_Logger()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestDeploymentService(null!));
    }

    [Fact]
    public void IsRunning_Should_Be_False_Initially()
    {
        // Arrange & Act
        var service = new TestDeploymentService(_logger);

        // Assert
        Assert.False(service.IsRunning);
    }

    [Fact]
    public async Task InitializeAsync_Should_Call_OnInitializeAsync()
    {
        // Arrange
        var service = new TestDeploymentService(_logger);

        // Act
        await service.InitializeAsync();

        // Assert
        Assert.True(service.InitializeCalled);
    }

    [Fact]
    public async Task StartAsync_Should_Set_IsRunning_True_And_Call_OnStartAsync()
    {
        // Arrange
        var service = new TestDeploymentService(_logger);

        // Act
        await service.StartAsync();

        // Assert
        Assert.True(service.IsRunning);
        Assert.True(service.StartCalled);
    }

    [Fact]
    public async Task StopAsync_Should_Set_IsRunning_False_And_Call_OnStopAsync()
    {
        // Arrange
        var service = new TestDeploymentService(_logger);
        await service.StartAsync();

        // Act
        await service.StopAsync();

        // Assert
        Assert.False(service.IsRunning);
        Assert.True(service.StopCalled);
    }

    [Fact]
    public async Task DisposeAsync_Should_Stop_Service_If_Running()
    {
        // Arrange
        var service = new TestDeploymentService(_logger);
        await service.StartAsync();
        Assert.True(service.IsRunning);

        // Act
        await service.DisposeAsync();

        // Assert
        Assert.False(service.IsRunning);
        Assert.True(service.StopCalled);
        Assert.True(service.DisposeCalled);
    }

    [Fact]
    public async Task InitializeAsync_Should_Log_Information()
    {
        // Arrange
        var service = new TestDeploymentService(_logger);

        // Act
        await service.InitializeAsync();

        // Assert
        Assert.Contains(_logger.LoggedMessages, m => m.Contains("Initializing TestDeploymentService"));
    }

    [Fact]
    public async Task StartAsync_Should_Log_Information()
    {
        // Arrange
        var service = new TestDeploymentService(_logger);

        // Act
        await service.StartAsync();

        // Assert
        Assert.Contains(_logger.LoggedMessages, m => m.Contains("Starting TestDeploymentService"));
    }

    [Fact]
    public async Task StopAsync_Should_Log_Information()
    {
        // Arrange
        var service = new TestDeploymentService(_logger);
        await service.StartAsync();

        // Act
        await service.StopAsync();

        // Assert
        Assert.Contains(_logger.LoggedMessages, m => m.Contains("Stopping TestDeploymentService"));
    }

    [Fact]
    public async Task ThrowIfDisposed_Should_Throw_After_Dispose()
    {
        // Arrange
        var service = new TestDeploymentService(_logger);
        await service.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() => service.InitializeAsync());
    }

    [Fact]
    public async Task InitializeAsync_Should_Handle_Exception_In_OnInitialize()
    {
        // Arrange
        var service = new TestDeploymentService(_logger, shouldThrowOnInitialize: true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.InitializeAsync());
        Assert.Equal("Initialize failed", exception.Message);
    }

    [Fact]
    public async Task StartAsync_Should_Handle_Exception_In_OnStart()
    {
        // Arrange
        var service = new TestDeploymentService(_logger, shouldThrowOnStart: true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.StartAsync());
        Assert.Equal("Start failed", exception.Message);
        Assert.False(service.IsRunning); // Should not be running if start failed
    }

    // Test logger implementation
    public class TestLogger : ILogger<TestDeploymentService>
    {
        public List<string> LoggedMessages { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            LoggedMessages.Add(message);
        }
    }

    // Test implementation class
    public class TestDeploymentService : BaseDeploymentService
    {
        private readonly bool _shouldThrowOnInitialize;
        private readonly bool _shouldThrowOnStart;

        public bool InitializeCalled { get; private set; }
        public bool StartCalled { get; private set; }
        public bool StopCalled { get; private set; }
        public bool DisposeCalled { get; private set; }

        public TestDeploymentService(ILogger<TestDeploymentService> logger, 
            bool shouldThrowOnInitialize = false, 
            bool shouldThrowOnStart = false) 
            : base(logger)
        {
            _shouldThrowOnInitialize = shouldThrowOnInitialize;
            _shouldThrowOnStart = shouldThrowOnStart;
        }

        protected override Task OnInitializeAsync(CancellationToken cancellationToken = default)
        {
            InitializeCalled = true;
            
            if (_shouldThrowOnInitialize)
            {
                throw new InvalidOperationException("Initialize failed");
            }
            
            return Task.CompletedTask;
        }

        protected override Task OnStartAsync(CancellationToken cancellationToken = default)
        {
            StartCalled = true;
            
            if (_shouldThrowOnStart)
            {
                throw new InvalidOperationException("Start failed");
            }
            
            return Task.CompletedTask;
        }

        protected override Task OnStopAsync(CancellationToken cancellationToken = default)
        {
            StopCalled = true;
            return Task.CompletedTask;
        }

        protected override ValueTask OnDisposeAsync()
        {
            DisposeCalled = true;
            return ValueTask.CompletedTask;
        }
    }
}