using GameConsole.UI.Profiles;
using Xunit;

namespace GameConsole.UI.Profiles.Tests;

/// <summary>
/// Tests for UI Profile configuration classes and data models
/// </summary>
public class ConfigurationTests
{
    [Fact]
    public void ConsoleMode_HasExpectedValues()
    {
        // Act & Assert
        Assert.Equal(0, (int)ConsoleMode.Game);
        Assert.Equal(1, (int)ConsoleMode.Editor);
        
        var values = Enum.GetValues<ConsoleMode>();
        Assert.Equal(2, values.Length);
    }

    [Fact]
    public void UIProfileMetadata_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var metadata = new UIProfileMetadata();

        // Assert
        Assert.Empty(metadata.DisplayName);
        Assert.Empty(metadata.Description);
        Assert.Equal("System", metadata.Author);
        Assert.Equal("1.0.0", metadata.Version);
        Assert.Empty(metadata.Tags);
        Assert.True(metadata.IsSystemProfile);
        Assert.Equal(0, metadata.Priority);
        Assert.Empty(metadata.Properties);
    }

    [Fact]
    public void CommandSet_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var commandSet = new CommandSet();

        // Assert
        Assert.Empty(commandSet.Categories);
        Assert.Equal("General", commandSet.DefaultCategory);
        Assert.Empty(commandSet.GlobalCommands);
    }

    [Fact]
    public void CommandDefinition_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var command = new CommandDefinition();

        // Assert
        Assert.Empty(command.Id);
        Assert.Empty(command.Name);
        Assert.Empty(command.Description);
        Assert.Empty(command.KeyBinding);
        Assert.Empty(command.AlternateKeyBindings);
        Assert.Equal(0, command.Priority);
        Assert.True(command.IsEnabled);
        Assert.True(command.IsVisible);
        Assert.Empty(command.Icon);
    }

    [Fact]
    public void LayoutConfiguration_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var layout = new LayoutConfiguration();

        // Assert
        Assert.Equal("Default", layout.LayoutTemplate);
        Assert.Empty(layout.Panels);
        Assert.Equal("Main", layout.DefaultFocusPanel);
        Assert.True(layout.AllowPanelResize);
        Assert.True(layout.AllowPanelReorder);
        Assert.Equal(200, layout.MinPanelWidth);
        Assert.Equal(100, layout.MinPanelHeight);
    }

    [Fact]
    public void PanelConfiguration_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var panel = new PanelConfiguration();

        // Assert
        Assert.Empty(panel.Id);
        Assert.Empty(panel.Title);
        Assert.Equal("General", panel.ContentType);
        Assert.Equal("auto", panel.Width);
        Assert.Equal("auto", panel.Height);
        Assert.Equal("Center", panel.DockPosition);
        Assert.True(panel.IsVisible);
        Assert.True(panel.CanClose);
        Assert.Equal(0, panel.Priority);
        Assert.Empty(panel.Properties);
    }

    [Fact]
    public void KeyBindingSet_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var keyBindings = new KeyBindingSet();

        // Assert
        Assert.Empty(keyBindings.GlobalBindings);
        Assert.Empty(keyBindings.ContextualBindings);
        Assert.Empty(keyBindings.SystemOverrides);
        Assert.True(keyBindings.AllowUserCustomization);
    }

    [Fact]
    public void MenuConfiguration_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var menuConfig = new MenuConfiguration();

        // Assert
        Assert.NotNull(menuConfig.MainMenu);
        Assert.Empty(menuConfig.ContextMenus);
        Assert.True(menuConfig.ShowKeyboardShortcuts);
        Assert.True(menuConfig.ShowIcons);
    }

    [Fact]
    public void MenuBarConfiguration_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var menuBar = new MenuBarConfiguration();

        // Assert
        Assert.Empty(menuBar.Items);
        Assert.True(menuBar.IsVisible);
        Assert.Equal("Top", menuBar.Position);
    }

    [Fact]
    public void MenuItemConfiguration_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var menuItem = new MenuItemConfiguration();

        // Assert
        Assert.Empty(menuItem.Id);
        Assert.Empty(menuItem.Text);
        Assert.Empty(menuItem.Command);
        Assert.Empty(menuItem.Icon);
        Assert.Empty(menuItem.ShortcutText);
        Assert.True(menuItem.IsEnabled);
        Assert.True(menuItem.IsVisible);
        Assert.False(menuItem.IsSeparator);
        Assert.Empty(menuItem.SubItems);
        Assert.Equal(0, menuItem.Priority);
    }

    [Fact]
    public void StatusBarConfiguration_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var statusBar = new StatusBarConfiguration();

        // Assert
        Assert.True(statusBar.IsVisible);
        Assert.Equal("Bottom", statusBar.Position);
        Assert.Empty(statusBar.Segments);
        Assert.Equal("Ready", statusBar.DefaultText);
        Assert.Equal(0, statusBar.AutoHideDelayMs);
    }

    [Fact]
    public void StatusBarSegment_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var segment = new StatusBarSegment();

        // Assert
        Assert.Empty(segment.Id);
        Assert.Equal("Text", segment.Type);
        Assert.Equal("auto", segment.Width);
        Assert.Equal("Left", segment.Alignment);
        Assert.True(segment.IsVisible);
        Assert.Equal(0, segment.Priority);
        Assert.Equal("{0}", segment.Format);
        Assert.Empty(segment.Properties);
    }

    [Fact]
    public void ToolbarConfiguration_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var toolbarConfig = new ToolbarConfiguration();

        // Assert
        Assert.Empty(toolbarConfig.Toolbars);
        Assert.True(toolbarConfig.AllowUserCustomization);
        Assert.True(toolbarConfig.AllowDocking);
        Assert.Equal("Main", toolbarConfig.DefaultToolbar);
    }

    [Fact]
    public void ToolbarDefinition_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var toolbar = new ToolbarDefinition();

        // Assert
        Assert.Empty(toolbar.Id);
        Assert.Empty(toolbar.Name);
        Assert.Equal("Top", toolbar.Position);
        Assert.True(toolbar.IsVisible);
        Assert.Empty(toolbar.Items);
        Assert.Equal(0, toolbar.Priority);
    }

    [Fact]
    public void ToolbarItemDefinition_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var toolbarItem = new ToolbarItemDefinition();

        // Assert
        Assert.Empty(toolbarItem.Id);
        Assert.Equal("Button", toolbarItem.Type);
        Assert.Empty(toolbarItem.Command);
        Assert.Empty(toolbarItem.Icon);
        Assert.Empty(toolbarItem.Tooltip);
        Assert.Empty(toolbarItem.Text);
        Assert.True(toolbarItem.IsEnabled);
        Assert.True(toolbarItem.IsVisible);
        Assert.Equal(0, toolbarItem.Priority);
        Assert.Empty(toolbarItem.Properties);
    }

    [Fact]
    public void UIProfileActivationContext_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var context = new UIProfileActivationContext();

        // Assert
        Assert.Null(context.PreviousProfile);
        Assert.Equal("Manual", context.Reason);
        Assert.Empty(context.Properties);
    }

    [Fact]
    public void UIProfileDeactivationContext_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var context = new UIProfileDeactivationContext();

        // Assert
        Assert.Null(context.NextProfile);
        Assert.Equal("Manual", context.Reason);
        Assert.Empty(context.Properties);
    }

    [Fact]
    public void ProfileChangedEventArgs_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var previousProfile = new TestUIProfile("previous");
        var currentProfile = new TestUIProfile("current");
        const string reason = "Test Switch";

        // Act
        var eventArgs = new ProfileChangedEventArgs(previousProfile, currentProfile, reason);

        // Assert
        Assert.Equal(previousProfile, eventArgs.PreviousProfile);
        Assert.Equal(currentProfile, eventArgs.CurrentProfile);
        Assert.Equal(reason, eventArgs.Reason);
    }

    [Fact]
    public void ProfileChangedEventArgs_WithNullReason_SetsEmptyString()
    {
        // Act
        var eventArgs = new ProfileChangedEventArgs(null, null, null!);

        // Assert
        Assert.Empty(eventArgs.Reason);
    }

    /// <summary>
    /// Simple test profile implementation for testing
    /// </summary>
    private class TestUIProfile : UIProfile
    {
        private readonly string _id;

        public TestUIProfile(string id)
        {
            _id = id;
        }

        public override string Id => _id;
        public override string Name => $"Test Profile {_id}";
        public override ConsoleMode TargetMode => ConsoleMode.Game;
        public override UIProfileMetadata Metadata => new UIProfileMetadata();

        public override CommandSet GetCommandSet() => new CommandSet();
        public override LayoutConfiguration GetLayoutConfiguration() => new LayoutConfiguration();
        public override KeyBindingSet GetKeyBindings() => new KeyBindingSet();
        public override MenuConfiguration GetMenuConfiguration() => new MenuConfiguration();
        public override StatusBarConfiguration GetStatusBarConfiguration() => new StatusBarConfiguration();
        public override ToolbarConfiguration GetToolbarConfiguration() => new ToolbarConfiguration();
    }
}