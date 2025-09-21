using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.Configuration.Core.Tests
{
    /// <summary>
    /// Tests for the EnvironmentConfigurationResolver implementation.
    /// </summary>
    public class EnvironmentConfigurationResolverTests
    {
        private readonly FakeLogger<EnvironmentConfigurationResolver> _logger;
        private readonly EnvironmentConfigurationResolver _resolver;
        private readonly ConfigurationContext _context;

        public EnvironmentConfigurationResolverTests()
        {
            _logger = new FakeLogger<EnvironmentConfigurationResolver>();
            _resolver = new EnvironmentConfigurationResolver(_logger, "config");
            _context = new ConfigurationContext { Environment = "Development", Mode = "Game" };
        }

        [Fact]
        public void SupportedEnvironments_ShouldReturnDefaultEnvironments()
        {
            // Act
            var environments = _resolver.SupportedEnvironments;

            // Assert
            Assert.Contains("Development", environments);
            Assert.Contains("Staging", environments);
            Assert.Contains("Production", environments);
        }

        [Theory]
        [InlineData("ConnectionStrings:Default", "Development", true)]
        [InlineData("Logging:Level", "Production", true)]
        [InlineData("Authentication:Secret", "Staging", true)]
        [InlineData("Some:OtherKey", "Development", false)]
        public void ShouldOverride_ShouldReturnExpectedResult(string key, string environment, bool expected)
        {
            // Act
            var result = _resolver.ShouldOverride(key, environment);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task ResolveAsync_ShouldReturnBaseConfiguration_WhenEnvironmentIsEmpty()
        {
            // Arrange
            var baseConfig = new ConfigurationBuilder().Build();
            var contextWithoutEnvironment = new ConfigurationContext { Environment = "", Mode = "Game" };

            // Act
            var result = await _resolver.ResolveAsync(contextWithoutEnvironment, baseConfig);

            // Assert
            Assert.Same(baseConfig, result);
        }
    }
}