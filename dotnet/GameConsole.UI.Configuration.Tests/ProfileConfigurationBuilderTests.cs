using GameConsole.UI.Configuration;
using Xunit;

namespace GameConsole.UI.Configuration.Tests;

public class ProfileConfigurationBuilderTests
{
    [Fact]
    public void Build_WithValidConfiguration_ReturnsProfileConfiguration()
    {
        // Arrange
        var builder = ProfileConfigurationBuilder.Create()
            .WithId("test-profile")
            .WithName("Test Profile")
            .WithDescription("A test profile configuration")
            .WithVersion(new Version(1, 0, 0))
            .ForEnvironment("Development")
            .WithMetadata("author", "Test Author");

        // Act
        var configuration = builder.Build();

        // Assert
        Assert.Equal("test-profile", configuration.Id);
        Assert.Equal("Test Profile", configuration.Name);
        Assert.Equal("A test profile configuration", configuration.Description);
        Assert.Equal(new Version(1, 0, 0), configuration.Version);
        Assert.Equal("Development", configuration.Environment);
        Assert.Null(configuration.ParentProfileId);
        Assert.NotNull(configuration.Configuration);
        Assert.NotNull(configuration.Metadata);
        Assert.Contains("author", configuration.Metadata.Keys);
        Assert.Equal("Test Author", configuration.Metadata["author"]);
    }

    [Fact]
    public void Build_WithoutRequiredProperties_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = ProfileConfigurationBuilder.Create();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void Build_WithInheritance_SetsParentProfileId()
    {
        // Arrange
        var builder = ProfileConfigurationBuilder.Create()
            .WithId("child-profile")
            .WithName("Child Profile")
            .WithDescription("A child profile configuration")
            .InheritsFrom("parent-profile");

        // Act
        var configuration = builder.Build();

        // Assert
        Assert.Equal("parent-profile", configuration.ParentProfileId);
    }

    [Fact]
    public void AddJsonString_WithValidJson_AddsToConfiguration()
    {
        // Arrange
        var json = """
        {
            "UI": {
                "Theme": "Dark",
                "FontSize": 14
            },
            "Commands": {
                "DefaultTimeout": "00:00:30"
            }
        }
        """;

        var builder = ProfileConfigurationBuilder.Create()
            .WithId("json-profile")
            .WithName("JSON Profile")
            .WithDescription("A profile with JSON configuration")
            .AddJsonString(json);

        // Act
        var configuration = builder.Build();

        // Assert
        Assert.Equal("Dark", configuration.Configuration["UI:Theme"]);
        Assert.Equal("14", configuration.Configuration["UI:FontSize"]);
        Assert.Equal("00:00:30", configuration.Configuration["Commands:DefaultTimeout"]);
    }

    [Fact]
    public void AddInMemoryCollection_WithKeyValuePairs_AddsToConfiguration()
    {
        // Arrange
        var data = new Dictionary<string, string?>
        {
            { "Setting1", "Value1" },
            { "Setting2", "Value2" },
            { "Section:NestedSetting", "NestedValue" }
        };

        var builder = ProfileConfigurationBuilder.Create()
            .WithId("memory-profile")
            .WithName("Memory Profile")
            .WithDescription("A profile with in-memory configuration")
            .AddInMemoryCollection(data);

        // Act
        var configuration = builder.Build();

        // Assert
        Assert.Equal("Value1", configuration.Configuration["Setting1"]);
        Assert.Equal("Value2", configuration.Configuration["Setting2"]);
        Assert.Equal("NestedValue", configuration.Configuration["Section:NestedSetting"]);
    }

    [Fact]
    public void Build_AutomaticallyAddsMetadata_CreatedAtAndCreatedBy()
    {
        // Arrange
        var builder = ProfileConfigurationBuilder.Create()
            .WithId("auto-metadata-profile")
            .WithName("Auto Metadata Profile")
            .WithDescription("A profile that gets automatic metadata");

        var beforeBuild = DateTime.UtcNow;

        // Act
        var configuration = builder.Build();

        var afterBuild = DateTime.UtcNow;

        // Assert
        Assert.Contains("CreatedAt", configuration.Metadata.Keys);
        Assert.Contains("CreatedBy", configuration.Metadata.Keys);
        
        var createdAt = (DateTime)configuration.Metadata["CreatedAt"];
        Assert.True(createdAt >= beforeBuild && createdAt <= afterBuild);
        Assert.Equal(Environment.UserName, configuration.Metadata["CreatedBy"]);
    }

    [Fact]
    public void GetSection_WithValidSectionPath_ReturnsTypedObject()
    {
        // Arrange
        var json = """
        {
            "UI": {
                "Theme": "Dark",
                "FontSize": 14,
                "ShowToolbar": true
            }
        }
        """;

        var configuration = ProfileConfigurationBuilder.Create()
            .WithId("typed-section-profile")
            .WithName("Typed Section Profile")
            .WithDescription("A profile for testing typed sections")
            .AddJsonString(json)
            .Build();

        // Act
        var uiSettings = configuration.GetSection<UISettings>("UI");

        // Assert
        Assert.NotNull(uiSettings);
        Assert.Equal("Dark", uiSettings.Theme);
        Assert.Equal(14, uiSettings.FontSize);
        Assert.True(uiSettings.ShowToolbar);
    }

    public class UISettings
    {
        public string Theme { get; set; } = string.Empty;
        public int FontSize { get; set; }
        public bool ShowToolbar { get; set; }
    }
}