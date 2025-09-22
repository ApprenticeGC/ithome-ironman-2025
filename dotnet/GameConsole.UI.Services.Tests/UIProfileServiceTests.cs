using GameConsole.UI.Core;
using GameConsole.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameConsole.UI.Services.Tests;

public class UIProfileServiceTests
{
    private readonly Mock<ILogger<UIProfileService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly UIProfileService _service;

    public UIProfileServiceTests()
    {
        _mockLogger = new Mock<ILogger<UIProfileService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _service = new UIProfileService(_mockLogger.Object, _mockConfiguration.Object);
    }

    [Fact]
    public async Task InitializeAsync_LoadsDefaultProfiles()
    {
        // Arrange & Act
        await _service.InitializeAsync();

        // Assert
        var profiles = await _service.GetAvailableProfilesAsync();
        Assert.NotEmpty(profiles);
        Assert.Contains(profiles, p => p.ProfileType == UIProfileType.CustomTUI);
        Assert.Contains(profiles, p => p.ProfileType == UIProfileType.Unity);
        Assert.Contains(profiles, p => p.ProfileType == UIProfileType.Godot);
        Assert.Contains(profiles, p => p.ProfileType == UIProfileType.Default);
    }

    [Fact]
    public async Task StartAsync_SetsDefaultProfile()
    {
        // Arrange
        await _service.InitializeAsync();

        // Act
        await _service.StartAsync();

        // Assert
        Assert.NotNull(_service.ActiveProfile);
        Assert.True(_service.IsRunning);
    }

    [Fact]
    public async Task GetProfileByIdAsync_ReturnsCorrectProfile()
    {
        // Arrange
        await _service.InitializeAsync();
        var profiles = await _service.GetAvailableProfilesAsync();
        var expectedProfile = profiles.First();

        // Act
        var result = await _service.GetProfileByIdAsync(expectedProfile.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedProfile.Id, result.Id);
        Assert.Equal(expectedProfile.Name, result.Name);
    }

    [Fact]
    public async Task GetProfileByIdAsync_ReturnsNullForInvalidId()
    {
        // Arrange
        await _service.InitializeAsync();

        // Act
        var result = await _service.GetProfileByIdAsync("invalid-id");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetProfilesByTypeAsync_ReturnsMatchingProfiles()
    {
        // Arrange
        await _service.InitializeAsync();

        // Act
        var unityProfiles = await _service.GetProfilesByTypeAsync(UIProfileType.Unity);
        var tuiProfiles = await _service.GetProfilesByTypeAsync(UIProfileType.CustomTUI);

        // Assert
        Assert.All(unityProfiles, p => Assert.Equal(UIProfileType.Unity, p.ProfileType));
        Assert.All(tuiProfiles, p => Assert.Equal(UIProfileType.CustomTUI, p.ProfileType));
    }

    [Fact]
    public async Task SwitchToProfileAsync_ChangesActiveProfile()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        
        var profiles = await _service.GetAvailableProfilesAsync();
        var targetProfile = profiles.First(p => p.Id != _service.ActiveProfile?.Id);
        var originalProfile = _service.ActiveProfile;

        // Act
        var result = await _service.SwitchToProfileAsync(targetProfile.Id);

        // Assert
        Assert.True(result);
        Assert.NotNull(_service.ActiveProfile);
        Assert.Equal(targetProfile.Id, _service.ActiveProfile.Id);
        Assert.NotEqual(originalProfile?.Id, _service.ActiveProfile.Id);
    }

    [Fact]
    public async Task SwitchToProfileAsync_ReturnsFalseForInvalidProfile()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.SwitchToProfileAsync("invalid-id");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateAllProfilesAsync_ReturnsValidationResults()
    {
        // Arrange
        await _service.InitializeAsync();

        // Act
        var results = await _service.ValidateAllProfilesAsync();

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results.Values, r => Assert.NotNull(r));
    }

    [Fact]
    public async Task HasCapabilityAsync_ReturnsTrueForSupportedTypes()
    {
        // Arrange & Act
        var hasUIProfileService = await _service.HasCapabilityAsync<IUIProfileService>();
        var hasUIProfile = await _service.HasCapabilityAsync<IUIProfile>();

        // Assert
        Assert.True(hasUIProfileService);
        Assert.True(hasUIProfile);
    }

    [Fact]
    public async Task GetCapabilityAsync_ReturnsCorrectCapability()
    {
        // Arrange & Act
        var serviceCapability = await _service.GetCapabilityAsync<IUIProfileService>();

        // Assert
        Assert.NotNull(serviceCapability);
        Assert.Same(_service, serviceCapability);
    }

    [Fact]
    public async Task StopAsync_DeactivatesActiveProfile()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        await _service.StopAsync();

        // Assert
        Assert.False(_service.IsRunning);
    }

    [Fact]
    public async Task DisposeAsync_CleansUpResources()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        await _service.DisposeAsync();

        // Assert
        Assert.False(_service.IsRunning);
        
        // Verify that calling methods after disposal throws
        await Assert.ThrowsAsync<ObjectDisposedException>(() => _service.GetAvailableProfilesAsync());
    }
}