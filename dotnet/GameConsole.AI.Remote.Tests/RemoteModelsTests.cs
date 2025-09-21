using GameConsole.AI.Remote;
using Xunit;

namespace GameConsole.AI.Remote.Tests;

public class RemoteModelsTests
{
    [Fact]
    public void AIProvider_ShouldCreateWithRequiredProperties()
    {
        // Arrange
        var id = "openai-1";
        var name = "OpenAI Provider";
        var type = AIProviderType.OpenAI;
        var baseUrl = "https://api.openai.com";

        // Act
        var provider = new AIProvider
        {
            Id = id,
            Name = name,
            Type = type,
            BaseUrl = baseUrl
        };

        // Assert
        Assert.Equal(id, provider.Id);
        Assert.Equal(name, provider.Name);
        Assert.Equal(type, provider.Type);
        Assert.Equal(baseUrl, provider.BaseUrl);
        Assert.True(provider.IsAvailable);
        Assert.Equal(1, provider.Priority);
        Assert.Equal(10, provider.MaxConcurrentRequests);
    }

    [Fact]
    public void LoadBalancingStatus_ShouldCreateWithProviderStatuses()
    {
        // Arrange
        var providerStatuses = new Dictionary<string, ProviderStatus>
        {
            ["provider-1"] = new ProviderStatus
            {
                ProviderId = "provider-1",
                IsHealthy = true,
                ActiveRequests = 5,
                AverageResponseTimeMs = 250.5,
                LoadPercentage = 50.0
            }
        };
        var strategy = LoadBalancingStrategy.LeastConnections;

        // Act
        var status = new LoadBalancingStatus
        {
            ProviderStatuses = providerStatuses,
            Strategy = strategy
        };

        // Assert
        Assert.Equal(providerStatuses, status.ProviderStatuses);
        Assert.Equal(strategy, status.Strategy);
        Assert.True(status.LastUpdated <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void ProviderStatus_ShouldCreateWithAllProperties()
    {
        // Arrange
        var providerId = "test-provider";
        var isHealthy = true;
        var activeRequests = 3;
        var avgResponseTime = 150.75;
        var loadPercentage = 30.0;

        // Act
        var status = new ProviderStatus
        {
            ProviderId = providerId,
            IsHealthy = isHealthy,
            ActiveRequests = activeRequests,
            AverageResponseTimeMs = avgResponseTime,
            LoadPercentage = loadPercentage
        };

        // Assert
        Assert.Equal(providerId, status.ProviderId);
        Assert.Equal(isHealthy, status.IsHealthy);
        Assert.Equal(activeRequests, status.ActiveRequests);
        Assert.Equal(avgResponseTime, status.AverageResponseTimeMs);
        Assert.Equal(loadPercentage, status.LoadPercentage);
    }

    [Fact]
    public void CostMonitoringInfo_ShouldCalculateTotalCost()
    {
        // Arrange
        var costByProvider = new Dictionary<string, decimal>
        {
            ["provider-1"] = 25.50m,
            ["provider-2"] = 15.75m
        };
        var costByModel = new Dictionary<string, decimal>
        {
            ["gpt-4"] = 30.00m,
            ["gpt-3.5-turbo"] = 11.25m
        };
        var timeRange = new TimeRange(
            DateTimeOffset.UtcNow.AddHours(-1),
            DateTimeOffset.UtcNow
        );

        // Act
        var costInfo = new CostMonitoringInfo
        {
            TotalCost = 41.25m,
            CostByProvider = costByProvider,
            CostByModel = costByModel,
            TotalTokens = 1000,
            TotalRequests = 50,
            TimeRange = timeRange
        };

        // Assert
        Assert.Equal(41.25m, costInfo.TotalCost);
        Assert.Equal(costByProvider, costInfo.CostByProvider);
        Assert.Equal(costByModel, costInfo.CostByModel);
        Assert.Equal(1000, costInfo.TotalTokens);
        Assert.Equal(50, costInfo.TotalRequests);
        Assert.Equal(timeRange, costInfo.TimeRange);
    }

    [Fact]
    public void FailoverConfiguration_ShouldHaveDefaultValues()
    {
        // Act
        var config = new FailoverConfiguration();

        // Assert
        Assert.True(config.Enabled);
        Assert.Equal(FailoverStrategy.FallbackToLocal, config.Strategy);
        Assert.Equal(3, config.MaxRetryAttempts);
        Assert.Equal(TimeSpan.FromSeconds(30), config.HealthCheckTimeout);
        Assert.Equal(TimeSpan.FromMinutes(1), config.HealthCheckInterval);
    }

    [Fact]
    public void RateLimitConfig_ShouldHaveDefaultValues()
    {
        // Act
        var config = new RateLimitConfig();

        // Assert
        Assert.Equal(60, config.RequestsPerMinute);
        Assert.Equal(100000, config.TokensPerMinute);
        Assert.Equal(10, config.BurstCapacity);
    }

    [Theory]
    [InlineData(AIProviderType.OpenAI)]
    [InlineData(AIProviderType.Azure)]
    [InlineData(AIProviderType.AWS)]
    [InlineData(AIProviderType.Google)]
    [InlineData(AIProviderType.Anthropic)]
    [InlineData(AIProviderType.Custom)]
    public void AIProviderType_ShouldSupportAllProviderTypes(AIProviderType providerType)
    {
        // Arrange & Act
        var provider = new AIProvider
        {
            Id = "test",
            Name = "Test",
            Type = providerType,
            BaseUrl = "https://example.com"
        };

        // Assert
        Assert.Equal(providerType, provider.Type);
    }

    [Theory]
    [InlineData(LoadBalancingStrategy.RoundRobin)]
    [InlineData(LoadBalancingStrategy.LeastConnections)]
    [InlineData(LoadBalancingStrategy.WeightedRoundRobin)]
    [InlineData(LoadBalancingStrategy.FastestResponse)]
    [InlineData(LoadBalancingStrategy.CostOptimized)]
    public void LoadBalancingStrategy_ShouldSupportAllStrategies(LoadBalancingStrategy strategy)
    {
        // Arrange & Act
        var status = new LoadBalancingStatus
        {
            ProviderStatuses = new Dictionary<string, ProviderStatus>(),
            Strategy = strategy
        };

        // Assert
        Assert.Equal(strategy, status.Strategy);
    }

    [Theory]
    [InlineData(FailoverStrategy.FailFast)]
    [InlineData(FailoverStrategy.FallbackToRemote)]
    [InlineData(FailoverStrategy.FallbackToLocal)]
    [InlineData(FailoverStrategy.QueueAndRetry)]
    public void FailoverStrategy_ShouldSupportAllStrategies(FailoverStrategy strategy)
    {
        // Arrange & Act
        var config = new FailoverConfiguration
        {
            Strategy = strategy
        };

        // Assert
        Assert.Equal(strategy, config.Strategy);
    }
}