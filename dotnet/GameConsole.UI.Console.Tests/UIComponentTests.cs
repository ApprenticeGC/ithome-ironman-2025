using GameConsole.UI.Core;
using GameConsole.UI.Console.Components;
using Xunit;

namespace GameConsole.UI.Console.Tests;

/// <summary>
/// Tests for UI component implementations.
/// </summary>
public class UIComponentTests
{
    [Fact]
    public void Label_Constructor_ShouldSetPropertiesCorrectly()
    {
        // Arrange & Act
        var label = new Label("test-label", "Hello World", new Position(5, 10));

        // Assert
        Assert.Equal("test-label", label.Id);
        Assert.Equal("Hello World", label.Text);
        Assert.Equal(5, label.Position.X);
        Assert.Equal(10, label.Position.Y);
        Assert.Equal(11, label.Size.Width); // Text length
        Assert.Equal(1, label.Size.Height);
        Assert.True(label.IsVisible);
    }

    [Fact]
    public void Label_WithNullText_ShouldHandleGracefully()
    {
        // Arrange & Act
        var label = new Label("test-label", null!, new Position(0, 0));

        // Assert
        Assert.Equal("", label.Text);
        Assert.Equal(0, label.Size.Width);
    }

    [Fact]
    public void Label_UpdateText_ShouldUpdateTextAndSize()
    {
        // Arrange
        var label = new Label("test-label", "Short", new Position(0, 0));

        // Act
        label.UpdateText("This is a much longer text");

        // Assert
        Assert.Equal("This is a much longer text", label.Text);
        Assert.Equal(26, label.Size.Width);
        Assert.Equal(1, label.Size.Height);
    }

    [Fact]
    public void Panel_Constructor_ShouldSetPropertiesCorrectly()
    {
        // Arrange & Act
        var panel = new Panel("test-panel", new Position(0, 0), new Size(20, 10), "Test Panel");

        // Assert
        Assert.Equal("test-panel", panel.Id);
        Assert.Equal("Test Panel", panel.Title);
        Assert.Equal(0, panel.Position.X);
        Assert.Equal(0, panel.Position.Y);
        Assert.Equal(20, panel.Size.Width);
        Assert.Equal(10, panel.Size.Height);
        Assert.Equal(BorderStyle.Single, panel.BorderStyle);
        Assert.Empty(panel.Children);
    }

    [Fact]
    public void Panel_AddChild_ShouldAddComponentToChildren()
    {
        // Arrange
        var panel = new Panel("test-panel", new Position(0, 0), new Size(20, 10));
        var label = new Label("child-label", "Child", new Position(1, 1));

        // Act
        panel.AddChild(label);

        // Assert
        Assert.Single(panel.Children);
        Assert.Contains(label, panel.Children);
    }

    [Fact]
    public void Panel_AddChild_WithNullComponent_ShouldThrowArgumentNullException()
    {
        // Arrange
        var panel = new Panel("test-panel", new Position(0, 0), new Size(20, 10));

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => panel.AddChild(null!));
    }

    [Fact]
    public void Panel_RemoveChild_WithExistingComponent_ShouldRemoveAndReturnTrue()
    {
        // Arrange
        var panel = new Panel("test-panel", new Position(0, 0), new Size(20, 10));
        var label = new Label("child-label", "Child", new Position(1, 1));
        panel.AddChild(label);

        // Act
        var result = panel.RemoveChild("child-label");

        // Assert
        Assert.True(result);
        Assert.Empty(panel.Children);
    }

    [Fact]
    public void Panel_RemoveChild_WithNonExistingComponent_ShouldReturnFalse()
    {
        // Arrange
        var panel = new Panel("test-panel", new Position(0, 0), new Size(20, 10));

        // Act
        var result = panel.RemoveChild("non-existing");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Button_Constructor_ShouldSetPropertiesCorrectly()
    {
        // Arrange & Act
        var button = new Button("test-button", "Click Me", new Position(5, 5));

        // Assert
        Assert.Equal("test-button", button.Id);
        Assert.Equal("Click Me", button.Text);
        Assert.Equal(5, button.Position.X);
        Assert.Equal(5, button.Position.Y);
        Assert.Equal(12, button.Size.Width); // Text length + padding
        Assert.Equal(3, button.Size.Height);
        Assert.False(button.IsSelected);
        Assert.NotNull(button.Style);
    }

    [Fact]
    public void Button_SetSelected_ShouldUpdateProperty()
    {
        // Arrange
        var button = new Button("test-button", "Click Me", new Position(0, 0));

        // Act
        button.IsSelected = true;

        // Assert
        Assert.True(button.IsSelected);
    }

    [Theory]
    [InlineData(BorderStyle.Single)]
    [InlineData(BorderStyle.Double)]
    [InlineData(BorderStyle.Thick)]
    public void Panel_WithDifferentBorderStyles_ShouldSetCorrectly(BorderStyle borderStyle)
    {
        // Arrange & Act
        var panel = new Panel("test-panel", new Position(0, 0), new Size(10, 5), borderStyle: borderStyle);

        // Assert
        Assert.Equal(borderStyle, panel.BorderStyle);
    }

    [Fact]
    public void UIComponentBase_Bounds_ShouldReturnCorrectRectangle()
    {
        // Arrange
        var label = new Label("test", "Test", new Position(10, 20));

        // Act
        var bounds = label.Bounds;

        // Assert
        Assert.Equal(10, bounds.Position.X);
        Assert.Equal(20, bounds.Position.Y);
        Assert.Equal(4, bounds.Size.Width); // "Test".Length
        Assert.Equal(1, bounds.Size.Height);
    }

    [Fact]
    public void UIComponentBase_VisibilityToggle_ShouldWorkCorrectly()
    {
        // Arrange
        var label = new Label("test", "Test", new Position(0, 0));
        Assert.True(label.IsVisible); // Default

        // Act & Assert
        label.IsVisible = false;
        Assert.False(label.IsVisible);

        label.IsVisible = true;
        Assert.True(label.IsVisible);
    }
}