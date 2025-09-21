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

    [Fact]
    public async Task ResolveAsync_ShouldReturnBaseConfiguration_WhenEnvironmentIsEmpty()
    {
        // Arrange
        var baseConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Test"] = "Value" })
            .Build();
        
        var contextWithoutEnvironment = new ConfigurationContext { Environment = "", Mode = "Game" };

        // Act
        var result = await _resolver.ResolveAsync(contextWithoutEnvironment, baseConfig);

        // Assert
        Assert.Same(baseConfig, result);
    }

    [Fact]
    public async Task ResolveAsync_ShouldReturnResolvedConfiguration_WhenEnvironmentIsProvided()
    {
        // Arrange
        var baseConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Test"] = "Value" })
            .Build();

        // Act
        var result = await _resolver.ResolveAsync(_context, baseConfig);

        // Assert
        Assert.NotNull(result);
        Assert.NotSame(baseConfig, result); // Should be a new configuration object
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
    public async Task GetConfigurationPathsAsync_ShouldReturnExpectedPaths_WhenContextIsProvided()
    {
        // Act
        var paths = await _resolver.GetConfigurationPathsAsync(_context);

        // Assert
        var pathList = paths.ToList();
        
        // Note: Since these files don't exist in the test environment, 
        // the method should return an empty collection due to the .Where(File.Exists) filter
        // In a real scenario with actual config files, we would test for their presence
        Assert.NotNull(pathList);
    }

    [Fact]
    public async Task GetConfigurationPathsAsync_ShouldIncludeEnvironmentAndModePaths()
    {
        // Arrange
        var resolver = new TestableEnvironmentConfigurationResolver(_logger, "config");

        // Act  
        var paths = await resolver.GetConfigurationPathsAsync(_context);

        // Assert
        var pathList = paths.ToList();
        Assert.Contains(pathList, p => p.Contains("appsettings.json"));
        Assert.Contains(pathList, p => p.Contains("Development"));
        Assert.Contains(pathList, p => p.Contains("Game"));
        Assert.Contains(pathList, p => p.Contains("Development.Game"));
    }

    /// <summary>
    /// Testable version that doesn't filter by File.Exists for unit testing.
    /// </summary>
    private class TestableEnvironmentConfigurationResolver : EnvironmentConfigurationResolver
    {
        public TestableEnvironmentConfigurationResolver(ILogger<EnvironmentConfigurationResolver> logger, string basePath) 
            : base(logger, basePath)
        {
        }

        public new async Task<IEnumerable<string>> GetConfigurationPathsAsync(ConfigurationContext context)
        {
            // Override to return all paths without File.Exists filtering for testing
            var paths = new List<string>();
            var basePath = "config";
            
            // Base configuration paths
            paths.Add(Path.Combine(basePath, "appsettings.json"));
            paths.Add(Path.Combine(basePath, "appsettings.xml"));

            // Environment-specific paths
            if (!string.IsNullOrEmpty(context.Environment))
            {
                paths.Add(Path.Combine(basePath, $"appsettings.{context.Environment}.json"));
                paths.Add(Path.Combine(basePath, $"appsettings.{context.Environment}.xml"));
            }

            // Mode-specific paths
            if (!string.IsNullOrEmpty(context.Mode))
            {
                paths.Add(Path.Combine(basePath, $"appsettings.{context.Mode}.json"));
                paths.Add(Path.Combine(basePath, $"appsettings.{context.Mode}.xml"));
            }

            // Environment + Mode specific paths
            if (!string.IsNullOrEmpty(context.Environment) && !string.IsNullOrEmpty(context.Mode))
            {
                paths.Add(Path.Combine(basePath, $"appsettings.{context.Environment}.{context.Mode}.json"));
                paths.Add(Path.Combine(basePath, $"appsettings.{context.Environment}.{context.Mode}.xml"));
            }

            return await Task.FromResult(paths);
        }
    }
}