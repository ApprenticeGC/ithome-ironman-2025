using GameConsole.UI.Profiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameConsole.UI.Profiles.Tests;

/// <summary>
/// Tests for the UI Profile Manager service
/// </summary>
public class UIProfileManagerTests
{
    private readonly Mock<ILogger<UIProfileManager>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly UIProfileManager _profileManager;

    public UIProfileManagerTests()
    {
        _mockLogger = new Mock<ILogger<UIProfileManager>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _profileManager = new UIProfileManager(_mockLogger.Object, _mockConfiguration.Object);
    }

    [Fact]
    public void Constructor_WithValidParameters_InitializesCorrectly()
    {
        // Arrange & Act
        var manager = new UIProfileManager(_mockLogger.Object, _mockConfiguration.Object);

        // Assert
        Assert.NotNull(manager);
        Assert.False(manager.IsRunning);
        Assert.Equal(ConsoleMode.Game, manager.CurrentMode);
        Assert.Null(manager.CurrentProfile);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new UIProfileManager(null!, _mockConfiguration.Object));
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new UIProfileManager(_mockLogger.Object, null!));
    }

    [Fact]
    public async Task InitializeAsync_LoadsBuiltInProfiles()
    {
        // Act
        await _profileManager.InitializeAsync();

        // Assert
        var availableProfiles = await _profileManager.GetAvailableProfilesAsync();
        Assert.Contains(availableProfiles, p => p.Id == "system.game.default");
        Assert.Contains(availableProfiles, p => p.Id == "system.editor.default");
    }

    [Fact]
    public async Task StartAsync_SwitchesToDefaultGameProfile()
    {
        // Arrange
        await _profileManager.InitializeAsync();

        // Act
        await _profileManager.StartAsync();

        // Assert
        Assert.True(_profileManager.IsRunning);
        Assert.NotNull(_profileManager.CurrentProfile);
        Assert.Equal("system.game.default", _profileManager.CurrentProfile.Id);
        Assert.Equal(ConsoleMode.Game, _profileManager.CurrentMode);
    }

    [Fact]
    public async Task GetProfilesForModeAsync_Game_ReturnsGameProfiles()
    {
        // Arrange
        await _profileManager.InitializeAsync();

        // Act
        var gameProfiles = await _profileManager.GetProfilesForModeAsync(ConsoleMode.Game);

        // Assert
        Assert.All(gameProfiles, p => Assert.Equal(ConsoleMode.Game, p.TargetMode));
        Assert.Contains(gameProfiles, p => p.Id == "system.game.default");
    }

    [Fact]
    public async Task GetProfilesForModeAsync_Editor_ReturnsEditorProfiles()
    {
        // Arrange
        await _profileManager.InitializeAsync();

        // Act
        var editorProfiles = await _profileManager.GetProfilesForModeAsync(ConsoleMode.Editor);

        // Assert
        Assert.All(editorProfiles, p => Assert.Equal(ConsoleMode.Editor, p.TargetMode));
        Assert.Contains(editorProfiles, p => p.Id == "system.editor.default");
    }

    [Fact]
    public async Task GetProfileByIdAsync_ExistingProfile_ReturnsProfile()
    {
        // Arrange
        await _profileManager.InitializeAsync();

        // Act
        var profile = await _profileManager.GetProfileByIdAsync("system.game.default");

        // Assert
        Assert.NotNull(profile);
        Assert.Equal("system.game.default", profile.Id);
        Assert.Equal(ConsoleMode.Game, profile.TargetMode);
    }

    [Fact]
    public async Task GetProfileByIdAsync_NonExistentProfile_ReturnsNull()
    {
        // Arrange
        await _profileManager.InitializeAsync();

        // Act
        var profile = await _profileManager.GetProfileByIdAsync("nonexistent.profile");

        // Assert
        Assert.Null(profile);
    }

    [Fact]
    public async Task SwitchToProfileAsync_ValidProfile_SwitchesSuccessfully()
    {
        // Arrange
        await _profileManager.InitializeAsync();
        await _profileManager.StartAsync();
        ProfileChangedEventArgs? eventArgs = null;
        _profileManager.ProfileChanged += (sender, args) => eventArgs = args;

        // Act
        var result = await _profileManager.SwitchToProfileAsync("system.editor.default");

        // Assert
        Assert.True(result);
        Assert.Equal("system.editor.default", _profileManager.CurrentProfile?.Id);
        Assert.Equal(ConsoleMode.Editor, _profileManager.CurrentMode);
        Assert.NotNull(eventArgs);
        Assert.Equal("system.editor.default", eventArgs.CurrentProfile?.Id);
    }

    [Fact]
    public async Task SwitchToProfileAsync_InvalidProfile_ReturnsFalse()
    {
        // Arrange
        await _profileManager.InitializeAsync();
        await _profileManager.StartAsync();

        // Act
        var result = await _profileManager.SwitchToProfileAsync("invalid.profile");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SwitchToModeAsync_ValidMode_SwitchesSuccessfully()
    {
        // Arrange
        await _profileManager.InitializeAsync();
        await _profileManager.StartAsync();

        // Act
        var result = await _profileManager.SwitchToModeAsync(ConsoleMode.Editor);

        // Assert
        Assert.True(result);
        Assert.Equal(ConsoleMode.Editor, _profileManager.CurrentMode);
        Assert.Equal(ConsoleMode.Editor, _profileManager.CurrentProfile?.TargetMode);
    }

    [Fact]
    public async Task RegisterProfileAsync_ValidProfile_RegistersSuccessfully()
    {
        // Arrange
        await _profileManager.InitializeAsync();
        var customProfile = new TestUIProfile();

        // Act
        var result = await _profileManager.RegisterProfileAsync(customProfile);

        // Assert
        Assert.True(result);
        var retrievedProfile = await _profileManager.GetProfileByIdAsync("test.profile");
        Assert.NotNull(retrievedProfile);
        Assert.Equal("test.profile", retrievedProfile.Id);
    }

    [Fact]
    public async Task RegisterProfileAsync_NullProfile_ReturnsFalse()
    {
        // Arrange
        await _profileManager.InitializeAsync();

        // Act
        var result = await _profileManager.RegisterProfileAsync(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UnregisterProfileAsync_ExistingProfile_UnregistersSuccessfully()
    {
        // Arrange
        await _profileManager.InitializeAsync();
        var customProfile = new TestUIProfile();
        await _profileManager.RegisterProfileAsync(customProfile);

        // Act
        var result = await _profileManager.UnregisterProfileAsync("test.profile");

        // Assert
        Assert.True(result);
        var retrievedProfile = await _profileManager.GetProfileByIdAsync("test.profile");
        Assert.Null(retrievedProfile);
    }

    [Fact]
    public async Task StopAsync_WhenRunning_StopsGracefully()
    {
        // Arrange
        await _profileManager.InitializeAsync();
        await _profileManager.StartAsync();
        Assert.True(_profileManager.IsRunning);

        // Act
        await _profileManager.StopAsync();

        // Assert
        Assert.False(_profileManager.IsRunning);
    }

    [Fact]
    public async Task DisposeAsync_CleansUpResources()
    {
        // Arrange
        await _profileManager.InitializeAsync();
        await _profileManager.StartAsync();

        // Act
        await _profileManager.DisposeAsync();

        // Assert
        Assert.False(_profileManager.IsRunning);
    }

    [Fact]
    public void ServiceMetadata_HasCorrectValues()
    {
        // Act & Assert
        Assert.Equal("UI Profile Manager", _profileManager.Name);
        Assert.Equal("1.0.0", _profileManager.Version);
        Assert.Contains("UI", _profileManager.Categories);
        Assert.Contains("Profiles", _profileManager.Categories);
        Assert.Contains("Configuration", _profileManager.Categories);
        Assert.NotEmpty(_profileManager.Properties);
    }

    /// <summary>
    /// Test UI Profile implementation for testing purposes
    /// </summary>
    private class TestUIProfile : UIProfile
    {
        public override string Id => "test.profile";
        public override string Name => "Test Profile";
        public override ConsoleMode TargetMode => ConsoleMode.Game;
        public override UIProfileMetadata Metadata => new UIProfileMetadata
        {
            DisplayName = "Test Profile",
            Description = "A test profile for unit testing",
            Author = "Test",
            Version = "1.0.0"
        };

        public override CommandSet GetCommandSet()
        {
            return new CommandSet
            {
                GlobalCommands = new List<CommandDefinition>
                {
                    new CommandDefinition
                    {
                        Id = "test.command",
                        Name = "Test Command",
                        Description = "A test command"
                    }
                }
            };
        }

        public override LayoutConfiguration GetLayoutConfiguration()
        {
            return new LayoutConfiguration { LayoutTemplate = "Test" };
        }

        public override KeyBindingSet GetKeyBindings()
        {
            return new KeyBindingSet();
        }

        public override MenuConfiguration GetMenuConfiguration()
        {
            return new MenuConfiguration();
        }

        public override StatusBarConfiguration GetStatusBarConfiguration()
        {
            return new StatusBarConfiguration();
        }

        public override ToolbarConfiguration GetToolbarConfiguration()
        {
            return new ToolbarConfiguration();
        }
    }
}