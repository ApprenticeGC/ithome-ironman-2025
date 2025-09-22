using GameConsole.Configuration.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.Configuration.Security.Tests;

/// <summary>
/// Tests for the UIProfileService class.
/// </summary>
public class UIProfileServiceTests : IDisposable
{
    private readonly TestLogger<UIProfileService> _logger;
    private readonly IConfiguration _configuration;
    private UIProfileService? _service;

    public UIProfileServiceTests()
    {
        _logger = new TestLogger<UIProfileService>();
        _configuration = new ConfigurationBuilder().Build();
    }

    [Fact]
    public async Task InitializeAsync_RegistersDefaultProfiles()
    {
        // Arrange
        _service = new UIProfileService(_logger, _configuration);

        // Act
        await _service.InitializeAsync();

        // Assert
        Assert.Equal(3, _service.AvailableProfiles.Count);
        Assert.Contains(_service.AvailableProfiles, p => p.ProfileId == "tui-default");
        Assert.Contains(_service.AvailableProfiles, p => p.ProfileId == "unity-simulation");
        Assert.Contains(_service.AvailableProfiles, p => p.ProfileId == "godot-simulation");
    }

    [Fact]
    public async Task InitializeAsync_SetsDefaultTUIProfileAsActive()
    {
        // Arrange
        _service = new UIProfileService(_logger, _configuration);

        // Act
        await _service.InitializeAsync();

        // Assert
        Assert.NotNull(_service.ActiveProfile);
        Assert.Equal("tui-default", _service.ActiveProfile?.ProfileId);
    }

    [Fact]
    public async Task StartAsync_SetsIsRunningToTrue()
    {
        // Arrange
        _service = new UIProfileService(_logger, _configuration);
        await _service.InitializeAsync();

        // Act
        await _service.StartAsync();

        // Assert
        Assert.True(_service.IsRunning);
    }

    [Fact]
    public async Task StopAsync_SetsIsRunningToFalse()
    {
        // Arrange
        _service = new UIProfileService(_logger, _configuration);
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        await _service.StopAsync();

        // Assert
        Assert.False(_service.IsRunning);
    }

    [Fact]
    public async Task SwitchToProfileAsync_WithValidProfileId_SwitchesProfile()
    {
        // Arrange
        _service = new UIProfileService(_logger, _configuration);
        await _service.InitializeAsync();
        var targetProfileId = "unity-simulation";

        // Act
        await _service.SwitchToProfileAsync(targetProfileId);

        // Assert
        Assert.NotNull(_service.ActiveProfile);
        Assert.Equal(targetProfileId, _service.ActiveProfile?.ProfileId);
    }

    [Fact]
    public async Task SwitchToProfileAsync_WithInvalidProfileId_ThrowsArgumentException()
    {
        // Arrange
        _service = new UIProfileService(_logger, _configuration);
        await _service.InitializeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SwitchToProfileAsync("non-existent-profile"));
    }

    [Fact]
    public async Task SwitchToProfileAsync_WithNullOrEmptyProfileId_ThrowsArgumentException()
    {
        // Arrange
        _service = new UIProfileService(_logger, _configuration);
        await _service.InitializeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SwitchToProfileAsync(null!));
        
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SwitchToProfileAsync(""));
        
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SwitchToProfileAsync("   "));
    }

    [Fact]
    public async Task SwitchToProfileAsync_WithSameProfile_DoesNothing()
    {
        // Arrange
        _service = new UIProfileService(_logger, _configuration);
        await _service.InitializeAsync();
        var originalProfileId = _service.ActiveProfile?.ProfileId;

        // Act
        await _service.SwitchToProfileAsync(originalProfileId!);

        // Assert
        Assert.Equal(originalProfileId, _service.ActiveProfile?.ProfileId);
    }

    [Fact]
    public async Task SwitchToProfileAsync_RaisesProfileChangedEvent()
    {
        // Arrange
        _service = new UIProfileService(_logger, _configuration);
        await _service.InitializeAsync();
        
        UIProfileChangedEventArgs? eventArgs = null;
        _service.ProfileChanged += (sender, args) => eventArgs = args;

        var targetProfileId = "unity-simulation";

        // Act
        await _service.SwitchToProfileAsync(targetProfileId);

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal("tui-default", eventArgs.PreviousProfile?.ProfileId);
        Assert.Equal(targetProfileId, eventArgs.NewProfile?.ProfileId);
    }

    [Fact]
    public async Task GetProfile_WithValidProfileId_ReturnsProfile()
    {
        // Arrange
        _service = new UIProfileService(_logger, _configuration);
        await _service.InitializeAsync();

        // Act
        var profile = _service.GetProfile("unity-simulation");

        // Assert
        Assert.NotNull(profile);
        Assert.Equal("unity-simulation", profile.ProfileId);
    }

    [Fact]
    public async Task GetProfile_WithInvalidProfileId_ReturnsNull()
    {
        // Arrange
        _service = new UIProfileService(_logger, _configuration);
        await _service.InitializeAsync();

        // Act
        var profile = _service.GetProfile("non-existent");

        // Assert
        Assert.Null(profile);
    }

    [Fact]
    public async Task RegisterProfile_WithValidProfile_AddsProfile()
    {
        // Arrange
        _service = new UIProfileService(_logger, _configuration);
        await _service.InitializeAsync();
        
        var customProfile = new UIProfileConfiguration(
            "custom-test",
            "Custom Test Profile",
            "Test profile",
            UIMode.Custom);

        var originalCount = _service.AvailableProfiles.Count;

        // Act
        _service.RegisterProfile(customProfile);

        // Assert
        Assert.Equal(originalCount + 1, _service.AvailableProfiles.Count);
        Assert.Contains(_service.AvailableProfiles, p => p.ProfileId == "custom-test");
    }

    [Fact]
    public async Task RegisterProfile_WithDuplicateProfileId_ThrowsArgumentException()
    {
        // Arrange
        _service = new UIProfileService(_logger, _configuration);
        await _service.InitializeAsync();
        
        var duplicateProfile = new UIProfileConfiguration(
            "tui-default", // This already exists
            "Duplicate Profile",
            "Duplicate test profile",
            UIMode.TUI);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.RegisterProfile(duplicateProfile));
    }

    [Fact]
    public async Task RegisterProfile_WithNullProfile_ThrowsArgumentNullException()
    {
        // Arrange
        _service = new UIProfileService(_logger, _configuration);
        await _service.InitializeAsync();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _service.RegisterProfile(null!));
    }

    [Fact]
    public async Task UnregisterProfile_WithExistingProfile_RemovesProfile()
    {
        // Arrange
        _service = new UIProfileService(_logger, _configuration);
        await _service.InitializeAsync();
        
        var customProfile = new UIProfileConfiguration(
            "custom-test",
            "Custom Test Profile",
            "Test profile",
            UIMode.Custom);
        _service.RegisterProfile(customProfile);

        var originalCount = _service.AvailableProfiles.Count;

        // Act
        var result = _service.UnregisterProfile("custom-test");

        // Assert
        Assert.True(result);
        Assert.Equal(originalCount - 1, _service.AvailableProfiles.Count);
        Assert.DoesNotContain(_service.AvailableProfiles, p => p.ProfileId == "custom-test");
    }

    [Fact]
    public async Task UnregisterProfile_WithNonExistentProfile_ReturnsFalse()
    {
        // Arrange
        _service = new UIProfileService(_logger, _configuration);
        await _service.InitializeAsync();

        // Act
        var result = _service.UnregisterProfile("non-existent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UnregisterProfile_WithActiveProfile_ClearsActiveProfile()
    {
        // Arrange
        _service = new UIProfileService(_logger, _configuration);
        await _service.InitializeAsync();
        
        var customProfile = new UIProfileConfiguration(
            "custom-test",
            "Custom Test Profile",
            "Test profile",
            UIMode.Custom);
        _service.RegisterProfile(customProfile);
        await _service.SwitchToProfileAsync("custom-test");

        UIProfileChangedEventArgs? eventArgs = null;
        _service.ProfileChanged += (sender, args) => eventArgs = args;

        // Act
        var result = _service.UnregisterProfile("custom-test");

        // Assert
        Assert.True(result);
        Assert.Null(_service.ActiveProfile);
        Assert.NotNull(eventArgs);
        Assert.Equal("custom-test", eventArgs.PreviousProfile?.ProfileId);
        Assert.Null(eventArgs.NewProfile);
    }

    [Theory]
    [InlineData(UIMode.TUI)]
    [InlineData(UIMode.Unity)]
    [InlineData(UIMode.Godot)]
    [InlineData(UIMode.Custom)]
    public async Task DefaultProfiles_HaveCorrectUIMode(UIMode expectedMode)
    {
        // Arrange
        _service = new UIProfileService(_logger, _configuration);
        await _service.InitializeAsync();

        // Act & Assert
        var profile = expectedMode switch
        {
            UIMode.TUI => _service.GetProfile("tui-default"),
            UIMode.Unity => _service.GetProfile("unity-simulation"),
            UIMode.Godot => _service.GetProfile("godot-simulation"),
            _ => null
        };

        if (expectedMode != UIMode.Custom)
        {
            Assert.NotNull(profile);
            Assert.Equal(expectedMode, profile!.Mode);
        }
    }

    [Fact]
    public async Task DefaultTUIProfile_HasExpectedSettings()
    {
        // Arrange
        _service = new UIProfileService(_logger, _configuration);
        await _service.InitializeAsync();

        // Act
        var profile = _service.GetProfile("tui-default");

        // Assert
        Assert.NotNull(profile);
        Assert.Equal(120, profile.Settings["ConsoleWidth"]);
        Assert.Equal(40, profile.Settings["ConsoleHeight"]);
        Assert.Equal("Dark", profile.Settings["ColorTheme"]);
    }

    [Fact]
    public async Task UnitySimulationProfile_HasExpectedSettings()
    {
        // Arrange
        _service = new UIProfileService(_logger, _configuration);
        await _service.InitializeAsync();

        // Act
        var profile = _service.GetProfile("unity-simulation");

        // Assert
        Assert.NotNull(profile);
        Assert.Equal(1920, profile.Settings["WindowWidth"]);
        Assert.Equal(1080, profile.Settings["WindowHeight"]);
        Assert.Equal(60, profile.Settings["TargetFrameRate"]);
        Assert.Equal(true, profile.Settings["VSync"]);
    }

    [Fact]
    public async Task GodotSimulationProfile_HasExpectedSettings()
    {
        // Arrange
        _service = new UIProfileService(_logger, _configuration);
        await _service.InitializeAsync();

        // Act
        var profile = _service.GetProfile("godot-simulation");

        // Assert
        Assert.NotNull(profile);
        Assert.Equal(1280, profile.Settings["WindowWidth"]);
        Assert.Equal(720, profile.Settings["WindowHeight"]);
        Assert.Equal(60, profile.Settings["TargetFrameRate"]);
        Assert.Equal(false, profile.Settings["Fullscreen"]);
    }

    public void Dispose()
    {
        _service?.DisposeAsync().AsTask().Wait();
    }
}

/// <summary>
/// Simple test logger implementation for testing.
/// </summary>
public class TestLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => new TestScope();
    
    public bool IsEnabled(LogLevel logLevel) => true;
    
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        // For tests, we don't need to actually log anything
    }
    
    private class TestScope : IDisposable
    {
        public void Dispose() { }
    }
}