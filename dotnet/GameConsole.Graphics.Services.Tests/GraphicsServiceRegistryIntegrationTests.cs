using GameConsole.Core.Abstractions;
using GameConsole.Core.Registry;
using GameConsole.Graphics.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.Graphics.Services.Tests;

public class GraphicsServiceRegistryIntegrationTests
{
    private readonly ILogger<RenderingService> _logger;

    public GraphicsServiceRegistryIntegrationTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => { });
        _logger = loggerFactory.CreateLogger<RenderingService>();
    }

    [Fact]
    public void RenderingService_HasCorrectServiceAttribute()
    {
        // Arrange & Act
        var attribute = typeof(RenderingService).GetCustomAttributes(typeof(ServiceAttribute), false)
            .FirstOrDefault() as ServiceAttribute;

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal("Rendering Service", attribute.Name);
        Assert.Contains("Graphics", attribute.Categories);
        Assert.Contains("Rendering", attribute.Categories);
        Assert.Equal(ServiceLifetime.Singleton, attribute.Lifetime);
    }

    [Fact]
    public void TextureManagerService_HasCorrectCategories()
    {
        // Arrange & Act  
        var attribute = typeof(TextureManagerService).GetCustomAttributes(typeof(ServiceAttribute), false)
            .FirstOrDefault() as ServiceAttribute;

        // Assert
        Assert.NotNull(attribute);
        Assert.Contains("Graphics", attribute.Categories);
        Assert.Contains("Resources", attribute.Categories);
        Assert.Contains("Textures", attribute.Categories);
    }

    [Fact]
    public async Task RenderingService_ProvidesCapabilities()
    {
        // Arrange
        var renderingService = new RenderingService(_logger);

        // Act
        await renderingService.InitializeAsync();
        await renderingService.StartAsync();

        // Assert - Check capabilities are available
        Assert.NotNull(renderingService.TextureManager);
        Assert.NotNull(renderingService.ShaderManager);
        Assert.NotNull(renderingService.MeshManager);
        Assert.NotNull(renderingService.CameraManager);

        // Check capability provider functionality
        var textureCapability = await renderingService.TextureManager!.HasCapabilityAsync<ITextureManagerCapability>();
        Assert.True(textureCapability);

        // Cleanup
        await renderingService.StopAsync();
        await renderingService.DisposeAsync();
    }

    [Fact]
    public async Task GraphicsServices_FollowServiceLifecycle()
    {
        // Arrange
        var renderingService = new RenderingService(_logger);

        // Act & Assert - Test lifecycle
        Assert.False(renderingService.IsRunning);

        await renderingService.InitializeAsync();
        Assert.False(renderingService.IsRunning); // Not running until started

        await renderingService.StartAsync();
        Assert.True(renderingService.IsRunning);

        await renderingService.StopAsync();
        Assert.False(renderingService.IsRunning);

        await renderingService.DisposeAsync();
        Assert.False(renderingService.IsRunning);
    }

    [Fact]
    public void AllGraphicsServices_HaveCorrectServiceAttributes()
    {
        // Arrange
        var serviceTypes = new[] 
        {
            typeof(RenderingService),
            typeof(TextureManagerService),
            typeof(ShaderService),
            typeof(MeshService),
            typeof(CameraService)
        };

        // Act & Assert
        foreach (var serviceType in serviceTypes)
        {
            var attribute = serviceType.GetCustomAttributes(typeof(ServiceAttribute), false).FirstOrDefault() as ServiceAttribute;
            
            Assert.NotNull(attribute);
            Assert.NotEmpty(attribute.Name);
            Assert.NotEmpty(attribute.Version);
            Assert.Contains("Graphics", attribute.Categories);
        }
    }
}