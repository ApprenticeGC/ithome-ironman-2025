namespace GameConsole.UI.Configuration.Tests;

public class ConfigurationValidatorTests
{
    private readonly ConfigurationValidator _validator;

    public ConfigurationValidatorTests()
    {
        _validator = new ConfigurationValidator();
    }

    [Fact]
    public async Task ValidateAsync_WithValidConfiguration_ReturnsSuccess()
    {
        // Arrange
        var config = new ProfileConfigurationBuilder()
            .WithId("valid-config")
            .WithName("Valid Configuration")
            .WithDescription("A valid configuration for testing")
            .WithVersion(1, 0, 0)
            .WithScope(ConfigurationScope.User)
            .WithEnvironment("Development")
            .WithSetting("key", "value")
            .Build();

        // Act
        var result = await _validator.ValidateAsync(config);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateAsync_WithMissingId_ReturnsError()
    {
        // Arrange
        var config = new ProfileConfigurationBuilder()
            .WithId("") // Empty ID
            .WithName("Test Configuration")
            .Build();

        // Act
        var result = await _validator.ValidateAsync(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Property == "Id");
    }

    [Fact]
    public async Task ValidateAsync_WithMissingName_ReturnsError()
    {
        // Arrange
        var config = new ProfileConfigurationBuilder()
            .WithId("test-config")
            .WithName("") // Empty name
            .Build();

        // Act
        var result = await _validator.ValidateAsync(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Property == "Name");
    }

    [Fact]
    public async Task ValidateAsync_WithVersionZero_ReturnsWarning()
    {
        // Arrange
        var config = new ProfileConfigurationBuilder()
            .WithId("test-config")
            .WithName("Test Configuration")
            .WithVersion(0, 1, 0) // Major version 0
            .Build();

        // Act
        var result = await _validator.ValidateAsync(config);

        // Assert
        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Property == "Version");
    }

    [Fact]
    public async Task ValidateAsync_WithComplexCollectionSetting_ReturnsWarning()
    {
        // Arrange
        var config = new ProfileConfigurationBuilder()
            .WithId("test-config")
            .WithName("Test Configuration")
            .WithSetting("listSetting", new List<int> { 1, 2, 3 }) // Complex collection
            .Build();

        // Act
        var result = await _validator.ValidateAsync(config);

        // Assert
        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Property == "listSetting");
    }

    [Fact]
    public async Task ValidateAsync_WithGlobalScopeAndNonDefaultEnvironment_ReturnsWarning()
    {
        // Arrange
        var config = new ProfileConfigurationBuilder()
            .WithId("test-config")
            .WithName("Test Configuration")
            .WithScope(ConfigurationScope.Global)
            .WithEnvironment("Production") // Non-default environment with Global scope
            .Build();

        // Act
        var result = await _validator.ValidateAsync(config);

        // Assert
        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Property == "Scope");
    }

    [Fact]
    public async Task ValidateAsync_WithEnvironmentScopeAndDefaultEnvironment_ReturnsWarning()
    {
        // Arrange
        var config = new ProfileConfigurationBuilder()
            .WithId("test-config")
            .WithName("Test Configuration")
            .WithScope(ConfigurationScope.Environment)
            .WithEnvironment("Default") // Default environment with Environment scope
            .Build();

        // Act
        var result = await _validator.ValidateAsync(config);

        // Assert
        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Property == "Scope");
    }

    [Fact]
    public async Task ValidateBatchAsync_WithMultipleConfigurations_ReturnsResultsForAll()
    {
        // Arrange
        var config1 = new ProfileConfigurationBuilder()
            .WithId("config1")
            .WithName("Configuration 1")
            .Build();

        var config2 = new ProfileConfigurationBuilder()
            .WithId("config2")
            .WithName("") // Invalid name
            .Build();

        var configurations = new[] { config1, config2 };

        // Act
        var results = await _validator.ValidateBatchAsync(configurations);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.True(results["config1"].IsValid);
        Assert.False(results["config2"].IsValid);
    }

    [Fact]
    public async Task ValidateCompatibilityAsync_WithMixedScopes_ReturnsWarning()
    {
        // Arrange
        var config1 = new ProfileConfigurationBuilder()
            .WithId("config1")
            .WithName("Configuration 1")
            .WithScope(ConfigurationScope.Global)
            .Build();

        var config2 = new ProfileConfigurationBuilder()
            .WithId("config2")
            .WithName("Configuration 2")
            .WithScope(ConfigurationScope.User)
            .Build();

        var configurations = new[] { config1, config2 };

        // Act
        var result = await _validator.ValidateCompatibilityAsync(configurations);

        // Assert
        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Property == "Scope" && w.WarningCode == "MIXED_SCOPES");
    }

    [Fact]
    public async Task ValidateCompatibilityAsync_WithIncompatibleMajorVersions_ReturnsError()
    {
        // Arrange
        var config1 = new ProfileConfigurationBuilder()
            .WithId("config1")
            .WithName("Configuration 1")
            .WithVersion(1, 0, 0)
            .Build();

        var config2 = new ProfileConfigurationBuilder()
            .WithId("config2")
            .WithName("Configuration 2")
            .WithVersion(2, 0, 0) // Different major version
            .Build();

        var configurations = new[] { config1, config2 };

        // Act
        var result = await _validator.ValidateCompatibilityAsync(configurations);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Property == "Version" && e.ErrorCode == "VERSION_INCOMPATIBILITY");
    }

    [Fact]
    public void AddRule_WithCustomRule_RuleIsExecuted()
    {
        // Arrange
        var customRule = new TestValidationRule();
        _validator.AddRule(customRule);

        var config = new ProfileConfigurationBuilder()
            .WithId("test-config")
            .WithName("Test Configuration")
            .Build();

        // Act
        var result = _validator.ValidateAsync(config).Result;

        // Assert
        Assert.True(customRule.WasCalled);
    }

    [Fact]
    public void RemoveRule_WithExistingRuleType_RemovesRule()
    {
        // Arrange
        _validator.AddRule(new TestValidationRule());

        // Act
        var removed = _validator.RemoveRule<TestValidationRule>();

        // Assert
        Assert.True(removed);
    }

    [Fact]
    public void SetContext_WithContextValue_ContextIsAvailableToRules()
    {
        // Arrange
        var contextRule = new ContextAwareRule();
        _validator.AddRule(contextRule);
        _validator.SetContext("testKey", "testValue");

        var config = new ProfileConfigurationBuilder()
            .WithId("test-config")
            .WithName("Test Configuration")
            .Build();

        // Act
        var result = _validator.ValidateAsync(config).Result;

        // Assert
        Assert.Equal("testValue", contextRule.ReceivedContextValue);
    }

    private class TestValidationRule : IValidationRule
    {
        public bool WasCalled { get; private set; }

        public Task<ValidationResult> ValidateAsync(ValidationContext context, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.FromResult(ValidationResult.Success());
        }
    }

    private class ContextAwareRule : IValidationRule
    {
        public object? ReceivedContextValue { get; private set; }

        public Task<ValidationResult> ValidateAsync(ValidationContext context, CancellationToken cancellationToken = default)
        {
            ReceivedContextValue = context.GlobalContext.TryGetValue("testKey", out var value) ? value : null;
            return Task.FromResult(ValidationResult.Success());
        }
    }
}