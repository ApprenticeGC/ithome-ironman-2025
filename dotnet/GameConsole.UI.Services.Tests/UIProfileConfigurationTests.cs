using FluentAssertions;
using GameConsole.Core.Abstractions;
using GameConsole.UI.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameConsole.UI.Services.Tests;

/// <summary>
/// Unit tests for the UI Profile Configuration System.
/// Tests the core functionality of profile management and switching.
/// </summary>
public class UIProfileConfigurationTests
{
    private readonly ILogger<UIProfileConfigurationService> _logger;

    public UIProfileConfigurationTests()
    {
        _logger = new NullLogger<UIProfileConfigurationService>();
    }

    [Fact]
    public async Task InitializeAsync_ShouldRegisterBuiltInProfiles()
    {
        // Arrange
        var service = new UIProfileConfigurationService(_logger);

        // Act
        await service.InitializeAsync();

        // Assert
        var profiles = await service.GetAvailableProfilesAsync();
        profiles.Should().HaveCount(3);
        
        var profileTypes = profiles.Select(p => p.ProfileType).ToList();
        profileTypes.Should().Contain(new[] { UIProfileType.TUI, UIProfileType.Unity, UIProfileType.Godot });
    }

    [Fact]
    public async Task StartAsync_ShouldActivateDefaultProfile()
    {
        // Arrange
        var service = new UIProfileConfigurationService(_logger);
        await service.InitializeAsync();

        // Act
        await service.StartAsync();

        // Assert
        service.ActiveProfile.Should().NotBeNull();
        service.ActiveProfile!.ProfileType.Should().Be(UIProfileType.TUI); // TUI should be default
        service.ActiveProfile.IsActive.Should().BeTrue();
        service.IsRunning.Should().BeTrue();
    }

    [Fact]
    public async Task SwitchProfileAsync_ShouldChangeActiveProfile()
    {
        // Arrange
        var service = new UIProfileConfigurationService(_logger);
        await service.InitializeAsync();
        await service.StartAsync();

        var profiles = await service.GetAvailableProfilesAsync();
        var unityProfile = profiles.First(p => p.ProfileType == UIProfileType.Unity);
        
        // Act
        await service.SwitchProfileAsync(unityProfile.Id);

        // Assert
        service.ActiveProfile.Should().NotBeNull();
        service.ActiveProfile!.Id.Should().Be(unityProfile.Id);
        service.ActiveProfile.ProfileType.Should().Be(UIProfileType.Unity);
        service.ActiveProfile.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task SwitchProfileAsync_ShouldRaiseProfileChangedEvent()
    {
        // Arrange
        var service = new UIProfileConfigurationService(_logger);
        await service.InitializeAsync();
        await service.StartAsync();

        var profiles = await service.GetAvailableProfilesAsync();
        var godotProfile = profiles.First(p => p.ProfileType == UIProfileType.Godot);
        
        UIProfileChangeEventArgs? eventArgs = null;
        service.ProfileChanged += (sender, args) => eventArgs = args;

        // Act
        await service.SwitchProfileAsync(godotProfile.Id);

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.NewProfile.Id.Should().Be(godotProfile.Id);
        eventArgs.PreviousProfile.Should().NotBeNull();
        eventArgs.PreviousProfile!.ProfileType.Should().Be(UIProfileType.TUI);
    }

    [Fact]
    public async Task GetProfilesByTypeAsync_ShouldFilterCorrectly()
    {
        // Arrange
        var service = new UIProfileConfigurationService(_logger);
        await service.InitializeAsync();

        // Act
        var tuiProfiles = await service.GetProfilesByTypeAsync(UIProfileType.TUI);
        var unityProfiles = await service.GetProfilesByTypeAsync(UIProfileType.Unity);

        // Assert
        tuiProfiles.Should().HaveCount(1);
        tuiProfiles.First().ProfileType.Should().Be(UIProfileType.TUI);
        
        unityProfiles.Should().HaveCount(1);
        unityProfiles.First().ProfileType.Should().Be(UIProfileType.Unity);
    }

    [Fact]
    public async Task RegisterProfileAsync_ShouldAddNewProfile()
    {
        // Arrange
        var service = new UIProfileConfigurationService(_logger);
        await service.InitializeAsync();
        
        var customProfile = new CustomTestProfile();

        // Act
        await service.RegisterProfileAsync(customProfile);

        // Assert
        var profiles = await service.GetAvailableProfilesAsync();
        profiles.Should().HaveCount(4); // 3 built-in + 1 custom
        
        var retrievedProfile = await service.GetProfileAsync(customProfile.Id);
        retrievedProfile.Should().NotBeNull();
        retrievedProfile!.Id.Should().Be(customProfile.Id);
    }

    [Fact]
    public async Task ValidateProfileAsync_ShouldReturnTrueForValidProfile()
    {
        // Arrange
        var service = new UIProfileConfigurationService(_logger);
        var validProfile = new CustomTestProfile();

        // Act
        var isValid = await service.ValidateProfileAsync(validProfile);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateProfileAsync_ShouldReturnFalseForInvalidProfile()
    {
        // Arrange
        var service = new UIProfileConfigurationService(_logger);

        // Act
        var isValid = await service.ValidateProfileAsync(null!);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task StopAsync_ShouldDeactivateActiveProfile()
    {
        // Arrange
        var service = new UIProfileConfigurationService(_logger);
        await service.InitializeAsync();
        await service.StartAsync();
        
        var activeProfile = service.ActiveProfile;

        // Act
        await service.StopAsync();

        // Assert
        service.IsRunning.Should().BeFalse();
        activeProfile.Should().NotBeNull();
        activeProfile!.IsActive.Should().BeFalse();
    }

    [Fact]
    public void HasCapabilityAsync_ShouldReturnTrueForIUIProfileProvider()
    {
        // Arrange
        var service = new UIProfileConfigurationService(_logger);

        // Act
        var hasCapability = service.HasCapabilityAsync<IUIProfileProvider>();

        // Assert
        hasCapability.Result.Should().BeTrue();
    }

    [Fact]
    public void GetCapabilityAsync_ShouldReturnServiceForIUIProfileProvider()
    {
        // Arrange
        var service = new UIProfileConfigurationService(_logger);

        // Act
        var capability = service.GetCapabilityAsync<IUIProfileProvider>();

        // Assert
        capability.Result.Should().NotBeNull();
        capability.Result.Should().BeSameAs(service);
    }
}

/// <summary>
/// Custom test profile for testing profile registration.
/// </summary>
internal class CustomTestProfile : BaseUIProfile
{
    public CustomTestProfile() 
        : base("custom-test", "Custom Test Profile", 
               "A test profile for unit testing", 
               UIProfileType.Custom, "1.0.0", 
               new NullLogger<CustomTestProfile>())
    {
    }

    protected override void InitializeDefaultConfiguration()
    {
        base.InitializeDefaultConfiguration();
        SetConfiguration("TestSetting", "TestValue");
    }
}