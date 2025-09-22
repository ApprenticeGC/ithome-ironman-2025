using GameConsole.UI.Core;
using Xunit;

namespace GameConsole.UI.Core.Tests;

public class UICommandTests
{
    [Fact]
    public void UICommand_CreatesWithMinimalParameters()
    {
        // Arrange
        var handler = new UICommandHandler(_ => Task.FromResult(new UICommandResult(true)));

        // Act
        var command = new UICommand("test", "Test command", handler);

        // Assert
        Assert.Equal("test", command.Name);
        Assert.Equal("Test command", command.Description);
        Assert.Equal(handler, command.Handler);
        Assert.Equal(UICommandPriority.Normal, command.Priority);
        Assert.Empty(command.Aliases);
        Assert.False(command.RequiresConfirmation);
        Assert.Equal(ConsoleMode.All, command.SupportedModes);
    }

    [Fact]
    public void UICommand_CreatesWithAllParameters()
    {
        // Arrange
        var handler = new UICommandHandler(_ => Task.FromResult(new UICommandResult(true)));
        var aliases = new[] { "t", "tst" };

        // Act
        var command = new UICommand(
            Name: "test",
            Description: "Test command",
            Handler: handler,
            Priority: UICommandPriority.High,
            Aliases: aliases,
            RequiresConfirmation: true,
            SupportedModes: ConsoleMode.Game);

        // Assert
        Assert.Equal("test", command.Name);
        Assert.Equal("Test command", command.Description);
        Assert.Equal(handler, command.Handler);
        Assert.Equal(UICommandPriority.High, command.Priority);
        Assert.Equal(aliases, command.Aliases);
        Assert.True(command.RequiresConfirmation);
        Assert.Equal(ConsoleMode.Game, command.SupportedModes);
    }

    [Fact]
    public void UICommand_WithNullAliases_ReturnsEmptyArray()
    {
        // Arrange
        var handler = new UICommandHandler(_ => Task.FromResult(new UICommandResult(true)));

        // Act
        var command = new UICommand("test", "Test command", handler, Aliases: null);

        // Assert
        Assert.NotNull(command.Aliases);
        Assert.Empty(command.Aliases);
    }

    [Fact]
    public async Task UICommand_HandlerCanBeInvoked()
    {
        // Arrange
        var expectedResult = new UICommandResult(true, "Handler executed");
        var handler = new UICommandHandler(_ => Task.FromResult(expectedResult));
        var command = new UICommand("test", "Test command", handler);
        var context = CreateTestContext();

        // Act
        var result = await command.Handler(context);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void UICommandAttribute_CreatesWithMinimalParameters()
    {
        // Act
        var attribute = new UICommandAttribute("test", "Test command");

        // Assert
        Assert.Equal("test", attribute.Name);
        Assert.Equal("Test command", attribute.Description);
        Assert.Equal(UICommandPriority.Normal, attribute.Priority);
        Assert.Equal(ConsoleMode.All, attribute.SupportedModes);
        Assert.False(attribute.RequiresConfirmation);
    }

    [Fact]
    public void UICommandAttribute_CanSetAllProperties()
    {
        // Act
        var attribute = new UICommandAttribute("test", "Test command")
        {
            Priority = UICommandPriority.Critical,
            SupportedModes = ConsoleMode.Editor,
            RequiresConfirmation = true
        };

        // Assert
        Assert.Equal("test", attribute.Name);
        Assert.Equal("Test command", attribute.Description);
        Assert.Equal(UICommandPriority.Critical, attribute.Priority);
        Assert.Equal(ConsoleMode.Editor, attribute.SupportedModes);
        Assert.True(attribute.RequiresConfirmation);
    }

    [Theory]
    [InlineData(UICommandPriority.Low)]
    [InlineData(UICommandPriority.Normal)]
    [InlineData(UICommandPriority.High)]
    [InlineData(UICommandPriority.Critical)]
    public void UICommand_AcceptsAllPriorityValues(UICommandPriority priority)
    {
        // Arrange
        var handler = new UICommandHandler(_ => Task.FromResult(new UICommandResult(true)));

        // Act
        var command = new UICommand("test", "Test command", handler, Priority: priority);

        // Assert
        Assert.Equal(priority, command.Priority);
    }

    [Theory]
    [InlineData(ConsoleMode.Game)]
    [InlineData(ConsoleMode.Editor)]
    [InlineData(ConsoleMode.Debug)]
    [InlineData(ConsoleMode.All)]
    [InlineData(ConsoleMode.Game | ConsoleMode.Editor)]
    public void UICommand_AcceptsAllModeValues(ConsoleMode modes)
    {
        // Arrange
        var handler = new UICommandHandler(_ => Task.FromResult(new UICommandResult(true)));

        // Act
        var command = new UICommand("test", "Test command", handler, SupportedModes: modes);

        // Assert
        Assert.Equal(modes, command.SupportedModes);
    }

    [Fact]
    public async Task UICommand_HandlerReturnsFailureResult()
    {
        // Arrange
        var expectedError = new InvalidOperationException("Test error");
        var handler = new UICommandHandler(_ => Task.FromResult(
            new UICommandResult(false, "Command failed", Error: expectedError)));
        var command = new UICommand("test", "Test command", handler);
        var context = CreateTestContext();

        // Act
        var result = await command.Handler(context);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Command failed", result.Message);
        Assert.Equal(expectedError, result.Error);
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