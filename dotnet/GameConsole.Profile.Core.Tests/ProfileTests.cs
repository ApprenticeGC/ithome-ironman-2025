using GameConsole.Profile.Core;
using Xunit;

namespace GameConsole.Profile.Core.Tests;

/// <summary>
/// Tests for the Profile class.
/// </summary>
public class ProfileTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesProfile()
    {
        // Arrange
        var id = "test-id";
        var name = "Test Profile";
        var type = ProfileType.Custom;
        var description = "Test description";

        // Act
        var profile = new Profile(id, name, type, description);

        // Assert
        Assert.Equal(id, profile.Id);
        Assert.Equal(name, profile.Name);
        Assert.Equal(type, profile.Type);
        Assert.Equal(description, profile.Description);
        Assert.False(profile.IsReadOnly);
        Assert.Equal("1.0", profile.Version);
        Assert.Empty(profile.ServiceConfigurations);
        Assert.True(profile.Created <= DateTime.UtcNow);
        Assert.True(profile.LastModified <= DateTime.UtcNow);
    }

    [Fact]
    public void Constructor_WithNullId_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new Profile(null!, "name", ProfileType.Custom));
    }

    [Fact]
    public void Constructor_WithNullName_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new Profile("id", null!, ProfileType.Custom));
    }

    [Fact]
    public void SetServiceConfiguration_WithValidParameters_AddsConfiguration()
    {
        // Arrange
        var profile = new Profile("id", "name", ProfileType.Custom);
        var config = new ServiceConfiguration
        {
            Implementation = "TestService",
            Enabled = true
        };

        // Act
        profile.SetServiceConfiguration("ITestService", config);

        // Assert
        Assert.Single(profile.ServiceConfigurations);
        Assert.True(profile.ServiceConfigurations.ContainsKey("ITestService"));
        Assert.Equal(config, profile.ServiceConfigurations["ITestService"]);
    }

    [Fact]
    public void SetServiceConfiguration_OnReadOnlyProfile_ThrowsInvalidOperationException()
    {
        // Arrange
        var profile = new Profile("id", "name", ProfileType.Custom, isReadOnly: true);
        var config = new ServiceConfiguration { Implementation = "TestService" };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            profile.SetServiceConfiguration("ITestService", config));
    }

    [Fact]
    public void RemoveServiceConfiguration_WithExistingService_RemovesAndReturnsTrue()
    {
        // Arrange
        var profile = new Profile("id", "name", ProfileType.Custom);
        var config = new ServiceConfiguration { Implementation = "TestService" };
        profile.SetServiceConfiguration("ITestService", config);

        // Act
        var result = profile.RemoveServiceConfiguration("ITestService");

        // Assert
        Assert.True(result);
        Assert.Empty(profile.ServiceConfigurations);
    }

    [Fact]
    public void RemoveServiceConfiguration_WithNonExistentService_ReturnsFalse()
    {
        // Arrange
        var profile = new Profile("id", "name", ProfileType.Custom);

        // Act
        var result = profile.RemoveServiceConfiguration("ITestService");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RemoveServiceConfiguration_OnReadOnlyProfile_ThrowsInvalidOperationException()
    {
        // Arrange
        var profile = new Profile("id", "name", ProfileType.Custom, isReadOnly: true);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            profile.RemoveServiceConfiguration("ITestService"));
    }

    [Fact]
    public void CreateCopy_CreatesIndependentCopy()
    {
        // Arrange
        var original = new Profile("original-id", "Original Profile", ProfileType.Unity, "Original description");
        var config = new ServiceConfiguration
        {
            Implementation = "TestService",
            Capabilities = new List<string> { "Cap1", "Cap2" },
            Settings = new Dictionary<string, object> { { "setting1", "value1" } },
            Enabled = true
        };
        original.SetServiceConfiguration("ITestService", config);

        // Act
        var copy = original.CreateCopy("copy-id", "Copy Profile");

        // Assert
        Assert.Equal("copy-id", copy.Id);
        Assert.Equal("Copy Profile", copy.Name);
        Assert.Equal(ProfileType.Unity, copy.Type);
        Assert.Equal("Original description", copy.Description);
        Assert.False(copy.IsReadOnly);
        Assert.Single(copy.ServiceConfigurations);
        
        // Verify independence
        Assert.NotSame(original.ServiceConfigurations["ITestService"], copy.ServiceConfigurations["ITestService"]);
        Assert.NotSame(original.ServiceConfigurations["ITestService"].Capabilities, 
                      copy.ServiceConfigurations["ITestService"].Capabilities);
        Assert.NotSame(original.ServiceConfigurations["ITestService"].Settings, 
                      copy.ServiceConfigurations["ITestService"].Settings);
    }

    [Theory]
    [InlineData(ProfileType.Custom)]
    [InlineData(ProfileType.Unity)]
    [InlineData(ProfileType.Godot)]
    [InlineData(ProfileType.Minimal)]
    [InlineData(ProfileType.Development)]
    [InlineData(ProfileType.Default)]
    public void ProfileType_AllValuesSupported(ProfileType profileType)
    {
        // Act
        var profile = new Profile("id", "name", profileType);

        // Assert
        Assert.Equal(profileType, profile.Type);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var profile = new Profile("id", "Test Profile", ProfileType.Unity);
        profile.SetServiceConfiguration("ITestService", new ServiceConfiguration { Implementation = "TestService" });

        // Act
        var result = profile.ToString();

        // Assert
        Assert.Contains("Test Profile", result);
        Assert.Contains("Unity", result);
        Assert.Contains("1 services", result);
    }
}