using Xunit;

namespace GameConsole.UI.Configuration.Tests;

public class ProfileConfigurationBuilderTests
{
    [Fact]
    public void Build_WithMinimalConfiguration_CreatesValidProfile()
    {
        // Arrange
        var builder = new ProfileConfigurationBuilder()
            .WithName("TestProfile");

        // Act
        var profile = builder.Build();

        // Assert
        Assert.NotNull(profile);
        Assert.Equal("TestProfile", profile.Name);
        Assert.NotNull(profile.ProfileId);
        Assert.Equal("1.0.0", profile.Version);
        Assert.Equal(ProfileScope.Global, profile.Scope);
        Assert.Equal("Default", profile.Environment);
    }

    [Fact]
    public void Build_WithAllProperties_CreatesCompleteProfile()
    {
        // Arrange
        var profileId = "test-profile-123";
        var builder = new ProfileConfigurationBuilder()
            .WithProfileId(profileId)
            .WithName("Complete Test Profile")
            .WithDescription("A comprehensive test profile")
            .WithVersion("2.1.0")
            .WithScope(ProfileScope.User)
            .ForEnvironment("Development")
            .InheritsFrom("parent-profile-456");

        // Act
        var profile = builder.Build();

        // Assert
        Assert.Equal(profileId, profile.ProfileId);
        Assert.Equal("Complete Test Profile", profile.Name);
        Assert.Equal("A comprehensive test profile", profile.Description);
        Assert.Equal("2.1.0", profile.Version);
        Assert.Equal(ProfileScope.User, profile.Scope);
        Assert.Equal("Development", profile.Environment);
        Assert.Equal("parent-profile-456", profile.ParentProfileId);
    }

    [Fact]
    public void WithSettings_AddsSingleSetting_SettingIsAccessible()
    {
        // Arrange
        var builder = new ProfileConfigurationBuilder()
            .WithName("TestProfile")
            .WithSetting("TestKey", "TestValue");

        // Act
        var profile = builder.Build();

        // Assert
        Assert.Equal("TestValue", profile.GetValue<string>("TestKey"));
        Assert.True(profile.HasKey("TestKey"));
    }

    [Fact]
    public void WithSettings_AddsMultipleSettings_AllSettingsAccessible()
    {
        // Arrange
        var settings = new Dictionary<string, object>
        {
            { "Setting1", "Value1" },
            { "Setting2", 42 },
            { "Setting3", true }
        };

        var builder = new ProfileConfigurationBuilder()
            .WithName("TestProfile")
            .WithSettings(settings);

        // Act
        var profile = builder.Build();

        // Assert
        Assert.Equal("Value1", profile.GetValue<string>("Setting1"));
        Assert.Equal(42, profile.GetValue<int>("Setting2"));
        Assert.True(profile.GetValue<bool>("Setting3"));
    }

    [Fact]
    public void ConfigureUI_WithUISettings_CreatesUIConfiguration()
    {
        // Arrange
        var builder = new ProfileConfigurationBuilder()
            .WithName("UITestProfile")
            .ConfigureUI(ui => ui
                .WithTheme("Dark")
                .WithLayout("Standard")
                .WithWindowSize(1920, 1080)
                .WithFullscreen(false));

        // Act
        var profile = builder.Build();

        // Assert
        Assert.Equal("Dark", profile.GetValue<string>("UI:Theme"));
        Assert.Equal("Standard", profile.GetValue<string>("UI:Layout"));
        Assert.Equal(1920, profile.GetValue<int>("UI:Window:Width"));
        Assert.Equal(1080, profile.GetValue<int>("UI:Window:Height"));
        Assert.False(profile.GetValue<bool>("UI:Fullscreen"));
    }

    [Fact]
    public void WithMetadata_AddsMetadata_MetadataIsAccessible()
    {
        // Arrange
        var builder = new ProfileConfigurationBuilder()
            .WithName("MetadataProfile")
            .WithMetadata("Author", "Test Author")
            .WithMetadata("Version", 1.5);

        // Act
        var profile = builder.Build();

        // Assert
        Assert.Equal("Test Author", profile.Metadata["Author"]);
        Assert.Equal(1.5, profile.Metadata["Version"]);
    }

    [Fact]
    public void FromExisting_CopiesAllProperties_NewProfileMatchesOriginal()
    {
        // Arrange
        var original = new ProfileConfigurationBuilder()
            .WithProfileId("original-id")
            .WithName("Original Profile")
            .WithDescription("Original Description")
            .WithVersion("1.5.0")
            .WithScope(ProfileScope.Mode)
            .ForEnvironment("Testing")
            .WithSetting("Key1", "Value1")
            .WithMetadata("Creator", "Test")
            .Build();

        // Act
        var copied = ProfileConfigurationBuilder.FromExisting(original).Build();

        // Assert
        Assert.Equal(original.ProfileId, copied.ProfileId);
        Assert.Equal(original.Name, copied.Name);
        Assert.Equal(original.Description, copied.Description);
        Assert.Equal(original.Version, copied.Version);
        Assert.Equal(original.Scope, copied.Scope);
        Assert.Equal(original.Environment, copied.Environment);
        Assert.Equal("Value1", copied.GetValue<string>("Key1"));
        Assert.Equal("Test", copied.Metadata["Creator"]);
    }

    [Fact]
    public void Build_WithoutName_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new ProfileConfigurationBuilder()
            .WithVersion("1.0.0");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void WithName_WithInvalidName_ThrowsArgumentNullException(string invalidName)
    {
        // Arrange
        var builder = new ProfileConfigurationBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.WithName(invalidName));
    }

    [Fact]
    public void WithName_WithNullName_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new ProfileConfigurationBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.WithName(null!));
    }
}