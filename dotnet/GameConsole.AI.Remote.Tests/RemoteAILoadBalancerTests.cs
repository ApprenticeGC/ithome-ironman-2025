using GameConsole.AI.Remote;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameConsole.AI.Remote.Tests;

public class RemoteAILoadBalancerTests
{
    private readonly Mock<ILogger<RemoteAILoadBalancer>> _mockLogger;
    private readonly LoadBalancerOptions _options;
    private readonly RemoteAILoadBalancer _loadBalancer;

    public RemoteAILoadBalancerTests()
    {
        _mockLogger = new Mock<ILogger<RemoteAILoadBalancer>>();
        _options = new LoadBalancerOptions
        {
            Strategy = LoadBalancingStrategy.RoundRobin,
            HealthCheckInterval = TimeSpan.FromMinutes(1)
        };
        _loadBalancer = new RemoteAILoadBalancer(Options.Create(_options), _mockLogger.Object);
    }

    [Fact]
    public void RegisterProvider_ShouldAddProviderToLoadBalancer()
    {
        // Arrange
        var provider = new AIProvider
        {
            Id = "test-provider",
            Name = "Test Provider",
            Type = AIProviderType.OpenAI,
            BaseUrl = "https://api.openai.com"
        };

        // Act
        _loadBalancer.RegisterProvider(provider);
        var selectedProvider = _loadBalancer.SelectProvider();

        // Assert
        Assert.NotNull(selectedProvider);
        Assert.Equal(provider.Id, selectedProvider.Id);
    }

    [Fact]
    public void SelectProvider_WithRoundRobin_ShouldCycleProviders()
    {
        // Arrange
        var provider1 = new AIProvider
        {
            Id = "provider-1",
            Name = "Provider 1",
            Type = AIProviderType.OpenAI,
            BaseUrl = "https://api.openai.com"
        };
        var provider2 = new AIProvider
        {
            Id = "provider-2",
            Name = "Provider 2", 
            Type = AIProviderType.Azure,
            BaseUrl = "https://api.azure.com"
        };

        _loadBalancer.RegisterProvider(provider1);
        _loadBalancer.RegisterProvider(provider2);

        // Act
        var first = _loadBalancer.SelectProvider();
        var second = _loadBalancer.SelectProvider();
        var third = _loadBalancer.SelectProvider();

        // Assert
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotNull(third);
        
        // Should cycle through providers
        Assert.NotEqual(first!.Id, second!.Id);
        Assert.Equal(first.Id, third!.Id);
    }

    [Fact]
    public void SelectProvider_WithExcludedProviders_ShouldRespectExclusions()
    {
        // Arrange
        var provider1 = new AIProvider
        {
            Id = "provider-1",
            Name = "Provider 1",
            Type = AIProviderType.OpenAI,
            BaseUrl = "https://api.openai.com"
        };
        var provider2 = new AIProvider
        {
            Id = "provider-2",
            Name = "Provider 2",
            Type = AIProviderType.Azure,
            BaseUrl = "https://api.azure.com"
        };

        _loadBalancer.RegisterProvider(provider1);
        _loadBalancer.RegisterProvider(provider2);

        var excludeProviders = new HashSet<string> { provider1.Id };

        // Act
        var selectedProvider = _loadBalancer.SelectProvider(excludeProviders);

        // Assert
        Assert.NotNull(selectedProvider);
        Assert.Equal(provider2.Id, selectedProvider.Id);
    }

    [Fact]
    public void StartRequest_ShouldReturnTrackingContext()
    {
        // Arrange
        var providerId = "test-provider";

        // Act
        var context = _loadBalancer.StartRequest(providerId);

        // Assert
        Assert.NotNull(context);
        Assert.Equal(providerId, context.ProviderId);
        Assert.True(context.StartTime <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void GetStatus_ShouldReturnLoadBalancingStatus()
    {
        // Arrange
        var provider = new AIProvider
        {
            Id = "test-provider",
            Name = "Test Provider",
            Type = AIProviderType.OpenAI,
            BaseUrl = "https://api.openai.com"
        };
        _loadBalancer.RegisterProvider(provider);

        // Act
        var status = _loadBalancer.GetStatus();

        // Assert
        Assert.NotNull(status);
        Assert.Equal(_options.Strategy, status.Strategy);
        Assert.Contains(provider.Id, status.ProviderStatuses.Keys);
    }

    [Fact]
    public void UnregisterProvider_ShouldRemoveProvider()
    {
        // Arrange
        var provider = new AIProvider
        {
            Id = "test-provider",
            Name = "Test Provider",
            Type = AIProviderType.OpenAI,
            BaseUrl = "https://api.openai.com"
        };
        _loadBalancer.RegisterProvider(provider);

        // Act
        _loadBalancer.UnregisterProvider(provider.Id);
        var selectedProvider = _loadBalancer.SelectProvider();

        // Assert
        Assert.Null(selectedProvider);
    }

    private void Dispose()
    {
        _loadBalancer?.Dispose();
    }
}