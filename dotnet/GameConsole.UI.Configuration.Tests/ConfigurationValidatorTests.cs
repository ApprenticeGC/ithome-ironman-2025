using GameConsole.UI.Configuration;
using Xunit;

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
        var configuration = ProfileConfigurationBuilder.Create()
            .WithId("valid-profile")
            .WithName("Valid Profile")
            .WithDescription("A valid profile configuration")
            .WithVersion(new Version(1, 0, 0))
            .ForEnvironment("Development")
            .AddJsonString("""
            {
                "UI": { "Theme": "Dark" },
                "Commands": { "DefaultTimeout": "30" },
                "Layout": { "DefaultView": "Console" }
            }
            """)
            .Build();

        // Act
        var result = await _validator.ValidateAsync(configuration);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateAsync_WithNullConfiguration_ReturnsFailure()
    {
        // Act
        var result = await _validator.ValidateAsync(null!);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Configuration cannot be null.", result.Errors);
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidEnvironment_ReturnsWarning()
    {
        // Arrange
        var configuration = ProfileConfigurationBuilder.Create()
            .WithId("invalid-env-profile")
            .WithName("Invalid Environment Profile")
            .WithDescription("A profile with non-standard environment")
            .ForEnvironment("CustomEnvironment")
            .Build();

        // Act
        var result = await _validator.ValidateAsync(configuration);

        // Assert
        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Contains("CustomEnvironment") && w.Contains("not standard"));
    }

    [Fact]
    public async Task ValidateAsync_WithMissingSections_ReturnsWarnings()
    {
        // Arrange
        var configuration = ProfileConfigurationBuilder.Create()
            .WithId("missing-sections-profile")
            .WithName("Missing Sections Profile")
            .WithDescription("A profile missing recommended sections")
            .AddJsonString("{}") // Empty JSON
            .Build();

        // Act
        var result = await _validator.ValidateAsync(configuration);

        // Assert
        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Contains("UI") && w.Contains("missing"));
        Assert.Contains(result.Warnings, w => w.Contains("Commands") && w.Contains("missing"));
        Assert.Contains(result.Warnings, w => w.Contains("Layout") && w.Contains("missing"));
    }

    [Fact]
    public async Task ValidateCompatibilityAsync_WithCompatibleConfigurations_ReturnsSuccess()
    {
        // Arrange
        var parentConfig = ProfileConfigurationBuilder.Create()
            .WithId("parent-profile")
            .WithName("Parent Profile")
            .WithDescription("Parent configuration")
            .WithVersion(new Version(1, 0, 0))
            .ForEnvironment("Development")
            .Build();

        var childConfig = ProfileConfigurationBuilder.Create()
            .WithId("child-profile")
            .WithName("Child Profile")
            .WithDescription("Child configuration")
            .WithVersion(new Version(1, 1, 0))
            .ForEnvironment("Development")
            .InheritsFrom("parent-profile")
            .Build();

        // Act
        var result = await _validator.ValidateCompatibilityAsync(childConfig, parentConfig);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateCompatibilityAsync_WithVersionMismatch_ReturnsWarning()
    {
        // Arrange
        var parentConfig = ProfileConfigurationBuilder.Create()
            .WithId("parent-profile")
            .WithName("Parent Profile")
            .WithDescription("Parent configuration")
            .WithVersion(new Version(2, 0, 0))
            .ForEnvironment("Development")
            .Build();

        var childConfig = ProfileConfigurationBuilder.Create()
            .WithId("child-profile")
            .WithName("Child Profile")
            .WithDescription("Child configuration")
            .WithVersion(new Version(1, 0, 0))
            .ForEnvironment("Development")
            .InheritsFrom("parent-profile")
            .Build();

        // Act
        var result = await _validator.ValidateCompatibilityAsync(childConfig, parentConfig);

        // Assert
        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Contains("older than parent"));
    }

    [Fact]
    public async Task ValidateCompatibilityAsync_WithEnvironmentMismatch_ReturnsWarning()
    {
        // Arrange
        var parentConfig = ProfileConfigurationBuilder.Create()
            .WithId("parent-profile")
            .WithName("Parent Profile")
            .WithDescription("Parent configuration")
            .ForEnvironment("Production")
            .Build();

        var childConfig = ProfileConfigurationBuilder.Create()
            .WithId("child-profile")
            .WithName("Child Profile")
            .WithDescription("Child configuration")
            .ForEnvironment("Development")
            .InheritsFrom("parent-profile")
            .Build();

        // Act
        var result = await _validator.ValidateCompatibilityAsync(childConfig, parentConfig);

        // Assert
        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Contains("differs from parent environment"));
    }

    [Fact]
    public async Task ValidateSchemaAsync_WithCompatibleVersion_ReturnsSuccess()
    {
        // Arrange
        var configuration = ProfileConfigurationBuilder.Create()
            .WithId("schema-profile")
            .WithName("Schema Profile")
            .WithDescription("Profile for schema validation")
            .WithVersion(new Version(1, 0, 0))
            .AddJsonString("""
            {
                "Metadata": { "Author": "Test" }
            }
            """)
            .Build();

        var schemaVersion = new Version(1, 0, 0);

        // Act
        var result = await _validator.ValidateSchemaAsync(configuration, schemaVersion);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateSchemaAsync_WithIncompatibleMajorVersion_ReturnsFailure()
    {
        // Arrange
        var configuration = ProfileConfigurationBuilder.Create()
            .WithId("incompatible-profile")
            .WithName("Incompatible Profile")
            .WithDescription("Profile with incompatible version")
            .WithVersion(new Version(2, 0, 0))
            .Build();

        var schemaVersion = new Version(1, 0, 0);

        // Act
        var result = await _validator.ValidateSchemaAsync(configuration, schemaVersion);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("incompatible") && e.Contains("Major versions"));
    }

    [Fact]
    public async Task ValidateSchemaAsync_WithOlderVersion_ReturnsWarning()
    {
        // Arrange
        var configuration = ProfileConfigurationBuilder.Create()
            .WithId("older-profile")
            .WithName("Older Profile")
            .WithDescription("Profile with older version")
            .WithVersion(new Version(1, 0, 0))
            .Build();

        var schemaVersion = new Version(1, 1, 0);

        // Act
        var result = await _validator.ValidateSchemaAsync(configuration, schemaVersion);

        // Assert
        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Contains("older than current schema"));
    }
}