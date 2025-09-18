namespace GameConsole.UI.Configuration.Tests;

public class ProfileConfigurationTests
{
    private readonly IProfileConfiguration _testConfig;

    public ProfileConfigurationTests()
    {
        _testConfig = new ProfileConfigurationBuilder()
            .WithId("test-config")
            .WithName("Test Configuration")
            .WithDescription("A configuration for testing")
            .WithVersion(1, 0, 0)
            .WithScope(ConfigurationScope.User)
            .WithEnvironment("Test")
            .WithSetting("stringValue", "hello")
            .WithSetting("intValue", 42)
            .WithSetting("boolValue", true)
            .WithSetting("nullValue", null)
            .Build();
    }

    [Fact]
    public void GetValue_WithExistingKey_ReturnsCorrectValue()
    {
        // Act & Assert
        Assert.Equal("hello", _testConfig.GetValue<string>("stringValue"));
        Assert.Equal(42, _testConfig.GetValue<int>("intValue"));
        Assert.True(_testConfig.GetValue<bool>("boolValue"));
    }

    [Fact]
    public void GetValue_WithNonExistentKey_ReturnsDefault()
    {
        // Act & Assert
        Assert.Null(_testConfig.GetValue<string>("nonexistent"));
        Assert.Equal(0, _testConfig.GetValue<int>("nonexistent"));
        Assert.False(_testConfig.GetValue<bool>("nonexistent"));
    }

    [Fact]
    public void GetValue_WithDefaultValue_ReturnsDefaultForNonExistentKey()
    {
        // Act & Assert
        Assert.Equal("default", _testConfig.GetValue("nonexistent", "default"));
        Assert.Equal(999, _testConfig.GetValue("nonexistent", 999));
    }

    [Fact]
    public void GetValue_WithDefaultValue_ReturnsActualValueForExistingKey()
    {
        // Act & Assert
        Assert.Equal("hello", _testConfig.GetValue("stringValue", "default"));
        Assert.Equal(42, _testConfig.GetValue("intValue", 999));
    }

    [Fact]
    public void HasValue_WithExistingKey_ReturnsTrue()
    {
        // Act & Assert
        Assert.True(_testConfig.HasValue("stringValue"));
        Assert.True(_testConfig.HasValue("intValue"));
        Assert.True(_testConfig.HasValue("nullValue")); // null values still count as existing
    }

    [Fact]
    public void HasValue_WithNonExistentKey_ReturnsFalse()
    {
        // Act & Assert
        Assert.False(_testConfig.HasValue("nonexistent"));
    }

    [Fact]
    public async Task ValidateAsync_WithValidConfiguration_ReturnsSuccess()
    {
        // Act
        var result = await _testConfig.ValidateAsync();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void WithOverrides_AppliesOverrides_ReturnsNewConfigurationWithChanges()
    {
        // Arrange
        var overrides = new Dictionary<string, object?>
        {
            ["stringValue"] = "overridden",
            ["newValue"] = "added"
        };

        // Act
        var newConfig = _testConfig.WithOverrides(overrides);

        // Assert
        Assert.Equal("overridden", newConfig.GetValue<string>("stringValue"));
        Assert.Equal("added", newConfig.GetValue<string>("newValue"));
        Assert.Equal(42, newConfig.GetValue<int>("intValue")); // Unchanged values preserved
        
        // Original config should be unchanged
        Assert.Equal("hello", _testConfig.GetValue<string>("stringValue"));
        Assert.False(_testConfig.HasValue("newValue"));
    }

    [Fact]
    public void ToJson_SerializesConfiguration_ReturnsValidJson()
    {
        // Act
        var json = _testConfig.ToJson();

        // Assert
        Assert.NotEmpty(json);
        Assert.Contains("\"Id\": \"test-config\"", json);
        Assert.Contains("\"Name\": \"Test Configuration\"", json);
        Assert.Contains("\"stringValue\": \"hello\"", json);
    }

    [Fact]
    public void ToXml_SerializesConfiguration_ReturnsValidXml()
    {
        // Act
        var xml = _testConfig.ToXml();

        // Assert
        Assert.NotEmpty(xml);
        Assert.Contains("<Id>test-config</Id>", xml);
        Assert.Contains("<Name>Test Configuration</Name>", xml);
        Assert.Contains("key=\"stringValue\"", xml);
        Assert.Contains("value=\"hello\"", xml);
    }

    [Theory]
    [InlineData("42", typeof(int), 42)]
    [InlineData("true", typeof(bool), true)]
    [InlineData("3.14", typeof(double), 3.14)]
    public void GetValue_WithTypeConversion_ConvertsCorrectly(string storedValue, Type expectedType, object expectedValue)
    {
        // Arrange
        var config = new ProfileConfigurationBuilder()
            .WithId("conversion-test")
            .WithName("Conversion Test")
            .WithSetting("convertibleValue", storedValue)
            .Build();

        // Act
        var result = config.GetValue<int>("convertibleValue") if expectedType == typeof(int) else
                    config.GetValue<bool>("convertibleValue") if expectedType == typeof(bool) else
                    config.GetValue<double>("convertibleValue");

        // Assert
        Assert.Equal(expectedValue, result);
    }
}