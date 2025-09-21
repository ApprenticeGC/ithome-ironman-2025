using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.Configuration.Core.Tests
{
    /// <summary>
    /// Simple fake logger for testing purposes.
    /// </summary>
    public class FakeLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }

    /// <summary>
    /// Tests for the ConfigurationManager implementation.
    /// </summary>
    public class ConfigurationManagerTests
    {
        private readonly FakeLogger<ConfigurationManager> _logger;
        private readonly List<IConfigurationProvider> _providers;
        private readonly IEnvironmentConfigurationResolver _environmentResolver;
        private readonly IConfigurationValidator _validator;
        private readonly ConfigurationContext _context;

        public ConfigurationManagerTests()
        {
            _logger = new FakeLogger<ConfigurationManager>();
            _providers = new List<IConfigurationProvider>();
            _environmentResolver = new TestEnvironmentConfigurationResolver();
            _validator = new TestConfigurationValidator();
            _context = new ConfigurationContext 
            { 
                Environment = "Test", 
                Mode = "Game" 
            };
        }

        [Fact]
        public async Task InitializeAsync_ShouldBuildConfiguration_WhenProvidersAreAvailable()
        {
            // Arrange
            var testProvider = new TestConfigurationProvider("Test", ConfigurationPriority.Base);
            _providers.Add(testProvider);
            
            var configManager = new ConfigurationManager(_logger, _providers, _environmentResolver, _validator, _context);

            // Act
            await configManager.InitializeAsync();

            // Assert
            Assert.NotNull(configManager.Configuration);
            Assert.True(testProvider.BuildConfigurationAsyncCalled);
        }

        [Fact]
        public void Context_ShouldReturnProvidedContext()
        {
            // Arrange & Act
            var configManager = new ConfigurationManager(_logger, _providers, _environmentResolver, _validator, _context);

            // Assert
            Assert.Equal(_context, configManager.Context);
            Assert.Equal("Test", configManager.Context.Environment);
            Assert.Equal("Game", configManager.Context.Mode);
        }

        private class TestConfigurationProvider : IConfigurationProvider
        {
            public TestConfigurationProvider(string name, ConfigurationPriority priority)
            {
                Name = name;
                Priority = priority;
            }

            public string Name { get; }
            public ConfigurationPriority Priority { get; }
            public bool SupportsReload => false;
            public bool BuildConfigurationAsyncCalled { get; private set; }

#pragma warning disable CS0067 // Event is never used in test
            public event EventHandler? ConfigurationChanged;
#pragma warning restore CS0067

            public async Task<bool> CanApplyAsync(ConfigurationContext context)
            {
                return await Task.FromResult(true);
            }

            public async Task BuildConfigurationAsync(IConfigurationBuilder builder, ConfigurationContext context)
            {
                BuildConfigurationAsyncCalled = true;
                await Task.CompletedTask;
            }
        }

        private class TestEnvironmentConfigurationResolver : IEnvironmentConfigurationResolver
        {
            public IReadOnlyList<string> SupportedEnvironments => new[] { "Test", "Development", "Production" };

            public async Task<IConfiguration> ResolveAsync(
                ConfigurationContext context, 
                IConfiguration baseConfiguration, 
                CancellationToken cancellationToken = default)
            {
                return await Task.FromResult(baseConfiguration);
            }

            public bool ShouldOverride(string key, string environment)
            {
                return key.Contains("Test");
            }

            public async Task<IEnumerable<string>> GetConfigurationPathsAsync(ConfigurationContext context)
            {
                return await Task.FromResult(Array.Empty<string>());
            }
        }

        private class TestConfigurationValidator : IConfigurationValidator
        {
            public IReadOnlyList<Type> SupportedTypes => Array.Empty<Type>();

            public async Task<ConfigurationValidationResult> ValidateAsync<T>(
                T configurationObject, 
                string sectionKey, 
                CancellationToken cancellationToken = default) where T : class
            {
                return await Task.FromResult(new ConfigurationValidationResult { IsValid = true });
            }

            public async Task<ConfigurationValidationResult> ValidateSectionAsync(
                IConfiguration configuration,
                string sectionKey,
                Type expectedType,
                CancellationToken cancellationToken = default)
            {
                return await Task.FromResult(new ConfigurationValidationResult { IsValid = true });
            }

            public void RegisterValidationRule<T>(Func<T, System.ComponentModel.DataAnnotations.ValidationResult?> validationRule) where T : class
            {
                // No-op for testing
            }

            public void RegisterSchema<T>(string jsonSchema) where T : class
            {
                // No-op for testing
            }
        }
    }
}