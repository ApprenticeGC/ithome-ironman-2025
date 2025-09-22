using GameConsole.UI.Core;
using Xunit;

namespace GameConsole.UI.Core.Tests;

/// <summary>
/// Tests for UI model classes and their behavior.
/// </summary>
public class UIModelsTests
{
    [Fact]
    public void UIMessage_Should_Initialize_With_Correct_Properties()
    {
        // Arrange
        var content = "Test message";
        var messageType = MessageType.Info;
        var beforeCreation = DateTime.UtcNow;

        // Act
        var message = new UIMessage(content, messageType);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(content, message.Content);
        Assert.Equal(messageType, message.Type);
        Assert.True(message.Timestamp >= beforeCreation);
        Assert.True(message.Timestamp <= afterCreation);
    }

    [Theory]
    [InlineData(MessageType.Info)]
    [InlineData(MessageType.Warning)]
    [InlineData(MessageType.Error)]
    [InlineData(MessageType.Success)]
    public void UIMessage_Should_Support_All_Message_Types(MessageType messageType)
    {
        // Arrange & Act
        var message = new UIMessage("Test", messageType);

        // Assert
        Assert.Equal(messageType, message.Type);
    }

    [Fact]
    public void MenuItem_Should_Initialize_With_Required_Properties()
    {
        // Arrange
        var id = "test-item";
        var display = "Test Item";

        // Act
        var menuItem = new MenuItem(id, display);

        // Assert
        Assert.Equal(id, menuItem.Id);
        Assert.Equal(display, menuItem.Display);
        Assert.Null(menuItem.Description);
        Assert.True(menuItem.IsEnabled);
    }

    [Fact]
    public void MenuItem_Should_Initialize_With_All_Properties()
    {
        // Arrange
        var id = "test-item";
        var display = "Test Item";
        var description = "Test description";
        var isEnabled = false;

        // Act
        var menuItem = new MenuItem(id, display, description, isEnabled);

        // Assert
        Assert.Equal(id, menuItem.Id);
        Assert.Equal(display, menuItem.Display);
        Assert.Equal(description, menuItem.Description);
        Assert.Equal(isEnabled, menuItem.IsEnabled);
    }

    [Fact]
    public void Menu_Should_Initialize_With_Title_And_Items()
    {
        // Arrange
        var title = "Test Menu";
        var items = new[]
        {
            new MenuItem("item1", "Item 1"),
            new MenuItem("item2", "Item 2"),
            new MenuItem("item3", "Item 3")
        };

        // Act
        var menu = new Menu(title, items);

        // Assert
        Assert.Equal(title, menu.Title);
        Assert.Equal(3, menu.Items.Count);
        Assert.Equal("item1", menu.Items[0].Id);
        Assert.Equal("item2", menu.Items[1].Id);
        Assert.Equal("item3", menu.Items[2].Id);
    }

    [Fact]
    public void Menu_Items_Should_Be_ReadOnly()
    {
        // Arrange
        var items = new[]
        {
            new MenuItem("item1", "Item 1")
        };
        var menu = new Menu("Test Menu", items);

        // Act & Assert
        Assert.IsAssignableFrom<IReadOnlyList<MenuItem>>(menu.Items);
    }

    [Fact]
    public void Menu_Should_Handle_Empty_Items_List()
    {
        // Arrange
        var title = "Empty Menu";
        var items = Array.Empty<MenuItem>();

        // Act
        var menu = new Menu(title, items);

        // Assert
        Assert.Equal(title, menu.Title);
        Assert.Empty(menu.Items);
    }
}