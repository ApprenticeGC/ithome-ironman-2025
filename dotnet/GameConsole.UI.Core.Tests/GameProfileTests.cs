using GameConsole.UI.Core;
using Xunit;

namespace GameConsole.UI.Core.Tests;

public class GameProfileTests
{
    [Fact]
    public void GameProfile_HasCorrectProperties()
    {
        // Act
        var profile = new GameProfile();

        // Assert
        Assert.Equal("Game", profile.Name);
        Assert.Equal(ConsoleMode.Game, profile.TargetMode);
        Assert.True(profile.SupportedCapabilities.HasFlag(UICapabilities.TextDisplay));
        Assert.True(profile.SupportedCapabilities.HasFlag(UICapabilities.KeyboardShortcuts));
        Assert.True(profile.SupportedCapabilities.HasFlag(UICapabilities.StatusBar));
        Assert.True(profile.SupportedCapabilities.HasFlag(UICapabilities.ColorDisplay));
        Assert.False(profile.SupportedCapabilities.HasFlag(UICapabilities.MenuNavigation));
    }

    [Fact]
    public void GameProfile_MetadataIsCorrect()
    {
        // Act
        var profile = new GameProfile();

        // Assert
        Assert.Equal("Game mode profile for runtime operations", profile.Metadata.Description);
        Assert.Equal("1.0.0", profile.Metadata.Version);
        Assert.Equal("GameConsole", profile.Metadata.Author);
        Assert.True(profile.Metadata.IsBuiltIn);
    }

    [Fact]
    public void GameProfile_GetCommandSet_ReturnsExpectedCommands()
    {
        // Arrange
        var profile = new GameProfile();

        // Act
        var commandSet = profile.GetCommandSet();

        // Assert
        Assert.NotEmpty(commandSet.Commands);
        Assert.Contains(commandSet.Commands, c => c.Name == "play");
        Assert.Contains(commandSet.Commands, c => c.Name == "pause");
        Assert.Contains(commandSet.Commands, c => c.Name == "debug");
        Assert.Contains(commandSet.Commands, c => c.Name == "stats");
        Assert.Contains(commandSet.Commands, c => c.Name == "quit");
        Assert.Contains(commandSet.Commands, c => c.Name == "help");
        Assert.Equal("help", commandSet.DefaultCommand);
    }

    [Fact]
    public void GameProfile_GetCommandSet_HasCorrectAliases()
    {
        // Arrange
        var profile = new GameProfile();

        // Act
        var commandSet = profile.GetCommandSet();

        // Assert
        Assert.NotEmpty(commandSet.Aliases);
        Assert.Equal("play", commandSet.Aliases["p"]);
        Assert.Equal("debug", commandSet.Aliases["d"]);
        Assert.Equal("stats", commandSet.Aliases["s"]);
        Assert.Equal("quit", commandSet.Aliases["q"]);
        Assert.Equal("help", commandSet.Aliases["h"]);
    }

    [Fact]
    public void GameProfile_GetLayoutConfiguration_ReturnsCorrectLayout()
    {
        // Arrange
        var profile = new GameProfile();

        // Act
        var layout = profile.GetLayoutConfiguration();

        // Assert
        Assert.Equal(LayoutType.SingleColumn, layout.Layout);
        Assert.Equal(1, layout.Columns);
        Assert.True(layout.ShowStatusBar);
        Assert.False(layout.ShowMenuBar);
        Assert.Equal("GAME | {time} | {status}", layout.StatusFormat);
    }

    [Fact]
    public async Task GameProfile_HandleCommand_PlayCommand_ReturnsSuccess()
    {
        // Arrange
        var profile = new GameProfile();
        var context = CreateTestContext();

        // Act
        var result = await profile.HandleCommandAsync("play", context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Game started", result.Message);
    }

    [Fact]
    public async Task GameProfile_HandleCommand_PauseCommand_ReturnsSuccess()
    {
        // Arrange
        var profile = new GameProfile();
        var context = CreateTestContext();

        // Act
        var result = await profile.HandleCommandAsync("pause", context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Game paused", result.Message);
    }

    [Fact]
    public async Task GameProfile_HandleCommand_DebugCommand_ReturnsSuccess()
    {
        // Arrange
        var profile = new GameProfile();
        var context = CreateTestContext();

        // Act
        var result = await profile.HandleCommandAsync("debug", context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Debug mode toggled", result.Message);
    }

    [Fact]
    public async Task GameProfile_HandleCommand_StatsCommand_ReturnsDataAndSuccess()
    {
        // Arrange
        var profile = new GameProfile();
        var context = CreateTestContext();

        // Act
        var result = await profile.HandleCommandAsync("stats", context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Game statistics retrieved", result.Message);
        Assert.NotNull(result.Data);
        Assert.Contains("fps", result.Data.Keys);
        Assert.Contains("memory", result.Data.Keys);
        Assert.Contains("uptime", result.Data.Keys);
    }

    [Fact]
    public async Task GameProfile_HandleCommand_QuitCommand_ReturnsSuccess()
    {
        // Arrange
        var profile = new GameProfile();
        var context = CreateTestContext();

        // Act
        var result = await profile.HandleCommandAsync("quit", context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Exiting game...", result.Message);
    }

    [Fact]
    public async Task GameProfile_HandleCommand_HelpCommand_ReturnsHelpText()
    {
        // Arrange
        var profile = new GameProfile();
        var context = CreateTestContext();

        // Act
        var result = await profile.HandleCommandAsync("help", context);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("play - Start or resume gameplay", result.Message);
        Assert.Contains("pause - Pause current game", result.Message);
        Assert.Contains("quit - Exit the game", result.Message);
    }

    [Fact]
    public async Task GameProfile_HandleCommand_WithAlias_ReturnsCorrectResult()
    {
        // Arrange
        var profile = new GameProfile();
        var context = CreateTestContext();

        // Act
        var result = await profile.HandleCommandAsync("p", context); // Alias for "play"

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Game started", result.Message);
    }

    [Fact]
    public async Task GameProfile_HandleCommand_UnknownCommand_ReturnsFailure()
    {
        // Arrange
        var profile = new GameProfile();
        var context = CreateTestContext();

        // Act
        var result = await profile.HandleCommandAsync("unknown", context);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Unknown command: unknown", result.Message);
    }

    [Fact]
    public void GameProfile_SupportsMode_Game_ReturnsTrue()
    {
        // Arrange
        var profile = new GameProfile();

        // Act
        var supports = profile.SupportsMode(ConsoleMode.Game);

        // Assert
        Assert.True(supports);
    }

    [Fact]
    public void GameProfile_SupportsMode_Editor_ReturnsFalse()
    {
        // Arrange
        var profile = new GameProfile();

        // Act
        var supports = profile.SupportsMode(ConsoleMode.Editor);

        // Assert
        Assert.False(supports);
    }

    [Fact]
    public void GameProfile_HasCapability_SupportedCapabilities_ReturnsTrue()
    {
        // Arrange
        var profile = new GameProfile();

        // Act & Assert
        Assert.True(profile.HasCapability(UICapabilities.TextDisplay));
        Assert.True(profile.HasCapability(UICapabilities.StatusBar));
        Assert.True(profile.HasCapability(UICapabilities.KeyboardShortcuts));
        Assert.True(profile.HasCapability(UICapabilities.ColorDisplay));
    }

    [Fact]
    public void GameProfile_HasCapability_UnsupportedCapabilities_ReturnsFalse()
    {
        // Arrange
        var profile = new GameProfile();

        // Act & Assert
        Assert.False(profile.HasCapability(UICapabilities.MenuNavigation));
        Assert.False(profile.HasCapability(UICapabilities.FormInput));
        Assert.False(profile.HasCapability(UICapabilities.TreeView));
    }

    [Fact]
    public void GameProfile_Configure_DoesNotThrow()
    {
        // Arrange
        var profile = new GameProfile();
        var config = new UIConfiguration(
            new UIPreferences(),
            new Dictionary<string, object>());

        // Act & Assert
        var exception = Record.Exception(() => profile.Configure(config));
        Assert.Null(exception);
    }

    [Fact]
    public async Task GameProfile_InitializeAsync_DoesNotThrow()
    {
        // Arrange
        var profile = new GameProfile();

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => profile.InitializeAsync());
        Assert.Null(exception);
    }

    [Fact]
    public async Task GameProfile_DisposeAsync_DoesNotThrow()
    {
        // Arrange
        var profile = new GameProfile();

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => profile.DisposeAsync());
        Assert.Null(exception);
    }

    private static UIContext CreateTestContext()
    {
        return new UIContext(
            Args: Array.Empty<string>(),
            State: new Dictionary<string, object>(),
            CurrentMode: ConsoleMode.Game,
            Preferences: new UIPreferences());
    }
}