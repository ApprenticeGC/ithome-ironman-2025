using GameConsole.UI.Core;
using Xunit;

namespace GameConsole.UI.Core.Tests;

public class EditorProfileTests
{
    [Fact]
    public void EditorProfile_HasCorrectProperties()
    {
        // Act
        var profile = new EditorProfile();

        // Assert
        Assert.Equal("Editor", profile.Name);
        Assert.Equal(ConsoleMode.Editor, profile.TargetMode);
        Assert.True(profile.SupportedCapabilities.HasFlag(UICapabilities.TextDisplay));
        Assert.True(profile.SupportedCapabilities.HasFlag(UICapabilities.MenuNavigation));
        Assert.True(profile.SupportedCapabilities.HasFlag(UICapabilities.FormInput));
        Assert.True(profile.SupportedCapabilities.HasFlag(UICapabilities.TableDisplay));
        Assert.True(profile.SupportedCapabilities.HasFlag(UICapabilities.TreeView));
        Assert.True(profile.SupportedCapabilities.HasFlag(UICapabilities.KeyboardShortcuts));
        Assert.True(profile.SupportedCapabilities.HasFlag(UICapabilities.MouseInteraction));
        Assert.True(profile.SupportedCapabilities.HasFlag(UICapabilities.ColorDisplay));
        Assert.True(profile.SupportedCapabilities.HasFlag(UICapabilities.ProgressIndicators));
        Assert.True(profile.SupportedCapabilities.HasFlag(UICapabilities.StatusBar));
    }

    [Fact]
    public void EditorProfile_MetadataIsCorrect()
    {
        // Act
        var profile = new EditorProfile();

        // Assert
        Assert.Equal("Editor mode profile for content creation and development", profile.Metadata.Description);
        Assert.Equal("1.0.0", profile.Metadata.Version);
        Assert.Equal("GameConsole", profile.Metadata.Author);
        Assert.True(profile.Metadata.IsBuiltIn);
    }

    [Fact]
    public void EditorProfile_GetCommandSet_ReturnsExpectedCommands()
    {
        // Arrange
        var profile = new EditorProfile();

        // Act
        var commandSet = profile.GetCommandSet();

        // Assert
        Assert.NotEmpty(commandSet.Commands);
        Assert.Contains(commandSet.Commands, c => c.Name == "create");
        Assert.Contains(commandSet.Commands, c => c.Name == "edit");
        Assert.Contains(commandSet.Commands, c => c.Name == "build");
        Assert.Contains(commandSet.Commands, c => c.Name == "test");
        Assert.Contains(commandSet.Commands, c => c.Name == "deploy");
        Assert.Contains(commandSet.Commands, c => c.Name == "import");
        Assert.Contains(commandSet.Commands, c => c.Name == "export");
        Assert.Contains(commandSet.Commands, c => c.Name == "validate");
        Assert.Contains(commandSet.Commands, c => c.Name == "help");
        Assert.Contains(commandSet.Commands, c => c.Name == "exit");
        Assert.Equal("help", commandSet.DefaultCommand);
    }

    [Fact]
    public void EditorProfile_GetCommandSet_HasCorrectAliases()
    {
        // Arrange
        var profile = new EditorProfile();

        // Act
        var commandSet = profile.GetCommandSet();

        // Assert
        Assert.NotEmpty(commandSet.Aliases);
        Assert.Equal("create", commandSet.Aliases["new"]);
        Assert.Equal("create", commandSet.Aliases["c"]);
        Assert.Equal("edit", commandSet.Aliases["e"]);
        Assert.Equal("build", commandSet.Aliases["b"]);
        Assert.Equal("test", commandSet.Aliases["t"]);
        Assert.Equal("deploy", commandSet.Aliases["d"]);
        Assert.Equal("import", commandSet.Aliases["i"]);
        Assert.Equal("export", commandSet.Aliases["x"]);
        Assert.Equal("validate", commandSet.Aliases["v"]);
        Assert.Equal("help", commandSet.Aliases["h"]);
        Assert.Equal("exit", commandSet.Aliases["q"]);
    }

    [Fact]
    public void EditorProfile_GetLayoutConfiguration_ReturnsCorrectLayout()
    {
        // Arrange
        var profile = new EditorProfile();

        // Act
        var layout = profile.GetLayoutConfiguration();

        // Assert
        Assert.Equal(LayoutType.TwoColumn, layout.Layout);
        Assert.Equal(2, layout.Columns);
        Assert.True(layout.ShowStatusBar);
        Assert.True(layout.ShowMenuBar);
        Assert.Equal("EDITOR | {time} | {project} | {mode}", layout.StatusFormat);
    }

    [Theory]
    [InlineData("create", "Asset creation wizard started")]
    [InlineData("edit", "Asset editor opened")]
    [InlineData("build", "Build process started")]
    [InlineData("test", "Test suite executed")]
    [InlineData("deploy", "Deployment initiated")]
    [InlineData("import", "Import wizard opened")]
    [InlineData("export", "Export process started")]
    [InlineData("exit", "Exiting editor mode...")]
    public async Task EditorProfile_HandleCommand_BasicCommands_ReturnsSuccess(string command, string expectedMessage)
    {
        // Arrange
        var profile = new EditorProfile();
        var context = CreateTestContext();

        // Act
        var result = await profile.HandleCommandAsync(command, context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(expectedMessage, result.Message);
    }

    [Fact]
    public async Task EditorProfile_HandleCommand_ValidateCommand_ReturnsDataAndSuccess()
    {
        // Arrange
        var profile = new EditorProfile();
        var context = CreateTestContext();

        // Act
        var result = await profile.HandleCommandAsync("validate", context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Project validation completed", result.Message);
        Assert.NotNull(result.Data);
        Assert.Contains("errors", result.Data.Keys);
        Assert.Contains("warnings", result.Data.Keys);
        Assert.Contains("assets_checked", result.Data.Keys);
        Assert.Equal(0, result.Data["errors"]);
        Assert.Equal(2, result.Data["warnings"]);
        Assert.Equal(45, result.Data["assets_checked"]);
    }

    [Fact]
    public async Task EditorProfile_HandleCommand_HelpCommand_ReturnsHelpText()
    {
        // Arrange
        var profile = new EditorProfile();
        var context = CreateTestContext();

        // Act
        var result = await profile.HandleCommandAsync("help", context);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("create - Create new asset or project", result.Message);
        Assert.Contains("edit - Edit existing asset", result.Message);
        Assert.Contains("build - Build project or assets", result.Message);
        Assert.Contains("validate - Validate project integrity", result.Message);
    }

    [Theory]
    [InlineData("new", "create", "Asset creation wizard started")]
    [InlineData("c", "create", "Asset creation wizard started")]
    [InlineData("e", "edit", "Asset editor opened")]
    [InlineData("b", "build", "Build process started")]
    [InlineData("t", "test", "Test suite executed")]
    [InlineData("d", "deploy", "Deployment initiated")]
    [InlineData("i", "import", "Import wizard opened")]
    [InlineData("x", "export", "Export process started")]
    [InlineData("v", "validate", "Project validation completed")]
    [InlineData("h", "help", null)] // Help message is complex, just check success
    [InlineData("q", "exit", "Exiting editor mode...")]
    public async Task EditorProfile_HandleCommand_WithAliases_ReturnsCorrectResult(string alias, string expectedCommand, string? expectedMessage)
    {
        // Arrange
        var profile = new EditorProfile();
        var context = CreateTestContext();

        // Act
        var result = await profile.HandleCommandAsync(alias, context);

        // Assert
        Assert.True(result.Success);
        if (expectedMessage != null)
        {
            Assert.Equal(expectedMessage, result.Message);
        }
    }

    [Fact]
    public async Task EditorProfile_HandleCommand_UnknownCommand_ReturnsFailure()
    {
        // Arrange
        var profile = new EditorProfile();
        var context = CreateTestContext();

        // Act
        var result = await profile.HandleCommandAsync("unknown", context);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Unknown command: unknown", result.Message);
    }

    [Fact]
    public void EditorProfile_SupportsMode_Editor_ReturnsTrue()
    {
        // Arrange
        var profile = new EditorProfile();

        // Act
        var supports = profile.SupportsMode(ConsoleMode.Editor);

        // Assert
        Assert.True(supports);
    }

    [Fact]
    public void EditorProfile_SupportsMode_Game_ReturnsFalse()
    {
        // Arrange
        var profile = new EditorProfile();

        // Act
        var supports = profile.SupportsMode(ConsoleMode.Game);

        // Assert
        Assert.False(supports);
    }

    [Fact]
    public void EditorProfile_HasCapability_AllSupportedCapabilities_ReturnsTrue()
    {
        // Arrange
        var profile = new EditorProfile();

        // Act & Assert - Test all capabilities that should be supported
        Assert.True(profile.HasCapability(UICapabilities.TextDisplay));
        Assert.True(profile.HasCapability(UICapabilities.MenuNavigation));
        Assert.True(profile.HasCapability(UICapabilities.FormInput));
        Assert.True(profile.HasCapability(UICapabilities.TableDisplay));
        Assert.True(profile.HasCapability(UICapabilities.TreeView));
        Assert.True(profile.HasCapability(UICapabilities.KeyboardShortcuts));
        Assert.True(profile.HasCapability(UICapabilities.MouseInteraction));
        Assert.True(profile.HasCapability(UICapabilities.ColorDisplay));
        Assert.True(profile.HasCapability(UICapabilities.ProgressIndicators));
        Assert.True(profile.HasCapability(UICapabilities.StatusBar));
    }

    [Fact]
    public void EditorProfile_Configure_DoesNotThrow()
    {
        // Arrange
        var profile = new EditorProfile();
        var config = new UIConfiguration(
            new UIPreferences(),
            new Dictionary<string, object>());

        // Act & Assert
        var exception = Record.Exception(() => profile.Configure(config));
        Assert.Null(exception);
    }

    [Fact]
    public async Task EditorProfile_InitializeAsync_DoesNotThrow()
    {
        // Arrange
        var profile = new EditorProfile();

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => profile.InitializeAsync());
        Assert.Null(exception);
    }

    [Fact]
    public async Task EditorProfile_DisposeAsync_DoesNotThrow()
    {
        // Arrange
        var profile = new EditorProfile();

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => profile.DisposeAsync());
        Assert.Null(exception);
    }

    private static UIContext CreateTestContext()
    {
        return new UIContext(
            Args: Array.Empty<string>(),
            State: new Dictionary<string, object>(),
            CurrentMode: ConsoleMode.Editor,
            Preferences: new UIPreferences());
    }
}