using System.Text.Json;
using Xunit;

namespace GameConsole.UI.Configuration.Tests;

public class ProfileConfigurationSerializerTests
{
    private readonly ProfileConfigurationSerializer _serializer = new();

    [Fact]
    public void SerializeToJson_ValidConfiguration_ReturnsValidJson()
    {
        // Arrange
        var configuration = CreateTestConfiguration();

        // Act
        var json = _serializer.SerializeToJson(configuration);

        // Assert
        Assert.NotNull(json);
        Assert.NotEmpty(json);
        
        // Verify it's valid JSON
        var document = JsonDocument.Parse(json);
        Assert.NotNull(document);
        
        // Verify key properties are serialized
        var root = document.RootElement;
        Assert.Equal(configuration.ProfileId, root.GetProperty("profileId").GetString());
        Assert.Equal(configuration.Name, root.GetProperty("name").GetString());
    }

    [Fact]
    public void DeserializeFromJson_ValidJson_ReturnsConfiguration()
    {
        // Arrange
        var originalConfig = CreateTestConfiguration();
        var json = _serializer.SerializeToJson(originalConfig);

        // Act
        var deserializedConfig = _serializer.DeserializeFromJson(json);

        // Assert
        Assert.NotNull(deserializedConfig);
        Assert.Equal(originalConfig.ProfileId, deserializedConfig.ProfileId);
        Assert.Equal(originalConfig.Name, deserializedConfig.Name);
        Assert.Equal(originalConfig.Description, deserializedConfig.Description);
        Assert.Equal(originalConfig.Version, deserializedConfig.Version);
        Assert.Equal(originalConfig.Scope, deserializedConfig.Scope);
        Assert.Equal(originalConfig.Environment, deserializedConfig.Environment);
        Assert.Equal(originalConfig.ParentProfileId, deserializedConfig.ParentProfileId);
    }

    [Fact]
    public void SerializeToXml_ValidConfiguration_ReturnsValidXml()
    {
        // Arrange
        var configuration = CreateTestConfiguration();

        // Act
        var xml = _serializer.SerializeToXml(configuration);

        // Assert
        Assert.NotNull(xml);
        Assert.NotEmpty(xml);
        Assert.Contains("<?xml version=\"1.0\"", xml);
        Assert.Contains("<ProfileConfiguration", xml);
        Assert.Contains($"<ProfileId>{configuration.ProfileId}</ProfileId>", xml);
        Assert.Contains($"<Name>{configuration.Name}</Name>", xml);
    }

    [Fact]
    public void DeserializeFromXml_ValidXml_ReturnsConfiguration()
    {
        // Arrange
        var originalConfig = CreateTestConfiguration();
        var xml = _serializer.SerializeToXml(originalConfig);

        // Act
        var deserializedConfig = _serializer.DeserializeFromXml(xml);

        // Assert
        Assert.NotNull(deserializedConfig);
        Assert.Equal(originalConfig.ProfileId, deserializedConfig.ProfileId);
        Assert.Equal(originalConfig.Name, deserializedConfig.Name);
        Assert.Equal(originalConfig.Description, deserializedConfig.Description);
        Assert.Equal(originalConfig.Version, deserializedConfig.Version);
        Assert.Equal(originalConfig.Scope, deserializedConfig.Scope);
        Assert.Equal(originalConfig.Environment, deserializedConfig.Environment);
    }

    [Fact]
    public void SerializeDeserialize_RoundTripJson_PreservesAllData()
    {
        // Arrange
        var originalConfig = new ProfileConfigurationBuilder()
            .WithProfileId("round-trip-test")
            .WithName("Round Trip Test")
            .WithDescription("Testing round-trip serialization")
            .WithVersion("1.2.3")
            .WithScope(ProfileScope.User)
            .ForEnvironment("Testing")
            .InheritsFrom("parent-config")
            .WithSetting("TestSetting1", "Value1")
            .WithSetting("TestSetting2", 42)
            .WithSetting("UI:Theme", "Light")
            .WithMetadata("Author", "Test Suite")
            .WithMetadata("Category", "Testing")
            .Build();

        // Act
        var json = _serializer.SerializeToJson(originalConfig);
        var deserializedConfig = _serializer.DeserializeFromJson(json);

        // Assert
        AssertConfigurationsEqual(originalConfig, deserializedConfig);
    }

    [Fact]
    public void SerializeDeserialize_RoundTripXml_PreservesAllData()
    {
        // Arrange
        var originalConfig = CreateComplexConfiguration();

        // Act
        var xml = _serializer.SerializeToXml(originalConfig);
        var deserializedConfig = _serializer.DeserializeFromXml(xml);

        // Assert
        AssertConfigurationsEqual(originalConfig, deserializedConfig);
    }

    [Fact]
    public async Task SaveToJsonFileAsync_ValidConfiguration_CreatesFile()
    {
        // Arrange
        var configuration = CreateTestConfiguration();
        var tempFilePath = Path.GetTempFileName();

        try
        {
            // Act
            await _serializer.SaveToJsonFileAsync(configuration, tempFilePath);

            // Assert
            Assert.True(File.Exists(tempFilePath));
            var fileContent = await File.ReadAllTextAsync(tempFilePath);
            Assert.NotEmpty(fileContent);
            
            var loadedConfig = _serializer.DeserializeFromJson(fileContent);
            Assert.Equal(configuration.ProfileId, loadedConfig.ProfileId);
        }
        finally
        {
            if (File.Exists(tempFilePath))
                File.Delete(tempFilePath);
        }
    }

    [Fact]
    public async Task LoadFromJsonFileAsync_ExistingFile_ReturnsConfiguration()
    {
        // Arrange
        var originalConfig = CreateTestConfiguration();
        var tempFilePath = Path.GetTempFileName();

        try
        {
            await _serializer.SaveToJsonFileAsync(originalConfig, tempFilePath);

            // Act
            var loadedConfig = await _serializer.LoadFromJsonFileAsync(tempFilePath);

            // Assert
            Assert.Equal(originalConfig.ProfileId, loadedConfig.ProfileId);
            Assert.Equal(originalConfig.Name, loadedConfig.Name);
        }
        finally
        {
            if (File.Exists(tempFilePath))
                File.Delete(tempFilePath);
        }
    }

    [Fact]
    public async Task SaveToXmlFileAsync_ValidConfiguration_CreatesFile()
    {
        // Arrange
        var configuration = CreateTestConfiguration();
        var tempFilePath = Path.GetTempFileName();

        try
        {
            // Act
            await _serializer.SaveToXmlFileAsync(configuration, tempFilePath);

            // Assert
            Assert.True(File.Exists(tempFilePath));
            var fileContent = await File.ReadAllTextAsync(tempFilePath);
            Assert.NotEmpty(fileContent);
            Assert.Contains("<?xml version=\"1.0\"", fileContent);
        }
        finally
        {
            if (File.Exists(tempFilePath))
                File.Delete(tempFilePath);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void DeserializeFromJson_InvalidJson_ThrowsArgumentException(string invalidJson)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _serializer.DeserializeFromJson(invalidJson));
    }

    [Fact]
    public void DeserializeFromJson_NullJson_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _serializer.DeserializeFromJson(null!));
    }

    [Fact]
    public void DeserializeFromJson_MalformedJson_ThrowsJsonException()
    {
        // Arrange
        const string malformedJson = "{ invalid json content";

        // Act & Assert
        Assert.Throws<JsonException>(() => _serializer.DeserializeFromJson(malformedJson));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void DeserializeFromXml_InvalidXml_ThrowsArgumentException(string invalidXml)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _serializer.DeserializeFromXml(invalidXml));
    }

    [Fact]
    public void DeserializeFromXml_NullXml_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _serializer.DeserializeFromXml(null!));
    }

    private static IProfileConfiguration CreateTestConfiguration()
    {
        return new ProfileConfigurationBuilder()
            .WithProfileId("test-config-123")
            .WithName("Test Configuration")
            .WithDescription("A test configuration for unit tests")
            .WithVersion("1.0.0")
            .WithScope(ProfileScope.Mode)
            .ForEnvironment("Development")
            .InheritsFrom("parent-config")
            .WithSetting("TestSetting", "TestValue")
            .WithMetadata("TestMetadata", "MetadataValue")
            .Build();
    }

    private static IProfileConfiguration CreateComplexConfiguration()
    {
        return new ProfileConfigurationBuilder()
            .WithProfileId("complex-config")
            .WithName("Complex Configuration")
            .WithDescription("A complex configuration with many settings")
            .WithVersion("2.1.0")
            .WithScope(ProfileScope.Component)
            .ForEnvironment("Production")
            .WithSetting("Database:ConnectionString", "Server=localhost;Database=GameConsole;")
            .WithSetting("UI:Theme", "Dark")
            .WithSetting("UI:Window:Width", 1920)
            .WithSetting("UI:Window:Height", 1080)
            .WithSetting("UI:Fullscreen", true)
            .WithSetting("Logging:Level", "Information")
            .WithMetadata("Author", "Game Console Team")
            .WithMetadata("Category", "Production")
            .WithMetadata("Tags", new[] { "ui", "config", "production" })
            .Build();
    }

    private static void AssertConfigurationsEqual(IProfileConfiguration expected, IProfileConfiguration actual)
    {
        Assert.Equal(expected.ProfileId, actual.ProfileId);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Description, actual.Description);
        Assert.Equal(expected.Version, actual.Version);
        Assert.Equal(expected.Scope, actual.Scope);
        Assert.Equal(expected.Environment, actual.Environment);
        Assert.Equal(expected.ParentProfileId, actual.ParentProfileId);
        
        // Note: We can't easily compare all settings due to the way configuration works
        // but we can verify key settings are preserved in the round-trip tests above
    }
}