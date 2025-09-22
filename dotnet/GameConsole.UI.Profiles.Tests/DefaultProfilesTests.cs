using GameConsole.UI.Profiles;
using Xunit;

namespace GameConsole.UI.Profiles.Tests;

/// <summary>
/// Tests for the default UI profiles (Game and Editor modes)
/// </summary>
public class DefaultProfilesTests
{
    [Fact]
    public void DefaultGameProfile_HasCorrectProperties()
    {
        // Arrange & Act
        var profile = new DefaultGameProfile();

        // Assert
        Assert.Equal("system.game.default", profile.Id);
        Assert.Equal("Default Game Profile", profile.Name);
        Assert.Equal(ConsoleMode.Game, profile.TargetMode);
        Assert.NotNull(profile.Metadata);
        Assert.Equal("Default Game Mode", profile.Metadata.DisplayName);
        Assert.True(profile.Metadata.IsSystemProfile);
        Assert.Equal(100, profile.Metadata.Priority);
    }

    [Fact]
    public void DefaultGameProfile_CommandSet_HasGameCommands()
    {
        // Arrange & Act
        var profile = new DefaultGameProfile();
        var commandSet = profile.GetCommandSet();

        // Assert
        Assert.NotNull(commandSet);
        Assert.Equal("Game", commandSet.DefaultCategory);
        Assert.Contains(commandSet.Categories, kvp => kvp.Key == "Game");
        Assert.Contains(commandSet.Categories, kvp => kvp.Key == "Debug");
        
        var gameCommands = commandSet.Categories["Game"];
        Assert.Contains(gameCommands, c => c.Id == "game.play");
        Assert.Contains(gameCommands, c => c.Id == "game.pause");
        Assert.Contains(gameCommands, c => c.Id == "game.stop");
    }

    [Fact]
    public void DefaultGameProfile_LayoutConfiguration_HasGameViewPanel()
    {
        // Arrange & Act
        var profile = new DefaultGameProfile();
        var layout = profile.GetLayoutConfiguration();

        // Assert
        Assert.NotNull(layout);
        Assert.Equal("Game", layout.LayoutTemplate);
        Assert.Equal("GameView", layout.DefaultFocusPanel);
        Assert.Contains(layout.Panels, p => p.Id == "GameView" && p.ContentType == "GameRenderer");
        Assert.Contains(layout.Panels, p => p.Id == "Console" && p.ContentType == "LogOutput");
    }

    [Fact]
    public void DefaultGameProfile_KeyBindings_HasGameControls()
    {
        // Arrange & Act
        var profile = new DefaultGameProfile();
        var keyBindings = profile.GetKeyBindings();

        // Assert
        Assert.NotNull(keyBindings);
        Assert.True(keyBindings.AllowUserCustomization);
        Assert.Contains(keyBindings.GlobalBindings, kvp => kvp.Key == "F5" && kvp.Value == "game.play");
        Assert.Contains(keyBindings.GlobalBindings, kvp => kvp.Key == "Space" && kvp.Value == "game.pause");
        Assert.Contains(keyBindings.GlobalBindings, kvp => kvp.Key == "Shift+F5" && kvp.Value == "game.stop");
    }

    [Fact]
    public void DefaultGameProfile_ValidatesSuccessfully()
    {
        // Arrange
        var profile = new DefaultGameProfile();

        // Act
        var validationErrors = profile.Validate();

        // Assert
        Assert.Empty(validationErrors);
    }

    [Fact]
    public void DefaultEditorProfile_HasCorrectProperties()
    {
        // Arrange & Act
        var profile = new DefaultEditorProfile();

        // Assert
        Assert.Equal("system.editor.default", profile.Id);
        Assert.Equal("Default Editor Profile", profile.Name);
        Assert.Equal(ConsoleMode.Editor, profile.TargetMode);
        Assert.NotNull(profile.Metadata);
        Assert.Equal("Default Editor Mode", profile.Metadata.DisplayName);
        Assert.True(profile.Metadata.IsSystemProfile);
        Assert.Equal(100, profile.Metadata.Priority);
    }

    [Fact]
    public void DefaultEditorProfile_CommandSet_HasEditorCommands()
    {
        // Arrange & Act
        var profile = new DefaultEditorProfile();
        var commandSet = profile.GetCommandSet();

        // Assert
        Assert.NotNull(commandSet);
        Assert.Equal("Edit", commandSet.DefaultCategory);
        Assert.Contains(commandSet.Categories, kvp => kvp.Key == "File");
        Assert.Contains(commandSet.Categories, kvp => kvp.Key == "Edit");
        Assert.Contains(commandSet.Categories, kvp => kvp.Key == "Assets");
        Assert.Contains(commandSet.Categories, kvp => kvp.Key == "Build");
        
        var fileCommands = commandSet.Categories["File"];
        Assert.Contains(fileCommands, c => c.Id == "file.new");
        Assert.Contains(fileCommands, c => c.Id == "file.open");
        Assert.Contains(fileCommands, c => c.Id == "file.save");
    }

    [Fact]
    public void DefaultEditorProfile_LayoutConfiguration_HasEditorPanels()
    {
        // Arrange & Act
        var profile = new DefaultEditorProfile();
        var layout = profile.GetLayoutConfiguration();

        // Assert
        Assert.NotNull(layout);
        Assert.Equal("Editor", layout.LayoutTemplate);
        Assert.Equal("SceneView", layout.DefaultFocusPanel);
        Assert.Contains(layout.Panels, p => p.Id == "SceneView" && p.ContentType == "SceneEditor");
        Assert.Contains(layout.Panels, p => p.Id == "Hierarchy" && p.ContentType == "SceneHierarchy");
        Assert.Contains(layout.Panels, p => p.Id == "Inspector" && p.ContentType == "PropertyEditor");
        Assert.Contains(layout.Panels, p => p.Id == "AssetBrowser" && p.ContentType == "AssetBrowser");
    }

    [Fact]
    public void DefaultEditorProfile_KeyBindings_HasEditorControls()
    {
        // Arrange & Act
        var profile = new DefaultEditorProfile();
        var keyBindings = profile.GetKeyBindings();

        // Assert
        Assert.NotNull(keyBindings);
        Assert.True(keyBindings.AllowUserCustomization);
        Assert.Contains(keyBindings.GlobalBindings, kvp => kvp.Key == "Ctrl+N" && kvp.Value == "file.new");
        Assert.Contains(keyBindings.GlobalBindings, kvp => kvp.Key == "Ctrl+S" && kvp.Value == "file.save");
        Assert.Contains(keyBindings.GlobalBindings, kvp => kvp.Key == "Ctrl+Z" && kvp.Value == "edit.undo");
        Assert.Contains(keyBindings.GlobalBindings, kvp => kvp.Key == "Ctrl+Y" && kvp.Value == "edit.redo");
    }

    [Fact]
    public void DefaultEditorProfile_MenuConfiguration_HasEditorMenus()
    {
        // Arrange & Act
        var profile = new DefaultEditorProfile();
        var menuConfig = profile.GetMenuConfiguration();

        // Assert
        Assert.NotNull(menuConfig);
        Assert.True(menuConfig.ShowKeyboardShortcuts);
        Assert.True(menuConfig.ShowIcons);
        Assert.True(menuConfig.MainMenu.IsVisible);
        Assert.Contains(menuConfig.MainMenu.Items, m => m.Id == "menu.file");
        Assert.Contains(menuConfig.MainMenu.Items, m => m.Id == "menu.edit");
        Assert.Contains(menuConfig.MainMenu.Items, m => m.Id == "menu.assets");
        Assert.Contains(menuConfig.MainMenu.Items, m => m.Id == "menu.build");
    }

    [Fact]
    public void DefaultEditorProfile_StatusBarConfiguration_HasEditorStatusSegments()
    {
        // Arrange & Act
        var profile = new DefaultEditorProfile();
        var statusConfig = profile.GetStatusBarConfiguration();

        // Assert
        Assert.NotNull(statusConfig);
        Assert.True(statusConfig.IsVisible);
        Assert.Equal("Editor Mode - Ready", statusConfig.DefaultText);
        Assert.Contains(statusConfig.Segments, s => s.Id == "mode");
        Assert.Contains(statusConfig.Segments, s => s.Id == "selection");
        Assert.Contains(statusConfig.Segments, s => s.Id == "tool");
    }

    [Fact]
    public void DefaultEditorProfile_ToolbarConfiguration_HasEditorToolbars()
    {
        // Arrange & Act
        var profile = new DefaultEditorProfile();
        var toolbarConfig = profile.GetToolbarConfiguration();

        // Assert
        Assert.NotNull(toolbarConfig);
        Assert.True(toolbarConfig.AllowUserCustomization);
        Assert.True(toolbarConfig.AllowDocking);
        Assert.Equal("MainTools", toolbarConfig.DefaultToolbar);
        Assert.Contains(toolbarConfig.Toolbars, t => t.Id == "MainTools");
        Assert.Contains(toolbarConfig.Toolbars, t => t.Id == "SceneTools");
        
        var mainToolbar = toolbarConfig.Toolbars.First(t => t.Id == "MainTools");
        Assert.Contains(mainToolbar.Items, i => i.Command == "file.new");
        Assert.Contains(mainToolbar.Items, i => i.Command == "file.save");
        Assert.Contains(mainToolbar.Items, i => i.Command == "build.build");
    }

    [Fact]
    public void DefaultEditorProfile_ValidatesSuccessfully()
    {
        // Arrange
        var profile = new DefaultEditorProfile();

        // Act
        var validationErrors = profile.Validate();

        // Assert
        Assert.Empty(validationErrors);
    }
}