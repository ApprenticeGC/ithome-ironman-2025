using Xunit;

namespace GameConsole.UI.Configuration.Tests;

public class ConfigurationValidatorTests
{
    private readonly ConfigurationValidator _validator = new();

    [Fact]
    public async Task ValidateAsync_ValidConfiguration_ReturnsSuccess()
    {
        // Arrange
        var configuration = new ProfileConfigurationBuilder()
            .WithProfileId("valid-profile")
            .WithName("Valid Profile")
            .WithDescription("A valid test profile")
            .WithVersion("1.0.0")
            .WithSetting("UI:Window:Width", 1920)
            .WithSetting("UI:Window:Height", 1080)
            .WithSetting("UI:Theme", "Dark")
            .Build();

        // Act
        var result = await _validator.ValidateAsync(configuration);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateAsync_MissingRequiredFields_ReturnsErrors()
    {
        // Arrange - Create a profile with missing required fields
        var configuration = new ProfileConfigurationBuilder()
            .WithProfileId("") // Empty profile ID
            .WithName("") // Empty name
            .WithVersion("") // Empty version
            .Build();

        // Act
        var result = await _validator.ValidateAsync(configuration);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Key == nameof(configuration.ProfileId));
        Assert.Contains(result.Errors, e => e.Key == nameof(configuration.Name));
        Assert.Contains(result.Errors, e => e.Key == nameof(configuration.Version));
    }

    [Fact]
    public async Task ValidateAsync_InvalidWindowDimensions_ReturnsErrors()
    {
        // Arrange
        var configuration = new ProfileConfigurationBuilder()
            .WithName("Invalid UI Profile")
            .WithSetting("UI:Window:Width", -100)
            .WithSetting("UI:Window:Height", 0)
            .Build();

        // Act
        var result = await _validator.ValidateAsync(configuration);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Key == "UI:Window:Width");
        Assert.Contains(result.Errors, e => e.Key == "UI:Window:Height");
    }

    [Fact]
    public async Task ValidateAsync_SmallWindowDimensions_ReturnsWarnings()
    {
        // Arrange
        var configuration = new ProfileConfigurationBuilder()
            .WithName("Small Window Profile")
            .WithSetting("UI:Window:Width", 640)
            .WithSetting("UI:Window:Height", 480)
            .Build();

        // Act
        var result = await _validator.ValidateAsync(configuration);

        // Assert
        Assert.True(result.IsValid); // Still valid, but with warnings
        Assert.Contains(result.Warnings, w => w.Key == "UI:Window:Width");
        Assert.Contains(result.Warnings, w => w.Key == "UI:Window:Height");
    }

    [Fact]
    public async Task ValidateAsync_EmptyTheme_ReturnsError()
    {
        // Arrange
        var configuration = new ProfileConfigurationBuilder()
            .WithName("Empty Theme Profile")
            .WithSetting("UI:Theme", "")
            .Build();

        // Act
        var result = await _validator.ValidateAsync(configuration);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Key == "UI:Theme");
    }

    [Fact]
    public async Task ValidateAsync_InvalidVersion_ReturnsError()
    {
        // Arrange
        var configuration = new ProfileConfigurationBuilder()
            .WithName("Invalid Version Profile")
            .WithVersion("not-a-version")
            .Build();

        // Act
        var result = await _validator.ValidateAsync(configuration);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Key == nameof(configuration.Version));
    }

    [Fact]
    public async Task ValidateAsync_ZeroVersion_ReturnsError()
    {
        // Arrange
        var configuration = new ProfileConfigurationBuilder()
            .WithName("Zero Version Profile")
            .WithVersion("0.0.0")
            .Build();

        // Act
        var result = await _validator.ValidateAsync(configuration);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Key == nameof(configuration.Version));
    }

    [Fact]
    public async Task ValidateAsync_NonStandardEnvironment_ReturnsWarning()
    {
        // Arrange
        var configuration = new ProfileConfigurationBuilder()
            .WithName("Custom Environment Profile")
            .ForEnvironment("CustomEnvironment")
            .Build();

        // Act
        var result = await _validator.ValidateAsync(configuration);

        // Assert
        Assert.True(result.IsValid); // Valid but with warnings
        Assert.Contains(result.Warnings, w => w.Key == nameof(configuration.Environment));
    }

    [Fact]
    public async Task ValidateAsync_MissingDescription_ReturnsWarning()
    {
        // Arrange
        var configuration = new ProfileConfigurationBuilder()
            .WithName("No Description Profile")
            // No description set
            .Build();

        // Act
        var result = await _validator.ValidateAsync(configuration);

        // Assert
        Assert.True(result.IsValid); // Valid but with warnings
        Assert.Contains(result.Warnings, w => w.Key == nameof(configuration.Description));
    }

    [Fact]
    public async Task ValidateAsync_MissingRecommendedMetadata_ReturnsWarnings()
    {
        // Arrange
        var configuration = new ProfileConfigurationBuilder()
            .WithName("Minimal Metadata Profile")
            .Build();

        // Act
        var result = await _validator.ValidateAsync(configuration);

        // Assert
        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Key == "Metadata.Author");
        Assert.Contains(result.Warnings, w => w.Key == "Metadata.Created");
        Assert.Contains(result.Warnings, w => w.Key == "Metadata.Category");
        Assert.Contains(result.Warnings, w => w.Key == "Metadata.Tags");
    }

    [Fact]
    public async Task ValidateBatchAsync_MultipleConfigurations_ReturnsAllResults()
    {
        // Arrange
        var validConfig = new ProfileConfigurationBuilder()
            .WithName("Valid Profile")
            .Build();

        var invalidConfig = new ProfileConfigurationBuilder()
            .WithName("") // Invalid - empty name
            .Build();

        var configurations = new[] { validConfig, invalidConfig };

        // Act
        var results = await _validator.ValidateBatchAsync(configurations);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.True(results[validConfig.ProfileId].IsValid);
        Assert.False(results[invalidConfig.ProfileId].IsValid);
    }

    [Fact]
    public async Task AddRule_CustomRule_RuleIsExecuted()
    {
        // Arrange
        var customRule = new TestValidationRule();
        _validator.AddRule(customRule);

        var configuration = new ProfileConfigurationBuilder()
            .WithName("Test Profile")
            .Build();

        // Act
        var result = await _validator.ValidateAsync(configuration);

        // Assert
        Assert.Contains(result.Errors, e => e.Message == "Custom validation failed");
    }

    [Fact]
    public async Task RemoveRule_ExistingRuleType_RemovesRule()
    {
        // Arrange
        var validator = new ConfigurationValidator();
        validator.AddRule(new TestValidationRule());

        // Act
        validator.RemoveRule<TestValidationRule>();

        // Assert
        var configuration = new ProfileConfigurationBuilder()
            .WithName("Test Profile")
            .Build();

        var result = await validator.ValidateAsync(configuration);
        Assert.DoesNotContain(result.Errors, e => e.Message == "Custom validation failed");
    }

    private class TestValidationRule : IValidationRule
    {
        public Task<ValidationResult> ValidateAsync(IProfileConfiguration configuration, CancellationToken cancellationToken = default)
        {
            var errors = new List<ValidationError>
            {
                new("TestRule", "Custom validation failed")
            };

            return Task.FromResult(new ValidationResult
            {
                IsValid = false,
                Errors = errors
            });
        }
    }
}