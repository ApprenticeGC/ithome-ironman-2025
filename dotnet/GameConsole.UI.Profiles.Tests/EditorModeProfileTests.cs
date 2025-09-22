using FluentAssertions;
using GameConsole.UI.Profiles.Implementations;

namespace GameConsole.UI.Profiles.Tests;

/// <summary>
/// Tests for the EditorModeProfile implementation.
/// </summary>
public class EditorModeProfileTests
{
    [Fact]
    public void Constructor_SetsExpectedProperties()
    {
        // Act
        var profile = new EditorModeProfile();
        
        // Assert
        profile.Name.Should().Be("EditorModeProfile");
        profile.TargetMode.Should().Be(ConsoleMode.Editor);
        profile.Metadata.Should().NotBeNull();
        profile.Metadata.DisplayName.Should().Be("Editor Mode");
        profile.Metadata.Priority.Should().Be(100);
    }
    
    [Fact]
    public void GetCommandSet_ReturnsEditorModeCommands()
    {
        // Arrange
        var profile = new EditorModeProfile();
        
        // Act
        var commandSet = profile.GetCommandSet();
        
        // Assert
        commandSet.Should().NotBeNull();
        commandSet.Commands.Should().NotBeEmpty();
        
        // Check for specific editor mode commands
        var newCommand = commandSet.GetCommand("file.new");
        newCommand.Should().NotBeNull();
        newCommand!.Name.Should().Be("New File");
        newCommand.KeyBinding.Should().Be("Ctrl+N");
        
        var buildCommand = commandSet.GetCommand("project.build");
        buildCommand.Should().NotBeNull();
        buildCommand!.Name.Should().Be("Build Project");
    }
    
    [Fact]
    public void GetLayoutConfiguration_ReturnsEditorModeLayout()
    {
        // Arrange
        var profile = new EditorModeProfile();
        
        // Act
        var layout = profile.GetLayoutConfiguration();
        
        // Assert
        layout.Should().NotBeNull();
        layout.MinimumConsoleWidth.Should().Be(120);
        layout.MinimumConsoleHeight.Should().Be(30);
        layout.Panels.Should().NotBeEmpty();
        
        // Check for hierarchy panel
        var hierarchyPanel = layout.Panels.FirstOrDefault(p => p.Id == "editor.hierarchy");
        hierarchyPanel.Should().NotBeNull();
        hierarchyPanel!.Name.Should().Be("Hierarchy");
        hierarchyPanel.Position.Should().Be("left");
    }
    
    [Fact]
    public void GetKeyBindings_ReturnsEditorModeBindings()
    {
        // Arrange
        var profile = new EditorModeProfile();
        
        // Act
        var keyBindings = profile.GetKeyBindings();
        
        // Assert
        keyBindings.Should().NotBeNull();
        keyBindings.Bindings.Should().NotBeEmpty();
        
        var ctrlNBinding = keyBindings.GetBinding("Ctrl+N");
        ctrlNBinding.Should().NotBeNull();
        ctrlNBinding!.CommandId.Should().Be("file.new");
        
        var ctrlSBinding = keyBindings.GetBinding("Ctrl+S");
        ctrlSBinding.Should().NotBeNull();
        ctrlSBinding!.CommandId.Should().Be("file.save");
    }
    
    [Fact]
    public void GetMenuConfiguration_ReturnsEditorMenus()
    {
        // Arrange
        var profile = new EditorModeProfile();
        
        // Act
        var menuConfig = profile.GetMenuConfiguration();
        
        // Assert
        menuConfig.Should().NotBeNull();
        menuConfig.MainMenu.Should().NotBeEmpty();
        
        // Check for File menu
        var fileMenu = menuConfig.MainMenu.FirstOrDefault(m => m.Id == "file.menu");
        fileMenu.Should().NotBeNull();
        fileMenu!.Text.Should().Be("&File");
        fileMenu.Children.Should().NotBeEmpty();
    }
    
    [Fact]
    public void IsApplicable_ReturnsTrueForEditorMode()
    {
        // Arrange
        var profile = new EditorModeProfile();
        var context = new UIProfileContext { Mode = ConsoleMode.Editor };
        
        // Act
        var result = profile.IsApplicable(context);
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Fact]
    public void IsApplicable_ReturnsFalseForGameMode()
    {
        // Arrange
        var profile = new EditorModeProfile();
        var context = new UIProfileContext { Mode = ConsoleMode.Game };
        
        // Act
        var result = profile.IsApplicable(context);
        
        // Assert
        result.Should().BeFalse();
    }
}