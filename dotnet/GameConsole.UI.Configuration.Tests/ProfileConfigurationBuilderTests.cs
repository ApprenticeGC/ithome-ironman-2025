namespace GameConsole.UI.Configuration.Tests;

public class ProfileConfigurationBuilderTests
{
    [Fact]
    public void Build_WithRequiredProperties_CreatesValidConfiguration()
    {
        // Arrange
        var builder = new ProfileConfigurationBuilder()
            .WithId("test-profile")
            .WithName("Test Profile")
            .WithDescription("A test profile for validation")
            .WithVersion(1, 2, 3)
            .WithScope(ConfigurationScope.User)
            .WithEnvironment("Development")
            .WithSetting("key1", "value1")
            .WithSetting("key2", 42);

        // Act
        var config = builder.Build();

        // Assert
        Assert.Equal("test-profile", config.Id);
        Assert.Equal("Test Profile", config.Name);
        Assert.Equal("A test profile for validation", config.Description);
        Assert.Equal(new Version(1, 2, 3), config.Version);
        Assert.Equal(ConfigurationScope.User, config.Scope);
        Assert.Equal("Development", config.Environment);
        Assert.Equal("value1", config.GetValue<string>("key1"));
        Assert.Equal(42, config.GetValue<int>("key2"));
    }

    [Fact]
    public void Build_WithoutId_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new ProfileConfigurationBuilder()
            .WithName("Test Profile");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Contains("Profile ID is required", exception.Message);
    }

    [Fact]
    public void Build_WithoutName_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new ProfileConfigurationBuilder()
            .WithId("test-profile");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Contains("Profile name is required", exception.Message);
    }

    [Fact]
    public void WithSettings_AddsDictionarySettings_ConfigurationContainsAllSettings()
    {
        // Arrange
        var settings = new Dictionary<string, object?>
        {
            ["setting1"] = "value1",
            ["setting2"] = 123,
            ["setting3"] = true
        };

        var builder = new ProfileConfigurationBuilder()
            .WithId("test-profile")
            .WithName("Test Profile")
            .WithSettings(settings);

        // Act
        var config = builder.Build();

        // Assert
        Assert.Equal("value1", config.GetValue<string>("setting1"));
        Assert.Equal(123, config.GetValue<int>("setting2"));
        Assert.True(config.GetValue<bool>("setting3"));
    }

    [Fact]
    public void RemoveSetting_RemovesExistingSetting_SettingNotPresent()
    {
        // Arrange
        var builder = new ProfileConfigurationBuilder()
            .WithId("test-profile")
            .WithName("Test Profile")
            .WithSetting("key1", "value1")
            .WithSetting("key2", "value2")
            .RemoveSetting("key1");

        // Act
        var config = builder.Build();

        // Assert
        Assert.False(config.HasValue("key1"));
        Assert.True(config.HasValue("key2"));
    }

    [Fact]
    public void FromConfiguration_CopiesExistingConfiguration_NewBuilderHasSameValues()
    {
        // Arrange
        var originalConfig = new ProfileConfigurationBuilder()
            .WithId("original")
            .WithName("Original Profile")
            .WithDescription("Original description")
            .WithVersion(2, 1, 0)
            .WithScope(ConfigurationScope.Plugin)
            .WithEnvironment("Production")
            .InheritsFrom("parent-profile")
            .WithSetting("key", "value")
            .Build();

        // Act
        var newConfig = ProfileConfigurationBuilder
            .FromConfiguration(originalConfig)
            .WithId("copied") // Change ID to create new config
            .Build();

        // Assert
        Assert.Equal("copied", newConfig.Id);
        Assert.Equal("Original Profile", newConfig.Name);
        Assert.Equal("Original description", newConfig.Description);
        Assert.Equal(new Version(2, 1, 0), newConfig.Version);
        Assert.Equal(ConfigurationScope.Plugin, newConfig.Scope);
        Assert.Equal("Production", newConfig.Environment);
        Assert.Equal("parent-profile", newConfig.InheritsFrom);
        Assert.Equal("value", newConfig.GetValue<string>("key"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void WithSetting_WithInvalidKey_ThrowsArgumentException(string key)
    {
        // Arrange
        var builder = new ProfileConfigurationBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithSetting(key, "value"));
    }

    [Fact]
    public void WithInheritance_SetsInheritanceProperty_ConfigurationHasParent()
    {
        // Arrange
        var builder = new ProfileConfigurationBuilder()
            .WithId("child-profile")
            .WithName("Child Profile")
            .InheritsFrom("parent-profile");

        // Act
        var config = builder.Build();

        // Assert
        Assert.Equal("parent-profile", config.InheritsFrom);
    }
}