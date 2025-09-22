using FluentAssertions;
using GameConsole.UI.Profiles.Implementations;

namespace GameConsole.UI.Profiles.Tests;

/// <summary>
/// Tests for the GameModeProfile implementation.
/// </summary>
public class GameModeProfileTests
{
    [Fact]
    public void Constructor_SetsExpectedProperties()
    {
        // Act
        var profile = new GameModeProfile();
        
        // Assert
        profile.Name.Should().Be("GameModeProfile");
        profile.TargetMode.Should().Be(ConsoleMode.Game);
        profile.Metadata.Should().NotBeNull();
        profile.Metadata.DisplayName.Should().Be("Game Mode");
        profile.Metadata.Priority.Should().Be(100);
    }
    
    [Fact]
    public void GetCommandSet_ReturnsGameModeCommands()
    {
        // Arrange
        var profile = new GameModeProfile();
        
        // Act
        var commandSet = profile.GetCommandSet();
        
        // Assert
        commandSet.Should().NotBeNull();
        commandSet.Commands.Should().NotBeEmpty();
        
        // Check for specific game mode commands
        var startCommand = commandSet.GetCommand("game.start");
        startCommand.Should().NotBeNull();
        startCommand!.Name.Should().Be("Start Game");
        startCommand.KeyBinding.Should().Be("F5");
        
        var debugCommand = commandSet.GetCommand("debug.console");
        debugCommand.Should().NotBeNull();
        debugCommand!.Name.Should().Be("Debug Console");
    }
    
    [Fact]
    public void GetLayoutConfiguration_ReturnsGameModeLayout()
    {
        // Arrange
        var profile = new GameModeProfile();
        
        // Act
        var layout = profile.GetLayoutConfiguration();
        
        // Assert
        layout.Should().NotBeNull();
        layout.MinimumConsoleWidth.Should().Be(80);
        layout.MinimumConsoleHeight.Should().Be(25);
        layout.Panels.Should().NotBeEmpty();
        
        // Check for game viewport panel
        var viewportPanel = layout.Panels.FirstOrDefault(p => p.Id == "game.viewport");
        viewportPanel.Should().NotBeNull();
        viewportPanel!.Name.Should().Be("Game Viewport");
        viewportPanel.IsVisible.Should().BeTrue();
    }
    
    [Fact]
    public void GetKeyBindings_ReturnsGameModeBindings()
    {
        // Arrange
        var profile = new GameModeProfile();
        
        // Act
        var keyBindings = profile.GetKeyBindings();
        
        // Assert
        keyBindings.Should().NotBeNull();
        keyBindings.Bindings.Should().NotBeEmpty();
        
        var f5Binding = keyBindings.GetBinding("F5");
        f5Binding.Should().NotBeNull();
        f5Binding!.CommandId.Should().Be("game.start");
    }
    
    [Fact]
    public void IsApplicable_ReturnsTrueForGameMode()
    {
        // Arrange
        var profile = new GameModeProfile();
        var context = new UIProfileContext { Mode = ConsoleMode.Game };
        
        // Act
        var result = profile.IsApplicable(context);
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Fact]
    public void IsApplicable_ReturnsFalseForEditorMode()
    {
        // Arrange
        var profile = new GameModeProfile();
        var context = new UIProfileContext { Mode = ConsoleMode.Editor };
        
        // Act
        var result = profile.IsApplicable(context);
        
        // Assert
        result.Should().BeFalse();
    }
}