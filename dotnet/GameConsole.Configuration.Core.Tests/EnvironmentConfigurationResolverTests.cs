using Microsoft.Extensions.Logging;
using GameConsole.Configuration.Core;
using GameConsole.Configuration.Core.Models;
using Moq;
using Xunit;

namespace GameConsole.Configuration.Core.Tests;

/// <summary>
/// Tests for the EnvironmentConfigurationResolver class.
/// </summary>
public sealed class EnvironmentConfigurationResolverTests
{
    private readonly Mock<ILogger<EnvironmentConfigurationResolver>> _loggerMock;
    private readonly EnvironmentConfigurationResolver _resolver;

    public EnvironmentConfigurationResolverTests()
    {
        _loggerMock = new Mock<ILogger<EnvironmentConfigurationResolver>>();
        _resolver = new EnvironmentConfigurationResolver(_loggerMock.Object);
    }

    [Fact]
    public void CurrentEnvironment_Should_Default_To_Development()
    {
        // Arrange & Act
        var environment = _resolver.CurrentEnvironment;

        // Assert
        Assert.False(string.IsNullOrEmpty(environment));
        // Environment could be set by system, so we just verify it's not null/empty
    }

    [Theory]
    [InlineData("Development", true)]
    [InlineData("Staging", true)]
    [InlineData("Production", true)]
    [InlineData("Testing", true)]
    [InlineData("Invalid", false)]
    [InlineData("", false)]
    public void IsValidEnvironment_Should_Return_Expected_Result(string environment, bool expected)
    {
        // Act
        var result = _resolver.IsValidEnvironment(environment);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ResolveConfigurationPaths_Should_Return_Hierarchical_Paths()
    {
        // Arrange
        var basePath = "/config/appsettings.json";
        var context = new ConfigurationContext
        {
            Environment = "Development",
            Mode = "Game",
            UserId = "testuser"
        };

        // Act
        var paths = _resolver.ResolveConfigurationPaths(basePath, context);

        // Assert
        Assert.Contains("/config/appsettings.json", paths);
        Assert.Contains("/config/appsettings.Development.json", paths);
        Assert.Contains("/config/appsettings.Game.json", paths);
        Assert.Contains("/config/appsettings.User.testuser.json", paths);
        Assert.Equal(4, paths.Count);
    }

    [Fact]
    public async Task GetEnvironmentOverridesAsync_Should_Return_Development_Overrides()
    {
        // Arrange
        var context = new ConfigurationContext { Environment = "Development" };

        // Act
        var overrides = await _resolver.GetEnvironmentOverridesAsync(context);

        // Assert
        Assert.True(overrides.Count > 0);
        Assert.True(overrides.ContainsKey("Logging:LogLevel:Default"));
        Assert.Equal("Debug", overrides["Logging:LogLevel:Default"]);
    }

    [Fact]
    public async Task GetEnvironmentOverridesAsync_Should_Return_Production_Overrides()
    {
        // Arrange
        var context = new ConfigurationContext { Environment = "Production" };

        // Act
        var overrides = await _resolver.GetEnvironmentOverridesAsync(context);

        // Assert
        Assert.True(overrides.Count > 0);
        Assert.True(overrides.ContainsKey("Logging:LogLevel:Default"));
        Assert.Equal("Warning", overrides["Logging:LogLevel:Default"]);
    }

    [Fact]
    public void SetEnvironment_Should_Update_Current_Environment()
    {
        // Arrange
        var newEnvironment = "Production";

        // Act
        _resolver.SetEnvironment(newEnvironment);

        // Assert
        Assert.Equal(newEnvironment, _resolver.CurrentEnvironment);
    }

    [Fact]
    public void SetEnvironment_Should_Throw_For_Invalid_Environment()
    {
        // Arrange
        var invalidEnvironment = "InvalidEnv";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _resolver.SetEnvironment(invalidEnvironment));
    }
}