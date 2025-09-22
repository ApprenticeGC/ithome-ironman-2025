using GameConsole.UI.Profiles;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameConsole.UI.Profiles.Tests;

public class UIProfileServiceTests
{
    private readonly Mock<ILogger<UIProfileService>> _loggerMock;
    private readonly UIProfileService _service;

    public UIProfileServiceTests()
    {
        _loggerMock = new Mock<ILogger<UIProfileService>>();
        _service = new UIProfileService(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        // Assert
        Assert.NotNull(_service);
        Assert.Null(_service.ActiveProfile);
        Assert.Empty(_service.Profiles);
        Assert.False(_service.IsRunning);
    }

    [Fact]
    public async Task InitializeAsync_RegistersDefaultTUIProfile()
    {
        // Act
        await _service.InitializeAsync();

        // Assert
        var profiles = _service.Profiles;
        Assert.Single(profiles);
        
        var defaultProfile = profiles.First();
        Assert.Equal(TUIProfile.DefaultId, defaultProfile.Id);
        Assert.Equal(UIMode.TUI, defaultProfile.Mode);
    }

    [Fact]
    public async Task StartAsync_ActivatesDefaultProfile()
    {
        // Arrange
        await _service.InitializeAsync();

        // Act
        await _service.StartAsync();
        
        // Give it a moment for the background task
        await Task.Delay(50);

        // Assert
        Assert.True(_service.IsRunning);
        Assert.NotNull(_service.ActiveProfile);
        Assert.Equal(TUIProfile.DefaultId, _service.ActiveProfile.Id);
        Assert.True(_service.ActiveProfile.IsActive);
    }

    [Fact]
    public async Task RegisterProfileAsync_WithValidProfile_RegistersSuccessfully()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var testProfile = new TestUIProfile("test-profile", "Test Profile", "Test Description", mockLogger.Object);

        // Act
        await _service.RegisterProfileAsync(testProfile);

        // Assert
        var profiles = _service.Profiles;
        Assert.Contains(testProfile, profiles);
        Assert.Equal(testProfile, _service.GetProfile("test-profile"));
    }

    [Fact]
    public async Task RegisterProfileAsync_WithNullProfile_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.RegisterProfileAsync(null!));
    }

    [Fact]
    public async Task ActivateProfileAsync_WithValidProfile_ActivatesCorrectly()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var testProfile = new TestUIProfile("test-profile", "Test Profile", "Test Description", mockLogger.Object);
        
        await _service.InitializeAsync();
        await _service.StartAsync();
        await _service.RegisterProfileAsync(testProfile);

        bool eventFired = false;
        IUIProfile? previousProfile = null;
        IUIProfile? newProfile = null;

        _service.ProfileChanged += (sender, e) =>
        {
            eventFired = true;
            previousProfile = e.PreviousProfile;
            newProfile = e.NewProfile;
        };

        // Act
        await _service.ActivateProfileAsync("test-profile");

        // Assert
        Assert.Equal(testProfile, _service.ActiveProfile);
        Assert.True(testProfile.IsActive);
        Assert.True(eventFired);
        Assert.NotNull(previousProfile); // Should be the default TUI profile
        Assert.Equal(testProfile, newProfile);
    }

    [Fact]
    public async Task ActivateProfileAsync_WithNonExistentProfile_ThrowsInvalidOperationException()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.ActivateProfileAsync("non-existent-profile"));
    }

    [Fact]
    public async Task GetProfilesByMode_ReturnsCorrectProfiles()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var tuiProfile1 = new TestUIProfile("tui-1", "TUI 1", "Description", mockLogger.Object, UIMode.TUI);
        var tuiProfile2 = new TestUIProfile("tui-2", "TUI 2", "Description", mockLogger.Object, UIMode.TUI);
        var unityProfile = new TestUIProfile("unity-1", "Unity 1", "Description", mockLogger.Object, UIMode.UnityStyle);

        await _service.RegisterProfileAsync(tuiProfile1);
        await _service.RegisterProfileAsync(tuiProfile2);
        await _service.RegisterProfileAsync(unityProfile);

        // Act
        var tuiProfiles = _service.GetProfilesByMode(UIMode.TUI);
        var unityProfiles = _service.GetProfilesByMode(UIMode.UnityStyle);
        var godotProfiles = _service.GetProfilesByMode(UIMode.GodotStyle);

        // Assert
        Assert.Equal(3, tuiProfiles.Count); // Including default TUI profile
        Assert.Single(unityProfiles);
        Assert.Empty(godotProfiles);
    }

    [Fact]
    public async Task UnregisterProfileAsync_RemovesProfileCorrectly()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var testProfile = new TestUIProfile("test-profile", "Test Profile", "Test Description", mockLogger.Object);
        
        await _service.RegisterProfileAsync(testProfile);
        Assert.Contains(testProfile, _service.Profiles);

        // Act
        await _service.UnregisterProfileAsync("test-profile");

        // Assert
        Assert.DoesNotContain(testProfile, _service.Profiles);
        Assert.Null(_service.GetProfile("test-profile"));
    }

    [Fact]
    public async Task UnregisterProfileAsync_WithActiveProfile_DeactivatesFirst()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var testProfile = new TestUIProfile("test-profile", "Test Profile", "Test Description", mockLogger.Object);
        
        await _service.InitializeAsync();
        await _service.StartAsync();
        await _service.RegisterProfileAsync(testProfile);
        await _service.ActivateProfileAsync("test-profile");

        Assert.Equal(testProfile, _service.ActiveProfile);
        Assert.True(testProfile.IsActive);

        // Act
        await _service.UnregisterProfileAsync("test-profile");

        // Assert
        Assert.Null(_service.ActiveProfile);
        Assert.False(testProfile.IsActive);
    }

    [Fact]
    public async Task StopAsync_DeactivatesCurrentProfile()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        
        // Give it a moment for the background task to activate default profile
        await Task.Delay(50);
        
        var activeProfile = _service.ActiveProfile;
        Assert.NotNull(activeProfile);
        Assert.True(activeProfile.IsActive);

        // Act
        await _service.StopAsync();

        // Assert
        Assert.False(_service.IsRunning);
        Assert.Null(_service.ActiveProfile);
        Assert.False(activeProfile.IsActive);
    }

    [Fact]
    public async Task DisposeAsync_CleansUpCorrectly()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        await Task.Delay(50); // Give it time to activate default profile

        // Act
        await _service.DisposeAsync();

        // Assert
        Assert.False(_service.IsRunning);
        Assert.Empty(_service.Profiles);
        Assert.Null(_service.ActiveProfile);
    }

    // Test helper class
    private class TestUIProfile : BaseUIProfile
    {
        public TestUIProfile(string id, string name, string description, ILogger logger, UIMode mode = UIMode.Custom)
            : base(id, name, description, mode, logger)
        {
        }

        protected override Task OnActivateAsync(CancellationToken cancellationToken = default)
        {
            SetProperty("test", "activated");
            return Task.CompletedTask;
        }

        protected override Task OnDeactivateAsync(CancellationToken cancellationToken = default)
        {
            RemoveProperty("test");
            return Task.CompletedTask;
        }
    }
}