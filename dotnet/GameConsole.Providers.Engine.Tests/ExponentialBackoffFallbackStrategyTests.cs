using GameConsole.Providers.Engine;
using Microsoft.Extensions.Logging;
using Moq;

namespace GameConsole.Providers.Engine.Tests;

/// <summary>
/// Tests for the ExponentialBackoffFallbackStrategy class.
/// </summary>
public class ExponentialBackoffFallbackStrategyTests
{
    private readonly Mock<IProviderSelector> _mockSelector;
    private readonly Mock<IProviderPerformanceMonitor> _mockMonitor;
    private readonly FakeLogger<ExponentialBackoffFallbackStrategy> _logger;
    private readonly ExponentialBackoffFallbackStrategy _strategy;

    public ExponentialBackoffFallbackStrategyTests()
    {
        _mockSelector = new Mock<IProviderSelector>();
        _mockMonitor = new Mock<IProviderPerformanceMonitor>();
        _logger = new FakeLogger<ExponentialBackoffFallbackStrategy>();
        _strategy = new ExponentialBackoffFallbackStrategy(_mockSelector.Object, _mockMonitor.Object, _logger);
    }

    [Fact]
    public async Task ExecuteWithFallbackAsync_ShouldSucceed_OnFirstProvider()
    {
        // Arrange
        var provider = new TestService();
        _mockSelector.Setup(s => s.GetAvailableProvidersAsync<ITestService>(null, default))
            .ReturnsAsync(new[] { provider });

        var operation = new Func<ITestService, CancellationToken, Task<string>>((svc, ct) => 
            Task.FromResult("success"));

        // Act
        var result = await _strategy.ExecuteWithFallbackAsync<ITestService, string>(operation);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("success", result.Result);
        Assert.Equal(1, result.AttemptCount);
        Assert.NotNull(result.SuccessfulProviderId);
        Assert.Empty(result.Errors);

        _mockMonitor.Verify(m => m.RecordSuccessAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), default), Times.Once);
    }

    [Fact]
    public async Task ExecuteWithFallbackAsync_ShouldFallback_WhenFirstProviderFails()
    {
        // Arrange
        var provider1 = new TestService();
        var provider2 = new TestService();
        _mockSelector.Setup(s => s.GetAvailableProvidersAsync<ITestService>(null, default))
            .ReturnsAsync(new[] { provider1, provider2 });

        var callCount = 0;
        var operation = new Func<ITestService, CancellationToken, Task<string>>((svc, ct) =>
        {
            callCount++;
            if (callCount == 1)
                throw new HttpRequestException("First provider failed");
            return Task.FromResult("success");
        });

        // Act
        var result = await _strategy.ExecuteWithFallbackAsync<ITestService, string>(operation);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("success", result.Result);
        Assert.Equal(2, result.AttemptCount);
        Assert.Single(result.Errors);

        _mockMonitor.Verify(m => m.RecordFailureAsync(It.IsAny<string>(), It.IsAny<Exception>(), default), Times.Once);
        _mockMonitor.Verify(m => m.RecordSuccessAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), default), Times.Once);
    }

    [Fact]
    public async Task ExecuteWithFallbackAsync_ShouldFail_WhenAllProvidersFail()
    {
        // Arrange
        var provider1 = new TestService();
        var provider2 = new TestService();
        _mockSelector.Setup(s => s.GetAvailableProvidersAsync<ITestService>(null, default))
            .ReturnsAsync(new[] { provider1, provider2 });

        var operation = new Func<ITestService, CancellationToken, Task<string>>((svc, ct) =>
            throw new HttpRequestException("Provider failed"));

        // Act
        var result = await _strategy.ExecuteWithFallbackAsync<ITestService, string>(operation);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.Result);
        Assert.Equal(2, result.AttemptCount);
        Assert.Equal(2, result.Errors.Count);
        Assert.Null(result.SuccessfulProviderId);

        _mockMonitor.Verify(m => m.RecordFailureAsync(It.IsAny<string>(), It.IsAny<Exception>(), default), Times.Exactly(2));
    }

    [Fact]
    public async Task ExecuteWithFallbackAsync_ShouldReturnEmpty_WhenNoProvidersAvailable()
    {
        // Arrange
        _mockSelector.Setup(s => s.GetAvailableProvidersAsync<ITestService>(null, default))
            .ReturnsAsync(Array.Empty<ITestService>());

        var operation = new Func<ITestService, CancellationToken, Task<string>>((svc, ct) => 
            Task.FromResult("success"));

        // Act
        var result = await _strategy.ExecuteWithFallbackAsync<ITestService, string>(operation);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.Result);
        Assert.Equal(0, result.AttemptCount);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(1, 100)]   // First retry: 100ms
    [InlineData(2, 200)]   // Second retry: 200ms  
    [InlineData(3, 400)]   // Third retry: 400ms
    public void GetRetryDelay_ShouldUseExponentialBackoff(int attemptNumber, int expectedMinDelay)
    {
        // Act
        var delay = _strategy.GetRetryDelay("test-provider", attemptNumber);

        // Assert
        Assert.True(delay.TotalMilliseconds >= expectedMinDelay);
        Assert.True(delay.TotalMilliseconds <= expectedMinDelay * 1.2); // Account for jitter
    }

    [Theory]
    [InlineData(typeof(ArgumentException))]
    [InlineData(typeof(OperationCanceledException))]
    public void ShouldRetry_ShouldReturnFalse_ForNonRetryableExceptions(Type exceptionType)
    {
        // Arrange
        var exception = (Exception)Activator.CreateInstance(exceptionType, "Test error")!;

        // Act
        var shouldRetry = _strategy.ShouldRetry(exception);

        // Assert
        Assert.False(shouldRetry);
    }

    [Fact]
    public void ShouldRetry_ShouldReturnTrue_ForRetryableExceptions()
    {
        // Arrange
        var exception = new HttpRequestException("Network error");

        // Act
        var shouldRetry = _strategy.ShouldRetry(exception);

        // Assert
        Assert.True(shouldRetry);
    }

    [Fact]
    public async Task ExecuteWithFallbackAsync_VoidOperation_ShouldWork()
    {
        // Arrange
        var provider = new TestService();
        _mockSelector.Setup(s => s.GetAvailableProvidersAsync<ITestService>(null, default))
            .ReturnsAsync(new[] { provider });

        var executed = false;
        var operation = new Func<ITestService, CancellationToken, Task>((svc, ct) =>
        {
            executed = true;
            return Task.CompletedTask;
        });

        // Act
        var result = await _strategy.ExecuteWithFallbackAsync<ITestService>(operation);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(executed);
        Assert.Equal(1, result.AttemptCount);
    }

    public interface ITestService { }

    public class TestService : ITestService { }
}