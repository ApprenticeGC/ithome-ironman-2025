using GameConsole.UI.Core;
using Xunit;

namespace GameConsole.UI.Core.Tests;

public class UIModelsTests
{
    [Fact]
    public void UIContext_CreatesCorrectly()
    {
        // Arrange
        var args = new[] { "arg1", "arg2" };
        var state = new Dictionary<string, object> { { "key1", "value1" } };
        var mode = ConsoleMode.Game;
        var preferences = new UIPreferences();

        // Act
        var context = new UIContext(args, state, mode, preferences);

        // Assert
        Assert.Equal(args, context.Args);
        Assert.Equal(state, context.State);
        Assert.Equal(mode, context.CurrentMode);
        Assert.Equal(preferences, context.Preferences);
    }

    [Fact]
    public void UIPreferences_HasCorrectDefaults()
    {
        // Act
        var preferences = new UIPreferences();

        // Assert
        Assert.True(preferences.UseColor);
        Assert.Equal(120, preferences.MaxWidth);
        Assert.Equal(40, preferences.MaxHeight);
        Assert.Equal("Default", preferences.Theme);
    }

    [Fact]
    public void UIPreferences_CanOverrideDefaults()
    {
        // Act
        var preferences = new UIPreferences(
            UseColor: false,
            MaxWidth: 80,
            MaxHeight: 25,
            Theme: "Dark");

        // Assert
        Assert.False(preferences.UseColor);
        Assert.Equal(80, preferences.MaxWidth);
        Assert.Equal(25, preferences.MaxHeight);
        Assert.Equal("Dark", preferences.Theme);
    }

    [Fact]
    public void CommandSet_CreatesCorrectly()
    {
        // Arrange
        var commands = new List<UICommand>
        {
            new UICommand("test", "Test command", _ => Task.FromResult(new UICommandResult(true)))
        };
        var aliases = new Dictionary<string, string> { { "t", "test" } };

        // Act
        var commandSet = new CommandSet(commands, aliases, "help");

        // Assert
        Assert.Equal(commands, commandSet.Commands);
        Assert.Equal(aliases, commandSet.Aliases);
        Assert.Equal("help", commandSet.DefaultCommand);
    }

    [Fact]
    public void LayoutConfiguration_CreatesWithDefaults()
    {
        // Act
        var layout = new LayoutConfiguration(LayoutType.SingleColumn);

        // Assert
        Assert.Equal(LayoutType.SingleColumn, layout.Layout);
        Assert.Equal(1, layout.Columns);
        Assert.True(layout.ShowStatusBar);
        Assert.False(layout.ShowMenuBar);
        Assert.Equal("{mode} | {time}", layout.StatusFormat);
    }

    [Fact]
    public void LayoutConfiguration_CanOverrideDefaults()
    {
        // Act
        var layout = new LayoutConfiguration(
            Layout: LayoutType.TwoColumn,
            Columns: 2,
            ShowStatusBar: false,
            ShowMenuBar: true,
            StatusFormat: "Custom: {status}");

        // Assert
        Assert.Equal(LayoutType.TwoColumn, layout.Layout);
        Assert.Equal(2, layout.Columns);
        Assert.False(layout.ShowStatusBar);
        Assert.True(layout.ShowMenuBar);
        Assert.Equal("Custom: {status}", layout.StatusFormat);
    }

    [Fact]
    public void UIProfileMetadata_HasCorrectDefaults()
    {
        // Act
        var metadata = new UIProfileMetadata();

        // Assert
        Assert.Equal("", metadata.Description);
        Assert.Equal("1.0.0", metadata.Version);
        Assert.Equal("", metadata.Author);
        Assert.False(metadata.IsBuiltIn);
    }

    [Fact]
    public void UIConfiguration_CreatesCorrectly()
    {
        // Arrange
        var preferences = new UIPreferences();
        var settings = new Dictionary<string, object> { { "debug", true } };

        // Act
        var config = new UIConfiguration(preferences, settings);

        // Assert
        Assert.Equal(preferences, config.Preferences);
        Assert.Equal(settings, config.Settings);
        Assert.True(config.EnableLogging);
        Assert.False(config.EnableMetrics);
    }

    [Fact]
    public void UICommandResult_Success_CreatesCorrectly()
    {
        // Act
        var result = new UICommandResult(true, "Success message");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Success message", result.Message);
        Assert.Null(result.Data);
        Assert.Null(result.Error);
    }

    [Fact]
    public void UICommandResult_WithData_CreatesCorrectly()
    {
        // Arrange
        var data = new Dictionary<string, object> { { "count", 42 } };

        // Act
        var result = new UICommandResult(true, "Success", data);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Success", result.Message);
        Assert.Equal(data, result.Data);
        Assert.Null(result.Error);
    }

    [Fact]
    public void UICommandResult_WithError_CreatesCorrectly()
    {
        // Arrange
        var error = new InvalidOperationException("Test error");

        // Act
        var result = new UICommandResult(false, "Failed", Error: error);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Failed", result.Message);
        Assert.Null(result.Data);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void UIRenderRequest_CreatesCorrectly()
    {
        // Arrange
        var layout = new LayoutConfiguration(LayoutType.SingleColumn);
        var options = new UIRenderOptions();

        // Act
        var request = new UIRenderRequest("Test content", layout, options);

        // Assert
        Assert.Equal("Test content", request.Content);
        Assert.Equal(layout, request.Layout);
        Assert.Equal(options, request.Options);
    }

    [Fact]
    public void UIRenderOptions_HasCorrectDefaults()
    {
        // Act
        var options = new UIRenderOptions();

        // Assert
        Assert.False(options.ClearScreen);
        Assert.True(options.UseColor);
        Assert.Equal(0, options.IndentLevel);
        Assert.Null(options.Prefix);
    }

    [Fact]
    public void UIInputRequest_CreatesWithDefaults()
    {
        // Act
        var request = new UIInputRequest("Enter value:");

        // Assert
        Assert.Equal("Enter value:", request.Prompt);
        Assert.Null(request.DefaultValue);
        Assert.False(request.IsPassword);
        Assert.True(request.IsRequired);
        Assert.Null(request.ValidationPattern);
    }

    [Fact]
    public void UIInputRequest_CanOverrideDefaults()
    {
        // Act
        var request = new UIInputRequest(
            Prompt: "Password:",
            DefaultValue: "default",
            IsPassword: true,
            IsRequired: false,
            ValidationPattern: @"^\d+$");

        // Assert
        Assert.Equal("Password:", request.Prompt);
        Assert.Equal("default", request.DefaultValue);
        Assert.True(request.IsPassword);
        Assert.False(request.IsRequired);
        Assert.Equal(@"^\d+$", request.ValidationPattern);
    }

    [Fact]
    public void UIMessage_CreatesWithMinimalParams()
    {
        // Act
        var message = new UIMessage(UIMessageType.Info, "Test message");

        // Assert
        Assert.Equal(UIMessageType.Info, message.Type);
        Assert.Equal("Test message", message.Content);
        Assert.Null(message.Title);
        Assert.False(message.RequireConfirmation);
    }

    [Fact]
    public void UIMessage_CreatesWithAllParams()
    {
        // Act
        var message = new UIMessage(
            Type: UIMessageType.Warning,
            Content: "Warning message",
            Title: "Warning Title",
            RequireConfirmation: true);

        // Assert
        Assert.Equal(UIMessageType.Warning, message.Type);
        Assert.Equal("Warning message", message.Content);
        Assert.Equal("Warning Title", message.Title);
        Assert.True(message.RequireConfirmation);
    }
}